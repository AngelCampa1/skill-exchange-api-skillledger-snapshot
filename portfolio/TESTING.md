# Testing

SkillLedger accumulated roughly 9,650 test cases across three layers and never ran
them on a gate. This page describes what the suite was, what its numbers measure, and
where it fell short. Raw counts and the commands behind them are in
[METRICS.md](./METRICS.md).

---

## Shape of the suite

The xUnit project is one assembly, `tests/SkillLedger.Tests`, holding 217 tracked C#
files and 122,933 lines. It is organized by test kind rather than by the layer under
test.

| Directory | Files | `[Fact` | What lives there |
|---|---:|---:|---|
| `Integration/` | 107 | 3,005 | HTTP-level tests via `WebApplicationFactory`, split into `Api/`, `Services/`, `Financial/` |
| `Core/` | 43 | 716 | Entity, service, and validator tests under `Core/Entities`, `Core/Services`, `Core/Validators` |
| `Unit/` | 15 | 235 | Service and extension-method tests with no host |
| `Security/` | 11 | 121 | Authorization, CSRF, rate limiting, escrow concurrency, SignalR hub access |
| `Performance/` | 4 | 34 | Timing and throughput on file management, fraud detection, messaging, search |
| `Regression/` | 1 | 15 | `BugFixRegressionTests.cs`, one test per fixed defect ID |
| `BDD/` | 1 | 7 | A single scripted user journey for project search |
| `Infrastructure/` | 14 | 3 | Base classes, fixtures, auth helpers, seeders |
| `Mocks/` | 18 | 0 | Fakes for the external boundaries |

`Fixtures/` holds one file, `CustomWebApplicationFactory.cs`. `Tools/DatabaseSeeder/`
is a separate console project the test csproj excludes from compilation
([SkillLedger.Tests.csproj:27](../tests/SkillLedger.Tests/SkillLedger.Tests.csproj#L27)),
so nothing verified it still compiled.

### How integration tests got a database

They did not use a real Postgres, and they did not use SQLite. Both base classes use
the EF Core in-memory provider.

`IntegrationTestBase` boots the real ASP.NET Core host through
`WebApplicationFactory<Program>`, then hands each test class its own logical database
inside a process-wide `InMemoryDatabaseRoot`
([IntegrationTestBase.cs:42](../tests/SkillLedger.Tests/Infrastructure/IntegrationTestBase.cs#L42)).
Isolation comes from a per-instance GUID database name pushed to the server on every
request as an `X-Test-Database` header
([IntegrationTestBase.cs:70](../tests/SkillLedger.Tests/Infrastructure/IntegrationTestBase.cs#L70)),
so the API resolves the same store whichever thread served the call. Authentication is
faked by a `TestAuthenticationHandler` reading `X-Test-UserId`, `X-Test-Roles`, and
`X-Test-Permissions` headers
([IntegrationTestBase.cs:186](../tests/SkillLedger.Tests/Infrastructure/IntegrationTestBase.cs#L186)).

`LightweightIntegrationTestBase` skips the host entirely: a bare `ServiceCollection`,
`UseInMemoryDatabase`
([LightweightIntegrationTestBase.cs:48](../tests/SkillLedger.Tests/Infrastructure/LightweightIntegrationTestBase.cs#L48)),
a mock email service, a memory cache, and nothing else. Its comment says the point was
"to prevent memory exhaustion": the factory-based path was expensive enough that a
second, cheaper base class was worth writing.

The in-memory provider bought tests that ran anywhere with no container, no connection
string, and no migration step. What it cost:

- **It is not a relational database.** Foreign keys, unique constraints, and check
  constraints are ignored. Any bug Postgres would have caught at write time passed.
- **Provider-specific SQL failed quietly.** Two tests are skipped with
  "EF.Functions.DateDiffDay not supported by InMemory provider - service returns empty
  DTO when this fails". The service returns an empty result rather than throwing, and
  that query path was never exercised against the engine that ran in production.
- **Raw SQL cleanup could not work.** `CleanTestDataAsync` issues
  `DELETE FROM AuditLogs WHERE UserId = {userId}` via `ExecuteSqlInterpolatedAsync`
  ([IntegrationTestBase.cs:449](../tests/SkillLedger.Tests/Infrastructure/IntegrationTestBase.cs#L449)).
  The provider cannot execute raw SQL, so it throws every time and falls through to the
  `catch` at line 424 and the entity-by-entity `FullCleanDatabase`. The intended fast
  path was dead code.
- **Concurrency behavior was fiction.** The escrow concurrency tests under `Security/`
  ran against a store with no row locking and no isolation levels.

The migrations were Postgres (`20260217200308_InitialPostgresCreate`), so tests ran
against the schema EF derived from the model at runtime, not the one the migrations
produced. Drift between the two was undetectable from the backend suite.

### Run configuration

`test.runsettings` is serial: `MaxCpuCount` 1, `DisableParallelization` true,
`MaxParallelThreads` 1, `TestSessionTimeout` 600000 ms. `xunit.runner.json` says the
opposite (`parallelizeTestCollections: true`, `maxParallelThreads: 4`), so which
applied depended on whether the runner got `--settings test.runsettings`. The 10-minute
timeout was tight: the January coverage report records that "Full suite (1,809 tests)
crashes after 15 minutes".

### The trait attributes do not work

`tests/SkillLedger.Tests/Infrastructure/TestCategories.cs` defines filtering attributes:
`[FastTest]`, `[IntegrationTest]`, `[SecurityTest]`, `[ApiTest]`, `[FinancialTest]`,
and about fifteen more, all deriving from `TestTraitAttribute`, which is declared as a
plain `Attribute`
([TestCategories.cs:87](../tests/SkillLedger.Tests/Infrastructure/TestCategories.cs#L87)).
It does not implement xUnit's `ITraitAttribute` and carries no `[TraitDiscoverer]`.
Neither identifier appears anywhere in the repository. xUnit therefore never converted
any of them into a trait, and `dotnet test --filter Category=Fast` matched nothing.

This has a consequence. The test csproj sets
`<VSTestCaseFilter>Category!=EndToEnd&Category!=Performance</VSTestCaseFilter>`
([SkillLedger.Tests.csproj:15](../tests/SkillLedger.Tests/SkillLedger.Tests.csproj#L15))
with the comment "Exclude resource-intensive tests by default". Only 15 real
`[Trait("...", "...")]` attributes exist in the whole test project, and none of them
carries a `Category` of `EndToEnd` or `Performance`. The filter excluded nothing, so
the 34 performance tests it was meant to skip ran with every other test.
`scripts/run-tests.ps1` worked around this by matching method names instead
(`"Fast" { "FullyQualifiedName~FastTest" }`).

---

## The frontend suite

Jest with React Testing Library, configured through `next/jest`
([web/jest.config.js](../web/jest.config.js)). 174 test files hold 5,150 `it(` and
`test(` callbacks, more cases than the backend, from 510 TypeScript files.
`collectCoverageFrom` is set but there is no `coverageThreshold` block, so nothing
enforced a floor despite `CLAUDE.md` requiring "95% code coverage minimum on every
file you touch". No frontend coverage report is tracked, so the figure is unknown.

Playwright covers end-to-end journeys. Seven `.spec.ts` files under `web/tests/e2e/`
hold 10 `test(` calls in 7 `test.describe(` blocks. Six are numbered journeys under
`journeys/` (client, provider, marketplace discovery, credit wallet, workspace
collaboration, CRM feedback widget), carrying 1 to 2 tests each, nine in total. The
seventh, `debug-registration.spec.ts`, sits outside `testDir`
([playwright.config.ts:7](../web/playwright.config.ts#L7)) and never ran.

The default `baseURL` is `process.env.BASE_URL || 'http://localhost:3030'`
([playwright.config.ts:24](../web/playwright.config.ts#L24)), a local dev port, with
no staging or production default. Seven browser projects are declared (Chromium,
Firefox, WebKit, Mobile Chrome, Mobile Safari, Edge, Chrome), so a full run was 63
executions, and `webServer` starts `dotnet run` for the API on port 8030 alongside
`npm run dev`, each with a 180-second startup timeout.

Nine end-to-end tests guarded a marketplace with a credit ledger, escrow, milestones,
and Stripe billing. That was the thinnest layer of the three and the only one that
exercised a real database.

---

## What the numbers do not mean

Two coverage documents sit in the repository root. Both are dated **January 12,
2026**. They contradict each other.

`OVERALL_BACKEND_COVERAGE_REPORT.md` is marked "MEASURED (not estimated)" and reports
merged coverage from 25 runs:

> **17.8% Line Coverage** (Measured)
> Lines Covered: 25,192 of 141,317 (17.8%)

By assembly: `SkillLedger.Api` 3.2%, `SkillLedger.Core` 59.4%,
`SkillLedger.Infrastructure` 17.3%. It lists AuthController, ProjectController,
PaymentController, MessagingController and "All other controllers" at 0%, states "No
E2E Tests: No tests that exercise full request → controller → service → database →
response flow", and records that the prior estimate had been "~73-78% overall".

`COVERAGE_STATUS_SUMMARY.md`, same date, is headed "PRIMARY OBJECTIVES ACHIEVED" and
ends with "Overall Status: MISSION ACCOMPLISHED". Its per-service table disagrees with
the measured report on every shared row:

| Service | `OVERALL_BACKEND_COVERAGE_REPORT.md` | `COVERAGE_STATUS_SUMMARY.md` |
|---|---:|---:|
| PaymentService | 85.4% | 100% |
| CreditTransferService | 98.2% | 100% |
| ProjectEscrowService | 88.5% | 100% |
| BadgeSecurityService | 84.6% | 100% |
| MilestoneTrackingService | 87.7% | 98.33% |
| ProjectApplicationService | 84.0% | 99.20% |
| AuditLogService | 87.8% | 94.52% |
| SubscriptionService | 95.9% | 96.66% |
| FinancialExportService | 80.9% | 82.1% |

The gaps run one direction. Every figure in the summary is equal to or higher than the
measured one, and four services are claimed at exactly 100%. Its headline claim, "ALL
TARGETS MET", fails on its own terms against the measured file: BadgeSecurityService
at 84.6% is below the 85% security target it is listed as meeting. The summary does
concede the point in a footnote it then ignores: under "Secondary Objectives" it
marks "Overall Backend Coverage / 80%" as "Requires full measurement", on the same day
the other file measured it at 17.8%.

Three further reasons not to lean on either number:

**They are stale.** Both are dated 2026-01-12; the last source commit was 2026-07-08.
Roughly six months of development landed after they were written and neither was
updated. Nothing in the tree re-measures coverage after January.

**The artifacts are gone.** `OVERALL_BACKEND_COVERAGE_REPORT.md` points readers at
`TestResults/CoverageReport/index.html`, `TestResults/CoverageReport/Summary.txt`, and
`TestResults/actual-backend-coverage.xml`. No `TestResults/` directory exists here.
Both files cite `COVERAGE_IMPROVEMENT_PLAN.md`; the summary also cites `BUGS_FOUND.md`
and a root-level `COVERAGE_ANALYSIS.md`. None are tracked. The claims cannot be
checked against their sources.

**The test count does not match.** The measured report cites "1,735 Passing Tests" in
a suite of "1,809 tests". This tree holds 4,136 `[Fact` and 88 `[Theory`. Whatever was
measured in January covered less than half of what eventually existed.

The defensible reading: financial and security services under
`Integration/Services/` were genuinely well tested, the 36 controllers were close to
untested, and the exact percentages are unknown as of the final commit.

---

## Skips, parked files, and open defects

### 21 hard-skipped backend tests

Grouped by the reason string in the `Skip` argument:

| Count | Reason (quoted) |
|---:|---|
| 6 | "SignalR group notifications do not propagate between in-process HubConnection clients in WebApplicationFactory in-memory test server (known ASP.NET Core test infrastructure limitation)" |
| 4 | "RequireModeratorPermission policy not configured in test environment" |
| 4 | "High concurrency / High volume / Large dataset / stress test - run manually for performance profiling" |
| 3 | "SignInManager.PasswordSignInAsync requires full ASP.NET Core cookie middleware pipeline; DefaultHttpContext in test host cannot emit auth cookies. Verified via E2E tests instead." (1 root, 2 dependents) |
| 2 | "EF.Functions.DateDiffDay not supported by InMemory provider - service returns empty DTO when this fails" |
| 1 | "Requires Admin role in database - role-based authorization checks database, not just claims" |
| 1 | "Obsolete - JWT Bearer token authentication removed in favor of cookie authentication" |

Most trace to the test host rather than to product bugs: the SignalR six, the
DateDiffDay two, the cookie-pipeline three. The cookie-pipeline skip says login was
"Verified via E2E tests instead", and that verification is one of the nine Playwright
tests, which had no CI to run it. The four moderator-permission skips are different in
kind: the authorization policy was never registered in the test environment, so the
moderator path had no automated coverage at all.

### 2 skipped frontend tests

- `web/src/app/projects/[id]/__tests__/page.integration.test.tsx:639`:
  `test.skip('handles application submission error', ...)`
- `web/src/components/__tests__/ProjectSearchForm.test.tsx:759`:
  `it.skip('handles successful geolocation request', ...)`

Neither carries a reason.

### 8 files parked with a suffix

Renaming a file to `.disabled` removes it from the build without deleting it. Four are
production code, four are tests, and they cluster on two features:

```text
src/SkillLedger.Api/Controllers/CheckoutController.cs.disabled
src/SkillLedger.Infrastructure/Services/StripeCheckoutService.cs.disabled
src/SkillLedger.Infrastructure/Services/StripePaymentService.cs.disabled
src/SkillLedger.Infrastructure/Services/StripeWebhookService.cs.disabled
tests/SkillLedger.Tests/Integration/FinancialReportingApiIntegrationTests.cs.disabled
tests/SkillLedger.Tests/Integration/Services/FinancialReportingServiceIntegrationTests.cs.broken
tests/SkillLedger.Tests/Unit/FinancialExportServiceTests.cs.disabled
tests/SkillLedger.Tests/Mocks/MockFinancialExportService.cs.wip
```

The Stripe checkout, payment, and webhook services were switched off in production
code, yet both coverage documents still report `StripeWebhookService` (83.33%) and
`FinancialExportService` (82.1% / 80.9%) as live, tested services. The 54 `[Fact`
attributes in the three disabled test files are the difference between the 4,190
`[Fact` in the tree and the 4,136 that could compile.

### Open defects in `web/FOUND_BUGS.md`

Ten entries carry the status `FOUND - Not Fixed`:

| ID | Title | Status line |
|---|---|---:|
| `BACKEND-BUG-002` | StripeWebhookService Audit Logging Not Working for Multiple Event Types | [323](../web/FOUND_BUGS.md#L323) |
| `BACKEND-BUG-003` | StripeWebhookService Null Data Object Causes NullReferenceException | [348](../web/FOUND_BUGS.md#L348) |
| `BACKEND-BUG-004` | PaymentIntentFailed Handler Not Updating Subscription Status | [376](../web/FOUND_BUGS.md#L376) |
| `BUG-WEEK18-001` | Second 401 After Refresh Doesn't Redirect to Login | [401](../web/FOUND_BUGS.md#L401) |
| `BUG-TEST-001` | CSRF Token Fetch Failure Silently Ignored | [435](../web/FOUND_BUGS.md#L435) |
| `BUG-TEST-027` | No CSRF Protection on Login Endpoint (marked SECURITY) | [649](../web/FOUND_BUGS.md#L649) |
| `BUG-TEST-030` | Draft Corruption - No Multi-Tab Protection (Race Condition) | [524](../web/FOUND_BUGS.md#L524) |
| `BUG-TEST-029` | Auto-Save Doesn't Respect Idle Timeout | [566](../web/FOUND_BUGS.md#L566) |
| `BUG-TEST-025` | MessageList Crashes When messages Prop is Undefined | [614](../web/FOUND_BUGS.md#L614) |
| `BUG-TEST-031` | Can Skip to Step 3 Without Completing Previous Steps | [680](../web/FOUND_BUGS.md#L680) |

Beyond those ten, the file records 25 more at plain `FOUND`, one at
`FOUND - BUG-FE-003/BUG-CRIT-009 NOT FIXED`, one `OPEN`, and one `NEW`, against six
marked `FIXED`. Two of the open items are security defects on the login path,
`BUG-TEST-027` and `BUG-TEST-001`, and `Security/CsrfProtectionTests.cs` existed in
the backend suite the whole time.

---

## No CI

There is no continuous integration in this repository.

```bash
git ls-files '.github/*'    # returns nothing
```

`docs/DEPLOYMENT_GUIDE.md` describes a pipeline at
`.github/workflows/azure-deployment.yml` in two places
([line 138](../docs/DEPLOYMENT_GUIDE.md#L138) and
[line 286](../docs/DEPLOYMENT_GUIDE.md#L286)). That file is not here, and neither is
any other workflow. Whether it was removed during sanitization or earlier in the
product's life cannot be settled from this snapshot; the documentation still describes
it as present, which is itself the tell.

The only enforcement recorded anywhere is a local pre-commit hook,
`.githooks/pre-commit`, which `CLAUDE.md` says runs `dotnet build` plus
`dotnet test --filter "Category!=E2E"` on backend changes. A local hook is opt-in: it
runs on the developer's machine, skips with `--no-verify`, and needs `core.hooksPath`
configured. Its `Category!=E2E` filter has the csproj filter's problem: no test carries
that trait, because the trait attributes never worked.

So roughly 9,650 test cases (4,494 backend, 5,150 Jest, 9 Playwright) had no
automated gate. Nothing outside a developer's laptop ran them, nothing recorded whether
they passed, and nothing blocked a merge when they did not. That changes how every
number above reads:

- They count test *code*, not passing tests. A `[Fact]` failing for four months still
  counts as one.
- The 17.8% figure was a point measurement, not a tracked series: no trend, no
  baseline, no way to tell whether coverage rose or fell over the next six months.
- The 21 skips, 8 parked files, and 131 unaddressed warnings accumulated because
  nothing objected. Skipping a test and renaming a file to `.disabled` are the cheapest
  ways to turn a red build green, and both were used.

The build is the one result verified in this tree, and the one worth trusting:
`dotnet build SkillLedger.sln -c Release` gave 0 errors and 131 warnings in 38 seconds
on .NET SDK 9.0.303.

### The test run, attempted

`dotnet test SkillLedger.sln -c Release --no-build` on .NET SDK 9.0.303, Windows 11, with no
database and no network. It did not finish.

| | |
|---|---:|
| Reported before the run stopped | 1,743 |
| Passed | 1,697 |
| Failed | 39 |
| Skipped | 7 |
| Elapsed at the last test event | 17 m 35 s |

The last test event is logged at `00:17:35`. After that the process printed nothing for a further
87 minutes while `testhost.exe`'s working set grew past 5 GB, and it was still in that state when I
terminated it. The `Test host process crashed` / `Test Run Aborted` line at the end of the log is
that termination, not a fault the runner detected on its own. Nothing here diagnoses why it stalled;
the observation is only that a full run did not complete on this machine.

1,743 is well under half the suite. The source carries 4,136 `[Fact]`, 88 `[Theory]` and 358
`[InlineData]` rows, and the runner never printed a discovered total because it was aborted. The
skip count is the same story: 7 reported against 21 `Skip =` attributes in the source, because the
run stopped before reaching the rest.

The 39 failures are not spread evenly. They cluster in integration tests that go through the API
surface:

| Cluster | Failures |
|---|---:|
| `BadgeApiIntegrationTests` | 13 |
| `AntiGamingApiIntegrationTests` | 8 |
| `ExperienceApiIntegrationTests` | 7 |
| `Integration/Api/*ControllerTests` (AntiGaming, Monitoring) | 6 |
| `Core/Services/SubscriptionServiceTests` | 3 |
| `AuthenticationIntegrationTests` (both logout paths) | 2 |

I have not investigated any of them. What the number establishes is narrow and worth stating
plainly: at the final commit, on a clean checkout, the backend suite does not run green and does
not run to completion. Since no CI ever executed it, nothing in the project's lifetime would have
surfaced that.

---

Back to [README](../README.md) · Related: [METRICS.md](./METRICS.md) ·
[ARCHITECTURE.md](./ARCHITECTURE.md) · [CREDIT-LEDGER.md](./CREDIT-LEDGER.md) ·
[ENGINEERING-LOG.md](./ENGINEERING-LOG.md)
