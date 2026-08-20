# Security

SkillLedger handled three things that raise the bar above a typical CRUD app: encrypted credit
wallets, Stripe in live mode, and ASP.NET Identity rows carrying real user PII. This document
covers the surface [CREDIT-LEDGER.md](./CREDIT-LEDGER.md) does not: authentication cookies, CSRF,
authorization policies, what PII actually sits in the database, and how Stripe is wired in. It does
not re-argue the wallet-encryption findings; those are documented there, with line citations, in
more depth than a summary here could carry: `AzureKeyVaultService` falls back to a key derived from
a hardcoded string when `Enabled` is false, `Program.cs` never binds
`AzureKeyVaultConfiguration` to configuration at all so that flag was always false in every
environment including production, `GetTransactionHashKeyAsync` signs the transaction hash chain with
a `const string`, and `RotateEncryptionKeysAsync` logs "not yet implemented" and returns `true`. Read
that document for the mechanism; this one is the rest of the attack surface.

Related: [CREDIT-LEDGER.md](./CREDIT-LEDGER.md), [ENGINEERING-LOG.md](./ENGINEERING-LOG.md),
[ARCHITECTURE.md](./ARCHITECTURE.md).

---

## Authentication is cookie-based, not JWT

The project's own tooling notes list "ASP.NET Identity, JWT Bearer" as the auth stack. What is
actually wired in `Program.cs` is Identity's cookie authentication and nothing else:
`AddJwtBearer` is never called. The one surviving reference to JWT is a comment above a memory-cache
registration, "Add memory cache for JWT token blacklisting"
([Program.cs:577](../src/SkillLedger.Api/Program.cs#L577)), for a bearer-token scheme that isn't
configured. Every login, in every environment, gets a `.SkillLedger.Auth` cookie
([Program.cs:238](../src/SkillLedger.Api/Program.cs#L238)) issued by `SignInManager`.

The cookie itself: `HttpOnly` always; 15-minute sliding expiration
([Program.cs:275](../src/SkillLedger.Api/Program.cs#L275)); `SameSite=Strict` and `Secure=Always` in
development unless `AllowInsecureDevCookies` is explicitly set, `SameSite=None` with
`Secure=Always` and `Domain=.skillledger.app` in production so the cookie crosses the
`skillledger.app` / `api.skillledger.app` split
([Program.cs:247-273](../src/SkillLedger.Api/Program.cs#L247)). `SameSite=None` widens the CSRF
surface on its own; it's why the antiforgery layer below carries real weight rather than being
redundant with `SameSite`.

Every request re-validates the security stamp (`ValidationInterval = TimeSpan.Zero`,
[Program.cs:231](../src/SkillLedger.Api/Program.cs#L231)), so `LogoutFromAllDevicesAsync` (which
calls `UserManager.UpdateSecurityStampAsync` then signs out the calling session,
[AuthenticationService.cs:175](../src/SkillLedger.Infrastructure/Services/AuthenticationService.cs#L175))
invalidates every other open cookie on the next request that session makes, not on its own schedule.
That's a real global-logout, not a cosmetic one.

**Password and account protection**, all verified against source rather than the docs describing
them: 12-character minimum with upper, lower, digit and symbol required
([Program.cs:207-212](../src/SkillLedger.Api/Program.cs#L207)); lockout after 5 failed attempts for
30 minutes ([Program.cs:215-217](../src/SkillLedger.Api/Program.cs#L215)); password hashing is
unmodified ASP.NET Identity: no custom `IPasswordHasher<User>` is registered anywhere in `src/`,
so it's the framework's PBKDF2-SHA256 default. Registration is rate-limited to 5 attempts per hour
per IP by default (`RegistrationPerHour`, configurable,
[RateLimitingConfiguration.cs:13](../src/SkillLedger.Api/Configuration/RateLimitingConfiguration.cs#L13)),
login to 10 attempts per 15 minutes per IP
([RateLimitingConfiguration.cs:48](../src/SkillLedger.Api/Configuration/RateLimitingConfiguration.cs#L48)).
The email-availability check at registration always returns `IsAvailable = true` with a generic
message regardless of whether the address exists
([AuthController.cs:151](../src/SkillLedger.Api/Controllers/AuthController.cs#L151)): deliberate
enumeration resistance, not an oversight; the comment on the line says so.

---

## CSRF: one global filter, and it's real

`AddControllers` registers `AutoValidateAntiforgeryTokenAttribute` as a global MVC filter
([Program.cs:121](../src/SkillLedger.Api/Program.cs#L121)), so every controller action requires a
valid antiforgery token by default; the scattered `[ValidateAntiForgeryToken]` attributes across
`SkillController`, `EscrowController`, `MilestoneController` and others are redundant with that
global filter, not the thing doing the work. The header name is `X-CSRF-TOKEN`
([Program.cs:298](../src/SkillLedger.Api/Program.cs#L298)); a client fetches a token from
`GET /api/auth/csrf-token`, which calls `IAntiforgery.GetAndStoreTokens`
([AuthController.cs:163-172](../src/SkillLedger.Api/Controllers/AuthController.cs#L163)) and returns
it in a JSON body alongside the header name. The antiforgery cookie is separate from the auth
cookie (`.SkillLedger.Antiforgery`), `HttpOnly`, and follows the same dev/prod `SameSite` split as
the auth cookie ([Program.cs:296-318](../src/SkillLedger.Api/Program.cs#L296)).

Endpoints that legitimately can't carry a CSRF token opt out explicitly with
`[IgnoreAntiforgeryToken]`: login, register and password-reset, because no session cookie exists yet
to bind a token to ([AuthController.cs:182](../src/SkillLedger.Api/Controllers/AuthController.cs#L182)
and three other sites in the same file), and the Stripe and Resend webhook endpoints
([WebhookController.cs:36](../src/SkillLedger.Api/Controllers/WebhookController.cs#L36),
[WebhooksController.cs:45](../src/SkillLedger.Api/Controllers/WebhooksController.cs#L45)), which
authenticate the caller by cryptographic signature instead: Stripe's HMAC-SHA256 request signature
via `EventUtility.ConstructEvent`
([StripeWebhookService.cs:77](../src/SkillLedger.Infrastructure/Services/StripeWebhookService.cs#L77)),
Resend's Svix-format HMAC-SHA256 with a 5-minute timestamp tolerance
([WebhooksController.cs:194](../src/SkillLedger.Api/Controllers/WebhooksController.cs#L194)). That's
the correct substitute for CSRF on a server-to-server callback, not a gap.

A second, narrower antiforgery filter, `ConditionalAntiforgeryFilter` (which skips validation when
`IWebHostEnvironment.IsEnvironment("Testing")`), is only wired into one controller,
`ProviderSelectionController`, via `[ServiceFilter]`
([ProviderSelectionController.cs:47](../src/SkillLedger.Api/Controllers/ProviderSelectionController.cs#L47)).
It's inert in practice: the integration test harness already fetches and attaches a real CSRF token
for POST requests
([IntegrationTestBase.cs:155-228](../tests/SkillLedger.Tests/Infrastructure/IntegrationTestBase.cs#L155)),
so nothing in the test suite currently depends
on this controller's carve-out. It's an inconsistency worth naming: one controller has a
test-environment escape hatch nothing else has, and nothing exercises it, not a hole, since the
global filter still runs underneath it.

---

## Authorization: three separate systems doing three separate jobs

**Role-based**, the coarsest layer: two named policies, `RequireAdminPermission` and
`RequireAdminRole`, both defined identically as `policy.RequireRole("Admin")`
([Program.cs:556-560](../src/SkillLedger.Api/Program.cs#L556)): two names for one check, which is
copy-paste debt rather than a security question, since both resolve to the same role membership.

**Permission-based**, the finest layer: `PermissionPolicyProvider` recognizes policy names of the
form `RequirePermission:{name}` and `RequirePermissions:{AND|OR}:{name1,name2}` at request time
rather than requiring every permission to be pre-registered
([PermissionPolicyProvider.cs:24-59](../src/SkillLedger.Infrastructure/Authorization/PermissionPolicyProvider.cs#L24)).
`PermissionAuthorizationHandler` resolves the caller's `NameIdentifier` claim and calls
`IAuthorizationService.HasAnyPermissionAsync` / `HasAllPermissionsAsync`
([PermissionAuthorizationHandler.cs:40-42](../src/SkillLedger.Infrastructure/Authorization/PermissionAuthorizationHandler.cs#L40)),
which is backed by a real `RolePermissions` table join through the user's roles
([AuthorizationService.cs:60](../src/SkillLedger.Infrastructure/Services/AuthorizationService.cs#L60)),
not a hardcoded list. A fixed set of seven permission names (`ManageRoles`, `ManageCredits`,
`ADMIN_ESCROW_MANAGEMENT` among them) is additionally flagged `PrivilegedPermissions` in that same
service for extra logging/audit treatment
([AuthorizationService.cs:19-27](../src/SkillLedger.Infrastructure/Services/AuthorizationService.cs#L19)).

**Subscription-based**, the business layer, not a security boundary in the traditional sense:
fourteen policies gating features by subscription tier (`BusinessOrHigher`, `EnterpriseTier`,
`AdvancedFraudDetection`, `MultiSignature` and others,
[Program.cs:501-553](../src/SkillLedger.Api/Program.cs#L501)), each backed by a custom
`IAuthorizationRequirement` and evaluated against the caller's live subscription row. These decide
what a paying account can *do*, not who is allowed to see whose data: worth keeping distinct from
the two systems above when reading the policy list.

---

## What PII this database actually holds

`User : IdentityUser<Guid>` carries the Identity framework columns directly (`Email`,
`NormalizedEmail`, `PasswordHash` (hashed, never plaintext), `PhoneNumber`, `SecurityStamp`), plus
custom columns added on top: `FirstName`, `LastName`, `CreatedFromIP` and `UpdatedFromIP` (both
`MaxLength(45)` to hold IPv6), and `ExternalCustomerId`, the Stripe customer ID
([User.cs:1-140](../src/SkillLedger.Core/Entities/User.cs#L1)). None of those columns are encrypted
at rest: encryption in this codebase is reserved for the four wallet-balance fields on
`CreditWallet`, described in [CREDIT-LEDGER.md](./CREDIT-LEDGER.md); a user's email, name and IP
history sit in plaintext columns, protected only by database access control, which is the ordinary
and expected posture for that class of data.

`Profile` adds a public-facing layer users opt into: bio, location, social links, avatar, and a
`Visibility` enum (`Private` / `VerifiedUsersOnly` / `Internal` / `Public`,
[Profile.cs:180-201](../src/SkillLedger.Core/Entities/Profile.cs#L180)) that gates who can see it.
Whether every read path actually enforces that enum is a claim this document is not making; it
would need its own audit of every controller that serves a `Profile`.

`PaymentMethod` stores what Stripe's client-side tokenization is meant to keep this database from
ever seeing: a `Token` (the tokenized identifier, not a card number), `Last4Digits`, `Brand`,
`ExpiryDate`, `CardholderName`, billing country and postal code
([PaymentMethod.cs:14-77](../src/SkillLedger.Core/Entities/PaymentMethod.cs#L14)). No full PAN, no
CVV field exists on the entity, consistent with Stripe.js handling card capture client-side and the
backend only ever holding what Stripe returns.

`AuditLog` is its own PII surface, not just a defense: `UserId`, `IPAddress`, `UserAgent` and a free-text
`Details` field are retained per event with no stated retention window
([AuditLog.cs](../src/SkillLedger.Core/Entities/AuditLog.cs)), a security control that is also a
growing store of who did what from which address, indefinitely, by default.

---

## Stripe: live mode, signature-verified, keys blank in the tree

`appsettings.Production.json` sets `"Stripe": { "IsEnabled": true, "IsTestMode": false }`
([appsettings.Production.json:66-69](../src/SkillLedger.Api/appsettings.Production.json#L66)): live
mode, consistent with [ARCHITECTURE.md](./ARCHITECTURE.md)'s and
[CREDIT-LEDGER.md](./CREDIT-LEDGER.md)'s framing of the product as having run with Stripe live.
Neither the frontend's publishable key nor the backend's secret key is a literal string anywhere in
this tree: `Stripe:SecretKey`, `Stripe:PublishableKey` and `Stripe:WebhookSecret` are all empty
strings in the checked-in `appsettings.json`
([appsettings.json:164-171](../src/SkillLedger.Api/appsettings.json#L164)), and `web/.env.example`
only names `NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY` without a value. A comment on the same lines in
`appsettings.json` notes the keys are meant to arrive via environment variable or user secrets: no live secret
key is present anywhere in this repository. `StripeCheckoutService` sets
`StripeConfiguration.ApiKey` from that (empty, in the tree) `SecretKey` at construction
([StripeCheckoutService.cs:39](../src/SkillLedger.Infrastructure/Services/StripeCheckoutService.cs#L39)).

The webhook path validates before it trusts: `WebhookController.HandleStripeWebhook` reads the raw
body, requires a `Stripe-Signature` header, and rejects anything that doesn't verify against
`Stripe:WebhookSecret` via `EventUtility.ConstructEvent` before any event is processed
([WebhookController.cs:34-85](../src/SkillLedger.Api/Controllers/WebhookController.cs#L34)); this
was itself a fix, `BUG-CRIT-004`, and the comment trail on those lines documents that it used to
process unverified payloads. `ProcessedStripeWebhookEvent` exists as a table specifically to make a
replayed webhook a no-op rather than a double-credit. Three open defects were still recorded against
the live webhook service at shutdown, including a `NullReferenceException` when `data.object` is
null with no guard before parsing, see
[ENGINEERING-LOG.md, entry 8](./ENGINEERING-LOG.md#8-stripe-was-checked-in-twice-and-one-copy-had-no-live-counterpart)
for the full list rather than restating it here, to avoid two documents disagreeing about the same
file.

Adjacent to Stripe is the transfer-receipt signing key, which is not encryption but a related
integrity mechanism worth flagging from here: `CreditTransferService` signs receipts with an HMAC
whose key is resolved through a three-step `??` fallback:
`AzureKeyVault:ReceiptSignatureKey` (a config path that doesn't exist in any settings file, so
always `null`), then `CreditTransfer:ReceiptSecretKey` (present but shipped as an empty string,
`""`, which `??` treats as a value, not a fallthrough), then an environment variable
([CreditTransferService.cs:49-55](../src/SkillLedger.Infrastructure/Services/CreditTransferService.cs#L49)).
The practical effect (every request that touches credit transfer throwing "does not meet minimum
security requirements" unless the framework's own `CreditTransfer__ReceiptSecretKey` environment
binding was set) is documented in full in
[ENGINEERING-LOG.md, entry 6](./ENGINEERING-LOG.md#6-a-three-step-fallback-chain-whose-last-two-steps-were-unreachable).

---

## Scope: what this review does not cover

Everything above is a reviewer's own reading of the code, not the output of a security process the
project ran on itself.

A few specific gaps, verified as absences rather than asserted from memory:

- **`AddDataProtection()` is called with no configuration at all**
  ([Program.cs:321](../src/SkillLedger.Api/Program.cs#L321)): no `PersistKeysToAzureBlobStorage`,
  no `ProtectKeysWithAzureKeyVault`, no `SetApplicationName`. Data Protection keys back both the
  auth cookie and the antiforgery cookie. Whether the hosting environment persisted those keys
  across restarts and shared them across instances is not something this repository can answer;
  the file that would say so isn't here.
- **No database-level constraint stops a wallet balance from going negative**, discussed in full in
  [CREDIT-LEDGER.md](./CREDIT-LEDGER.md#what-encrypting-the-balance-actually-costs): overdraft
  protection is entirely an application-layer check inside a `Serializable` transaction.
- **The distributed lock silently degrades to a process-local lock** when Redis is absent, which
  removes the mutual exclusion that escrow releases and credit transfers rely on across more than
  one running instance, see
  [ENGINEERING-LOG.md, entry 9](./ENGINEERING-LOG.md#9-redis-was-registered-as-literal-null-and-the-lock-quietly-stopped-being-distributed).
  Whether the deployed instance count ever exceeded one, and so ever exercised this failure mode, is
  not recorded anywhere in this repository.
- **`VerifyEncryptionIntegrityAsync` checks that a row decrypts, not that it's correct**: it
  doesn't recompute the transaction chain or compare a decrypted balance to anything, per
  [CREDIT-LEDGER.md](./CREDIT-LEDGER.md#the-key-rotation-that-was-designed-and-never-written). An
  operator trusting that method's name would trust more than it verifies.
- **No secrets scanning, dependency vulnerability scanning, or SAST tooling is configured** anywhere
  in this tree: no `.github/dependabot.yml`, no `CodeQL` workflow, nothing under `.githooks/` that
  runs one. Whichever of the placeholder keys and constant HMAC secrets documented above and in
  [CREDIT-LEDGER.md](./CREDIT-LEDGER.md) would have been caught by that kind of tooling, none of it
  ran.

This is a security *reading*, not a security *clearance*: a reviewer's pass through the code, not
the output of a dedicated audit process.
