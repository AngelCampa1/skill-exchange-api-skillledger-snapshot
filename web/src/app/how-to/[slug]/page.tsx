import { notFound } from'next/navigation'
import type { Metadata } from'next'
import Link from'next/link'
import { scenariosData, getScenarioBySlug } from'@/lib/data/scenarios-data'
import { featuresData } from'@/lib/data/features-data'
import { skillToCategorySlug, findScenariosForCategory, findArticlesForCategory, findComparisonsForCategory } from'@/lib/cross-links'
import { buildPublicPageMetadata, generateFAQSchema, generateBreadcrumbSchema, SITE_CONFIG } from'@/lib/seo'
import { JsonLd } from'@/components/marketing/JsonLd'
import { FunnelCTA } from'@/components/marketing/FunnelCTA'
import { FunnelLinks } from'@/components/marketing/FunnelLinks'

export const dynamicParams = false

interface Props {
  params: Promise<{ slug: string }>
}

export async function generateStaticParams() {
  return scenariosData.map((s) => ({ slug: s.slug }))
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params
  const scenario = getScenarioBySlug(slug)
  if (!scenario) return {}
  return buildPublicPageMetadata(
    `${scenario.title} — Step-by-Step Guide`,
    scenario.description,
    `/how-to/${slug}`,
    [scenario.skillOffered.toLowerCase(), scenario.skillNeeded.toLowerCase(),'skill exchange how to']
  )
}

export default async function HowToPage({ params }: Props) {
  const { slug } = await params
  const scenario = getScenarioBySlug(slug)
  if (!scenario) notFound()

  const offeredCategorySlug = skillToCategorySlug(scenario.skillOffered)
  const neededCategorySlug = skillToCategorySlug(scenario.skillNeeded)

  // Show related scenarios that share a skill category, falling back to first 3
  const relatedBySkill = new Set<string>()
  for (const catSlug of [offeredCategorySlug, neededCategorySlug].filter(Boolean) as string[]) {
    for (const s of findScenariosForCategory(catSlug)) {
      if (s.slug !== slug) relatedBySkill.add(s.slug)
    }
  }
  const moreScenarios = relatedBySkill.size > 0
    ? scenariosData.filter((s) => relatedBySkill.has(s.slug)).slice(0, 3)
    : scenariosData.filter((s) => s.slug !== slug).slice(0, 3)

  // Find articles related to this scenario's skill categories
  const relatedArticles = [offeredCategorySlug, neededCategorySlug]
    .filter(Boolean)
    .flatMap((catSlug) => findArticlesForCategory(catSlug as string))
    .filter((article, index, self) => self.findIndex((a) => a.slug === article.slug) === index)
    .slice(0, 3)

  // Compare Your Options: comparisons from both category slugs, deduplicated
  const relatedComparisons = [
    ...(offeredCategorySlug ? findComparisonsForCategory(offeredCategorySlug) : []),
    ...(neededCategorySlug ? findComparisonsForCategory(neededCategorySlug) : []),
  ].filter((c, i, self) => self.findIndex((d) => d.slug === c.slug) === i).slice(0, 3)

  // Feature links
  const relatedFeatures = featuresData.slice(0, 3)

  const howToSchema = {'@context':'https://schema.org','@type':'HowTo',
    name: scenario.title,
    description: scenario.description,
    step: scenario.steps.map((step, index) => ({'@type':'HowToStep',
      position: index + 1,
      name: step.name,
      text: step.text,
    })),
  }

  const faqSchema = scenario.faqs.length > 0 ? generateFAQSchema(scenario.faqs) : null
  const breadcrumbSchema = generateBreadcrumbSchema([
    { name:'Home', url: SITE_CONFIG.url },
    { name:'How-To Guides', url: `${SITE_CONFIG.url}/how-to` },
    { name: scenario.title, url: `${SITE_CONFIG.url}/how-to/${slug}` },
  ])

  return (
    <>
      <JsonLd schema={howToSchema} />
      {faqSchema && <JsonLd schema={faqSchema} />}
      <JsonLd schema={breadcrumbSchema} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' /'}
            <Link href="/how-to" className="hover:text-foreground">How-To Guides</Link>
            {' /'}
            <span>{scenario.title}</span>
          </nav>

          <header className="mb-16 max-w-3xl">
            <div className="flex items-center gap-2 mb-4 text-sm text-muted-foreground">
              {offeredCategorySlug ? (
                <Link href={`/categories/${offeredCategorySlug}`} className="px-3 py-1 bg-primary/10 text-primary rounded-full font-medium hover:bg-primary/20 transition-colors">{scenario.skillOffered}</Link>
              ) : (
                <span className="px-3 py-1 bg-primary/10 text-primary rounded-full font-medium">{scenario.skillOffered}</span>
              )}
              <span>for</span>
              {neededCategorySlug ? (
                <Link href={`/categories/${neededCategorySlug}`} className="px-3 py-1 bg-secondary/50 rounded-full font-medium hover:bg-secondary/70 transition-colors">{scenario.skillNeeded}</Link>
              ) : (
                <span className="px-3 py-1 bg-secondary/50 rounded-full font-medium">{scenario.skillNeeded}</span>
              )}
            </div>
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">{scenario.title}</h1>
            <p className="text-xl text-muted-foreground leading-relaxed mb-8">{scenario.description}</p>
            <div className="flex flex-wrap gap-3">
              <Link href="/register" className="btn-primary">Start This Exchange</Link>
              {offeredCategorySlug && neededCategorySlug && (
                <Link
                  href={`/trade/${offeredCategorySlug}/for/${neededCategorySlug}`}
                  className="px-6 py-3 border border-border rounded-lg font-medium hover:border-primary hover:text-primary transition-colors text-sm"
                >
                  Browse Exchange Hub &rarr;
                </Link>
              )}
            </div>
          </header>

          <section className="mb-16 max-w-3xl">
            <h2 className="text-2xl font-bold mb-8">Step-by-Step Guide</h2>
            <ol className="space-y-6">
              {scenario.steps.map((step, index) => (
                <li key={index} className="flex gap-5">
                  <div className="flex-shrink-0 w-10 h-10 rounded-full bg-primary text-primary-foreground flex items-center justify-center font-black text-sm">
                    {index + 1}
                  </div>
                  <div className="pt-1">
                    <h3 className="font-bold mb-2">{step.name}</h3>
                    <p className="text-muted-foreground leading-relaxed">{step.text}</p>
                  </div>
                </li>
              ))}
            </ol>
          </section>

          {scenario.benefits.length > 0 && (
            <section className="mb-16 max-w-3xl">
              <h2 className="text-2xl font-bold mb-6">Benefits of This Exchange</h2>
              <ul className="space-y-3">
                {scenario.benefits.map((benefit, index) => (
                  <li key={index} className="flex items-start gap-3">
                    <span className="flex-shrink-0 w-5 h-5 rounded-full bg-green-100  text-green-700  flex items-center justify-center text-xs font-bold mt-0.5">✓</span>
                    <span className="text-muted-foreground">{benefit}</span>
                  </li>
                ))}
              </ul>
            </section>
          )}

          {scenario.faqs.length > 0 && (
            <section className="mb-16 max-w-3xl">
              <h2 className="text-2xl font-bold mb-8">Frequently Asked Questions</h2>
              <div className="space-y-6">
                {scenario.faqs.map((faq, i) => (
                  <div key={i} className="border border-border rounded-xl p-6">
                    <h3 className="font-bold mb-3">{faq.question}</h3>
                    <p className="text-muted-foreground leading-relaxed">{faq.answer}</p>
                  </div>
                ))}
              </div>
            </section>
          )}

          {moreScenarios.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">More How-To Guides</h2>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {moreScenarios.map((s) => (
                  <Link
                    key={s.slug}
                    href={`/how-to/${s.slug}`}
                    className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
                  >
                    <h3 className="font-bold group-hover:text-primary transition-colors mb-2 line-clamp-2 text-sm">
                      {s.title}
                    </h3>
                    <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                      <span className="px-2 py-0.5 bg-primary/10 text-primary rounded">{s.skillOffered}</span>
                      <span>for</span>
                      <span className="px-2 py-0.5 bg-primary/10 text-primary rounded">{s.skillNeeded}</span>
                    </div>
                  </Link>
                ))}
              </div>
              <div className="text-center mt-6">
                <Link href="/how-to" className="text-primary font-medium hover:underline">View All How-To Guides</Link>
              </div>
            </section>
          )}

          {/* Compare Your Options */}
          {relatedComparisons.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">Compare Your Options</h2>
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

          {/* Feature links */}
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

          {relatedArticles.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">Related Articles</h2>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {relatedArticles.map((article) => (
                  <Link
                    key={article.slug}
                    href={`/resources/${article.slug}`}
                    className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
                  >
                    <h3 className="font-bold group-hover:text-primary transition-colors mb-2 line-clamp-2 text-sm">
                      {article.frontmatter.title}
                    </h3>
                    <p className="text-sm text-muted-foreground line-clamp-2">{article.frontmatter.description}</p>
                    <span className="text-xs text-muted-foreground mt-2 inline-block">{article.readingTime}</span>
                  </Link>
                ))}
              </div>
            </section>
          )}

          <FunnelLinks stage="mofu" features={relatedFeatures} />
          <FunnelCTA stage="mofu" />
        </div>
      </div>
    </>
  )
}
