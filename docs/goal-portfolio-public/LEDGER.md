# Goal: SkillLedger snapshot ready for public release

> Take this repository to the standard where its owner can make it public without a second
> thought. Every claim checkable from the tree, every link resolving, every image worth showing,
> and a root-level `portfolio/` directory a reviewer finds in the first five seconds.
>
> The audience is a skeptical senior engineer who gives the page ninety seconds. The bar is that
> nothing on it is inflated and nothing on it is hidden. SkillLedger is dead; the honesty about
> how it died is the asset, not a liability to be managed.
>
> This file records the work. It is committed and public-bound, so it describes what was found
> and what was decided, never anything that would be unsafe to publish.

## Method

1. Work out what the product actually was, from the solution structure, the EF entities, the
   controllers and the user stories. The repository's own README could not be trusted for this
   (see SL-05).
2. Export the tracked tree at a single commit, sanitize it, and verify the result with assertions
   rather than by eye. The exporter and its verifier live outside this repository on purpose:
   their configuration describes what was withheld, so publishing them would defeat the point.
3. Find the one genuinely hard or unusual engineering idea in the codebase and put it above the
   fold with the file and line that proves it. A feature list is not a story.
4. Measure everything that gets asserted. `portfolio/METRICS.md` carries the command behind each
   number so a reader can re-run it.
5. Log findings as P0 / P1 / P2, fix what is fixable in a snapshot, and record what is not.
6. Re-verify: export completeness, credential shapes, machine-local paths, and every relative
   link including heading and line anchors.

## Cycle log

### Cycle 0 — 2026-08-13 — Identifying the product

The repository advertises itself, in its own README title, as a secure user registration
implementation. It is not that. Reading `SkillLedger.sln`, the 62 entities under
`SkillLedger.Core/Entities`, the 36 controllers, and `docs/user-stories/EPIC-03-CREDIT-ECONOMY.md`
established what it really was: a professional skills-barter marketplace where users posted
projects and settled in an internal credit currency rather than money, with escrow, milestone
release, reputation scoring and fraud detection on top.

Deployment status was established from the tree rather than assumed. `web/wrangler.jsonc` pins
custom-domain routes with `workers_dev: false`; `appsettings.Production.json` carries Stripe with
test mode off; `web/FOUND_BUGS.md` records an end-to-end run against the live site on 2026-03-18.
So it shipped. The same document records that the platform had three users. So it never launched.

### Cycle 1 — 2026-08-13 — Export pipeline

Built on the existing exporters in this portfolio rather than from scratch: single-commit export
via `git archive` (which emits the index and nothing else, so build output cannot reach the
snapshot even if it is sitting on disk), real source history recorded into
`docs/source-history.json`, a scrub pass, and a verifier that runs as assertions.

The existing exporters were written for Node repositories. Adapting them to a .NET solution meant
teaching the verifier about `bin/`, `obj/`, `TestResults/`, `.vs/` and `*.user`, which appear
inside each of five project directories once anything is built. That guard earned itself during
Cycle 3, when a manual re-stage swept compiled assemblies into the index and the verifier refused
the tree.

`docs/source-history.json` was extended beyond the usual fields to carry per-month commit counts
and the final commit's subject line. Both are cited in the README. Without them the README would
be asserting facts about a history that no holder of this snapshot could check.

### Cycle 2 — 2026-08-13 — Reading the credit ledger

The credit subsystem is where the engineering is. Wallet balances are stored as ciphertext, the
transaction log is signed and chained, and every path that moves value opens at `Serializable`.
Tracing how those three fit together produced SL-02, and the finding is unflattering enough that
it is worth saying plainly why it stayed: an honest account of a mechanism that did not work is
more use to a reader than a description of the mechanism as designed.

Two claims were corrected during drafting rather than shipped wrong. The hash chain is formed at
one of eight signing sites, not all of them, so the first draft's "implemented correctly" was
replaced with the actual count. And the `Serializable` remediation is applied at fourteen sites,
not fifteen; the fifteenth match was not a call.

### Cycle 3 — 2026-08-13 — Portfolio docs, build, and verification

Six documents written into a root-level `portfolio/`: architecture, the credit ledger, an
engineering log, testing, the domain question, and metrics. The 13 research documents in
`docs/research/` stayed where they were, as working material. They did not shape the code, so
promoting them to portfolio material would have misrepresented them; `portfolio/DOMAIN.md` says
so directly.

The solution was built and the suite was run rather than described. Build: clean. Suite: SL-03.

Final verification pass: export completeness against the source tree, credential-shape patterns,
machine-local path literals, and 323 relative links across the README and `portfolio/` including
every heading anchor and every `#Lnnn` line anchor. All resolve.

## Findings registry

`P0` = broken or blocking · `P1` = looks bad or confusing · `P2` = polish
`RETRACTED` = recorded, then disproved on re-verification. Retractions stay in the log.

- **SL-01 (P0, NOT FIXED — product defect, recorded)** — The marketing site sold a tax-compliance
  feature that does not exist. It states, as shipped copy rather than as roadmap, that the
  platform "automatically tracks fair market value for every exchange and generates 1099-B-ready
  reports", and uses it as a differentiator against named competitors. `1099` appears 35 times
  across 9 files under `web/src/`. There is no fair-market-value field, no tax year, no reporting
  artifact and no barter-tax code anywhere in roughly 115,000 lines of C#; a case-insensitive
  search of `src/` for "barter" returns nothing at all. The only `1099` string under `src/` or
  `tests/` is a line in a seed-persona reference describing workflows that were never built.
  Not fixable in a snapshot without rewriting shipped product copy, which would falsify the
  record. Documented instead, in full, at `portfolio/DOMAIN.md`. Note that an end-to-end run had
  already caught a smaller instance of the same failure on the same site — a "thousands of
  professionals" claim against a database holding three users — and filed it as severity LOW.

- **SL-02 (P0, NOT FIXED — product defect, recorded)** — The wallet encryption was inoperative in
  production, and failed open rather than closed. `appsettings.Production.json` enables the key
  vault by setting `UseKeyVault`, which is read by no C# file in the repository. The property the
  code branches on is `Enabled`, which the base configuration sets to `false` and production never
  overrides, so the key-derivation routine took its development branch in production and derived
  the data key from a hardcoded test seed compiled into the assembly. The integrity half has the
  same shape: the HMAC key for the transaction log is a `const string` with no key-vault path at
  all. Nothing asserts at startup that a production environment has a reachable vault. Compounding
  it, `RotateEncryptionKeysAsync` — the method the wallet's key-identifier column exists to serve —
  logs "not yet implemented" and returns `true`, so a caller checking the result is told the keys
  were rotated. This is the repository's headline engineering idea and it did not work. Written up
  at `portfolio/CREDIT-LEDGER.md` and carried in the README above the fold.

- **SL-03 (P1, NOT FIXED — product defect, recorded)** — The backend suite does not run green and
  does not run to completion. A release build of the solution is clean: 0 errors, 131 warnings.
  Running the suite on that build reported **1,697 passed, 39 failed, 7 skipped, 1,743 total** and
  then produced no further output for 87 minutes before being terminated; the last test event was
  logged at **17 m 35 s**. 1,743 is well under half the suite. The 39 failures cluster in
  integration tests that go through the API surface rather than spreading evenly. No CI ever ran
  any of it — the tree has no workflow directory — so nothing in the project's lifetime would have
  surfaced either the failures or the stall. Recorded rather than fixed: diagnosing 39 integration
  failures in a dead product is not what this track is for, and hiding the result would have been
  the only alternative. Full numbers at `portfolio/TESTING.md` and `portfolio/METRICS.md`.

- **SL-04 (RETRACTED — the expected hook was not in the code)** — The working hypothesis, taken
  from the presence of 13 research documents on barter economics and US barter-exchange tax law,
  was that the interesting engineering problem here would be valuing and recording barter
  transactions so that they satisfy tax reporting. That is a real constraint most side projects
  ignore, and it would have been an excellent hook. It is not implemented. Searches across every
  `.cs` file in `src/` and `tests/` for barter, fair market value, taxable income, and 1099 return
  nothing. The hypothesis was searched for, disproved, and replaced by SL-02, which is what the
  code actually does. The research became SL-01 rather than the story: it explains where the false
  marketing claims came from, since the SEO surface was generated from the same source material
  that correctly describes the law. Recorded because a track that only logs confirmed hypotheses
  is not a record of anything.

- **SL-05 (P1, FIXED)** — The repository's README was a stale story-level document titled
  "SkillLedger API - Secure User Registration Implementation". It described one user story out of
  twenty and had not been updated as the product grew to 36 controllers and five domains. A reader
  landing on the page would have concluded the repository was a registration sample. Replaced
  entirely; the new README opens by saying what the product was, who it was for, that it is dead,
  and what is worth reading. The original is not preserved — it documented a feature, and that
  feature is still in the tree and better described by the code.

- **SL-06 (P2, FIXED)** — No product imagery exists. The source carries fifteen images and all of
  them are logos or favicons: no screenshots, no captures, nothing showing the application
  running. Rather than embed a logo as a stand-in or reconstruct a screen that was never captured,
  the README says plainly that there are no screenshots and carries a diagram of the credit
  transfer path instead. Every element in it is drawn from named source files and is checkable
  from the tree. Its connector routing was verified geometrically so no line crosses a box it does
  not terminate on.

- **SL-07 (P2, FIXED)** — The agent-instruction files read as live operating instructions for a
  system that no longer exists: ports to bind, a database container to start, a domain to deploy
  to. They are carried across deliberately and were not otherwise edited, so a status banner was
  added to the top of each instead. The banner also names the drift they contain, which is real
  and worth a reader knowing: their port table and database notes still describe the SQL Server
  setup the application moved off in February 2026, as does the compose file, which still starts
  a database engine the application can no longer talk to.

- **SL-08 (P2, FIXED)** — `portfolio/DOMAIN.md`'s per-file breakdown of the `1099` count listed
  `faq-data.ts` as 2 occurrences; the true count is 3 (`web/src/lib/data/faq-data.ts:158` has
  "Form 1099-B" twice on one line). The listed rows summed to 34 against the section's own
  headline of 35. Corrected the row and added the explicit sum so the table checks itself. Also
  tightened the section to distinguish two different measurements that a prior report had
  conflated: the bare string `1099` (35 occurrences, 9 files, scoped to `web/src/`) and the exact
  phrase "1099-B compliance" (10 occurrences, 6 files, repo-wide — a subset of the 35, not an
  alternate count of it).

- **SL-09 (P1, FIXED)** — `apple-touch-icon.png`, `favicon-16x16.png`, `favicon-32x32.png`,
  `favicon.ico` and `favicon.svg` all carried an unrelated orange/brown shield-and-checkmark mark
  instead of SkillLedger's actual blue-and-purple "S", which is present elsewhere in the tree
  (`assets/`, `web/public/logo-simplified.png`). This is a pre-existing project issue, not
  something the export introduced. Replaced all five with crops of the existing "S" mark —
  cropped to its bounding box, padded, and resized per target dimension; no new artwork drawn.
  `web/src/components/Logo.tsx` still points at `web/public/logo.svg`, which still carries the old
  shield mark and was left as is: it is the site's in-page header logo rather than a favicon
  variant, and redrawing it as clean vector art was out of scope for this pass. Worth the owner's
  attention if this repository is ever revisited.

- **SL-10 (P1, FIXED)** — 34 markdown links pointed at files that do not exist, found with a
  link-audit script that separates real defects from web-app routes and vendored-package
  cross-references (the same four-class split used to produce SL-06 through SL-09's link counts).
  Three distinct causes:
  1. `STORY_TRACKER.md`'s 22 links to individual user-story documents (epics 01–05) used the
     path the tracker had before the repository was restructured into `docs/user-stories/`.
     Retargeted all 22 to their current location.
  2. Three links — two to an epic-06/US-402 document under an `atomic-user-stories/` path that
     never existed anywhere in the tree, one to a `STORY_PROGRESS.md` that likewise never
     existed — described documents the reader could never open. The content they pointed at is
     already written out inline in `STORY_TRACKER.md`, so the links were removed rather than
     retargeted or stubbed.
  3. `docs/user-stories/README.md` linked to seven early-planning documents (Azure cost and
     enterprise architecture, a bootstrap budget analysis, an original development plan, a
     testing-patterns guide, and separate backend/frontend TDD setup guides) that have no trace
     anywhere in the tree or in `docs/source-history.json`. Removed the links; where a sentence
     existed only to introduce one, reworded it so it no longer promises a document.

  Two more links live inside vendored agent-skill packages, the same category as
  `.claude/skills/**`, which `linkcheck.py` excludes entirely so neither the script's counts nor
  its output cover them: `.codex/skills/arrange/SKILL.md:48` and
  `.codex/skills/typeset/SKILL.md:48` both linked `reference/spatial-design.md` /
  `reference/typography.md` as if each skill carried its own `reference/` folder. Neither does —
  that path was copied from `frontend-design/SKILL.md`, the skill that does have a sibling
  `reference/`, and both files exist only there
  (`.codex/skills/frontend-design/reference/`). Not a case of a companion file never shipping:
  the target exists in the tree, the link was just pointed at the wrong directory. Fixed by
  retargeting both to `../frontend-design/reference/...` and confirmed with `test -f` that both
  now resolve. Everything else under `.claude/` and `.codex/` stayed untouched.

  `README.md` and `portfolio/` had no broken links to begin with. Before/after per
  `python linkcheck.py skillledger`: 34 `MISSING` → 0. That count excludes the two vendored-skill
  links above, which are fixed but invisible to the script.

### Cycle 4 — 2026-08-18 — Structural pass: README to spec, portfolio index, diagrams, root cleanup

The prior three cycles established what the product was and wrote the six `portfolio/` documents.
This cycle brought the repository into line with the cross-portfolio structural standard that
governs all fifteen `*-snapshot` repos: exact required headings in the required order, a
`portfolio/` index, mermaid where prose was standing in for a diagram, and no documentation loose
at the repository root.

Two claims this repository's honesty rests on — the hardcoded wallet-encryption seed (SL-02) and
the unimplemented 1099-B claim (SL-01) — were re-verified independently against source rather than
carried forward on trust. Both held. The wallet-seed re-verification went one layer deeper than the
existing writeup and found the config binding for `AzureKeyVaultConfiguration` doesn't exist at
all, in any environment; see SL-20 below.

- **SL-18 (P1, FIXED)** — `portfolio/` held six documents and no index, which the structural
  standard requires of every repo in the portfolio. Created `portfolio/README.md`: who it's for,
  the checkability promise, a table of all six files with a one-line summary and length each, and
  a "what is not here" paragraph drawing the line against `docs/`.

- **SL-19 (P2, FIXED)** — `portfolio/ENGINEERING-LOG.md`'s ten section headings (`### 1.` through
  `### 10.`) sat directly under the document's `# Engineering log` H1 with no H2 in between —
  an H1→H3 jump. Bumped all ten to H2.

- **SL-20 (P1, FIXED)** — Zero mermaid diagrams existed in this repository despite two clear cases
  named directly in the structural standard: the credit/escrow lifecycle and the domain-entity
  graph, both currently prose-and-tables only. Added a `stateDiagram-v2` of `ProjectEscrow`'s
  six-state lifecycle to `portfolio/CREDIT-LEDGER.md`, built from the entity's own transition
  methods (`ReleaseAmount`, `RaiseDispute`, `ResolveDispute`, `Freeze`, `Unfreeze`, `Cancel` at
  `ProjectEscrow.cs:185-260`) rather than inferred from behavior. Added a `flowchart` of the
  five-domain entity relationships to `portfolio/ARCHITECTURE.md`, scoped to one representative FK
  chain per domain rather than all 69 entities, with a caption pointing to the migration folder for
  the other 55. Both diagrams were extracted and rendered with `@mermaid-js/mermaid-cli` (no syntax
  errors) before being committed to the docs.

  Building the domain flowchart surfaced a real finding the prior cycles missed: `ProjectMilestone`
  (workspace domain) and `EscrowMilestone` (credit domain) are two unrelated entities that happen to
  share a name pattern, and `DeliverableSubmission.MilestoneId` references the former, not the
  credit-domain one. Neither has a foreign key to the other. Noted in the diagram's caption in
  `ARCHITECTURE.md` rather than left implicit.

  Re-verifying SL-02 for this pass went one step past the existing writeup. `AzureKeyVaultService`
  binds to `IOptions<AzureKeyVaultConfiguration>`, and `Program.cs` never calls
  `Configure<AzureKeyVaultConfiguration>(...)` anywhere — only `Configure<AzureKeyVaultSettings>`,
  an unrelated, near-identically-named class. So `_config.Enabled` isn't false because production
  failed to override a shared setting; it's false because nothing binds that class to configuration
  in any environment, including a hypothetical correctly-configured one.
  `appsettings.Production.json` separately carries `"AzureKeyVaultConfiguration": { "Enabled": true
  }` (line 20), which reads like the fix to anyone skimming the JSON and is not one, for the same
  reason. Added to `portfolio/CREDIT-LEDGER.md#the-flag-that-was-never-read` rather than filed as a
  new, separate finding, since the observed production behavior SL-02 already described is
  unchanged — the mechanism just turned out to be one layer more disconnected than written.

- **SL-21 (P2, FIXED)** — Seven documents sat loose at the repository root, outside both `docs/`
  and `portfolio/`: `BUG_REPORT.md`, `COVERAGE_STATUS_SUMMARY.md`, `DatabaseFix.cs`,
  `OVERALL_BACKEND_COVERAGE_REPORT.md`, `RESEND_MIGRATION_GUIDE.md`, `STORY_TRACKER.md`,
  `TESTING_SUMMARY.md`. Filed all seven into `docs/` with `git mv` and updated the eight relative
  links in `README.md` and `portfolio/` that pointed at the old root paths. CLAUDE.md's un-hyperlinked
  mentions of `BUG_REPORT.md` and `STORY_TRACKER.md` (bare filenames, not links) were left as-is,
  consistent with SL-07's decision not to hand-edit that file beyond its status banner.

- **SL-22 (P1, FIXED)** — `README.md` did not match the cross-portfolio structural standard: the
  status line was a hand-bolded blockquote rather than `> [!IMPORTANT]`, there was no byline/license
  `> [!NOTE]` near the top, `## Scale` hadn't been renamed to the required `## By the numbers`, and
  three *always*-required sections were missing outright — `## Testing`, `## Repository map`, and
  `## Built with AI agents` — plus `## Known gaps`. Restructured to the required heading order;
  folded the former `## Status, plainly` section into `## What it did`, tense-consistently; trimmed
  `## Documentation` to two sentences and two links per the standard's §1.6, since the file-by-file
  table now lives in exactly one place, `portfolio/README.md`; added `## Contents` once the rewrite
  crossed 250 lines. `## Built with AI agents` cites the one number that survives the squash — 69 of
  944 commits authored `Claude Code Assistant <claude-code@anthropic.com>`, per
  `docs/source-history.json` — and one concrete enforced gate, `.githooks/pre-commit`, rather than
  an unverifiable claim that AI helped write the code. `## Known gaps` consolidates the eight
  standing defects (wallet seed, 1099-B, one-of-eight hash chain, key rotation stub, the stalled
  test suite, the SQL Server/Postgres docker-compose drift, the duplicate Stripe wiring, the
  literal-null Redis registration) with a pointer to the portfolio doc that covers each in full,
  rather than restating them at length.

- **SL-23 (P2, FIXED)** — `portfolio/ARCHITECTURE.md:160-161` held a markdown link whose destination
  was split across a line wrap inside the parentheses — under CommonMark, a link destination cannot
  contain unescaped whitespace, so this rendered as broken (or as literal text) rather than as a
  link. Pre-existing, unrelated to this cycle's other edits; found and fixed while editing the
  surrounding paragraph for the domain-flowchart addition.

- **SL-24 (P2, FIXED)** — Ten fence blocks across `portfolio/CREDIT-LEDGER.md`,
  `portfolio/DOMAIN.md`, `portfolio/TESTING.md`, and the relocated `docs/TESTING_SUMMARY.md` opened
  with a bare ` ``` ` and no language tag. Tagged each by content (` ```text ` for hash-format
  strings and file listings, ` ```console ` for a shell session with a `$` prompt and exit code).
  Checked with a paired-fence script rather than by eye, since a bare closing fence is normal and
  not itself a defect. `README.md` and the rest of `portfolio/` already tagged every fence.

### Cycle 5 — 2026-08-18 — Missing SECURITY.md, a diagram fix, and two small corrections

A reviewer graded this repository's `portfolio/` P1 for the absence of `SECURITY.md`, which the
structural standard requires (§2.4) for any repo touching encrypted financial data, live-mode
Stripe, or ASP.NET Identity PII — SkillLedger touches all three. The same pass fixed a rendering
defect in the escrow-lifecycle diagram and two smaller wording/structural corrections a separate
review had raised.

- **SL-25 (P1, FIXED)** — `portfolio/SECURITY.md` did not exist. Wrote it covering the security
  surface `portfolio/CREDIT-LEDGER.md` does not: cookie authentication (`.SkillLedger.Auth`,
  15-minute sliding expiration, per-request security-stamp revalidation), the global CSRF filter
  and its `X-CSRF-TOKEN` header, the three separate authorization systems (role, permission, and
  subscription-tier policies), exactly what PII the `User`/`Profile`/`PaymentMethod`/`AuditLog`
  tables hold, how Stripe is wired (live mode in `appsettings.Production.json`, signature-verified
  webhooks, no live key literal anywhere in the tree), and the receipt-signature key fallback chain
  already recorded in `ENGINEERING-LOG.md` entry 6. Closes with an explicit "not protected, never
  verified" section: no audit, no penetration test, no certification, plus the unconfigured
  `AddDataProtection()` call and the absence of any dependency/secret-scanning tooling. Every claim
  was checked against source before being written, not carried forward from the docs describing the
  system. 245 lines, added to the `portfolio/README.md` index.

- **SL-26 (P2, FIXED)** — The `stateDiagram-v2` escrow diagram in `portfolio/CREDIT-LEDGER.md` drew
  all seven of `Active`/`PartiallyReleased`'s outbound transitions on one graph, which put
  `ReleaseAmount()`, `Cancel()`, `Freeze()` and `RaiseDispute()` edge labels close enough together
  near those two hub states to render as a merged, unreadable string. Split into two diagrams by
  concern — funding/release/cancellation versus disputes/freezes, sharing the `Active` and
  `PartiallyReleased` states rather than duplicating them — so no diagram asks more than four labels
  to share a node. While re-deriving the diagram from source, found `RaiseDisputeAsync` rejects a
  dispute only when `escrow.IsTerminal` (`Completed` or `Cancelled`,
  `ProjectEscrowService.cs:581`), which makes `Frozen → Disputed` a real, reachable transition the
  original diagram omitted. Added it. Every other edge was re-checked against `ProjectEscrow.cs` and
  `ProjectEscrowService.cs` and is unchanged from the original, verified-accurate set.

- **SL-27 (P2, FIXED)** — `portfolio/DOMAIN.md` claimed the exact phrase "Form 1099-B" appears
  twice on `web/src/lib/data/faq-data.ts:158`. It does not: the substring `1099-B` appears twice
  there ("Form 1099-B is an IRS form..." and "...you may receive a 1099-B"), but the full phrase
  "Form 1099-B" appears only once. Reworded to state what's actually true; the surrounding file and
  section counts (35 occurrences of `1099` across 9 files, 3 in `faq-data.ts`) were unaffected and
  re-checked as still correct.

- **SL-28 (P2, FIXED)** — `portfolio/img/` held the repository's one referenced diagram
  (`credit-path.svg`), but the structural standard specifies `portfolio/screenshots/` (§3.6). Moved
  the file at the filesystem level, updated the one reference to it (`README.md:24`), and confirmed
  programmatically that no reference to `portfolio/img` remains anywhere in the tree.

`portfolio/README.md`'s index table was updated for all of the above: the `SECURITY.md` row was
added at its correct `wc -l` length, and the `CREDIT-LEDGER.md` and `DOMAIN.md` rows — both stale
after this cycle's edits — were corrected to their new line counts (381 and 233) rather than left at
the pre-edit numbers. Every relative link and heading/line anchor across `portfolio/*.md` and the
root `README.md` was checked programmatically (path exists; heading-slug or line-number anchor
resolves) rather than by eye — all clean.

### Cycle 6 — 2026-08-18 — Corpus-wide index column order, and the snapshot-provenance section move

- The cross-repo standard fixed `portfolio/README.md`'s index table column order as link,
  length, summary. This repo's table had `File | Covers | Length`, length last — reordered
  to `File | Length | Covers`; all seven rows and the alignment row updated.
- Spec item 15a fixes the snapshot-provenance section as `## About this snapshot`, placed
  immediately after `## Documentation` and before `## Built with AI agents`. `README.md`
  already used that exact heading name, but it sat between `## Repository map` and
  `## Documentation` — one slot too early. Moved the section (prose untouched) to
  immediately after `## Documentation`, and reordered its `## Contents` entry to match.
- Checked for inbound links to `#about-this-snapshot` elsewhere in the repo: only the
  `## Contents` entry itself, already updated; the anchor slug is unchanged since only the
  heading's position moved, not its text, so nothing needed repointing.
- Recomputed every length cell in `portfolio/README.md` against `wc -l` after all edits: all
  seven rows match exactly.
- Ran a relative-link and `#anchor` resolution sweep over `README.md` and every
  `portfolio/*.md` file, using GitHub's slug rules: all resolve. (`#L<n>` GitHub source-line
  links in `README.md` and several `portfolio/*.md` files were excluded from the anchor
  check, since they resolve against the linked file's line count, not its headings.)
