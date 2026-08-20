---
name: ai-seo
description: >
  Optimize content for AI search engines so it gets cited by ChatGPT, Perplexity, Google AI Overviews, Gemini, Claude, and Copilot.
  Use this whenever the user wants to get recommended by AI, appear in AI-generated answers, optimize for generative search,
  do GEO (Generative Engine Optimization), check AI visibility, or make content extractable by LLMs. Also use when the user
  mentions "AI SEO," "answer engine optimization," "AEO," "LLM visibility," "AI citations," "AI Overviews," or asks
  "how do I get cited by ChatGPT/Claude/Perplexity."
---

# AI SEO — Generative Engine Optimization (GEO)

Traditional SEO gets you ranked. AI SEO gets you **cited**. A well-structured page can get cited even from page 2 or 3 of traditional results because AI systems select sources based on content quality, structure, and semantic completeness, not just rank position. 47% of AI Overview citations come from pages ranking below position #5 in traditional search.

## Before Starting

**Check for product marketing context first:**
If `.agents/product-marketing-context.md` exists (or `.claude/product-marketing-context.md`), read it before asking questions. Use that context and only ask for information not already covered.

Gather this context (ask if not provided):

1. **Current AI Visibility** — Does the brand appear in AI answers today? Which queries matter most?
2. **Content & Domain** — What content types exist? Any structured data (schema markup)?
3. **Goals** — Get cited as a source? Appear in AI Overviews? Compete with specific brands?
4. **Competitive Landscape** — Who gets cited where you don't?

---

## How AI Retrieval Actually Works

Understanding the retrieval pipeline is essential because it dictates exactly what to optimize. Skip this if you just need the checklists, but read it if you want to understand *why* the optimization tactics work.

For the full deep dive, read [references/retrieval-mechanics.md](references/retrieval-mechanics.md).

### The Two-Filter Pipeline

Every AI search engine runs your content through two filters before it can be cited:

1. **Lexical filter (BM25)** — Fast keyword matching on titles, headings, meta descriptions, and body text. If your headings don't contain the exact terms users search for, you fail here and the AI never reads your page.
2. **Semantic filter (vector embeddings)** — Converts your content into mathematical vectors and measures meaning-similarity against the user's query. Dense, factual body text with domain vocabulary scores highest here.

**The critical paradox:** Over-optimizing body text for "semantic richness" (fancy vocabulary, complex phrasing) tanks lexical retrieval by an average of 22 positions. The fix is a **bifurcated approach**: keep headings, titles, and meta tight on exact-match keywords (for BM25), while making body paragraphs dense with facts, numbers, and domain terminology (for semantic matching).

### Chunking and Contextual Retrieval

AI systems extract information in modular blocks ("chunks"), not full pages. Each chunk is evaluated independently. If a paragraph relies on pronouns pointing to previous sections or requires understanding the full page narrative, it gets discarded as low-confidence during retrieval.

**Optimal chunk sizes:**
- **Snippet answers** (featured snippets, quick citations): 40-60 words
- **Semantic units** (RAG retrieval, in-depth citations): 134-167 words
- Both must be **self-contained** — replace "this solution" with "the CAM audit solution", replace "the company" with the actual company name

### Query Fan-Out and the Multi-Query Penalty

When a user asks a broad question, the AI decomposes it into dozens of hidden sub-queries and searches for each independently. Content optimized for one narrow query loses visibility across the broader topic cluster in 69% of cases. The fix: build comprehensive pages that address multiple related sub-queries through well-structured H2/H3 sections, not hyper-targeted single-query landing pages.

---

## Platform Profiles — The Big Three

Each AI engine has distinct biases. For detailed per-platform optimization (including Copilot, Perplexity, and robots.txt config), read [references/platform-ranking-factors.md](references/platform-ranking-factors.md).

| Platform | Primary Bias | Key Signal | Conversion Rate |
|----------|-------------|------------|:--------------:|
| **ChatGPT** | Earned media — favors third-party sources over brand-owned domains | Content-answer fit (55% of citation likelihood), freshness (30-day recency = 3.2x boost), domain authority | 14.2% |
| **Google Gemini / AI Overviews** | E-E-A-T shield — filters generic AI-generated content aggressively | Schema markup (30-40% boost), verified authorship, multimodal content (156% higher selection with mixed media), Knowledge Graph presence | — |
| **Claude** | Contextual depth — most selective, highest conversion | Extreme factual density, structural logic, exhaustive documentation, zero marketing rhetoric | 16.8% |

**Perplexity** favors FAQ schema, public PDFs, and publishing velocity. **Copilot** favors Bing indexing, LinkedIn/GitHub presence, and sub-2s load times. Details in the platform reference file.

---

## AI Visibility Audit

### Step 1: Check AI Answers for Key Queries

Test 10-20 important queries across platforms:

| Query | Google AI Overview | ChatGPT | Perplexity | You Cited? | Competitors Cited? |
|-------|:-:|:-:|:-:|:-:|:-:|
| [query] | Yes/No | Yes/No | Yes/No | Yes/No | [who] |

**Query types to test:**
- "What is [your product category]?"
- "Best [category] for [use case]"
- "[Brand] vs [competitor]"
- "How to [problem you solve]"
- "[Category] pricing"

### Step 2: Content Extractability Check

For each priority page:

| Check | Pass/Fail |
|-------|-----------|
| Clear definition/answer in first 50 words of each section? (BLUF) | |
| Self-contained answer blocks (no pronouns pointing elsewhere)? | |
| Statistics with sources and dates cited? | |
| Comparison tables for "[X] vs [Y]" queries? | |
| FAQ section with natural-language questions? | |
| Schema markup (FAQ, HowTo, Article, Product)? | |
| Expert attribution (author name, credentials)? | |
| Updated within 6 months? "Last updated" date visible? | |
| Heading structure matches query patterns? (exact terms, not clever rewrites) | |
| AI bots allowed in robots.txt? | |
| Semantic units 134-167 words, self-contained? | |
| 15+ recognized entities (people, orgs, concepts) on the page? | |

### Step 3: AI Bot Access Check

Check robots.txt for these user agents — blocking any of them means that platform cannot cite you:

- **GPTBot** + **ChatGPT-User** — OpenAI
- **PerplexityBot** — Perplexity
- **ClaudeBot** + **anthropic-ai** — Anthropic
- **Google-Extended** — Gemini / AI Overviews
- **Bingbot** — Copilot

You can safely block **CCBot** (Common Crawl, training only) without losing any AI citations.

---

## Optimization Strategy

### The Three Pillars + Research-Backed Specifics

```
1. Structure  → Make it extractable (for the retrieval pipeline)
2. Authority  → Make it citable (for the ranking/selection phase)
3. Presence   → Be where AI already looks (for earned media bias)
```

### Pillar 1: Structure — Make Content Extractable

**BLUF (Bottom Line Up Front):** Deliver a dense, direct answer within the first 50 words of every section. Testing shows a 62% chance of being the primary cited source when the answer comes first. Burying the answer 3 paragraphs deep drops you from position 1 to position 24 in AI retrieval.

**Bifurcated keyword strategy:**
- **Headings, titles, meta** — Exact-match keywords users actually search for (satisfies BM25 lexical filter)
- **Body paragraphs** — Dense facts, domain vocabulary, semantic richness (satisfies vector embedding filter)

**Passage engineering:**
- Snippet answers: 40-60 words, direct and conclusive
- Semantic units: 134-167 words, self-contained, no ambiguous pronouns
- Inject a statistic or data point every 150-200 words
- Bulleted lists and tables improve citation recall by 40% vs. unbroken paragraphs

**Combat query fan-out:** Use prompt engineering to generate 50-100 sub-queries for your topic during drafting. Address them systematically through H2/H3 headers phrased as natural-language questions. This protects against the multi-query penalty.

**Content block patterns:** For reusable templates (definition blocks, step-by-step blocks, comparison tables, FAQ blocks, evidence sandwiches), read [references/content-patterns.md](references/content-patterns.md).

### Pillar 2: Authority — Make Content Citable

The Princeton GEO research (KDD 2024) ranked optimization methods by impact:

| Method | Visibility Boost | Application |
|--------|:--:|---|
| **Cite sources** | +40% | Authoritative references with links. 132% boost with Tier-1 sources (.gov, .edu, Gartner, Pew) |
| **Add statistics** | +37% | Specific numbers with dates and sources. Inject every 150-200 words |
| **Add quotations** | +30% | Expert quotes with name, title, organization |
| **Authoritative tone** | +25% | Demonstrated expertise, not marketing fluff |
| **Fluency + Stats combo** | Best combo | Low-ranking sites see up to 115% visibility increase |
| ~~Keyword stuffing~~ | **-10%** | **Actively hurts AI visibility** |

**Entity density:** Pages with 15+ recognized, interconnected entities (people, organizations, standards, regulations) see 4.8x higher selection probability. Map your entity relationships deliberately.

**Freshness:**
- "Last updated: [date]" prominently displayed
- Quarterly minimum refresh for competitive topics
- AI systems differentiate superficial date changes from substantive updates — actually update the content

**E-E-A-T signals:**
- Named authors with real credentials and dedicated bio pages
- First-hand experience demonstrated (case studies, original data, proprietary methodologies)
- Person schema linking authors to their broader digital footprint
- 96% of AI Overview citations come from domains with strong E-E-A-T signals

### Pillar 3: Presence — Be Where AI Looks

AI systems (especially ChatGPT) exhibit heavy algorithmic bias toward **earned media** — third-party sources over brand-owned domains. You're 6.5x more likely to get cited via a third-party mention than your own site.

**Platform omnipresence:**
- Wikipedia mentions (7.8% of all ChatGPT citations)
- Reddit participation (1.8% of ChatGPT citations, growing)
- Industry publications and guest posts
- Review sites (G2, Capterra, TrustRadius for B2B SaaS)
- YouTube (frequently cited by Google AI Overviews)
- LinkedIn and GitHub (Copilot ranking boost)

**Citation seeding:** When the same proprietary statistic or framework appears across 5+ independent high-authority domains, the probability of it becoming the AI's definitive answer approaches certainty. Proactively seed your original data across external publications, PR networks, and academic channels.

### Schema Markup for AI

Structured data gives AI systems an API-like translation of your content. 73% higher selection rate with proper markup.

| Content Type | Schema | Why It Helps |
|---|---|---|
| Articles/Blog posts | `Article`, `BlogPosting` | Author, date, topic identification |
| How-to content | `HowTo` | Step extraction for process queries |
| FAQs | `FAQPage` | Direct Q&A extraction |
| Products | `Product` | Pricing, features, reviews |
| Comparisons | `ItemList` | Structured comparison data |
| Reviews | `Review`, `AggregateRating` | Trust signals |
| Organization | `Organization` | Entity recognition |
| Voice search | `Speakable` | Siri/Alexa compatibility |

For implementation, use the **schema-markup** skill.

### Multimodal Content (156% Selection Boost)

Text-only optimization is insufficient. Pages with mixed media (text + original images + video transcripts) see 156% higher selection. With schema annotations on that media: 317%.

- **No stock photography** — LLMs recognize commodity imagery and attribute zero authority
- **Original infographics and data visualizations** with hyper-descriptive alt-text
- **60-90 second instructional videos** adjacent to relevant text, with machine-readable transcripts immediately below
- **Open access** — avoid aggressive gating that hides content from headless AI crawlers

---

## Content Types That Get Cited Most

| Content Type | Citation Share | Why AI Cites It |
|---|:--:|---|
| **Comparison articles** | ~33% | Structured, balanced, high-intent |
| **Definitive guides** | ~15% | Comprehensive, authoritative |
| **Original research/data** | ~12% | Unique, citable statistics |
| **Best-of/listicles** | ~10% | Clear structure, entity-rich |
| **Product pages** | ~10% | Specific extractable details |
| **How-to guides** | ~8% | Step-by-step structure |
| **Opinion/analysis** | ~10% | Expert perspective, quotable |

**Low performers:** Generic blog posts without structure, thin marketing pages, gated content, undated/unattributed content, PDF-only content.

---

## Monitoring AI Visibility

### What to Track

| Metric | How to Check |
|---|---|
| AI Overview presence | Manual check or Semrush/Ahrefs |
| Brand citation rate | Peec AI, Otterly, ZipTie, LLMrefs |
| Share of AI voice | Peec AI, Otterly |
| Citation sentiment | Manual review across platforms |
| AI referral traffic | GA4 — isolate referrals from chat.openai.com, perplexity.ai, gemini.google.com |

**Timeline:** GEO tactics (schema + semantic chunking) can show initial AI visibility improvements in 2-4 weeks, far faster than traditional SEO's 3-6 month cycle. But content decay is also faster — continuous refresh is required.

### DIY Monthly Check (No Tools)

1. Pick your top 20 queries
2. Run each through ChatGPT, Perplexity, and Google
3. Record: Are you cited? Who is? What page?
4. Log in a spreadsheet, track month-over-month

---

## The Seven Core Ranking Determinants

Aggregated from 15,847 AI search results across 63 industries. Traditional Domain Authority has dropped to r=0.18 correlation. These are what matter now:

| # | Factor | Correlation | Key Threshold |
|---|---|:--:|---|
| 1 | Semantic Completeness | r=0.87 | Blocks scoring 8.5+/10 are 4.2x more likely to be cited |
| 2 | Multi-Modal Integration | r=0.92 | 156% higher selection; 317% with schema |
| 3 | Real-Time Factual Verification | r=0.89 | Dated stats with Tier-1 sources = 89% higher selection |
| 4 | Vector Embedding Alignment | r=0.84 | Cosine similarity >0.88 = 7.3x citation multiplier |
| 5 | E-E-A-T Authority Signals | r=0.81 | 96% of AI citations from strong E-E-A-T domains |
| 6 | Entity Knowledge Graph Density | r=0.76 | 15+ entities = 4.8x selection probability |
| 7 | Structured Data / Schema | 73% boost | JSON-LD removes guesswork from AI extraction |

---

## Common Mistakes

- **Burying the answer** — BLUF failure drops you from position 1 to 24 in retrieval
- **Over-optimizing for one query** — 69% visibility loss across the topic cluster
- **Semantic-rich body text without keyword headings** — Fails BM25 lexical filter, 22-position average drop
- **No freshness signals** — Undated content loses to dated content
- **Gating all content** — AI can't access gated content
- **Ignoring third-party presence** — Wikipedia mention may matter more than your own blog
- **No structured data** — 73% selection boost left on the table
- **Keyword stuffing** — Actively reduces AI visibility by 10%
- **Blocking AI bots** — Those platforms literally cannot cite you
- **Generic content without data** — "We're the best" won't get cited. "Customers see 3x improvement in [metric]" will
- **Stock photography** — LLMs attribute zero authority to commodity imagery
- **Pronoun-heavy paragraphs** — Chunks with ambiguous references get discarded during retrieval

---

## Related Skills

- **seo-audit** — Traditional technical and on-page SEO audits
- **schema-markup** — Implementing structured data
- **content-strategy** — Planning what content to create
- **competitor-alternatives** — Building comparison pages
- **programmatic-seo** — Building SEO pages at scale
- **copywriting** — Writing human-readable, AI-extractable content
