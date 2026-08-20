import { Metadata } from 'next'
import Link from 'next/link'
import { comparisonsData } from '@/lib/data/comparisons-data'
import { featuresData } from '@/lib/data/features-data'
import { buildPublicPageMetadata, generateBreadcrumbSchema, generateItemListSchema, SITE_CONFIG } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import { RelatedHubs } from '@/components/marketing/RelatedHubs'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

export const metadata: Metadata = buildPublicPageMetadata(
  'Compare',
  'Side-by-side comparisons of SkillLedger vs. Fiverr, Upwork, Simbi, Thumbtack, and more. See how skill exchange stacks up against cash freelancing platforms.',
  '/compare',
  ['skillledger comparison', 'fiverr alternative', 'upwork alternative', 'skill exchange comparison']
)

export default function ComparePage() {
  const topFeatures = featuresData.slice(0, 3)

  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'Compare', url: `${SITE_CONFIG.url}/compare` },
  ])
  const listSchema = generateItemListSchema(
    comparisonsData.map((c) => ({
      name: c.title,
      url: `${SITE_CONFIG.url}/compare/${c.slug}`,
      description: c.description,
    })),
    'SkillLedger Comparisons'
  )

  return (
    <>
      <JsonLd schema={breadcrumbSchema} />
      <JsonLd schema={listSchema} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' / '}
            <span>Compare</span>
          </nav>

          <header className="mb-16 text-center">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">Platform Comparisons</h1>
            <p className="text-xl text-muted-foreground max-w-2xl mx-auto">
              See how SkillLedger and skill exchange stack up against traditional freelancing platforms, time banking, and cash-based models.
            </p>
          </header>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {comparisonsData.map((c) => (
              <Link
                key={c.slug}
                href={`/compare/${c.slug}`}
                className="card-feature p-6 hover:shadow-lg transition-all duration-200 group"
              >
                <h2 className="text-lg font-bold group-hover:text-primary transition-colors mb-3">{c.title}</h2>
                <p className="text-sm text-muted-foreground mb-4 leading-relaxed line-clamp-3">{c.description}</p>
                <div className="flex flex-wrap gap-1">
                  <span className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded">{c.sideA.name}</span>
                  <span className="text-xs text-muted-foreground px-1">vs.</span>
                  <span className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded">{c.sideB.name}</span>
                </div>
              </Link>
            ))}
          </div>

          <div className="mt-16">
            <RelatedHubs currentPath="/compare" />
            <FunnelLinks stage="mofu" features={topFeatures} />
            <FunnelCTA stage="mofu" />
          </div>
        </div>
      </div>
    </>
  )
}
