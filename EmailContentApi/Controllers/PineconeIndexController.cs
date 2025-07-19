using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;

namespace EmailContentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PineconeIndexController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public PineconeIndexController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Test Pinecone API connectivity and credentials
        /// </summary>
        /// <returns>API key status and test result</returns>
        [HttpGet("test")]
        public async Task<IActionResult> TestPinecone()
        {
            var apiKey = _configuration["Pinecone:ApiKey"];
            var environment = _configuration["Pinecone:Environment"];
            var projectId = _configuration["Pinecone:ProjectId"];

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(environment))
            {
                return BadRequest("Pinecone API key or environment is not configured.");
            }

            try
            {
                // Use the configured PineconeClient with proxy settings
                var client = _httpClientFactory.CreateClient("PineconeClient");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Api-Key", apiKey);
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    if (!client.DefaultRequestHeaders.Contains("x-project-id"))
                        client.DefaultRequestHeaders.Add("x-project-id", projectId);
                }

                var url = $"https://controller.{environment}.pinecone.io/databases";
                
                Console.WriteLine($"=== Pinecone Test ===");
                Console.WriteLine($"URL: {url}");
                Console.WriteLine($"API Key (first 10 chars): {apiKey.Substring(0, Math.Min(10, apiKey.Length))}...");
                Console.WriteLine($"Environment: {environment}");
                Console.WriteLine($"Project ID: {projectId}");
                Console.WriteLine($"Using configured HTTP client with proxy settings");

                var response = await client.GetAsync(url);
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Body: {responseBody}");

                return Ok(new
                {
                    message = "Pinecone API test completed",
                    statusCode = response.StatusCode,
                    isSuccess = response.IsSuccessStatusCode,
                    apiKeyPrefix = apiKey.Substring(0, Math.Min(10, apiKey.Length)) + "...",
                    environment = environment,
                    projectId = projectId,
                    url = url,
                    response = responseBody
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error testing Pinecone API", 
                    error = ex.Message,
                    apiKeyPrefix = apiKey.Substring(0, Math.Min(10, apiKey.Length)) + "...",
                    environment = environment,
                    projectId = projectId
                });
            }
        }

        /// <summary>
        /// Creates a new Pinecone index using the specified configuration
        /// </summary>
        /// <returns>Status of the index creation</returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateIndex()
        {
            var apiKey = _configuration["Pinecone:ApiKey"];
            var environment = _configuration["Pinecone:Environment"];
            var projectId = _configuration["Pinecone:ProjectId"];

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(environment))
            {
                return BadRequest("Pinecone API key or environment is not configured.");
            }

            try
            {
                // Use the configured PineconeClient with proxy settings
                var client = _httpClientFactory.CreateClient("PineconeClient");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Api-Key", apiKey);
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    if (!client.DefaultRequestHeaders.Contains("x-project-id"))
                        client.DefaultRequestHeaders.Add("x-project-id", projectId);
                }

                var result = await CreatePineconeIndexAsync(client, environment);
                return Ok(new { message = "Index created successfully!", details = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create index", error = ex.Message });
            }
        }

        /// <summary>
        /// Creates a Pinecone index with custom configuration
        /// </summary>
        /// <param name="request">Custom index configuration</param>
        /// <returns>Status of the index creation</returns>
        [HttpPost("create-custom")]
        public async Task<IActionResult> CreateCustomIndex([FromBody] PineconeIndexRequest request)
        {
            var apiKey = _configuration["Pinecone:ApiKey"];
            var environment = _configuration["Pinecone:Environment"];
            var projectId = _configuration["Pinecone:ProjectId"];

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(environment))
            {
                return BadRequest("Pinecone API key or environment is not configured.");
            }

            try
            {
                // Use the configured PineconeClient with proxy settings
                var client = _httpClientFactory.CreateClient("PineconeClient");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Api-Key", apiKey);
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    if (!client.DefaultRequestHeaders.Contains("x-project-id"))
                        client.DefaultRequestHeaders.Add("x-project-id", projectId);
                }

                var result = await CreatePineconeIndexAsync(client, environment, request);
                return Ok(new { message = "Custom index created successfully!", details = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create custom index", error = ex.Message });
            }
        }

        /// <summary>
        /// Lists all Pinecone indexes
        /// </summary>
        /// <returns>List of all indexes</returns>
        [HttpGet("list")]
        public async Task<IActionResult> ListIndexes()
        {
            var apiKey = _configuration["Pinecone:ApiKey"];
            var environment = _configuration["Pinecone:Environment"];
            var projectId = _configuration["Pinecone:ProjectId"];

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(environment))
            {
                return BadRequest("Pinecone API key or environment is not configured.");
            }

            try
            {
                // Use the configured PineconeClient with proxy settings
                var client = _httpClientFactory.CreateClient("PineconeClient");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Api-Key", apiKey);
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    if (!client.DefaultRequestHeaders.Contains("x-project-id"))
                        client.DefaultRequestHeaders.Add("x-project-id", projectId);
                }

                var response = await client.GetAsync($"https://controller.{environment}.pinecone.io/databases");
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { message = "Indexes retrieved successfully", indexes = responseBody });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { message = "Failed to retrieve indexes", error = responseBody });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving indexes", error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a Pinecone index
        /// </summary>
        /// <param name="indexName">Name of the index to delete</param>
        /// <returns>Status of the index deletion</returns>
        [HttpDelete("delete/{indexName}")]
        public async Task<IActionResult> DeleteIndex(string indexName)
        {
            var apiKey = _configuration["Pinecone:ApiKey"];
            var environment = _configuration["Pinecone:Environment"];
            var projectId = _configuration["Pinecone:ProjectId"];

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(environment))
            {
                return BadRequest("Pinecone API key or environment is not configured.");
            }

            try
            {
                // Use the configured PineconeClient with proxy settings
                var client = _httpClientFactory.CreateClient("PineconeClient");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Api-Key", apiKey);
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    if (!client.DefaultRequestHeaders.Contains("x-project-id"))
                        client.DefaultRequestHeaders.Add("x-project-id", projectId);
                }

                var response = await client.DeleteAsync($"https://controller.{environment}.pinecone.io/databases/{indexName}");
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { message = $"Index '{indexName}' deleted successfully!" });
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { message = "Failed to delete index", error = responseBody });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting index", error = ex.Message });
            }
        }

        /// <summary>
        /// Creates a Pinecone index using the provided configuration
        /// </summary>
        /// <param name="client">HTTP client</param>
        /// <param name="environment">Pinecone environment</param>
        /// <param name="customConfig">Optional custom configuration</param>
        /// <returns>Creation result</returns>
        private async Task<string> CreatePineconeIndexAsync(HttpClient client, string environment, PineconeIndexRequest? customConfig = null)
        {
            var indexConfig = customConfig ?? new PineconeIndexRequest
            {
                name = "emails-index",
                dimension = 1536,
                metric = "cosine"
            };

            var json = JsonSerializer.Serialize(indexConfig);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://controller.{environment}.pinecone.io/databases";
            var projectId = _configuration["Pinecone:ProjectId"];
            if (!string.IsNullOrWhiteSpace(projectId))
            {
                if (!client.DefaultRequestHeaders.Contains("x-project-id"))
                    client.DefaultRequestHeaders.Add("x-project-id", projectId);
            }
            // Debug information
            Console.WriteLine($"=== Pinecone API Debug ===");
            Console.WriteLine($"URL: {url}");
            Console.WriteLine($"Environment: {environment}");
            Console.WriteLine($"Project ID: {projectId}");
            Console.WriteLine($"API Key (first 10 chars): {client.DefaultRequestHeaders.Authorization?.Parameter?.Substring(0, Math.Min(10, client.DefaultRequestHeaders.Authorization?.Parameter?.Length ?? 0))}...");
            Console.WriteLine($"Authorization Header: {client.DefaultRequestHeaders.Authorization}");
            Console.WriteLine($"x-project-id Header: {projectId}");
            Console.WriteLine($"Content-Type: {content.Headers.ContentType}");
            Console.WriteLine($"Request Payload: {json}");
            Console.WriteLine($"=========================");

            var response = await client.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Response Status: {response.StatusCode}");
            Console.WriteLine($"Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");
            Console.WriteLine($"Response Body: {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Pinecone API returned {response.StatusCode}: {responseBody}");
            }

            Console.WriteLine("Index created!");
            return responseBody;
        }
    }

    public class PineconeIndexRequest
    {
        public string name { get; set; } = string.Empty;
        public int dimension { get; set; }
        public string metric { get; set; } = "cosine";
        public int replicas { get; set; } = 1;
        public int shards { get; set; } = 1;
        public int pods { get; set; } = 1;
        public string pod_type { get; set; } = "p1.x1";
    }
} 