import { Metadata } from 'next'
import Link from 'next/link'
import SkillMatchQuiz from '@/components/SkillMatchQuiz'
import { buildMetadata } from '@/lib/seo'
import { generateBreadcrumbSchema, SITE_CONFIG } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import { comparisonsData } from '@/lib/data/comparisons-data'
import { scenariosData } from '@/lib/data/scenarios-data'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

export const metadata: Metadata = buildMetadata({
  title: 'Find Your Skill Match',
  description:
    'Take a quick 3-question quiz to discover the best skill exchange categories and how-to scenarios for your profession. Find your perfect match on SkillLedger.',
  path: '/skill-match',
})

export default function SkillMatchPage() {
  const funnelComparisons = comparisonsData.slice(0, 3)
  const funnelHowTo = scenariosData.slice(0, 3)

  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'Find Your Skill Match', url: `${SITE_CONFIG.url}/skill-match` },
  ])

  return (
    <>
      <JsonLd schema={breadcrumbSchema} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <div className="text-center mb-12">
            <h1 className="text-3xl lg:text-4xl font-black tracking-tight mb-4">
              <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                Find Your Skill Match
              </span>
            </h1>
            <p className="text-lg text-muted-foreground max-w-xl mx-auto">
              Answer 3 quick questions and we will show you the best skill exchange opportunities for your profession.
            </p>
          </div>
          <SkillMatchQuiz />

          <section className="mt-20 max-w-3xl mx-auto">
            <h2 className="text-xl font-bold mb-6 text-center">Explore More</h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              <Link href="/categories" className="card-feature p-5 hover:border-primary/30 transition-colors text-center">
                <h3 className="font-bold text-sm mb-1">Skill Categories</h3>
                <p className="text-xs text-muted-foreground">Browse all 19 professional categories</p>
              </Link>
              <Link href="/how-to" className="card-feature p-5 hover:border-primary/30 transition-colors text-center">
                <h3 className="font-bold text-sm mb-1">How-To Guides</h3>
                <p className="text-xs text-muted-foreground">Step-by-step exchange tutorials</p>
              </Link>
              <Link href="/industries" className="card-feature p-5 hover:border-primary/30 transition-colors text-center">
                <h3 className="font-bold text-sm mb-1">Industries</h3>
                <p className="text-xs text-muted-foreground">Skill exchange by profession</p>
              </Link>
              <Link href="/trade" className="card-feature p-5 hover:border-primary/30 transition-colors text-center">
                <h3 className="font-bold text-sm mb-1">Trade Pairings</h3>
                <p className="text-xs text-muted-foreground">Find what your skills are worth</p>
              </Link>
              <Link href="/tools/barter-valuation-calculator" className="card-feature p-5 hover:border-primary/30 transition-colors text-center">
                <h3 className="font-bold text-sm mb-1">Valuation Calculator</h3>
                <p className="text-xs text-muted-foreground">Calculate fair exchange rates</p>
              </Link>
              <Link href="/register" className="card-feature p-5 hover:border-primary/30 transition-colors text-center">
                <h3 className="font-bold text-sm mb-1">Get Started</h3>
                <p className="text-xs text-muted-foreground">Create a free account today</p>
              </Link>
            </div>
          </section>

          <div className="mt-12">
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
