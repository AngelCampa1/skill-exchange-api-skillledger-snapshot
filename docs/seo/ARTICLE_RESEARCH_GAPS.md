# Article Research Gaps — SkillLedger SEO Content

> Documents the specific research needed before each article can be written with accuracy and authority. Cross-references with `CONTENT_SEO_STRATEGY.md` article roadmap.

---

## How to Use This Document

Before writing any article in the 30-article roadmap, check this document for:
1. **Required research** — facts/data/legal specifics needed before writing
2. **Gemini prompt** — copy-paste prompt to gather that research
3. **Status** — whether the research has been done

Once research is gathered, paste results back and request article writing. Articles without research done should not be written — accuracy matters for E-E-A-T signals (Google's Experience, Expertise, Authoritativeness, Trustworthiness).

---

## Cluster 1: Tax & Legal (Articles 1–7)

### ⚠️ CRITICAL: Cluster-Wide Legal Review Needed

**All 7 tax/legal articles require the following foundational research before any writing begins.** This is not optional — publishing inaccurate tax guidance creates legal liability and destroys E-E-A-T.

---

### Research Gap 1.A — IRS Rules for Barter Exchange Platforms

**Needed for**: Articles 1, 2, 6 (barter income taxes, 1099-B, tax myths)

**Specific unknowns**:
- When exactly does a platform become a "commercial barter exchange" under IRS rules?
- Is SkillLedger's credit system a "barter exchange" under IRC § 6045(c)?
- What are the exact 1099-B filing thresholds and timing requirements?
- What is the IRS's current position on digital credit systems (e.g., do SkillLedger credits = barter income at issuance, at use, or at redemption)?
- How is fair market value determined for professional services bartered?

**Key IRS sources to cite**:
- IRS Publication 525 (Taxable and Nontaxable Income) — barter section
- IRS Revenue Procedure 96-52 (barter exchange reporting)
- IRC § 6045(c) and (d) — definitions and obligations
- IRS Topic 420 (Bartering Income)
- Any IRS guidance issued 2020–2025 specific to digital/credit barter systems

**Gemini Research Prompt**:
```
Research the exact IRS rules for barter exchange platforms in the United States. I need:

1. The legal definition of a "commercial barter exchange" under IRC § 6045(c) — what thresholds or characteristics trigger this classification?
2. Whether a platform using an internal credit system (where credits can only be spent on the platform, not cashed out) constitutes a "barter exchange" requiring 1099-B filings
3. The exact IRS position on when barter income is recognized: at the time of the exchange, at receipt of services, or at redemption of credits?
4. How IRS requires fair market value to be determined for professional services (not goods) in a barter
5. Any IRS guidance, PLRs, or revenue rulings from 2015–2025 specifically addressing digital/platform credit systems and their barter tax treatment
6. State-level treatment differences in California, New York, and Texas specifically
7. What a freelancer must include on Schedule C for barter income — step by step

Format: detailed technical summary with specific IRS publication/section citations for each point.
```

**Status**: ✅ **DONE** — See `docs/research/IRS Barter Exchange Platform Rules.md` and `docs/research/IRS Barter Exchange Platform Tax Rules.md`. Both documents provide exhaustive coverage of IRC § 6045(c) definitions, credit system treatment, constructive receipt doctrine (Rev. Rul. 80-52), FMV determination for professional services, state-level treatment (CA, NY, TX), and Schedule C reporting mechanics.

---

### Research Gap 1.B — QuickBooks/Xero Barter Transaction Methods

**Needed for**: Article 3 (how to record barter in QuickBooks)

**Specific unknowns**:
- What is the current correct method in QuickBooks Online to record a $0 barter transaction while still tracking imputed income?
- Does Xero handle this differently than QuickBooks?
- What chart of accounts structure is recommended for a freelancer doing barter?
- Are there any QuickBooks or Xero official help articles covering barter?
- What tax reports does this method generate, and are they 1099-ready?

**Gemini Research Prompt**:
```
How should a freelancer record barter transactions in QuickBooks Online and Xero for IRS compliance? I need:

1. Step-by-step instructions for creating a "Barter Clearing Account" in QuickBooks Online
2. How to create an invoice at full FMV and then apply a "Barter Exchange" credit to zero the balance — with screenshots or field-by-field instructions
3. How to do the same in Xero
4. What the resulting tax reports look like (P&L impact, 1099 generation)
5. Any official QuickBooks or Xero documentation, blog posts, or community answers specifically addressing barter
6. Common mistakes freelancers make when recording barter in accounting software

Format: step-by-step tutorial format with clear numbered steps, suitable for a non-accountant freelancer audience.
```

**Status**: ⬜ **NOT RESEARCHED** — This is the only remaining research gap. The Legal requirements research covers GAAP/ASC 845 journal entries and barter invoice formatting, but specific QuickBooks Online and Xero step-by-step workflows are still needed before Article 3 can be written.

---

### Research Gap 1.C — Barter Contract Legal Requirements

**Needed for**: Articles 4, 5, 7 (contract templates, invoice templates, IP rights)

**Specific unknowns**:
- What clauses must a barter service agreement contain to be legally enforceable?
- Who holds IP rights to work delivered in a barter trade by default (Work for Hire doctrine applies?)
- How does scope creep affect a barter agreement — what happens when one party delivers more than agreed?
- What jurisdiction do barter agreements fall under — contract law, or something else?
- Are there any standard barter contract templates from legal organizations (ABA, NOLO, LegalZoom)?
- How should a barter invoice format the FMV exchange to satisfy IRS and accounting requirements?

**Gemini Research Prompt**:
```
Research the legal requirements for barter service agreements between professionals in the United States. I need:

1. The 5 legally required clauses in a barter/service-exchange contract (offer, acceptance, consideration, etc.) — and how each applies when no money changes hands
2. How US copyright law (Work for Hire doctrine) applies to creative services delivered in a barter — who owns the copyright by default if the contract is silent?
3. How to handle scope creep in a barter agreement — what are the legal options if one party delivers 20% more than agreed?
4. Whether a barter agreement needs to be in writing to be enforceable (state-by-state if it varies)
5. Any existing free/open-source barter contract templates from legal organizations
6. How a zero-dollar invoice for barter should be formatted to satisfy both IRS FMV requirements and standard accounting practices
7. How a barter NDA differs from a standard NDA

Sources to include: Nolo.com, ABA, specific state bar guidance, and any legal databases.
```

**Status**: ✅ **DONE** — See `docs/research/Legal requirements for barter service agreements in the US.md`. Covers all 5 contract elements with case law (Hamer v. Sidway, Batsakis v. Demotsis), Work for Hire doctrine (CCNV v. Reid 12-factor test, 9 enumerated categories), scope creep remedies (unjust enrichment, quantum meruit, contract modification), Statute of Frauds state-by-state analysis, free template sources (eForms, Rocket Lawyer, UpCounsel, PandaDoc), and barter invoice FMV formatting requirements under GAAP ASC 845/606.

---

## Cluster 2: Trust & Safety (Articles 8–11)

### Research Gap 2.A — Real Barter Dispute Cases and Resolution Patterns

**Needed for**: Articles 8, 9 (scam avoidance, what happens when barter fails)

**Specific unknowns**:
- What are the most common failure modes in skill barter (documented in Reddit, Quora, forums)?
- Are there small claims court cases involving failed barter arrangements for services?
- How do existing barter platforms (Simbi, commercial barter exchanges) handle dispute resolution?
- What escrow mechanisms do professional service barter platforms use?
- What's the legal recourse if a barter partner delivers substandard work?

**Gemini Research Prompt**:
```
Research documented failure cases and dispute resolution in professional service bartering. I need:

1. The 5 most common ways skill barter arrangements fail — sourced from Reddit (r/freelance, r/entrepreneur), Quora, and any legal forums, with real examples
2. Whether failed barter arrangements can be taken to small claims court — and what the legal basis would be (contract breach, unjust enrichment?)
3. How existing commercial barter exchanges (IRTA members, Bartercard) handle dispute resolution — their official policies
4. How Simbi.com handles disputes between users — from their terms of service and community posts
5. What "escrow for services" looks like in practice — platforms that use milestone-based service escrow and how it works
6. Practical vetting checklist: what signals indicate a trustworthy barter partner vs. a risky one (portfolio quality, communication speed, platform history)?

Format: concrete, specific examples with sources. Real anecdotes from forums preferred over generic advice.
```

**Status**: ✅ **DONE** — See `docs/research/Skill barter arrangements what goes wrong and how to protect yourself.md`. Covers 5 failure modes with real named examples (BrianMurphy/DVXuser ghosting, Kevin Ng/LinkedIn quality disputes, Schindler/Computerworld scope creep), small claims court legal framework (breach of contract, unjust enrichment, quantum meruit), commercial exchange dispute policies (IRTA three-strikes, Bartercard 48-hour window, ITEX freeze mechanism, BarterPays! escrow, BizX 3-step), Simbi's minimal protections, and practical vetting checklist.

---

## Cluster 3: Barter Valuation (Articles 12–16)

### Research Gap 3.A — Service Valuation Frameworks

**Needed for**: Articles 12, 13 (how to value services, hour-for-hour fallacy)

**Specific unknowns**:
- Are there established professional frameworks for valuing services in non-monetary exchanges?
- What do financial professionals recommend for "fair market value" of professional services?
- Is there data on typical credit rate equivalencies between different professional skills (e.g., 1 hour of senior dev = X hours of copywriting)?
- How do commercial barter exchanges (Bartercard, IRTA) set trade credit rates?
- Any academic research on value-based vs. time-based pricing in professional services?

**Gemini Research Prompt**:
```
Research frameworks and data for valuing professional services in barter trades. I need:

1. How the IRS defines "fair market value" for professional services specifically (not goods) — the specific test used
2. How IRTA-member commercial barter exchanges set trade dollar values for services — their methodology
3. Any published credit rate equivalency tables between professional skill types (e.g., legal consulting vs. web development vs. copywriting)
4. Academic or professional research on value-based pricing vs. hourly pricing for freelancers — specifically when one professional's hourly rate is significantly different from another's
5. Real examples: what does a senior software engineer typically "charge" in credits when trading with a junior designer? Are there any documented barter rate guides?
6. The "Double Coincidence of Wants" problem in service barter — academic sources and practical solutions

Format: reference-ready with citations. Looking for authoritative sources (IRS, IRTA, academic journals, financial planning associations).
```

**Status**: ✅ **DONE** — See `docs/research/Valuing professional services in barter frameworks, authorities, and documented practice.md`. Covers IRS FMV standard (Treas. Reg. § 1.61-2(d)(1)), Rev. Rul. 79-24 and 80-52, IRTA 1:1 trade dollar peg and Quantity Theory of Money framework, BizX/ITEX/IMS enforcement mechanisms, why no published credit rate equivalency tables exist, 4 academic frameworks for rate disparities (dollar-for-dollar, hour-for-hour, value-based, hybrid), time banking comparison (Cahn, Shih et al. 2015 CHI study), Kranton (1996) reciprocal exchange theory, and real-world exchange examples (Sardex, ITEX Chicagoland).

---

### Research Gap 3.B — "State of the Barter Economy 2026" Data

**Needed for**: Article 16 (annual industry report)

**Specific unknowns**:
- What IRS data exists on barter exchange volume (Form 1099-B aggregate filings)?
- BLS data on gig/freelance workforce size and growth 2020–2025
- Any survey data on freelancer alternative payment preferences
- Academic research on non-monetary exchange growth during inflationary periods
- IRTA (International Reciprocal Trade Association) annual trade volume statistics

**Gemini Research Prompt**:
```
Research publicly available data on the size and growth of the barter economy in the United States and globally. I need for an annual industry report covering 2020–2025 trends:

1. IRS data: How many 1099-B forms were filed by barter exchanges in 2020–2024? Total reported barter income volume? (Check IRS Statistics of Income publications)
2. BLS data: Size and growth rate of the US freelance/gig workforce 2020–2025; any data on payment preferences or alternative compensation
3. IRTA (International Reciprocal Trade Association): Annual trade volume statistics for member exchanges 2020–2025; number of businesses participating in organized barter
4. Academic research: Any peer-reviewed studies on non-monetary exchange growth, especially during the 2021–2024 inflation period
5. Survey data from Upwork, Fiverr, Toptal, Freelancers Union: Any surveys on alternative payment preferences, cashless transactions, or barter interest
6. Growth of specific barter platforms: Simbi user growth, TimeBank network expansion, Bartercard transaction volume

Format: year-by-year table where possible. Include source URLs and publication dates.
```

**Status**: ✅ **DONE** — See `docs/research/The barter economy's data desert what we know and what's missing.md`. Critical finding: the barter economy is severely under-measured. IRS publishes no barter-specific 1099-B filing data. BLS tracks freelancers (72.9M independents per MBO 2025) but not barter. IRTA's $12-14B estimate has no disclosed methodology and hasn't changed in a decade. Academic counter-cyclical evidence exists (Marvasti & Smyth, Stodder on WIR) but nothing covers 2021-2024. Gig platform surveys (Upwork, Fiverr, MBO) ignore barter entirely. The data gap IS the story for Article 16.

---

## Cluster 4: Startup & Bootstrapping (Articles 17–20)

### Research Gap 4.A — Startup Barter Precedents

**Needed for**: Articles 17, 18, 19, 20 (MVP without cash, equity vs. barter, zero-cash GTM, cashless agency)

**Specific unknowns**:
- Are there documented case studies of startups/founders who bartered services to build their MVP or early product?
- What do startup advisors say about service barter vs. equity as a form of compensation?
- Legal: can a startup legally barter services with contractors under SEC rules (is it an unregistered security if treated as equity-adjacent)?
- What GAAP rules apply to a company recording service barter as revenue?
- Any YC/startup community discussions about skill exchange for early-stage companies?

**Gemini Research Prompt**:
```
Research the use of skill bartering and service exchange in early-stage startups. I need:

1. Documented case studies of startups that bartered services (not equity) to build their MVP — specifically trading technical services for marketing/design or vice versa
2. Legal analysis: can a startup legally barter professional services with contractors under US law? Any SEC implications if services are treated as equity-adjacent compensation?
3. How GAAP requires startups to record revenue from barter transactions (ASC 606 barter revenue recognition)
4. Y Combinator, First Round, a16z blog posts or podcasts discussing service barter as an alternative to equity for bootstrapped startups
5. What startup advisors say about equity vs. service barter as founder resources — pros, cons, typical valuations
6. r/startups and r/YCombinator discussions about finding technical co-founders or contractors via service exchange
7. Any data on what % of pre-seed startups use some form of service barter

Format: narrative summary + specific examples with links.
```

**Status**: ✅ **DONE** — See `docs/research/Skill bartering in startups the untracked economy founders quietly rely on.md`. Covers named case studies (Tata Tickaradze/To The Moon Social, Jared Krause/Currency, Cream City Music $50K ad barter, Publicize Swap Shop), IRS treatment with SEC boundary analysis (Howey Test — pure barter avoids securities law), GAAP ASC 845/606 accounting treatment, investor silence (no VC firm has published barter guidance except Stripe's bootstrapping guide), advisor consensus (equity for short-term services is almost always wrong), community views from HN/Startups.com, and data gap analysis (no survey tracks startup barter prevalence).

---

## Cluster 5: Comparison & Competitive (Articles 21–24)

### Research Gap 5.A — Competitor Platform Deep Dive

**Needed for**: Articles 21, 22, 24 (SkillLedger vs. Simbi, barter vs. cash, Thumbtack alternatives)

**Specific unknowns**:
- Current Simbi platform features, pricing, user base size, and trust mechanisms (2025 data)
- Thumbtack and Fiverr current pricing/fee structures for freelancers
- User complaints about Thumbtack/Fiverr from Reddit and review sites that SkillLedger can address
- What does Simbi's community say about platform limitations?
- What specific features make commercial barter exchanges (Bartercard) different from informal barter?

**Gemini Research Prompt**:
```
Research current (2025) features, pricing, user reviews, and limitations of these platforms for a competitive comparison:

1. Simbi (simbi.com): Current feature set, credit system mechanics, user base size/growth, pricing, how disputes are handled, user complaints from Reddit/App Store reviews
2. Thumbtack: Current lead pricing model, average lead cost by category, top user complaints on r/freelance, Trustpilot, and BBB
3. Fiverr: Current fee structure for sellers/buyers, top 5 user complaints in 2024–2025, what types of professionals Fiverr is least suited for
4. Bartercard: How commercial barter exchange credits work, what industries participate, fees, and differences from informal barter
5. Bark.com: Pricing model, lead quality reputation, user reviews vs. Thumbtack

Format: comparison table where possible, with specific pricing numbers, fees, and verbatim user complaint examples from public sources.
```

**Status**: ✅ **DONE** — See `docs/research/Five service platforms compared pricing, complaints, and who actually benefits.md`. Covers Simbi (501(c)(3), 50₴ signup credits, density problem, no dispute resolution), Thumbtack ($10-$170+ per lead, 10-30% conversion, Trustpilot 2.6/5 with 6,336 reviews), Fiverr (20% seller commission, 27.6% effective take rate, active buyers down to 3.3M), Bartercard ($29-59/mo + 6.5% per transaction, 13% round-trip), and Bark ($14-$65+ per lead). Includes verbatim user complaints from App Store, Reddit, Trustpilot, PissedConsumer, and BBB.

---

## Cluster 6: Industry Deep Dives (Articles 25–30)

### Research Gap 6.A — Legal Professionals Barter Ethics

**Needed for**: Article 26 (legal professionals barter guide)

**Specific unknowns**:
- Do any state bars explicitly prohibit bartering legal services?
- ABA Model Rules — which rules apply to barter arrangements for attorneys?
- State-specific ethics opinions on attorney barter (especially CA, NY, TX, FL)
- How do attorneys currently barter informally (what communities, how they find partners)?
- IOLTA implications of barter income for trust accounting?

**Gemini Research Prompt**:
```
Research US state bar ethics rules on bartering legal services. I need:

1. Do any US state bar associations explicitly prohibit attorneys from bartering legal services? If so, which states and what is the prohibition?
2. Which ABA Model Rules apply when an attorney barters legal services — specifically Rule 1.5 (fees), Rule 1.7 (conflicts), Rule 1.8 (business transactions with clients)?
3. Any notable state bar ethics opinions from CA, NY, TX, or FL addressing barter arrangements for attorneys — with opinion numbers and dates
4. IOLTA implications: if an attorney receives services instead of cash, do they still need to track this through their trust account?
5. How attorneys currently barter informally — Reddit (r/law, r/legaladvice), LinkedIn groups, bar association networking events
6. Any IRS guidance on how attorneys should report barter income differently from other professionals

Format: cite specific ABA Model Rule numbers, ethics opinion citation numbers, and bar association sources.
```

**Status**: ✅ **DONE** — See `docs/research/Bartering legal services the ethics rules attorneys must navigate.md`. Covers ABA Model Rules 1.8(a), 1.5, 1.7(a)(2), and 1.15 compliance framework. No state flatly prohibits one-on-one barter. Three historical waves traced (hostile 1977-1986, permissive shift 1990s-2010s, modern era 2010+). Specific state opinions: CA FO 1977-44 and 1981-60 (restrictive, never updated), TX Ethics Opinions 410/435 (prohibitive under old code), NYSBA 665 (1994 landmark permissive), NC 2010 FEO 4 (comprehensive modern analysis). IOLTA: barter dollars cannot enter trust accounts. IRS Rev. Rul. 79-24 uses lawyer-painter example. Badell v. Commissioner Tax Court case. Active barter channels: ITEX ($140M+ annual), IMS Barter, Florida Barter, BizX.

---

### Research Gap 6.B — Healthcare HIPAA and Barter

**Needed for**: Articles 28, and healthcare industry page

**Specific unknowns**:
- Does HIPAA apply to barter arrangements involving healthcare providers?
- Can a therapist legally barter therapy sessions for other services?
- IRS treatment of healthcare services provided in barter
- State licensing board rules for therapists/counselors engaging in barter

**Gemini Research Prompt**:
```
Research the legal and regulatory considerations for healthcare and wellness professionals bartering their services:

1. Does HIPAA apply to barter arrangements? Specifically: if a therapist trades therapy sessions for web design services, does HIPAA's Privacy Rule or Security Rule impose any obligations on the web designer receiving PHI?
2. Are there state psychology/counseling licensing board rules that prohibit or restrict therapists from bartering sessions? (APA Ethics Code Section 6.05 is relevant — include it)
3. IRS treatment of healthcare services in barter — is there any difference from professional services barter? Medicare/Medicaid billing implications?
4. How wellness professionals (yoga instructors, massage therapists, personal trainers) currently barter — what communities exist, what the common arrangements look like
5. Any malpractice insurance considerations for healthcare providers engaged in barter arrangements

Format: organized by profession (physicians, therapists, wellness practitioners) with specific regulatory citations.
```

**Status**: ✅ **DONE** — See `docs/research/Bartering healthcare services creates a regulatory minefield across five legal domains.md`. Covers HIPAA (3 scenarios: personal barter, practice website no PHI, practice website with PHI — BAA required under 45 CFR §§ 164.502(e)/164.504(e)), APA Ethics Code 6.05 evolution (1988 denounced → 2017 permissive), state variations (TX LPC outright ban 22 TAC §681.38(d)(6), CA mixed signals, NY problematic listing, FL permissive), NASW/ACA/AAMFT codes, Medicare/Medicaid fraud exposure (Anti-Kickback 42 USC § 1320a-7b up to 10yr prison, Stark Law strict liability, False Claims Act treble damages), wellness barter prevalence (75% of massage therapists barter per AMTA), malpractice insurance coverage (no standard exclusion for barter), and state licensing requirements.

---

## Summary Table — Research Status

| Research Gap | Articles Affected | Research Source | Status |
|---|---|---|---|
| 1.A — IRS Barter Exchange Rules | 1, 2, 6 | `docs/research/IRS Barter Exchange Platform Rules.md` + `IRS Barter Exchange Platform Tax Rules.md` | ✅ Done |
| 1.B — QuickBooks/Xero Barter Methods | 3 | *(none yet)* | ⬜ **ONLY REMAINING GAP** |
| 1.C — Barter Contract Legal Requirements | 4, 5, 7 | `docs/research/Legal requirements for barter service agreements in the US.md` | ✅ Done |
| 2.A — Real Dispute Cases | 8, 9 | `docs/research/Skill barter arrangements what goes wrong and how to protect yourself.md` | ✅ Done |
| 3.A — Service Valuation Frameworks | 12, 13 | `docs/research/Valuing professional services in barter frameworks, authorities, and documented practice.md` | ✅ Done |
| 3.B — State of the Barter Economy Data | 16 | `docs/research/The barter economy's data desert what we know and what's missing.md` | ✅ Done |
| 4.A — Startup Barter Precedents | 17, 18, 19, 20 | `docs/research/Skill bartering in startups the untracked economy founders quietly rely on.md` | ✅ Done |
| 5.A — Competitor Platform Deep Dive | 21, 22, 24 | `docs/research/Five service platforms compared pricing, complaints, and who actually benefits.md` | ✅ Done |
| 6.A — Legal Professionals Barter Ethics | 26 | `docs/research/Bartering legal services the ethics rules attorneys must navigate.md` | ✅ Done |
| 6.B — Healthcare HIPAA and Barter | 28 | `docs/research/Bartering healthcare services creates a regulatory minefield across five legal domains.md` | ✅ Done |

---

## Article Readiness — Updated March 2026

With 9 of 10 research gaps completed, **29 of 30 articles** are now ready to write. Only Article 3 (QuickBooks/Xero tutorial) requires additional research.

### Ready to Write NOW (29 articles)

| Article | Cluster | Research Source |
|---|---|---|
| 1 — Barter Income Taxes Freelancer Guide | Tax & Legal | Research 1.A (IRS Rules) |
| 2 — IRS Form 1099-B Explained | Tax & Legal | Research 1.A (IRS Rules) |
| 4 — Barter Contract Templates | Tax & Legal | Research 1.C (Contract Legal Reqs) |
| 5 — How to Invoice Barter Transaction | Tax & Legal | Research 1.C (Contract Legal Reqs) |
| 6 — Barter Tax Myths vs Reality | Tax & Legal | Research 1.A (IRS Rules) |
| 7 — IP Rights in Skill Exchange | Tax & Legal | Research 1.C (Contract Legal Reqs) |
| 8 — How to Avoid Barter Scams | Trust & Safety | Research 2.A (Dispute Cases) |
| 9 — What If Barter Partner Doesn't Deliver | Trust & Safety | Research 2.A (Dispute Cases) |
| 10 — How Escrow Works in Skill Exchange | Trust & Safety | SkillLedger product knowledge + Research 2.A |
| 11 — How to Assess a Barter Partner's Portfolio | Trust & Safety | Research 2.A (vetting section) |
| 12 — How to Value Services for Barter | Barter Valuation | Research 3.A (Valuation Frameworks) |
| 13 — Time vs Value Fallacy Skill Exchange | Barter Valuation | Research 3.A (4 frameworks for rate disparities) |
| 14 — Barter System vs Credit System | Barter Valuation | Research 3.A (IRTA standards) + concept-based |
| 15 — Multi-Party Barter Trade | Barter Valuation | Research 3.A (IRTA UC inter-exchange) + concept-based |
| 16 — State of the Barter Economy 2026 | Barter Valuation | Research 3.B (data desert — the gap IS the story) |
| 17 — How to Build MVP Without Cash | Startup | Research 4.A (startup barter precedents) |
| 18 — Barter Credits vs Startup Equity | Startup | Research 4.A (SEC analysis, advisor consensus) |
| 19 — Zero-Cash Go-To-Market | Startup | Research 4.A (case studies, community views) |
| 20 — Run a Business on Barter Model | Startup | Research 4.A + 3.A (IRTA guidelines, exchange mechanics) |
| 21 — SkillLedger vs Simbi | Comparison | Research 5.A (platform deep dive) |
| 22 — Skill Barter vs Cash Freelancing | Comparison | Research 5.A (fee structures, complaints) |
| 23 — Time Banking vs Skill Exchange | Comparison | Research 3.A (time banking section) |
| 24 — Alternatives to Thumbtack | Comparison | Research 5.A (Thumbtack/Bark/Fiverr data) |
| 25 — SaaS Startup Barter Guide | Industry | Research 4.A + product knowledge |
| 26 — Legal Professionals Barter Guide | Industry | Research 6.A (attorney ethics) |
| 27 — Non-Profit Skill Exchange Guide | Industry | Practical guide |
| 28 — Healthcare Barter Guide | Industry | Research 6.B (healthcare regulatory) |
| 29 — Creative Agency Skill Swap Guide | Industry | Practical guide |
| 30 — Content Creator Skill Exchange Guide | Industry | Practical guide |

### NOT Ready — Needs Research (1 article)

| Article | Cluster | Missing Research |
|---|---|---|
| **3 — How to Record Barter Transactions in QuickBooks** | Tax & Legal | Research 1.B — QuickBooks Online and Xero step-by-step workflows. Need screenshot-level instructions for creating barter clearing accounts, zero-balance invoicing, and 1099-ready tax reports. |
