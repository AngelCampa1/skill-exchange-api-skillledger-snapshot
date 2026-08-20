import { notFound } from 'next/navigation'
import type { Metadata } from 'next'
import Link from 'next/link'
import { glossaryData, getTermBySlug } from '@/lib/data/glossary-data'
import { comparisonsData } from '@/lib/data/comparisons-data'
import { findArticlesForTerm, skillToCategorySlug, findScenariosForCategory } from '@/lib/cross-links'
import { categoriesData } from '@/lib/data/categories-data'
import { buildPublicPageMetadata, generateBreadcrumbSchema, SITE_CONFIG } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

export const dynamicParams = false

interface Props {
  params: Promise<{ term: string }>
}

export async function generateStaticParams() {
  return glossaryData.map((t) => ({ term: t.slug }))
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { term: termSlug } = await params
  const term = getTermBySlug(termSlug)
  if (!term) return {}
  return buildPublicPageMetadata(
    term.term,
    term.definition.slice(0, 155),
    `/glossary/${termSlug}`,
    ['skill exchange glossary', 'barter economy terms']
  )
}

export default async function GlossaryTermPage({ params }: Props) {
  const { term: termSlug } = await params
  const term = getTermBySlug(termSlug)
  if (!term) notFound()

  const definedTermSchema = {
    '@context': 'https://schema.org',
    '@type': 'DefinedTerm',
    name: term.term,
    description: term.definition,
    inDefinedTermSet: {
      '@type': 'DefinedTermSet',
      name: 'SkillLedger Glossary',
      url: `${SITE_CONFIG.url}/glossary`,
    },
  }

  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'Glossary', url: `${SITE_CONFIG.url}/glossary` },
    { name: term.term, url: `${SITE_CONFIG.url}/glossary/${termSlug}` },
  ])

  const relatedTerms = term.relatedTerms
    .map((slug) => glossaryData.find((t) => t.slug === slug))
    .filter(Boolean)

  const relatedArticles = findArticlesForTerm(termSlug, term.term).slice(0, 3)

  // Find related category (if this term maps to a skill category)
  const termCategorySlug = skillToCategorySlug(term.term) || skillToCategorySlug(termSlug.replace(/-/g, ' '))
  const termCategory = termCategorySlug ? categoriesData.find((c) => c.slug === termCategorySlug) : null
  const relatedScenarios = termCategorySlug ? findScenariosForCategory(termCategorySlug).slice(0, 3) : []

  // Generic TOFU funnel links: use top comparisons and existing relatedScenarios
  const genericComparisons = comparisonsData.slice(0, 3)

  return (
    <>
      <JsonLd schema={definedTermSchema} />
      <JsonLd schema={breadcrumbSchema} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24 max-w-4xl">
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' / '}
            <Link href="/glossary" className="hover:text-foreground">Glossary</Link>
            {' / '}
            <span>{term.term}</span>
          </nav>

          <header className="mb-12">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">{term.term}</h1>
            <p className="text-xl text-muted-foreground leading-relaxed">{term.definition}</p>
          </header>

          {relatedTerms.length > 0 && (
            <section className="mb-12">
              <h2 className="text-xl font-bold mb-4">Related Terms</h2>
              <div className="flex flex-wrap gap-3">
                {relatedTerms.map((related) => related && (
                  <Link
                    key={related.slug}
                    href={`/glossary/${related.slug}`}
                    className="px-4 py-2 border border-border rounded-lg text-sm font-medium hover:border-primary hover:text-primary transition-colors"
                  >
                    {related.term}
                  </Link>
                ))}
              </div>
            </section>
          )}

          {termCategory && (
            <section className="mb-12">
              <h2 className="text-xl font-bold mb-4">Related Skill Category</h2>
              <Link
                href={`/categories/${termCategory.slug}`}
                className="inline-flex items-center gap-2 px-5 py-3 border border-border rounded-xl hover:border-primary hover:text-primary transition-colors group"
              >
                <span className="font-semibold">{termCategory.name}</span>
                <span className="text-xs text-muted-foreground group-hover:text-primary">&rarr; Browse exchange hub</span>
              </Link>
            </section>
          )}

          {relatedScenarios.length > 0 && (
            <section className="mb-12">
              <h2 className="text-xl font-bold mb-4">How-To Guides</h2>
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
                    <div className="flex items-center gap-1.5 text-xs text-muted-foreground mt-1">
                      <span>{scenario.skillOffered}</span>
                      <span>&harr;</span>
                      <span>{scenario.skillNeeded}</span>
                    </div>
                  </Link>
                ))}
              </div>
            </section>
          )}

          {relatedArticles.length > 0 && (
            <section className="mb-12">
              <h2 className="text-xl font-bold mb-4">Learn More</h2>
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
            comparisons={genericComparisons.map((c) => ({ slug: c.slug, title: c.title, description: c.description }))}
            howToGuides={relatedScenarios.map((s) => ({ slug: s.slug, title: s.title, skillOffered: s.skillOffered, skillNeeded: s.skillNeeded }))}
          />

          <FunnelCTA stage="tofu" />

          <div className="mt-8 pt-8 border-t border-border flex items-center justify-between">
            <Link href="/glossary" className="text-sm text-muted-foreground hover:text-foreground">
              &larr; Back to full glossary
            </Link>
            <Link href="/resources" className="text-sm text-primary font-medium hover:underline">
              Browse All Resources
            </Link>
          </div>
        </div>
      </div>
    </>
  )
}
