import type { Metadata } from'next'
import Link from'next/link'
import { buildPublicPageMetadata, generateBreadcrumbSchema, SITE_CONFIG } from'@/lib/seo'
import { getArticleBySlug } from'@/lib/content'
import { JsonLd } from'@/components/marketing/JsonLd'
import { featuresData } from'@/lib/data/features-data'
import { FunnelLinks } from'@/components/marketing/FunnelLinks'
import { FunnelCTA } from'@/components/marketing/FunnelCTA'

export const metadata: Metadata = buildPublicPageMetadata('Free Barter Contract Templates','Download free barter contract templates including service agreements, NDAs, statements of work, invoices, and scope change addendums.','/resources/templates',
  ['barter contract template','service exchange agreement','barter invoice template']
)

const templates = [
  {
    title:'Non-Monetary Service Agreement',
    description:'A comprehensive contract template for professional barter exchanges. Covers scope of work, fair market value declarations, delivery timelines, quality standards, and dispute resolution. Designed for direct service-for-service trades between independent professionals.',
  },
  {
    title:'Barter-Specific NDA',
    description:'Protects confidential information shared during skill exchanges. Unlike generic NDAs, this template addresses the unique risks of barter — where both parties simultaneously act as client and provider, creating bidirectional confidentiality obligations.',
  },
  {
    title:'Skill Swap Statement of Work',
    description:'Defines deliverables, milestones, credit values, and acceptance criteria for each side of a barter exchange. Includes sections for revision limits, timeline dependencies, and the credit allocation schedule that governs the trade.',
  },
  {
    title:'Zero-Balance Barter Invoice',
    description:'Documents the fair market value of exchanged services for IRS reporting purposes. Both parties issue invoices showing the FMV of services rendered, creating the paper trail required for Form 1099-B compliance and barter exchange reporting.',
  },
  {
    title:'Scope Change Addendum',
    description:'Handles mid-project changes to barter terms without renegotiating the entire agreement. Covers additional deliverables, timeline extensions, credit adjustments, and the approval process for modifying an active exchange.',
  },
]

export default function TemplatesPage() {
  const funnelFeatures = featuresData.slice(0, 3)

  const relatedArticles = ['barter-contract-templates','how-to-invoice-barter-transaction','ip-rights-in-skill-exchange',
  ]
    .map((s) => getArticleBySlug(s))
    .filter((a): a is NonNullable<ReturnType<typeof getArticleBySlug>> => a !== null)

  const breadcrumbSchema = generateBreadcrumbSchema([
    { name:'Home', url: SITE_CONFIG.url },
    { name:'Resources', url: `${SITE_CONFIG.url}/resources` },
    { name:'Templates', url: `${SITE_CONFIG.url}/resources/templates` },
  ])

  return (
    <>
      <JsonLd schema={breadcrumbSchema} />

      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          {/* Breadcrumb */}
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' /'}
            <Link href="/resources" className="hover:text-foreground">Resources</Link>
            {' /'}
            <span>Templates</span>
          </nav>

          {/* Header */}
          <div className="max-w-3xl mb-12">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">
              Barter Contract Templates for Professionals
            </h1>
            <p className="text-xl text-muted-foreground leading-relaxed">
              Professional barter exchanges deserve professional documentation. These templates
              help you formalize skill swaps, protect both parties, and maintain compliance with
              IRS reporting requirements.
            </p>
          </div>

          {/* Template cards */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-16">
            {templates.map((template) => (
              <div
                key={template.title}
                className="card-feature p-6 flex flex-col"
              >
                <div className="flex items-start justify-between mb-3">
                  <h2 className="text-lg font-bold">{template.title}</h2>
                  <span className="shrink-0 ml-3 text-xs font-semibold rounded-full bg-amber-100 text-amber-800   px-2.5 py-0.5">
                    Coming Soon
                  </span>
                </div>
                <p className="text-sm text-muted-foreground leading-relaxed mb-6 flex-1">
                  {template.description}
                </p>
                <Link
                  href="/register"
                  className="btn-primary inline-block text-center text-sm"
                >
                  Register for Early Access
                </Link>
              </div>
            ))}
          </div>

          {/* Related Resources */}
          {relatedArticles.length > 0 && (
            <section className="mb-16">
              <h2 className="text-2xl font-bold mb-6">Related Resources</h2>
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
              <p className="text-muted-foreground mt-4">
                Calculate fair exchange values with our{''}
                <Link href="/tools/barter-valuation-calculator" className="text-primary font-medium hover:underline">
                  Barter Valuation Calculator
                </Link>.
              </p>
            </section>
          )}

          <FunnelLinks stage="mofu" features={funnelFeatures} />
          <FunnelCTA stage="mofu" />
        </div>
      </div>
    </>
  )
}
