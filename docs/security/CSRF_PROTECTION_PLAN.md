# CSRF Protection Implementation Plan

## Current Status: CRITICAL VULNERABILITY

SkillLedger currently has **CRITICAL CSRF vulnerabilities** due to cookie-based authentication without proper CSRF token validation on state-changing endpoints.

## Problem Summary

**Risk Level**: CRITICAL
**Impact**: Attackers can perform unauthorized actions on behalf of authenticated users
**Affected**: All cookie-authenticated POST/PUT/DELETE/PATCH endpoints

### Vulnerable Endpoints

The following endpoints use `[IgnoreAntiforgeryToken]` which bypasses CSRF protection:

#### Authentication Controller (`/api/auth/*`)
- ❌ `POST /api/auth/register` - Account creation
- ❌ `POST /api/auth/login` - Session creation
- ❌ `POST /api/auth/logout` - Session destruction
- ❌ `POST /api/auth/logout-all` - Global session destruction
- ❌ `POST /api/auth/forgot-password` - Password reset initiation
- ❌ `POST /api/auth/reset-password` - Password change

**Example Attack Scenario**:
```html
<!-- Attacker's malicious website -->
<form action="https://skilledger.com/api/auth/logout" method="POST">
  <input type="submit" value="Click for free gift!">
</form>
```
When a logged-in user clicks this, they're automatically logged out.

More severe attacks could:
- Transfer credits to attacker accounts
- Modify user profiles
- Accept/reject project applications
- Release escrow funds

## Root Cause

The `IgnoreAntiforgeryTokenAttribute` was designed for JWT authentication but SkillLedger uses **cookie-based authentication**. This creates a fundamental mismatch:

- **JWT Auth** (stateless): CSRF protection not needed (tokens stored in localStorage)
- **Cookie Auth** (stateful): CSRF protection REQUIRED (browsers automatically send cookies)

## Implementation Plan

### Phase 1: Backend - Enable CSRF Validation (Completed ✅)

1. ✅ **Update `IgnoreAntiforgeryTokenAttribute`**: Add security warnings and logging
2. ✅ **Document vulnerable endpoints**: Create this file
3. ⏳ **Remove attribute from critical endpoints**: Start with high-risk operations

### Phase 2: Frontend - Add CSRF Token Support (TODO)

1. **Create CSRF token utility**:
```typescript
// web/src/utils/csrf.ts
export async function getCsrfToken(): Promise<string | null> {
  try {
    const response = await fetch('/api/auth/csrf-token', {
      credentials: 'include'
    });
    if (!response.ok) return null;
    const data = await response.json();
    return data.csrfToken;
  } catch (error) {
    console.error('Failed to fetch CSRF token:', error);
    return null;
  }
}
```

2. **Update API client to include CSRF tokens**:
```typescript
// web/src/utils/apiClient.ts
import { getCsrfToken } from './csrf';

export async function apiPost(url: string, data: any) {
  const csrfToken = await getCsrfToken();

  const response = await fetch(url, {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      'X-CSRF-TOKEN': csrfToken || ''
    },
    body: JSON.stringify(data)
  });

  return response;
}
```

3. **Update all state-changing API calls**:
   - Login form
   - Registration form
   - Profile updates
   - Project creation/updates
   - Credit transfers
   - Escrow operations
   - All file uploads

### Phase 3: Backend - Add CSRF Token Endpoint (TODO)

```csharp
// src/SkillLedger.Api/Controllers/AuthController.cs

/// <summary>
/// Get CSRF token for subsequent requests
/// </summary>
[HttpGet("csrf-token")]
[IgnoreAntiforgeryToken] // This endpoint is safe to access without CSRF
public IActionResult GetCsrfToken()
{
    var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
    return Ok(new { csrfToken = tokens.RequestToken });
}
```

### Phase 4: Gradual Migration (TODO)

**Week 1-2**: High-risk financial endpoints
- Credit transfers
- Escrow operations
- Payment processing
- Subscription management

**Week 3-4**: Medium-risk endpoints
- Profile modifications
- Project creation/updates
- File uploads
- Workspace changes

**Week 5-6**: Low-risk endpoints
- Settings updates
- Preference changes
- Non-critical operations

**Week 7**: Authentication endpoints (requires careful coordination)
- Login
- Logout
- Registration
- Password reset

### Phase 5: Testing & Verification

1. **Unit Tests**: CSRF token validation logic
2. **Integration Tests**: End-to-end flows with CSRF tokens
3. **Security Tests**: Verify CSRF attacks are blocked
4. **User Acceptance Testing**: Ensure no UX regressions

## Temporary Mitigation (Current State)

Until full CSRF protection is implemented, the following mitigations are in place:

1. ✅ **Logging**: All CSRF bypasses are logged with warnings
2. ✅ **Documentation**: Vulnerable endpoints documented
3. ✅ **SameSite Cookies**: `SameSite=Lax` prevents some CSRF attacks (but not all)
4. ✅ **Rate Limiting**: Slows down automated attacks
5. ✅ **Audit Logging**: All state changes are logged with IP addresses

**These mitigations reduce but DO NOT ELIMINATE the risk.**

## Configuration

CSRF protection is configured in `src/SkillLedger.Api/Program.cs`:

```csharp
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
    options.Cookie.Name = ".SkillLedger.Antiforgery";
    options.Cookie.SameSite = SameSiteMode.Lax; // Development
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Development
    options.Cookie.HttpOnly = true;
});
```

## References

- [OWASP CSRF Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cross-Site_Request_Forgery_Prevention_Cheat_Sheet.html)
- [ASP.NET Core Antiforgery Documentation](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery)
- [Next.js CSRF Protection](https://nextjs.org/docs/advanced-features/security-headers)

## Related Files

- `src/SkillLedger.Api/Attributes/IgnoreAntiforgeryTokenAttribute.cs` - Deprecated attribute
- `src/SkillLedger.Api/Filters/ConditionalAntiforgeryFilter.cs` - Testing environment bypass
- `src/SkillLedger.Api/Program.cs` - CSRF configuration
- `src/SkillLedger.Api/Controllers/AuthController.cs` - Authentication endpoints (vulnerable)
- All other controllers with `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]` attributes

## Decision Log

**2025-11-13**:
- Identified critical CSRF vulnerabilities across cookie-authenticated endpoints
- Updated `IgnoreAntiforgeryTokenAttribute` with security warnings and logging
- Created comprehensive implementation plan
- Decision: Gradual migration approach to avoid breaking existing frontend
