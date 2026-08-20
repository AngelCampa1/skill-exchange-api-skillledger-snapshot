import { Metadata } from 'next'
import Link from 'next/link'
import { citiesData } from '@/lib/data/cities-data'
import { comparisonsData } from '@/lib/data/comparisons-data'
import { scenariosData } from '@/lib/data/scenarios-data'
import { buildPublicPageMetadata, generateBreadcrumbSchema, generateItemListSchema, SITE_CONFIG } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import { RelatedHubs } from '@/components/marketing/RelatedHubs'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

export const metadata: Metadata = buildPublicPageMetadata(
  'Skill Exchange by City',
  'Find professionals to exchange skills with in your city. Browse 50+ cities across the US for local skill exchange opportunities on SkillLedger.',
  '/skill-exchange',
  ['skill exchange near me', 'local skill swap', 'barter services by city', 'professional exchange city']
)

export default function SkillExchangePage() {
  const topComparisons = comparisonsData.slice(0, 3)
  const topHowTo = scenariosData.slice(0, 3)

  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'Skill Exchange', url: `${SITE_CONFIG.url}/skill-exchange` },
  ])
  const listSchema = generateItemListSchema(
    citiesData.map((c) => ({
      name: `Skill Exchange in ${c.city}, ${c.state}`,
      url: `${SITE_CONFIG.url}/skill-exchange/${c.slug}`,
      description: `Exchange professional skills with verified professionals in ${c.city}, ${c.state}. Top skills: ${c.topSkills.slice(0, 3).join(', ')}.`,
    })),
    'SkillLedger Skill Exchange by City'
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
            <span>Skill Exchange</span>
          </nav>

          <header className="mb-16 text-center">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">Skill Exchange by City</h1>
            <p className="text-xl text-muted-foreground max-w-2xl mx-auto">
              Find professionals to exchange skills with in your area. Browse 50+ cities across the US.
            </p>
          </header>

          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
            {citiesData.map((city) => (
              <Link
                key={city.slug}
                href={`/skill-exchange/${city.slug}`}
                className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
              >
                <h2 className="font-bold group-hover:text-primary transition-colors mb-2">
                  {city.city}, {city.state}
                </h2>
                <div className="flex flex-wrap gap-1">
                  {city.topSkills.slice(0, 3).map((skill) => (
                    <span key={skill} className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded">{skill}</span>
                  ))}
                </div>
              </Link>
            ))}
          </div>

          <div className="mt-16">
            <RelatedHubs currentPath="/skill-exchange" />
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
