# Portfolio

This folder is for a reader deciding whether the engineer who built SkillLedger is worth hiring or
working with, not for someone trying to run the product: it no longer runs anywhere. Every claim
below traces to a file in this tree, and every number traces to the command that produced it, given
in [METRICS.md](./METRICS.md). Nothing here is written from memory of what the code was supposed to
do; it is written from what the code at HEAD actually does, checked by reading it.

If you read one thing, read [CREDIT-LEDGER.md](./CREDIT-LEDGER.md): it is where the one real
engineering idea in this codebase and the one real failure in this codebase turn out to be the same
mechanism.

## Files

| File | Length | Covers |
|---|---|---|
| [ARCHITECTURE.md](./ARCHITECTURE.md) | 397 lines | The three .NET projects, where the layering leaks, the 70-table data model, and the SQL Server to Postgres move |
| [CREDIT-LEDGER.md](./CREDIT-LEDGER.md) | 382 lines | The encrypted wallet, the signed transaction log, the escrow state machine, and the three places the mechanism is theatre |
| [DOMAIN.md](./DOMAIN.md) | 233 lines | Barter tax law, the research behind it, and the gap between what the marketing site claims and what `src/` implements |
| [ENGINEERING-LOG.md](./ENGINEERING-LOG.md) | 221 lines | Ten real defects, in narrative form, each with root cause and file:line |
| [METRICS.md](./METRICS.md) | 244 lines | Every number used anywhere in this portfolio, with the exact command that produced it |
| [SECURITY.md](./SECURITY.md) | 243 lines | Cookie auth, CSRF, permission policies, what PII the database holds, Stripe wiring, and what was never audited |
| [TESTING.md](./TESTING.md) | 368 lines | Test layers, the coverage numbers, the 21 hard skips, and the absence of CI |

`portfolio/` is retrospective: finite, written after the fact, and edited for a reader who was
never in the room, drawing on the dated working residue in `docs/` (20 user-story specs, 13
barter-law research documents, the bug trackers, the coverage reports, a Stripe refactor plan,
deployment notes), cited as evidence, quoted or linked with a line number, rather than summarized
as if it were itself a portfolio piece. Read `docs/` if you want the process; read this folder if
you want the verdict.
