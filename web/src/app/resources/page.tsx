import { Metadata } from 'next'
import Link from 'next/link'
import { getAllArticles } from '@/lib/content'
import { comparisonsData } from '@/lib/data/comparisons-data'
import { scenariosData } from '@/lib/data/scenarios-data'
import { buildPublicPageMetadata, generateBreadcrumbSchema, generateItemListSchema, generateWebPageSchema, SITE_CONFIG } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import type { FunnelStage } from '@/lib/funnel'
import { RelatedHubs } from '@/components/marketing/RelatedHubs'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

// Stage badge style uses design tokens from globals.css / tailwind.config.js
const stageBadgeClass: Record<FunnelStage, string> = {
  tofu: 'bg-info/10 text-info',
  mofu: 'bg-warning/10 text-warning-foreground',
  bofu: 'bg-success/10 text-success',
}

export const dynamic = 'force-static'

export const metadata: Metadata = buildPublicPageMetadata(
  'Resources',
  'Articles, guides, templates, and tools for professional skill exchange. Learn about barter economics, tax compliance, freelancing, and building trust in service swaps.',
  '/resources',
  ['skill exchange resources', 'barter guides', 'freelancer resources', 'barter tax guide']
)

// Ordered list of silos for deterministic rendering
const siloOrder = [
  'barter-economy',
  'freelancing',
  'skill-exchange',
  'tax-and-legal',
  'trust-and-safety',
  'credit-systems',
  'collaboration',
  'industries',
] as const

const siloLabels: Record<string, string> = {
  'barter-economy': 'Barter Economy',
  'credit-systems': 'Credit Systems',
  'freelancing': 'Freelancing',
  'skill-exchange': 'Skill Exchange',
  'tax-and-legal': 'Tax & Legal',
  'trust-and-safety': 'Trust & Safety',
  'collaboration': 'Collaboration',
  'industries': 'Industries',
}

const stageLabels: Record<string, { label: string; desc: string; funnelStage: FunnelStage }> = {
  awareness: { label: 'Just Exploring', desc: 'Get up to speed on skill exchange and barter economics.', funnelStage: 'tofu' },
  consideration: { label: 'Evaluating Options', desc: 'Compare platforms, read how-to guides, and understand your choices.', funnelStage: 'mofu' },
  decision: { label: 'Ready to Start', desc: 'Pricing, setup guides, and next steps to get your first exchange going.', funnelStage: 'bofu' },
}

export default function ResourcesPage() {
  const articles = getAllArticles()
  const topComparisons = comparisonsData.slice(0, 3)
  const topHowTo = scenariosData.slice(0, 3)


  // Group articles by buyerStage for the "Browse by Stage" section (up to 4 per stage)
  const byStage = articles.reduce<Record<string, typeof articles>>((acc, article) => {
    const stage = article.frontmatter.buyerStage ?? 'awareness'
    if (!acc[stage]) acc[stage] = []
    acc[stage].push(article)
    return acc
  }, {})

  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'Resources', url: `${SITE_CONFIG.url}/resources` },
  ])
  const listSchema = generateItemListSchema(
    articles.map((a) => ({
      name: a.frontmatter.title,
      url: `${SITE_CONFIG.url}/resources/${a.slug}`,
      description: a.frontmatter.description,
    })),
    'SkillLedger Resources'
  )

  // Group articles by silo
  const grouped = articles.reduce<Record<string, typeof articles>>((acc, article) => {
    const silo = article.frontmatter.silo
    if (!acc[silo]) acc[silo] = []
    acc[silo].push(article)
    return acc
  }, {})

  return (
    <>
      <JsonLd schema={breadcrumbSchema} />
      <JsonLd schema={listSchema} />
      <JsonLd schema={generateWebPageSchema({ name: 'Resources', description: 'Articles, guides, templates, and tools for professional skill exchange.', url: `${SITE_CONFIG.url}/resources` })} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <nav className="text-sm text-muted-foreground mb-8" aria-label="Breadcrumb">
            <Link href="/" className="hover:text-foreground">Home</Link>
            <span aria-hidden="true"> / </span>
            <span>Resources</span>
          </nav>

          <header className="mb-16 text-center">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">Resources</h1>
            <p className="text-xl text-muted-foreground max-w-2xl mx-auto">
              Guides, articles, and tools to help you succeed with professional skill exchange.
            </p>
          </header>

          {/* Browse by Stage */}
          <section className="mb-16">
            <h2 className="text-2xl font-bold tracking-tight mb-2">Browse by Stage</h2>
            <p className="text-muted-foreground mb-8">Find the right resources for where you are in your journey.</p>
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
              {(['awareness', 'consideration', 'decision'] as const).map((stage) => {
                const stageInfo = stageLabels[stage]
                const stageArticles = (byStage[stage] ?? []).slice(0, 4)
                if (stageArticles.length === 0) return null
                return (
                  <div key={stage} className="border border-border rounded-2xl p-6">
                    <div className="mb-4">
                      <span className={`text-xs font-bold uppercase tracking-wider px-2 py-1 rounded ${stageBadgeClass[stageInfo.funnelStage]}`}>
                        {stageInfo.label}
                      </span>
                      <p className="text-sm text-muted-foreground mt-2">{stageInfo.desc}</p>
                    </div>
                    <div className="space-y-3">
                      {stageArticles.map((article) => (
                        <Link
                          key={article.slug}
                          href={`/resources/${article.slug}`}
                          className="block hover:text-primary transition-colors group"
                        >
                          <p className="text-sm font-medium group-hover:text-primary line-clamp-2">{article.frontmatter.title}</p>
                          <p className="text-xs text-muted-foreground mt-0.5">{article.readingTime}</p>
                        </Link>
                      ))}
                    </div>
                  </div>
                )
              })}
            </div>
          </section>

          {/* Quick Links */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-16">
            <Link href="/how-to" className="card-feature p-6 hover:shadow-lg transition-all duration-200 group text-center">
              <h3 className="font-bold group-hover:text-primary transition-colors mb-2">How-To Guides</h3>
              <p className="text-sm text-muted-foreground">Step-by-step skill exchange scenarios</p>
            </Link>
            <Link href="/resources/templates" className="card-feature p-6 hover:shadow-lg transition-all duration-200 group text-center">
              <h3 className="font-bold group-hover:text-primary transition-colors mb-2">Contract Templates</h3>
              <p className="text-sm text-muted-foreground">Free barter agreement templates</p>
            </Link>
            <Link href="/tools/barter-valuation-calculator" className="card-feature p-6 hover:shadow-lg transition-all duration-200 group text-center">
              <h3 className="font-bold group-hover:text-primary transition-colors mb-2">Credit Calculator</h3>
              <p className="text-sm text-muted-foreground">Calculate fair exchange values</p>
            </Link>
          </div>

          {/* Articles by Silo — rendered in explicit order */}
          {siloOrder.filter((silo) => grouped[silo]?.length > 0).map((silo) => (
            <section key={silo} className="mb-12">
              <h2 className="text-2xl font-bold tracking-tight mb-6">{siloLabels[silo] || silo}</h2>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {grouped[silo].map((article) => (
                  <Link
                    key={article.slug}
                    href={`/resources/${article.slug}`}
                    className="card-feature p-6 hover:shadow-lg transition-all duration-200 group"
                  >
                    <h3 className="text-lg font-bold group-hover:text-primary transition-colors mb-2 line-clamp-2">
                      {article.frontmatter.title}
                    </h3>
                    <p className="text-sm text-muted-foreground mb-3 leading-relaxed line-clamp-3">
                      {article.frontmatter.description}
                    </p>
                    <div className="flex items-center gap-3 text-xs text-muted-foreground">
                      <time dateTime={article.frontmatter.publishedAt}>
                        {new Date(article.frontmatter.publishedAt).toLocaleDateString('en-US', {
                          year: 'numeric', month: 'long', day: 'numeric'
                        })}
                      </time>
                      <span>{article.readingTime}</span>
                      <span className="bg-primary/10 text-primary px-2 py-0.5 rounded">{siloLabels[silo] || silo}</span>
                    </div>
                  </Link>
                ))}
              </div>
            </section>
          ))}

          <div className="mt-8">
            <RelatedHubs currentPath="/resources" />
            <FunnelLinks
              stage="tofu"
              comparisons={topComparisons}
              howToGuides={topHowTo.map((s) => ({ slug: s.slug, title: s.title, skillOffered: s.skillOffered, skillNeeded: s.skillNeeded }))}
            />
            <FunnelCTA stage="tofu" />
          </div>
        </div>
      </div>
    </>
  )
}
