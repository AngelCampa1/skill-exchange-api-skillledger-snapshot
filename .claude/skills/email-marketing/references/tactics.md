# Email Marketing Tactics — SkillLedger Reference

## The 2026 Machine-to-Machine Paradigm

In 2026, AI assistants (Gmail AI, Outlook Copilot) act as gatekeepers before humans see emails. They sort, summarize, and suppress content. Emails must be:
- **Human-sounding**: conversational, non-corporate, specific
- **Hyper-relevant**: references something specific to the recipient (company, role, recent event)
- **Deliverability-optimized**: technical infrastructure must be clean before copy matters

## AI Personalization Tiers

| Tier | Personalization Level | Implementation |
|---|---|---|
| **Basic** | First name, company | `{{first_name}}` tokens. Table stakes in 2026. |
| **Contextual** | Job title + industry + known pain | "As a lease administrator managing NNN leases..." |
| **Behavioral** | Actions taken (scanned but didn't pay) | "You found $X in potential overcharges — here's why they matter." |
| **Predictive** | AI-inferred intent signals | Triggered by time-since-scan, Q1 proximity, property type |
| **Hyper-personal** | Individual-level data + AI drafting | "I noticed you operate in [city] where [market condition]..." |

For cold outreach, contextual tier minimum. For nurture, behavioral and predictive are table stakes.

## Subject Line A/B Testing

**Champion/Challenger framework:**
- Champion: best-performing current subject line (baseline)
- Challenger: one variable changed (curiosity vs. direct benefit, personalized vs. generic)
- Test one variable at a time
- Minimum 200 opens per variant for statistical significance
- Metrics: open rate (primary), click rate (secondary), reply rate (for cold)

**Subject line formula library for SkillLedger:**

*Loss aversion:*
- "You have [X] credits expiring unused this month"
- "[$X] in services you could be trading for right now"

*Curiosity gap:*
- "What most freelancers never realize about their downtime between projects"
- "The service category that [X]% of SkillLedger users trade for first"

*Social proof:*
- "How [freelancer type] got [service] without spending a dollar on it"
- "What a SkillLedger user traded for last month (real example)"

*Direct benefit:*
- "Get the services you need without touching your cash runway"
- "Make your first trade on SkillLedger in under 10 minutes"

*Question/engagement:*
- "Quick question about what services you need right now"
- "Are you sitting on skills you could be trading for something you need?"

## Deliverability Infrastructure

Deliverability must be solved before copy. Even great emails don't convert if they land in spam.

**Technical checklist:**
- [ ] SPF record configured
- [ ] DKIM signature enabled
- [ ] DMARC policy set (p=quarantine minimum)
- [ ] Custom sending domain (not shared IP pool)
- [ ] List hygiene: remove bounces weekly, suppress unengaged 90+ days
- [ ] Warm new sending domains: ramp from 20 → 500 emails/day over 4 weeks
- [ ] Engagement-based sending: prioritize openers for first sends

**Spam trigger words to avoid:** free, guarantee, 100%, urgent, act now, limited time, no obligation, winner, cash, credit card required

**Plain text vs. HTML:**
- Cold outreach: plain text wins (higher deliverability, feels human)
- Transactional / product emails: minimal HTML acceptable (ExampleAudit brand color, one image max)
- Never use image-heavy HTML for cold sequences

## Send-Time Optimization

**General benchmarks:**
- Highest open rates: Tuesday–Thursday, 9–11am recipient local time
- Avoid: Friday afternoons, Mondays before 10am, Saturday/Sunday
- Re-engagement campaigns: Saturday 10am sometimes outperforms (less competition)

**AI-driven send-time optimization:** Most modern ESPs (Klaviyo, ActiveCampaign, HubSpot) offer "send time optimization" that delivers each email when each individual contact is most likely to open, based on historical behavior. Enable this for nurture sequences.

## AI-Powered Sequence Automation

**Behavioral triggers (set up in ESP):**
- `scan_completed` → trigger free-to-paid nurture sequence
- `report_opened_but_not_purchased` (3+ opens) → trigger urgency sequence
- `purchase_completed` → trigger onboarding + upsell sequence
- `inactive_30_days` → trigger re-engagement sequence
- `Q1_approaching` (January triggers) → send "dispute window" campaign to all free-scan users

**Suppression rules:**
- Stop nurture sequence if purchase event fires
- Stop cold sequence if reply received (route to CRM)
- Stop re-engagement if unsubscribe or hard bounce

## Compliance (CAN-SPAM + GDPR)

**CAN-SPAM (US):**
- Physical mailing address in every email footer (required)
- Clear identification as advertising where applicable
- One-click unsubscribe (must be honored within 10 business days)
- No deceptive subject lines or headers

**GDPR (EU/UK):**
- Explicit opt-in consent required — cannot cold-email without prior consent or legitimate interest basis
- Right to erasure: honor unsubscribe + delete from all lists within 30 days
- Data processing agreements with ESPs
- If cold emailing EU contacts: document the "legitimate interest" basis

**Practical rule:** For cold outreach, US audiences → CAN-SPAM compliant. EU audiences → require opt-in, use LinkedIn or gated content lead generation instead.

## Advanced Sequence Types

### Webinar Follow-Up (3 emails)
1. Day 0: "You're registered — here's what to expect" (confirm + anticipation)
2. Day of: "We're live in 2 hours" (last-chance reminder)
3. Day +1: "Replay is available" (capture non-attendees)

### Post-Demo / Post-Trial Nurture (7 emails)
1. Day 1: What you showed → why it matters (key moment recap)
2. Day 3: Social proof (customer similar to them)
3. Day 5: Objection handling (cost, implementation, ROI)
4. Day 7: Case study email (specific numbers)
5. Day 10: FOMO / urgency (renewal window, pricing change, Q1 deadline)
6. Day 14: "Still thinking it over?" (direct, short)
7. Day 21: Break-up email ("Closing the loop on your trial")

### Re-Engagement (3 emails)
1. "We miss you" + reminder of their free scan results
2. "New finding type" (announce a new detection rule or report feature)
3. "Last chance" + list-clean warning (honest: "We'll remove you from our list in 5 days")

## SkillLedger-Specific Email Timing

| Month | Event | Campaign |
|---|---|---|
| January | New year, new project resolutions | "Start the year by trading for what your business needs" — onboarding campaign |
| March–April | Q1 project planning | "What does your Q2 roadmap need? Trade for it now" — activation campaign |
| June | Mid-year freelance slowdown | "Turn your slow season into a trading season" — re-engagement campaign |
| September | Fall project surge | "New projects starting? Trade for support before you need it" — acquisition campaign |
| November | Year-end budget freeze | "No budget left? Your skills still have value" — end-of-year campaign |
