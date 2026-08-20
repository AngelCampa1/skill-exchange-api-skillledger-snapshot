import { Metadata } from 'next'
import Link from 'next/link'
import { industriesData } from '@/lib/data/industries-data'
import { comparisonsData } from '@/lib/data/comparisons-data'
import { scenariosData } from '@/lib/data/scenarios-data'
import { buildPublicPageMetadata, generateBreadcrumbSchema, generateItemListSchema, generateWebPageSchema, SITE_CONFIG } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import { RelatedHubs } from '@/components/marketing/RelatedHubs'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

export const metadata: Metadata = buildPublicPageMetadata(
  'Industries',
  'Explore how professionals across 10 industries use SkillLedger to exchange skills, reduce costs, and grow their businesses.',
  '/industries',
  ['industry skill exchange', 'professional barter by industry']
)

export default function IndustriesPage() {
  const topComparisons = comparisonsData.slice(0, 3)
  const topHowTo = scenariosData.slice(0, 3)

  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'Industries', url: `${SITE_CONFIG.url}/industries` },
  ])
  const listSchema = generateItemListSchema(
    industriesData.map((ind) => ({
      name: ind.name,
      url: `${SITE_CONFIG.url}/industries/${ind.slug}`,
      description: ind.description,
    })),
    'SkillLedger Industries'
  )

  return (
    <>
      <JsonLd schema={breadcrumbSchema} />
      <JsonLd schema={listSchema} />
      <JsonLd schema={generateWebPageSchema({ name: 'Industries', description: 'Explore how professionals across 10 industries use SkillLedger to exchange skills, reduce costs, and grow their businesses.', url: `${SITE_CONFIG.url}/industries` })} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' / '}
            <span>Industries</span>
          </nav>

          <header className="mb-16 text-center">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">Skill Exchange by Industry</h1>
            <p className="text-xl text-muted-foreground max-w-2xl mx-auto">
              Discover how professionals in your industry use SkillLedger to exchange expertise, reduce costs, and build valuable relationships.
            </p>
          </header>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {industriesData.map((ind) => (
              <Link
                key={ind.slug}
                href={`/industries/${ind.slug}`}
                className="card-feature p-6 hover:shadow-lg transition-all duration-200 group"
              >
                <h2 className="text-lg font-bold group-hover:text-primary transition-colors mb-3">{ind.name}</h2>
                <p className="text-sm text-muted-foreground mb-4 leading-relaxed">{ind.description}</p>
                <div className="flex flex-wrap gap-1">
                  {ind.keyBenefits.slice(0, 2).map((benefit) => (
                    <span key={benefit} className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded line-clamp-1">{benefit}</span>
                  ))}
                </div>
              </Link>
            ))}
          </div>

          <div className="mt-16">
            <RelatedHubs currentPath="/industries" />
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
