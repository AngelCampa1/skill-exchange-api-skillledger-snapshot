import { scenariosData, type ScenarioData } from '@/lib/data/scenarios-data'
import { industriesData, type IndustryData } from '@/lib/data/industries-data'
import { categoriesData } from '@/lib/data/categories-data'
import { comparisonsData, type ComparisonData } from '@/lib/data/comparisons-data'
import { featuresData, type FeatureData } from '@/lib/data/features-data'
import type { Article } from '@/lib/content'

/**
 * Maps human-readable skill names (used in scenarios & industry pairings)
 * to category slugs. Scenario skills don't always match category names 1:1.
 */
const SKILL_TO_CATEGORY: Record<string, string> = {
  'web development': 'web-development',
  'graphic design': 'design',
  'design': 'design',
  'marketing': 'marketing',
  'writing & content': 'writing',
  'writing': 'writing',
  'business consulting': 'consulting',
  'consulting': 'consulting',
  'legal services': 'legal',
  'legal': 'legal',
  'photography': 'photography',
  'video production': 'video-production',
  'data science': 'data-science',
  'ai & machine learning': 'ai-ml',
  'mobile development': 'mobile-development',
  'finance & accounting': 'finance',
  'finance': 'finance',
  'engineering': 'engineering',
  'seo': 'marketing',
  'copywriting': 'writing',
  'branding & identity design': 'design',
  'software development': 'web-development',
  'digital marketing & seo': 'marketing',
  'legal review & contract drafting': 'legal',
  'contract drafting & negotiation': 'legal',
  'intellectual property counsel': 'legal',
  'compliance consulting': 'consulting',
  // City topSkills mappings
  'ai/ml': 'ai-ml',
  'product design': 'design',
  'product management': 'business',
  'ui/ux design': 'design',
  'ux design': 'design',
  'cloud computing': 'web-development',
  'game development': 'web-development',
  'music production': 'music-audio',
  'real estate photography': 'photography',
  'real estate marketing': 'real-estate',
  'healthcare it': 'healthcare-wellness',
  'wellness coaching': 'healthcare-wellness',
  'financial analysis': 'finance',
  'data analytics': 'data-science',
  'fintech development': 'finance',
}

// Build reverse map: category slug → list of skill name keys that map to it
const CATEGORY_SKILL_NAMES: Record<string, string[]> = {}
for (const [skill, slug] of Object.entries(SKILL_TO_CATEGORY)) {
  if (!CATEGORY_SKILL_NAMES[slug]) CATEGORY_SKILL_NAMES[slug] = []
  CATEGORY_SKILL_NAMES[slug].push(skill)
}

/** Get the category slug for a skill name, or null if no match. */
export function skillToCategorySlug(skillName: string): string | null {
  const slug = SKILL_TO_CATEGORY[skillName.toLowerCase()]
  if (slug && categoriesData.some((c) => c.slug === slug)) return slug
  return null
}

/** Get the category name for a skill name. */
export function skillToCategoryName(skillName: string): string | null {
  const slug = skillToCategorySlug(skillName)
  if (!slug) return null
  return categoriesData.find((c) => c.slug === slug)?.name ?? null
}

/** Find scenarios where skillOffered or skillNeeded maps to the given category. */
export function findScenariosForCategory(categorySlug: string): ScenarioData[] {
  const skillNames = CATEGORY_SKILL_NAMES[categorySlug] ?? []
  if (skillNames.length === 0) return []

  return scenariosData.filter((s) => {
    const offered = s.skillOffered.toLowerCase()
    const needed = s.skillNeeded.toLowerCase()
    return skillNames.includes(offered) || skillNames.includes(needed)
  })
}

/** Find industries whose commonPairings mention skills matching the given category. */
export function findIndustriesForCategory(categorySlug: string): IndustryData[] {
  const skillNames = CATEGORY_SKILL_NAMES[categorySlug] ?? []
  if (skillNames.length === 0) return []

  return industriesData.filter((ind) =>
    ind.commonPairings.some((p) => {
      const offered = p.skillOffered.toLowerCase()
      const needed = p.skillNeeded.toLowerCase()
      return skillNames.includes(offered) || skillNames.includes(needed)
    })
  )
}

/** Map a list of skill names (e.g. city topSkills) to their category slugs. */
export function findCategoriesForSkills(skills: string[]): { skill: string; slug: string; name: string }[] {
  const seen = new Set<string>()
  const results: { skill: string; slug: string; name: string }[] = []
  for (const skill of skills) {
    const slug = skillToCategorySlug(skill)
    if (slug && !seen.has(slug)) {
      seen.add(slug)
      const cat = categoriesData.find((c) => c.slug === slug)
      if (cat) results.push({ skill, slug, name: cat.name })
    }
  }
  return results
}

/** Find scenarios matching any of the given skill names. */
export function findScenariosForSkills(skills: string[]): ScenarioData[] {
  const categorySlugs = new Set<string>()
  for (const skill of skills) {
    const slug = skillToCategorySlug(skill)
    if (slug) categorySlugs.add(slug)
  }
  if (categorySlugs.size === 0) return []

  return scenariosData.filter((s) => {
    const offeredSlug = skillToCategorySlug(s.skillOffered)
    const neededSlug = skillToCategorySlug(s.skillNeeded)
    return (
      (offeredSlug && categorySlugs.has(offeredSlug)) ||
      (neededSlug && categorySlugs.has(neededSlug))
    )
  })
}

/**
 * Find articles whose tags match the glossary term slug or name.
 * Uses lazy import of @/lib/content to avoid poisoning non-fs importers.
 * Only called at build time from glossary [term] pages (staticParams + dynamicParams=false).
 */
export function findArticlesForTerm(termSlug: string, termName: string): Article[] {
  // Lazy require to keep cross-links.ts safe for runtime/client importers
  const { getAllArticles } = require('@/lib/content') as { getAllArticles: () => Article[] }
  const termLower = termName.toLowerCase()
  const slugWords = termSlug.replace(/-/g, ' ')
  return getAllArticles().filter((a) => {
    const tags = a.frontmatter.tags.map((t) => t.toLowerCase())
    const titleLower = a.frontmatter.title.toLowerCase()
    return (
      tags.includes(termSlug) ||
      tags.includes(termLower) ||
      tags.includes(slugWords) ||
      titleLower.includes(termLower)
    )
  })
}

/**
 * Find articles relevant to a category by matching tags against the category's skill names.
 * Uses lazy import of @/lib/content — only called at build time.
 */
export function findArticlesForCategory(categorySlug: string): Article[] {
  const skillNames = CATEGORY_SKILL_NAMES[categorySlug] ?? []
  const { getAllArticles } = require('@/lib/content') as { getAllArticles: () => Article[] }
  return getAllArticles().filter((a) => {
    const tags = a.frontmatter.tags.map((t) => t.toLowerCase())
    // Match if any tag maps to this category, or the category slug itself is a tag
    if (tags.includes(categorySlug)) return true
    return skillNames.some((skill) => tags.includes(skill) || tags.includes(skill.replace(/\s+/g, '-')))
  })
}

/**
 * Find articles relevant to an industry by matching tags against industry pairing skills.
 * Uses lazy import of @/lib/content — only called at build time.
 */
export function findArticlesForIndustry(industrySlug: string): Article[] {
  const industry = industriesData.find((i) => i.slug === industrySlug)
  if (!industry) return []

  const pairingSkills = new Set<string>()
  for (const p of industry.commonPairings) {
    pairingSkills.add(p.skillOffered.toLowerCase())
    pairingSkills.add(p.skillNeeded.toLowerCase())
  }

  // Also match the industry slug itself and name as potential tag matches
  const industryTerms = new Set([industrySlug, industry.name.toLowerCase(), industry.name.toLowerCase().replace(/\s+/g, '-')])

  const { getAllArticles } = require('@/lib/content') as { getAllArticles: () => Article[] }
  return getAllArticles().filter((a) => {
    const tags = a.frontmatter.tags.map((t) => t.toLowerCase())
    const titleLower = a.frontmatter.title.toLowerCase()
    // Match if tags or title reference this specific industry
    if (tags.some((tag) => industryTerms.has(tag))) return true
    if (industryTerms.has(a.slug)) return true
    if (titleLower.includes(industry.name.toLowerCase())) return true
    // Match if any tag overlaps with industry pairing skills
    return tags.some((tag) => pairingSkills.has(tag) || pairingSkills.has(tag.replace(/-/g, ' ')))
  })
}

/**
 * Find industries relevant to a city based on its top skills.
 * Maps city skills → category slugs → industries whose pairings use those categories.
 */
export function findIndustriesForCity(citySkills: string[]): IndustryData[] {
  const categorySlugs = new Set<string>()
  for (const skill of citySkills) {
    const slug = skillToCategorySlug(skill)
    if (slug) categorySlugs.add(slug)
  }
  if (categorySlugs.size === 0) return []

  return industriesData.filter((ind) =>
    ind.commonPairings.some((p) => {
      const offeredSlug = SKILL_TO_CATEGORY[p.skillOffered.toLowerCase()]
      const neededSlug = SKILL_TO_CATEGORY[p.skillNeeded.toLowerCase()]
      return (
        (offeredSlug && categorySlugs.has(offeredSlug)) ||
        (neededSlug && categorySlugs.has(neededSlug))
      )
    })
  )
}

/** Find categories matching article tags. */
export function findCategoriesForArticle(tags: string[]): { slug: string; name: string }[] {
  const categories: { slug: string; name: string }[] = []
  const seen = new Set<string>()
  for (const tag of tags) {
    const slug = skillToCategorySlug(tag)
    if (slug && !seen.has(slug)) {
      seen.add(slug)
      const cat = categoriesData.find((c) => c.slug === slug)
      if (cat) categories.push({ slug: cat.slug, name: cat.name })
    }
  }
  return categories.slice(0, 4)
}

/** Find trade pairs relevant to an article based on its tags. */
export function findTradePairsForArticle(tags: string[]): { skillA: string; skillB: string; nameA: string; nameB: string }[] {
  const categorySlugs = tags.map((t) => skillToCategorySlug(t)).filter((s): s is string => !!s)
  const unique = [...new Set(categorySlugs)]
  const pairs: { skillA: string; skillB: string; nameA: string; nameB: string }[] = []
  for (let i = 0; i < unique.length && pairs.length < 3; i++) {
    for (let j = i + 1; j < unique.length && pairs.length < 3; j++) {
      const catA = categoriesData.find((c) => c.slug === unique[i])
      const catB = categoriesData.find((c) => c.slug === unique[j])
      if (catA && catB) {
        pairs.push({ skillA: catA.slug, skillB: catB.slug, nameA: catA.name, nameB: catB.name })
      }
    }
  }
  return pairs
}

/**
 * Find comparisons relevant to a category.
 * All comparisons are platform-level ("SkillLedger vs X") rather than category-specific,
 * so keyword-matching against category skill names rarely produces a match. The fallback
 * (top 3 from the full list) is intentional: every page needs comparison links for funnel
 * progression, and the platform comparisons are universally relevant regardless of category.
 * If category-specific comparison pages are added in future, add a `relatedCategories` field
 * to `ComparisonData` (mirroring `FeatureData.relatedCategories`) for precise matching.
 */
export function findComparisonsForCategory(categorySlug: string): ComparisonData[] {
  const skillNames = CATEGORY_SKILL_NAMES[categorySlug] ?? []

  // Try keyword match on comparison titles/descriptions first
  const matched = comparisonsData.filter((c) => {
    const text = `${c.title} ${c.description}`.toLowerCase()
    return skillNames.some((skill) => text.includes(skill))
  })

  return matched.length > 0 ? matched.slice(0, 3) : comparisonsData.slice(0, 3)
}

/**
 * Find comparisons relevant to an industry.
 * Uses the industry's pairing skills for keyword matching, falling back to top 3.
 */
export function findComparisonsForIndustry(industrySlug: string): ComparisonData[] {
  const industry = industriesData.find((i) => i.slug === industrySlug)
  if (!industry) return comparisonsData.slice(0, 3)

  const pairingSkills = new Set<string>()
  for (const p of industry.commonPairings) {
    pairingSkills.add(p.skillOffered.toLowerCase())
    pairingSkills.add(p.skillNeeded.toLowerCase())
  }

  const matched = comparisonsData.filter((c) => {
    const text = `${c.title} ${c.description}`.toLowerCase()
    return [...pairingSkills].some((skill) => text.includes(skill))
  })

  return matched.length > 0 ? matched.slice(0, 3) : comparisonsData.slice(0, 3)
}

/** Find features whose relatedCategories include the given category slug. */
export function findFeaturesForCategory(categorySlug: string): FeatureData[] {
  return featuresData.filter((f) => f.relatedCategories.includes(categorySlug))
}

/**
 * Resolve a tag string to a category slug, handling both space-separated and dash-separated forms.
 * Tries `skillToCategorySlug` first, then falls back to the raw SKILL_TO_CATEGORY map with
 * dash-to-space normalisation (covering tags like 'web-development' that aren't in the primary map).
 */
function tagToCategorySlug(tag: string): string | null {
  return skillToCategorySlug(tag) ?? SKILL_TO_CATEGORY[tag.toLowerCase().replace(/-/g, ' ')] ?? null
}

/**
 * Find how-to guides (scenarios) relevant to an article's tags.
 * Maps tags → category slugs → scenarios for those categories.
 */
export function findHowToGuidesForArticle(tags: string[]): ScenarioData[] {
  const categorySlugs = new Set<string>()
  for (const tag of tags) {
    const slug = tagToCategorySlug(tag)
    if (slug) categorySlugs.add(slug)
  }
  if (categorySlugs.size === 0) return []

  const seen = new Set<string>()
  const results: ScenarioData[] = []
  for (const catSlug of categorySlugs) {
    for (const scenario of findScenariosForCategory(catSlug)) {
      if (!seen.has(scenario.slug)) {
        seen.add(scenario.slug)
        results.push(scenario)
      }
    }
  }
  return results.slice(0, 3)
}

/**
 * Find comparisons relevant to an article's tags.
 * Maps tags → category slugs → comparisons for those categories.
 */
export function findComparisonsForArticle(tags: string[]): ComparisonData[] {
  const seen = new Set<string>()
  const results: ComparisonData[] = []
  for (const tag of tags) {
    const slug = tagToCategorySlug(tag)
    if (slug) {
      for (const comparison of findComparisonsForCategory(slug)) {
        if (!seen.has(comparison.slug)) {
          seen.add(comparison.slug)
          results.push(comparison)
        }
      }
    }
  }
  return results.length > 0 ? results.slice(0, 3) : comparisonsData.slice(0, 3)
}

/** Find scenarios whose skillOffered/skillNeeded match any commonPairing in the industry. */
export function findScenariosForIndustry(industrySlug: string): ScenarioData[] {
  const industry = industriesData.find((i) => i.slug === industrySlug)
  if (!industry) return []

  // Collect all skill names from the industry's pairings
  const pairingSkills = new Set<string>()
  for (const p of industry.commonPairings) {
    pairingSkills.add(p.skillOffered.toLowerCase())
    pairingSkills.add(p.skillNeeded.toLowerCase())
  }

  // Map those skill names to category slugs, then find scenarios matching those categories
  const categorySlugs = new Set<string>()
  for (const skill of pairingSkills) {
    const slug = SKILL_TO_CATEGORY[skill]
    if (slug) categorySlugs.add(slug)
  }

  return scenariosData.filter((s) => {
    const offeredSlug = SKILL_TO_CATEGORY[s.skillOffered.toLowerCase()]
    const neededSlug = SKILL_TO_CATEGORY[s.skillNeeded.toLowerCase()]
    return (
      (offeredSlug && categorySlugs.has(offeredSlug)) ||
      (neededSlug && categorySlugs.has(neededSlug))
    )
  })
}
