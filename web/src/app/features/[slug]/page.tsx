import { notFound } from 'next/navigation'
import type { Metadata } from 'next'
import Link from 'next/link'
import { Wallet, ShieldCheck, Award, MessageSquare, Search } from 'lucide-react'
import { featuresData, getFeatureBySlug } from '@/lib/data/features-data'
import { categoriesData } from '@/lib/data/categories-data'
import { comparisonsData } from '@/lib/data/comparisons-data'
import { findArticlesForCategory, findScenariosForCategory } from '@/lib/cross-links'
import { buildPublicPageMetadata, generateFAQSchema, generateBreadcrumbSchema, SITE_CONFIG } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'

export const dynamicParams = false

const iconMap: Record<string, React.ElementType> = {
  Wallet,
  ShieldCheck,
  Award,
  MessageSquare,
  Search,
}

interface Props {
  params: Promise<{ slug: string }>
}

export async function generateStaticParams() {
  return featuresData.map((f) => ({ slug: f.slug }))
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params
  const feature = getFeatureBySlug(slug)
  if (!feature) return {}
  return buildPublicPageMetadata(
    feature.name,
    feature.description,
    `/features/${slug}`,
    feature.targetKeywords
  )
}

export default async function FeatureDetailPage({ params }: Props) {
  const { slug } = await params
  const feature = getFeatureBySlug(slug)
  if (!feature) notFound()

  const Icon = iconMap[feature.icon] || Wallet

  // Cross-links: related categories, articles, trade pairs, and how-to guides
  const relatedCats = categoriesData.filter((c) => feature.relatedCategories.includes(c.slug))
  const relatedArticles = feature.relatedCategories
    .flatMap((catSlug) => findArticlesForCategory(catSlug))
    .filter((a, i, self) => self.findIndex((b) => b.slug === a.slug) === i)
    .slice(0, 4)

  // Trade pairs: combine related categories into /trade/${catA}/for/${catB} links (up to 3)
  const tradePairs: { catA: string; catB: string; nameA: string; nameB: string }[] = []
  for (let i = 0; i < relatedCats.length && tradePairs.length < 3; i++) {
    for (let j = i + 1; j < relatedCats.length && tradePairs.length < 3; j++) {
      tradePairs.push({ catA: relatedCats[i].slug, catB: relatedCats[j].slug, nameA: relatedCats[i].name, nameB: relatedCats[j].name })
    }
  }

  // How-to guides: find scenarios for any related category
  const relatedScenarios = feature.relatedCategories
    .flatMap((catSlug) => findScenariosForCategory(catSlug))
    .filter((s, i, self) => self.findIndex((t) => t.slug === s.slug) === i)
    .slice(0, 3)

  const relatedComparisons = comparisonsData.slice(0, 3)

  const otherFeatures = featuresData.filter((f) => f.slug !== slug)

  const faqSchema = feature.faqs.length > 0 ? generateFAQSchema(feature.faqs) : null
  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'Features', url: `${SITE_CONFIG.url}/features` },
    { name: feature.name, url: `${SITE_CONFIG.url}/features/${slug}` },
  ])

  return (
    <>
      {faqSchema && <JsonLd schema={faqSchema} />}
      <JsonLd schema={breadcrumbSchema} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' / '}
            <Link href="/features" className="hover:text-foreground">Features</Link>
            {' / '}
            <span>{feature.name}</span>
          </nav>

          <header className="mb-16 max-w-3xl">
            <div className="flex items-center gap-4 mb-6">
              <div className="p-4 bg-gradient-to-br from-primary/20 to-primary/10 rounded-2xl">
                <Icon className="w-8 h-8 text-primary" />
              </div>
              <h1 className="text-4xl lg:text-5xl font-black tracking-tight">{feature.name}</h1>
            </div>
            <p className="text-xl text-muted-foreground leading-relaxed">{feature.tagline}</p>
          </header>

          {/* Long Description */}
          <section className="mb-16 max-w-3xl">
            {feature.longDescription.split('\n\n').map((para, i) => (
              <p key={i} className="text-muted-foreground leading-relaxed mb-6">{para}</p>
            ))}
          </section>

          {/* Benefits */}
          <section className="mb-16">
            <h2 className="text-2xl font-bold mb-8">Key Benefits</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {feature.benefits.map((benefit, i) => (
                <div key={i} className="flex items-start gap-3 card-feature p-5">
                  <span className="text-primary font-bold text-lg shrink-0">&#10003;</span>
                  <p className="text-sm leading-relaxed">{benefit}</p>
                </div>
              ))}
            </div>
          </section>

          {/* FAQs */}
          {feature.faqs.length > 0 && (
            <section className="mb-16 max-w-3xl">
              <h2 className="text-2xl font-bold mb-8">Frequently Asked Questions</h2>
              <div className="space-y-6">
                {feature.faqs.map((faq, i) => (
                  <div key={i} className="border border-border rounded-xl p-6">
                    <h3 className="font-bold mb-3">{faq.question}</h3>
                    <p className="text-muted-foreground leading-relaxed">{faq.answer}</p>
                  </div>
                ))}
              </div>
            </section>
          )}

          {/* Related Categories */}
          {relatedCats.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">Related Skill Categories</h2>
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                {relatedCats.map((cat) => (
                  <Link
                    key={cat.slug}
                    href={`/categories/${cat.slug}`}
                    className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
                  >
                    <h3 className="font-bold group-hover:text-primary transition-colors mb-2">{cat.name}</h3>
                    <p className="text-sm text-muted-foreground line-clamp-2">{cat.description}</p>
                  </Link>
                ))}
              </div>
            </section>
          )}

          {/* Related Articles */}
          {relatedArticles.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">Related Articles</h2>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
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

          {/* Trade Pair Links */}
          {tradePairs.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">Related Exchanges</h2>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {tradePairs.map((pair) => (
                  <Link
                    key={`${pair.catA}-${pair.catB}`}
                    href={`/trade/${pair.catA}/for/${pair.catB}`}
                    className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
                  >
                    <div className="flex items-center gap-2 text-sm font-medium">
                      <span className="px-2 py-1 bg-primary/10 text-primary rounded">{pair.nameA}</span>
                      <span className="text-muted-foreground">&harr;</span>
                      <span className="px-2 py-1 bg-primary/10 text-primary rounded">{pair.nameB}</span>
                    </div>
                    <p className="text-xs text-muted-foreground mt-2 group-hover:text-primary transition-colors">Browse exchange hub &rarr;</p>
                  </Link>
                ))}
              </div>
            </section>
          )}

          {/* How-To Guides */}
          {relatedScenarios.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">How-To Guides</h2>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {relatedScenarios.map((scenario) => (
                  <Link
                    key={scenario.slug}
                    href={`/how-to/${scenario.slug}`}
                    className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
                  >
                    <h3 className="font-bold group-hover:text-primary transition-colors mb-2 line-clamp-2 text-sm">
                      {scenario.title}
                    </h3>
                    <div className="flex items-center gap-1.5 text-xs text-muted-foreground mt-2">
                      <span>{scenario.skillOffered}</span>
                      <span>&harr;</span>
                      <span>{scenario.skillNeeded}</span>
                    </div>
                  </Link>
                ))}
              </div>
            </section>
          )}

          {/* Compare Platforms */}
          <section className="mb-16">
            <h2 className="text-2xl font-bold mb-6">See How We Compare</h2>
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
            <div className="text-center mt-4">
              <Link href="/compare" className="text-primary font-medium hover:underline text-sm">View All Comparisons →</Link>
            </div>
          </section>

          {/* Other Features */}
          <section className="mb-16">
            <h2 className="text-2xl font-bold mb-6">Explore More Features</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {otherFeatures.map((f) => {
                const OtherIcon = iconMap[f.icon] || Wallet
                return (
                  <Link
                    key={f.slug}
                    href={`/features/${f.slug}`}
                    className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
                  >
                    <div className="flex items-center gap-3 mb-2">
                      <OtherIcon className="w-5 h-5 text-primary shrink-0" />
                      <h3 className="font-bold group-hover:text-primary transition-colors">{f.name}</h3>
                    </div>
                    <p className="text-sm text-muted-foreground line-clamp-2">{f.tagline}</p>
                  </Link>
                )
              })}
            </div>
          </section>

          <FunnelLinks stage="mofu" features={otherFeatures.slice(0, 3)} />
          <FunnelCTA stage="mofu" />
        </div>
      </div>
    </>
  )
}
