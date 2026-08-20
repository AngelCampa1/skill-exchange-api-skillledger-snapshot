# Domain: barter tax law, and the feature that was never built

SkillLedger was a barter exchange. That word has a specific meaning in US tax law, and the meaning carries
obligations. This document covers the research the project did on those obligations, what the backend
implemented, and what the marketing site told users. The three do not agree.

Related: [ARCHITECTURE.md](./ARCHITECTURE.md), [CREDIT-LEDGER.md](./CREDIT-LEDGER.md),
[ENGINEERING-LOG.md](./ENGINEERING-LOG.md), [TESTING.md](./TESTING.md), [METRICS.md](./METRICS.md),
[README](../README.md).

---

## The constraint

[`docs/research/`](../docs/research) holds 13 tracked documents, about 62,000 words, on barter economics and
US barter-exchange law. They cover IRS barter exchange platform rules, valuation of professional services,
bartering in regulated professions (healthcare, legal), state-level treatment, and competitive/SEO analysis.
The tax conclusions are consistent across them:

1. **Services received in a barter exchange are taxable income at fair market value.** Revenue Ruling 79-24
   (1979-1 C.B. 60) is the anchor: its own example is a lawyer trading legal work for house painting, and
   both parties include the FMV received in gross income. Treas. Reg. § 1.61-2(d)(1) is the operative
   regulation for services.

2. **A third-party barter exchange is a broker with Form 1099-B reporting obligations.** IRC § 6045(c) defines
   "broker" to include a barter exchange, and Treas. Reg. § 1.6045-1(a)(4) covers any person whose members
   contract to trade property or services through them. Unlike the general § 6041 regime there is no $600
   floor and no corporate exemption: the de minimis threshold is $1.00 per transaction, and the only volume
   relief is fewer than 100 exchanges in a calendar year.

3. **An internal, non-cashable credit ledger does not avoid this.** From
   [IRS Barter Exchange Platform Rules.md:60](../docs/research/IRS%20Barter%20Exchange%20Platform%20Rules.md#L60):

   > a digital platform operating a strict "no cash-out" credit ledger is the exact archetype of a
   > commercial barter exchange described in the Treasury Regulations

reported on Form 1099-B, Box 13. And from
[the same document, line 82](../docs/research/IRS%20Barter%20Exchange%20Platform%20Rules.md#L82), the taxable
event fires when the platform credits the account, not when the credits are spent.

That third point describes SkillLedger's design precisely: the research identified this product as in scope,
not the category in the abstract. It is a real constraint most side projects would never look up, and the work
of finding it was done properly.

[`CLAUDE.md:70`](../CLAUDE.md#L70) states the requirement in the project's own words: "SkillLedger is a
professional collaboration platform and barter exchange requiring enterprise-grade security, tax compliance,
and financial services standards."

---

## What the backend implements: nothing

Run in this snapshot:

```console
$ grep -rniE '\b(barter|IRS|1099|taxable|fair.?market.?value|FMV)\b' \
      --include='*.cs' src tests | grep -v '/bin/\|/obj/'
$ echo $?
1
```

No matches. Widening to every file under `src/`, regardless of extension, also returns nothing. There is no
tax year, no FMV field, no 1099 generation, no exchange-member reporting, no withholding, no TIN collection,
and no reference to `docs/research/` anywhere in the backend.

Three things the grep does not catch, stated so the finding is not overclaimed:

- [`User.TaxCompliant`](../src/SkillLedger.Core/Entities/User.cs#L36) is a `bool`, commented "Whether the user
  has completed tax compliance setup." It is a mapped column with a `defaultValue: false`. It is read once,
  into a response DTO in `AuthenticationService`. Nothing in `src/` ever assigns it. There is also a
  `UserStatus.TaxCompliant` enum value used as a general "verified" tier in `ProjectService` and
  `ReputationCalculationService`, unrelated to tax.
- The word "tax" appears in Stripe subscription billing code, meaning sales tax on the SaaS subscription, not
  barter income.
- One tracked document does mention the feature.
  [`tests/TEST_DATA_REFERENCE.md:305`](../tests/TEST_DATA_REFERENCE.md#L305) defines a seed persona,
  "Patricia Williams (Eve) - Enterprise Compliance Officer", whose listed use cases include
  [tax compliance workflows](../tests/TEST_DATA_REFERENCE.md#L318) and
  [W-9/1099 generation](../tests/TEST_DATA_REFERENCE.md#L321). It is the only `1099` string anywhere under
  `src/` or `tests/`. A test persona was seeded for workflows that have no code behind them.

The ledger itself has no place to put the numbers. `CreditTransaction` records a single
[`public int Amount`](../src/SkillLedger.Core/Entities/CreditTransaction.cs#L50): credits, with no paired
dollar figure, no rate and no valuation timestamp. Nothing in `src/` maps a credit to a currency amount.

### What `FinancialReporting*` actually does

The names invite the assumption that this is the tax module. It is not.

[`FinancialReportingController`](../src/SkillLedger.Api/Controllers/FinancialReportingController.cs) exposes
13 endpoints: `credit-summary`, `dashboard`, `analytics`, `export`, `monthly-reports`, `budget-tracking`,
`goal-progress`, `transaction-breakdown`, `trends`, `insights`, plus admin `system-analytics`, `top-earners`
and `data-integrity`.
[`FinancialReportingService`](../src/SkillLedger.Infrastructure/Services/FinancialReportingService.cs) behind
it is 1,341 lines of personal-finance dashboard: spending and earning analytics, monthly and quarterly
rollups, budget alerts, goal progress, category breakdowns, trend series, a 30-day earnings forecast, and
peak-activity detection. Exports are CSV, JSON and PDF of transaction history and credit summaries.

The closest thing to a tax artifact is
[`GenerateAnnualReportAsync(userId, year)`](../src/SkillLedger.Infrastructure/Services/FinancialReportingService.cs#L261).
It builds a `CreditSummaryReport` for Jan 1 to Dec 31 of a calendar year, a per-user calendar window over credit totals,
with no FMV, no recipient identification, no form layout and no filing path. It is not even reachable from a
controller route; the controller exposes monthly reports and arbitrary date ranges. It is a year-shaped hole
where the tax feature would go, and nothing was put in it.

---

## What the marketing site claims

`1099` appears **35 times across 9 files under `web/src/`**, not as a roadmap item but as a shipped
differentiator, in present tense, usually against a named competitor.

The distribution: [comparisons-data.ts](../web/src/lib/data/comparisons-data.ts) 15,
[glossary-data.ts](../web/src/lib/data/glossary-data.ts) 5,
[industries-data.ts](../web/src/lib/data/industries-data.ts) 4,
[features-data.ts](../web/src/lib/data/features-data.ts) 3, [faq-data.ts](../web/src/lib/data/faq-data.ts) 3
(the substring `1099-B` appears twice on
[line 158 alone](../web/src/lib/data/faq-data.ts#L158): "Form 1099-B is an IRS form..." and "...you may
receive a 1099-B", though the full phrase "Form 1099-B" itself appears only once there),
[about/page.tsx](../web/src/app/about/page.tsx) 2, and one each in [seo.ts](../web/src/lib/seo.ts),
[resources/templates](../web/src/app/resources/templates/page.tsx) and
[tools/barter-valuation-calculator](../web/src/app/tools/barter-valuation-calculator/page.tsx): 15 + 5 + 4 + 3 +
3 + 2 + 1 + 1 + 1 = 35, matching the headline count above.

Two different measurements appear in this section and they should not be collapsed into each other: the bare
string `1099` counted above is **35 occurrences across 9 files under `web/src/`**. The narrower exact phrase
**"1099-B compliance"** (used below as one recurring example of the claim's wording) appears **10 times
across 6 files repo-wide** (`about/page.tsx`, `resources/templates/page.tsx`, `comparisons-data.ts` ×4, plus
two mirrored occurrences in generated `web/public/md/` snapshots and this document's own citations of the
phrase). It is a subset of the 35, not an alternate count of the same thing.

A sample, all verbatim:

- [about/page.tsx:55](../web/src/app/about/page.tsx#L55): "SkillLedger automatically tracks FMV for every
  exchange and generates 1099-B-ready documentation to simplify year-end tax reporting." The same page's meta
  description at [line 11](../web/src/app/about/page.tsx#L11) sells "escrow protection and 1099-B compliance."
- [comparisons-data.ts:479](../web/src/lib/data/comparisons-data.ts#L479), answering "Which platform handles
  taxes better?" against BarterOnly: "SkillLedger automatically tracks fair market value for every exchange
  and generates 1099-B-compatible reports." Near-identical wording appears against time banks at
  [line 529](../web/src/lib/data/comparisons-data.ts#L529), and the claim sits in SkillLedger's own strengths
  list in seven separate comparisons (Simbi, Fiverr, TaskRabbit, BarterOnly, Time Banking Apps, Contra,
  Freelancer.com), e.g. [line 439](../web/src/lib/data/comparisons-data.ts#L439).
- [features-data.ts:24](../web/src/lib/data/features-data.ts#L24): "The platform tracks fair market values
  automatically and generates tax-ready documentation at year end," with "1099-B-ready reports generated
  automatically at year end" listed as a product benefit.
- [industries-data.ts:216](../web/src/lib/data/industries-data.ts#L216) is the sharpest one, because it is a
  statement of fact about what will happen to the reader: "Businesses using a barter exchange (including
  SkillLedger) will receive Form 1099-B reporting gross barter proceeds."
- [faq-data.ts:156](../web/src/lib/data/faq-data.ts#L156) and
  [glossary-data.ts:208](../web/src/lib/data/glossary-data.ts#L208) explain Form 1099-B accurately as general
  tax education, which makes the platform-specific claims read as equally grounded.
- [templates/page.tsx:29](../web/src/app/resources/templates/page.tsx#L29) offers a barter invoice template
  "creating the paper trail required for Form 1099-B compliance."
- [barter-valuation-calculator/page.tsx:14](../web/src/app/tools/barter-valuation-calculator/page.tsx#L14)
  positions a public calculator as determining "equitable trade values for IRS compliance."

The claims are structural rather than decorative: `seo.ts` lists "IRS 1099-B reporting" among the site's
positioning keywords, and the comparison pages use FMV tracking as the load-bearing reason to pick SkillLedger
over cheaper alternatives.

---

## The failure mode

The sequence is worth naming, because it is common and it is expensive:

1. Real domain research was commissioned and completed: 13 documents, correct citations, correct conclusion
   that this specific product architecture is a regulated barter exchange.
2. A programmatic SEO surface was generated **from that research**. The glossary entries, industry pages,
   comparison matrices, and the valuation calculator are all downstream of the same source material. That is
   why the general tax explanations in them are accurate.
3. The generator did not distinguish "what the law requires" from "what this product does." It turned domain
   facts into feature claims and repeated them across the generated surface.
4. The backend feature was never started. Not partially built, not stubbed, not ticketed in a way that left a
   trace in `src/`. Zero lines.

**A marketing site generated from a domain model the product does not implement.** The research made the copy
sound credible, which is what made it dangerous: a reader with a tax question got accurate law and a false
claim about the platform in the same paragraph, in the same register. Users could reasonably have relied on it
and not tracked FMV themselves.

The site had this problem more than once. [`web/FOUND_BUGS.md:131`](../web/FOUND_BUGS.md#L131) records
BUG-E2E-012, "Fabricated social proof metrics," found independently during E2E testing: the
subscription page invited visitors to join "thousands of professionals" against a figure the
database did not support. It is filed as severity LOW, citing a `CLAUDE.md` rule ("Never invent
user counts"). The 1099-B claims are the
same class of defect with materially higher stakes, and nothing in the tree shows anyone caught them.

---

## What implementing it would have taken

Five pieces, none optional:

1. **FMV capture at agreement time.** Both parties agree a price when a project is accepted; Rev. Rul. 79-24
   treats a stipulated price as presumptive FMV, so that moment is the cheapest place to capture a defensible
   dollar figure. It has to be stored on the transaction and frozen: an FMV recomputed later from current
   rates is not the value at the time of the exchange. Today `CreditTransaction` has only
   [`int Amount`](../src/SkillLedger.Core/Entities/CreditTransaction.cs#L50) and nowhere to put a dollar
   figure.
2. **Per-member annual aggregation** of the FMV of credits *allocated* (credited, not spent) per the
   realization timing in the research.
3. **A tax-year boundary** (a timezone decision as much as a date one), plus a correction path for
   transactions reversed or refunded after year end.
4. **Retention and identity**: TIN/W-9 collection, address of record, multi-year immutable storage of the
   figures actually reported.
5. **A real reporting artifact**: Box 13 output in IRS file format, recipient copies, a corrections workflow.
   A CSV export is not this.

Steps 1, 3, 4 and 5 are ordinary work. Step 2 is where the ledger's design fights back.

`CreditTransaction.Amount` is a plaintext `int`, so summing transactions in SQL is possible. The running
totals are not: [`CreditWallet`](../src/SkillLedger.Core/Entities/CreditWallet.cs#L37) stores
`EncryptedBalance`, `EncryptedPendingBalance`, `EncryptedTotalEarned` and `EncryptedTotalSpent` as 512-char
ciphertext, with `[NotMapped]` plaintext mirrors the service layer fills in after decryption. No `SUM()` can
touch them, so any per-member annual total must be rebuilt by decrypting row by row in application code, for
every member, every year, every recomputation or correction.

The integrity chain makes the rebuild harder to trust rather than easier.
[`CreditWalletService`](../src/SkillLedger.Infrastructure/Services/CreditWalletService.cs#L366) sets
`PreviousTransactionHash` from the single most recent transaction **platform-wide**, ordered by `CreatedAt`
with no tiebreaker and no per-user scoping. That gives one global chain, so verifying one member's year means
walking every member's transactions in that window, and concurrent writes can produce two transactions
claiming the same predecessor. A tax figure you cannot independently re-derive and verify is a figure you
cannot sign your name to. [CREDIT-LEDGER.md](./CREDIT-LEDGER.md) covers the chain and the encryption in
detail.

The honest summary: the hard part was never the tax law. The law was researched correctly and early. The hard
part was that the credit ledger was built as a private balance store rather than as an auditable record of
valued exchanges, and by the time the tax feature would have been written, the aggregation it needed had been
designed out.
