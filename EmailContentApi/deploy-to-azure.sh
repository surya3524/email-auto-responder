#!/bin/bash

# Azure Deployment Script for Email Content API
# This script sets up Azure resources and deploys the application

# Check if required parameters are provided
if [ $# -lt 5 ]; then
    echo "Usage: $0 <ResourceGroupName> <Location> <AppName> <KeyVaultName> <PineconeApiKey> <OpenAiApiKey> [DatabaseConnectionString]"
    echo "Example: $0 my-resource-group eastus my-app my-keyvault 'pinecone-key' 'openai-key' 'connection-string'"
    exit 1
fi

RESOURCE_GROUP_NAME=$1
LOCATION=${2:-eastus}
APP_NAME=$3
KEY_VAULT_NAME=$4
PINECONE_API_KEY=$5
OPENAI_API_KEY=$6
DATABASE_CONNECTION_STRING=$7

echo "Starting Azure deployment for Email Content API..."

# 1. Create Resource Group
echo "Creating resource group..."
az group create --name $RESOURCE_GROUP_NAME --location $LOCATION

# 2. Create App Service Plan
echo "Creating App Service plan..."
az appservice plan create --name "$APP_NAME-plan" --resource-group $RESOURCE_GROUP_NAME --sku B1 --is-linux

# 3. Create App Service
echo "Creating App Service..."
az webapp create --name $APP_NAME --resource-group $RESOURCE_GROUP_NAME --plan "$APP_NAME-plan" --runtime "DOTNETCORE:9.0"

# 4. Enable managed identity
echo "Enabling managed identity..."
az webapp identity assign --name $APP_NAME --resource-group $RESOURCE_GROUP_NAME

# 5. Create Key Vault
echo "Creating Key Vault..."
az keyvault create --name $KEY_VAULT_NAME --resource-group $RESOURCE_GROUP_NAME --location $LOCATION --sku standard --enable-soft-delete true --enable-purge-protection true

# 6. Store secrets in Key Vault
echo "Storing secrets in Key Vault..."
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "Pinecone--ApiKey" --value "$PINECONE_API_KEY"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name "OpenAI--ApiKey" --value "$OPENAI_API_KEY"

if [ ! -z "$DATABASE_CONNECTION_STRING" ]; then
    az keyvault secret set --vault-name $KEY_VAULT_NAME --name "ConnectionStrings--DefaultConnection" --value "$DATABASE_CONNECTION_STRING"
fi

# 7. Get managed identity object ID and configure Key Vault access
echo "Configuring Key Vault access..."
IDENTITY=$(az webapp identity show --name $APP_NAME --resource-group $RESOURCE_GROUP_NAME --query principalId --output tsv)
az keyvault set-policy --name $KEY_VAULT_NAME --object-id $IDENTITY --secret-permissions get list

# 8. Configure App Service settings
echo "Configuring App Service settings..."
az webapp config appsettings set --name $APP_NAME --resource-group $RESOURCE_GROUP_NAME --settings \
    "KeyVault__VaultUrl=https://$KEY_VAULT_NAME.vault.azure.net/" \
    "Pinecone__Environment=us-east1-gcp" \
    "Pinecone__ProjectId=434a5cb5-c973-4a02-954b-ca07cf753d50" \
    "Pinecone__IndexHost=https://integrated-dense-dotnet-6hzgm10.svc.aped-4627-b74a.pinecone.io" \
    "OpenAI__Model=text-embedding-3-small" \
    "ASPNETCORE_ENVIRONMENT=Production"

# 9. Build and publish the application
echo "Building and publishing application..."
dotnet publish -c Release -o ./publish

# 10. Deploy to Azure
echo "Deploying to Azure..."
cd publish
zip -r ../publish.zip .
cd ..
az webapp deployment source config-zip --resource-group $RESOURCE_GROUP_NAME --name $APP_NAME --src ./publish.zip

# 11. Clean up
rm -rf ./publish.zip ./publish

echo "Deployment completed successfully!"
echo "Your application is available at: https://$APP_NAME.azurewebsites.net"
echo "Key Vault URL: https://$KEY_VAULT_NAME.vault.azure.net/"

# 12. Display next steps
echo ""
echo "Next steps:"
echo "1. Test your application endpoints"
echo "2. Monitor application logs: az webapp log tail --name $APP_NAME --resource-group $RESOURCE_GROUP_NAME"
echo "3. Set up monitoring and alerts"
echo "4. Configure custom domain if needed" 