# How Each AI Platform Picks Sources

Each AI search platform has its own search index, ranking logic, and algorithmic biases. This guide covers what matters for getting cited on each one.

Sources: Princeton GEO study (KDD 2024), SE Ranking domain authority study, ZipTie content-answer fit analysis, Aggarwal et al. GEO (2024), IF-GEO framework (2026), Gemini Deep Research aggregate (2026).

---

## The Fundamentals

Every AI platform shares three baseline requirements:

1. **Your content must be in their index** — Each platform uses a different search backend (Google, Bing, Brave, or their own). If you're not indexed, you can't be cited.
2. **Your content must be crawlable** — AI bots need access via robots.txt. Block the bot, lose the citation.
3. **Your content must be extractable** — AI systems pull passages, not pages. Clear structure and self-contained paragraphs win.

Beyond these, each platform weights different signals.

---

## Google AI Overviews / Gemini

**Search backend:** Google's own index + Knowledge Graph
**Reach:** AI Overviews appear in 85%+ of informational queries. Gemini also powers Apple Intelligence/Siri via a 2026 multi-year agreement, making local context and geographic reputation paramount on mobile.

### Algorithmic Bias: The E-E-A-T Shield

Gemini uses Google's E-E-A-T framework as an aggressive filter against generic AI-generated content. Because generative models have an ~11% hallucination rate in complex sectors like finance and insurance, Gemini prioritizes domains with high institutional credibility, verified authorship, and real-time verifiable facts.

### Multimodal Preference

Gemini has a massive statistical preference for content that fuses text with rich visual data. It processes images, video, and text simultaneously within its 2-million token context window. Pages with mixed media and schema annotations see up to 317% higher selection.

### What to Focus On

- **Schema markup is the single biggest lever** — Article, FAQPage, HowTo, Product, and Speakable schemas (30-40% visibility boost)
- Build topical authority through content clusters with strong internal linking
- Include named, sourced citations in your content (132% visibility boost with Tier-1 sources)
- Author bios with real credentials — E-E-A-T is weighted heavily
- Get into Google's Knowledge Graph (accurate Wikipedia entry helps)
- Target "how to" and "what is" query patterns — these trigger AI Overviews most often
- Original infographics, data charts, and video transcripts (not stock photos)
- Only ~15% of AI Overview sources overlap with conventional organic Top 10 — strong structured data can get you cited even without a page-1 ranking

---

## ChatGPT (SearchGPT)

**Search backend:** Bing-based index + real-time organic web crawls
**Reach:** 800M weekly active users, 2B+ daily queries, 68% of global AI chatbot traffic
**Conversion rate:** 14.2%

### Algorithmic Bias: Earned Media

ChatGPT exhibits a systematic bias toward "earned media" — authoritative third-party sources over brand-owned domains. It actively seeks external verification and aggregates third-party validation signals. Organizations cannot rely solely on their own websites; they must build a pervasive, authoritative presence across trusted secondary domains.

**Where ChatGPT looks beyond your site:**
- Wikipedia: 7.8% of all citations
- Reddit: 1.8% of all citations
- Forbes: 1.1% of all citations
- Review sites, industry publications, guest posts

### Content-Answer Fit

A ZipTie analysis of 400,000 pages found that how well your content's style and structure matches ChatGPT's own response format accounts for ~55% of citation likelihood. Domain authority is only 12%, on-page structure 14%. Write the way ChatGPT would answer the question.

### Key Signals

| Signal | Weight | Detail |
|---|---|---|
| Content-answer fit | ~55% | Match ChatGPT's response format |
| Domain authority | ~12% | High referring domain counts (350K+) = 8.4 citations per response |
| Trust score | threshold | Sites scoring 91-96 (vs 97-100) drop from 8.4 to 6 citations |
| Freshness | 3.2x | Content updated within 30 days gets cited 3.2x more |

### What to Focus On

- Invest in backlinks and domain authority — strongest baseline signal
- Update competitive content at least monthly
- Structure content the way ChatGPT structures answers (conversational, direct, well-organized)
- Include verifiable statistics with named sources
- Clean heading hierarchy (H1 > H2 > H3) with descriptive headings
- Build presence on Wikipedia, Reddit, and industry publications

---

## Claude

**Search backend:** Brave Search (when web search is enabled)
**Conversion rate:** 16.8% — highest among all answer engines
**Architecture:** Multi-agent research system with parallel autonomous retrieval agents

### Algorithmic Bias: Contextual Depth

Claude uses a sophisticated multi-agent research architecture. Rather than simple query-and-retrieve, it dynamically plans research processes, creates parallel retrieval agents to scour different databases simultaneously, and adapts search parameters to new findings in real-time.

With an industry-leading context window and superior reasoning benchmarks, Claude is designed for deep logical analysis and exhaustive data processing. It is the most selective engine — it demands extreme semantic depth, explicit structural logic, and complete avoidance of superficial marketing rhetoric.

### What to Focus On

- Verify content appears in Brave Search results (search.brave.com)
- Allow ClaudeBot and anthropic-ai user agents in robots.txt
- Maximize factual density — specific numbers, named sources, dated statistics
- Use exhaustive structural logic with clear heading hierarchy
- Cite authoritative sources within content
- Aim to be the most factually accurate and comprehensive source on your topic
- Zero marketing rhetoric — Claude algorithmically penalizes promotional language
- Comprehensive technical documentation and peer-reviewed research perform best

---

## Perplexity

**Search backend:** Own index + Google's, with multiple reranking passes
**Architecture:** Initial relevance retrieval → traditional ranking → ML-based quality evaluation that can discard entire result sets

### Algorithmic Bias: Research-Oriented

Perplexity is the most transparent AI search (always shows clickable source links). It maintains curated lists of authoritative domains with inherent ranking boosts. Uses a time-decay algorithm that evaluates new content quickly, giving fresh publishers a real shot.

### Unique Content Preferences

- **FAQ Schema (JSON-LD)** — Pages with FAQ structured data get cited noticeably more
- **PDF documents** — Publicly accessible PDFs (whitepapers, research reports) are prioritized
- **Publishing velocity** — How frequently you publish matters more than keyword targeting
- **Self-contained paragraphs** — Prefers atomic, semantically complete paragraphs it can extract cleanly

### What to Focus On

- Allow PerplexityBot in robots.txt
- Implement FAQPage schema on any page with Q&A content
- Host PDF resources publicly (whitepapers, guides, reports)
- Add Article schema with publication and modification timestamps
- Write in clear, self-contained paragraphs
- Build deep topical authority in your specific niche

---

## Microsoft Copilot

**Search backend:** Bing's index
**Distribution:** Edge, Windows, Microsoft 365, Bing Search

### Algorithmic Bias: Microsoft Ecosystem

The ecosystem connection creates unique optimization opportunities. LinkedIn and GitHub mentions provide ranking boosts other platforms don't offer. Copilot puts more weight on page speed.

### What to Focus On

- Submit site to Bing Webmaster Tools (many sites only submit to Google Search Console)
- Use IndexNow protocol for faster indexing
- Optimize page speed to under 2 seconds
- Write clear entity definitions — make definitions explicit and extractable
- Build presence on LinkedIn (articles, company page) and GitHub if relevant
- Ensure Bingbot has full crawl access

---

## Allowing AI Bots in robots.txt

If your robots.txt blocks an AI bot, that platform can't cite your content:

```
User-agent: GPTBot           # OpenAI — powers ChatGPT search
User-agent: ChatGPT-User     # ChatGPT browsing mode
User-agent: PerplexityBot    # Perplexity AI search
User-agent: ClaudeBot        # Anthropic Claude
User-agent: anthropic-ai     # Anthropic Claude (alternate)
User-agent: Google-Extended   # Google Gemini and AI Overviews
User-agent: Bingbot          # Microsoft Copilot (via Bing)
Allow: /
```

**Training vs. search:** Some AI bots handle both training and search. If you want citation but not training, options are limited. However, you can safely block **CCBot** (Common Crawl) without affecting any AI search citations — it's training-only.

---

## Priority Order

If optimizing for the first time:

1. **Google AI Overviews** — Largest reach (85%+ informational queries). Add schema, cited sources, E-E-A-T signals.
2. **ChatGPT** — Most-used standalone AI search. Focus on freshness, domain authority, content-answer fit.
3. **Perplexity** — Valuable for research/tech audiences. Add FAQ schema, publish PDFs, write self-contained paragraphs.
4. **Claude** — Highest conversion rate (16.8%). Worth targeting for B2B and high-ticket. Requires maximum factual depth.
5. **Copilot** — If audience skews enterprise/Microsoft. Bing indexing + LinkedIn presence.

**Actions that help everywhere:**
1. Allow all AI bots in robots.txt
2. Implement schema markup (FAQPage, Article, Organization minimum)
3. Include statistics with named sources
4. Update content regularly — monthly for competitive topics
5. Clear heading structure (H1 > H2 > H3)
6. Page load under 2 seconds
7. Author bios with credentials
