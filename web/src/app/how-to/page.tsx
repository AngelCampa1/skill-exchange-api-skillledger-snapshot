import { Metadata } from 'next'
import Link from 'next/link'
import { scenariosData } from '@/lib/data/scenarios-data'
import { featuresData } from '@/lib/data/features-data'
import { buildPublicPageMetadata, generateBreadcrumbSchema, generateItemListSchema, SITE_CONFIG } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import { RelatedHubs } from '@/components/marketing/RelatedHubs'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

export const metadata: Metadata = buildPublicPageMetadata(
  'How-To Guides',
  'Step-by-step guides for trading professional skills. Learn how to exchange web development for design, marketing for writing, and more on SkillLedger.',
  '/how-to',
  ['how to trade skills', 'skill exchange guide', 'barter how to', 'service swap tutorial']
)

export default function HowToPage() {
  const topFeatures = featuresData.slice(0, 3)

  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'How-To Guides', url: `${SITE_CONFIG.url}/how-to` },
  ])
  const listSchema = generateItemListSchema(
    scenariosData.map((s) => ({
      name: s.title,
      url: `${SITE_CONFIG.url}/how-to/${s.slug}`,
      description: s.description,
    })),
    'SkillLedger How-To Guides'
  )

  return (
    <>
      <JsonLd schema={breadcrumbSchema} />
      <JsonLd schema={listSchema} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <nav className="text-sm text-muted-foreground mb-8" aria-label="Breadcrumb">
            <Link href="/" className="hover:text-foreground">Home</Link>
            <span aria-hidden="true"> / </span>
            <span>How-To Guides</span>
          </nav>

          <header className="mb-16 text-center">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">How-To Guides</h1>
            <p className="text-xl text-muted-foreground max-w-2xl mx-auto">
              Step-by-step guides showing exactly how to exchange professional skills on SkillLedger.
            </p>
          </header>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {scenariosData.map((scenario) => (
              <Link
                key={scenario.slug}
                href={`/how-to/${scenario.slug}`}
                className="card-feature p-6 hover:shadow-lg transition-all duration-200 group"
              >
                <h2 className="text-lg font-bold group-hover:text-primary transition-colors mb-3 line-clamp-2">
                  {scenario.title}
                </h2>
                <p className="text-sm text-muted-foreground mb-4 leading-relaxed line-clamp-3">
                  {scenario.description}
                </p>
                <div className="flex flex-wrap gap-1.5">
                  <span className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded">{scenario.skillOffered}</span>
                  <span className="text-xs text-muted-foreground px-1">for</span>
                  <span className="text-xs bg-secondary/10 text-secondary px-2 py-0.5 rounded">{scenario.skillNeeded}</span>
                </div>
              </Link>
            ))}
          </div>

          <div className="mt-16">
            <RelatedHubs currentPath="/how-to" />
            <FunnelLinks stage="mofu" features={topFeatures} />
            <FunnelCTA stage="mofu" />
          </div>
        </div>
      </div>
    </>
  )
}
