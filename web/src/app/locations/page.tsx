import type { Metadata } from 'next'
import Link from 'next/link'
import { citiesData } from '@/lib/data/cities-data'
import { categoriesData } from '@/lib/data/categories-data'
import { comparisonsData } from '@/lib/data/comparisons-data'
import { scenariosData } from '@/lib/data/scenarios-data'
import { buildPublicPageMetadata, generateBreadcrumbSchema, generateWebPageSchema, SITE_CONFIG } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import { RelatedHubs } from '@/components/marketing/RelatedHubs'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

const LOCATION_PAGE_COUNT = citiesData.length * categoriesData.length

export const metadata: Metadata = buildPublicPageMetadata(
  'Skill Exchange by Location',
  `Find professionals for skill exchange in ${citiesData.length} US cities across ${categoriesData.length} categories. Browse location-specific exchange pages for web development, design, marketing, legal, and more.`,
  '/locations',
  ['skill exchange by city', 'local freelancer exchange', 'barter by location']
)

export default function LocationsPage() {
  const topComparisons = comparisonsData.slice(0, 3)
  const topHowTo = scenariosData.slice(0, 3)

  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'Locations', url: `${SITE_CONFIG.url}/locations` },
  ])
  const webPageSchema = generateWebPageSchema({
    name: 'Skill Exchange by Location',
    description: 'Find professionals for skill exchange in 50 US cities across 19 categories.',
    url: `${SITE_CONFIG.url}/locations`,
  })

  const topCategories = categoriesData.filter((c) => c.demandLevel === 'high').slice(0, 4)

  return (
    <>
      <JsonLd schema={breadcrumbSchema} />
      <JsonLd schema={webPageSchema} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' / '}
            <span>Locations</span>
          </nav>

          <header className="mb-16 max-w-3xl">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">
              Skill Exchange by Location
            </h1>
            <p className="text-xl text-muted-foreground leading-relaxed">
              {LOCATION_PAGE_COUNT} location-specific exchange pages across {citiesData.length} US cities and {categoriesData.length} skill categories. Find professionals where you are — or anywhere in the country.
            </p>
          </header>

          <section className="mb-16">
            <h2 className="text-2xl font-bold mb-8">Browse by City</h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
              {citiesData.map((city) => (
                <div key={city.slug} className="card-feature p-6">
                  <div className="flex items-center justify-between mb-3">
                    <h3 className="font-bold text-lg">{city.city}, {city.state}</h3>
                  </div>
                  <div className="flex flex-wrap gap-2 mb-4">
                    {city.topSkills.slice(0, 3).map((skill) => (
                      <span key={skill} className="text-xs px-2 py-1 bg-primary/10 text-primary rounded">{skill}</span>
                    ))}
                  </div>
                  <div className="space-y-1">
                    {topCategories.map((cat) => (
                      <Link
                        key={cat.slug}
                        href={`/locations/${city.slug}/${cat.slug}`}
                        className="block text-sm text-primary hover:underline"
                      >
                        {cat.name} in {city.city} →
                      </Link>
                    ))}
                  </div>
                  <Link
                    href={`/skill-exchange/${city.slug}`}
                    className="text-xs text-muted-foreground hover:text-foreground mt-3 inline-block"
                  >
                    All exchanges in {city.city}
                  </Link>
                </div>
              ))}
            </div>
          </section>

          <section className="mb-16">
            <h2 className="text-2xl font-bold mb-8">Browse by Category</h2>
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
              {categoriesData.map((cat) => (
                <Link
                  key={cat.slug}
                  href={`/categories/${cat.slug}`}
                  className="border border-border rounded-xl p-4 hover:border-primary hover:text-primary transition-colors group"
                >
                  <div className="font-bold text-sm group-hover:text-primary transition-colors">{cat.name}</div>
                  <div className="text-xs text-muted-foreground mt-1">{cat.averageCreditRate} cr/hr avg</div>
                </Link>
              ))}
            </div>
          </section>

          <RelatedHubs currentPath="/locations" />
          <FunnelLinks
            stage="tofu"
            comparisons={topComparisons}
            howToGuides={topHowTo.map((s) => ({ slug: s.slug, title: s.title, skillOffered: s.skillOffered, skillNeeded: s.skillNeeded }))}
          />
          <FunnelCTA stage="tofu" />
        </div>
      </div>
    </>
  )
}
