import { notFound } from'next/navigation'
import type { Metadata } from'next'
import Link from'next/link'
import { comparisonsData, getComparisonBySlug } from'@/lib/data/comparisons-data'
import { featuresData } from'@/lib/data/features-data'
import { findArticlesForTerm, findScenariosForCategory, skillToCategorySlug } from'@/lib/cross-links'
import { buildPublicPageMetadata, generateFAQSchema, generateBreadcrumbSchema, generateProsConsSchema, SITE_CONFIG } from'@/lib/seo'
import { JsonLd } from'@/components/marketing/JsonLd'
import { FunnelCTA } from'@/components/marketing/FunnelCTA'
import { FunnelLinks } from'@/components/marketing/FunnelLinks'

export const dynamicParams = false

interface Props {
  params: Promise<{ slug: string }>
}

export async function generateStaticParams() {
  return comparisonsData.map((c) => ({ slug: c.slug }))
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params
  const comparison = getComparisonBySlug(slug)
  if (!comparison) return {}
  const metaDesc = comparison.description
  return buildPublicPageMetadata(
    comparison.title,
    metaDesc.length > 160 ? metaDesc.slice(0, 157) +'...' : metaDesc,
    `/compare/${slug}`,
    [comparison.sideA.name.toLowerCase(), comparison.sideB.name.toLowerCase(),'comparison','skill exchange']
  )
}

export default async function ComparisonPage({ params }: Props) {
  const { slug } = await params
  const comparison = getComparisonBySlug(slug)
  if (!comparison) notFound()

  const otherComparisons = comparisonsData.filter((c) => c.slug !== slug).slice(0, 4)

  // Find articles related to this comparison's topics
  const relatedArticles = [
    ...findArticlesForTerm(comparison.sideA.name.toLowerCase().replace(/\s+/g,'-'), comparison.sideA.name),
    ...findArticlesForTerm(comparison.sideB.name.toLowerCase().replace(/\s+/g,'-'), comparison.sideB.name),
  ].filter((article, index, self) => self.findIndex((a) => a.slug === article.slug) === index).slice(0, 3)

  // How-to guides: find scenarios relevant to skills mentioned in the comparison title
  const titleWords = comparison.title.toLowerCase().split(/\s+/)
  const howToCatSlugs = new Set<string>()
  for (const word of titleWords) {
    const catSlug = skillToCategorySlug(word)
    if (catSlug) howToCatSlugs.add(catSlug)
  }
  const relatedHowToGuides = [...howToCatSlugs]
    .flatMap((catSlug) => findScenariosForCategory(catSlug))
    .filter((s, i, self) => self.findIndex((t) => t.slug === s.slug) === i)
    .slice(0, 3)

  // Feature links: show all features (small set)
  const relatedFeatures = featuresData.slice(0, 3)

  const faqSchema = comparison.faqs.length > 0 ? generateFAQSchema(comparison.faqs) : null
  const prosConsSchemaA = generateProsConsSchema({
    name: comparison.sideA.name,
    url: `${SITE_CONFIG.url}/compare/${slug}`,
    positiveNotes: comparison.sideA.strengths,
    negativeNotes: comparison.sideA.weaknesses,
  })
  const prosConsSchemaB = generateProsConsSchema({
    name: comparison.sideB.name,
    url: `${SITE_CONFIG.url}/compare/${slug}`,
    positiveNotes: comparison.sideB.strengths,
    negativeNotes: comparison.sideB.weaknesses,
  })
  const breadcrumbSchema = generateBreadcrumbSchema([
    { name:'Home', url: SITE_CONFIG.url },
    { name:'Compare', url: `${SITE_CONFIG.url}/compare` },
    { name: comparison.title, url: `${SITE_CONFIG.url}/compare/${slug}` },
  ])

  return (
    <>
      {faqSchema && <JsonLd schema={faqSchema} />}
      <JsonLd schema={breadcrumbSchema} />
      <JsonLd schema={prosConsSchemaA} />
      <JsonLd schema={prosConsSchemaB} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' /'}
            <Link href="/compare" className="hover:text-foreground">Compare</Link>
            {' /'}
            <span>{comparison.title}</span>
          </nav>

          <header className="mb-16 max-w-3xl">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">{comparison.title}</h1>
            <p className="text-xl text-muted-foreground leading-relaxed">{comparison.description}</p>
          </header>

          {comparison.keyStatistic && (
            <div className="rounded-lg border-l-4 border-primary bg-primary/5 p-4 mb-8">
              <p className="text-sm font-medium">{comparison.keyStatistic}</p>
            </div>
          )}

          <section className="mb-16">
            <h2 className="text-2xl font-bold mb-6">Side-by-Side Comparison</h2>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {[comparison.sideA, comparison.sideB].map((side) => (
                <div key={side.name} className="card-feature p-6">
                  <h3 className="text-xl font-bold mb-4">{side.name}</h3>

                  <div className="mb-4">
                    <h4 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground mb-2">Strengths</h4>
                    <ul className="space-y-2">
                      {side.strengths.map((s, i) => (
                        <li key={i} className="flex items-start gap-2 text-sm leading-relaxed">
                          <span className="text-green-600  mt-0.5 shrink-0">+</span>
                          <span>{s}</span>
                        </li>
                      ))}
                    </ul>
                  </div>

                  <div className="mb-4">
                    <h4 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground mb-2">Weaknesses</h4>
                    <ul className="space-y-2">
                      {side.weaknesses.map((w, i) => (
                        <li key={i} className="flex items-start gap-2 text-sm leading-relaxed text-muted-foreground">
                          <span className="mt-0.5 shrink-0">&minus;</span>
                          <span>{w}</span>
                        </li>
                      ))}
                    </ul>
                  </div>

                  <div className="pt-4 border-t border-border">
                    <span className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">Pricing: </span>
                    <span className="text-sm">{side.pricing}</span>
                  </div>
                </div>
              ))}
            </div>
          </section>

          <section className="mb-16 max-w-3xl">
            <h2 className="text-2xl font-bold mb-6">Verdict</h2>
            <div className="bg-primary/5 border border-primary/20 rounded-xl p-6">
              <p className="text-muted-foreground leading-relaxed">{comparison.verdict}</p>
            </div>
          </section>

          {comparison.faqs.length > 0 && (
            <section className="mb-16 max-w-3xl">
              <h2 className="text-2xl font-bold mb-8">Frequently Asked Questions</h2>
              <div className="space-y-6">
                {comparison.faqs.map((faq, i) => (
                  <div key={i} className="border border-border rounded-xl p-6">
                    <h3 className="font-bold mb-3">{faq.question}</h3>
                    <p className="text-muted-foreground leading-relaxed">{faq.answer}</p>
                  </div>
                ))}
              </div>
            </section>
          )}

          {otherComparisons.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">More Comparisons</h2>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {otherComparisons.map((c) => (
                  <Link
                    key={c.slug}
                    href={`/compare/${c.slug}`}
                    className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
                  >
                    <h3 className="font-bold group-hover:text-primary transition-colors mb-2">{c.title}</h3>
                    <p className="text-sm text-muted-foreground line-clamp-2">{c.description}</p>
                  </Link>
                ))}
              </div>
            </section>
          )}

          {relatedArticles.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">Related Resources</h2>
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

          {relatedHowToGuides.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">How-To Guides</h2>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {relatedHowToGuides.map((s) => (
                  <Link
                    key={s.slug}
                    href={`/how-to/${s.slug}`}
                    className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
                  >
                    <h3 className="font-bold group-hover:text-primary transition-colors mb-2 line-clamp-2 text-sm">{s.title}</h3>
                    <div className="flex items-center gap-1.5 text-xs text-muted-foreground mt-1">
                      <span>{s.skillOffered}</span>
                      <span>↔</span>
                      <span>{s.skillNeeded}</span>
                    </div>
                  </Link>
                ))}
              </div>
            </section>
          )}

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
            <div className="text-center mt-4">
              <Link href="/features" className="text-primary font-medium hover:underline text-sm">All Features →</Link>
            </div>
          </section>

          <FunnelLinks stage="mofu" features={relatedFeatures} />
          <FunnelCTA stage="mofu" />
        </div>
      </div>
    </>
  )
}
