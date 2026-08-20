import { Metadata } from 'next'
import Link from 'next/link'
import { Calculator } from 'lucide-react'
import { buildPublicPageMetadata, generateBreadcrumbSchema, generateItemListSchema, SITE_CONFIG } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import { comparisonsData } from '@/lib/data/comparisons-data'
import { scenariosData } from '@/lib/data/scenarios-data'
import { RelatedHubs } from '@/components/marketing/RelatedHubs'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

const tools = [
  {
    slug: 'barter-valuation-calculator',
    name: 'Barter Valuation Calculator',
    description: 'Calculate the fair market value of your skill exchanges. Enter your hourly rate and project scope to estimate credit values for any barter transaction.',
    icon: Calculator,
  },
]

export const metadata: Metadata = buildPublicPageMetadata(
  'Tools & Calculators',
  'Free tools to help you value, document, and plan professional skill exchanges. Calculate fair barter values and estimate credit rates.',
  '/tools',
  ['barter calculator', 'skill exchange tools', 'credit rate calculator', 'barter valuation tool']
)

export default function ToolsPage() {
  const topComparisons = comparisonsData.slice(0, 3)
  const topHowTo = scenariosData.slice(0, 3)

  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'Tools', url: `${SITE_CONFIG.url}/tools` },
  ])
  const listSchema = generateItemListSchema(
    tools.map((t) => ({
      name: t.name,
      url: `${SITE_CONFIG.url}/tools/${t.slug}`,
      description: t.description,
    })),
    'SkillLedger Tools & Calculators'
  )

  return (
    <>
      <JsonLd schema={breadcrumbSchema} />
      <JsonLd schema={listSchema} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <nav className="text-sm text-muted-foreground mb-8">
            <Link href="/" className="hover:text-foreground">Home</Link>
            {' / '}
            <span>Tools</span>
          </nav>

          <header className="mb-16 text-center">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">Tools & Calculators</h1>
            <p className="text-xl text-muted-foreground max-w-2xl mx-auto leading-relaxed">
              Free tools to help you value, document, and plan your professional skill exchanges.
            </p>
          </header>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8 max-w-4xl mx-auto">
            {tools.map((tool) => {
              const Icon = tool.icon
              return (
                <Link
                  key={tool.slug}
                  href={`/tools/${tool.slug}`}
                  className="card-feature p-8 hover:shadow-lg transition-all duration-200 group text-center"
                >
                  <div className="flex justify-center mb-6">
                    <div className="p-4 bg-gradient-to-br from-primary/20 to-primary/10 rounded-2xl">
                      <Icon className="w-8 h-8 text-primary" />
                    </div>
                  </div>
                  <h2 className="text-xl font-bold group-hover:text-primary transition-colors mb-3">{tool.name}</h2>
                  <p className="text-sm text-muted-foreground leading-relaxed">{tool.description}</p>
                </Link>
              )
            })}
          </div>

          <section className="mt-20 mb-16">
            <h2 className="text-2xl font-bold mb-6">Related Resources</h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              <Link href="/resources/templates" className="card-feature p-5 hover:border-primary/30 transition-colors">
                <h3 className="font-bold text-sm mb-1">Barter Contract Templates</h3>
                <p className="text-xs text-muted-foreground">Free templates for documenting skill exchanges</p>
              </Link>
              <Link href="/glossary/fair-market-value" className="card-feature p-5 hover:border-primary/30 transition-colors">
                <h3 className="font-bold text-sm mb-1">Fair Market Value</h3>
                <p className="text-xs text-muted-foreground">How barter values are determined for IRS compliance</p>
              </Link>
              <Link href="/how-to" className="card-feature p-5 hover:border-primary/30 transition-colors">
                <h3 className="font-bold text-sm mb-1">How-To Guides</h3>
                <p className="text-xs text-muted-foreground">Step-by-step tutorials for common skill exchanges</p>
              </Link>
              <Link href="/categories" className="card-feature p-5 hover:border-primary/30 transition-colors">
                <h3 className="font-bold text-sm mb-1">Skill Categories</h3>
                <p className="text-xs text-muted-foreground">Browse 19 professional skill categories</p>
              </Link>
              <Link href="/glossary/credit-rate" className="card-feature p-5 hover:border-primary/30 transition-colors">
                <h3 className="font-bold text-sm mb-1">Credit Rate</h3>
                <p className="text-xs text-muted-foreground">Understand how credit rates translate to value</p>
              </Link>
              <Link href="/resources" className="card-feature p-5 hover:border-primary/30 transition-colors">
                <h3 className="font-bold text-sm mb-1">Articles</h3>
                <p className="text-xs text-muted-foreground">Guides on valuing, negotiating, and completing exchanges</p>
              </Link>
            </div>
          </section>

          <div className="mt-4">
            <RelatedHubs currentPath="/tools" />
            <FunnelLinks
              stage="tofu"
              comparisons={topComparisons}
              howToGuides={topHowTo.map((s) => ({ slug: s.slug, title: s.title, skillOffered: s.skillOffered, skillNeeded: s.skillNeeded }))}
            />
            <FunnelCTA stage="tofu" />
          </div>
        </div>
      </div>
    </>
  )
}
