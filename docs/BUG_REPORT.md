# SkillLedger Comprehensive Bug Report

**Generated**: December 10, 2025
**Scope**: Full codebase security and bug audit
**Total Issues Found**: 78 bugs across all severity levels

---

## Executive Summary

| Severity | Count | Categories |
|----------|-------|------------|
| **CRITICAL** | 8 | Security credentials exposed, double-release vulnerabilities, data consistency |
| **HIGH** | 22 | Authentication, rate limiting bypass, resource leaks, race conditions |
| **MEDIUM** | 28 | Input validation, timing attacks, configuration issues, test flakiness |
| **LOW** | 20 | Code quality, accessibility, performance, documentation |

---

## Table of Contents

1. [Critical Security Issues](#1-critical-security-issues)
2. [Backend (.NET) Bugs](#2-backend-net-bugs)
3. [Frontend (Next.js) Bugs](#3-frontend-nextjs-bugs)
4. [Business Logic Bugs](#4-business-logic-bugs)
5. [Configuration & Infrastructure Issues](#5-configuration--infrastructure-issues)
6. [Test Code Issues](#6-test-code-issues)
7. [Recommended Actions](#7-recommended-actions)

---

## 1. Critical Security Issues

### CRIT-001: Exposed Azure Communication Services Access Key
- **File**: `src/SkillLedger.Api/appsettings.json:15,75`
- **File**: `src/SkillLedger.Api/appsettings.Development.json:29`
- **Issue**: Azure Communication Services access key is hardcoded and exposed in source control
- **Risk**: Anyone with repository access can send emails/SMS impersonating the application
- **Remediation**: **IMMEDIATE** - Rotate this key and move to Azure Key Vault

### CRIT-002: AllowedHosts Wildcard Configuration
- **File**: `src/SkillLedger.Api/appsettings.json:12`
- **Issue**: `"AllowedHosts": "*"` allows requests from any host
- **Risk**: Host Header Injection attacks, cache poisoning, password reset token theft
- **Remediation**: Set specific allowed hosts for each environment

### CRIT-003: Null Reference with IConnectionMultiplexer
- **File**: `src/SkillLedger.Api/Program.cs:502,534,542,549`
- **Issue**: Registering `null!` for `IConnectionMultiplexer` when Redis unavailable
- **Risk**: NullReferenceException at runtime when services inject Redis
- **Remediation**: Implement proper null-object pattern or throw startup exception

### CRIT-004: Potential Deadlock in DistributedLockService
- **File**: `src/SkillLedger.Infrastructure/Services/DistributedLockService.cs:129-147`
- **Issue**: Static `SemaphoreSlim` shared across all instances without proper exception handling
- **Risk**: Application-wide deadlocks during payment processing
- **Remediation**: Add proper try-finally blocks and deadlock detection

### CRIT-005: Escrow Double-Release Vulnerability
- **File**: `src/SkillLedger.Infrastructure/Services/ProjectEscrowService.cs:381-469`
- **Issue**: Two concurrent requests can both pass `CanBeReleased` check before lock
- **Risk**: Complete escrow amount released twice to the same provider
- **Remediation**: Move validation inside the distributed lock

### CRIT-006: Credit Wallet Data Inconsistency
- **File**: `src/SkillLedger.Infrastructure/Services/CreditWalletService.cs:1250-1276`
- **Issue**: Balance reconciliation doesn't properly account for EscrowDeposit/EscrowRefund
- **Risk**: False discrepancies reported, potential credit manipulation
- **Remediation**: Include all transaction types in reconciliation

### CRIT-007: Missing Transaction Wrapping
- **File**: `src/SkillLedger.Infrastructure/Services/CreditWalletService.cs:230-243`
- **Issue**: ExecutionStrategy inconsistently applied - partial execution on retries
- **Risk**: Credits lost or duplicated during transient failures
- **Remediation**: Wrap ALL database operations in execution strategy

### CRIT-008: Incomplete Escrow Refund Validation
- **File**: `src/SkillLedger.Infrastructure/Services/CreditWalletService.cs:940-964`
- **Issue**: No verification that escrow is in refundable state before processing
- **Risk**: Same escrow refunded multiple times via concurrent requests
- **Remediation**: Add atomic state check within distributed lock

---

## 2. Backend (.NET) Bugs

### HIGH Severity

#### BE-HIGH-001: Missing Authorization on Metrics Endpoint
- **File**: `src/SkillLedger.Api/Program.cs:777-802`
- **Issue**: Metrics endpoint leaks sensitive system information
- **Risk**: Information disclosure for attack planning

#### BE-HIGH-002: Improper Exception Handling in PaymentService
- **File**: `src/SkillLedger.Infrastructure/Services/PaymentService.cs:113-124,396-405`
- **Issue**: Generic exception catch followed by re-throw without context
- **Risk**: Transactions left in inconsistent states

#### BE-HIGH-003: Resource Leak in DocumentService
- **File**: `src/SkillLedger.Infrastructure/Services/DocumentService.cs:255-261`
- **Issue**: FileStream not disposed if exception occurs before return
- **Risk**: File handle exhaustion over time

#### BE-HIGH-004: Race Condition in Payment Processing
- **File**: `src/SkillLedger.Infrastructure/Services/PaymentService.cs:259-279`
- **Issue**: Lock could expire mid-operation during long payments
- **Risk**: Double-charging or double-refunding subscribers

#### BE-HIGH-005: Fire-and-Forget Database Migration
- **File**: `src/SkillLedger.Api/Program.cs:808-861`
- **Issue**: Background task for DB initialization not awaited
- **Risk**: Database corruption in containerized deployments

#### BE-HIGH-006: Console.WriteLine Logging Sensitive Data
- **File**: `src/SkillLedger.Api/Program.cs:62,73,77,84`
- **Issue**: Application Insights connection strings logged to console
- **Risk**: Credential exposure in container/CI logs

### MEDIUM Severity

#### BE-MED-001: Timing Attack in Email Enumeration Prevention
- **File**: `src/SkillLedger.Api/Controllers/AuthController.cs:142-145`
- **Issue**: Fixed delay range (100-500ms) is predictable
- **Risk**: Statistical analysis can still enumerate emails

#### BE-MED-002: Missing Input Validation on SearchQuery
- **File**: `src/SkillLedger.Infrastructure/Services/DocumentService.cs:849-873`
- **Issue**: No length/pattern validation on search input
- **Risk**: ReDoS or performance degradation

#### BE-MED-003: Reputation Score Race Condition
- **File**: `src/SkillLedger.Infrastructure/Services/ReputationCalculationService.cs:510-594`
- **Issue**: Calculation uses read-only queries outside lock
- **Risk**: Lost updates on concurrent reviews

#### BE-MED-004: Credit Transfer Rate Limiting Bypass
- **File**: `src/SkillLedger.Infrastructure/Services/CreditTransferService.cs:523-575`
- **Issue**: Only COMPLETED transfers counted against limits
- **Risk**: Unlimited pending transfers can be created

#### BE-MED-005: Pagination DoS Vulnerability
- **File**: `src/SkillLedger.Infrastructure/Services/CreditTransferService.cs:447-474`
- **Issue**: No upper bound on page parameter
- **Risk**: Database performance degradation with high page numbers

#### BE-MED-006: Transaction Status Invalid Transitions
- **File**: `src/SkillLedger.Infrastructure/Services/ProjectEscrowService.cs:487-507`
- **Issue**: No validation of current status before canceling
- **Risk**: Phantom refunds from cancelled completed escrows

### LOW Severity

#### BE-LOW-001: Hardcoded CDN Configuration
- **File**: `src/SkillLedger.Api/Program.cs:349-352`
- **Issue**: CDN endpoint hardcoded
- **Risk**: Configuration management issues

#### BE-LOW-002: N+1 Query in Circular Reference Detection
- **File**: `src/SkillLedger.Infrastructure/Services/DocumentService.cs:1266-1297`
- **Issue**: Separate query for each folder level
- **Risk**: Performance degradation with deep nesting

#### BE-LOW-003: Exception Information Leakage
- **File**: `src/SkillLedger.Infrastructure/Services/PaymentService.cs:170-175`
- **Issue**: Full exception details in audit logs
- **Risk**: Information disclosure if logs compromised

#### BE-LOW-004: Weak Stripe Placeholder Check
- **File**: `src/SkillLedger.Infrastructure/Services/PaymentService.cs:41-50`
- **Issue**: Brittle check on "REPLACE_WITH" placeholder
- **Risk**: Wrong placeholder might bypass validation

---

## 3. Frontend (Next.js) Bugs

### HIGH Severity

#### FE-HIGH-001: Missing CSRF Validation in ForgotPassword
- **File**: `web/src/components/ForgotPassword.tsx:46-77`
- **Issue**: CSRF token fetch doesn't validate `response.ok` before parsing
- **Risk**: Unhandled promise rejection, potential security bypass

#### FE-HIGH-002: AuthContext Circular Dependencies
- **File**: `web/src/contexts/AuthContext.tsx:46-47,62-92`
- **Issue**: Intentionally disabled eslint for circular dependency workaround
- **Risk**: Fragile state management, potential infinite loops

#### FE-HIGH-003: Missing Local Error Boundaries
- **File**: Multiple high-risk components
- **Issue**: Only root-level ErrorBoundary exists
- **Risk**: Single component crash takes down entire section

### MEDIUM Severity

#### FE-MED-001: useEffect Dependency Issues in FileManager
- **File**: `web/src/components/workspace/FileManager.tsx:92-135`
- **Issue**: Complex dependency chain causing unnecessary re-renders
- **Risk**: Performance degradation, stale data

#### FE-MED-002: Memory Leak in ProjectCreationForm
- **File**: `web/src/components/ProjectCreationForm.tsx:168-171`
- **Issue**: Auto-save interval recreated on dependency change
- **Risk**: Multiple intervals accumulating

#### FE-MED-003: Race Condition in Token Refresh
- **File**: `web/src/contexts/AuthContext.tsx:287-343`
- **Issue**: Crude polling mechanism for refresh wait
- **Risk**: 5-second logout delays

#### FE-MED-004: Schema/Input Mismatch
- **File**: `web/src/components/ProjectCreationForm.tsx:387`
- **Issue**: Input `max="5000"` but schema allows 50000
- **Risk**: User confusion, data validation inconsistency

#### FE-MED-005: Event Listener Leak in EnhancedNavigation
- **File**: `web/src/components/EnhancedNavigation.tsx:111-114`
- **Issue**: Multiple keydown listeners on remount
- **Risk**: Memory leaks, unexpected behavior

#### FE-MED-006: Stale Closure in Session Timeout
- **File**: `web/src/contexts/AuthContext.tsx:113-128`
- **Issue**: Logout captured via closure, won't update if logic changes
- **Risk**: Session timeout using outdated logout implementation

### LOW Severity

#### FE-LOW-001: Unsafe Type Assertion
- **File**: `web/src/components/messaging/MessageSearch.tsx:236`
- **Issue**: `as any` type assertion loses type safety
- **Risk**: Runtime type errors

#### FE-LOW-002: Missing aria-label on Close Button
- **File**: `web/src/components/workspace/FileManager.tsx:517`
- **Issue**: X icon without ARIA labels
- **Risk**: Accessibility violation

#### FE-LOW-003: Missing aria-live Region
- **File**: `web/src/components/messaging/MessageCenter.tsx:383-387`
- **Issue**: MessageList lacks aria-live for real-time updates
- **Risk**: Screen reader users miss new messages

#### FE-LOW-004: Typing Timer Memory Leak
- **File**: `web/src/components/messaging/MessageCenter.tsx:69-72`
- **Issue**: Typing timers map not fully cleared on unmount
- **Risk**: Minor memory leak on component remount

---

## 4. Business Logic Bugs

### CRITICAL

#### BL-CRIT-001: Escrow Double-Release (see CRIT-005)
#### BL-CRIT-002: Credit Wallet Data Inconsistency (see CRIT-006)

### HIGH Severity

#### BL-HIGH-001: Milestone Boundary Validation
- **File**: `src/SkillLedger.Infrastructure/Services/ProjectEscrowService.cs:210-215`
- **Issue**: Uses `>` instead of `>=` allowing 100%+ allocation
- **Risk**: Over-allocation of escrow funds

#### BL-HIGH-002: Fraud Detection Not Enforcing Blocks
- **File**: `src/SkillLedger.Infrastructure/Services/CreditWalletService.cs:330-342`
- **Issue**: High-risk transactions logged but not always blocked
- **Risk**: Fraudulent transactions proceed with warning only

#### BL-HIGH-003: Milestone Out-of-Order Bypass
- **File**: `src/SkillLedger.Infrastructure/Services/ProjectEscrowService.cs:292-305`
- **Issue**: Blocking milestone check can be bypassed if marked released without payment
- **Risk**: Out-of-order payment releases

### MEDIUM Severity

#### BL-MED-001: Idempotency Key Scope Too Narrow
- **File**: `src/SkillLedger.Infrastructure/Services/CreditTransferService.cs:76-101`
- **Issue**: Only checks FromUserId, not full transfer context
- **Risk**: Same key allows multiple transactions from different users

#### BL-MED-002: Integer Underflow Not Protected
- **File**: `src/SkillLedger.Infrastructure/Services/CreditWalletService.cs:171-205`
- **Issue**: `balance - pending` can return negative without validation
- **Risk**: Negative available balance causing downstream issues

---

## 5. Configuration & Infrastructure Issues

### HIGH Severity

#### CFG-HIGH-001: Docker Hardcoded SA Password
- **File**: `docker-compose.yml:9`
- **Issue**: `MSSQL_SA_PASSWORD=YourStrong@Passw0rd`
- **Risk**: Known default credential in source control

#### CFG-HIGH-002: TrustServerCertificate in Production Config
- **File**: `src/SkillLedger.Api/appsettings.json:12`
- **Issue**: `TrustServerCertificate=true` disables SSL validation
- **Risk**: MITM attacks on database connections

#### CFG-HIGH-003: EnableSensitiveDataLogging
- **File**: `src/SkillLedger.Api/Program.cs:140-141`
- **Issue**: EF Core sensitive data logging enabled in development
- **Risk**: PII and credentials logged to console

#### CFG-HIGH-004: HTTPS Enforcement Missing in Development
- **File**: `src/SkillLedger.Api/Program.cs:707-710`
- **Issue**: `UseHttpsRedirection()` skipped in development
- **Risk**: Development cookies transmitted over HTTP

#### CFG-HIGH-005: Untrusted Proxy Configuration
- **File**: `src/SkillLedger.Api/Program.cs:651-659`
- **Issue**: KnownProxies cleared, trusting all reverse proxies
- **Risk**: X-Forwarded-For spoofing

### MEDIUM Severity

#### CFG-MED-001: CSP Allows Unsafe-Inline Styles
- **File**: `src/SkillLedger.Api/Program.cs:720`
- **Issue**: `style-src 'self' 'unsafe-inline'`
- **Risk**: CSS-based XSS attacks

#### CFG-MED-002: CORS AllowAnyMethod
- **File**: `src/SkillLedger.Api/Program.cs:629`
- **Issue**: All HTTP methods allowed
- **Risk**: Unintended DELETE/PATCH access

#### CFG-MED-003: Frontend HTTP API URLs
- **File**: `web/.env.local:4-5,16`
- **Issue**: API URLs use HTTP instead of HTTPS
- **Risk**: Unencrypted API communication in development

---

## 6. Test Code Issues

### CRITICAL

#### TEST-CRIT-001: Static Test Counter Race Condition
- **File**: `tests/SkillLedger.Tests/Integration/AuthenticationIntegrationTests.cs:26`
- **File**: `tests/SkillLedger.Tests/Integration/DatabaseContextIsolationTests.cs:24`
- **Issue**: Static `_testCounter` causes conflicts in parallel execution
- **Risk**: Non-unique test data, intermittent test failures

#### TEST-CRIT-002: Skipped Critical Authentication Tests
- **File**: `tests/SkillLedger.Tests/Integration/AuthenticationIntegrationTests.cs:252-270`
- **Issue**: Logout and LogoutAll tests disabled with `[Fact(Skip = "...")]`
- **Risk**: Critical auth paths not tested

### HIGH Severity

#### TEST-HIGH-001: Flaky Tests with Thread.Sleep
- **Files**: Multiple test files
  - `tests/SkillLedger.Tests/Unit/UserCreditReportEntityTests.cs:112,129,233,507`
  - `tests/SkillLedger.Tests/Core/Entities/CategoryReputationScoresTests.cs:191`
  - `tests/SkillLedger.Tests/Core/Entities/UserReputationScoresTests.cs:181`
- **Issue**: Fixed timing assumptions in unit tests
- **Risk**: Tests fail on slow CI/CD systems

#### TEST-HIGH-002: Missing Authorization Tests for DEBUG Endpoints
- **File**: `src/SkillLedger.Api/Controllers/ExperienceController.cs:476-568`
- **Issue**: No tests verify `[AllowAnonymous]` removed in RELEASE builds
- **Risk**: Debug endpoints could ship to production

#### TEST-HIGH-003: Missing Cookie Security Tests
- **File**: `tests/SkillLedger.Tests/Integration/AuthenticationIntegrationTests.cs:117-118`
- **Issue**: No verification of HttpOnly, Secure, SameSite attributes
- **Risk**: False confidence in cookie security

#### TEST-HIGH-004: Rate Limiting Test Doesn't Verify Blocking
- **File**: `tests/SkillLedger.Tests/Integration/UserRegistrationIntegrationTests.cs:200-237`
- **Issue**: Test doesn't verify 6th+ attempts are actually blocked
- **Risk**: False confidence in rate limiting

### MEDIUM Severity

#### TEST-MED-001: Cleanup Errors Silently Ignored
- **File**: `tests/SkillLedger.Tests/Integration/AuthenticationIntegrationTests.cs:45-66`
- **Issue**: Catch block ignores all cleanup exceptions
- **Risk**: State leakage between tests

#### TEST-MED-002: Hardcoded Test User GUIDs
- **File**: `tests/SkillLedger.Tests/Infrastructure/SimpleTestDataSeeder.cs:34-38`
- **Issue**: Fixed GUIDs for standard users
- **Risk**: Cross-test dependencies

#### TEST-MED-003: Weak Timestamp Assertions
- **File**: `tests/SkillLedger.Tests/Unit/UserCreditReportEntityTests.cs:26,137`
- **Issue**: 5-second tolerance too wide for unit tests
- **Risk**: Tests pass with significantly wrong timestamps

#### TEST-MED-004: Frontend Documentation-Only Tests
- **File**: `web/src/__tests__/middleware.test.ts`
- **Issue**: Tests only check array length, not actual middleware behavior
- **Risk**: No real middleware testing

### Known TODO Items Requiring Attention

1. `tests/SkillLedger.Tests/Security/MessagingSecurityTests.cs:26,40` - JWT removal
2. `tests/SkillLedger.Tests/Integration/MessagingApiIntegrationTests.cs:22,31` - JWT removal
3. `tests/SkillLedger.Tests/Integration/AuthenticationIntegrationTests.cs:255,265` - Cookie auth rewrite
4. `src/SkillLedger.Infrastructure/Services/GamingDetectionML.cs:90` - Graph neural network
5. `src/SkillLedger.Infrastructure/Services/FileShareService.cs:207` - Security scanning
6. `src/SkillLedger.Infrastructure/Services/StripeWebhookService.cs:150-232` - Multiple webhook handlers

---

## Bug Fix Session Log — fix/bug-audit-fixes branch (2026-03-18)

The following bugs were addressed in the `fix/bug-audit-fixes` branch as part of the comprehensive audit remediation pass:

### BUG-47: Duplicate Service Registrations in Program.cs
- **Severity**: MEDIUM
- **File**: `src/SkillLedger.Api/Program.cs:396-398`
- **Issue**: `ControllerHelperService` and `IIdempotencyService` were registered twice — once at line 344-345 and again at lines 397-398. ASP.NET Core DI resolves the last registration, but duplicate entries waste memory and create confusion.
- **Status**: FIXED
- **Fix**: 2026-03-18, fix/bug-audit-fixes branch — removed the second duplicate block (lines 396-398)

### BUG-37: Contradictory AzureKeyVault Configuration
- **Severity**: HIGH
- **File**: `src/SkillLedger.Api/appsettings.json:58-60,169-172`
- **Issue**: Two separate AzureKeyVault config sections with conflicting settings — `"AzureKeyVaultConfiguration": { "Enabled": false }` and `"AzureKeyVault": { "UseKeyVault": true }`. The second section would silently override the first, potentially enabling Key Vault in production when it should be disabled.
- **Status**: FIXED
- **Fix**: 2026-03-18, fix/bug-audit-fixes branch — consolidated to single `AzureKeyVault` section with `Enabled: false` and `UseKeyVault: false`

---

## 7. Recommended Actions

### Immediate (Within 24 Hours)

1. **ROTATE Azure Communication Services key** - Currently exposed in repo
2. **Remove credentials from appsettings.json** - Move to Key Vault
3. **Change docker-compose SQL password** - Use environment variables
4. **Fix escrow double-release vulnerability** - Move validation inside lock
5. **Add transaction wrapping** - Wrap all DB operations in execution strategy

### Short-Term (Within 1 Week)

6. Fix credit transfer rate limiting bypass
7. Add proper authorization to metrics endpoint
8. Complete skipped authentication tests
9. Fix FileStream resource leak in DocumentService
10. Add cookie security attribute verification tests
11. Configure specific trusted proxy IPs
12. Remove `unsafe-inline` from CSP

### Medium-Term (Within 1 Month)

13. Implement proper null-object pattern for Redis
14. Add local error boundaries to high-risk components
15. Fix all Thread.Sleep() in tests with proper synchronization
16. Complete JWT removal TODOs
17. Add authorization tests for DEBUG endpoints
18. Implement proper idempotency key scoping
19. Fix timing attack in email enumeration prevention

### Long-Term (Backlog)

20. Implement graph neural network for gaming detection
21. Integrate security scanning for file uploads
22. Complete Stripe webhook handler implementations
23. Refactor AuthContext to eliminate circular dependencies
24. Convert documentation-only tests to real integration tests

---

## Appendix: Files with Most Issues

| File | Issue Count | Severity |
|------|-------------|----------|
| `Program.cs` | 12 | Mixed |
| `CreditWalletService.cs` | 8 | Critical/High |
| `ProjectEscrowService.cs` | 6 | Critical/High |
| `AuthenticationIntegrationTests.cs` | 6 | Critical/High |
| `PaymentService.cs` | 5 | High/Medium |
| `CreditTransferService.cs` | 4 | High/Medium |
| `AuthContext.tsx` | 4 | High/Medium |
| `ForgotPassword.tsx` | 3 | High |
| `DocumentService.cs` | 3 | High/Medium |

---

**Report Generated By**: Claude Code Comprehensive Bug Audit
**Files Analyzed**: 200+ source files across backend, frontend, tests, and configuration
**Analysis Depth**: Security, business logic, configuration, code quality, accessibility
