# Metrics

Every number that appears in this repository's documentation is listed here with the
command that produced it. All commands were run from the repository root of this
snapshot on 2026-08-13, in Git Bash on Windows.

Two caveats. This snapshot is a single commit, so `git log` describes the snapshot and
not the product; source-repo history comes from `docs/source-history.json`. And counts
of `[Fact]`, `it(`, and similar count *source text*, not tests the runner discovered.

---

## Source size

| Measure | Value |
|---|---:|
| Tracked files | 1,407 |
| Tracked `.md` files | 154 |
| `src/` C# files | 380 |
| `src/` C# lines | 115,872 |
| `tests/` C# files | 217 |
| `tests/` C# lines | 122,933 |
| `web/` `.ts` + `.tsx` files | 510 |
| `web/` `.ts` + `.tsx` lines | 173,214 |
| Tracked files under `web/content/` | 52 |
| Tracked `.disabled` / `.broken` / `.wip` files | 8 |
| `src/SkillLedger.Api/Program.cs` lines | 1,037 |

Test C# outweighs production C# by about 7,000 lines. That is a fact about the tree,
not evidence the code was well tested: see
[TESTING.md](./TESTING.md#what-the-numbers-do-not-mean).

```bash
# lines() sums a pathspec; quote the pathspec so the shell does not glob it first
lines() { git ls-files "$@" | xargs wc -l | grep -E 'total$' | awk '{s+=$1} END {print s}'; }

git ls-files 'src/*.cs'   | wc -l ; lines 'src/*.cs'              # 380  115,872
git ls-files 'tests/*.cs' | wc -l ; lines 'tests/*.cs'            # 217  122,933
git ls-files 'web/*.ts' 'web/*.tsx' | wc -l                       # 510
lines 'web/*.ts' 'web/*.tsx'                                      #      173,214

git ls-files | wc -l                                # 1,407
git ls-files '*.md' | wc -l                         # 154
git ls-files 'web/content/*' | wc -l                # 52
git ls-files | grep -cE '\.(disabled|broken|wip)$'  # 8
wc -l < src/SkillLedger.Api/Program.cs              # 1,037
```

Note on the glob: git pathspecs are not shell globs. `'web/*.ts'` matches recursively
because `*` crosses `/`, whereas `'web/**/*.ts'` returns 507: the literal `**/`
requires an intermediate directory and skips the three `.ts` files directly in `web/`.

---

## Tests

| Measure | Value |
|---|---:|
| `[Fact` / `[Theory` / `[InlineData` in compiled `tests/**/*.cs` | 4,136 / 88 / 358 |
| `[MemberData` / `[ClassData` | 0 |
| Hard-skipped xUnit tests (`[Fact(Skip=` / `[Theory(Skip=`) | 21 |
| Backend cases implied by the above (4,136 + 358) | 4,494 |
| Jest test files, and `it(` / `test(` callbacks in them | 174 / 5,150 |
| Frontend `it.skip(` / `test.skip(` | 2 |
| Playwright spec files under `web/tests/e2e/` (of which inside `testDir`) | 7 (6) |
| Playwright `test(` calls, all specs (of which inside `testDir`) | 10 (9) |
| Playwright `test.describe(` blocks | 7 |
| Implied total across all three layers | ~9,653 |

`[Fact` across *all* tracked files under `tests/` is 4,190. The extra 54 sit in the
three `.disabled` / `.broken` files, which the compiler never sees. 4,136 is the
number that could run.

The Playwright config sets `testDir: './tests/e2e/journeys'`
([playwright.config.ts:7](../web/playwright.config.ts#L7)), so
`web/tests/e2e/debug-registration.spec.ts` and its single `test(` were outside the
run: nine tests, not ten. Nesting adds nothing: the 10 total equals the sum of the
per-file counts, and the 7 `test.describe(` blocks are grouping wrappers.

```bash
for p in '\[Fact' '\[Theory' '\[InlineData' '\[MemberData|\[ClassData' \
         '\[(Fact|Theory)\(Skip'; do
  git grep -oE "$p" -- 'tests/*.cs' | wc -l          # 4136 88 358 0 21
done

git ls-files 'web/*.test.ts' 'web/*.test.tsx' | wc -l                       # 174
git grep -oE '\b(it|test)\(' -- 'web/*.test.ts' 'web/*.test.tsx' | wc -l    # 5150
git grep -nE '\b(it|test|describe)\.skip\(' -- web/ | wc -l                 # 2

E=web/tests/e2e; J=$E/journeys
git ls-files "$E/*.spec.ts" | wc -l ; git ls-files "$J/*.spec.ts" | wc -l   # 7  6
git grep -oE '(^|[^.a-zA-Z])test\(' -- "$E/*" | wc -l                       # 10
git grep -oE '(^|[^.a-zA-Z])test\(' -- "$J/*" | wc -l                       # 9
git grep -oE 'test\.describe\('     -- "$E/*" | wc -l                       # 7
```

The per-directory breakdown of those 4,136 Facts and 88 Theories is in
[TESTING.md](./TESTING.md#shape-of-the-suite). It sums to 214 `.cs` files rather than
217 because the three excluded `.disabled` / `.broken` files are not `.cs`. Swap the
directory name into the same three commands to reproduce any row:

```bash
D=tests/SkillLedger.Tests/Integration
git ls-files "$D/*.cs" | wc -l
git grep -o '\[Fact'   -- "$D/*.cs" | wc -l
git grep -o '\[Theory' -- "$D/*.cs" | wc -l
```

---

## Components

| Measure | Value |
|---|---:|
| API controllers | 36 |
| Infrastructure services | 70 |
| Core interfaces | 52 |
| Core entities | 62 |
| `builder.Services.Add*` registrations in `Program.cs` | 93 |

93 DI registrations in a single 1,037-line composition root, ending at
[Program.cs:1037](../src/SkillLedger.Api/Program.cs#L1037). See
[ARCHITECTURE.md](./ARCHITECTURE.md) for what those layers were meant to do.

```bash
for d in Api/Controllers Infrastructure/Services Core/Interfaces Core/Entities; do
  git ls-files "src/SkillLedger.${d%%/*}/${d#*/}/*.cs" | wc -l
done
git grep -oE 'builder\.Services\.Add[A-Za-z]*' -- src/SkillLedger.Api/Program.cs | wc -l
```

---

## Data model

| Measure | Value |
|---|---:|
| `DbSet<` in `SkillLedgerDbContext.cs` | 64 |
| EF Core migrations | 3 |
| Latest migration | `20260525001000_AddPrivacyRequests` |

The three are `20260217200308_InitialPostgresCreate`,
`20260525000000_AddProcessedStripeWebhookEvents`, and
`20260525001000_AddPrivacyRequests`. The directory also holds a `.Designer.cs` and
`SkillLedgerDbContextModelSnapshot.cs`, generated companions rather than migrations.
Ledger schema detail is in [CREDIT-LEDGER.md](./CREDIT-LEDGER.md).

```bash
git grep -c 'DbSet<' -- src/SkillLedger.Infrastructure/Data/SkillLedgerDbContext.cs
git ls-files 'src/SkillLedger.Infrastructure/Migrations/*.cs' \
  | grep -vE '\.Designer\.cs$|ModelSnapshot\.cs$'
```

---

## History

This snapshot has one commit. The figures below come from
[`docs/source-history.json`](../docs/source-history.json), exported from the private
source repository at commit `7612134` on 2026-08-13. They cannot be re-derived here.

| Measure | Value |
|---|---:|
| Commits | 944 |
| First commit | 2025-08-29 |
| Last commit | 2026-07-08 |
| Commits, VentoraLabs | 751 |
| Commits, Angel Campa | 124 |
| Commits, Claude Code Assistant | 69 |

```bash
cat docs/source-history.json
```

Narrative for that period is in [ENGINEERING-LOG.md](./ENGINEERING-LOG.md).

---

## Build and test run

Verified in this tree on .NET SDK 9.0.303: **0 errors, 131 warnings, 38 seconds**.

```bash
dotnet --version          # 9.0.303
dotnet build SkillLedger.sln -c Release
```

131 warnings on a clean build is the honest headline. Nothing forced them down and
nothing failed on them, because there was no automated gate: see
[TESTING.md](./TESTING.md#no-ci).

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

```bash
dotnet test SkillLedger.sln -c Release --settings test.runsettings
```

`test.runsettings` pins `MaxCpuCount` to 1 and disables parallelization, so that run
is serial by design.

---

Back to [README](../README.md) · Related: [TESTING.md](./TESTING.md) ·
[ARCHITECTURE.md](./ARCHITECTURE.md) · [CREDIT-LEDGER.md](./CREDIT-LEDGER.md) ·
[ENGINEERING-LOG.md](./ENGINEERING-LOG.md)
