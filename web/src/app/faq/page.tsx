import type { Metadata } from 'next'
import Link from 'next/link'
import { buildPublicPageMetadata, generateBreadcrumbSchema, generateFAQSchema, SITE_CONFIG } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import { faqSections, getAllFAQs } from '@/lib/data/faq-data'
import { featuresData } from '@/lib/data/features-data'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

export const metadata: Metadata = buildPublicPageMetadata(
  'Frequently Asked Questions',
  'Find answers to common questions about SkillLedger credits, exchanges, escrow protection, pricing, tax obligations, and how the platform works.',
  '/faq',
  ['skillledger faq', 'skill exchange questions', 'barter platform help', 'credit exchange faq']
)

export default function FAQPage() {
  const funnelFeatures = featuresData.slice(0, 3)

  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'FAQ', url: `${SITE_CONFIG.url}/faq` },
  ])

  const faqSchema = generateFAQSchema(getAllFAQs())

  return (
    <>
      <JsonLd schema={breadcrumbSchema} />
      <JsonLd schema={faqSchema} />

      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          {/* Breadcrumb */}
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' / '}
            <span>FAQ</span>
          </nav>

          {/* Header */}
          <div className="text-center max-w-3xl mx-auto mb-16">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">
              Frequently Asked Questions
            </h1>
            <p className="text-xl text-muted-foreground leading-relaxed">
              Everything you need to know about exchanging professional services on SkillLedger.
              Can&apos;t find your answer? <Link href="/register" className="text-primary hover:text-primary/80 font-medium">Start a free trial</Link> and
              reach out to our support team.
            </p>
          </div>

          {/* Quick Nav */}
          <nav className="max-w-3xl mx-auto mb-12">
            <div className="flex flex-wrap gap-2 justify-center">
              {faqSections.map((section) => (
                <a
                  key={section.id}
                  href={`#${section.id}`}
                  className="text-sm px-4 py-2 rounded-full border border-border hover:border-primary hover:text-primary transition-colors"
                >
                  {section.title}
                </a>
              ))}
            </div>
          </nav>

          {/* FAQ Sections */}
          <div className="max-w-3xl mx-auto space-y-16">
            {faqSections.map((section) => (
              <section key={section.id} id={section.id}>
                <h2 className="text-2xl font-bold tracking-tight mb-6">
                  {section.title}
                </h2>
                <div className="space-y-4">
                  {section.faqs.map((faq) => (
                    <div
                      key={faq.question}
                      className="border border-border rounded-xl p-6"
                    >
                      <h3 className="font-bold mb-3">{faq.question}</h3>
                      <p className="text-muted-foreground leading-relaxed">
                        {faq.answer}
                      </p>
                    </div>
                  ))}
                </div>
              </section>
            ))}
          </div>

          {/* Related Links */}
          <section className="max-w-3xl mx-auto mt-16 mb-16">
            <h2 className="text-2xl font-bold tracking-tight text-center mb-8">
              Learn More
            </h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              <Link href="/resources" className="card-feature p-5 text-center hover:border-primary/30 transition-colors">
                <h3 className="font-bold text-sm mb-1">Articles</h3>
                <p className="text-xs text-muted-foreground">Guides and insights for professionals</p>
              </Link>
              <Link href="/how-to" className="card-feature p-5 text-center hover:border-primary/30 transition-colors">
                <h3 className="font-bold text-sm mb-1">How-To Guides</h3>
                <p className="text-xs text-muted-foreground">Step-by-step exchange tutorials</p>
              </Link>
              <Link href="/glossary" className="card-feature p-5 text-center hover:border-primary/30 transition-colors">
                <h3 className="font-bold text-sm mb-1">Glossary</h3>
                <p className="text-xs text-muted-foreground">Key terms and definitions</p>
              </Link>
              <Link href="/compare" className="card-feature p-5 text-center hover:border-primary/30 transition-colors">
                <h3 className="font-bold text-sm mb-1">Platform Comparisons</h3>
                <p className="text-xs text-muted-foreground">See how SkillLedger compares</p>
              </Link>
              <Link href="/pricing" className="card-feature p-5 text-center hover:border-primary/30 transition-colors">
                <h3 className="font-bold text-sm mb-1">Pricing</h3>
                <p className="text-xs text-muted-foreground">Free and Premium plan details</p>
              </Link>
              <Link href="/categories" className="card-feature p-5 text-center hover:border-primary/30 transition-colors">
                <h3 className="font-bold text-sm mb-1">Skill Categories</h3>
                <p className="text-xs text-muted-foreground">Browse 19 professional categories</p>
              </Link>
            </div>
          </section>

          <FunnelLinks stage="mofu" features={funnelFeatures} />
          <FunnelCTA stage="mofu" />
        </div>
      </div>
    </>
  )
}
