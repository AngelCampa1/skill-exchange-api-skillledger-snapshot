# AEO and GEO Content Patterns

Reusable content block patterns optimized for answer engines and AI citation.

---

## Contents
- Answer Engine Optimization (AEO) Patterns (Definition Block, Step-by-Step Block, Comparison Table Block, Pros and Cons Block, FAQ Block, Listicle Block)
- Generative Engine Optimization (GEO) Patterns (Statistic Citation Block, Expert Quote Block, Authoritative Claim Block, Self-Contained Semantic Unit, Evidence Sandwich Block)
- Chunk Engineering Guidelines
- Domain-Specific GEO Tactics (Technology, Health/Medical, Financial, Legal, Business/Marketing)
- Voice Search Optimization

## Answer Engine Optimization (AEO) Patterns

These patterns help content appear in featured snippets, AI Overviews, voice search results, and answer boxes.

### Definition Block

Use for "What is [X]?" queries. Lead with BLUF (Bottom Line Up Front) — deliver the answer in the first 50 words.

```markdown
## What is [Term]?

[Term] is [concise 1-sentence definition]. [Expanded 1-2 sentence explanation with key characteristics]. [Brief context on why it matters or how it's used].
```

**Example:**
```markdown
## What is Answer Engine Optimization?

Answer Engine Optimization (AEO) is the practice of structuring content so AI-powered systems can easily extract and present it as direct answers to user queries. Unlike traditional SEO that focuses on ranking in search results, AEO optimizes for featured snippets, AI Overviews, and voice assistant responses. This approach has become essential as over 60% of Google searches now end without a click.
```

### Step-by-Step Block

Use for "How to [X]" queries. Optimal for list snippets and HowTo schema.

```markdown
## How to [Action/Goal]

[1-sentence overview of the process]

1. **[Step Name]**: [Clear action description in 1-2 sentences]
2. **[Step Name]**: [Clear action description in 1-2 sentences]
3. **[Step Name]**: [Clear action description in 1-2 sentences]
4. **[Step Name]**: [Clear action description in 1-2 sentences]
5. **[Step Name]**: [Clear action description in 1-2 sentences]

[Optional: Brief note on expected outcome or time estimate]
```

### Comparison Table Block

Use for "[X] vs [Y]" queries. Tables beat prose for comparison content — AI systems extract these directly.

```markdown
## [Option A] vs [Option B]: [Brief Descriptor]

| Feature | [Option A] | [Option B] |
|---------|------------|------------|
| [Criteria 1] | [Value/Description] | [Value/Description] |
| [Criteria 2] | [Value/Description] | [Value/Description] |
| [Criteria 3] | [Value/Description] | [Value/Description] |
| [Criteria 4] | [Value/Description] | [Value/Description] |
| Best For | [Use case] | [Use case] |

**Bottom line**: [1-2 sentence recommendation based on different needs]
```

### Pros and Cons Block

Use for evaluation queries: "Is [X] worth it?", "Should I [X]?"

```markdown
## Advantages and Disadvantages of [Topic]

[1-sentence overview of the evaluation context]

### Pros

- **[Benefit category]**: [Specific explanation]
- **[Benefit category]**: [Specific explanation]
- **[Benefit category]**: [Specific explanation]

### Cons

- **[Drawback category]**: [Specific explanation]
- **[Drawback category]**: [Specific explanation]
- **[Drawback category]**: [Specific explanation]

**Verdict**: [1-2 sentence balanced conclusion with recommendation]
```

### FAQ Block

Use for topic pages with multiple common questions. Essential for FAQPage schema.

```markdown
## Frequently Asked Questions

### [Question phrased exactly as users search]?

[Direct answer in first sentence]. [Supporting context in 2-3 additional sentences].

### [Question phrased exactly as users search]?

[Direct answer in first sentence]. [Supporting context in 2-3 additional sentences].
```

**Tips for FAQ questions:**
- Use natural question phrasing ("How do I..." not "How does one...")
- Include question words: what, how, why, when, where, who, which
- Match "People Also Ask" queries from search results
- Keep answers between 50-100 words
- Each answer must be self-contained (no pronouns pointing to other answers)

### Listicle Block

Use for "Best [X]", "Top [X]", "[Number] ways to [X]" queries.

```markdown
## [Number] Best [Items] for [Goal/Purpose]

[1-2 sentence intro establishing context and selection criteria]

### 1. [Item Name]

[Why it's included in 2-3 sentences with specific benefits]

### 2. [Item Name]

[Why it's included in 2-3 sentences with specific benefits]
```

---

## Generative Engine Optimization (GEO) Patterns

These patterns optimize content for citation by AI assistants like ChatGPT, Claude, Perplexity, and Gemini.

### Statistic Citation Block

Statistics increase AI citation rates by 37%. Always include sources and dates.

```markdown
[Claim statement]. According to [Source/Organization] ([Year]), [specific statistic with number and timeframe]. [Context for why this matters].
```

**Example:**
```markdown
Mobile optimization is no longer optional for SEO success. According to Google's 2025 Core Web Vitals report, 70% of web traffic now comes from mobile devices, and pages failing mobile usability standards see 24% higher bounce rates. This makes mobile-first indexing a critical ranking factor.
```

### Expert Quote Block

Named expert attribution adds 30% visibility boost.

```markdown
"[Direct quote from expert]," says [Expert Name], [Title/Role] at [Organization]. [1 sentence of context or interpretation].
```

### Authoritative Claim Block

Structure claims for easy AI extraction with clear attribution.

```markdown
[Topic] [verb: is/has/requires/involves] [clear, specific claim]. [Source] [confirms/reports/found] that [supporting evidence]. This [explains/means/suggests] [implication or action].
```

### Self-Contained Semantic Unit

The core building block for RAG retrieval. Must work as a standalone passage with zero external context.

**Rules:**
- 134-167 words (optimal range for RAG chunking)
- No ambiguous pronouns — replace "this solution" with the actual name
- No references to other sections ("as mentioned above," "see below")
- Include at least one specific data point or named source
- Convey one complete idea with claim + evidence + implication

```markdown
**[Topic/Question]**: [Complete, self-contained answer that makes sense extracted in isolation. Include the specific entity names, not pronouns. Provide at least one statistic or data point with source and date. State the practical implication or actionable takeaway. Target 134-167 words total.]
```

### Evidence Sandwich Block

Structure claims with evidence for maximum credibility.

```markdown
[Opening claim statement].

Evidence supporting this includes:
- [Data point 1 with source and date]
- [Data point 2 with source and date]
- [Data point 3 with source and date]

[Concluding statement connecting evidence to actionable insight].
```

---

## Chunk Engineering Guidelines

AI systems extract information in modular chunks. Engineer your content to survive the chunking process.

### The Self-Containment Test

For every paragraph, ask: "If someone read only this paragraph with zero context, would they understand the claim, who/what it's about, and why it matters?" If not, rewrite it.

### Pronoun Replacement Checklist

| Instead of... | Write... |
|---|---|
| "This solution" | "The CAM audit platform" |
| "The company" | "ExampleAudit" |
| "As mentioned above" | [Restate the fact] |
| "Their" (ambiguous) | [The specific entity's name] |
| "It" (unclear referent) | [The specific thing] |

### Fact Density Target

Inject a concrete data point every 150-200 words:
- Statistics with source and date
- Named expert quote with title
- Specific numerical claim
- Dated reference to a study or report

### Structural Formatting

Bulleted lists and tables improve citation recall by 40% vs. unbroken paragraphs. Use:
- Tables for comparisons
- Numbered lists for processes
- Bulleted lists for features/benefits
- Bold key terms for scannability

---

## Domain-Specific GEO Tactics

### Technology Content
- Emphasize technical precision and correct terminology
- Include version numbers and dates for software/tools
- Reference official documentation
- Add code examples where relevant

### Health/Medical Content
- Cite peer-reviewed studies with publication details
- Include expert credentials (MD, RN, etc.)
- Note study limitations and context
- Add "last reviewed" dates

### Financial Content
- Reference regulatory bodies (SEC, FTC, etc.)
- Include specific numbers with timeframes
- Note that information is educational, not advice
- Cite recognized financial institutions

### Legal Content
- Cite specific laws, statutes, and regulations
- Reference jurisdiction clearly
- Include professional disclaimers
- Note when professional consultation is advised

### Business/Marketing Content
- Include case studies with measurable results
- Reference industry research and reports
- Add percentage changes and timeframes
- Quote recognized thought leaders

---

## Voice Search Optimization

Voice queries are conversational and question-based.

### Question Formats for Voice
- "What is..."
- "How do I..."
- "Where can I find..."
- "Why does..."
- "When should I..."
- "Who is..."

### Voice-Optimized Answer Structure
- Lead with direct answer (under 30 words ideal)
- Use natural, conversational language
- Avoid jargon unless targeting expert audience
- Include local context where relevant
- Structure for single spoken response
- Implement Speakable schema for Siri/Alexa compatibility
