import type { Metadata } from 'next'
import Link from 'next/link'
import { buildPublicPageMetadata, generateBreadcrumbSchema, generateWebPageSchema, generateFAQSchema, SITE_CONFIG } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import { featuresData } from '@/lib/data/features-data'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

export const metadata: Metadata = buildPublicPageMetadata(
  'About SkillLedger',
  'Trade skills across 19 categories in 50 cities with escrow protection and 1099-B compliance. Learn how 40+ guides help professionals exchange services.',
  '/about',
  ['about skillledger', 'skill exchange platform', 'professional barter exchange']
)

const stats = [
  { label: 'Skill Categories', value: '19' },
  { label: 'Resource Articles', value: '40+' },
  { label: 'US Cities Covered', value: '50' },
  { label: 'Glossary Terms', value: '48' },
]

const values = [
  {
    title: 'Trust',
    description:
      'Every exchange is protected by escrow, verified reviews, and reputation scores. Both parties commit before work begins, and credits release only when both confirm satisfaction.',
  },
  {
    title: 'Fairness',
    description:
      'Credits map to fair market value, so professionals at every rate level receive equitable exchanges. No one subsidizes the other. The math balances transparently.',
  },
  {
    title: 'Community',
    description:
      'SkillLedger connects professionals across 19 skill categories. The network effect means more members create more exchange opportunities for everyone.',
  },
  {
    title: 'Transparency',
    description:
      'Credit rates, reputation scores, and exchange terms are visible to both parties. No hidden fees, no opaque algorithms, no surprises after you commit.',
  },
]

const aboutFaqs = [
  {
    question: 'Who is SkillLedger for?',
    answer:
      'SkillLedger is built for freelancers, consultants, and small businesses who want to exchange professional services without cash. Whether you are a developer who needs design work or a marketer who needs legal review, skill exchange lets you access expertise by offering your own.',
  },
  {
    question: 'Is skill exchange legal and taxable?',
    answer:
      'Yes. The IRS treats barter exchanges as taxable income at fair market value under Revenue Ruling 79-24. SkillLedger automatically tracks FMV for every exchange and generates 1099-B-ready documentation to simplify year-end tax reporting.',
  },
  {
    question: 'How does SkillLedger protect both parties in an exchange?',
    answer:
      'Every exchange on SkillLedger is protected by built-in escrow. Credits are held until both parties confirm the work is complete and satisfactory. If a dispute arises, our structured resolution process with neutral mediators ensures a fair outcome.',
  },
  {
    question: 'How is SkillLedger different from Fiverr or Upwork?',
    answer:
      'Fiverr and Upwork are cash marketplaces that charge 10-27% in fees. SkillLedger is a credit-based exchange platform. You trade skills directly with no commissions, keep the full value of your work, and access services without cash outlay.',
  },
]

const steps = [
  {
    step: '01',
    title: 'Post Your Skills',
    description:
      'Create a profile listing your professional skills and set your credit rate based on your market value. Browse the marketplace to see what others offer.',
  },
  {
    step: '02',
    title: 'Exchange Services',
    description:
      'Find professionals who need your skills and offer what you need in return. Propose exchanges with clear scope, timelines, and credit terms. Escrow protects both sides.',
  },
  {
    step: '03',
    title: 'Build Reputation',
    description:
      'Complete exchanges, leave reviews, and build a verified track record. Your reputation score unlocks better opportunities and signals reliability to future partners.',
  },
]

export default function AboutPage() {
  const funnelFeatures = featuresData.slice(0, 3)

  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'About', url: `${SITE_CONFIG.url}/about` },
  ])

  const webPageSchema = generateWebPageSchema({
    name: 'About SkillLedger',
    description: 'SkillLedger is a professional collaboration platform where freelancers and businesses exchange skills using a credit-based barter system.',
    url: `${SITE_CONFIG.url}/about`,
  })

  const faqSchema = generateFAQSchema(aboutFaqs)

  return (
    <>
      <JsonLd schema={breadcrumbSchema} />
      <JsonLd schema={webPageSchema} />
      <JsonLd schema={faqSchema} />

      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          {/* Breadcrumb */}
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' / '}
            <span>About</span>
          </nav>

          {/* Hero */}
          <header className="max-w-3xl mb-20">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">
              Professional Services Should Be Accessible to Every Professional
            </h1>
            <p className="text-xl text-muted-foreground leading-relaxed mb-4">
              SkillLedger is a platform for professionals to exchange skills and services
              without cash. Earn credits by providing your expertise, then spend credits to
              access the services you need, from web development and design to legal review
              and marketing strategy.
            </p>
            <p className="text-lg text-muted-foreground leading-relaxed">
              We built SkillLedger because too many talented professionals are held back by
              cash flow, not capability. A developer who needs a brand identity should not
              wait until they can afford an agency. A designer who needs legal review should
              not skip it because attorneys are expensive. Skill exchange unlocks professional
              growth that cash constraints block.
            </p>
          </header>

          {/* How It Works */}
          <section className="mb-20">
            <h2 className="text-3xl font-bold tracking-tight mb-10">How It Works</h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
              {steps.map((item) => (
                <div key={item.step} className="card-feature p-8">
                  <div className="text-4xl font-black text-primary/20 mb-4">{item.step}</div>
                  <h3 className="text-xl font-bold mb-3">{item.title}</h3>
                  <p className="text-muted-foreground leading-relaxed">{item.description}</p>
                </div>
              ))}
            </div>
          </section>

          {/* Values */}
          <section className="mb-20">
            <h2 className="text-3xl font-bold tracking-tight mb-10">What We Stand For</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {values.map((value) => (
                <div key={value.title} className="card-feature p-8">
                  <h3 className="text-xl font-bold mb-3">{value.title}</h3>
                  <p className="text-muted-foreground leading-relaxed">{value.description}</p>
                </div>
              ))}
            </div>
          </section>

          {/* By the Numbers */}
          <section className="mb-20">
            <h2 className="text-3xl font-bold tracking-tight mb-10">By the Numbers</h2>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
              {stats.map((stat) => (
                <div key={stat.label} className="text-center card-feature p-8">
                  <div className="text-4xl font-black text-primary mb-2">{stat.value}</div>
                  <div className="text-sm text-muted-foreground font-medium">{stat.label}</div>
                </div>
              ))}
            </div>
          </section>

          {/* Explore */}
          <section className="mb-20 max-w-3xl">
            <h2 className="text-3xl font-bold tracking-tight mb-6">Explore the Platform</h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Link href="/categories" className="card-feature p-5 hover:shadow-lg transition-all group">
                <h3 className="font-bold group-hover:text-primary transition-colors">Skill Categories</h3>
                <p className="text-sm text-muted-foreground">Browse 19 professional skill categories</p>
              </Link>
              <Link href="/resources" className="card-feature p-5 hover:shadow-lg transition-all group">
                <h3 className="font-bold group-hover:text-primary transition-colors">Resources</h3>
                <p className="text-sm text-muted-foreground">Guides, templates, and tools for skill exchange</p>
              </Link>
              <Link href="/how-to" className="card-feature p-5 hover:shadow-lg transition-all group">
                <h3 className="font-bold group-hover:text-primary transition-colors">How-To Guides</h3>
                <p className="text-sm text-muted-foreground">Step-by-step exchange scenarios</p>
              </Link>
              <Link href="/pricing" className="card-feature p-5 hover:shadow-lg transition-all group">
                <h3 className="font-bold group-hover:text-primary transition-colors">Pricing</h3>
                <p className="text-sm text-muted-foreground">Free and Premium plans</p>
              </Link>
            </div>
          </section>

          {/* FAQ */}
          <section className="mb-20 max-w-3xl">
            <h2 className="text-3xl font-bold tracking-tight mb-10">
              Frequently Asked Questions
            </h2>
            <div className="space-y-6">
              {aboutFaqs.map((faq) => (
                <div key={faq.question} className="border border-border rounded-xl p-6">
                  <h3 className="font-bold mb-3">{faq.question}</h3>
                  <p className="text-muted-foreground leading-relaxed">{faq.answer}</p>
                </div>
              ))}
            </div>
          </section>

          <FunnelLinks stage="mofu" features={funnelFeatures} />
          <FunnelCTA stage="mofu" />
        </div>
      </div>
    </>
  )
}
