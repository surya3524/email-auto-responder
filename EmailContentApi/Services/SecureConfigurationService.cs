using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace EmailContentApi.Services
{
    /// <summary>
    /// Service for securely managing API keys and sensitive configuration
    /// </summary>
    public class SecureConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly SecretClient? _secretClient;
        private readonly bool _useKeyVault;

        public SecureConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
            _useKeyVault = !string.IsNullOrEmpty(_configuration["KeyVault:VaultUrl"]);
            
            if (_useKeyVault)
            {
                var vaultUrl = _configuration["KeyVault:VaultUrl"];
                var credential = new DefaultAzureCredential();
                _secretClient = new SecretClient(new Uri(vaultUrl!), credential);
            }
        }

        /// <summary>
        /// Gets a secret value from Azure Key Vault or configuration
        /// </summary>
        public async Task<string?> GetSecretAsync(string secretName)
        {
            if (_useKeyVault && _secretClient != null)
            {
                try
                {
                    var secret = await _secretClient.GetSecretAsync(secretName);
                    return secret.Value.Value;
                }
                catch (Exception ex)
                {
                    // Fallback to configuration if Key Vault fails
                    Console.WriteLine($"Failed to retrieve secret '{secretName}' from Key Vault: {ex.Message}");
                    return _configuration[secretName];
                }
            }
            
            return _configuration[secretName];
        }

        /// <summary>
        /// Gets the OpenAI API key securely
        /// </summary>
        public async Task<string?> GetOpenAIApiKeyAsync()
        {
            return await GetSecretAsync("OpenAI:ApiKey");
        }

        /// <summary>
        /// Gets the Pinecone API key securely
        /// </summary>
        public async Task<string?> GetPineconeApiKeyAsync()
        {
            return await GetSecretAsync("Pinecone:ApiKey");
        }

        /// <summary>
        /// Gets the database connection string securely
        /// </summary>
        public async Task<string?> GetDatabaseConnectionStringAsync()
        {
            return await GetSecretAsync("ConnectionStrings:DefaultConnection");
        }

        /// <summary>
        /// Gets all Pinecone configuration securely
        /// </summary>
        public async Task<PineconeConfig> GetPineconeConfigAsync()
        {
            return new PineconeConfig
            {
                ApiKey = await GetPineconeApiKeyAsync(),
                Environment = _configuration["Pinecone:Environment"],
                ProjectId = _configuration["Pinecone:ProjectId"],
                IndexHost = _configuration["Pinecone:IndexHost"]
            };
        }

        /// <summary>
        /// Gets all OpenAI configuration securely
        /// </summary>
        public async Task<OpenAIConfig> GetOpenAIConfigAsync()
        {
            return new OpenAIConfig
            {
                ApiKey = await GetOpenAIApiKeyAsync(),
                Model = _configuration["OpenAI:Model"] ?? "text-embedding-3-small"
            };
        }
    }

    public class PineconeConfig
    {
        public string? ApiKey { get; set; }
        public string? Environment { get; set; }
        public string? ProjectId { get; set; }
        public string? IndexHost { get; set; }
    }

    public class OpenAIConfig
    {
        public string? ApiKey { get; set; }
        public string? Model { get; set; }
    }
} 