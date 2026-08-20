# Secrets Management Guide

## ⚠️ CRITICAL SECURITY REQUIREMENT

**NEVER commit secrets, API keys, or credentials to source control!**

This guide explains how to properly manage sensitive configuration in SkillLedger.

## Secrets That Must Be Protected

### Azure Communication Services
- **Connection String**: Contains access key for sending emails
- **Location**: `ConnectionStrings:AzureCommunicationServices` and `Email:ConnectionString`
- **Required For**: Email verification, password reset, notifications

### Stripe Payment Processing
- **Secret Key**: `Stripe:SecretKey`
- **Webhook Secret**: `Stripe:WebhookSecret`
- **Required For**: Credit purchases, payment processing

### Azure Key Vault (Production)
- **Client Secret**: `KeyVault:ClientSecret`
- **Required For**: Production secrets management

### Credit Transfer Receipts
- **Receipt Secret Key**: `CreditTransfer:ReceiptSecretKey`
- **Required For**: Cryptographically signing transfer receipts

## Development Environment Setup

### Option 1: User Secrets (Recommended for Development)

User Secrets keep sensitive data out of your project tree and source control.

#### Initialize User Secrets

```bash
cd src/SkillLedger.Api
dotnet user-secrets init
```

#### Set Required Secrets

```bash
# Azure Communication Services
dotnet user-secrets set "ConnectionStrings:AzureCommunicationServices" "endpoint=https://...;accesskey=YOUR_KEY_HERE"
dotnet user-secrets set "Email:ConnectionString" "endpoint=https://...;accesskey=YOUR_KEY_HERE"

# Stripe (for credit purchases)
dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_KEY_HERE"
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_YOUR_KEY_HERE"
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_YOUR_SECRET_HERE"

# Credit Transfer Security
dotnet user-secrets set "CreditTransfer:ReceiptSecretKey" "YOUR_RANDOM_256BIT_KEY_HERE"
```

#### View Your Secrets

```bash
dotnet user-secrets list
```

### Option 2: Environment Variables

Set environment variables in your shell or IDE:

**Windows (PowerShell)**:
```powershell
$env:ConnectionStrings__AzureCommunicationServices="endpoint=https://...;accesskey=YOUR_KEY"
$env:Email__ConnectionString="endpoint=https://...;accesskey=YOUR_KEY"
$env:Stripe__SecretKey="sk_test_YOUR_KEY"
```

**Linux/macOS (bash)**:
```bash
export ConnectionStrings__AzureCommunicationServices="endpoint=https://...;accesskey=YOUR_KEY"
export Email__ConnectionString="endpoint=https://...;accesskey=YOUR_KEY"
export Stripe__SecretKey="sk_test_YOUR_KEY"
```

**Note**: Use double underscore `__` to represent nested JSON paths.

### Option 3: appsettings.Development.json (Local Only)

⚠️ **ONLY for local development** - This file must be in `.gitignore`!

Create `src/SkillLedger.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "AzureCommunicationServices": "endpoint=https://...;accesskey=YOUR_KEY"
  },
  "Email": {
    "ConnectionString": "endpoint=https://...;accesskey=YOUR_KEY"
  },
  "Stripe": {
    "SecretKey": "sk_test_YOUR_KEY",
    "PublishableKey": "pk_test_YOUR_KEY",
    "WebhookSecret": "whsec_YOUR_SECRET"
  }
}
```

Verify this file is in `.gitignore`:
```bash
grep "appsettings.Development.json" .gitignore
```

## Production Environment Setup

### Azure App Service

Set Application Settings in Azure Portal:

1. Go to your App Service
2. Navigate to **Configuration** → **Application settings**
3. Add the following settings:

| Name | Value | Example |
|------|-------|---------|
| `ConnectionStrings__AzureCommunicationServices` | Your Azure CS connection string | `endpoint=https://...;accesskey=...` |
| `Email__ConnectionString` | Your Azure CS connection string | `endpoint=https://...;accesskey=...` |
| `Stripe__SecretKey` | Your Stripe secret key | `sk_live_...` |
| `Stripe__WebhookSecret` | Your Stripe webhook secret | `whsec_...` |
| `CreditTransfer__ReceiptSecretKey` | Cryptographically secure random key | Generate with Azure Key Vault |

### Azure Key Vault (Recommended for Production)

For production, use Azure Key Vault to manage secrets:

1. **Enable Azure Key Vault in Configuration**:
```json
{
  "AzureKeyVaultConfiguration": {
    "Enabled": true
  },
  "AzureKeyVault": {
    "VaultUri": "https://your-keyvault.vault.azure.net/",
    "UseKeyVault": true
  }
}
```

2. **Configure Managed Identity**:
   - Enable System-assigned managed identity for your App Service
   - Grant the identity `Secret Get` and `Secret List` permissions on the Key Vault

3. **Store Secrets in Key Vault**:
```bash
az keyvault secret set --vault-name "your-keyvault" \
  --name "ConnectionStrings--AzureCommunicationServices" \
  --value "endpoint=https://...;accesskey=..."
```

## Generating Secure Keys

### Receipt Secret Key (256-bit)

**PowerShell**:
```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)
```

**Linux/macOS**:
```bash
openssl rand -base64 32
```

## Verification

### Check Configuration is Working

```bash
cd src/SkillLedger.Api
dotnet run
```

If secrets are missing, you'll see errors like:
```
Configuration error: Missing required setting 'ConnectionStrings:AzureCommunicationServices'
```

### Test Email Sending

Once configured, test email sending works:
1. Register a new user account
2. Check you receive the verification email
3. If emails don't arrive, check:
   - Azure Communication Services connection string is valid
   - Email domain is verified in Azure
   - Check application logs for errors

## Security Best Practices

### ✅ DO:
- Use User Secrets for local development
- Use Azure Key Vault for production
- Rotate secrets regularly (at least annually)
- Use different keys for dev/staging/production
- Audit secret access regularly
- Use managed identities instead of connection strings when possible

### ❌ DON'T:
- Commit secrets to Git (check with `git log -p | grep "accesskey"`)
- Share secrets via email or chat
- Use production secrets in development
- Store secrets in source code comments
- Screenshot or document actual secret values
- Use the same secret across multiple environments

## Incident Response

### If Secrets Are Committed to Git

1. **Immediately rotate the compromised secret** in Azure Portal
2. **Update all deployments** with the new secret
3. **Remove from Git history**:
   ```bash
   # Use BFG Repo-Cleaner or git-filter-repo
   git filter-repo --path src/SkillLedger.Api/appsettings.json --invert-paths
   ```
4. **Force push** (coordinate with team first):
   ```bash
   git push --force --all
   ```
5. **Notify security team** and document the incident

### If Secrets Are Leaked Publicly

1. **Rotate ALL secrets immediately**
2. **Review access logs** for unauthorized usage
3. **Enable additional monitoring** for suspicious activity
4. **Update incident response documentation**
5. **Conduct post-incident review**

## Getting Secrets for Development

### Azure Communication Services

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your Azure Communication Services resource
3. Go to **Keys**
4. Copy the **Primary connection string**

### Stripe Keys

1. Go to [Stripe Dashboard](https://dashboard.stripe.com)
2. Navigate to **Developers** → **API keys**
3. Copy the **Secret key** (starts with `sk_test_` for test mode)
4. For webhooks: **Developers** → **Webhooks** → **Add endpoint** → Copy signing secret

## Troubleshooting

### "Configuration validation failed" Error

- Check all required secrets are set
- Verify secret names match exactly (case-sensitive)
- For environment variables, use double underscore `__` for nesting

### Email Sending Fails

- Verify Azure Communication Services connection string is valid
- Check the sender email domain is verified
- Review application logs for detailed error messages

### Stripe Payments Fail

- Ensure using correct Stripe keys (test vs live)
- Verify webhook signing secret matches Stripe dashboard
- Test with Stripe CLI: `stripe listen --forward-to localhost:8030/api/webhooks/stripe`

## Questions?

Contact the security team or refer to:
- [Azure Key Vault Documentation](https://docs.microsoft.com/en-us/azure/key-vault/)
- [.NET User Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure Communication Services Security](https://docs.microsoft.com/en-us/azure/communication-services/concepts/security)
