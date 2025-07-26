# Enterprise Security Guide - API Key Management

## Overview

This document outlines enterprise best practices for securely managing API keys and sensitive configuration in the Email Content API.

## 🚨 **CRITICAL: Never Store API Keys in Source Code**

**Current Issue**: API keys are hardcoded in configuration files, which is a major security risk.

## Enterprise API Key Management Options

### 1. **Azure Key Vault (Recommended for Azure environments)**

#### Setup Instructions:

1. **Create Azure Key Vault**:
   ```bash
   az keyvault create --name your-keyvault-name --resource-group your-rg --location eastus
   ```

2. **Store Secrets in Key Vault**:
   ```bash
   az keyvault secret set --vault-name your-keyvault-name --name "OpenAI--ApiKey" --value "your-openai-api-key"
   az keyvault secret set --vault-name your-keyvault-name --name "Pinecone--ApiKey" --value "your-pinecone-api-key"
   az keyvault secret set --vault-name your-keyvault-name --name "ConnectionStrings--DefaultConnection" --value "your-connection-string"
   ```

3. **Configure Access Policies**:
   ```bash
   az keyvault set-policy --name your-keyvault-name --object-id <managed-identity-object-id> --secret-permissions get list
   ```

4. **Update Configuration**:
   ```json
   {
     "KeyVault": {
       "VaultUrl": "https://your-keyvault-name.vault.azure.net/"
     }
   }
   ```

#### Benefits:
- ✅ Centralized secret management
- ✅ Automatic rotation
- ✅ Access control and auditing
- ✅ Integration with Azure services
- ✅ Compliance (SOC, PCI, HIPAA)

### 2. **AWS Secrets Manager (For AWS environments)**

#### Setup Instructions:

1. **Install AWS SDK**:
   ```xml
   <PackageReference Include="AWSSDK.SecretsManager" Version="3.7.300" />
   ```

2. **Store Secrets**:
   ```bash
   aws secretsmanager create-secret --name "EmailContentAPI/OpenAI" --secret-string "your-openai-api-key"
   aws secretsmanager create-secret --name "EmailContentAPI/Pinecone" --secret-string "your-pinecone-api-key"
   ```

3. **Configure IAM Roles**:
   ```json
   {
     "Version": "2012-10-17",
     "Statement": [
       {
         "Effect": "Allow",
         "Action": "secretsmanager:GetSecretValue",
         "Resource": "arn:aws:secretsmanager:region:account:secret:EmailContentAPI/*"
       }
     ]
   }
   ```

### 3. **Environment Variables (Development/Testing)**

#### For Development:
```bash
# Windows
setx OpenAI__ApiKey "your-openai-api-key"
setx Pinecone__ApiKey "your-pinecone-api-key"

# Linux/macOS
export OpenAI__ApiKey="your-openai-api-key"
export Pinecone__ApiKey="your-pinecone-api-key"
```

#### For Docker:
```dockerfile
ENV OpenAI__ApiKey=your-openai-api-key
ENV Pinecone__ApiKey=your-pinecone-api-key
```

### 4. **User Secrets (Development Only)**

```bash
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-api-key"
dotnet user-secrets set "Pinecone:ApiKey" "your-pinecone-api-key"
```

## Implementation in Code

### Using SecureConfigurationService

```csharp
public class SomeController : ControllerBase
{
    private readonly SecureConfigurationService _secureConfig;

    public SomeController(SecureConfigurationService secureConfig)
    {
        _secureConfig = secureConfig;
    }

    public async Task<IActionResult> SomeAction()
    {
        var openAIConfig = await _secureConfig.GetOpenAIConfigAsync();
        var pineconeConfig = await _secureConfig.GetPineconeConfigAsync();
        
        // Use configurations securely
    }
}
```

## Security Best Practices

### 1. **Access Control**
- Use Managed Identities (Azure) or IAM Roles (AWS)
- Implement least privilege principle
- Regular access reviews

### 2. **Secret Rotation**
- Automate secret rotation
- Use different keys for different environments
- Monitor secret expiration

### 3. **Monitoring & Auditing**
- Enable Key Vault logging
- Monitor secret access patterns
- Set up alerts for suspicious activity

### 4. **Network Security**
- Use private endpoints for Key Vault
- Implement network security groups
- Enable firewall rules

### 5. **Compliance**
- Encrypt secrets at rest and in transit
- Implement audit trails
- Regular security assessments

## Environment-Specific Configuration

### Development
```json
{
  "KeyVault": {
    "VaultUrl": "https://dev-keyvault.vault.azure.net/"
  }
}
```

### Staging
```json
{
  "KeyVault": {
    "VaultUrl": "https://staging-keyvault.vault.azure.net/"
  }
}
```

### Production
```json
{
  "KeyVault": {
    "VaultUrl": "https://prod-keyvault.vault.azure.net/"
  }
}
```

## Migration Steps

### 1. **Immediate Actions**
- [ ] Remove API keys from source code
- [ ] Add to .gitignore: `appsettings.*.json`
- [ ] Rotate all exposed API keys
- [ ] Set up Key Vault or equivalent

### 2. **Short Term**
- [ ] Implement SecureConfigurationService
- [ ] Update all controllers to use secure config
- [ ] Set up monitoring and alerting
- [ ] Document procedures

### 3. **Long Term**
- [ ] Implement secret rotation automation
- [ ] Set up compliance monitoring
- [ ] Regular security audits
- [ ] Team training on security practices

## Troubleshooting

### Common Issues:

1. **Key Vault Access Denied**
   - Check managed identity permissions
   - Verify Key Vault access policies
   - Ensure correct tenant/subscription

2. **Fallback to Configuration**
   - Check Key Vault connectivity
   - Verify secret names match configuration keys
   - Review error logs

3. **Performance Issues**
   - Implement caching for frequently accessed secrets
   - Use connection pooling
   - Monitor Key Vault throttling

## Compliance Considerations

### SOC 2 Type II
- Secret management controls
- Access monitoring
- Regular audits

### PCI DSS
- Encryption of sensitive data
- Access control
- Audit trails

### HIPAA
- Data encryption
- Access controls
- Audit logging

## Emergency Procedures

### Secret Compromise
1. Immediately rotate compromised secrets
2. Investigate breach scope
3. Update all dependent systems
4. Document incident and lessons learned

### Key Vault Outage
1. Use fallback configuration
2. Monitor system health
3. Implement temporary workarounds
4. Restore service ASAP

## Resources

- [Azure Key Vault Documentation](https://docs.microsoft.com/en-us/azure/key-vault/)
- [AWS Secrets Manager Documentation](https://docs.aws.amazon.com/secretsmanager/)
- [OWASP Security Guidelines](https://owasp.org/www-project-api-security/)
- [Microsoft Security Best Practices](https://docs.microsoft.com/en-us/azure/security/) 