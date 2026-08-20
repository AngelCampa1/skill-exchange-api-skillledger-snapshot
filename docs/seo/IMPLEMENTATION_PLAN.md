# SkillLedger SEO Content Implementation Plan

> Turns 10 completed research documents into ~1,000+ indexed pages across 30 articles, 15 glossary terms, 6 scenarios, 4 categories, 10 industry pages, 7 comparison pages, 225 skill-pairing pages, and 625 city-skill pages.
>
> Created: March 2026 | Research status: 9/10 gaps complete (only QuickBooks/Xero tutorial research missing)

---

## Table of Contents

1. [Execution Summary](#1-execution-summary)
2. [Phase A — Foundation (Weeks 1-4)](#2-phase-a--foundation-weeks-1-4)
3. [Phase B — Trust Layer + City Expansion (Weeks 5-8)](#3-phase-b--trust-layer--city-expansion-weeks-5-8)
4. [Phase C — Comparison + Startup Content (Weeks 9-14)](#4-phase-c--comparison--startup-content-weeks-9-14)
5. [Phase D — Programmatic Scale (Weeks 15-20)](#5-phase-d--programmatic-scale-weeks-15-20)
6. [Phase E — International (Post Week 20)](#6-phase-e--international-post-week-20)
7. [Technical Infrastructure Tasks](#7-technical-infrastructure-tasks)
8. [Content Quality Standards](#8-content-quality-standards)
9. [SEO Technical Checklist Per Page Type](#9-seo-technical-checklist-per-page-type)
10. [Measurement Framework](#10-measurement-framework)

---

## 1. Execution Summary

### What exists today

| Asset | Count | Location |
|-------|-------|----------|
| MDX articles | 7 | `web/content/articles/{silo}/` |
| Glossary terms | 33 | `web/src/lib/data/glossary-data.ts` |
| Skill categories | 15 | `web/src/lib/data/categories-data.ts` |
| City pages | 25 | `web/src/lib/data/cities-data.ts` |
| How-to scenarios | 18 | `web/src/lib/data/scenarios-data.ts` |
| Article silos | 5 | `freelancing`, `skill-exchange`, `barter-economy`, `credit-systems`, `collaboration` |
| **Total indexed URLs** | **~98** | |

### What this plan builds

| Asset | New Count | Total After | Route |
|-------|-----------|-------------|-------|
| MDX articles | +30 | 37 | `/resources/[slug]` |
| Glossary terms | +15 | 48 | `/glossary/[term]` |
| Skill categories | +4 | 19 | `/categories/[slug]` |
| City pages | +25 | 50 | `/skill-exchange/[city]` |
| How-to scenarios | +6 | 24 | `/how-to/[slug]` |
| Industry pages | +10 | 10 | `/industries/[slug]` *(new route)* |
| Comparison pages | +7 | 7 | `/compare/[slug]` *(new route)* |
| Skill-pairing pages | +225 | 225 | `/trade/[a]/for/[b]` *(new route)* |
| City-skill pages | +625 | 625 | `/locations/[city]/[skill]` *(new route)* |
| Article silos | +3 | 8 | `tax-and-legal`, `trust-and-safety`, `industries` |
| **Total new indexed URLs** | **~945** | **~1,043** | |

### Research-to-article mapping

| Research Document | Articles It Feeds | Phase |
|---|---|---|
| `IRS Barter Exchange Platform Rules.md` + `Tax Rules.md` | 1, 2, 6 | A |
| `Legal requirements for barter service agreements.md` | 4, 5, 7 | A |
| `Skill barter arrangements what goes wrong.md` | 8, 9, 10, 11 | B |
| `Valuing professional services in barter.md` | 12, 13, 14, 15 | B |
| `The barter economy's data desert.md` | 16 | B |
| `Skill bartering in startups.md` | 17, 18, 19, 20 | C |
| `Five service platforms compared.md` | 21, 22, 24 | C |
| `Bartering legal services the ethics rules.md` | 26 | D |
| `Bartering healthcare services.md` | 28 | D |
| *(QuickBooks/Xero — NOT YET RESEARCHED)* | 3 | A (blocked) |

---

## 2. Phase A — Foundation (Weeks 1-4)

> Goal: Fix the highest-value zero-coverage gaps. Tax & legal content has the highest conversion intent and zero competitor coverage.

### A1. Update silo enum (Day 1)

**File:** `web/src/lib/content.ts`

Add 3 new silo values to the `ArticleFrontmatterSchema`:

```typescript
silo: z.enum([
  'freelancing',
  'skill-exchange',
  'barter-economy',
  'credit-systems',
  'collaboration',
  'tax-and-legal',       // NEW
  'trust-and-safety',    // NEW
  'industries',          // NEW
])
```

Create the corresponding content directories:
```
web/content/articles/tax-and-legal/
web/content/articles/trust-and-safety/
web/content/articles/industries/
```

**Effort:** 15 min | **SEO impact:** Enables all new article silos

---

### A2. Add 15 glossary terms (Week 1)

**File:** `web/src/lib/data/glossary-data.ts`

Add these terms to the `glossaryTerms` array, following the existing `GlossaryTerm` interface:

```typescript
interface GlossaryTerm {
  slug: string
  term: string
  definition: string
  relatedTerms: string[]
}
```

**Terms to add (ordered by SEO priority):**

| # | Slug | Term | Target Keyword | Est. Volume | Research Source for Definition |
|---|------|------|---------------|-------------|-------------------------------|
| 1 | `barter-agreement` | Barter Agreement | service barter agreement template | 1,500/mo | `Legal requirements for barter service agreements.md` § 1 |
| 2 | `form-1099-b` | Form 1099-B | form 1099-b barter exchange | 600/mo | `IRS Barter Exchange Platform Rules.md` § 1 |
| 3 | `barter-valuation` | Barter Valuation | how to value a service for barter | 1,200/mo | `Valuing professional services in barter.md` § 1 |
| 4 | `scope-creep` | Scope Creep | barter scope creep | high | `Legal requirements for barter service agreements.md` § 3 |
| 5 | `service-exchange-rate` | Service Exchange Rate | — | — | `Valuing professional services in barter.md` § 3-4 |
| 6 | `schedule-c-barter-income` | Schedule C Barter Income | — | — | `IRS Barter Exchange Platform Tax Rules.md` § 5 |
| 7 | `trade-exchange-network` | Trade Exchange Network | — | — | `Valuing professional services in barter.md` § 2 (IRTA) |
| 8 | `lets-local-employment-trading` | LETS (Local Employment Trading System) | LETS trading system | 900/mo | `Valuing professional services in barter.md` § 3 |
| 9 | `mutual-credit-clearing` | Mutual Credit Clearing | — | — | `Valuing professional services in barter.md` § 2 (IRTA UC) |
| 10 | `fractional-skill-swap` | Fractional Skill Swap | — | emerging | *Concept-based, no specific research needed* |
| 11 | `barter-invoice` | Barter Invoice | how to invoice barter | 400/mo | `Legal requirements for barter service agreements.md` § 6 |
| 12 | `reciprocal-arrangement` | Reciprocal Arrangement | — | — | `IRS Barter Exchange Platform Rules.md` § 2 |
| 13 | `skill-deficit` | Skill Deficit | — | — | *Concept-based* |
| 14 | `credit-liquidity` | Credit Liquidity | — | — | `Valuing professional services in barter.md` § 2 (IRTA deficit guidelines) |
| 15 | `bootstrapping-with-barter` | Bootstrapping with Barter | — | — | `Skill bartering in startups.md` § 1-2 |

**`relatedTerms` linking strategy:**
- `form-1099-b` → `['barter-income', 'fair-market-value', 'barter-agreement']`
- `barter-agreement` → `['barter-valuation', 'scope-creep', 'barter-invoice']`
- `barter-valuation` → `['fair-market-value', 'service-exchange-rate', 'credit-rate']`
- Each new term should link to 2-3 existing terms AND 1-2 other new terms to create internal link density

**Effort:** 2-3 hours | **SEO impact:** +15 indexed pages, Featured Snippet opportunities for `form-1099-b` and `barter-agreement`

---

### A3. Add 6 missing scenario pages (Week 1)

**File:** `web/src/lib/data/scenarios-data.ts`

Add 6 entries following the existing `ScenarioData` interface:

```typescript
interface ScenarioData {
  slug: string
  skillOffered: string
  skillNeeded: string
  title: string
  description: string
  steps: Array<{ name: string; text: string }>
  benefits: string[]
  faqs: Array<{ question: string; answer: string }>
}
```

| # | Slug | Skill Offered | Skill Needed | Research Basis |
|---|------|--------------|--------------|----------------|
| 1 | `legal-for-consulting` | Legal | Consulting | `Bartering legal services.md` (ABA compliance framework) |
| 2 | `consulting-for-web-development` | Consulting | Web Development | `Valuing professional services.md` (rate disparity frameworks) |
| 3 | `video-editing-for-scriptwriting` | Video Production | Writing | *Content creator vertical, concept-based* |
| 4 | `design-for-legal` | Design | Legal | `Bartering legal services.md` + `Valuing professional services.md` |
| 5 | `photography-for-marketing` | Photography | Marketing | *Real estate angle, concept-based* |
| 6 | `seo-for-copywriting` | Marketing | Writing | *Concept-based, high search interest* |

**For each scenario, write:**
- 5-7 steps describing the exchange process
- 4-5 benefits specific to this skill pairing
- 3 FAQs (generates FAQPage schema automatically via existing `generateFAQSchema()`)

**Effort:** 3-4 hours | **SEO impact:** +6 indexed pages with HowTo + FAQ schema

---

### A4. Add 4 new skill categories (Week 1)

**File:** `web/src/lib/data/categories-data.ts`

Add 4 entries following the existing `CategoryData` interface:

```typescript
interface CategoryData {
  slug: string
  name: string
  description: string
  longDescription: string
  sampleSkills: string[]
  averageCreditRate: number
  demandLevel: 'high' | 'medium' | 'low'
  faqs: Array<{ question: string; answer: string }>
}
```

| # | Slug | Name | Credit Rate | Demand | Research Source for FAQs |
|---|------|------|------------|--------|-------------------------|
| 1 | `healthcare-wellness` | Healthcare & Wellness | 90 | medium | `Bartering healthcare services.md` (HIPAA, APA 6.05, massage barter stats) |
| 2 | `real-estate` | Real Estate | 85 | medium | *Concept-based, real estate marketing + photography angle* |
| 3 | `non-profit` | Non-Profit | 65 | low | *Concept-based, Taproot Foundation gap* |
| 4 | `content-creators` | Content Creators | 75 | high | `Five service platforms compared.md` (creator economy data) |

**Healthcare FAQ example** (sourced from research):
- "Is it legal for therapists to barter services?" → Answer citing APA 6.05 and state variations
- "Do I need a HIPAA agreement for healthcare barter?" → Answer with 3 scenarios from research
- "How are bartered healthcare services taxed?" → Answer: identically to cash, per IRS

**Effort:** 3-4 hours | **SEO impact:** +4 indexed pages with FAQ schema, captures new vertical keywords

---

### A5. Write Tax & Legal articles 1, 2, 4, 5, 6, 7 (Weeks 2-4)

> Article 3 (QuickBooks/Xero) is blocked on Research Gap 1.B. Write the other 6 first.

**Location:** `web/content/articles/tax-and-legal/`

Each article is an MDX file with this frontmatter structure:

```yaml
---
title: "Article Title"
description: "160-char meta description targeting primary keyword"
publishedAt: "2026-03-XX"
author: "SkillLedger Team"
silo: "tax-and-legal"
tags: ["barter taxes", "IRS compliance", ...]
draft: false
buyerStage: "decision"  # Tax content targets decision-stage users
relatedSlugs: ["other-article-slug", ...]
---
```

#### Article 1: `barter-income-taxes-freelancer-guide.mdx`

| Attribute | Value |
|-----------|-------|
| **Target keyword** | barter income tax (1,200/mo) |
| **Word count** | 2,500 |
| **Research source** | `IRS Barter Exchange Platform Rules.md` (primary) + `IRS Barter Exchange Platform Tax Rules.md` (supplementary) |
| **Key sections to cover** | IRC § 61 gross income definition, constructive receipt doctrine (Rev. Rul. 80-52), FMV determination (Treas. Reg. § 1.61-2(d)(1)), Schedule C reporting step-by-step, state-level CA/NY/TX differences, self-employment tax on barter income |
| **Unique data from research** | The $1.00 de minimis threshold (Notice 2000-6), the 100-exchange volume exemption, the "no corporate exemption" rule for 1099-B |
| **Internal links** | Glossary: `form-1099-b`, `schedule-c-barter-income`, `fair-market-value`, `barter-income` |
| **relatedSlugs** | `irs-form-1099-b-explained`, `barter-tax-myths-vs-reality`, `how-to-invoice-barter-transaction` |

#### Article 2: `irs-form-1099-b-explained.mdx`

| Attribute | Value |
|-----------|-------|
| **Target keyword** | form 1099-b barter exchange (600/mo) |
| **Word count** | 1,800 |
| **Research source** | `IRS Barter Exchange Platform Rules.md` § 1 (thresholds), `IRS Barter Exchange Platform Tax Rules.md` § 1-2 (credit/scrip definitions) |
| **Key sections** | Box 13 (Bartering) walkthrough, who files (platform vs individual), 100-exchange threshold, $1.00 de minimis, corporate exemption override, double-reporting protection (Treas. Reg. § 1.6045-1(a)(4)), comparison table: 1099-B vs 1099-NEC vs 1099-MISC |
| **Unique data** | Comparison table of IRC § 6041 vs § 6045 reporting regimes (from research) |
| **Internal links** | Glossary: `form-1099-b`, `barter-agreement`; Articles: 1, 6 |

#### Article 4: `barter-contract-templates.mdx`

| Attribute | Value |
|-----------|-------|
| **Target keyword** | service barter agreement template (1,500/mo) |
| **Word count** | 2,000 |
| **Research source** | `Legal requirements for barter service agreements.md` § 1 (5 elements), § 2 (IP/copyright), § 5 (template landscape) |
| **Key sections** | 5 contract elements (offer, acceptance, consideration, capacity, legality) with case law, IP ownership defaults (CCNV v. Reid), why no major org offers free templates (research finding), essential clauses checklist (FMV, scope, IP, termination, tax acknowledgment) |
| **Unique data** | Template source comparison (Rocket Lawyer, UpCounsel, eForms, PandaDoc) with quality assessment from research |
| **CTA opportunity** | "SkillLedger generates compliant agreements automatically" — links to platform signup |
| **Internal links** | Glossary: `barter-agreement`, `scope-creep`, `barter-valuation` |

#### Article 5: `how-to-invoice-barter-transaction.mdx`

| Attribute | Value |
|-----------|-------|
| **Target keyword** | how to invoice for a barter transaction (400/mo) |
| **Word count** | 1,800 |
| **Research source** | `Legal requirements for barter service agreements.md` § 6 (invoice requirements, GAAP journal entries) |
| **Key sections** | Why barter invoices must NOT show $0, FMV display requirements, sample invoice format, GAAP ASC 845 journal entry (debit barter receivable / credit service revenue), 1099 reporting triggers |
| **Unique data** | Step-by-step journal entry table from research, cross-reference format for reciprocal invoices |

#### Article 6: `barter-tax-myths-vs-reality.mdx`

| Attribute | Value |
|-----------|-------|
| **Target keyword** | is bartering taxable (high volume) |
| **Word count** | 2,000 |
| **Research source** | `IRS Barter Exchange Platform Rules.md` § 2 (no-cash-out misconception), § 3 (timing myths), `IRS Barter Exchange Platform Tax Rules.md` § 2-3 |
| **Key myths to debunk** | "No cash = no tax" (WRONG — economic benefit doctrine), "Tax only when you spend credits" (WRONG — Rev. Rul. 80-52 constructive receipt), "Small trades are exempt" (WRONG — only <$1.00 per Notice 2000-6), "Corporations are exempt from 1099-B" (WRONG — Treas. Reg. § 1.6045-1(f)(2)(ii)) |
| **Format** | Myth/Reality pairs with regulatory citations — ideal for Featured Snippet |

#### Article 7: `ip-rights-in-skill-exchange.mdx`

| Attribute | Value |
|-----------|-------|
| **Target keyword** | ip rights barter trade (200/mo) |
| **Word count** | 1,800 |
| **Research source** | `Legal requirements for barter service agreements.md` § 2 (copyright defaults, Work for Hire, implied license) |
| **Key sections** | 17 U.S.C. § 201(a) default ownership, CCNV v. Reid 12-factor test, 9 work-for-hire categories (most creative barter work falls OUTSIDE these), Effects Associates implied license (narrow), why written assignment under § 204(a) is essential |
| **Unique angle** | Most freelancers don't realize that in barter, the creator retains copyright BY DEFAULT — this is counterintuitive and makes great content |

**Effort:** ~2 days per article (research synthesis + writing + internal linking) = 12 working days for 6 articles
**SEO impact:** Captures the entire "barter tax" keyword cluster with zero competitor coverage. Highest-converting content type.

---

### A6. Update sitemap.ts (End of Week 1, then ongoing)

**File:** `web/src/app/sitemap.ts`

No changes needed for glossary, categories, or scenarios — the existing `getGlossaryPages()`, `getCategoryPages()`, and `getScenarioPages()` functions read from data files dynamically. New entries auto-populate.

New articles auto-populate via `getArticlePages()` which calls `getAllArticles()`.

**Action for Phase A:** Verify build succeeds after data additions. No sitemap code changes needed yet.

---

### A7. Update robots.ts for new routes (End of Phase A, prep for B-D)

**File:** `web/src/app/robots.ts`

Add future routes to the AI bot allow list now (they'll 404 until built, which is fine):

```typescript
// Add to AI bot rules allow list:
'/industries/',
'/compare/',
'/trade/',
'/locations/',
'/tools/',
'/resources/templates',
```

**Effort:** 15 min | **SEO impact:** Ensures AI crawlers can access new routes immediately when deployed

---

### Phase A Deliverables Summary

| Deliverable | New Pages | Effort | Priority |
|-------------|-----------|--------|----------|
| Silo enum update | 0 | 15 min | P0 — blocks all articles |
| 15 glossary terms | +15 | 3 hrs | P1 |
| 6 scenario pages | +6 | 4 hrs | P1 |
| 4 category pages | +4 | 4 hrs | P1 |
| 6 Tax & Legal articles | +6 | 12 days | P1 |
| robots.ts update | 0 | 15 min | P2 |
| **Phase A total** | **+31 pages** | **~3 weeks** | |

---

## 3. Phase B — Trust Layer + City Expansion (Weeks 5-8)

### B1. Write Trust & Safety articles 8-11 (Weeks 5-6)

**Location:** `web/content/articles/trust-and-safety/`

#### Article 8: `how-to-avoid-barter-scams.mdx`

| Attribute | Value |
|-----------|-------|
| **Target keyword** | barter scam freelance (2,000 words) |
| **Research source** | `Skill barter arrangements what goes wrong.md` § 1 (5 failure modes with real names) |
| **Unique data** | Named examples: BrianMurphy/DVXuser (ghosting after $5K work), Kevin Ng/LinkedIn (quality dispute), Schindler/Computerworld (scope creep), Goodgold/Medium (deprioritization) |
| **Key sections** | 5 failure modes with real stories, red flags checklist, written agreement requirements, platform-based protections vs DIY barter |
| **CTA** | "SkillLedger's escrow and reputation system prevents these exact problems" |

#### Article 9: `what-happens-if-barter-partner-doesnt-deliver.mdx`

| Attribute | Value |
|-----------|-------|
| **Target keyword** | barter partner dispute resolution (1,800 words) |
| **Research source** | `Skill barter arrangements what goes wrong.md` § 2-4 (legal remedies, exchange policies, Simbi) |
| **Key sections** | Breach of contract elements, unjust enrichment, quantum meruit, small claims court process (evidence needed, limits by state), how commercial exchanges handle disputes (IRTA 3-strikes, ITEX freeze, BarterPays! escrow, BizX 3-step), Simbi's minimal protection |

#### Article 10: `how-escrow-works-skill-exchange.mdx`

| Attribute | Value |
|-----------|-------|
| **Target keyword** | escrow for barter (1,600 words) |
| **Research source** | `Skill barter arrangements what goes wrong.md` § 3-4 (BarterPays! escrow, ITEX freeze, Simbi pending) + SkillLedger product knowledge |
| **Key sections** | Why milestone-based escrow solves barter's trust problem, how BarterPays! is the only exchange with explicit escrow, how SkillLedger's credit escrow works, comparison: no escrow vs platform escrow |

#### Article 11: `how-to-assess-barter-partner-portfolio.mdx`

| Attribute | Value |
|-----------|-------|
| **Target keyword** | vetting barter partner (1,600 words) |
| **Research source** | `Skill barter arrangements what goes wrong.md` § 5 (vetting checklist) |
| **Key sections** | Portfolio quality signals, communication speed indicators, platform history review, trial-project strategy, reference checking |

**Effort:** 8 working days (2 per article)
**SEO impact:** Displaces Reddit horror stories from SERP positions. Addresses #1 conversion objection.

---

### B2. Write Barter Valuation articles 12-16 (Weeks 6-8)

**Location:** `web/content/articles/barter-economy/` (extends existing silo)

#### Article 12: `how-to-value-services-barter.mdx`

| Attribute | Value |
|-----------|-------|
| **Target keyword** | how to value a service for a barter trade (2,500 words) |
| **Research source** | `Valuing professional services in barter.md` § 1 (IRS standard), § 4 (4 frameworks) |
| **Key sections** | IRS FMV definition (Treas. Reg. § 1.61-2(d)(1)), Rev. Rul. 79-24 lawyer-painter example, stipulated price presumption, 4 valuation frameworks (dollar-for-dollar, hour-for-hour, value-based, hybrid), when each framework is appropriate, worked example: $400/hr lawyer vs $75/hr designer |
| **Unique angle** | No other page on the internet presents these 4 frameworks side-by-side with IRS citations |

#### Article 13: `time-vs-value-fallacy-skill-exchange.mdx`

| Attribute | Value |
|-----------|-------|
| **Target keyword** | is hour for hour trading fair (2,000 words) |
| **Research source** | `Valuing professional services in barter.md` § 3 (time banking tension), § 4 (Shih et al. 2015 CHI study) |
| **Key sections** | Why 1hr=1hr feels fair but isn't (Shih et al. found tension between instrumental vs idealistic motivations), the $400/hr lawyer problem, how credits solve the double coincidence of wants, why IRTA pegs 1 trade dollar = 1 USD |

#### Article 14: `barter-system-vs-credit-system.mdx`

| Attribute | Value |
|-----------|-------|
| **Target keyword** | barter system vs credit system (1,800 words) |
| **Research source** | `Valuing professional services in barter.md` § 2 (IRTA, BizX, ITEX mechanics) |
| **Key sections** | Double coincidence of wants problem, how credits solve it, IRTA Quantity Theory of Money framework, 1:1 peg enforcement mechanisms, comparison table: direct barter vs time banking vs credit exchange |

#### Article 15: `multi-party-barter-trade.mdx`

| Attribute | Value |
|-----------|-------|
| **Target keyword** | multi party barter trade (2,000 words) |
| **Research source** | `Valuing professional services in barter.md` § 2 (IRTA Universal Currency, Sardex cascade example) |
| **Key sections** | Why 2-party barter fails at scale, IRTA UC connecting 100+ exchanges globally ($14.5M record in 2017), Sardex cascade (restaurant→accountant→printer→supplier), how SkillLedger credits enable N-party trades |

#### Article 16: `state-of-barter-economy-2026.mdx`

| Attribute | Value |
|-----------|-------|
| **Target keyword** | barter economy 2026 (3,000 words) |
| **Research source** | `The barter economy's data desert.md` (ALL sections) |
| **Key sections** | The data desert IS the story — frame it as investigative journalism. IRS publishes no barter-specific data. IRTA's $12-14B estimate has no methodology. BLS doesn't track barter. Freelance workforce sizing (72.9M per MBO 2025, 64M per Upwork 2023). Counter-cyclical evidence (Stodder on WIR, Marvasti & Smyth). COVID impact (Mattsson et al. 2023 Kenya data). Wong (2026) Toronto Bunz barter community. |
| **Unique angle** | First-ever comprehensive accounting of what barter data exists and doesn't. Establishes SkillLedger as the authority. Annual refresh opportunity. |
| **Link magnet** | This is the #1 backlink-earning article — journalists and researchers will cite it. |

**Effort:** 10 working days
**SEO impact:** Owns the "barter valuation" cluster. Article 16 is the primary backlink magnet.

---

### B3. Add 25 new cities to cities-data.ts (Week 5)

**File:** `web/src/lib/data/cities-data.ts`

Add 25 entries following the existing `CityData` interface. Full city list is in `CONTENT_SEO_STRATEGY.md` § 5.1.

**For each city, write:**
- `topSkills`: 5 skills based on city's industry profile (from strategy doc rationale)
- `faqs`: 3 FAQs per city:
  1. "What skills trade best in {City}'s skill-exchange economy?"
  2. "How does SkillLedger compare to local barter networks in {City}?"
  3. "Is there an active skill-swap community in {City}?"

**Effort:** 1 day (data entry + city-specific FAQ writing)
**SEO impact:** +25 indexed pages, doubles city coverage to 50

---

### B4. Enhance existing 25 city page FAQs (Week 5)

**File:** `web/src/lib/data/cities-data.ts`

Add the same 3 enhanced FAQ questions to all 25 existing city entries. Customize answers per city.

**Effort:** 1 day
**SEO impact:** Enriches existing pages, improves FAQ schema coverage

---

### B5. Build `/industries/[slug]` route + data (Weeks 7-8)

**New files to create:**

1. **Data file:** `web/src/lib/data/industries-data.ts`
2. **Page component:** `web/src/app/industries/[slug]/page.tsx`
3. **Index page:** `web/src/app/industries/page.tsx`

**IndustryData interface:**

```typescript
export interface IndustryData {
  slug: string
  name: string
  h1Title: string
  description: string
  longDescription: string
  keyBenefits: string[]
  commonPairings: Array<{ skillOffered: string; skillNeeded: string; description: string }>
  regulatoryNotes: string     // from research — legal/compliance considerations
  faqs: Array<{ question: string; answer: string }>
}
```

**10 industry entries** (full specs in `CONTENT_SEO_STRATEGY.md` § 6.1):

| # | Slug | Research Source for `regulatoryNotes` |
|---|------|--------------------------------------|
| 1 | `legal-professionals` | `Bartering legal services.md` — ABA Rules 1.8(a)/1.5/1.7, IOLTA limitations |
| 2 | `healthcare-wellness` | `Bartering healthcare services.md` — HIPAA scenarios, APA 6.05, state variations |
| 3 | `non-profit-organizations` | *Concept-based — Taproot Foundation gap* |
| 4 | `saas-startups` | `Skill bartering in startups.md` — GAAP ASC 845, SEC boundary |
| 5 | `creative-agencies` | *Concept-based* |
| 6 | `content-creators` | `Five service platforms compared.md` — creator economy data |
| 7 | `local-small-businesses` | *Concept-based* |
| 8 | `real-estate-professionals` | *Concept-based* |
| 9 | `independent-consultants` | `Valuing professional services.md` — rate disparity frameworks |
| 10 | `ecommerce-brands` | *Concept-based* |

**Schema:** Service + FAQPage + BreadcrumbList (use existing `generateFAQSchema()` and `generateBreadcrumbSchema()`, add new `generateServiceSchema()` to `seo.ts`)

**Sitemap update:** Add `getIndustryPages()` to `sitemap.ts`

**Effort:** 3-4 days (route + data + 10 entries)
**SEO impact:** +10 indexed pages, captures industry-specific long-tail keywords

---

### Phase B Deliverables Summary

| Deliverable | New Pages | Effort | Priority |
|-------------|-----------|--------|----------|
| Trust & Safety articles (8-11) | +4 | 8 days | P1 |
| Valuation articles (12-16) | +5 | 10 days | P1 |
| 25 new cities | +25 | 1 day | P1 |
| 25 existing city FAQ enhancements | 0 | 1 day | P2 |
| Industry route + 10 pages | +10 | 4 days | P2 |
| **Phase B total** | **+44 pages** | **~5 weeks** | |

---

## 4. Phase C — Comparison + Startup Content (Weeks 9-14)

### C1. Write Startup articles 17-20 (Weeks 9-10)

**Location:** `web/content/articles/freelancing/` (extends existing silo)

| Article | File | Words | Research Source |
|---------|------|-------|----------------|
| 17 | `how-to-build-mvp-without-cash.mdx` | 2,500 | `Skill bartering in startups.md` § 1 (Tata Tickaradze, Jared Krause, Publicize Swap Shop) |
| 18 | `barter-credits-vs-startup-equity.mdx` | 2,000 | `Skill bartering in startups.md` § 3-5 (SEC Howey Test, Paul Graham equity equation, Carta H1 2024 median advisor equity 0.13%, community consensus) |
| 19 | `zero-cash-go-to-market.mdx` | 2,000 | `Skill bartering in startups.md` § 1 (Cream City Music $50K ad barter, case studies) |
| 20 | `run-a-business-on-barter-model.mdx` | 3,000 | `Skill bartering in startups.md` + `Valuing professional services.md` § 5 (IRTA 10-15% of sales recommendation, cost-of-trade-dollars concept) |

**Effort:** 8 days
**SEO impact:** Captures pre-seed founder demographic searching for cashless alternatives

---

### C2. Write Comparison articles 21-24 (Weeks 11-12)

**Location:** `web/content/articles/skill-exchange/` (extends existing silo)

| Article | File | Words | Research Source |
|---------|------|-------|----------------|
| 21 | `skillledger-vs-simbi.mdx` | 2,000 | `Five service platforms compared.md` § Simbi (density problem, 501(c)(3), no dispute resolution, verbatim complaints) |
| 22 | `skill-barter-vs-cash-freelancing.mdx` | 2,000 | `Five service platforms compared.md` § all platforms (fee comparison table, effective take rates) |
| 23 | `time-banking-vs-skill-exchange.mdx` | 1,800 | `Valuing professional services.md` § 3 (Edgar Cahn, hOurworld, Shih et al. 2015 tension findings, IRS non-taxable ruling for time banks) |
| 24 | `alternatives-to-thumbtack-freelancers.mdx` | 2,000 | `Five service platforms compared.md` § Thumbtack + Bark (lead costs, conversion rates, verbatim complaints from Trustpilot/BBB/Reddit) |

**Effort:** 8 days
**SEO impact:** Captures branded comparison queries and high-commercial-intent "alternatives to" searches (850/mo for Thumbtack alternatives)

---

### C3. Build `/compare/[slug]` route + data (Week 12)

**New files:**
1. `web/src/lib/data/comparisons-data.ts`
2. `web/src/app/compare/[slug]/page.tsx`

**ComparisonData interface:**

```typescript
export interface ComparisonData {
  slug: string
  title: string
  description: string
  sideA: { name: string; strengths: string[]; weaknesses: string[]; pricing: string }
  sideB: { name: string; strengths: string[]; weaknesses: string[]; pricing: string }
  verdict: string
  faqs: Array<{ question: string; answer: string }>
}
```

**7 comparison pages** (specs in `CONTENT_SEO_STRATEGY.md` § 6.2). Data sourced from `Five service platforms compared.md`.

**Schema:** Article + FAQPage (use existing generators)
**Sitemap update:** Add `getComparisonPages()` to `sitemap.ts`

**Effort:** 2 days
**SEO impact:** +7 indexed pages targeting branded and unbranded comparison queries

---

### C4. Build Barter Valuation Calculator (Weeks 13-14)

**New files:**
1. `web/src/app/tools/barter-valuation-calculator/page.tsx`
2. `web/src/components/tools/BarterCalculator.tsx`

**Implementation:**
- Pure frontend React component — no backend needed
- Uses `averageCreditRate` from `categories-data.ts` (now 19 categories)
- Input: Select skill category A + hourly rate → Select skill category B + hourly rate
- Output: Credit equivalency, hours required from each party, FMV for tax purposes
- Add `SoftwareApplication` or `WebApplication` schema

**Research basis for calculator logic:**
- `Valuing professional services.md` § 1: IRS FMV = provider's normal retail rate
- `Valuing professional services.md` § 4: Dollar-for-dollar framework (IRS-mandated)
- Calculator implements the dollar-for-dollar ratio: `hours_B = (rate_A * hours_A) / rate_B`

**Effort:** 3-4 days
**SEO impact:** Highest-opportunity tool in the sector — no competitor has built this. Earns backlinks and Featured Snippet placement.

---

### C5. Create downloadable templates + landing page (Week 14)

**New files:**
1. `web/src/app/resources/templates/page.tsx`
2. `web/public/downloads/` (PDF files)

**5 templates to create** (based on `Legal requirements for barter service agreements.md` § 1-6):
1. Non-Monetary Service Agreement — 5 contract elements + IP clause + FMV + tax acknowledgment
2. Barter-Specific NDA — modified from standard NDA per research § 2
3. Statement of Work for Skill Swaps — scope definition, hourly rates, change-order procedure
4. Zero-Balance Barter Invoice — FMV display, payment method notation, cross-reference
5. Scope Change Addendum — per research § 3 (Angel v. Murray standard, 5-10% threshold)

**Email gate:** Requires email to download. Feeds top-of-funnel list.

**Effort:** 2-3 days (template creation + landing page)
**SEO impact:** Captures "barter contract template" (1,500/mo) with gated lead magnet

---

### Phase C Deliverables Summary

| Deliverable | New Pages | Effort | Priority |
|-------------|-----------|--------|----------|
| Startup articles (17-20) | +4 | 8 days | P1 |
| Comparison articles (21-24) | +4 | 8 days | P1 |
| Comparison route + 7 pages | +7 | 2 days | P2 |
| Valuation calculator | +1 | 4 days | P1 |
| Template library + landing | +1 | 3 days | P2 |
| **Phase C total** | **+17 pages** | **~5 weeks** | |

---

## 5. Phase D — Programmatic Scale (Weeks 15-20)

### D1. Write Industry articles 25-30 (Weeks 15-16)

**Location:** `web/content/articles/industries/` (new silo)

| Article | File | Words | Research Source |
|---------|------|-------|----------------|
| 25 | `saas-startup-barter-guide.mdx` | 2,000 | `Skill bartering in startups.md` + product knowledge |
| 26 | `legal-professionals-barter-guide.mdx` | 2,000 | `Bartering legal services.md` (ABA framework, state opinions, IOLTA, Rev. Rul. 79-24, Badell v. Commissioner) |
| 27 | `non-profit-skill-exchange-guide.mdx` | 1,800 | *Concept-based — Taproot gap* |
| 28 | `healthcare-barter-guide.mdx` | 1,800 | `Bartering healthcare services.md` (5 regulatory domains, APA 6.05, HIPAA scenarios, Medicare fraud exposure, massage therapist stats) |
| 29 | `creative-agency-skill-swap-guide.mdx` | 1,800 | *Concept-based* |
| 30 | `content-creator-skill-exchange-guide.mdx` | 1,800 | *Concept-based* |

**Effort:** 10 days
**SEO impact:** Feeds industry vertical pages with deep-linked supporting content

---

### D2. Build `/trade/[skill-a]/for/[skill-b]` route (Weeks 17-18)

**New files:**
1. `web/src/app/trade/[skillA]/for/[skillB]/page.tsx`

**Implementation:**
- `generateStaticParams()` cross-multiplies 19 category slugs: 19 × 18 = 342 unique pages (excluding self-pairs)
- Each page shows: skill descriptions from `categories-data.ts`, credit rate equivalency, step-by-step trade process, 3 FAQs, CTA to post/browse this trade
- Template-driven — write ONE page component, data from existing `categoriesData`
- No new data file needed — combinatorial generation

**Schema:** Service + FAQPage per page

**Effort:** 3-4 days (page component + template logic)
**SEO impact:** +342 indexed pages. Highest-conversion content type — captures "trade X for Y" queries with zero competition

---

### D3. Build `/locations/[city]/[skill]` route (Weeks 18-19)

**New files:**
1. `web/src/app/locations/[city]/[skill]/page.tsx`

**Implementation:**
- `generateStaticParams()` cross-multiplies 50 cities × 19 categories = 950 pages
- Each page shows: city info from `cities-data.ts`, category info from `categories-data.ts`, local demand narrative, FAQs, CTA
- Template-driven — one component, data from existing sources

**Schema:** LocalBusiness + AggregateRating + BreadcrumbList

**New schema function needed in `seo.ts`:**

```typescript
function generateLocalBusinessSchema(city: CityData, category: CategoryData) {
  return {
    '@type': 'LocalBusiness',
    name: `SkillLedger — ${category.name} in ${city.city}`,
    areaServed: { '@type': 'City', name: city.city, addressRegion: city.state },
    url: `https://skillledger.app/locations/${city.slug}/${category.slug}`,
  }
}
```

**Effort:** 3-4 days
**SEO impact:** +950 indexed pages. The Thumbtack playbook for skill exchange. LocalBusiness schema generates star ratings in SERPs.

---

### D4. Add missing schema to existing pages (Week 19)

**City pages** — add LocalBusiness schema:
- **File:** `web/src/app/skill-exchange/[city]/page.tsx`
- Add `generateLocalBusinessSchema()` call

**Category pages** — add Service schema:
- **File:** `web/src/app/categories/[slug]/page.tsx`
- Add new `generateServiceSchema()` to `seo.ts` and call it

**Effort:** 1 day
**SEO impact:** Enriches 50 city + 19 category pages with additional schema types

---

### D5. Write Article 3 (QuickBooks tutorial) — requires Gap 1.B research (Week 20)

**Prerequisite:** Complete Research Gap 1.B (QuickBooks/Xero step-by-step workflows).

Once researched:
- **File:** `web/content/articles/tax-and-legal/how-to-record-barter-transactions-quickbooks.mdx`
- 2,000 words, targeting "how to record barter transactions in quickbooks" (800/mo)
- Step-by-step with field-by-field instructions for QBO and Xero

---

### D6. Update llms.txt with new content (Week 20)

**File:** `web/public/llms.txt`

Add sections for:
- Tax & Legal resources (articles 1-7)
- Trust & Safety resources (articles 8-11)
- Industry guides (articles 25-30)
- Comparison pages
- Valuation calculator tool
- Template library

Create `web/public/llms-full.txt` with comprehensive content for large-context LLMs.

**Effort:** 1-2 hours
**SEO impact:** Ensures AI crawlers discover all new content via the llms.txt protocol

---

### Phase D Deliverables Summary

| Deliverable | New Pages | Effort | Priority |
|-------------|-----------|--------|----------|
| Industry articles (25-30) | +6 | 10 days | P1 |
| Skill-pairing route | +342 | 4 days | P1 |
| City-skill route | +950 | 4 days | P1 |
| Missing schema on existing pages | 0 | 1 day | P2 |
| Article 3 (blocked on research) | +1 | 2 days | P2 |
| llms.txt update | 0 | 2 hrs | P2 |
| **Phase D total** | **+1,299 pages** | **~4 weeks** | |

---

## 6. Phase E — International (Post Week 20)

### E1. Canadian cities (Toronto, Vancouver, Waterloo)

Add to `cities-data.ts` with Canada-specific FAQs (CRA barter tax treatment differs from IRS).

### E2. UK cities (London, Manchester)

Add with HMRC barter tax treatment and NHS healthcare angle.

**Effort:** 1-2 days per country
**SEO impact:** Opens new geographic keyword markets with minimal competition

---

## 7. Technical Infrastructure Tasks

These run in parallel with content creation.

### T1. Add Service schema generator to seo.ts (Phase A)

```typescript
export function generateServiceSchema(service: { name: string; serviceType: string; areaServed?: string }) {
  return {
    '@context': 'https://schema.org',
    '@type': 'Service',
    name: service.name,
    provider: { '@type': 'Organization', name: 'SkillLedger' },
    serviceType: service.serviceType,
    areaServed: service.areaServed || 'Worldwide',
  }
}
```

### T2. Add LocalBusiness schema generator to seo.ts (Phase D)

See D3 above.

### T3. Add sitemap functions for new routes (Phase B-D)

Add to `sitemap.ts`:
- `getIndustryPages()` — reads from `industries-data.ts` (Phase B)
- `getComparisonPages()` — reads from `comparisons-data.ts` (Phase C)
- `getSkillPairingPages()` — combinatorial from `categories-data.ts` (Phase D)
- `getCitySkillPages()` — combinatorial from `cities-data.ts × categories-data.ts` (Phase D)

### T4. Internal linking strategy

Every new article should include:
- 2-3 glossary term links (using markdown links to `/glossary/[term]`)
- 1-2 related article links via `relatedSlugs` frontmatter
- 1 CTA link to relevant platform page (marketplace, register, or calculator)
- Cross-silo links where relevant (tax article → trust article → industry article)

### T5. Information Gain audit per article

Before publishing each article, verify it contains at least ONE of:
- Specific regulatory citation not in any competing page (IRS section numbers, case names)
- Named real-world example with source (forum anecdote, case study)
- Original data or comparison table not available elsewhere
- Proprietary framework or tool (calculator, template)

This is required by the 2026 SQRG "Information Gain" standard.

---

## 8. Content Quality Standards

### Per-Article Checklist

- [ ] Frontmatter complete (title, description ≤160 chars, publishedAt, silo, tags, buyerStage, relatedSlugs)
- [ ] Target keyword appears in: H1, first paragraph, 1-2 H2s, meta description
- [ ] At least 2 glossary internal links
- [ ] At least 1 related article link via `relatedSlugs`
- [ ] Information Gain present (novel data, named example, or regulatory citation not in competing pages)
- [ ] No AI-detectable patterns (avoid "delve," "tapestry," "landscape," excessive em dashes, rule-of-three)
- [ ] FAQPage-eligible section with 3+ Q&A pairs in clear format
- [ ] CTA to platform feature (marketplace, register, calculator, templates)
- [ ] Word count meets spec (±10%)
- [ ] All regulatory citations verified against source research document

### Research Citation Format in Articles

When citing research in MDX articles, use this pattern:
```markdown
According to [IRS Publication 525](https://www.irs.gov/publications/p525), barter income must be reported at fair market value...
```

Do NOT include footnote numbers or academic-style references — these signal AI-generated content. Use natural inline citations with links.

---

## 9. SEO Technical Checklist Per Page Type

| Page Type | JSON-LD Schema | Priority | Internal Links Target |
|-----------|---------------|----------|----------------------|
| MDX articles | Article + BreadcrumbList | existing ✅ | 3 glossary + 2 articles |
| Glossary terms | DefinedTerm + BreadcrumbList | existing ✅ | 2-3 related terms |
| Categories | Service + FAQPage + BreadcrumbList | Service needed ⚠️ | 3 articles + 2 scenarios |
| City pages | LocalBusiness + FAQPage + BreadcrumbList | LocalBusiness needed ⚠️ | 2 category + 1 article |
| How-to scenarios | HowTo + FAQPage + BreadcrumbList | existing ✅ | 2 categories + 1 article |
| Industry pages | Service + FAQPage + BreadcrumbList | **new route** | 2 articles + 2 categories |
| Comparison pages | Article + FAQPage | **new route** | 2 articles + 1 glossary |
| Skill-pairing pages | Service + FAQPage | **new route** | 2 categories + 1 scenario |
| City-skill pages | LocalBusiness + AggregateRating + BreadcrumbList | **new route** | 1 city + 1 category |

---

## 10. Measurement Framework

### Weekly Metrics (Start Week 2)

- Pages indexed (Google Search Console → Index Coverage)
- Rich results generated (GSC → Enhancements → FAQ, HowTo, etc.)
- Impressions for target keyword clusters (GSC → Performance)

### Monthly Metrics (Start Month 2)

- Organic clicks by content cluster (tax-legal, trust-safety, valuation, startup, comparison, industry)
- Featured Snippet captures (track manually for top 15 keywords)
- AI citation share — check ChatGPT, Perplexity, Gemini for brand mentions on target queries

### Quarterly Metrics (Start Month 4)

- Domain authority growth
- Backlinks earned (especially for Article 16 annual report and calculator tool)
- Email captures from template downloads
- Signup attribution from /resources/ pages

### Target Keywords to Monitor

| Keyword | Volume | Target Position | Phase |
|---------|--------|-----------------|-------|
| barter income tax | 1,200 | Top 3 | A |
| service barter agreement template | 1,500 | Top 3 | A |
| form 1099-b barter exchange | 600 | Featured Snippet | A |
| how to value a service for a barter trade | 1,200 | Featured Snippet | B |
| barter scam freelance | — | Top 5 | B |
| alternatives to thumbtack | 850 | Top 5 | C |
| time banking vs bartering | 500 | Top 3 | C |
| skill exchange platform | 2,400 | Top 10 | D |
| trade web design for seo | 450 | Top 3 | D |

---

## Appendix: Full Timeline at a Glance

```
Week 1     ████ Silo enum + glossary + scenarios + categories + robots.ts
Week 2-4   ████████████ Tax & Legal articles (1, 2, 4, 5, 6, 7)
Week 5     ████ 25 new cities + existing city FAQ enhancements
Week 5-6   ████████ Trust & Safety articles (8-11)
Week 6-8   ██████████ Valuation articles (12-16)
Week 7-8   ████████ Industry route + 10 pages
Week 9-10  ████████ Startup articles (17-20)
Week 11-12 ████████ Comparison articles (21-24) + comparison route
Week 13-14 ████████ Valuation calculator + template library
Week 15-16 ██████████ Industry articles (25-30)
Week 17-18 ████████ Skill-pairing route (342 pages)
Week 18-19 ████████ City-skill route (950 pages)
Week 19    ████ Schema additions to existing pages
Week 20    ████ Article 3 (if research done) + llms.txt update
```

**Total new indexed pages: ~1,391**
**Total effort: ~20 weeks**
**Articles ready to write NOW: 29 of 30**
