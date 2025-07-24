# Azure API Key Configuration Guide

This guide covers multiple secure approaches to store and access API keys in Azure for your Email Content API.

## Approach 1: Azure Key Vault (Recommended)

### 1.1 Create Azure Key Vault

```bash
# Create a resource group (if not exists)
az group create --name your-resource-group --location eastus

# Create Key Vault
az keyvault create --name your-keyvault-name --resource-group your-resource-group --location eastus --sku standard

# Enable soft delete and purge protection
az keyvault update --name your-keyvault-name --enable-soft-delete true --enable-purge-protection true
```

### 1.2 Store API Keys in Key Vault

```bash
# Store Pinecone API Key
az keyvault secret set --vault-name your-keyvault-name --name "Pinecone--ApiKey" --value "your-pinecone-api-key"

# Store OpenAI API Key
az keyvault secret set --vault-name your-keyvault-name --name "OpenAI--ApiKey" --value "your-openai-api-key"

# Store Database Connection String
az keyvault secret set --vault-name your-keyvault-name --name "ConnectionStrings--DefaultConnection" --value "your-connection-string"
```

### 1.3 Configure Managed Identity

```bash
# Create App Service with managed identity
az webapp create --name your-app-name --resource-group your-resource-group --plan your-app-service-plan

# Enable managed identity
az webapp identity assign --name your-app-name --resource-group your-resource-group

# Grant Key Vault access to the managed identity
az keyvault set-policy --name your-keyvault-name --object-id <managed-identity-object-id> --secret-permissions get list
```

### 1.4 Update Configuration

Update your `appsettings.Production.json`:

```json
{
  "KeyVault": {
    "VaultUrl": "https://your-keyvault-name.vault.azure.net/"
  },
  "Pinecone": {
    "Environment": "us-east1-gcp",
    "ProjectId": "434a5cb5-c973-4a02-954b-ca07cf753d50",
    "IndexHost": "https://integrated-dense-dotnet-6hzgm10.svc.aped-4627-b74a.pinecone.io"
  },
  "OpenAI": {
    "Model": "text-embedding-3-small"
  }
}
```

## Approach 2: Azure App Service Configuration

### 2.1 Application Settings

In Azure Portal:
1. Go to your App Service
2. Navigate to Configuration > Application settings
3. Add the following settings:

```
Pinecone__ApiKey = your-pinecone-api-key
OpenAI__ApiKey = your-openai-api-key
ConnectionStrings__DefaultConnection = your-connection-string
```

### 2.2 Using Azure CLI

```bash
# Set application settings
az webapp config appsettings set --name your-app-name --resource-group your-resource-group --settings \
  "Pinecone__ApiKey=your-pinecone-api-key" \
  "OpenAI__ApiKey=your-openai-api-key" \
  "ConnectionStrings__DefaultConnection=your-connection-string"
```

## Approach 3: Environment Variables

### 3.1 Local Development

Create `appsettings.Development.json`:

```json
{
  "Pinecone": {
    "ApiKey": "your-pinecone-api-key",
    "Environment": "us-east1-gcp",
    "ProjectId": "434a5cb5-c973-4a02-954b-ca07cf753d50",
    "IndexHost": "https://integrated-dense-dotnet-6hzgm10.svc.aped-4627-b74a.pinecone.io"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "Model": "text-embedding-3-small"
  }
}
```

### 3.2 Azure Container Instances

```bash
# Deploy with environment variables
az container create \
  --resource-group your-resource-group \
  --name your-container-name \
  --image your-registry.azurecr.io/email-content-api:latest \
  --environment-variables \
    Pinecone__ApiKey=your-pinecone-api-key \
    OpenAI__ApiKey=your-openai-api-key \
    ConnectionStrings__DefaultConnection=your-connection-string
```

## Approach 4: Azure Container Registry Secrets

### 4.1 Store Secrets in ACR

```bash
# Store secrets in ACR
az acr task create \
  --registry your-registry \
  --name build-and-push \
  --image email-content-api:{{.Run.ID}} \
  --context . \
  --file Dockerfile \
  --arg PINECONE_API_KEY=your-pinecone-api-key \
  --arg OPENAI_API_KEY=your-openai-api-key
```

## Security Best Practices

### 1. Never Commit API Keys to Source Control

Add to `.gitignore`:
```
appsettings.Development.json
appsettings.Production.json
*.user
secrets.json
```

### 2. Use Key Vault for Production

- Store all sensitive data in Azure Key Vault
- Use managed identities for authentication
- Enable soft delete and purge protection
- Set up proper access policies

### 3. Rotate Keys Regularly

```bash
# Rotate Pinecone API Key
az keyvault secret set --vault-name your-keyvault-name --name "Pinecone--ApiKey" --value "new-pinecone-api-key"

# Rotate OpenAI API Key
az keyvault secret set --vault-name your-keyvault-name --name "OpenAI--ApiKey" --value "new-openai-api-key"
```

### 4. Monitor Access

```bash
# Enable diagnostic logging
az monitor diagnostic-settings create \
  --resource your-keyvault-name \
  --resource-group your-resource-group \
  --name keyvault-diagnostics \
  --storage-account your-storage-account \
  --logs '[{"category": "AuditEvent", "enabled": true}]'
```

## Implementation Steps

### Step 1: Update Program.cs for Key Vault Integration

```csharp
// Add Key Vault configuration
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["KeyVault:VaultUrl"];
    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());
    }
}
```

### Step 2: Update Controller to Use Configuration

```csharp
// In your controller, the configuration will automatically resolve from Key Vault
var apiKey = _configuration["Pinecone:ApiKey"];
var openAiApiKey = _configuration["OpenAI:ApiKey"];
```

### Step 3: Deploy to Azure

```bash
# Deploy to App Service
az webapp deployment source config-zip \
  --resource-group your-resource-group \
  --name your-app-name \
  --src ./publish.zip

# Or deploy to Container Instances
az container create \
  --resource-group your-resource-group \
  --name your-container-name \
  --image your-registry.azurecr.io/email-content-api:latest
```

## Troubleshooting

### Common Issues

1. **Key Vault Access Denied**
   - Verify managed identity is assigned
   - Check access policies
   - Ensure correct object ID

2. **Configuration Not Found**
   - Verify secret names match configuration keys
   - Check environment-specific settings
   - Ensure proper casing (use double underscores for nested keys)

3. **Connection String Issues**
   - Verify database server allows Azure connections
   - Check firewall rules
   - Ensure connection string format is correct

### Debug Commands

```bash
# Check Key Vault secrets
az keyvault secret list --vault-name your-keyvault-name

# Verify App Service configuration
az webapp config appsettings list --name your-app-name --resource-group your-resource-group

# Check managed identity
az webapp identity show --name your-app-name --resource-group your-resource-group
```

## Cost Considerations

- **Key Vault**: ~$0.03 per 10,000 operations
- **App Service**: Standard pricing + configuration storage
- **Container Registry**: Storage and bandwidth costs
- **Managed Identity**: Free

## Next Steps

1. Choose your preferred approach (Key Vault recommended)
2. Set up the infrastructure
3. Update your configuration files
4. Test locally with development settings
5. Deploy to Azure
6. Monitor and rotate keys regularly 