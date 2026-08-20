# Engineering log

SkillLedger ran in production at `skillledger.app` from August 2025 to July 2026: 944 commits, .NET 9 API plus a Next.js frontend, Neon Postgres, Stripe in live mode. It is shut down. What follows is a post-mortem of the defects that survived to the end, ordered by how much they taught rather than by severity label. Every claim links to the file that proves it. Related reading: [CREDIT-LEDGER.md](./CREDIT-LEDGER.md), [ARCHITECTURE.md](./ARCHITECTURE.md), [TESTING.md](./TESTING.md), [METRICS.md](./METRICS.md).

---

## 1. The escrow race fix was compiled out of every test that covered it

`BUG-HIGH-010` was the escrow double-release and wallet-balance race. The remediation was to open every financial mutation under `Serializable` isolation and take `FOR UPDATE` row locks on the wallets. It landed at 14 call sites: seven in [CreditWalletService.cs](../src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L266), four in [ProjectEscrowService.cs](../src/SkillLedger.Infrastructure/Services/ProjectEscrowService.cs#L43), three in [CreditTransferService.cs](../src/SkillLedger.Infrastructure/Services/CreditTransferService.cs#L133).

Eleven of those sites are guarded:

```csharp
// Skip transactions for InMemory database in tests
var useTransactions = !_context.Database.ProviderName!.Contains("InMemory");
// BUG-HIGH-010 FIX: Use Serializable isolation for financial operations to prevent race conditions
using var transaction = useTransactions ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
```

([CreditWalletService.cs:235-237](../src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L235); the escrow variant compares `ProviderName` for equality at [ProjectEscrowService.cs:42-44](../src/SkillLedger.Infrastructure/Services/ProjectEscrowService.cs#L42).) The same guard also skips the `FOR UPDATE` row locks: the branch at [CreditWalletService.cs:307](../src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L307) drops to plain LINQ reads because the InMemory provider has no raw SQL.

Every backend test runs on the InMemory provider. [LightweightIntegrationTestBase.cs:48](../tests/SkillLedger.Tests/Infrastructure/LightweightIntegrationTestBase.cs#L48) and [SharedWebApplicationFactory.cs:213](../tests/SkillLedger.Tests/Infrastructure/SharedWebApplicationFactory.cs#L213) both call `UseInMemoryDatabase`. So the fix for the race was never once executed by a test. The three `CreditTransferService` sites that lack the provider check fare no better: EF silently no-ops the transaction and raises `TransactionIgnoredWarning`, which the test host suppresses, with a comment explaining exactly what is being hidden:

```csharp
// CRITICAL: Suppress transaction warnings for in-memory database
// CreditTransferService and other services use transactions with isolation levels
// which are not supported by the in-memory provider. This allows tests to run.
options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
```

([SharedWebApplicationFactory.cs:216-219](../tests/SkillLedger.Tests/Infrastructure/SharedWebApplicationFactory.cs#L216), repeated at [line 569](../tests/SkillLedger.Tests/Infrastructure/SharedWebApplicationFactory.cs#L569).)

**Root cause.** The test harness was chosen for startup speed before the concurrency work existed, and nobody revisited it when the concurrency work arrived. The provider check was the cheapest way to keep the suite green.

**Fix.** None. Both the guards and the warning suppression were in the tree at shutdown.

**Why it is interesting.** The remaining cost is real and also untested: `Serializable` on Postgres aborts conflicting transactions with SQLSTATE `40001`, and nothing in this tree handles that code. The only retry configured is `EnableRetryOnFailure(maxRetryCount: 3, …, errorCodesToAdd: null)` at [Program.cs:191](../src/SkillLedger.Api/Program.cs#L191). `CreditWalletService` at least wraps its work in an execution strategy ([line 242](../src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L242)); `ProjectEscrowService` and `CreditTransferService` contain no `CreateExecutionStrategy` call at all. Under contention, a user's escrow release would fail rather than serialize, and no test could have shown that, because no test ever opened the transaction.

---

## 2. The tamper-evident ledger was signed with a string compiled into the binary

`CreditTransaction` is documented as a "blockchain-inspired immutable ledger with tamper detection" ([CreditTransaction.cs:9](../src/SkillLedger.Core/Entities/CreditTransaction.cs#L9)). Each row carries an HMAC-SHA256 over its own fields ([CalculateHash, line 211](../src/SkillLedger.Core/Entities/CreditTransaction.cs#L211)) and `VerifyHash` recomputes it ([line 224](../src/SkillLedger.Core/Entities/CreditTransaction.cs#L224)). The key comes from here:

```csharp
private Task<byte[]> GetTransactionHashKeyAsync()
{
    // In a real implementation, this would get a dedicated HMAC key from Azure Key Vault
    // For testing/development, use a fixed key derived from a known string
    const string fixedSeed = "SkillLedger-TransactionHash-Key-2024";
    return Task.FromResult(System.Text.Encoding.UTF8.GetBytes(fixedSeed));
}
```

([CreditWalletService.cs:1798-1804](../src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L1798).) It is a `const`. It ships in the assembly. The comment saying what a real implementation would do was written first and never acted on.

The tamper-evidence is decorative. Precisely: it detects storage corruption, a bad backup restore, and a DBA editing a row in a SQL console who does not know the hash column exists. It detects nothing against anyone holding the source, the compiled DLL, or a decompiler: that person recomputes any hash they like in one line. The `GET /api/creditwallet/validate/{id}` endpoint at [CreditWalletController.cs:558](../src/SkillLedger.Api/Controllers/CreditWalletController.cs#L558) reports `isValid: true` against exactly that threat model.

**Fix.** None. `AzureKeyVaultService` exists ([here](../src/SkillLedger.Infrastructure/Services/AzureKeyVaultService.cs)) and is wired for other secrets. It was never wired to this one.

---

## 3. The hash chain had one writer

`CreditTransaction.PreviousTransactionHash` links each row to its predecessor, is indexed for the purpose ([CreditTransactionConfiguration.cs:122](../src/SkillLedger.Infrastructure/Configurations/CreditTransactionConfiguration.cs#L122)), and is folded into the signed payload ([CreditTransaction.cs:213](../src/SkillLedger.Core/Entities/CreditTransaction.cs#L213)).

It is assigned in exactly one place in the entire source tree: [CreditWalletService.cs:370](../src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L370), inside the generic transfer path. The other seven sites that compute a hash (the welcome-bonus credit at [line 90](../src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L90), escrow deposit at [736](../src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L736), the two escrow releases at [856](../src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L856) and [1002](../src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L1002), the refund at [1151](../src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L1151), and two more) sign a row whose `PreviousTransactionHash` is still `null`. Deleting an escrow row from the middle of the ledger leaves nothing behind that points at the gap.

The one writer that does chain has its own problem. It reads the predecessor with an unscoped, untiebroken `OrderByDescending(t => t.CreatedAt).FirstOrDefaultAsync()` across the whole table ([lines 366-368](../src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L366)). Two concurrent transfers read the same tail and both chain to it, which forks the chain rather than extending it.

Separately, `CreditTransfer` (a different entity, a different table, the same product concept) has its own integrity scheme with no key at all:

```csharp
public string GenerateTransactionHash()
{
    var data = $"{FromUserId}|{ToUserId}|{Amount}|{CreatedAt:O}|{Id}";
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    ...
}
```

([CreditTransfer.cs:220-226](../src/SkillLedger.Core/Entities/CreditTransfer.cs#L220), called from [CreditTransferService.cs:179](../src/SkillLedger.Infrastructure/Services/CreditTransferService.cs#L179) and [411](../src/SkillLedger.Infrastructure/Services/CreditTransferService.cs#L411).) A bare digest of five public columns. Anyone who can edit the row can recompute it without needing a key to leak, because there is no key.

**Why it is interesting.** Three integrity mechanisms were built for one ledger, and the strongest of them was still weaker than the docstrings implied. Detail in [CREDIT-LEDGER.md](./CREDIT-LEDGER.md).

---

## 4. Two coverage reports from the same day, disagreeing by up to 15 points

Both dated January 12, 2026.

[OVERALL_BACKEND_COVERAGE_REPORT.md](../docs/OVERALL_BACKEND_COVERAGE_REPORT.md#L5) is marked "MEASURED (not estimated)" and reports 17.8% line coverage overall (25,192 of 141,317 lines, [line 15](../docs/OVERALL_BACKEND_COVERAGE_REPORT.md#L15)), with `SkillLedger.Api` at 3.2% and most controllers at 0% ([lines 21](../docs/OVERALL_BACKEND_COVERAGE_REPORT.md#L21), [52-57](../docs/OVERALL_BACKEND_COVERAGE_REPORT.md#L52)). It states its method: `dotnet-coverage merge` over 25 cobertura files ([lines 158-163](../docs/OVERALL_BACKEND_COVERAGE_REPORT.md#L158)). It names its own earlier estimate of 73-78% as wrong and says why ([lines 106-113](../docs/OVERALL_BACKEND_COVERAGE_REPORT.md#L106)).

[COVERAGE_STATUS_SUMMARY.md](../docs/COVERAGE_STATUS_SUMMARY.md#L28) reports PaymentService at 100%, CreditTransferService at 100%, ProjectEscrowService at 100%. The other file, same day, gives the same three services 85.4%, 98.2% and 88.5% ([lines 37-43](../docs/OVERALL_BACKEND_COVERAGE_REPORT.md#L37)). The summary closes with "MISSION ACCOMPLISHED" ([line 257](../docs/COVERAGE_STATUS_SUMMARY.md#L257)) while its own secondary-objectives table lists overall backend coverage as "Requires full measurement" ([line 176](../docs/COVERAGE_STATUS_SUMMARY.md#L176)), and its Phase 22 entry records catching itself out by 14 points a few weeks earlier: "PaymentService: Found 14% discrepancy (65.62% vs. claimed 79.75%)" ([line 70](../docs/COVERAGE_STATUS_SUMMARY.md#L70)).

I would believe the 17.8%. It names its tool and version, publishes the raw line counts, describes three prior measurement attempts that failed and why, and contradicts a number the same author had previously published. The 100% figures are per-service percentages read off individual runs and never reconciled against each other. A document that catches itself lying and prints the correction is worth more than one that reports three perfect scores.

**Why it is interesting.** The ProjectEscrowService "100%" is the same service whose `Serializable` path is skipped by provider check (entry 1). The number was probably accurate about which lines executed. It was silently wrong about which behaviour was verified. More in [TESTING.md](./TESTING.md) and [METRICS.md](./METRICS.md).

---

## 5. Key rotation returned success without rotating anything

`CreditWallet` carries a `KeyIdentifier` column added specifically for rotation ([CreditWallet.cs:85-90](../src/SkillLedger.Core/Entities/CreditWallet.cs#L85)), and `GenerateKeyIdentifierAsync` populates it on every wallet ([CreditWalletService.cs:1792](../src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L1792)). The interface promises the operation: "Re-encrypts all wallet data with new keys from Azure Key Vault" ([ICreditWalletService.cs:224](../src/SkillLedger.Core/Interfaces/ICreditWalletService.cs#L224)). The implementation:

```csharp
public async Task<bool> RotateEncryptionKeysAsync()
{
    // This would be a complex operation involving: ...
    // For now, return true as placeholder
    _logger.LogInformation("Encryption key rotation requested - not yet implemented");
    return await Task.FromResult(true);
}
```

([CreditWalletService.cs:1563-1574](../src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L1563).)

The return value is the sharp part. `false` would have propagated. `true` means an operator's rotation script, a compliance job, or a caller checking the result all record a successful key rotation that did not happen.

There is a test, and it passes:

```csharp
[Fact]
public async Task RotateEncryptionKeys_ShouldMaintainDataIntegrity()
{
    var wallet = await _service.CreateWalletAsync(_testUser.Id);
    var originalBalance = wallet.Balance;
    await _service.RotateEncryptionKeysAsync();
    var walletAfterRotation = await _service.GetWalletAsync(_testUser.Id);
    walletAfterRotation!.Balance.Should().Be(originalBalance);
}
```

([CreditWalletServiceTests.cs:364-380](../tests/SkillLedger.Tests/Core/Services/CreditWalletServiceTests.cs#L364).) It asserts the balance survives rotation. The balance survives because nothing was rotated. It sits under a region header reading "Key Rotation Tests (TDD Red Phase)" ([line 362](../tests/SkillLedger.Tests/Core/Services/CreditWalletServiceTests.cs#L362)) and has been green the whole time.

---

## 6. A three-step fallback chain whose last two steps were unreachable

`CreditTransferService` signs transfer receipts with a real keyed HMAC ([CreditTransfer.cs:233](../src/SkillLedger.Core/Entities/CreditTransfer.cs#L233)). The key is resolved in the constructor:

```csharp
// Priority order: Azure Key Vault > Configuration > Environment Variable
_receiptSecretKey =
    _configuration["AzureKeyVault:ReceiptSignatureKey"] ??
    _configuration["CreditTransfer:ReceiptSecretKey"] ??
    Environment.GetEnvironmentVariable("RECEIPT_SECRET_KEY") ??
    throw new InvalidOperationException(...);
```

([CreditTransferService.cs:49-55](../src/SkillLedger.Infrastructure/Services/CreditTransferService.cs#L49).)

`??` short-circuits on `null`, not on empty. The shipped [appsettings.json:157](../src/SkillLedger.Api/appsettings.json#L157) contains `"ReceiptSecretKey": ""`, and .NET configuration returns that as an empty string. The second operand therefore always wins, the `RECEIPT_SECRET_KEY` environment variable is dead code, and the length check three lines later ([line 58](../src/SkillLedger.Infrastructure/Services/CreditTransferService.cs#L58)) throws "does not meet minimum security requirements" at DI resolution, on every request that touches credit transfer. The only working override was the framework's own `CreditTransfer__ReceiptSecretKey` env-var binding, which is the mechanism the comment's stated priority order gets backwards: ASP.NET Core layers environment variables *above* `appsettings.json`, not below it.

**Why it is interesting.** The `AzureKeyVault` section exists at [appsettings.json:58](../src/SkillLedger.Api/appsettings.json#L58) but contains no `ReceiptSignatureKey`, so the first operand was also always `null`. A three-way fallback with one live branch and a failure mode that surfaces as a 500 rather than a startup error.

---

## 7. The first production end-to-end run found roughly 1,200 broken pages

[web/FOUND_BUGS.md](../web/FOUND_BUGS.md#L7) records a Playwright run against live `skillledger.app` on 2026-03-18: 13 bugs, 2 critical. BUG-E2E-001 ([line 24](../web/FOUND_BUGS.md#L24)) lists ten dynamic route patterns (`/categories/[slug]`, `/glossary/[term]`, `/industries/[slug]`, `/compare/[slug]`, `/how-to/[slug]`, `/features/[slug]`, `/skill-exchange/[city]`, `/trade/[a]/for/[b]`, `/locations/[city]/[skill]`, `/resources/[slug]`), all returning HTTP 404 in production, against a sitemap the same run verified as valid with 1,286 URLs ([line 159](../web/FOUND_BUGS.md#L159)). The recorded impact: "~1,200+ pages from the sitemap are broken. Google will deindex all dynamic content."

The rest of the run is worth reading as a set. Sign-out did nothing and made no network call (BUG-E2E-003, [line 44](../web/FOUND_BUGS.md#L44)); the same run found no audit log entry for the attempt either (BUG-E2E-013, [line 140](../web/FOUND_BUGS.md#L140)). New registrations got no `CreditWallet` row (BUG-E2E-007, [line 82](../web/FOUND_BUGS.md#L82)), on a product whose entire currency is credits. The subscription page claimed "thousands of professionals", a figure the database did not support (BUG-E2E-012, [line 131](../web/FOUND_BUGS.md#L131)), which the project's own instructions forbid in writing.

**Fix.** The routing fix landed. `generateStaticParams` and `dynamicParams = false` are both present in [categories/[slug]/page.tsx](../web/src/app/categories/%5Bslug%5D/page.tsx#L11), which is the exact remedy the report hypothesised at [line 31](../web/FOUND_BUGS.md#L31). All 13 entries still read `Status: NEW`.

**Why it is interesting.** The tracker was write-only. Bugs went in, fixes went into the code, and the two never met. Four months later at shutdown a reader of that file would have concluded the site was still entirely 404ing.

---

## 8. Stripe was checked in twice, and one copy had no live counterpart

Four files carry a `.disabled` extension, which excludes them from the `**/*.cs` compile glob without removing them from the repository:

| Disabled file | Live sibling |
|---|---|
| [CheckoutController.cs.disabled](../src/SkillLedger.Api/Controllers/CheckoutController.cs.disabled) (10 KB) | [CheckoutController.cs](../src/SkillLedger.Api/Controllers/CheckoutController.cs) (17 KB) |
| [StripeCheckoutService.cs.disabled](../src/SkillLedger.Infrastructure/Services/StripeCheckoutService.cs.disabled) (18 KB) | [StripeCheckoutService.cs](../src/SkillLedger.Infrastructure/Services/StripeCheckoutService.cs) (24 KB) |
| [StripeWebhookService.cs.disabled](../src/SkillLedger.Infrastructure/Services/StripeWebhookService.cs.disabled) (27 KB) | [StripeWebhookService.cs](../src/SkillLedger.Infrastructure/Services/StripeWebhookService.cs) (58 KB) |
| [StripePaymentService.cs.disabled](../src/SkillLedger.Infrastructure/Services/StripePaymentService.cs.disabled) (27 KB) | none |

The live versions are the wired ones: [Program.cs:432-433](../src/SkillLedger.Api/Program.cs#L432) registers `StripeCheckoutService` and `StripeWebhookService`. `IPaymentService` resolves to `PaymentService`, not to the orphaned `StripePaymentService` ([Program.cs:419](../src/SkillLedger.Api/Program.cs#L419)), so 27 KB implementing `IPaymentService` sat in the tree with no live counterpart, no registration, and no compiler ever looking at it.

**Why it is interesting.** The dead copies are not stale duplicates of the live ones; they are earlier forks that diverged. Renaming a file instead of deleting it means the next person to read `StripeWebhookService` finds two, both plausible, and only a `Program.cs` lookup tells them which one the money went through.

The live webhook service had three open defects at shutdown, all recorded as `FOUND - Not Fixed`: audit logs missing for five event types including refunds and subscription creation (BACKEND-BUG-002, [FOUND_BUGS.md:322](../web/FOUND_BUGS.md#L322)); a `NullReferenceException` inside Stripe's own deserializer when `data.object` is null, with no guard before parsing (BACKEND-BUG-003, [line 347](../web/FOUND_BUGS.md#L347)); and `PaymentIntentFailed` never moving a subscription to `PastDue`, so failed payments kept their access (BACKEND-BUG-004, [line 375](../web/FOUND_BUGS.md#L375)).

---

## 9. Redis was registered as literal null, and the lock quietly stopped being distributed

Three branches in [Program.cs](../src/SkillLedger.Api/Program.cs#L642) do the same thing when Redis is absent, unreachable, or the environment is `Testing`:

```csharp
// Register a null IConnectionMultiplexer to signal Redis is unavailable
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => null!);
```

([lines 642](../src/SkillLedger.Api/Program.cs#L642), [653](../src/SkillLedger.Api/Program.cs#L653), [663](../src/SkillLedger.Api/Program.cs#L663); a fourth `return null!` sits inside the connect-failure catch at [line 610](../src/SkillLedger.Api/Program.cs#L610).) `ICacheService` and `IDistributedLockService` are then registered unconditionally on top ([lines 674](../src/SkillLedger.Api/Program.cs#L674) and [679](../src/SkillLedger.Api/Program.cs#L679)).

[BUG_REPORT.md CRIT-003](../docs/BUG_REPORT.md#L47) predicted a `NullReferenceException` at runtime. That prediction is wrong, and the truth is worse. `DistributedLockService` declares its dependency as nullable and null-guards every use: `IConnectionMultiplexer? redis` at [DistributedLockService.cs:24](../src/SkillLedger.Infrastructure/Services/DistributedLockService.cs#L24), then `if (_redis?.IsConnected == true)` before each Redis path ([lines 73](../src/SkillLedger.Infrastructure/Services/DistributedLockService.cs#L73) and [115](../src/SkillLedger.Infrastructure/Services/DistributedLockService.cs#L115)). So nothing throws. It falls through to a `static Dictionary<string, LocalLock>` guarded by a `static SemaphoreSlim` ([lines 16-17](../src/SkillLedger.Infrastructure/Services/DistributedLockService.cs#L16)).

That is a process-local lock wearing the name `IDistributedLockService`. It is correct on one instance and worthless on two, and the transition between those states is a config value with no log line above `Information`. Every escrow release and credit transfer takes this lock before opening its transaction ([ProjectEscrowService.cs:390](../src/SkillLedger.Infrastructure/Services/ProjectEscrowService.cs#L390), [CreditTransferService.cs:121](../src/SkillLedger.Infrastructure/Services/CreditTransferService.cs#L121)), which is exactly the mutual exclusion that CRIT-005, the escrow double-release, depended on.

**Fix.** Partial. CRIT-004's deadlock risk was addressed with a semaphore timeout ([line 21](../src/SkillLedger.Infrastructure/Services/DistributedLockService.cs#L21)). The null registration was left as-is; `BUG_REPORT.md` filed "Implement proper null-object pattern for Redis" under Medium-Term ([line 451](../docs/BUG_REPORT.md#L451)) and it stayed there.

---

## 10. Twenty-one tests skipped because the harness could not reach the code

The skip reasons are the useful part, because they are honest ([full list](../tests/SkillLedger.Tests)):

- Six in [MessagingHubIntegrationTests.cs:179](../tests/SkillLedger.Tests/Integration/MessagingHubIntegrationTests.cs#L179): "SignalR group notifications do not propagate between in-process HubConnection clients in `WebApplicationFactory` in-memory test server (known ASP.NET Core test infrastructure limitation)". Group fan-out is the entire feature.
- Four in [ProjectControllerIntegrationTests.cs:724](../tests/SkillLedger.Tests/Integration/Api/ProjectControllerIntegrationTests.cs#L724): "RequireModeratorPermission policy not configured in test environment". The authorization policy that gates moderation was never registered in the test host, so the tests for it could not run.
- Three in [AuthenticationServiceTests.cs:80](../tests/SkillLedger.Tests/Core/Services/AuthenticationServiceTests.cs#L80): "`SignInManager.PasswordSignInAsync` requires full ASP.NET Core cookie middleware pipeline; `DefaultHttpContext` in test host cannot emit auth cookies. Verified via E2E tests instead." The two that follow ([146](../tests/SkillLedger.Tests/Core/Services/AuthenticationServiceTests.cs#L146), [167](../tests/SkillLedger.Tests/Core/Services/AuthenticationServiceTests.cs#L167)) are skipped because they depend on the first.
- Two in [ProjectApplicationServiceIntegrationTests.cs:884](../tests/SkillLedger.Tests/Integration/Services/ProjectApplicationServiceIntegrationTests.cs#L884): "`EF.Functions.DateDiffDay` not supported by InMemory provider - service returns empty DTO when this fails". The provider does not merely block the test; it changes what the service returns.
- Four in [FraudDetectionPerformanceTests.cs](../tests/SkillLedger.Tests/Performance/FraudDetectionPerformanceTests.cs#L32), skipped by design for manual profiling, plus one obsolete JWT test and one needing a real Admin role row.

"Verified via E2E tests instead" is the claim to check against entry 7: the production E2E run found sign-out silently broken and unaudited.

**Why it is interesting.** Each skip is individually defensible. Together they name the same root cause as entry 1: a test host that could not reach the parts of the system where the interesting behaviour lived. Fifteen of the twenty-one trace to the in-memory harness. The response was to skip the tests rather than to change the harness, and the coverage numbers in entry 4 were computed over what remained.
