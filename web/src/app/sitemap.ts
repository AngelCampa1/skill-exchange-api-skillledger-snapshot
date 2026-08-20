import { MetadataRoute } from 'next'
import { categoriesData } from '@/lib/data/categories-data'
import { citiesData } from '@/lib/data/cities-data'
import { glossaryData } from '@/lib/data/glossary-data'
import { scenariosData } from '@/lib/data/scenarios-data'
import { industriesData } from '@/lib/data/industries-data'
import { comparisonsData } from '@/lib/data/comparisons-data'
import { featuresData } from '@/lib/data/features-data'
import { getAllArticles } from '@/lib/content'

// ============================================================================
// Types
// ============================================================================

export interface SitemapEntry {
  url: string
  lastModified?: Date | string
  changeFrequency?: 'always' | 'hourly' | 'daily' | 'weekly' | 'monthly' | 'yearly' | 'never'
  priority?: number
}

interface PublicProject {
  id: string
  title: string
  updatedAt: string
}

// ============================================================================
// Constants
// ============================================================================

const BASE_URL = 'https://skillledger.app'
const API_URL = process.env.NEXT_PUBLIC_API_URL || 'https://api.skillledger.app'

// ============================================================================
// Force static generation — pre-rendered during `next build` on Node.js,
// served as a static asset on Cloudflare Workers (avoids runtime fs issues)
// ============================================================================

export const dynamic = 'force-static'

// ============================================================================
// Static Pages (excludes /login and /register)
// ============================================================================

export function getStaticPages(): SitemapEntry[] {
  const currentDate = new Date()

  return [
    { url: BASE_URL, lastModified: currentDate, changeFrequency: 'daily', priority: 1 },
    { url: `${BASE_URL}/marketplace`, lastModified: currentDate, changeFrequency: 'daily', priority: 0.9 },
    { url: `${BASE_URL}/projects/search`, lastModified: currentDate, changeFrequency: 'daily', priority: 0.8 },
    { url: `${BASE_URL}/categories`, lastModified: currentDate, changeFrequency: 'weekly', priority: 0.8 },
    { url: `${BASE_URL}/glossary`, lastModified: currentDate, changeFrequency: 'monthly', priority: 0.7 },
    { url: `${BASE_URL}/resources`, lastModified: currentDate, changeFrequency: 'weekly', priority: 0.7 },
    { url: `${BASE_URL}/tools/barter-valuation-calculator`, lastModified: currentDate, changeFrequency: 'monthly', priority: 0.7 },
    { url: `${BASE_URL}/resources/templates`, lastModified: currentDate, changeFrequency: 'monthly', priority: 0.7 },
    { url: `${BASE_URL}/compare`, lastModified: currentDate, changeFrequency: 'monthly', priority: 0.7 },
    { url: `${BASE_URL}/how-to`, lastModified: currentDate, changeFrequency: 'weekly', priority: 0.8 },
    { url: `${BASE_URL}/industries`, lastModified: currentDate, changeFrequency: 'weekly', priority: 0.8 },
    { url: `${BASE_URL}/skill-exchange`, lastModified: currentDate, changeFrequency: 'weekly', priority: 0.8 },
    { url: `${BASE_URL}/about`, lastModified: currentDate, changeFrequency: 'monthly', priority: 0.8 },
    { url: `${BASE_URL}/pricing`, lastModified: currentDate, changeFrequency: 'monthly', priority: 0.9 },
    { url: `${BASE_URL}/faq`, lastModified: currentDate, changeFrequency: 'monthly', priority: 0.7 },
    { url: `${BASE_URL}/features`, lastModified: currentDate, changeFrequency: 'monthly', priority: 0.8 },
    { url: `${BASE_URL}/tools`, lastModified: currentDate, changeFrequency: 'monthly', priority: 0.7 },
    { url: `${BASE_URL}/skill-match`, lastModified: currentDate, changeFrequency: 'monthly', priority: 0.8 },
    { url: `${BASE_URL}/trade`, lastModified: currentDate, changeFrequency: 'monthly', priority: 0.8 },
    { url: `${BASE_URL}/locations`, lastModified: currentDate, changeFrequency: 'monthly', priority: 0.8 },
    { url: `${BASE_URL}/privacy`, lastModified: currentDate, changeFrequency: 'yearly', priority: 0.3 },
    { url: `${BASE_URL}/terms`, lastModified: currentDate, changeFrequency: 'yearly', priority: 0.3 },
  ]
}

// ============================================================================
// Dynamic Project Pages
// ============================================================================

export async function getProjectPages(): Promise<SitemapEntry[]> {
  try {
    const response = await fetch(`${API_URL}/api/v1/projects/public`, {
      next: { revalidate: 3600 },
    })
    if (!response.ok) return []
    const data = await response.json()
    const projects: PublicProject[] = data.projects || []
    return projects.map((project) => ({
      url: `${BASE_URL}/projects/${project.id}`,
      lastModified: project.updatedAt ? new Date(project.updatedAt) : new Date(),
      changeFrequency: 'weekly' as const,
      priority: 0.7,
    }))
  } catch {
    return []
  }
}

// ============================================================================
// pSEO Pages
// ============================================================================

export function getCategoryPages(): SitemapEntry[] {
  const currentDate = new Date('2026-03-15')
  return categoriesData.map((cat) => ({
    url: `${BASE_URL}/categories/${cat.slug}`,
    lastModified: currentDate,
    changeFrequency: 'weekly' as const,
    priority: 0.7,
  }))
}

export function getCityPages(): SitemapEntry[] {
  const currentDate = new Date('2026-03-15')
  return citiesData.map((city) => ({
    url: `${BASE_URL}/skill-exchange/${city.slug}`,
    lastModified: currentDate,
    changeFrequency: 'monthly' as const,
    priority: 0.6,
  }))
}

export function getGlossaryPages(): SitemapEntry[] {
  const currentDate = new Date('2026-03-15')
  return glossaryData.map((term) => ({
    url: `${BASE_URL}/glossary/${term.slug}`,
    lastModified: currentDate,
    changeFrequency: 'yearly' as const,
    priority: 0.5,
  }))
}

export function getScenarioPages(): SitemapEntry[] {
  const currentDate = new Date('2026-03-15')
  return scenariosData.map((s) => ({
    url: `${BASE_URL}/how-to/${s.slug}`,
    lastModified: currentDate,
    changeFrequency: 'monthly' as const,
    priority: 0.6,
  }))
}

export function getIndustryPages(): SitemapEntry[] {
  const currentDate = new Date('2026-03-15')
  return industriesData.map((industry) => ({
    url: `${BASE_URL}/industries/${industry.slug}`,
    lastModified: currentDate,
    changeFrequency: 'weekly' as const,
    priority: 0.7,
  }))
}

export function getComparisonPages(): SitemapEntry[] {
  const currentDate = new Date('2026-03-15')
  return comparisonsData.map((comp) => ({
    url: `${BASE_URL}/compare/${comp.slug}`,
    lastModified: currentDate,
    changeFrequency: 'monthly' as const,
    priority: 0.7,
  }))
}

export function getFeaturePages(): SitemapEntry[] {
  const currentDate = new Date()
  return featuresData.map((feature) => ({
    url: `${BASE_URL}/features/${feature.slug}`,
    lastModified: currentDate,
    changeFrequency: 'monthly' as const,
    priority: 0.8,
  }))
}

export function getSkillPairingPages(): SitemapEntry[] {
  const currentDate = new Date('2026-03-17')
  return categoriesData.flatMap((a) =>
    categoriesData
      .filter((b) => b.slug !== a.slug)
      .map((b) => ({
        url: `${BASE_URL}/trade/${a.slug}/for/${b.slug}`,
        lastModified: currentDate,
        changeFrequency: 'monthly' as const,
        priority: 0.6,
      }))
  )
}

export function getCitySkillPages(): SitemapEntry[] {
  const currentDate = new Date('2026-03-17')
  return citiesData.flatMap((city) =>
    categoriesData.map((cat) => ({
      url: `${BASE_URL}/locations/${city.slug}/${cat.slug}`,
      lastModified: currentDate,
      changeFrequency: 'monthly' as const,
      priority: 0.5,
    }))
  )
}

export function getArticlePages(): SitemapEntry[] {
  // Reads from the filesystem at build time — stays automatically in sync with content/articles/
  return getAllArticles().map((article) => ({
    url: `${BASE_URL}/resources/${article.slug}`,
    lastModified: new Date(article.frontmatter.publishedAt),
    changeFrequency: 'monthly' as const,
    priority: 0.7,
  }))
}

// ============================================================================
// Main Sitemap Function — single sitemap (no segmentation)
// ============================================================================

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const staticPages = getStaticPages()
  const articlePages = getArticlePages()
  const categoryPages = getCategoryPages()
  const industryPages = getIndustryPages()
  const scenarioPages = getScenarioPages()
  const comparisonPages = getComparisonPages()
  const featurePages = getFeaturePages()
  const glossaryPages = getGlossaryPages()
  const skillPairingPages = getSkillPairingPages()
  const cityPages = getCityPages()
  const citySkillPages = getCitySkillPages()
  const projectPages = await getProjectPages()

  return [
    ...staticPages,
    ...articlePages,
    ...categoryPages,
    ...industryPages,
    ...scenarioPages,
    ...comparisonPages,
    ...featurePages,
    ...glossaryPages,
    ...skillPairingPages,
    ...cityPages,
    ...citySkillPages,
    ...projectPages,
  ]
}
