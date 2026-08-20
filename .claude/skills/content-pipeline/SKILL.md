---
name: content-pipeline
description: Plan, design, and implement an automated AI content pipeline for SkillLedger. Use this whenever you want to automate video production from blog posts, set up a content distribution system, choose between HeyGen/Zoice/Synthesia for avatar videos, select Make.com vs n8n vs Zapier for orchestration, plan a zero-click publishing workflow, or understand how to turn one freelance economy guide into a week of multi-platform video content automatically.
---

# AI Content Generation Pipeline for SkillLedger

You are helping design or implement an automated content pipeline that turns SkillLedger's written content (freelance economy guides, skill exchange tutorials, credit economy explainers, founder collaboration stories) into short-form videos distributed across TikTok, Instagram Reels, YouTube Shorts, and LinkedIn — with minimal manual effort after initial setup.

This is an infrastructure skill. The output is a **plan, architecture, or implementation roadmap** — not content copy.

## Step 1: Assess current state

Ask or infer:
- **What content exists?** (Blog posts? Case studies? Success stories? Help center articles?)
- **What's currently manual?** (Recording videos? Editing? Uploading?)
- **Technical level**: no-code (Zapier), low-code (Make.com), or developer-ready (n8n)?
- **Budget tier**: bootstrapped / growth-stage / scaling?
- **Goal**: personal brand (founder-led avatar) or company brand?

## Step 2: Choose the tool stack

The pipeline has 4 components. Recommend based on their situation.

### A. Script Generation (LLM)
- **Claude Sonnet 4.6 or Opus 4.6** via API
- System prompt: extract 5 key insights → format as 5-minute scripts → generate metadata (captions, hashtags, B-roll prompts)
- Constrain to: max 150 spoken words, no jargon, start with hook, no intro clichés

### B. Avatar / Video Generation
See detailed comparison in `references/tools.md`. Quick guidance:

| Need | Recommended Platform |
|---|---|
| Founder-led personal brand, photorealistic | Zoice |
| High-volume automation + API integration | HeyGen |
| Enterprise governance, team access | Synthesia |
| Interactive / real-time (sales bot) | Tavus |

### C. Middleware Orchestration
| Situation | Recommended |
|---|---|
| Non-technical, quick start | Zapier (but costs scale badly) |
| High-volume batch processing, visual logic | Make.com (recommended for SkillLedger at growth stage) |
| Full control, self-hosted, developer team | n8n |

**Make.com is the sweet spot for SkillLedger**: handles arrays/iterators natively (processing 5 videos per article), visual debugging, cost-efficient at scale.

### D. Social Media Distribution
- **Upload-Post.com**: single API call → publishes to TikTok, Reels, YouTube Shorts, LinkedIn, Facebook, X. Built-in FFmpeg formatting. Has native Make.com/n8n nodes. **Recommended.**
- **Blotato**: n8n community nodes, pulls from Google Drive, schedule-based.
- **Ayrshare**: 15+ platforms, Python/JS SDKs, white-label option.

## Step 3: Design the pipeline

### End-to-end zero-click pipeline (Make.com + HeyGen + Upload-Post.com)

**Trigger**: New blog post published in CMS webhook

**Phase 1 — Semantic Extraction**
1. Middleware intercepts webhook, extracts article text
2. API call to Claude: extract 5 key insights → generate 5 × 5-minute scripts + metadata (captions, hashtags, B-roll prompts)

**Phase 2 — Dual-Engine Rendering** (per video, looped ×5)
3a. **Avatar** (foreground): POST script to HeyGen API → renders talking-head video
3b. **B-roll** (background): Send B-roll prompts to video AI → generates contextual cutaway footage
4. Poll webhooks → retrieve files
5. Merge via Shotstack or Upload-Post FFmpeg → avatar over B-roll

**Phase 3 — Distribution**
6. Construct JSON payload (MP4 URL + captions + hashtags)
7. Send to Upload-Post.com → auto-adapts for each platform
8. Schedule: stagger 5 videos over 5 consecutive days
9. Log to Airtable/Google Sheets

**Output from one article**: 5 platform-native videos, published over 5 days, zero manual clicks after initial config.

## Step 4: Deliver the plan

**For architecture questions:** Provide a tool stack recommendation with rationale.
**For implementation questions:** Provide a phase-by-phase setup guide.
**For budget questions:** Provide estimated monthly cost at different volume levels.

## Output format

- **Quick question**: tool recommendation + 2-3 sentences why
- **Architecture planning**: diagram description + tool stack table
- **Implementation roadmap**: numbered setup steps with estimated time per step
- **Cost estimate**: table of tools + pricing at target volume

## SkillLedger-Specific Considerations

- Content types to automate first: "how a bootstrapped startup got [X] built without cash using SkillLedger credits" success stories, and skill exchange explainer series (design, development, writing, consulting → all skill categories as separate videos)
- For founder-led brand (Angel): Zoice or HeyGen personal clone → daily content without daily recording
- High-engagement angles: "built a startup without VC money," "zero-cash skill barter," "freelance without the payment friction"

## Copy Rules (Mandatory)

- **Run the humanizer skill on all output.** After drafting any content, invoke the `humanizer` skill to remove AI writing patterns before delivering the final version.
- **Em dashes are strictly prohibited.** Never use em dashes (—) in any output. Use commas, colons, parentheses, or restructure the sentence instead.

## References

For detailed avatar platform comparison, B-roll AI tools, middleware architecture comparison, unified social API options, and the full technical pipeline spec, read `references/tools.md`.
