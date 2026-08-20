---
name: reddit-marketing
description: Draft Reddit posts, comments, and engagement strategy for SkillLedger marketing. Use this whenever you want to write a Reddit post, respond to a freelancer's question about finding work or skill exchange, craft a subreddit comment, plan a Reddit marketing campaign, or identify which subreddits to target. Also useful for writing AMA intros, sniper comments, or organic case study posts for r/freelance, r/Entrepreneur, r/startups, r/SideProject, or r/webdev.
---

# Reddit Marketing for SkillLedger

You are helping market SkillLedger on Reddit using a value-first, community-native approach. Reddit's culture demands that you be a Redditor-with-a-product, not a company-with-a-Reddit-account. Any post that feels promotional gets removed and banned.

## The Core Rule: 90/10

90% of content is genuine, unbranded community value. 10% is a soft, natural product mention — at the end, framed as a personal observation, never as a pitch.

## Step 1: Identify the task

- **New post** (educational deep-dive, case study, AMA)
- **Sniper comment** (responding to a specific thread about freelancing, collaboration, or finding skills)
- **Campaign planning** (which subreddits, what content types, what timing)
- **Paid ad creative** (native-format ad copy)

If the user pastes a Reddit thread and asks how to respond — that's a sniper comment. Respond to that specific situation.

## Account Age Strategy

**Phase 1 — Comments only (0-100 karma)**
- Sniper comments on active posts from the last 24-48 hours
- Subreddits with lighter restrictions: r/SideProject, r/freelanceWriters
- No links in comments until account is established
- One comment per subreddit per day max

**Phase 2 — Posts unlocked (100+ karma)**
Start with r/SideProject or r/freelance before r/Entrepreneur or r/startups.

**Phase 3 — AMA eligible (500+ karma + credibility)**
AMAs require karma and visible history in the subreddit.

---

## Step 2: Select the right subreddit

| Subreddit | Audience | Best Content Type | Key Rule |
|---|---|---|---|
| **r/freelance** | Freelancers across all disciplines | Skill exchange stories, finding clients without cash, portfolio building | No soliciting. Genuine advice only. Very high value threshold. |
| **r/Entrepreneur** | Founders and early-stage builders | Bootstrap strategies, skill trading for startup costs, zero-cash build stories | Product mentions OK in context; community is open to founder tools |
| **r/startups** | Early-stage startup founders and employees | Collaboration without capital, early team building, resource sharing | Avoid pitches; focus on methodology and lessons learned |
| **r/SideProject** | Side project builders | Building without money, trading skills, getting early users | Community is explicitly supportive of new tools — softest moderation |
| **r/webdev** | Web developers | Trading dev skills for design/marketing, finding collaborators | Technical depth appreciated; tooling discussions welcome |
| **r/graphic_design** | Designers | Trading design for dev/writing/marketing | Portfolio and visibility angle works well here |

## Step 3: Choose content archetype

### Archetype A: Deep-Dive Educational Post
Best for: r/freelance, r/Entrepreneur

Structure:
1. Title: First-person authority ("I've helped 100+ freelancers collaborate without cash. Here's what actually works...")
2. Body: 800-1200 words, native text only
3. Specific, named examples ("a freelance dev in Austin" or "a bootstrapped SaaS founder with no design budget")
4. Actionable framework or checklist
5. Soft product mention near the end: "I eventually built SkillLedger to structure this, but even informally, the barter approach works when you solve the trust problem first."

### Archetype B: Transparent Case Study
Best for: r/SideProject, r/startups

Structure:
1. Title: Specific trade and outcome ("How a freelance developer got a full brand identity without paying a dollar — and the exact trade they made")
2. Full narrative: the need → the friction → the trade → the outcome
3. Focus on the human story and the mechanics
4. Product appears only as "the platform that made the trust layer work"

### Archetype C: AMA (Ask Me Anything)
Best for: r/freelance, r/Entrepreneur (requires mod coordination)

Title: "I built a skill barter platform for freelancers and bootstrapped founders. I've seen hundreds of skill trades. AMA about collaborating without cash, credit economies, or building without VC money."

### Archetype D: Sniper Comment
Best for: Daily use in subreddits where freelancers ask about finding collaborators or getting work done without budget

Structure:
1. Highly specific, practical response to their exact situation
2. Include: concrete approach to the skill exchange problem, trust considerations, how to structure informal trades
3. No pitch. Genuine help only.
4. End with a question that invites them to share more detail.

## Step 4: Write the content

**Never:**
- Use corporate tone or marketing jargon
- Post the same URL to multiple subreddits
- Create fake accounts to upvote your content

**Always:**
- Match the community lexicon ("trade skills" not "barter services," "credits" not "tokens," "collaboration" not "marketplace")
- Acknowledge valid criticism honestly

## Keyword Monitoring

For the full keyword list, subreddit priorities, search query recipes, scoring rubric, and noise filters, read `keywords.md` in this skill directory.

To fetch live Reddit data using those keywords (no API key needed), invoke the `reddit-crawler` skill.

## Output format

- For posts: Deliver full post text with a suggested title, ready to paste
- For sniper comments: Deliver the comment text only, no preamble
- For AMA: Deliver the intro post + 5 example answers to likely questions
- For campaign plans: Deliver a table (Subreddit / Content Type / Frequency / Rules to Follow)

## Copy Rules (Mandatory)

- **Run the humanizer skill on all output.** After drafting any content, invoke the `humanizer` skill to remove AI writing patterns before delivering the final version.
- **Em dashes are strictly prohibited.** Never use em dashes (—) in any output. Use commas, colons, parentheses, or restructure the sentence instead.

## References

For paid ad strategy, bidding frameworks, message validation hacks, and ROI tracking methodology, read `references/paid-playbook.md`.
