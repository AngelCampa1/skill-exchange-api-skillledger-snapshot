---
name: email-marketing
description: Draft email campaigns, sequences, and individual emails for SkillLedger marketing. Use this whenever you need to write a cold outreach email, nurture sequence, onboarding flow, re-engagement campaign, product announcement, or follow-up email for SkillLedger leads, new users, or active members. Also useful for subject line optimization, A/B test variants, segmentation strategy, and compliance review.
---

# Email Marketing for SkillLedger

You are drafting email content informed by AI-driven email marketing best practices for 2026. SkillLedger's email strategy spans signup activation, first-transaction nudge, credit economy education, re-engagement of idle accounts, and trust-building for the reputation system.

## Step 1: Identify the email type

| Type | Trigger | Goal |
|---|---|---|
| **Welcome / activation** | New user signs up | Complete profile + post first project or offer within 48h |
| **First transaction nudge** | Profile complete but no transactions | Post first project or respond to an offer |
| **Credit education** | User has credits but hasn't spent them | Understand how credits work, first exchange |
| **Match notification** | System finds a relevant skill match | Engage with the match, initiate a conversation |
| **Re-engagement** | Inactive 30-90 days | Return to platform before credits expire or reputation stagnates |
| **Trust milestone** | First completed transaction | Celebrate the win, prompt a review |
| **Credit purchase nudge** | Low credit balance | Purchase additional credits to continue building |
| **Referral ask** | Active user with completed transactions | Drive referral from satisfied members |

Ask the user which type, or infer from their request.

## Step 2: Define the audience segment

SkillLedger audiences need different messaging:

- **Freelance developer**: ROI framing — "trade 2 hours of your time for design work worth $200." Technical and direct.
- **Designer / creative**: Portfolio and visibility angle — "get client work without pitching. Trade your skills for what you actually need."
- **Early-stage startup founder**: Bootstrap angle — "build your MVP without burning runway. Trade equity-free."
- **Consultant**: Leverage angle — "use your expertise to get other expertise. No cash required."
- **Freelance writer**: Reach and trade angle — "write for what you need instead of what pays."
- **Solopreneur**: All-in-one appeal — "stop paying for every service. Build your business by trading yours."

## Step 3: Write the email

### Subject line
- Mobile preview: ≤50 characters (41 characters optimal)
- Personalization token increases open rate (+26% average)
- Best-performing subject line types:
  - Curiosity gap: "The freelancer who got a logo without paying a cent"
  - Direct benefit: "Trade [2 hours of dev work] for [design, copy, or consulting]"
  - Loss aversion: "Your 100 starting credits are waiting. Here's how to use them."
  - Question: "What would you build if you could trade skills instead of pay cash?"
  - Ultra-low friction: "One thing to try in SkillLedger today"

**Avoid:** spam trigger words (free, guarantee, 100%), all-caps subject lines, misleading RE: prefixes

### Email body
- **Opening**: never start with "I", "My name is", or company name in the first sentence
- Lead with the recipient's frustration or goal, not the product
- **Length by type**: Activation nudge = 100-150 words. Nurture = 200-350 words. Educational = up to 500 words.
- One topic per email. One CTA per email.
- Plain text outperforms HTML for cold outreach

### CTA
- One action only
- Low-friction: "Post your first project" / "Browse skill offers" / "Complete your profile"
- Create urgency where legitimate: "Your 100 starting credits are ready" / "3 people with your needed skills posted this week"

## Step 4: Sequences

For multi-email sequences, deliver each email labeled with timing and goal.

### Onboarding sequence (first transaction nudge)
- Day 0 (immediate): Welcome — "You're in. Here's how SkillLedger works in 3 minutes."
- Day 1: Profile prompt — "One thing before your first match: complete your skill profile."
- Day 3: Credit education — "Your 100 credits are worth more than you think."
- Day 5: First action nudge — "Someone with [their needed skill] posted a project today."
- Day 10: Social proof — "How a bootstrapped founder got their landing page built without cash"
- Day 21: Re-engagement — "Your credits are sitting idle. Here's an easy first trade."

### Cold outreach sequence (5-touch)
- Email 1: Ultra-short (<150 words), freelance-friction-focused, no pitch
- Email 2 (+3 days): Value drop — useful insight about skill exchange economics
- Email 3 (+5 days): Case study ("A freelance dev got branding work worth $800 by trading 5 hours of code")
- Email 4 (+7 days): Direct ask — simple question, not a demo request
- Email 5 (+10 days): Break-up email ("Closing the loop")

## Step 5: Compliance check

Before finalizing any cold email, confirm:
- Physical mailing address in footer (CAN-SPAM)
- One-click unsubscribe link
- No deceptive subject lines
- For EU recipients: explicit consent/opt-in required (GDPR)
- No purchased lists for GDPR targets

## Output format

- **Single email**: Subject line (2-3 variants) + body text, ready to paste
- **Sequence**: All emails labeled with timing, subject lines, and goal
- **A/B test**: Two subject line variants with rationale for which to test

## SkillLedger Email Angles That Convert

- The cash-free collaboration hook: "Build without money. Trade what you know."
- Bootstrapper empathy: "Not every project needs a budget. Some need the right trade."
- First-transaction social proof: "A developer got a full brand kit without paying a dollar. Here's how."
- Credit urgency (light): "100 credits are waiting. They don't expire, but your window to get early traction does."
- Trust system: "Your reputation score grows with every successful trade. Here's how to start."

## Copy Rules (Mandatory)

- **Run the humanizer skill on all output.** After drafting any content, invoke the `humanizer` skill to remove AI writing patterns before delivering the final version.
- **Em dashes are strictly prohibited.** Never use em dashes (—) in any output. Use commas, colons, parentheses, or restructure the sentence instead.

## References

For AI personalization architecture, deliverability infrastructure, send-time optimization, AI inbox filtering strategy, and advanced sequence types, read `references/tactics.md`.
