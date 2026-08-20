# SkillLedger Deployment Guide

## Overview

This guide provides comprehensive instructions for deploying SkillLedger to production environments on Microsoft Azure.

## Prerequisites

### Azure Requirements
- Azure Subscription with appropriate permissions
- Azure CLI installed and configured
- Docker and Docker Compose
- .NET 9.0 SDK
- Node.js 20.x
- GitHub account with repository access

### Required Azure Services
- Azure App Service (P2v3 or higher)
- Azure SQL Database (S3 or higher)
- Azure Key Vault
- Azure Container Registry
- Application Insights
- Azure Static Web Apps (for frontend)

## Architecture Overview

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │   Backend API   │    │   Database      │
│ (Next.js 14)    │◄──►│  (.NET 9 API)   │◄──►│ (Azure SQL)     │
│ Static Web Apps │    │   App Service   │    │   Database      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌─────────────────┐
                       │   Key Vault     │
                       │ (Secrets Mgmt)  │
                       └─────────────────┘
```

## Deployment Steps

### 1. Infrastructure Setup

#### 1.1 Create Resource Group
```bash
az group create \
  --name skillledger-rg \
  --location eastus
```

#### 1.2 Deploy Azure Resources
```bash
# Deploy infrastructure using ARM template
az deployment group create \
  --resource-group skillledger-rg \
  --template-file azure-deployment/azure-app-service.json \
  --parameters azure-deployment/azure-app-service.parameters.json
```

#### 1.3 Configure Key Vault
```bash
# Add secrets to Key Vault
az keyvault secret set \
  --vault-name skillledger-kv \
  --name "Database-ConnectionString" \
  --value "Server=skillledger-sql.database.windows.net;Database=skillledger-db;..."

az keyvault secret set \
  --vault-name skillledger-kv \
  --name "Jwt-PrivateKey" \
  --value "-----BEGIN RSA PRIVATE KEY-----..."
```

### 2. Backend Deployment

#### 2.1 Build and Push Docker Image
```bash
# Build Docker image
docker build -t skillledger/api:latest .

# Tag for Azure Container Registry
docker tag skillledger/api:latest skillledgeracr.azurecr.io/skillledger-api:latest

# Push to ACR
docker push skillledgeracr.azurecr.io/skillledger-api:latest
```

#### 2.2 Configure App Service
```bash
# Set environment variables
az webapp config appsettings set \
  --resource-group skillledger-rg \
  --name skillledger-api \
  --settings \
  AZURE_KEY_VAULT_ENDPOINT="https://skillledger-kv.vault.azure.net/" \
  ASPNETCORE_ENVIRONMENT="Production"

# Configure container settings
az webapp config container set \
  --resource-group skillledger-rg \
  --name skillledger-api \
  --docker-custom-image-name skillledgeracr.azurecr.io/skillledger-api:latest \
  --docker-registry-server-url https://skillledgeracr.azurecr.io
```

#### 2.3 Enable Managed Identity
```bash
# Enable system-assigned managed identity
az webapp identity assign \
  --resource-group skillledger-rg \
  --name skillledger-api

# Grant Key Vault access
principalId=$(az webapp identity show \
  --resource-group skillledger-rg \
  --name skillledger-api \
  --query principalId \
  --output tsv)

az keyvault set-policy \
  --name skillledger-kv \
  --object-id $principalId \
  --secret-permissions get list
```

### 3. Frontend Deployment

#### 3.1 Build Frontend
```bash
cd web
npm install
npm run build
```

#### 3.2 Deploy to Azure Static Web Apps
```bash
# Using GitHub Actions (configured in .github/workflows/azure-deployment.yml)
# Or manual deployment:
az staticwebapp create \
  --name skillledger-web \
  --resource-group skillledger-rg \
  --source <private-source-repository> \
  --location eastus \
  --branch main \
  --app-location web \
  --output-location out
```

### 4. Database Setup

#### 4.1 Run Database Migrations
```bash
# Using Azure Cloud Shell or local with connection string
dotnet ef database update \
  --connection "Server=skillledger-sql.database.windows.net;Database=skillledger-db;..."

# Initialize system data
dotnet run --project src/SkillLedger.Api -- --initialize-data
```

#### 4.2 Configure Database Security
```sql
-- Create database user for application
CREATE USER skillledger_app WITH PASSWORD = 'ComplexPassword123!';

-- Grant necessary permissions
ALTER ROLE db_datareader ADD MEMBER skillledger_app;
ALTER ROLE db_datawriter ADD MEMBER skillledger_app;

-- Configure row-level security if needed
EXEC sp_addrolemember 'db_owner', 'skillledger_admin';
```

### 5. Monitoring and Logging

#### 5.1 Configure Application Insights
```bash
# Create Application Insights resource
az monitor app-insights component create \
  --app skillledger-ai \
  --location eastus \
  --resource-group skillledger-rg \
  --application-type web

# Get instrumentation key
instrumentationKey=$(az monitor app-insights component show \
  --app skillledger-ai \
  --resource-group skillledger-rg \
  --query instrumentationKey \
  --output tsv)

# Add to App Service settings
az webapp config appsettings set \
  --resource-group skillledger-rg \
  --name skillledger-api \
  --settings \
  APPINSIGHTS_INSTRUMENTATIONKEY=$instrumentationKey
```

#### 5.2 Set Up Alerts
```bash
# Create alert for high response time
az monitor metrics alert create \
  --name "High Response Time Alert" \
  --resource-group skillledger-rg \
  --scopes "/subscriptions/{subscription-id}/resourceGroups/skillledger-rg/providers/Microsoft.Web/sites/skillledger-api" \
  --condition "avg HttpResponseTime > 200" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --severity 2 \
  --action-group skillledger-alerts
```

### 6. Security Configuration

#### 6.1 Configure SSL/TLS
```bash
# Enforce HTTPS
az webapp update \
  --resource-group skillledger-rg \
  --name skillledger-api \
  --https-only true

# Configure TLS version
az webapp config ssl set \
  --resource-group skillledger-rg \
  --name skillledger-api \
  --tls-version 1.2
```

#### 6.2 Set Up CORS
```bash
# Configure allowed origins
az webapp cors add \
  --resource-group skillledger-rg \
  --name skillledger-api \
  --allowed-origins "https://skillledger-web.azurewebsites.net"
```

#### 6.3 Configure Security Headers
Security headers are configured in the application middleware:
- X-Frame-Options: DENY
- X-Content-Type-Options: nosniff
- Referrer-Policy: strict-origin-when-cross-origin
- X-XSS-Protection: 1; mode=block
- Content-Security-Policy: default-src 'self'

## Environment-Specific Configuration

### Development
```json
{
  "ASPNETCORE_ENVIRONMENT": "Development",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SkillLedger_Dev;Trusted_Connection=true;"
  }
}
```

### Staging
```json
{
  "ASPNETCORE_ENVIRONMENT": "Staging",
  "AZURE_KEY_VAULT_ENDPOINT": "https://skillledger-staging-kv.vault.azure.net/",
  "ConnectionStrings": {
    "DefaultConnection": "@Microsoft.KeyVault(SecretUri=https://skillledger-staging-kv.vault.azure.net/secrets/Database-ConnectionString/)"
  }
}
```

### Production
```json
{
  "ASPNETCORE_ENVIRONMENT": "Production",
  "AZURE_KEY_VAULT_ENDPOINT": "https://skillledger-kv.vault.azure.net/",
  "ConnectionStrings": {
    "DefaultConnection": "@Microsoft.KeyVault(SecretUri=https://skillledger-kv.vault.azure.net/secrets/Database-ConnectionString/)"
  }
}
```

## CI/CD Pipeline

### GitHub Actions Configuration
The deployment pipeline is configured in `.github/workflows/azure-deployment.yml`:

1. **Test Phase**: Runs unit tests, integration tests, and E2E tests
2. **Security Scan**: Performs OWASP security scanning
3. **Build Phase**: Builds .NET API and Next.js frontend
4. **Deploy Phase**: Deploys to Azure App Service and Static Web Apps
5. **Post-Deployment**: Runs health checks and smoke tests

### Manual Deployment Commands
```bash
# Deploy backend
az webapp up \
  --resource-group skillledger-rg \
  --name skillledger-api \
  --location eastus \
  --sku P2v3

# Deploy frontend
az staticwebapp up \
  --name skillledger-web \
  --resource-group skillledger-rg \
  --source .
```

## Monitoring and Maintenance

### Health Checks
- **Basic Health**: `/health` - Overall system status
- **Detailed Health**: `/health?detailed=true` - Comprehensive health information
- **Metrics**: `/api/monitoring/metrics` - Performance metrics (admin only)

### Log Analysis
```bash
# View application logs
az webapp log tail \
  --resource-group skillledger-rg \
  --name skillledger-api

# Download logs
az webapp log download \
  --resource-group skillledger-rg \
  --name skillledger-api
```

### Performance Monitoring
- Application Insights provides comprehensive monitoring
- Key metrics to monitor:
  - Response time (< 200ms target)
  - Error rate (< 1% target)
  - Memory usage (< 80% threshold)
  - CPU usage (< 70% threshold)

## Troubleshooting

### Common Issues

#### 1. Database Connection Issues
```bash
# Check connection string in Key Vault
az keyvault secret show \
  --vault-name skillledger-kv \
  --name Database-ConnectionString

# Test database connectivity
sqlcmd -S skillledger-sql.database.windows.net -d skillledger-db -U skillledger_app -P "Password"
```

#### 2. Application Startup Issues
```bash
# Check application logs
az webapp log tail \
  --resource-group skillledger-rg \
  --name skillledger-api

# Check configuration
az webapp config appsettings list \
  --resource-group skillledger-rg \
  --name skillledger-api
```

#### 3. Performance Issues
```bash
# Scale up App Service
az webapp update \
  --resource-group skillledger-rg \
  --name skillledger-api \
  --sku P3v3

# Check Application Insights
az monitor app-insights query \
  --app skillledger-ai \
  --analytics-query "requests | take 10"
```

### Emergency Procedures

#### 1. Rollback Deployment
```bash
# Swap deployment slots
az webapp deployment slot swap \
  --resource-group skillledger-rg \
  --name skillledger-api \
  --slot staging \
  --target-slot production
```

#### 2. Scale Down for Cost Savings
```bash
az webapp update \
  --resource-group skillledger-rg \
  --name skillledger-api \
  --sku B1
```

#### 3. Emergency Access
```bash
# Enable diagnostics
az webapp config appsettings set \
  --resource-group skillledger-rg \
  --name skillledger-api \
  --settings \
  DIAGNOSTICS_AZUREBLOBCONTAINERSURL="https://skillledgerdiag.blob.core.windows.net/"
```

## Security Best Practices

1. **Use Managed Identities**: Always use Azure managed identities instead of secrets in configuration
2. **Enable SSL/TLS**: Enforce HTTPS for all communications
3. **Regular Updates**: Keep all dependencies and frameworks updated
4. **Access Control**: Implement principle of least privilege for all resources
5. **Monitoring**: Set up comprehensive monitoring and alerting
6. **Backup Strategy**: Implement regular database and application backups
7. **Security Scanning**: Regular security scans and penetration testing

## Support and Maintenance

### Regular Maintenance Tasks
- Monthly: Review and update dependencies
- Quarterly: Performance optimization and capacity planning
- Semi-annually: Security audit and penetration testing
- Annually: Architecture review and disaster recovery testing

### Contact Information
- Development Team: dev-team@skillledger.app
- Infrastructure Team: infra-team@skillledger.app
- Security Team: security@skillledger.app
- 24/7 Support: support@skillledger.app

## Appendices

### A. Environment Variables
| Variable | Description | Required |
|----------|-------------|----------|
| AZURE_KEY_VAULT_ENDPOINT | Key Vault endpoint | Yes |
| ASPNETCORE_ENVIRONMENT | Environment name | Yes |
| DATABASE_CONNECTION_STRING | Database connection | Yes |
| JWT_PRIVATE_KEY | JWT signing key | Yes |

### B. Port Configuration
| Service | Port | Protocol |
|---------|------|----------|
| Backend API | 8030 | HTTP |
| Backend API | 8031 | HTTPS |
| Frontend | 3030 | HTTP |
| SQL Server | 9030 | TCP |

### C. Resource Sizing
| Environment | App Service Plan | Database | Cost (USD/month) |
|-------------|------------------|----------|-----------------|
| Development | B1 | Basic | ~$50 |
| Staging | P1v3 | Standard | ~$200 |
| Production | P2v3 | S3 | ~$500 |
