import type { Metadata } from 'next'
import Link from 'next/link'
import { buildPublicPageMetadata, generateBreadcrumbSchema, SITE_CONFIG } from '@/lib/seo'
import { getArticleBySlug } from '@/lib/content'
import { BarterCalculator } from '@/components/tools/BarterCalculator'
import { JsonLd } from '@/components/marketing/JsonLd'
import { comparisonsData } from '@/lib/data/comparisons-data'
import { scenariosData } from '@/lib/data/scenarios-data'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

export const metadata: Metadata = buildPublicPageMetadata(
  'Barter Valuation Calculator',
  'Calculate fair exchange rates for professional service barter. Compare hourly rates across skill categories and determine equitable trade values for IRS compliance.',
  '/tools/barter-valuation-calculator',
  ['barter valuation calculator', 'skill exchange calculator', 'fair market value barter']
)

export default function BarterValuationCalculatorPage() {
  const funnelComparisons = comparisonsData.slice(0, 3)
  const funnelHowTo = scenariosData.slice(0, 3)

  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'Resources', url: `${SITE_CONFIG.url}/resources` },
    { name: 'Barter Valuation Calculator', url: `${SITE_CONFIG.url}/tools/barter-valuation-calculator` },
  ])

  const relatedArticles = [
    'how-to-value-services-barter',
    'barter-income-taxes-freelancer-guide',
    'irs-form-1099-b-explained',
    'barter-contract-templates',
  ]
    .map((s) => getArticleBySlug(s))
    .filter((a): a is NonNullable<ReturnType<typeof getArticleBySlug>> => a !== null)

  const webAppSchema = {
    '@context': 'https://schema.org',
    '@type': 'WebApplication',
    name: 'Barter Valuation Calculator',
    description:
      'Calculate fair exchange rates for professional service barter. Compare hourly rates across skill categories and determine equitable trade values for IRS compliance.',
    url: `${SITE_CONFIG.url}/tools/barter-valuation-calculator`,
    applicationCategory: 'BusinessApplication',
    operatingSystem: 'Web',
    offers: {
      '@type': 'Offer',
      price: '0',
      priceCurrency: 'USD',
    },
  }

  return (
    <>
      <JsonLd schema={breadcrumbSchema} />
      <JsonLd schema={webAppSchema} />

      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          {/* Breadcrumb */}
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' / '}
            <Link href="/resources" className="hover:text-foreground">Resources</Link>
            {' / '}
            <span>Barter Valuation Calculator</span>
          </nav>

          {/* Header */}
          <div className="max-w-3xl mb-12">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">
              Barter Valuation Calculator
            </h1>
            <p className="text-xl text-muted-foreground leading-relaxed">
              Determine fair exchange rates when bartering professional services. Select skill
              categories, adjust hourly rates, and instantly see how many hours each party should
              contribute for an equitable trade.
            </p>
          </div>

          {/* Calculator */}
          <div className="max-w-4xl mb-20">
            <BarterCalculator />
          </div>

          {/* How barter valuation works */}
          <section className="max-w-3xl mb-16">
            <h2 className="text-3xl font-bold tracking-tight mb-6">
              How Barter Valuation Works
            </h2>
            <p className="text-muted-foreground leading-relaxed mb-4">
              The IRS treats bartered services identically to cash compensation. Under{' '}
              <strong>IRS Publication 525</strong>, the fair market value (FMV) of services you
              receive through barter must be included in your gross income in the year you receive
              them. This applies whether you barter directly with another person or through a barter
              exchange.
            </p>
            <p className="text-muted-foreground leading-relaxed mb-4">
              <strong>Treasury Regulation &sect; 1.61-2(d)(1)</strong> establishes that when
              services are exchanged, each party must include in income the FMV of the services
              received. The regulation makes no distinction between cash payments and barter;
              both constitute taxable income.
            </p>
            <p className="text-muted-foreground leading-relaxed mb-4">
              <strong>Revenue Ruling 79-24</strong> confirmed that an exchange of services between
              two parties results in taxable income to both, measured by the FMV of the services
              received. For example, when a house painter paints a dentist&rsquo;s home in exchange
              for dental work, both must report the FMV of the services they received.
            </p>
            <p className="text-muted-foreground leading-relaxed">
              <strong>Revenue Ruling 80-52</strong> further clarified that members of barter clubs
              or exchanges must report the FMV of goods or services received through the exchange,
              even if they use credits or other units of account rather than direct swaps. This
              ruling is particularly relevant to platforms like SkillLedger that use a credit system.
            </p>
          </section>

          {/* Four valuation frameworks */}
          <section className="max-w-3xl mb-16">
            <h2 className="text-3xl font-bold tracking-tight mb-6">
              Four Valuation Frameworks
            </h2>
            <p className="text-muted-foreground leading-relaxed mb-8">
              Professionals use four primary approaches to value barter exchanges. Each has
              trade-offs depending on the services involved and the parties&rsquo; priorities.
            </p>

            <div className="space-y-6">
              <div className="card-feature p-6">
                <h3 className="text-lg font-bold mb-2">1. Dollar-for-Dollar</h3>
                <p className="text-muted-foreground leading-relaxed">
                  Each party values their services at their standard market rate, and the exchange
                  balances when the dollar amounts match. A designer charging $100/hour trades one
                  hour for two hours of a writer charging $50/hour. This approach is
                  straightforward but can feel unequal when hourly rates diverge significantly.
                </p>
              </div>

              <div className="card-feature p-6">
                <h3 className="text-lg font-bold mb-2">2. Hour-for-Hour</h3>
                <p className="text-muted-foreground leading-relaxed">
                  Each party trades an equal number of hours regardless of their market rate. One
                  hour of development for one hour of design. This approach prioritizes equality of
                  time but ignores market rate differentials, which may disadvantage higher-rate
                  professionals.
                </p>
              </div>

              <div className="card-feature p-6">
                <h3 className="text-lg font-bold mb-2">3. Value-Based</h3>
                <p className="text-muted-foreground leading-relaxed">
                  Each party values the deliverable rather than the time. A logo package might be
                  traded for a landing page regardless of the hours either party invests. This works
                  well when both parties can clearly define scope but requires careful upfront
                  negotiation.
                </p>
              </div>

              <div className="card-feature p-6">
                <h3 className="text-lg font-bold mb-2">4. Hybrid (Credit-Based)</h3>
                <p className="text-muted-foreground leading-relaxed">
                  A platform-mediated approach where services are priced in credits at each
                  party&rsquo;s chosen rate, and the exchange is balanced through the credit system.
                  This is the model SkillLedger uses. It combines the transparency of
                  dollar-for-dollar with the flexibility of value-based pricing.
                </p>
              </div>
            </div>
          </section>

          {relatedArticles.length > 0 && (
            <section className="max-w-3xl mb-16">
              <h2 className="text-3xl font-bold tracking-tight mb-6">Related Resources</h2>
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
              <p className="text-muted-foreground mt-6">
                Need contracts too? Browse our{' '}
                <Link href="/resources/templates" className="text-primary font-medium hover:underline">
                  free barter contract templates
                </Link>.
              </p>
            </section>
          )}

          <div className="max-w-3xl">
            <FunnelLinks
              stage="tofu"
              comparisons={funnelComparisons}
              howToGuides={funnelHowTo.map((s) => ({ slug: s.slug, title: s.title, skillOffered: s.skillOffered, skillNeeded: s.skillNeeded }))}
            />
            <FunnelCTA stage="tofu" />
          </div>
        </div>
      </div>
    </>
  )
}
