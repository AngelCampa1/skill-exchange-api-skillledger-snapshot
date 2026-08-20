# Resend Migration Guide

## Overview

This guide documents the migration from Azure Communication Services (ACS) to Resend for email delivery in SkillLedger.

**Migration Date**: January 19, 2026
**Branch**: `feature/replace-acs-with-resend` (merged to `main`)
**Commit**: `d6845ad`

---

## What Changed

### 1. Email Service Provider
- **Before**: Azure Communication Services (ACS)
- **After**: Resend
- **Reason**: Better developer experience, modern infrastructure, React Email support

### 2. Email Sender Address
- **Before**: `DoNotReply@acs-ventora-shared.azurecomm.net`
- **After**: `noreply@skillledger.app`

### 3. NuGet Packages
- **Removed**: `Azure.Communication.Email` v1.0.1
- **Added**: `Resend` v0.2.1

---

## Configuration Changes

### Development Environment

**File**: `src/SkillLedger.Api/appsettings.Development.json`

```json
{
  "Resend": {
    "ApiKey": "",
    "_ApiKey_Note": "Set via User Secrets or environment variable"
  },
  "EmailSettings": {
    "FromEmail": "noreply@skillledger.app",
    "FromDisplayName": "SkillLedger (Development)"
  }
}
```

**Remove**: `ConnectionStrings.AzureCommunicationServices`

### Production Environment

**File**: `src/SkillLedger.Api/appsettings.json`

```json
{
  "Resend": {
    "ApiKey": "",
    "_ApiKey_Note": "Set via Azure Key Vault or environment variable"
  },
  "EmailSettings": {
    "FromEmail": "noreply@skillledger.app",
    "FromDisplayName": "SkillLedger"
  }
}
```

---

## Setup Instructions

### Step 1: Get Resend API Key

1. Sign up at https://resend.com/ (if not already registered)
2. Navigate to **API Keys** section
3. Create a new API key with name: `SkillLedger Production` or `SkillLedger Development`
4. Copy the API key (starts with `re_`)

### Step 2: Configure API Key Locally

**Option A: User Secrets (Recommended for Development)**

```bash
cd src/SkillLedger.Api
dotnet user-secrets set "Resend:ApiKey" "re_your_api_key_here"
```

**Option B: Environment Variable**

```bash
# Windows (PowerShell)
$env:Resend__ApiKey="re_your_api_key_here"

# Windows (Command Prompt)
set Resend__ApiKey=re_your_api_key_here

# Linux/macOS
export Resend__ApiKey="re_your_api_key_here"
```

**Option C: launchSettings.json (Not Recommended - Don't commit)**

Edit `src/SkillLedger.Api/Properties/launchSettings.json`:

```json
{
  "profiles": {
    "https": {
      "environmentVariables": {
        "Resend__ApiKey": "re_your_api_key_here"
      }
    }
  }
}
```

### Step 3: Configure DNS Records

**CRITICAL**: Without these DNS records, emails will fail or be marked as spam.

1. **Add Domain to Resend**:
   - Go to https://resend.com/domains
   - Click "Add Domain"
   - Enter: `skillledger.app`
   - Copy the DNS records shown

2. **Add DNS Records** (at your DNS provider - e.g., Cloudflare, GoDaddy, Namecheap):

   | Type | Name/Host | Value | TTL |
   |------|-----------|-------|-----|
   | TXT | `@` or `skillledger.app` | `v=spf1 include:_spf.resend.com ~all` | 3600 |
   | TXT | `resend._domainkey` | *[DKIM key from Resend dashboard]* | 3600 |
   | TXT | `_dmarc` | `v=DMARC1; p=quarantine; rua=mailto:dmarc@skillledger.app` | 3600 |

3. **Verify Domain** in Resend Dashboard:
   - Wait 10-30 minutes for DNS propagation
   - Click "Verify" button in Resend dashboard
   - Status should change to "Verified"

### Step 4: Configure Email Forwarding

To receive emails sent to `support@skillledger.app`, `contact@skillledger.app`, etc., use a forwarding service:

**Option A: ImprovMX (Recommended - Free)**

1. Sign up at https://improvmx.com/
2. Add domain: `skillledger.app`
3. Add forwarding rules:
   - `support@skillledger.app` → Your admin inbox
   - `contact@skillledger.app` → Your admin inbox
   - `feedback@skillledger.app` → Your admin inbox
   - `*@skillledger.app` → Your admin inbox (catch-all)
4. Add their MX records to your DNS

**Option B: ForwardEmail.net (Open Source)**

1. Sign up at https://forwardemail.net/
2. Configure similarly to ImprovMX

**Option C: Resend Inbound Email (Webhooks)**

If you want programmatic handling:
1. Enable receiving in Resend dashboard
2. Add MX record from Resend
3. Create webhook endpoint in API to forward emails

---

## Testing the Migration

### Test 1: Verify Configuration

```bash
cd src/SkillLedger.Api
dotnet run
```

Check console output for:
- ✅ `Using ResendEmailService for email delivery` (if API key configured)
- ⚠️ `Using MockEmailService - Resend API key not configured` (if no API key)

### Test 2: Run Unit Tests

```bash
dotnet test --filter "FullyQualifiedName~ResendEmailServiceTests"
```

Expected: All 12 tests pass

### Test 3: Test Welcome Email (Manual)

1. Start the API: `dotnet run --project src/SkillLedger.Api`
2. Register a new user via the frontend or API
3. Check email inbox for welcome email from `noreply@skillledger.app`

### Test 4: Test Password Reset Email (Manual)

1. Use "Forgot Password" feature
2. Check email for reset link
3. Verify link works

### Test 5: Test Feedback Email (Manual)

1. Submit feedback via `/api/feedback` endpoint
2. Check the configured admin inbox for feedback email (emails go to `support@skillledger.app`)

---

## Rollback Plan

If issues occur, revert to Azure Communication Services:

```bash
# Option 1: Git revert
git revert d6845ad
git push

# Option 2: Manual rollback
git checkout 9f817c8  # Last commit before migration
git checkout -b rollback/revert-resend-migration

# Then restore:
# 1. Add Azure.Communication.Email package
# 2. Restore EmailService.cs from git history
# 3. Update Program.cs service registration
# 4. Update appsettings.json with ACS connection string
```

---

## Production Deployment Checklist

Before deploying to production:

- [ ] Resend API key added to Azure Key Vault
- [ ] DNS records configured and verified in Resend dashboard
- [ ] Domain status shows "Verified" in Resend
- [ ] Email forwarding configured and tested
- [ ] Test email sent successfully from production environment
- [ ] Welcome email tested on production
- [ ] Password reset email tested on production
- [ ] Monitoring/alerts configured for email failures
- [ ] Document Resend API rate limits and quotas

---

## Monitoring & Troubleshooting

### Check Email Logs

Resend provides email logs in their dashboard:
- Go to https://resend.com/logs
- Filter by date, status, recipient

### Common Issues

**Issue**: `Using MockEmailService - Resend API key not configured`
- **Solution**: Set `Resend:ApiKey` via User Secrets or environment variable

**Issue**: Emails not sending
- **Solution**: Check Resend dashboard for error logs
- **Solution**: Verify domain is verified in Resend
- **Solution**: Check API key is valid

**Issue**: Emails going to spam
- **Solution**: Verify SPF, DKIM, DMARC records are correct
- **Solution**: Use Resend's email testing tools

**Issue**: "Domain not verified" error
- **Solution**: Wait for DNS propagation (up to 48 hours)
- **Solution**: Verify DNS records match Resend requirements

---

## Cost Analysis

### Resend Pricing (as of January 2026)

| Plan | Price | Emails/Month | Cost per Email |
|------|-------|--------------|----------------|
| Free | $0 | 3,000 | $0 |
| Pro | $20 | 50,000 | $0.0004 |
| Scale | $90 | 100,000 | $0.0009 |

### Azure Communication Services Pricing

- **Cost**: ~$0.00025 per email + $0.00012 per MB
- **Example**: 100,000 emails = ~$25

### Recommendation

- **Development**: Use Free tier (3,000 emails/month)
- **Production**: Start with Pro tier ($20/month for 50k emails)
- **Scale**: Upgrade to Scale tier if needed

---

## Support & Resources

- **Resend Documentation**: https://resend.com/docs
- **Resend .NET SDK**: https://github.com/resend/resend-dotnet
- **Resend Support**: support@resend.com
- **DNS Help**: https://resend.com/docs/dashboard/domains/dns-records

---

## Files Modified

### Backend Files

| File | Change |
|------|--------|
| `src/SkillLedger.Infrastructure/SkillLedger.Infrastructure.csproj` | Replaced package reference |
| `src/SkillLedger.Infrastructure/Services/ResendEmailService.cs` | Created new service |
| `src/SkillLedger.Infrastructure/Services/EmailService.cs` | Deleted (ACS) |
| `src/SkillLedger.Api/Program.cs` | Updated service registration |
| `src/SkillLedger.Api/appsettings.json` | Updated configuration |
| `src/SkillLedger.Api/appsettings.Development.json` | Updated configuration |
| `src/SkillLedger.Core/Interfaces/IEmailService.cs` | Updated documentation |

### Test Files

| File | Change |
|------|--------|
| `tests/SkillLedger.Tests/Core/Services/ResendEmailServiceTests.cs` | Created new test suite |

---

## Contact

For issues or questions about this migration:
- **Developer**: Claude Code
- **Owner**: Angel Campa (support@skillledger.app)
- **Project**: SkillLedger
- **Repository**: <private-source-repository>
