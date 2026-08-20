# Reddit Keyword Monitoring — SkillLedger

Use this file with the `reddit-crawler` skill to find sniper comment opportunities.

---

## Target Subreddits

### Tier 1 — High Volume, High Intent

| Subreddit | Why |
|---|---|
| r/freelance | Primary audience — freelancers looking for collaboration and clients |
| r/Entrepreneur | Bootstrapped founders who need skills they can't afford to buy |
| r/startups | Early-stage founders building with limited resources |
| r/SideProject | Side project builders explicitly open to tools and collaboration |

### Tier 2 — Specific Audience Types

| Subreddit | Why |
|---|---|
| r/webdev | Developers trading skills for design, writing, or marketing |
| r/graphic_design | Designers trading for dev, writing, or business skills |
| r/freelanceWriters | Writers who can trade content for technical or design skills |
| r/digitalnomad | Location-independent professionals seeking collaboration |
| r/Entrepreneur | Founders bootstrapping without VC — skill trading is directly relevant |
| r/UXDesign | UX/product designers who could trade for dev or writing |

### Tier 3 — Monitor Only (lower conversion)

| Subreddit | Notes |
|---|---|
| r/smallbusiness | Occasionally skill exchange or collaboration questions |
| r/marketing | Marketers who need complementary skills |
| r/personalfinance | Occasionally alternative income/trade discussions |

---

## Keyword Groups

### Group 1: Direct Skill Exchange / Barter (highest intent — always engage)

```
skill exchange
skill barter
barter services
trade skills
skills for services
swap skills
barter freelance
skill swap
exchange services
trade work
work exchange
skills trade
```

### Group 2: Collaboration Without Cash (high intent)

```
collaborate without money
find collaborator no budget
work together no pay
equity free collaboration
bootstrap collaboration
build without money
no cash collaboration
skill sharing
collaborate for equity
trade for services
get work done no budget
startup collaboration no money
```

### Group 3: Finding Freelancers / Affordable Help (high intent — screen for right angle)

```
affordable freelancer
find cheap freelancer
freelancer without paying much
trade for design work
get design help no money
need developer no budget
find writer affordable
marketing help no budget
design work without cash
web development affordable
```

### Group 4: Freelance Economy Questions (medium intent)

```
how to get clients freelance
freelance without clients
build portfolio freelance
get experience freelance
freelance starting out
first freelance client
freelance income alternative
reduce freelance costs
freelance tools
freelance collaboration
find work remotely
```

### Group 5: Bootstrap and Startup Resource Questions (medium intent — high quality leads)

```
bootstrapped startup
build startup no money
build MVP without funding
startup without VC
zero budget startup
self funded startup
bootstrap MVP
get startup help cheap
startup resource sharing
early stage no budget
founder collaboration
startup skills needed
```

### Group 6: Trust and Platform Signals (medium intent — worth engaging)

```
how to trust freelancer
vetting freelancers
freelance reputation
verify freelancer
safe to work with online
reputation online work
find trustworthy freelancer
freelance scam
platform for freelancers
marketplace for freelancers
skill marketplace
freelance marketplace alternative
```

---

## Search Query Recipes

### Quick Daily Scan (run every 24h, site-wide)

```
q=skill+exchange&sort=new&t=day
q=trade+skills+freelance&sort=new&t=day
q=barter+services&sort=new&t=day
q=collaborate+no+budget&sort=new&t=day
q=bootstrap+startup+skills&sort=new&t=day
q=find+collaborator+cheap&sort=new&t=day
q=freelance+skill+swap&sort=new&t=day
q=startup+without+money&sort=new&t=day
```

### Tier 1 Subreddit Scans (daily, restrict_sr=1)

```
r/freelance      + q=trade
r/freelance      + q=exchange
r/freelance      + q=collaborate
r/Entrepreneur   + q=skills
r/Entrepreneur   + q=bootstrap
r/Entrepreneur   + q=collaboration
r/startups       + q=no+budget
r/startups       + q=skills
r/SideProject    + q=collaboration
r/SideProject    + q=trade+skills
```

### Tier 2 Subreddit Scans (2-3x per week)

```
r/webdev          + q=design+collaboration
r/webdev          + q=trade+skills
r/graphic_design  + q=developer
r/graphic_design  + q=trade
r/freelanceWriters + q=exchange
r/digitalnomad    + q=collaborate
r/UXDesign        + q=developer+collaboration
```

### Extended Scan (weekly — finds evergreen threads)

```
q=skill+barter+startup&t=week
q=trade+work+no+cash&t=week
q=freelance+collaboration&t=week
q=build+startup+without+money&t=week
q=get+design+work+cheap&t=week
```

---

## Relevance Filter — Is This Post Actually for Us?

After crawling, apply this filter before scoring. **Skip the post if any of these are true:**

| Rejection Rule | Why |
|---|---|
| Post is about employment (W-2 job) not freelance/collaboration | Wrong audience |
| OP is asking about paid platforms for clients, not peer collaboration | Different product need |
| Post is about revenue sharing or equity arrangements | Different structure than SkillLedger credits |
| Post is about NFTs, crypto, or web3 token economies | Different audience |

**Pass the post if at least one of these is true:**

| Relevance Signal | What it means |
|---|---|
| Mentions skill exchange, barter, or trading services | Direct match |
| Mentions needing skills they can't afford to buy | Pain point we solve |
| Asks about finding collaborators without paying upfront | Right audience |
| Uses bootstrap framing with specific skill needs | High-intent |

---

## Scoring: When to Respond

| Signal | Score |
|---|---|
| Posted in last 48 hours | +1 |
| Posted in last 24 hours (bonus, replaces above) | +2 |
| Freelancer/founder (not recruiter) is the OP | +1 |
| Specific skill need or trade situation described | +1 |
| No budget explicitly stated | +1 |
| No substantive answer yet | +1 |
| Post has 3+ upvotes (validated pain) | +1 |
| Keyword from Group 1 or 2 | +1 |
| Post is NOT archived and NOT locked | required (skip if false) |

**Score 3-4:** Leave a sniper comment.
**Score 5+:** Prioritize — high-value engagement opportunity.

---

## Noise Filters (skip even if relevance filter passes)

- Posts older than 72 hours (unless still getting new comments)
- Posts where an expert has already given a comprehensive, accurate answer
- Posts where the OP has already resolved the situation
- Posts with `archived: true` or `locked: true`
- Posts about crypto/NFT barter economies (wrong product fit)
