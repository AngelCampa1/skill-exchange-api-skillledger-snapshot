import type { Metadata } from 'next'
import Link from 'next/link'
import { buildPublicPageMetadata, generateBreadcrumbSchema, generateFAQSchema, SITE_CONFIG } from '@/lib/seo'
import { TrustBadges } from '@/components/TrustBadges'
import { PricingCards } from '@/components/PricingCards'
import { JsonLd } from '@/components/marketing/JsonLd'
import type { SubscriptionTier } from '@/types/subscription'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

export const metadata: Metadata = buildPublicPageMetadata(
  'Pricing',
  'Start your 30-day free trial. Professional at $19/mo, Business at $49/mo, Enterprise at $99/mo. Credit card required. Cancel anytime.',
  '/pricing',
  ['skillledger pricing', 'skill exchange platform cost', 'barter platform pricing', 'professional service exchange price']
)

const faqs = [
  {
    question: 'Do I need a credit card to sign up?',
    answer:
      'Yes. We collect your card upfront to activate your 30-day free trial. You will not be charged until the trial ends. Cancel before the trial is up and you owe nothing.',
  },
  {
    question: 'What payment methods do you accept?',
    answer:
      'We accept all major credit cards, debit cards, and ACH bank transfers through Stripe. All payments are processed securely with PCI-compliant encryption.',
  },
  {
    question: 'Can I cancel my subscription anytime?',
    answer:
      'Yes. Cancel anytime from your account settings. Your access remains active through the end of your current billing period. No cancellation fees and no long-term contracts.',
  },
  {
    question: 'What happens after my 30-day trial?',
    answer:
      'Your subscription renews automatically at the plan rate you chose. You will receive an email reminder a few days before your trial ends. Cancel anytime before renewal if the plan is not right for you.',
  },
  {
    question: 'Can I switch plans after signing up?',
    answer:
      'Yes. Upgrade or downgrade at any time. Upgrades take effect immediately with prorated billing. Downgrades take effect at the start of the next billing cycle.',
  },
]

async function getSubscriptionTiers(): Promise<SubscriptionTier[]> {
  try {
    const baseUrl = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:8030'
    const res = await fetch(`${baseUrl}/api/subscriptiontier`, {
      next: { revalidate: 3600 },
    })
    if (!res.ok) return []
    return res.json()
  } catch {
    return []
  }
}

export default async function PricingPage() {
  const tiers = await getSubscriptionTiers()

  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'Pricing', url: `${SITE_CONFIG.url}/pricing` },
  ])

  const faqSchema = generateFAQSchema(faqs)

  const offerSchema = tiers.map((tier) => ({
    '@context': 'https://schema.org',
    '@type': 'Offer',
    name: `SkillLedger ${tier.name}`,
    description: tier.description ?? `${tier.name} plan for SkillLedger professional skill exchange`,
    price: String(tier.price),
    priceCurrency: 'USD',
    priceSpecification: {
      '@type': 'UnitPriceSpecification',
      price: String(tier.price),
      priceCurrency: 'USD',
      billingDuration: 'P1M',
    },
    url: `${SITE_CONFIG.url}/register`,
    eligibleRegion: { '@type': 'Country', name: 'US' },
    availability: 'https://schema.org/InStock',
    validFrom: '2026-01-01',
  }))

  return (
    <>
      <JsonLd schema={breadcrumbSchema} />
      <JsonLd schema={faqSchema} />
      <JsonLd schema={offerSchema} />

      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          {/* Breadcrumb */}
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' / '}
            <span>Pricing</span>
          </nav>

          {/* Header */}
          <div className="text-center max-w-3xl mx-auto mb-16">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">
              Simple, Transparent Pricing
            </h1>
            <p className="text-xl text-muted-foreground leading-relaxed">
              Every plan includes a 30-day free trial. Your card is required to start — you
              won&apos;t be charged until your trial ends.
            </p>
          </div>

          {/* Trust Badges */}
          <div className="mb-16">
            <TrustBadges />
          </div>

          {/* Pricing Cards */}
          <div className="mb-20">
            <PricingCards tiers={tiers} />
          </div>

          {/* FAQ */}
          <section className="max-w-3xl mx-auto mb-20">
            <h2 className="text-3xl font-bold tracking-tight text-center mb-10">
              Frequently Asked Questions
            </h2>
            <div className="space-y-6">
              {faqs.map((faq) => (
                <div key={faq.question} className="border border-border rounded-xl p-6">
                  <h3 className="font-bold mb-3">{faq.question}</h3>
                  <p className="text-muted-foreground leading-relaxed">{faq.answer}</p>
                </div>
              ))}
            </div>
          </section>

          <FunnelCTA stage="bofu" />
        </div>
      </div>
    </>
  )
}
