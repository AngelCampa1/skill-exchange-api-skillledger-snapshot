import { notFound } from'next/navigation'
import type { Metadata } from'next'
import Link from'next/link'
import { industriesData, getIndustryBySlug } from'@/lib/data/industries-data'
import { skillToCategorySlug, findScenariosForIndustry, findArticlesForIndustry, findComparisonsForIndustry, findFeaturesForCategory } from'@/lib/cross-links'
import { buildPublicPageMetadata, generateFAQSchema, generateBreadcrumbSchema, generateItemListSchema, SITE_CONFIG } from'@/lib/seo'
import { JsonLd } from'@/components/marketing/JsonLd'
import { FunnelCTA } from'@/components/marketing/FunnelCTA'
import { FunnelLinks } from'@/components/marketing/FunnelLinks'

export const dynamicParams = false

interface Props {
  params: Promise<{ slug: string }>
}

export async function generateStaticParams() {
  return industriesData.map((i) => ({ slug: i.slug }))
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params
  const industry = getIndustryBySlug(slug)
  if (!industry) return {}
  const metaDesc = `Skill exchange for ${industry.name.toLowerCase()} on SkillLedger. ${industry.description}`
  return buildPublicPageMetadata(
    `${industry.name} Skill Exchange`,
    metaDesc.length > 160 ? metaDesc.slice(0, 157) +'...' : metaDesc,
    `/industries/${slug}`,
    [industry.name.toLowerCase(), `${industry.name.toLowerCase()} barter`, `${industry.name.toLowerCase()} skill exchange`]
  )
}

export default async function IndustryPage({ params }: Props) {
  const { slug } = await params
  const industry = getIndustryBySlug(slug)
  if (!industry) notFound()

  const relatedScenarios = findScenariosForIndustry(slug).slice(0, 4)
  const relatedArticles = findArticlesForIndustry(slug).slice(0, 4)
  const relatedComparisons = findComparisonsForIndustry(slug).slice(0, 3)

  // Feature links: pull features related to the industry's pairing category slugs
  const industryCatSlugs = industry.commonPairings
    .flatMap((p) => [skillToCategorySlug(p.skillOffered), skillToCategorySlug(p.skillNeeded)])
    .filter((s): s is string => !!s)
  const relatedFeatures = [...new Set(industryCatSlugs)]
    .flatMap((catSlug) => findFeaturesForCategory(catSlug))
    .filter((f, i, self) => self.findIndex((g) => g.slug === f.slug) === i)
    .slice(0, 3)

  const faqSchema = industry.faqs.length > 0 ? generateFAQSchema(industry.faqs) : null
  const breadcrumbSchema = generateBreadcrumbSchema([
    { name:'Home', url: SITE_CONFIG.url },
    { name:'Industries', url: `${SITE_CONFIG.url}/industries` },
    { name: industry.name, url: `${SITE_CONFIG.url}/industries/${slug}` },
  ])
  const pairingsSchema = generateItemListSchema(
    industry.commonPairings.map((p) => ({
      name: `${p.skillOffered} for ${p.skillNeeded}`,
      url: `${SITE_CONFIG.url}/industries/${slug}#pairings`,
      description: p.description,
    })),
    `${industry.name} Skill Pairings`
  )

  return (
    <>
      {faqSchema && <JsonLd schema={faqSchema} />}
      <JsonLd schema={breadcrumbSchema} />
      <JsonLd schema={pairingsSchema} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' /'}
            <Link href="/industries" className="hover:text-foreground">Industries</Link>
            {' /'}
            <span>{industry.name}</span>
          </nav>

          <header className="mb-16 max-w-3xl">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">{industry.name} Skill Exchange</h1>
            <p className="text-xl text-muted-foreground leading-relaxed mb-8">{industry.description}</p>
            <Link href="/register" className="btn-primary">Start Exchanging Skills</Link>
          </header>

          {industry.keyStatistic && (
            <div className="rounded-lg border-l-4 border-primary bg-primary/5 p-4 mb-8">
              <p className="text-sm font-medium">{industry.keyStatistic}</p>
            </div>
          )}

          <section className="mb-16 max-w-3xl">
            <h2 className="text-2xl font-bold mb-6">Overview</h2>
            <div className="prose prose-lg text-muted-foreground leading-relaxed space-y-4">
              {industry.longDescription.split('\n\n').map((paragraph, i) => (
                <p key={i}>{paragraph.trim()}</p>
              ))}
            </div>
          </section>

          <section className="mb-16">
            <h2 className="text-2xl font-bold mb-6">Key Benefits</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {industry.keyBenefits.map((benefit, i) => (
                <div key={i} className="flex items-start gap-3 p-4 border border-border rounded-xl">
                  <span className="text-primary font-bold text-lg mt-0.5">{i + 1}.</span>
                  <p className="text-muted-foreground leading-relaxed">{benefit}</p>
                </div>
              ))}
            </div>
          </section>

          <section id="pairings" className="mb-16">
            <h2 className="text-2xl font-bold mb-6">Common Skill Pairings</h2>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {industry.commonPairings.map((pairing, i) => {
                const offeredSlug = skillToCategorySlug(pairing.skillOffered)
                const neededSlug = skillToCategorySlug(pairing.skillNeeded)
                return (
                  <div key={i} className="border border-border rounded-xl p-6">
                    <div className="flex items-center gap-2 mb-3">
                      {offeredSlug ? (
                        <Link href={`/categories/${offeredSlug}`} className="px-3 py-1 bg-primary/10 text-primary rounded-full text-sm font-medium hover:bg-primary/20 transition-colors">{pairing.skillOffered}</Link>
                      ) : (
                        <span className="px-3 py-1 bg-primary/10 text-primary rounded-full text-sm font-medium">{pairing.skillOffered}</span>
                      )}
                      <span className="text-muted-foreground">for</span>
                      {neededSlug ? (
                        <Link href={`/categories/${neededSlug}`} className="px-3 py-1 bg-primary/10 text-primary rounded-full text-sm font-medium hover:bg-primary/20 transition-colors">{pairing.skillNeeded}</Link>
                      ) : (
                        <span className="px-3 py-1 bg-primary/10 text-primary rounded-full text-sm font-medium">{pairing.skillNeeded}</span>
                      )}
                    </div>
                    <p className="text-muted-foreground leading-relaxed text-sm">{pairing.description}</p>
                  </div>
                )
              })}
            </div>
          </section>

          <section className="mb-16 max-w-3xl">
            <h2 className="text-2xl font-bold mb-6">Regulatory Considerations</h2>
            <div className="bg-yellow-50  border border-yellow-200  rounded-xl p-6">
              <p className="text-muted-foreground leading-relaxed">{industry.regulatoryNotes}</p>
            </div>
          </section>

          {industry.faqs.length > 0 && (
            <section className="mb-16 max-w-3xl">
              <h2 className="text-2xl font-bold mb-8">Frequently Asked Questions</h2>
              <div className="space-y-6">
                {industry.faqs.map((faq, i) => (
                  <div key={i} className="border border-border rounded-xl p-6">
                    <h3 className="font-bold mb-3">{faq.question}</h3>
                    <p className="text-muted-foreground leading-relaxed">{faq.answer}</p>
                  </div>
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
