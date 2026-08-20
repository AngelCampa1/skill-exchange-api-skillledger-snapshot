import { notFound } from 'next/navigation'
import type { Metadata } from 'next'
import Link from 'next/link'
import { citiesData, getCityBySlug } from '@/lib/data/cities-data'
import { skillToCategorySlug, findScenariosForSkills, findCategoriesForSkills, findIndustriesForCity, findComparisonsForCategory } from '@/lib/cross-links'
import { buildPublicPageMetadata, generateFAQSchema, generateBreadcrumbSchema, generateLocalBusinessSchema, SITE_CONFIG } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'

export const dynamicParams = false

interface Props {
  params: Promise<{ city: string }>
}

export async function generateStaticParams() {
  return citiesData.map((c) => ({ city: c.slug }))
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { city: citySlug } = await params
  const city = getCityBySlug(citySlug)
  if (!city) return {}
  return buildPublicPageMetadata(
    `Skill Exchange in ${city.city}`,
    `Connect with professionals in ${city.city}, ${city.state} for skill exchange. Trade ${city.topSkills.slice(0, 3).join(', ')} and more on SkillLedger.`,
    `/skill-exchange/${citySlug}`,
    [`${city.city} freelancers`, `skill exchange ${city.city}`]
  )
}

export default async function CityPage({ params }: Props) {
  const { city: citySlug } = await params
  const city = getCityBySlug(citySlug)
  if (!city) notFound()

  const relatedScenarios = findScenariosForSkills(city.topSkills).slice(0, 4)
  const matchedCategories = findCategoriesForSkills(city.topSkills).slice(0, 6)
  const relatedIndustries = findIndustriesForCity(city.topSkills).slice(0, 4)

  // Build comparisons from the city's top skill categories
  const cityTopCatSlugs = city.topSkills
    .map((s) => skillToCategorySlug(s))
    .filter((s): s is string => !!s)
  const cityComparisons = [...new Set(cityTopCatSlugs)]
    .flatMap((catSlug) => findComparisonsForCategory(catSlug))
    .filter((c, i, self) => self.findIndex((d) => d.slug === c.slug) === i)
    .slice(0, 3)

  const faqSchema = city.faqs.length > 0 ? generateFAQSchema(city.faqs) : null
  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'Skill Exchange', url: `${SITE_CONFIG.url}/skill-exchange` },
    { name: `${city.city}, ${city.state}`, url: `${SITE_CONFIG.url}/skill-exchange/${citySlug}` },
  ])
  const topCategory = matchedCategories[0]
  const localBusinessSchema = topCategory ? generateLocalBusinessSchema(city, topCategory) : null

  return (
    <>
      {faqSchema && <JsonLd schema={faqSchema} />}
      <JsonLd schema={breadcrumbSchema} />
      {localBusinessSchema && <JsonLd schema={localBusinessSchema} />}
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' / '}
            <Link href="/skill-exchange" className="hover:text-foreground">Skill Exchange</Link>
            {' / '}
            <span>{city.city}, {city.state}</span>
          </nav>

          <header className="mb-16 max-w-3xl">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">
              Skill Exchange in {city.city}, {city.state}
            </h1>
            <p className="text-xl text-muted-foreground leading-relaxed mb-8">
              Connect with professionals in {city.city} to exchange services without cash. SkillLedger matches you with local experts in {city.topSkills.slice(0, 3).join(', ')}, and more.
            </p>
            <Link href="/register" className="btn-primary">Join {city.city} Professionals</Link>
          </header>

          <section className="mb-16">
            <h2 className="text-2xl font-bold mb-6">Top Skills Traded in {city.city}</h2>
            <div className="flex flex-wrap gap-3">
              {city.topSkills.map((skill) => {
                const catSlug = skillToCategorySlug(skill)
                return catSlug ? (
                  <Link key={skill} href={`/categories/${catSlug}`} className="px-4 py-2 bg-primary/10 text-primary rounded-lg font-medium hover:bg-primary/20 transition-colors">{skill}</Link>
                ) : (
                  <span key={skill} className="px-4 py-2 bg-primary/10 text-primary rounded-lg font-medium">{skill}</span>
                )
              })}
            </div>
          </section>

          {relatedScenarios.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">Related How-To Guides</h2>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {relatedScenarios.map((scenario) => (
                  <Link
                    key={scenario.slug}
                    href={`/how-to/${scenario.slug}`}
                    className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
                  >
                    <h3 className="font-bold group-hover:text-primary transition-colors mb-2 line-clamp-2">
                      {scenario.title}
                    </h3>
                    <div className="flex items-center gap-2 text-xs text-muted-foreground">
                      <span className="px-2 py-0.5 bg-primary/10 text-primary rounded">{scenario.skillOffered}</span>
                      <span>for</span>
                      <span className="px-2 py-0.5 bg-primary/10 text-primary rounded">{scenario.skillNeeded}</span>
                    </div>
                  </Link>
                ))}
              </div>
            </section>
          )}

          {matchedCategories.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">Browse Categories</h2>
              <div className="flex flex-wrap gap-3">
                {matchedCategories.map((cat) => (
                  <Link
                    key={cat.slug}
                    href={`/categories/${cat.slug}`}
                    className="px-4 py-2 border border-border rounded-lg font-medium hover:border-primary hover:text-primary transition-colors"
                  >
                    {cat.name}
                  </Link>
                ))}
              </div>
            </section>
          )}

          {relatedIndustries.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">Industries in {city.city}</h2>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {relatedIndustries.map((industry) => (
                  <Link
                    key={industry.slug}
                    href={`/industries/${industry.slug}`}
                    className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
                  >
                    <h3 className="font-bold group-hover:text-primary transition-colors mb-1">{industry.name}</h3>
                    <p className="text-sm text-muted-foreground line-clamp-2">{industry.description}</p>
                  </Link>
                ))}
              </div>
            </section>
          )}

          {city.faqs.length > 0 && (
            <section className="mb-16 max-w-3xl">
              <h2 className="text-2xl font-bold mb-8">Frequently Asked Questions</h2>
              <div className="space-y-6">
                {city.faqs.map((faq, i) => (
                  <div key={i} className="border border-border rounded-xl p-6">
                    <h3 className="font-bold mb-3">{faq.question}</h3>
                    <p className="text-muted-foreground leading-relaxed">{faq.answer}</p>
                  </div>
                ))}
              </div>
            </section>
          )}

          <FunnelLinks
            stage="tofu"
            comparisons={cityComparisons}
            howToGuides={relatedScenarios.map((s) => ({ slug: s.slug, title: s.title, skillOffered: s.skillOffered, skillNeeded: s.skillNeeded }))}
          />
          <FunnelCTA stage="tofu" pageContext={city.city} />
        </div>
      </div>
    </>
  )
}
