import type { Metadata } from 'next'
import Link from 'next/link'
import { categoriesData } from '@/lib/data/categories-data'
import { comparisonsData } from '@/lib/data/comparisons-data'
import { scenariosData } from '@/lib/data/scenarios-data'
import { buildPublicPageMetadata, generateBreadcrumbSchema, generateWebPageSchema, SITE_CONFIG } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import { RelatedHubs } from '@/components/marketing/RelatedHubs'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

const PAIRING_COUNT = categoriesData.length * (categoriesData.length - 1)

export const metadata: Metadata = buildPublicPageMetadata(
  'Trade Skills: All Exchange Pairings',
  `Browse every skill exchange pairing on SkillLedger. Trade web development for design, marketing for legal, AI for finance. ${PAIRING_COUNT} combinations across ${categoriesData.length} professional categories.`,
  '/trade',
  ['skill trade', 'service exchange pairings', 'barter categories']
)

export default function TradePage() {
  const topComparisons = comparisonsData.slice(0, 3)
  const topHowTo = scenariosData.slice(0, 3)

  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'Trade', url: `${SITE_CONFIG.url}/trade` },
  ])
  const webPageSchema = generateWebPageSchema({
    name: 'Trade Skills: All Exchange Pairings',
    description: 'Browse every skill exchange pairing on SkillLedger.',
    url: `${SITE_CONFIG.url}/trade`,
  })

  return (
    <>
      <JsonLd schema={breadcrumbSchema} />
      <JsonLd schema={webPageSchema} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' / '}
            <span>Trade</span>
          </nav>

          <header className="mb-16 max-w-3xl">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">
              Trade Any Skill for Any Skill
            </h1>
            <p className="text-xl text-muted-foreground leading-relaxed">
              {PAIRING_COUNT} exchange pairings across {categoriesData.length} professional categories. Find the trade that fits your practice. No cash required.
            </p>
          </header>

          <section>
            <h2 className="text-2xl font-bold mb-8">Browse by Category</h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
              {categoriesData.map((cat) => {
                const topPairings = categoriesData.filter((b) => b.slug !== cat.slug).slice(0, 3)
                return (
                  <div key={cat.slug} className="card-feature p-6">
                    <div className="flex items-center justify-between mb-3">
                      <h3 className="font-bold text-lg">{cat.name}</h3>
                      <span className="text-xs text-muted-foreground">{cat.averageCreditRate} cr/hr</span>
                    </div>
                    <p className="text-sm text-muted-foreground mb-4 line-clamp-2">{cat.description}</p>
                    <div className="space-y-2">
                      {topPairings.map((pair) => (
                        <Link
                          key={pair.slug}
                          href={`/trade/${cat.slug}/for/${pair.slug}`}
                          className="block text-sm text-primary hover:underline"
                        >
                          {cat.name} for {pair.name} →
                        </Link>
                      ))}
                    </div>
                    <Link
                      href={`/categories/${cat.slug}`}
                      className="text-xs text-muted-foreground hover:text-foreground mt-3 inline-block"
                    >
                      View all {cat.name} exchanges
                    </Link>
                  </div>
                )
              })}
            </div>
          </section>

          <div className="mt-16">
            <RelatedHubs currentPath="/trade" />
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
