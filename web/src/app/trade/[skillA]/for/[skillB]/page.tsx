import { notFound } from 'next/navigation'
import type { Metadata } from 'next'
import Link from 'next/link'
import { categoriesData, getCategoryBySlug } from '@/lib/data/categories-data'
import { scenariosData } from '@/lib/data/scenarios-data'
import { findArticlesForCategory, findComparisonsForCategory } from '@/lib/cross-links'
import {
  buildPublicPageMetadata,
  generateServiceSchema,
  generateFAQSchema,
  generateBreadcrumbSchema,
  SITE_CONFIG,
} from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

export const dynamicParams = false

interface Props {
  params: Promise<{ skillA: string; skillB: string }>
}

export async function generateStaticParams() {
  return categoriesData.flatMap((a) =>
    categoriesData
      .filter((b) => b.slug !== a.slug)
      .map((b) => ({ skillA: a.slug, skillB: b.slug }))
  )
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { skillA, skillB } = await params
  const catA = getCategoryBySlug(skillA)
  const catB = getCategoryBySlug(skillB)
  if (!catA || !catB) return {}
  return buildPublicPageMetadata(
    `Trade ${catA.name} for ${catB.name} Skills | Free Exchange`,
    `Exchange ${catA.name.toLowerCase()} skills for ${catB.name.toLowerCase()} services on SkillLedger. ${catA.averageCreditRate} vs ${catB.averageCreditRate} credits/hr. See how the rates compare and join free.`,
    `/trade/${skillA}/for/${skillB}`,
    [catA.name, catB.name, 'skill exchange', 'barter']
  )
}

export default async function TradeSkillsPage({ params }: Props) {
  const { skillA, skillB } = await params
  const catA = getCategoryBySlug(skillA)
  const catB = getCategoryBySlug(skillB)
  if (!catA || !catB) notFound()

  const rateDiff = catA.averageCreditRate - catB.averageCreditRate
  const safeRateA = catA.averageCreditRate > 0 ? catA.averageCreditRate : 1
  const hoursNeeded = Math.ceil((catB.averageCreditRate / safeRateA) * 10) / 10
  const rateNote =
    rateDiff === 0
      ? `Both skills trade at the same average rate of ${catA.averageCreditRate} credits/hr. That makes this a straightforward 1:1 hour exchange.`
      : rateDiff > 0
        ? `${catA.name} averages ${catA.averageCreditRate} credits/hr, while ${catB.name} sits at ${catB.averageCreditRate} credits/hr. Because ${catA.name} earns more per hour, your credits go further when you hire ${catB.name} work.`
        : `${catB.name} averages ${catB.averageCreditRate} credits/hr compared to ${catA.averageCreditRate} credits/hr for ${catA.name}. The ${Math.abs(rateDiff)}-credit gap means you will need to put in more hours of ${catA.name} work than you receive in ${catB.name} time. Keep this in mind when scoping the project.`

  const faqs = [
    {
      question: `How many ${catA.name} credits equal one hour of ${catB.name} work?`,
      answer: `${rateDiff === 0 ? 'The rates are equal, so one hour of work trades for one hour.' : `About ${hoursNeeded} hours of ${catA.name} work funds one hour of ${catB.name}.`} At standard rates, 1 hour of ${catA.name} earns ~${catA.averageCreditRate} credits and 1 hour of ${catB.name} costs ~${catB.averageCreditRate} credits. Both parties set their own rates, so the final ratio depends on what you negotiate.`,
    },
    {
      question: `What should I include in a ${catA.name}-for-${catB.name} exchange agreement?`,
      answer: `Cover four things: (1) deliverables with clear acceptance criteria for each side, (2) credit amounts and payment milestones, (3) timeline and number of revision rounds, (4) what happens if either party cannot complete their work. SkillLedger's escrow holds credits until both parties confirm completion.`,
    },
    {
      question: `Is it common to trade ${catA.name} for ${catB.name} on SkillLedger?`,
      answer: `${catA.demandLevel === 'high' && catB.demandLevel === 'high' ? `Yes. Both ${catA.name} and ${catB.name} are high-demand categories, and this pairing sees regular activity.` : `${catA.name} and ${catB.name} are both established categories on SkillLedger. Professionals in these fields frequently need exactly what the other offers, which makes it a practical exchange.`}`,
    },
  ]

  const steps = [
    { title: 'List your skill', description: `Describe your ${catA.name} services, set a credit rate, and note your availability.` },
    { title: 'Search for a match', description: `Browse ${catB.name} providers or use the match tool to find people who need ${catA.name} help.` },
    { title: 'Lock in the details', description: 'Set deliverables, credit amounts, and a timeline. Both sides confirm before any work starts.' },
    { title: 'Work under escrow', description: 'Credits stay in escrow while work is in progress. They release once both parties mark the project complete.' },
    { title: 'Review each other', description: 'Rate the exchange so future partners can see your track record.' },
  ]

  const matchingScenario = scenariosData.find(
    (s) => s.slug === `${skillA}-for-${skillB}` || s.slug === `${skillB}-for-${skillA}`
  )

  const relatedArticles = [
    ...findArticlesForCategory(catA.slug),
    ...findArticlesForCategory(catB.slug),
  ].filter((a, i, arr) => arr.findIndex((b) => b.slug === a.slug) === i).slice(0, 4)

  // Related pairings: other categories that pair well with catA
  const relatedPairings = categoriesData
    .filter((c) => c.slug !== catA.slug && c.slug !== catB.slug)
    .slice(0, 4)

  // Comparison links: from both category slugs, deduplicated, max 2
  const tradeComparisons = [
    ...findComparisonsForCategory(catA.slug),
    ...findComparisonsForCategory(catB.slug),
  ].filter((c, i, self) => self.findIndex((d) => d.slug === c.slug) === i).slice(0, 2)

  const serviceSchema = generateServiceSchema({
    name: `Trade ${catA.name} for ${catB.name}`,
    serviceType: 'Professional Skill Exchange',
  })
  const faqSchema = generateFAQSchema(faqs)
  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'Trade', url: `${SITE_CONFIG.url}/trade` },
    { name: `${catA.name} for ${catB.name}`, url: `${SITE_CONFIG.url}/trade/${skillA}/for/${skillB}` },
  ])

  return (
    <>
      <JsonLd schema={serviceSchema} />
      <JsonLd schema={faqSchema} />
      <JsonLd schema={breadcrumbSchema} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' / '}
            <Link href="/trade" className="hover:text-foreground">Trade</Link>
            {' / '}
            <span>{catA.name} for {catB.name}</span>
          </nav>

          <header className="mb-16 max-w-3xl">
            <div className="flex items-center gap-3 mb-4">
              <span className="text-sm px-3 py-1 rounded-full font-medium bg-primary/10 text-primary">
                {catA.averageCreditRate} credits/hr
              </span>
              <span className="text-sm text-muted-foreground">↔</span>
              <span className="text-sm px-3 py-1 rounded-full font-medium bg-primary/10 text-primary">
                {catB.averageCreditRate} credits/hr
              </span>
            </div>
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">
              Trade {catA.name} for {catB.name}
            </h1>
            <p className="text-xl text-muted-foreground leading-relaxed mb-8">
              Put your {catA.name.toLowerCase()} skills to work and get {catB.name.toLowerCase()} services in return. No cash changes hands. SkillLedger handles the credits and escrow so both sides deliver.
            </p>
            <Link href="/register" className="btn-primary">Start This Exchange</Link>
          </header>

          {matchingScenario && (
            <div className="rounded-lg border border-primary/20 bg-primary/5 p-4 mb-8">
              <p className="text-sm">
                <span className="font-semibold">Step-by-step guide available:</span>{' '}
                <Link href={`/how-to/${matchingScenario.slug}`} className="text-primary hover:underline">
                  How to Exchange {catA.name} for {catB.name}
                </Link>
              </p>
            </div>
          )}

          <section className="mb-16 max-w-3xl">
            <h2 className="text-2xl font-bold mb-6">Credit Rate Comparison</h2>
            <div className="grid grid-cols-2 gap-4 mb-6">
              <div className="border border-border rounded-xl p-6 text-center">
                <div className="text-3xl font-black text-primary mb-1">{catA.averageCreditRate}</div>
                <div className="text-sm font-medium mb-1">{catA.name}</div>
                <div className="text-xs text-muted-foreground">credits / hr average</div>
              </div>
              <div className="border border-border rounded-xl p-6 text-center">
                <div className="text-3xl font-black text-primary mb-1">{catB.averageCreditRate}</div>
                <div className="text-sm font-medium mb-1">{catB.name}</div>
                <div className="text-xs text-muted-foreground">credits / hr average</div>
              </div>
            </div>
            <p className="text-muted-foreground leading-relaxed">{rateNote}</p>
          </section>

          {/* What You Can Trade */}
          <section className="grid md:grid-cols-2 gap-6 mb-12">
            <div>
              <h3 className="text-lg font-bold mb-3">{catA.name} Services You Can Offer</h3>
              <ul className="space-y-1.5">
                {catA.sampleSkills.slice(0, 6).map((skill) => (
                  <li key={skill} className="text-sm text-muted-foreground flex items-center gap-2">
                    <span className="w-1.5 h-1.5 rounded-full bg-primary shrink-0" />
                    {skill}
                  </li>
                ))}
              </ul>
              <Link href={`/categories/${catA.slug}`} className="text-sm text-primary hover:underline mt-2 inline-block">
                View all {catA.name} skills
              </Link>
            </div>
            <div>
              <h3 className="text-lg font-bold mb-3">{catB.name} Services You Can Get</h3>
              <ul className="space-y-1.5">
                {catB.sampleSkills.slice(0, 6).map((skill) => (
                  <li key={skill} className="text-sm text-muted-foreground flex items-center gap-2">
                    <span className="w-1.5 h-1.5 rounded-full bg-secondary shrink-0" />
                    {skill}
                  </li>
                ))}
              </ul>
              <Link href={`/categories/${catB.slug}`} className="text-sm text-primary hover:underline mt-2 inline-block">
                View all {catB.name} skills
              </Link>
            </div>
          </section>

          <section className="mb-16">
            <h2 className="text-2xl font-bold mb-8">How This Exchange Works</h2>
            <div className="space-y-4 max-w-3xl">
              {steps.map((step, i) => (
                <div key={i} className="flex gap-4 border border-border rounded-xl p-5">
                  <div className="flex-shrink-0 w-8 h-8 rounded-full bg-primary/10 text-primary flex items-center justify-center font-bold text-sm">
                    {i + 1}
                  </div>
                  <div>
                    <h3 className="font-bold mb-1">{step.title}</h3>
                    <p className="text-sm text-muted-foreground">{step.description}</p>
                  </div>
                </div>
              ))}
            </div>
          </section>

          <section className="mb-16 max-w-3xl">
            <h2 className="text-2xl font-bold mb-8">Frequently Asked Questions</h2>
            <div className="space-y-6">
              {faqs.map((faq, i) => (
                <div key={i} className="border border-border rounded-xl p-6">
                  <h3 className="font-bold mb-3">{faq.question}</h3>
                  <p className="text-muted-foreground leading-relaxed">{faq.answer}</p>
                </div>
              ))}
            </div>
          </section>

          <FunnelLinks
            stage="tofu"
            comparisons={tradeComparisons.map((c) => ({ slug: c.slug, title: c.title, description: c.description }))}
            howToGuides={matchingScenario ? [{ slug: matchingScenario.slug, title: matchingScenario.title, skillOffered: matchingScenario.skillOffered, skillNeeded: matchingScenario.skillNeeded }] : []}
          />

          {relatedArticles.length > 0 && (
            <section className="mb-12">
              <h2 className="text-xl font-bold mb-4">Related Articles</h2>
              <div className="grid sm:grid-cols-2 gap-4">
                {relatedArticles.map((article) => (
                  <Link key={article.slug} href={`/resources/${article.slug}`} className="card-feature p-4 hover:border-primary/30 transition-colors">
                    <h3 className="font-semibold text-sm mb-1">{article.frontmatter.title}</h3>
                    <p className="text-xs text-muted-foreground line-clamp-2">{article.frontmatter.description}</p>
                  </Link>
                ))}
              </div>
            </section>
          )}

          <section className="mb-16">
            <h2 className="text-2xl font-bold mb-6">Other {catA.name} Exchange Pairings</h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {relatedPairings.map((cat) => (
                <Link
                  key={cat.slug}
                  href={`/trade/${skillA}/for/${cat.slug}`}
                  className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
                >
                  <h3 className="font-bold group-hover:text-primary transition-colors mb-1">
                    {catA.name} for {cat.name}
                  </h3>
                  <p className="text-sm text-muted-foreground">~{cat.averageCreditRate} credits/hr</p>
                </Link>
              ))}
            </div>
          </section>

          <FunnelCTA stage="tofu" />
        </div>
      </div>
    </>
  )
}
