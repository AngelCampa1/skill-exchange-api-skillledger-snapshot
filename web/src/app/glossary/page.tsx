import { Metadata } from 'next'
import Link from 'next/link'
import { getTermsByFirstLetter } from '@/lib/data/glossary-data'
import { buildPublicPageMetadata, generateItemListSchema, generateWebPageSchema, SITE_CONFIG } from '@/lib/seo'
import { glossaryData } from '@/lib/data/glossary-data'
import { comparisonsData } from '@/lib/data/comparisons-data'
import { scenariosData } from '@/lib/data/scenarios-data'
import { JsonLd } from '@/components/marketing/JsonLd'
import { RelatedHubs } from '@/components/marketing/RelatedHubs'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

export const metadata: Metadata = buildPublicPageMetadata(
  'Skill Exchange Glossary',
  'Definitions for professional skill exchange, barter economy, and SkillLedger platform terms. Learn the language of professional service trading.',
  '/glossary',
  ['skill barter glossary', 'barter economy terms', 'professional exchange definitions']
)

export default function GlossaryPage() {
  const topComparisons = comparisonsData.slice(0, 3)
  const topHowTo = scenariosData.slice(0, 3)
  const grouped = getTermsByFirstLetter()
  const letters = Object.keys(grouped).sort()

  const schema = generateItemListSchema(
    glossaryData.map((t) => ({
      name: t.term,
      url: `${SITE_CONFIG.url}/glossary/${t.slug}`,
      description: t.definition.length > 150 ? t.definition.slice(0, 150) + '...' : t.definition,
    })),
    'SkillLedger Skill Exchange Glossary'
  )

  return (
    <>
      <JsonLd schema={schema} />
      <JsonLd schema={generateWebPageSchema({ name: 'Skill Exchange Glossary', description: 'Definitions for professional skill exchange, barter economy, and SkillLedger platform terms.', url: `${SITE_CONFIG.url}/glossary` })} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <header className="mb-16 text-center">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">Skill Exchange Glossary</h1>
            <p className="text-xl text-muted-foreground max-w-2xl mx-auto">
              Definitions for professional skill exchange, barter economy, and SkillLedger platform terms.
            </p>
          </header>

          <nav className="mb-12 flex flex-wrap gap-2 justify-center">
            {letters.map((letter) => (
              <a
                key={letter}
                href={`#${letter}`}
                className="w-9 h-9 flex items-center justify-center rounded-lg bg-primary/10 text-primary font-bold text-sm hover:bg-primary hover:text-primary-foreground transition-colors"
              >
                {letter}
              </a>
            ))}
          </nav>

          <div className="space-y-12">
            {letters.map((letter) => (
              <section key={letter} id={letter}>
                <h2 className="text-2xl font-black mb-6 border-b border-border pb-3">{letter}</h2>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {grouped[letter].map((term) => (
                    <Link
                      key={term.slug}
                      href={`/glossary/${term.slug}`}
                      className="block p-5 border border-border rounded-xl hover:border-primary hover:shadow-sm transition-all group"
                    >
                      <h3 className="font-bold mb-2 group-hover:text-primary transition-colors">{term.term}</h3>
                      <p className="text-sm text-muted-foreground leading-relaxed line-clamp-2">{term.definition}</p>
                    </Link>
                  ))}
                </div>
              </section>
            ))}
          </div>

          <div className="mt-16">
            <RelatedHubs currentPath="/glossary" />
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
