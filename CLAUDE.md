# Claude Code Configuration for SkillLedger

> **Status: shut down.** SkillLedger is no longer live and `skillledger.app` no longer serves it.
> Nothing described here is deployed anywhere. The file is kept unedited so the repository stays
> readable and buildable locally, which means it carries the project's own drift: the port table
> and database notes below still describe the SQL Server setup that the application moved off in
> February 2026, and `docker-compose.yml` still starts SQL Server rather than Postgres.

## Design Canon

- **Buttons are pills.** Treat fully rounded button geometry as a standing product preference. Every button or button-styled CTA should use pill corners (`border-radius: 9999px`, `rounded-full`, or equivalent), including primary/secondary actions, link-buttons, toolbar buttons, segmented/toggle controls, and icon buttons (circular when square). Do not introduce square or mildly rounded button shapes unless the user explicitly asks for that exception.

## Execution Expectations

**Work end-to-end without pausing for progress check-ins.** Do not stop after completing a batch or phase to ask "ready for feedback?" or "should I continue?". Execute the full plan autonomously from start to finish. Asking clarifying questions about implementation requirements is still expected and encouraged.

---

## Table of Contents
- [Quick Reference](#quick-reference)
- [Project Overview](#project-overview)
- [Project Structure](#project-structure)
- [Development Setup](#development-setup)
- [Architecture](#architecture)
- [Coding Standards](#coding-standards)
- [Testing](#testing)
- [Workflow](#workflow)
- [Bug Management](#bug-management)

---

## Quick Reference

### Essential Commands

| Task | Backend | Frontend |
|------|---------|----------|
| **Build** | `dotnet build` | `cd web && yarn build` |
| **Test** | `dotnet test` | `cd web && yarn test` |
| **Test (watch)** | `dotnet test --watch` | `cd web && yarn test --watch` |
| **Test (coverage)** | `dotnet test --collect:"XPlat Code Coverage"` | `cd web && yarn test --coverage` |
| **Run** | `dotnet run --project src/SkillLedger.Api` | `cd web && yarn dev` |
| **Lint** | `dotnet format && dotnet build` | `cd web && yarn lint` |
| **Type check** | *(included in build)* | `cd web && yarn typecheck` |

### Ports

| Service | Port |
|---------|------|
| Backend HTTP | 8030 |
| Backend HTTPS | 8031 |
| Frontend | 3030 |
| SQL Server (Docker) | 9030 |
| SQL Server (Windows) | `localhost\SQLEXPRESS01` |

### Key Files

| Purpose | Path |
|---------|------|
| Backend entry | `src/SkillLedger.Api/Program.cs` |
| DB context | `src/SkillLedger.Infrastructure/Data/SkillLedgerDbContext.cs` |
| API settings | `src/SkillLedger.Api/appsettings.Development.json` |
| Frontend config | `web/next.config.js` |
| Test base class | `tests/SkillLedger.Tests/Infrastructure/IntegrationTestBase.cs` |

---

## Project Overview

SkillLedger is a professional collaboration platform and barter exchange requiring enterprise-grade security, tax compliance, and financial services standards.

### Technology Stack

| Layer | Technology |
|-------|------------|
| **Backend** | .NET 9, ASP.NET Core, Entity Framework Core 9 |
| **Frontend** | Next.js 14, TypeScript, Tailwind CSS |
| **Database** | SQL Server 2022 |
| **Auth** | ASP.NET Identity, JWT Bearer |
| **Email** | Resend |
| **Payments** | Stripe |
| **Cloud** | Azure (Key Vault, App Insights, SQL, CDN) |
| **Real-time** | SignalR |

---

## Project Structure

```
SkillLedger/
├── src/
│   ├── SkillLedger.Api/           # ASP.NET Core Web API (Controllers, Middleware)
│   ├── SkillLedger.Core/          # Domain layer (Entities, Interfaces, DTOs)
│   └── SkillLedger.Infrastructure/ # Data access (DbContext, Services)
├── web/                            # Next.js 14 frontend
│   └── src/
│       ├── app/                   # Route-based pages (App Router)
│       ├── components/            # React components by domain
│       ├── contexts/              # State management (Auth, Theme)
│       ├── hooks/                 # Custom React hooks
│       ├── services/              # API client abstractions
│       └── types/                 # TypeScript definitions
├── tests/
│   └── SkillLedger.Tests/         # xUnit tests
│       ├── Integration/Api/       # API integration tests
│       ├── Core/Services/         # Service tests
│       ├── Mocks/                 # Mock implementations
│       └── Infrastructure/        # Test base classes
├── docs/                           # Documentation
│   ├── user-stories/              # Epic/story specifications
│   └── security/                  # Security guides
└── database/                       # Database scripts
```

---

## Development Setup

### Port Configuration

All projects use standardized ports to prevent conflicts when running simultaneously:

| Project | Backend | Frontend | SQL |
|---------|---------|----------|-----|
| **SkillLedger** | **8030** | **3030** | **9030** |

**Pattern**: Backend `80X0`, Frontend `30X0`, SQL `90X0`

### Database Configuration

**Windows Native (Recommended)**:
```
Server=localhost\SQLEXPRESS01;Database=SkillLedgerDb_Dev;Trusted_Connection=True;TrustServerCertificate=True
```
- Uses Windows Authentication (no password)
- Named instance, no port needed

**Docker/WSL Alternative**:
```
Server=localhost,9030;Database=SkillLedgerDb_Dev;User Id=sa;Password=YourPassword
```
- Map container port 1433 → host port 9030 in `docker-compose.yml`

### Configuration Files

| File | Purpose |
|------|---------|
| `src/SkillLedger.Api/Properties/launchSettings.json` | Backend ports |
| `src/SkillLedger.Api/appsettings.Development.json` | Dev settings, CORS |
| `web/package.json` | Frontend scripts (dev uses `-p 3030`) |
| `web/.env.local` | Frontend env vars, API URL |
| `web/next.config.js` | API proxy rewrites |

---

## Architecture

### Backend: Clean Architecture

```
┌─────────────────────────────────────────────────┐
│  SkillLedger.Api (Presentation)                 │
│  - Controllers, Middleware, Filters             │
├─────────────────────────────────────────────────┤
│  SkillLedger.Core (Domain)                      │
│  - Entities, Interfaces, DTOs, Enums            │
├─────────────────────────────────────────────────┤
│  SkillLedger.Infrastructure (Data Access)       │
│  - DbContext, Services, External integrations   │
└─────────────────────────────────────────────────┘
```

### Service Categories

**Financial Services** (require 90% test coverage):
- `CreditWalletService`, `CreditTransferService`
- `SubscriptionService`, `SubscriptionBillingService`
- `ProjectEscrowService`, `StripeCheckoutService`

**Security Services** (require 85% test coverage):
- `AuthenticationService`, `AuthorizationService`
- `BadgeSecurityService`, `EncryptionService`
- `AuditLogService`

**Content Services**:
- `DocumentService`, `FileShareService`
- `ContentModerationService`, `ProjectService`

**External Integrations** (OK to mock):
- `IEmailService` → Resend
- `IPaymentService` → Stripe
- `IFileStorageService` → Azure Blob
- `ICdnService` → Azure CDN
- `IVirusScanService` → External API

### Frontend: Next.js App Router

Components organized by domain:
- `admin/` - Admin panel
- `badges/` - Reputation badges
- `feedback/` - Feedback forms
- `messaging/` - Chat UI
- `ui/` - Primitives (Button, Modal, etc.)
- `wizard/` - Multi-step wizards
- `workspace/` - Collaboration workspace

---

## Coding Standards

### Quality Gates — Zero Tolerance
- **No placeholder code.** Every function must be fully implemented.
- **No TODO/FIXME/HACK comments.** If it needs doing, do it now or don't write the comment.
- **No `pass` in non-abstract methods.** No empty function bodies.
- **No `any` type in TypeScript.** Use proper types or `unknown` with narrowing.
- **No `eslint-disable` without explanation.** Fix the lint error instead.
- **No `// type: ignore` or `#pragma warning disable` without explanation.** Fix the types.
- **No mock-only tests.** Tests must exercise real logic. Mocks are only for external boundaries.

### Content Integrity
- **No fabricated metrics.** Never invent user counts, transaction volumes, or social proof statistics. Only use numbers that are real and verifiable. If no real metric exists, omit social proof rather than making one up.

### Code Review Before Commit — MANDATORY
After finishing implementation (tests pass, linting clean), you **must** invoke the `superpowers:requesting-code-review` skill before committing. The workflow is:
1. Complete implementation and verify tests pass locally
2. Invoke the `superpowers:requesting-code-review` skill (this spins up a code-reviewer agent)
3. Address **all** issues the reviewer identifies — no skipping, no deferring
4. Re-run tests after fixes to confirm nothing broke
5. Only then proceed to commit

Do not commit until code review is clean. This is not optional.

### Security Requirements

- All auth endpoints require CSRF protection
- Rate limiting: 5 registration attempts/hour/IP
- Password: 12+ chars with complexity rules
- Email enumeration protection: Always return generic success
- Audit logging: All security events with IP tracking
- HTTPS required in all environments

### Error Handling

**Backend**:
- Use `Result<T>` pattern for business operations
- Throw exceptions only for unexpected errors
- Return appropriate HTTP status codes:
  - `400` - Validation errors
  - `401` - Authentication required
  - `403` - Forbidden (authorized but not permitted)
  - `404` - Resource not found
  - `409` - Conflict (duplicate, concurrency)
  - `500` - Unexpected server error

**Frontend**:
- Use error boundaries for React components
- Display user-friendly error messages
- Log errors to console in development
- Report errors to monitoring in production

### Logging Standards

**Backend** (Serilog):
```csharp
// Structured logging with context
_logger.LogInformation("User {UserId} created project {ProjectId}", userId, projectId);

// Log levels:
// - Trace: Detailed debugging
// - Debug: Development debugging
// - Information: Normal operations
// - Warning: Recoverable issues
// - Error: Failures requiring attention
// - Critical: System failures
```

**Frontend**:
- Use `console.error` for errors
- Use `console.warn` for warnings
- Avoid `console.log` in production code

---

## Testing

### The Golden Rule

**Mock external services only. Never mock internal services.**

### What CAN Be Mocked (External Dependencies)

```csharp
IEmailService         // Resend
IFileStorageService   // Azure Blob Storage
IVirusScanService     // External virus scanning
ICdnService           // Azure CDN
IPaymentService       // Stripe
IGamingDetectionML    // External ML service
IGraphDatabaseService // SQL Server Graph (uses existing UserNetworkConnections table)
```

### What MUST NOT Be Mocked (Internal Services)

```csharp
IAuditLogService              // Use real or MockAuditLogService (persists to DB)
ISubscriptionService          // Business logic
IReputationCalculationService // Financial calculations
ICreditTransferService        // Financial transactions
IProjectEscrowService         // Escrow logic
IUserService                  // User management
// Any service under SkillLedger.Infrastructure.Services
```

### Anti-Pattern (DO NOT)

```csharp
// BAD: Mocking internal services tests NOTHING
private readonly Mock<IAuditLogService> _mockAuditLogService;
_mockAuditLogService.Verify(x => x.LogEventAsync(...), Times.Once);
// This passes even if the real service is broken!
```

### Correct Pattern

```csharp
// GOOD: Real internal services, only mock external
private readonly AuditLogService _realAuditLogService;
private readonly MockEmailService _mockEmailService;

// Verify REAL database state
var auditLog = await Context.AuditLogs.FirstOrDefaultAsync(a => a.Action == "Upload");
auditLog.Should().NotBeNull();
```

### Test Validity Rules

A test is **VALID** when it:
1. Tests real business logic with a real database
2. Only mocks external services (max 3 mocks)
3. Verifies actual database state changes
4. Can fail if the real implementation is broken

A test is **INVALID** when it:
1. Has 4+ mocked internal dependencies
2. Only verifies mock interactions (`.Verify()`)
3. Would pass even if the real service throws
4. Tests implementation details rather than behavior

### Coverage Requirements

- **95% code coverage minimum on every file you touch.** Not the repo average — each individual file.
- Backend: `dotnet test --collect:"XPlat Code Coverage"` — check per-file output
- Frontend: `cd web && yarn test --coverage` — check per-file output
- If a file drops below 95%, you are not done. Write more tests.

### Additional Coverage Thresholds (Category Minimums)

| Category | Target |
|----------|--------|
| Financial Services | 90% |
| Security Services | 85% |
| Business Logic | 80% |
| Utility Services | 70% |

**Note:** The 95% per-file rule on touched files supersedes these category minimums when you modify a file.

### Test Categories (Backend)

```csharp
[UnitTest]        // Pure unit tests
[IntegrationTest] // Database + services
[ApiTest]         // HTTP endpoint tests
[SecurityTest]    // Security validation
[FinancialTest]   // Money calculations
[FastTest]        // Completes in <100ms
```

### Naming Conventions

**Backend**:
- `POST_Register_WithValidData_ReturnsOk`
- `CreateProjectAsync_ValidData_CreatesProject`

**Frontend**:
- `renders all form fields`
- `validates email format`
- `shows error when submission fails`

---

## Workflow

### Database Migrations

The backend uses Entity Framework Core with PostgreSQL (Neon.tech serverless). Migrations live in `src/SkillLedger.Infrastructure/Migrations/`.

If your task touches the database schema:
1. **Create the migration first** (`dotnet ef migrations add <Name> --project src/SkillLedger.Infrastructure --startup-project src/SkillLedger.Api`)
2. **Apply it locally** (`dotnet ef database update --startup-project src/SkillLedger.Api`) before writing any test or implementation code
3. **Commit migration and dependent code together** in the same commit

Never write tests or application code against a schema that hasn't been applied locally.

### Worktree Discipline

- **Use worktrees for all work.** Do not work directly on `main`. **Always create worktrees from `main`** — never branch off another feature branch.
- Only commit files you created or modified for your task — stage explicitly by name, never `git add -A`
- Never commit unrelated changes or `.env` files

### Worktree Cleanup (post-merge)
After your branch is merged into main, remove the worktree:
```bash
git worktree remove .worktrees/<branch-slug>
```
If dirty, check what's dirty first:
```bash
git -C .worktrees/<branch-slug> status --short
```
**Never use `git worktree prune` as a substitute for `git worktree remove`.** Prune only cleans git's internal registration — it does not delete the directory.

### TDD — MANDATORY

Every task follows this exact cycle. No exceptions:
1. **Write the failing test first.** The test must define expected behavior before any implementation exists.
2. **Run the test. Confirm it fails.** If it passes, your test is wrong.
3. **Write the minimal implementation** to make the test pass.
4. **Run the test. Confirm it passes.**
5. **Refactor** if needed, re-run tests to confirm still green.
6. **Commit.**

### Pre-Commit Hook

A pre-commit hook (`.githooks/pre-commit`) runs automatically on every commit. It detects whether your changes are in `src/` (backend) or `web/` (frontend) and runs the applicable checks **in parallel**. Your commit will be **rejected** if any check fails:

**Backend checks:** `dotnet build` + `dotnet test --filter "Category!=E2E"`
**Frontend checks:** `yarn lint` + `yarn typecheck` + `yarn test` + `yarn build`

Do not bypass the hook with `--no-verify`. Fix the issue instead.

### Git Commit Policy

1. **Commit everything related, not just what you modified**
   - Review full working directory with `git status`
   - Include all related changes in one coherent commit

2. **Commit message format**:
   ```
   type(scope): Brief description

   - Detail 1
   - Detail 2

   🤖 Generated with [Claude Code](https://claude.ai/code)

   Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
   ```

3. **Bug fix commits**:
   ```
   fix(CATEGORY-XXX): Brief description

   - Root cause explanation
   - Fix approach
   - Regression test: TestClass.TestMethod

   🤖 Generated with [Claude Code](https://claude.ai/code)
   ```

### Manual Testing with Playwright

When asked to test with Playwright MCP:
1. Launch development servers
2. Navigate and test manually (don't write Playwright tests)
3. Verify UI functionality and user experience
4. Report observations about actual behavior
5. **Close all spawned servers when done**

### Definition of Done

**All of these MUST pass**:

- [ ] `dotnet build` - 0 errors
- [ ] `dotnet test` - 0 failures
- [ ] `cd web && yarn build` - Success
- [ ] `cd web && yarn test` - 0 failures
- [ ] `cd web && yarn lint` - 0 errors
- [ ] `cd web && yarn typecheck` - 0 errors

**NO EXCEPTIONS**: A user story is NOT complete until all checks pass.

### Story Tracking

Check `STORY_TRACKER.md` before and after tasks:
- Review current stories in progress
- Verify dependencies and blockers
- Update status when completing work

---

## Bug Management

### Bug Tracking Files

| File | Purpose |
|------|---------|
| `BUG_REPORT.md` | Backend/Infrastructure bugs |
| `web/FOUND_BUGS.md` | Frontend bugs |

### Severity Definitions

| Level | Description |
|-------|-------------|
| **CRITICAL** | Security vulnerabilities, data loss, crashes |
| **HIGH** | Auth issues, session handling, UX blockers |
| **MEDIUM** | Race conditions, validation gaps, memory leaks |
| **LOW** | Performance, accessibility, minor edge cases |

### Bug Fix Priority

1. **CRITICAL** - Fix immediately
2. **HIGH** - Fix before new features
3. **MEDIUM** - Fix in current sprint
4. **LOW** - Backlog

### Bug Entry Format

```markdown
### BUG-CATEGORY-XXX: Brief Title
- **Severity**: CRITICAL / HIGH / MEDIUM / LOW
- **File**: `path/to/file.ts:123`
- **Issue**: Description of the problem
- **Status**: NEW / FIXED / BLOCKED
- **Fix**: Date, commit hash
- **Regression Test**: `TestClass.TestMethod`
```

### Immediate Fix Policy

When a bug is discovered:
1. **STOP** current work
2. **DOCUMENT** in appropriate tracking file
3. **FIX** immediately
4. **ADD** regression test that proves the fix
5. **RESUME** original work

---

## Design Tokens

Design tokens are defined as CSS custom properties in `web/src/app/globals.css` with Tailwind configuration in `web/tailwind.config.js`.

**Never hardcode colors, spacing, or typography values.** Always reference the CSS custom properties (e.g., `hsl(var(--primary))`). If a new value is needed, add it to `globals.css` as a CSS custom property and extend `tailwind.config.js` — do not add one-off overrides in component styles.

---

## Content Writing Skills

**For any human-facing text** (UI copy, landing pages, emails, blog posts, guides, error messages, tooltips, onboarding flows):
- Use the `humanizer` skill to remove AI-generated patterns from the final text

This is mandatory for all user-visible content. Do not ship AI-sounding copy.

---

## Notes

- The project uses HTTPS in development; ensure SSL certificates are trusted
- Redis is configured for caching but can be disabled in development
- Azure Key Vault is disabled in development (use local secrets)
- Focus on critical flows over comprehensive coverage
- Security and compliance requirements are non-negotiable
- All financial calculations must have unit tests

---

## Sub-Agent Driven Development

**Worktree isolation.** All feature/fix work MUST happen inside a git worktree. Use the `using-git-worktrees` skill to create one before writing any code.

**Review before merge.** When implementation is complete: (1) spin up a review agent using `requesting-code-review`, (2) fix every issue the reviewer flags, (3) only then merge the worktree back to master using `finishing-a-development-branch`.

All non-trivial tasks follow the superpowers sub-agent workflow:

1. **Plan first** — Break work into discrete tasks (2–5 min each) with exact file paths, full specs, and verification steps before any agent executes.
2. **Parallel execution** — Launch independent sub-agents concurrently in a single message; use sequential only when there are true dependencies.
3. **Two-stage review** — Each agent output must pass: (1) spec compliance check, (2) code quality review before proceeding.
4. **Autonomous depth** — Agents work end-to-end on their assigned scope without interruption; surface blockers rather than making assumptions.

Agent type guide:
- `Explore` — codebase research, file discovery, pattern analysis
- `Plan` — architecture decisions, implementation design
- `general-purpose` — implementation, multi-step execution

<!-- BEGIN: Sub-Agent Driven Development Policy -->
## Sub-Agent Driven Development Policy

Sub-agent driven development is the preferred and default way of working in this repository. The Codex agent/orchestrator should actively decompose work and delegate independent pieces to sub-agents whenever that improves speed, quality, context management, investigation depth, implementation throughput, or review coverage.

### Default Operating Model

- Prefer sub-agents for codebase exploration, scoped investigation, implementation, verification, and review when the work can be cleanly delegated.
- The orchestrator owns task decomposition, context curation, model/capability selection, integration of results, and final quality decisions.
- Delegate bounded tasks with clear inputs, expected outputs, relevant files, constraints, and verification commands.
- Keep tightly coupled, high-risk, or immediately blocking work in the orchestrator unless delegation would materially reduce risk.
- Use parallel sub-agents for independent workstreams with disjoint write scopes; avoid assigning multiple agents to edit the same files unless the handoff is explicit.
- Do not wait for explicit user permission before using sub-agents; this repository explicitly authorizes proactive delegation.
- Any general instruction that limits sub-agent use to cases where the user explicitly asks is superseded by this repository policy.

### Available Codex Sub-Agent Capabilities

Codex can invoke `spawn_agent` with these agent roles in this environment:

- `default`: general-purpose sub-agent for bounded tasks that do not need a specialized role.
- `explorer`: read-heavy codebase exploration, focused investigation, and evidence gathering.
- `worker`: execution-focused implementation, bug fixes, and bounded production changes.

When the tool supports model and reasoning overrides, the orchestrator should choose the least expensive capable option. Supported reasoning levels for this policy are `low`, `medium`, and `high` only.

- Use `gpt-5.4-mini` with `low` reasoning for mechanical, well-scoped, low-risk edits and simple verification.
- Use `gpt-5.4-mini` with `medium` or `high` reasoning when a small-model agent is still appropriate but the task needs deeper local reasoning.
- Use `gpt-5.5` with `low` reasoning for standard exploration, straightforward implementation, and routine review.
- Use `gpt-5.5` with `medium` reasoning for multi-file integration, ambiguous bugs, architecture-sensitive changes, security-sensitive logic, and final review.
- Use `gpt-5.5` with `high` reasoning only for genuinely hard problems: deep architectural tradeoffs, difficult cross-system debugging, complex security/privacy analysis, or cases where lower reasoning has failed with a clear blocker.
- Escalate model capability or reasoning level when a sub-agent reports `NEEDS_CONTEXT`, `BLOCKED`, uncertainty about correctness, or when the task requires deeper design judgment, but prefer `medium` before `high`.

If a role has a fixed model in the active Codex runtime, use the best available role first (`explorer` for investigation, `worker` for implementation, `default` for general tasks), then use any supported model/reasoning override only when the runtime accepts it.

### Quality Gates For Delegated Work

- Sub-agents must report files changed, tests run, findings, blockers, and residual risks.
- The orchestrator must review sub-agent output before treating it as complete.
- For implementation work, prefer a two-stage review: first spec compliance, then code quality.
- All delegated changes remain subject to this repository's normal tests, linting, typechecking, security, privacy, and deployment rules.
<!-- END: Sub-Agent Driven Development Policy -->

## AI Agent Orchestration

AI agent instances operating in this repository are orchestrators. They must delegate exploration, implementation, verification, and other execution work to sub-agents whenever the work can be cleanly scoped, preserving the orchestrator's context window for coordination, integration, and final judgment.

## Required marketing copy pass

For this repo, all marketing copy must pass through both writing checks before completion:

1. Use the `humanizer` skill to remove AI-sounding, bloated, or generic copy.
2. Use the `third-grade-copy` skill to rewrite and audit the result for a third-grade reading level.

This applies to landing pages, hero copy, CTAs, pricing copy, onboarding copy, emails, ads, popups, social copy, SEO pages, and user-facing UI text that sells, explains, persuades, activates, or reassures.

Do not apply this rule to code identifiers, logs, API docs, technical docs for developers, exact legal text, database values, or user-generated content unless the user asks.

<!-- BEGIN: User-Facing Copy Guardrails -->
## User-Facing Copy Guardrails

For any user-facing copy in this repo, run the copy through these guardrails before you call the work done. This applies to product UI text, landing pages, hero copy, CTAs, pricing copy, onboarding copy, emails, ads, popups, social posts, SEO pages, help text, empty states, reassurance text, and any copy that sells, explains, persuades, activates, or reassures.

Required order:

1. Run the globally installed `humanizer` skill to remove AI-sounding, bloated, or generic copy.
2. Run the globally installed `third-grade-copy` skill to rewrite and audit the result for a third-grade reading level. The source package for this skill lives in a shared internal package repository; if the global skill is missing or stale, reinstall or sync it from there before finalizing copy.
3. Verify there are zero lies: no made-up numbers, claims, proof, testimonials, guarantees, rankings, integrations, prices, timelines, or capabilities. Check claims against the product source of truth before publishing.
4. Verify the message fits the whole place it appears: the page, flow, audience, offer, brand voice, surrounding copy, and user intent. Do not approve a line just because it is clear in isolation.

Do not apply this rule to code identifiers, logs, API docs, technical docs for developers, exact legal text, database values, or user-generated content unless the user asks.
<!-- END: User-Facing Copy Guardrails -->

## Working autonomously
- **Poll, don't idle.** When a task, build, test run, or hook is running, actively poll its status and output until it finishes. Don't just sit and wait passively for it to return.
- **Keep going.** When working toward a goal, finishing one chunk of work means moving straight to the next chunk. Don't stop and wait for further input mid-goal — continue until the goal is done or you are genuinely blocked.