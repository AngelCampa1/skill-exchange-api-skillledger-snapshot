---
name: skillledger-business-context
description: Use when working on SkillLedger, answering questions about the product, skill exchange mechanics, credit economy, reputation system, pricing, conversion flows, user personas, or implementing any feature that touches business logic (credit transfers, project marketplace, reputation scoring, collaboration tools, security).
---

# SkillLedger — Business Context Reference

Full product and business knowledge for agents implementing features or answering business questions. Also reference the `business-advisor` skill for GTM strategy and acquisition channel guidance.

---

## What SkillLedger Is

**One-sentence pitch:** SkillLedger is a professional collaboration platform and barter exchange where freelancers, consultants, and bootstrapped founders trade skills using a credit economy — eliminating payment friction and enabling collaboration without cash.

**The problem it solves:** The classic "double coincidence of wants" problem in peer-to-peer skill exchange. A developer needs design work. A designer needs development. They need each other but can't coordinate. Traditional solutions are either cash (expensive, creates financial friction) or equity (complex, inappropriate for small exchanges). SkillLedger's credit economy solves this: each party earns and spends credits across a network, not just in bilateral trades.

**What SkillLedger enables:**
- Freelancers to access skills they need without cash outlay
- Bootstrapped founders to build their products by trading their expertise
- Consultants to leverage their knowledge for complementary services
- Solopreneurs to build their business by trading what they know

**Defining constraint:** SkillLedger is built on trust and reputation. The credit economy only works if the reputation system is trustworthy. All anti-fraud ML, security, and reputation features are core infrastructure — not optional add-ons.

**Core architectural principle:** Clean Architecture with strong separation of domain logic. Financial services (90%+ coverage). Security services (85%+ coverage). The credit economy must be mathematically sound and auditable.

---

## Target Users

| Persona | Situation | Pain | Conversion Driver |
|---|---|---|---|
| Freelance Developer | Has strong technical skills; needs design, writing, or marketing | Pays for every service they need; losing earnings to other freelancers | First successful trade — "I got design work without spending money" |
| Designer / Creative | Has strong visual skills; needs development, copy, or consulting | Same mirror problem as developer | First trade confirmation |
| Early-Stage Startup Founder | Building before funding; needs multiple skill types | No budget for freelancers; can't offer meaningful equity for small tasks | "I built this feature without a budget" |
| Consultant | Deep expertise in one domain; needs complementary skills | Time is expensive; paying for junior skills wastes leverage | Efficient leverage of expertise |
| Solopreneur | Running a one-person business | Everything costs money; wearing too many hats | "I finally got [the skill I hate doing] handled" |
| Freelance Writer | Content skills; needs dev, design, or SEO | Under-priced in cash markets; skills feel "cheaper" | Trades content for higher-value technical work |

---

## Business Model

| Revenue Stream | How It Works | Current Rate |
|---|---|---|
| Starting credits | 100 credits issued to every new verified user | One-time |
| Transaction fee | Fee on every credit transfer | 2.5% |
| Credit purchases | Users buy additional credits | Price TBD based on credit economy balance |
| Subscription tiers | Premium features (priority matching, advanced analytics) | TBD |

**Key credit mechanics:**
- 100 starting credits for every new verified user (enough for ~1-2 initial trades)
- 2.5% transaction fee on all credit transfers (platform sustainability)
- Credits are not cryptocurrency — they are internal platform currency with real service value
- Anti-fraud ML prevents credit farming and fake transaction exploitation

---

## Five Core Features

1. **User Identity & Profile**: Skill taxonomy, verified credentials, portfolio links, reputation badges
2. **Project Marketplace**: Post projects needing skills, browse offers, propose trades
3. **Credit Economy**: Credit issuance, transfer, fee calculation, balance management, purchase flow
4. **Collaboration Workspace**: Real-time messaging, file sharing, milestone tracking, delivery confirmation
5. **Reputation System**: Multi-dimensional scoring (quality, reliability, communication, fair dealing)

---

## Competitive Position

| Competitor | SkillLedger Advantage |
|---|---|
| Fiverr / Upwork | Cash-only marketplaces — payment friction, race-to-bottom pricing |
| LinkedIn Services | No transaction or delivery infrastructure |
| Skillshare / Udemy | Learning platforms, not collaboration |
| Barter communities (informal Reddit/Facebook groups) | No trust infrastructure, no escrow, no reputation |
| Contra (commission-free) | Cash-based, no barter mechanic |

**SkillLedger's strategic position:** Only structured platform with a credit economy that enables multi-party skill exchange (not just bilateral barter), enterprise-grade security, and a reputation system that survives beyond individual relationships.

---

## Key Business Rules

| Decision | Rationale |
|---|---|
| 2.5% transaction fee (not percentage of service value) | Fair to both parties; predictable; scales with platform usage |
| 100 starting credits for verified users | Lowers barrier to first trade; credits as an onboarding hook |
| No cash payments between users | Keeps the platform in the credit economy; prevents disintermediation |
| Anti-fraud ML is core, not optional | Credit economies collapse without fraud prevention |
| Multi-dimensional reputation, not a single score | Single scores are gameable; multi-dimensional is more trustworthy |

---

## GTM Strategy (from business-advisor skill)

**90-Day Plan (from business-advisor SKILL.md):**
- Phase 1 (Days 1-30): Foundation — define vertical, ICP, LinkedIn presence, standalone tool
- Phase 2 (Days 31-60): Early Traction — personal outreach, free early access, office hours, partnerships
- Phase 3 (Days 61-90): Validation — referral program, case studies, content, LinkedIn ads, NPS surveys

**First 100 Customers Playbook:**
- Founder-led sales is non-negotiable at 0 customers
- Do things that don't scale: manual matching, white-glove first trades
- Leverage partnerships (developer communities, designer communities)

**Primary acquisition channels:**
1. LinkedIn content (founder voice, skill trade stories)
2. Reddit engagement (r/freelance, r/SideProject, r/startups)
3. X/Twitter content (bootstrap angle, credit economy education)
4. YouTube (skill exchange explainers, product walkthroughs)

---

## Technical Architecture

- Backend: Clean Architecture — Domain → Application → Infrastructure
- Frontend: Next.js with TypeScript, Tailwind CSS
- Port: Backend 8030, Frontend 3030, SQL Server 9030
- Test coverage: Financial (90%), Security (85%), Business Logic (80%)

**The Golden Rule (testing):** Mock external services only — email, payments, file storage, CDN. Never mock internal business logic.

---

## Success Metrics

| Category | Metric | Target |
|---|---|---|
| Marketplace Health | Liquidity (% of posted projects matched) | >10% |
| Activation | Time to First Transaction | <7 days |
| Growth | WAU Growth | 10-15%/week |
| Activation | Activation Rate | >25% |
| Conversion | Free-to-Paid (credit purchase) | 5-9% |
| Engagement | Community Participation | >20% |
| Quality | NPS Score | >40 |
