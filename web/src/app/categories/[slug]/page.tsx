import { notFound } from'next/navigation'
import type { Metadata } from'next'
import Link from'next/link'
import { categoriesData, getCategoryBySlug } from'@/lib/data/categories-data'
import { findScenariosForCategory, findIndustriesForCategory, findArticlesForCategory, findComparisonsForCategory, findFeaturesForCategory } from'@/lib/cross-links'
import { buildPublicPageMetadata, generateFAQSchema, generateBreadcrumbSchema, generateItemListSchema, generateServiceSchema, SITE_CONFIG } from'@/lib/seo'
import { JsonLd } from'@/components/marketing/JsonLd'
import { FunnelCTA } from'@/components/marketing/FunnelCTA'
import { FunnelLinks } from'@/components/marketing/FunnelLinks'

export const dynamicParams = false

interface Props {
  params: Promise<{ slug: string }>
}

export async function generateStaticParams() {
  return categoriesData.map((c) => ({ slug: c.slug }))
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params
  const cat = getCategoryBySlug(slug)
  if (!cat) return {}
  return buildPublicPageMetadata(
    `${cat.name} Skills Exchange`,
    `Exchange ${cat.name.toLowerCase()} skills on SkillLedger. ${cat.description}`,
    `/categories/${slug}`,
    cat.sampleSkills
  )
}

export default async function CategoryPage({ params }: Props) {
  const { slug } = await params
  const cat = getCategoryBySlug(slug)
  if (!cat) notFound()

  const relatedScenarios = findScenariosForCategory(slug).slice(0, 4)
  const relatedIndustries = findIndustriesForCategory(slug).slice(0, 4)
  const relatedArticles = findArticlesForCategory(slug).slice(0, 4)
  const relatedComparisons = findComparisonsForCategory(slug).slice(0, 3)
  const relatedFeatures = findFeaturesForCategory(slug).slice(0, 3)

  const faqSchema = cat.faqs.length > 0 ? generateFAQSchema(cat.faqs) : null
  const breadcrumbSchema = generateBreadcrumbSchema([
    { name:'Home', url: SITE_CONFIG.url },
    { name:'Categories', url: `${SITE_CONFIG.url}/categories` },
    { name: cat.name, url: `${SITE_CONFIG.url}/categories/${slug}` },
  ])
  const skillsSchema = generateItemListSchema(
    cat.sampleSkills.map((s) => ({ name: s, url: `${SITE_CONFIG.url}/categories/${slug}#${s.toLowerCase().replace(/\s+/g,'-')}` })),
    `${cat.name} Skills`
  )
  const serviceSchema = generateServiceSchema({ name: `${cat.name} Exchange`, serviceType:'Professional Skill Exchange' })

  return (
    <>
      {faqSchema && <JsonLd schema={faqSchema} />}
      <JsonLd schema={breadcrumbSchema} />
      <JsonLd schema={skillsSchema} />
      <JsonLd schema={serviceSchema} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' /'}
            <Link href="/categories" className="hover:text-foreground">Categories</Link>
            {' /'}
            <span>{cat.name}</span>
          </nav>

          <header className="mb-16 max-w-3xl">
            <div className="flex items-center gap-3 mb-4">
              <span className={`text-sm px-3 py-1 rounded-full font-medium ${cat.demandLevel ==='high' ?'bg-green-100  text-green-700' : cat.demandLevel ==='medium' ?'bg-yellow-100  text-yellow-700' :'bg-gray-100  text-gray-600'}`}>
                {cat.demandLevel.charAt(0).toUpperCase() + cat.demandLevel.slice(1)} Demand
              </span>
              <span className="text-sm text-muted-foreground">~{cat.averageCreditRate} credits/hr average</span>
            </div>
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">{cat.name} Skills Exchange</h1>
            <p className="text-xl text-muted-foreground leading-relaxed mb-8">{cat.longDescription}</p>
            <Link href="/register" className="btn-primary">Start Exchanging {cat.name} Skills</Link>
          </header>

          <section className="mb-16">
            <h2 className="text-2xl font-bold mb-6">Skills Available for Exchange</h2>
            <div className="flex flex-wrap gap-3">
              {cat.sampleSkills.map((skill) => (
                <span key={skill} className="px-4 py-2 bg-primary/10 text-primary rounded-lg font-medium">{skill}</span>
              ))}
            </div>
          </section>

          {cat.faqs.length > 0 && (
            <section className="mb-16 max-w-3xl">
              <h2 className="text-2xl font-bold mb-8">Frequently Asked Questions</h2>
              <div className="space-y-6">
                {cat.faqs.map((faq, i) => (
                  <div key={i} className="border border-border rounded-xl p-6">
                    <h3 className="font-bold mb-3">{faq.question}</h3>
                    <p className="text-muted-foreground leading-relaxed">{faq.answer}</p>
                  </div>
                ))}
              </div>
            </section>
          )}

          {/* Compare Platforms */}
          {relatedComparisons.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">Compare Platforms</h2>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {relatedComparisons.map((c) => (
                  <Link
                    key={c.slug}
                    href={`/compare/${c.slug}`}
                    className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
                  >
                    <h3 className="font-bold group-hover:text-primary transition-colors mb-2 line-clamp-2 text-sm">{c.title}</h3>
                    <p className="text-sm text-muted-foreground line-clamp-2">{c.description}</p>
                  </Link>
                ))}
              </div>
            </section>
          )}

          {/* Platform Features */}
          {relatedFeatures.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">Platform Features</h2>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {relatedFeatures.map((f) => (
                  <Link
                    key={f.slug}
                    href={`/features/${f.slug}`}
                    className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
                  >
                    <h3 className="font-bold group-hover:text-primary transition-colors mb-2 text-sm">{f.name}</h3>
                    <p className="text-sm text-muted-foreground line-clamp-2">{f.tagline}</p>
                  </Link>
                ))}
              </div>
            </section>
          )}

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

          {relatedIndustries.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">Used in These Industries</h2>
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

          {relatedArticles.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">Related Articles</h2>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {relatedArticles.map((article) => (
                  <Link
                    key={article.slug}
                    href={`/resources/${article.slug}`}
                    className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
                  >
                    <h3 className="font-bold group-hover:text-primary transition-colors mb-2 line-clamp-2">
                      {article.frontmatter.title}
                    </h3>
                    <p className="text-sm text-muted-foreground line-clamp-2">{article.frontmatter.description}</p>
                    <span className="text-xs text-muted-foreground mt-2 inline-block">{article.readingTime}</span>
                  </Link>
                ))}
              </div>
            </section>
          )}

          <FunnelLinks
            stage="tofu"
            comparisons={relatedComparisons}
            howToGuides={relatedScenarios.map((s) => ({ slug: s.slug, title: s.title, skillOffered: s.skillOffered, skillNeeded: s.skillNeeded }))}
          />
          <FunnelCTA stage="tofu" />
        </div>
      </div>
    </>
  )
}
