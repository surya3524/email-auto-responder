# Azure Deployment Script for Email Content API
# This script sets up Azure resources and deploys the application

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$Location = "eastus",
    
    [Parameter(Mandatory=$true)]
    [string]$AppName,
    
    [Parameter(Mandatory=$true)]
    [string]$KeyVaultName,
    
    [Parameter(Mandatory=$true)]
    [string]$PineconeApiKey,
    
    [Parameter(Mandatory=$true)]
    [string]$OpenAiApiKey,
    
    [Parameter(Mandatory=$false)]
    [string]$DatabaseConnectionString
)

Write-Host "Starting Azure deployment for Email Content API..." -ForegroundColor Green

# 1. Create Resource Group
Write-Host "Creating resource group..." -ForegroundColor Yellow
az group create --name $ResourceGroupName --location $Location

# 2. Create App Service Plan
Write-Host "Creating App Service plan..." -ForegroundColor Yellow
az appservice plan create --name "$AppName-plan" --resource-group $ResourceGroupName --sku B1 --is-linux

# 3. Create App Service
Write-Host "Creating App Service..." -ForegroundColor Yellow
az webapp create --name $AppName --resource-group $ResourceGroupName --plan "$AppName-plan" --runtime "DOTNETCORE:9.0"

# 4. Enable managed identity
Write-Host "Enabling managed identity..." -ForegroundColor Yellow
az webapp identity assign --name $AppName --resource-group $ResourceGroupName

# 5. Create Key Vault
Write-Host "Creating Key Vault..." -ForegroundColor Yellow
az keyvault create --name $KeyVaultName --resource-group $ResourceGroupName --location $Location --sku standard --enable-soft-delete true --enable-purge-protection true

# 6. Store secrets in Key Vault
Write-Host "Storing secrets in Key Vault..." -ForegroundColor Yellow
az keyvault secret set --vault-name $KeyVaultName --name "Pinecone--ApiKey" --value $PineconeApiKey
az keyvault secret set --vault-name $KeyVaultName --name "OpenAI--ApiKey" --value $OpenAiApiKey

if ($DatabaseConnectionString) {
    az keyvault secret set --vault-name $KeyVaultName --name "ConnectionStrings--DefaultConnection" --value $DatabaseConnectionString
}

# 7. Get managed identity object ID
Write-Host "Configuring Key Vault access..." -ForegroundColor Yellow
$identity = az webapp identity show --name $AppName --resource-group $ResourceGroupName --query principalId --output tsv
az keyvault set-policy --name $KeyVaultName --object-id $identity --secret-permissions get list

# 8. Configure App Service settings
Write-Host "Configuring App Service settings..." -ForegroundColor Yellow
az webapp config appsettings set --name $AppName --resource-group $ResourceGroupName --settings `
    "KeyVault__VaultUrl=https://$KeyVaultName.vault.azure.net/" `
    "Pinecone__Environment=us-east1-gcp" `
    "Pinecone__ProjectId=434a5cb5-c973-4a02-954b-ca07cf753d50" `
    "Pinecone__IndexHost=https://integrated-dense-dotnet-6hzgm10.svc.aped-4627-b74a.pinecone.io" `
    "OpenAI__Model=text-embedding-3-small" `
    "ASPNETCORE_ENVIRONMENT=Production"

# 9. Build and publish the application
Write-Host "Building and publishing application..." -ForegroundColor Yellow
dotnet publish -c Release -o ./publish

# 10. Deploy to Azure
Write-Host "Deploying to Azure..." -ForegroundColor Yellow
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force
az webapp deployment source config-zip --resource-group $ResourceGroupName --name $AppName --src ./publish.zip

# 11. Clean up
Remove-Item ./publish.zip -Force -ErrorAction SilentlyContinue
Remove-Item ./publish -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Deployment completed successfully!" -ForegroundColor Green
Write-Host "Your application is available at: https://$AppName.azurewebsites.net" -ForegroundColor Cyan
Write-Host "Key Vault URL: https://$KeyVaultName.vault.azure.net/" -ForegroundColor Cyan

# 12. Display next steps
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Test your application endpoints" -ForegroundColor White
Write-Host "2. Monitor application logs: az webapp log tail --name $AppName --resource-group $ResourceGroupName" -ForegroundColor White
Write-Host "3. Set up monitoring and alerts" -ForegroundColor White
Write-Host "4. Configure custom domain if needed" -ForegroundColor White 