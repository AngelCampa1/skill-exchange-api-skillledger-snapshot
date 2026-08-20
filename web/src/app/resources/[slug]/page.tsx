import { notFound } from 'next/navigation'
import Link from 'next/link'
import { getAllArticles, getArticleBySlug } from '@/lib/content'
import { buildArticleMetadata, generateBreadcrumbSchema, generateArticleSchema, generateFAQSchema, SITE_CONFIG } from '@/lib/seo'
import { MDXContent } from '@/components/mdx/MDXContent'
import { NewsletterSignup } from '@/components/NewsletterSignup'
import { JsonLd } from '@/components/marketing/JsonLd'
import { findCategoriesForArticle, findTradePairsForArticle, findHowToGuidesForArticle, findComparisonsForArticle } from '@/lib/cross-links'
import { buyerStageToFunnel } from '@/lib/funnel'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import type { Metadata } from 'next'

interface Props {
  params: Promise<{ slug: string }>
}

// Prevent runtime fallback rendering — all valid slugs are pre-rendered at build time.
// Requests for unknown slugs return 404 statically.
export const dynamicParams = false

export async function generateStaticParams() {
  const articles = getAllArticles()
  return articles.map((a) => ({ slug: a.slug }))
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params
  const article = getArticleBySlug(slug)
  if (!article) return {}

  return buildArticleMetadata({
    title: article.frontmatter.title,
    description: article.frontmatter.description,
    path: `/resources/${slug}`,
    publishedAt: article.frontmatter.publishedAt,
    tags: article.frontmatter.tags,
  })
}

export default async function ArticlePage({ params }: Props) {
  const { slug } = await params
  const article = getArticleBySlug(slug)
  if (!article) notFound()

  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'Resources', url: `${SITE_CONFIG.url}/resources` },
    { name: article.frontmatter.title, url: `${SITE_CONFIG.url}/resources/${slug}` },
  ])

  const articleSchema = generateArticleSchema({
    title: article.frontmatter.title,
    description: article.frontmatter.description,
    url: `${SITE_CONFIG.url}/resources/${slug}`,
    publishedAt: article.frontmatter.publishedAt,
    modifiedAt: article.frontmatter.modifiedAt,
    author: article.frontmatter.author,
  })

  const faqSchema = article.frontmatter.faqs?.length
    ? generateFAQSchema(article.frontmatter.faqs)
    : null

  const relatedArticles = (article.frontmatter.relatedSlugs ?? [])
    .map((s) => getArticleBySlug(s))
    .filter((a): a is NonNullable<ReturnType<typeof getArticleBySlug>> => a !== null)

  const relatedCategories = findCategoriesForArticle(article.frontmatter.tags || [])
  const relatedTradePairs = findTradePairsForArticle(article.frontmatter.tags || [])
  const relatedHowToGuides = findHowToGuidesForArticle(article.frontmatter.tags || [])
  const relatedComparisons = findComparisonsForArticle(article.frontmatter.tags || [])
  const funnelStage = buyerStageToFunnel(article.frontmatter.buyerStage ?? 'awareness')

  return (
    <>
      <JsonLd schema={breadcrumbSchema} />
      <JsonLd schema={articleSchema} />
      {faqSchema && <JsonLd schema={faqSchema} />}
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' / '}
            <Link href="/resources" className="hover:text-foreground">Resources</Link>
            {' / '}
            <span>{article.frontmatter.title}</span>
          </nav>

          <article className="max-w-3xl mx-auto">
            <section aria-label="Article header">
              <header className="mb-12">
                <div className="flex items-center gap-4 text-sm text-muted-foreground mb-4">
                  <span>{article.frontmatter.silo.replace(/-/g, ' ')}</span>
                  <span>·</span>
                  <time dateTime={article.frontmatter.publishedAt}>
                    {new Date(article.frontmatter.publishedAt).toLocaleDateString('en-US', {
                      year: 'numeric', month: 'long', day: 'numeric'
                    })}
                  </time>
                  {article.frontmatter.modifiedAt && article.frontmatter.modifiedAt !== article.frontmatter.publishedAt && (
                    <>
                      <span>·</span>
                      <span>
                        Updated{' '}
                        <time dateTime={article.frontmatter.modifiedAt}>
                          {new Date(article.frontmatter.modifiedAt).toLocaleDateString('en-US', {
                            year: 'numeric', month: 'long', day: 'numeric'
                          })}
                        </time>
                      </span>
                    </>
                  )}
                  <span>·</span>
                  <span>{article.readingTime}</span>
                </div>
                <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">
                  {article.frontmatter.title}
                </h1>
                <p className="text-xl text-muted-foreground leading-relaxed">
                  {article.frontmatter.description}
                </p>
              </header>
            </section>

            {article.frontmatter.keyTakeaways && article.frontmatter.keyTakeaways.length > 0 && (
              <aside className="rounded-xl border border-primary/20 bg-primary/5 p-6 mb-8" aria-label="Key takeaways">
                <h2 className="text-sm font-bold uppercase tracking-wider text-primary mb-3">Key Takeaways</h2>
                <ul className="space-y-2">
                  {article.frontmatter.keyTakeaways.map((takeaway, i) => (
                    <li key={i} className="flex items-start gap-2 text-sm">
                      <span className="text-primary font-bold mt-0.5">&mdash;</span>
                      <span>{takeaway}</span>
                    </li>
                  ))}
                </ul>
              </aside>
            )}

            <section aria-label="Article body">
              <MDXContent content={article.content} />
            </section>

            {article.frontmatter.faqs && article.frontmatter.faqs.length > 0 && (
              <section className="mt-12 pt-8 border-t border-border" aria-label="Frequently asked questions">
                <h2 className="text-2xl font-bold mb-6">Frequently Asked Questions</h2>
                <dl className="space-y-6">
                  {article.frontmatter.faqs.map((faq, i) => (
                    <div key={i}>
                      <dt className="text-base font-semibold text-foreground mb-2">{faq.question}</dt>
                      <dd className="text-muted-foreground leading-relaxed">{faq.answer}</dd>
                    </div>
                  ))}
                </dl>
              </section>
            )}

            <NewsletterSignup variant="inline" />

            {(relatedCategories.length > 0 || relatedTradePairs.length > 0 || relatedHowToGuides.length > 0 || relatedComparisons.length > 0) && (
              <section className="mb-12" aria-label="Related pages">
                <h2 className="text-xl font-bold mb-4">Explore More</h2>
                <div className="flex flex-wrap gap-3">
                  {relatedCategories.map((cat) => (
                    <Link key={cat.slug} href={`/categories/${cat.slug}`} className="rounded-lg border border-border px-4 py-2 text-sm hover:border-primary/30 hover:bg-primary/5 transition-colors">
                      {cat.name}
                    </Link>
                  ))}
                  {relatedTradePairs.map((pair) => (
                    <Link key={`${pair.skillA}-${pair.skillB}`} href={`/trade/${pair.skillA}/for/${pair.skillB}`} className="rounded-lg border border-border px-4 py-2 text-sm hover:border-primary/30 hover:bg-primary/5 transition-colors">
                      {pair.nameA} for {pair.nameB}
                    </Link>
                  ))}
                  {relatedHowToGuides.map((guide) => (
                    <Link key={guide.slug} href={`/how-to/${guide.slug}`} className="rounded-lg border border-border px-4 py-2 text-sm hover:border-primary/30 hover:bg-primary/5 transition-colors">
                      How-To: {guide.title}
                    </Link>
                  ))}
                  {relatedComparisons.map((comparison) => (
                    <Link key={comparison.slug} href={`/compare/${comparison.slug}`} className="rounded-lg border border-border px-4 py-2 text-sm hover:border-primary/30 hover:bg-primary/5 transition-colors">
                      {comparison.title}
                    </Link>
                  ))}
                </div>
              </section>
            )}

            {/* FunnelLinks shows "Dig Deeper" / "Ready to Decide?" content blocks before the final CTA */}
            <FunnelLinks
              stage={funnelStage}
              comparisons={relatedComparisons.map((c) => ({ slug: c.slug, title: c.title, description: c.description }))}
              howToGuides={relatedHowToGuides.map((h) => ({ slug: h.slug, title: h.title, skillOffered: h.skillOffered, skillNeeded: h.skillNeeded }))}
            />

            {relatedArticles.length > 0 && (
              <section className="mt-16 pt-12 border-t border-border" aria-label="Related articles">
                <h2 className="text-2xl font-bold mb-6">Related Articles</h2>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {relatedArticles.map((related) => (
                    <Link
                      key={related.slug}
                      href={`/resources/${related.slug}`}
                      className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
                    >
                      <h3 className="font-bold group-hover:text-primary transition-colors mb-2 line-clamp-2">
                        {related.frontmatter.title}
                      </h3>
                      <p className="text-sm text-muted-foreground line-clamp-2">{related.frontmatter.description}</p>
                      <span className="text-xs text-muted-foreground mt-2 inline-block">{related.readingTime}</span>
                    </Link>
                  ))}
                </div>
              </section>
            )}

            <FunnelCTA stage={funnelStage} />

            <div className="mt-12 pt-8 border-t border-border text-center">
              <Link href="/resources" className="text-primary font-medium hover:underline">
                Browse All Resources
              </Link>
            </div>
          </article>
        </div>
      </div>
    </>
  )
}
