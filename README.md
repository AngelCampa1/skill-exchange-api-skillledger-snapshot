# SkillLedger

A marketplace where professionals traded work for work instead of money. You posted a project, took
someone else's, and settled in an internal credit. You got 100 to start, earned more by delivering,
and spent them by receiving. Escrow held the credit until milestones cleared, reviews fed a
reputation score, and a fraud service watched for people trading with themselves.

It ran in production at `skillledger.app` from March 2026. It is shut down. The hosted services,
the domain, and the database are gone, so everything below is past tense.

The reason to read this repository is the ledger underneath it: an internal points balance that was
built like a bank account, and the two constants that made the whole apparatus decorative.

> [!IMPORTANT]
> **Status: shut down.** 944 commits from 2025-08-29 to 2026-07-08, ending with one titled
> *"consolidate outstanding local work (wind-down backup)"*.

> [!NOTE]
> Built by Angel Campa: [github.com/AngelCampa1](https://github.com/AngelCampa1). Source
> available, all rights reserved: no license to use, copy, modify, or redistribute is granted. See
> [License](#license).

![Diagram of one credit transfer: a Serializable database transaction decrypts eight wallet fields, checks the balance in C# rather than SQL, signs the transaction row, and re-encrypts. Two keys feed it. Both resolve to hashes of string constants held in this repository, because the production configuration sets UseKeyVault, a setting no code reads, while the Enabled flag the code does read stays false.](./portfolio/screenshots/credit-path.svg)

*There are no product screenshots in this repository, and I am not going to fake one. The only
images the source ever carried were logos and favicons: this is an API-first product with a
frontend nobody outside three test accounts ever saw running. This diagram is drawn from
[`CreditWalletService.cs`](./src/SkillLedger.Infrastructure/Services/CreditWalletService.cs),
[`AzureKeyVaultService.cs`](./src/SkillLedger.Infrastructure/Services/AzureKeyVaultService.cs) and
the two `appsettings` files it names; every claim in it is checkable from the tree. Two more
mechanism diagrams (the escrow state machine and the domain-entity graph) are in
[CREDIT-LEDGER.md](./portfolio/CREDIT-LEDGER.md) and
[ARCHITECTURE.md](./portfolio/ARCHITECTURE.md).*

---

## Contents

- [If you read one thing](#if-you-read-one-thing)
- [What it did](#what-it-did)
- [Architecture](#architecture)
- [What is worth your time here](#what-is-worth-your-time-here)
- [By the numbers](#by-the-numbers)
- [Testing](#testing)
- [Repository map](#repository-map)
- [Documentation](#documentation)
- [About this snapshot](#about-this-snapshot)
- [Built with AI agents](#built-with-ai-agents)
- [Running it locally](#running-it-locally)
- [Who built this](#who-built-this)
- [License](#license)

---

## If you read one thing

Wallet balances were stored as AES-256-GCM ciphertext. Every row in the transaction log was signed
with HMAC-SHA256 over its own fields. Every money path opened at `IsolationLevel.Serializable`.
There was a `KeyIdentifier` column on the wallet table for nothing but key rotation.

`appsettings.Production.json` turned the key vault on like this:

```json
"AzureKeyVault": { "UseKeyVault": true }
```

`UseKeyVault` appears in three configuration files and in no C# file in this repository. The
property the code actually reads is `Enabled`, which base `appsettings.json` sets to `false` and
production never overrides. So
[`GetDataEncryptionKeyAsync`](./src/SkillLedger.Infrastructure/Services/AzureKeyVaultService.cs#L68)
took its development branch in production and returned
`SHA256("SkillLedger-Test-DEK-Seed-For-Consistent-Encryption")`, a string you are reading right
now. Its integrity counterpart,
[`GetTransactionHashKeyAsync`](./src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L1798),
never had a key vault path at all; it returns the UTF-8 bytes of
`"SkillLedger-TransactionHash-Key-2024"`.

Both keys failed open, together, silently. Nothing asserted at startup that Production had a
reachable vault. `RotateEncryptionKeysAsync`, the method the `KeyIdentifier` column exists to
serve, logs *"not yet implemented"* and
[returns `true`](./src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L1563).

That is the thesis of this codebase in one method: a correct construction, wired to a constant, in
a system that reported success either way.

→ [CREDIT-LEDGER.md](./portfolio/CREDIT-LEDGER.md) takes this apart properly, including what the
design got right.

---

## What it did

Five domains, in the order a user met them:

| | |
|---|---|
| **Identity** | Registration with email and phone verification, professional profiles, skills with endorsements, work history |
| **Marketplace** | Structured project creation, faceted search with saved searches, applications with attachments, provider selection via questionnaires |
| **Credit economy** | Encrypted wallets, signed transactions, project escrow with milestone release, transfers, financial reporting and export |
| **Workspace** | Per-project workspace, SignalR messaging with reactions and typing indicators, deliverable submission, document sharing with access control |
| **Reputation** | Post-project reviews, category-scoped reputation scores, trust badges with earning history, and an anti-gaming service using device fingerprints and a graph store to detect reciprocal-review rings |

Stripe handled subscription tiers on top, and exists twice: once live, once as four `.disabled`
files. [ENGINEERING-LOG.md](./portfolio/ENGINEERING-LOG.md) sorts out which was wired in.

It was deployed. `web/wrangler.jsonc` names the Worker and pins custom-domain routes for
`skillledger.app` with `workers_dev: false`. `appsettings.Production.json` carries
`"Stripe": { "IsEnabled": true, "IsTestMode": false }`. A production E2E run recorded in
[`web/FOUND_BUGS.md`](./web/FOUND_BUGS.md) on 2026-03-18 hit the live site and found thirteen bugs,
the first of which was every dynamic route returning 404, which broke roughly 1,200 sitemap pages.
The same run filed a bug against the subscription page for a marketing claim the database did not
support, which is the sort of thing a harness catches and a person re-reading their own copy does
not.

It was wound down rather than abandoned. Per
[`docs/source-history.json`](./docs/source-history.json), commit volume ran 277 and 287 in November
and December 2025, 99 in March 2026 during the production push, then 15, 15, and 1. The final
commit's subject is recorded in the same file: *"chore: consolidate outstanding local work
(wind-down backup)"*. Nothing at HEAD is half-written or mid-refactor.

Both bug trackers are the record of a sweep run against this code rather than a defect rate.
`docs/BUG_REPORT.md` holds the 78 defects the repository-wide sweep turned up, `web/FOUND_BUGS.md`
the 13 from the production run, and at least one of those 13 is demonstrably fixed at HEAD.

---

## Architecture

Three .NET projects plus a Next.js frontend, over Postgres. The full directory layout is in
[Repository map](#repository-map).

`Program.cs` is the composition root and every registration is inline. There is no
`AddInfrastructure()` extension. The layering is nominally clean but `SkillLedger.Core` carries EF
Core data annotations and inherits from ASP.NET Identity, so the domain project depends on both
frameworks. [ARCHITECTURE.md](./portfolio/ARCHITECTURE.md) walks through where it holds and where
it leaks, including a domain-entity diagram, rather than describing the diagram it was supposed
to be.

There are only three EF migrations for a 70-table schema because the project moved from SQL Server
to Postgres in February 2026 and collapsed everything before that into one `InitialPostgresCreate`.
The schema's history prior to that date is not recoverable from this tree.

---

## What is worth your time here

- **The only non-TypeScript stack in this portfolio.** .NET 9, EF Core, ASP.NET Core Identity,
  xUnit, Docker. 380 C# source files and 217 test files, on Postgres. → [ARCHITECTURE.md](./portfolio/ARCHITECTURE.md)
- **An encrypted ledger and the price of one column-type decision.** A balance the database cannot
  read is a balance you cannot sum, index, or constrain. That single choice reshaped the reporting
  layer, removed any `balance >= 0` check, and put overdraft protection entirely in C#.
  → [CREDIT-LEDGER.md](./portfolio/CREDIT-LEDGER.md)
- **A real concurrency bug, fixed everywhere and tested nowhere.** `BUG-HIGH-010` was an escrow
  double-release: two requests both pass `CanBeReleased`, both release. The fix moved fourteen call
  sites to `Serializable` rather than just the one where it was seen. It is also guarded by an
  in-memory-provider check that switches it off under exactly the conditions the tests run in.
  → [ENGINEERING-LOG.md](./portfolio/ENGINEERING-LOG.md)
- **A test suite larger than the product it tests, that does not run green.** 122,933 lines of test
  C# against 115,872 lines of source. Measured line coverage was 17.8% overall and 3.2% for the API
  project, with most controllers at zero, and two coverage reports written the same day disagree
  with each other. Running it here got 1,697 passing and 39 failing before it stalled at 17 minutes
  and never finished. No CI ever ran any of it. → [TESTING.md](./portfolio/TESTING.md)
- **Thirteen research documents on IRS barter law, and a backend that implements none of it.** The
  marketing site sold automatic fair-market-value tracking and 1099-B reporting as a competitive
  differentiator. `git grep -i barter -- src/` returns nothing. → [DOMAIN.md](./portfolio/DOMAIN.md)
- **Every number on this page, with the command that produced it.** → [METRICS.md](./portfolio/METRICS.md)

---

## By the numbers

| | |
|---|---|
| Application source | 115,872 lines across 380 C# files |
| Tests | 122,933 lines across 217 C# files, more test code than product code |
| Frontend | 173,214 lines across 510 TypeScript and TSX files |
| Test cases | 4,136 `[Fact]` · 88 `[Theory]` · 358 `[InlineData]` · 5,150 Jest callbacks · 10 Playwright tests |
| Data model | 70 tables · 64 `DbSet` · 62 entity files · 59 EF configurations · 3 migrations |
| API surface | 36 controllers · 70 Infrastructure services · 52 Core interfaces |
| History | 944 commits, 2025-08-29 to 2026-07-08 |

Every figure is reproducible; [METRICS.md](./portfolio/METRICS.md) gives the command for each.

---

## Testing

`dotnet build SkillLedger.sln` is clean: 0 errors, 131 warnings on .NET SDK 9.0.303. `dotnet test`
is not: **1,697 passed, 39 failed, 7 skipped, 1,743 total**, then no further output for 87 minutes
before the run was terminated (the last event logged at 17 m 35 s, well under half the suite's
size). No CI workflow exists anywhere in the tree, so nothing in the project's lifetime ever saw
this. The frontend suite (Jest, 5,150 callbacks; Playwright, 10 tests) was not re-run for this
snapshot; its last recorded state is in [TESTING.md](./portfolio/TESTING.md), alongside the 21
backend tests that are hard-skipped rather than passing, and the two backend coverage reports
written the same day that disagree with each other by up to 15 points.

---

## Repository map

```text
src/SkillLedger.Api             36 controllers, middleware, and a 1,037-line Program.cs
src/SkillLedger.Core            62 entities, 52 interfaces, DTOs, enums
src/SkillLedger.Infrastructure  70 services, 59 EF configurations, 3 migrations
tests/SkillLedger.Tests         217 test files across unit, integration, and security
web/                            Next.js App Router: the product and an SEO site in one
portfolio/                      The write-ups linked from this page
docs/                           Working docs: 20 user stories, 13 research papers, deployment notes
```

---

## Documentation

[`portfolio/`](./portfolio/README.md) is the retrospective, evidence-backed write-up, indexed with a
one-line summary and length for every file it contains. [`docs/`](./docs/) is the dated working
residue that shaped the product: user stories, barter-law research, bug trackers, and deployment
notes, kept as-is rather than rewritten for a reader.

---

## About this snapshot

This repository is a single-commit export of a private repository. It holds the complete working
tree and none of the history. The history figures above describe that source repository, not this
export, and are recorded in [`docs/source-history.json`](./docs/source-history.json).

A handful of paths were withheld: a tracked `.env.production`, about 950 KB of captured console
output that had been force-added during a debugging session, an operations runbook covering other
projects, and a favicon-generator archive. Configuration values pointing at private infrastructure
were replaced with placeholders in the repository's own `REPLACE-WITH-*` style, and a status banner
was added at the top of `CLAUDE.md` and `AGENTS.md` so nobody follows live-looking setup
instructions for a system that is gone. Nothing else was altered. The `.claude/` and `.codex/`
skill directories, the agent instructions themselves, and the parked `.disabled` source files are
all deliberate and stay.

---

## Built with AI agents

Sixty-nine of the 944 commits in the source history are authored `Claude Code Assistant
<claude-code@anthropic.com>`, per [`docs/source-history.json`](./docs/source-history.json): the
rest are squashed into this snapshot's single commit, so that is the one number that survives.
`CLAUDE.md`, `AGENTS.md`, `.claude/` and `.codex/` are committed on purpose and reviewed like
source; they are not scrubbed to look human-only, and the drift-disclosure banner at the top of
`CLAUDE.md` is itself part of that review.

One concrete gate the agent process left behind: [`.githooks/pre-commit`](./.githooks/pre-commit)
runs `dotnet build` and `dotnet test` for backend changes, and `yarn lint`, `yarn typecheck`, and
`yarn test` for frontend changes, and rejects the commit if any of them fail. It could not enforce
what it never ran: there is no CI, so the hook only ever applied to whoever had it installed
locally, but it is a real, checked-in quality gate rather than an unverifiable claim that "AI
helped write this."

---

## Running it locally

```bash
dotnet build SkillLedger.sln  # 0 errors, 131 warnings on .NET SDK 9.0.303
dotnet test  SkillLedger.sln  # 1,697 pass, 39 fail, then it stalls. See TESTING.md
cd web && yarn install && yarn dev
```

The backend serves on 8030, the frontend on 3030.

Two things will trip you up, and both are true of the repository rather than of these
instructions. [`docker-compose.yml`](./docker-compose.yml) still starts **SQL Server 2022** on port
9030. The local development stack was never migrated when the application moved to Postgres in
February 2026, so it starts a database the application can no longer talk to. And the `web/` app
defaults to an API at `api.skillledger.app` unless you set `NEXT_PUBLIC_API_URL`, and that host is
gone.

`CLAUDE.md` has the fuller setup and carries the same drift: its port table and connection notes
still describe the SQL Server era.

---

## Who built this

Angel Campa, [github.com/AngelCampa1](https://github.com/AngelCampa1)

Built with AI assistance, openly. The `.claude/` and `.codex/` directories, `CLAUDE.md` and
`AGENTS.md` are committed on purpose and describe how the work was actually done.

## License

Source available, all rights reserved. No license to use, copy, modify, or redistribute is granted.
See [LICENSE](./LICENSE).
