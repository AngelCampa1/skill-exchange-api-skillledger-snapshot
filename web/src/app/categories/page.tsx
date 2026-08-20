import { Metadata } from 'next'
import Link from 'next/link'
import { categoriesData } from '@/lib/data/categories-data'
import { comparisonsData } from '@/lib/data/comparisons-data'
import { scenariosData } from '@/lib/data/scenarios-data'
import { buildPublicPageMetadata, generateItemListSchema, generateWebPageSchema, SITE_CONFIG } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import { RelatedHubs } from '@/components/marketing/RelatedHubs'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

export const metadata: Metadata = buildPublicPageMetadata(
  'Skill Categories',
  'Browse all skill categories on SkillLedger. Exchange web development, design, marketing, writing, and 10+ more professional skills.',
  '/categories',
  ['skill categories', 'professional exchange categories']
)

export default function CategoriesPage() {
  const topComparisons = comparisonsData.slice(0, 3)
  const topHowTo = scenariosData.slice(0, 3)

  const schema = generateItemListSchema(
    categoriesData.map((c) => ({
      name: c.name,
      url: `${SITE_CONFIG.url}/categories/${c.slug}`,
      description: c.description,
    })),
    'SkillLedger Skill Categories'
  )

  return (
    <>
      <JsonLd schema={schema} />
      <JsonLd schema={generateWebPageSchema({ name: 'Skill Categories', description: 'Browse all skill categories on SkillLedger. Exchange web development, design, marketing, writing, and 10+ more professional skills.', url: `${SITE_CONFIG.url}/categories` })} />
      <div className="min-h-screen bg-background">
        <div className="container-premium py-16 lg:py-24">
          <header className="mb-16 text-center">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">Skill Categories</h1>
            <p className="text-xl text-muted-foreground max-w-2xl mx-auto">
              Browse professional skills available for exchange on SkillLedger. Find your category and start trading today.
            </p>
          </header>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {categoriesData.map((cat) => (
              <Link
                key={cat.slug}
                href={`/categories/${cat.slug}`}
                className="card-feature p-6 hover:shadow-lg transition-all duration-200 group"
              >
                <div className="flex items-start justify-between mb-3">
                  <h2 className="text-lg font-bold group-hover:text-primary transition-colors">{cat.name}</h2>
                  <span className={`text-xs px-2 py-1 rounded-full ${cat.demandLevel === 'high' ? 'bg-green-100 text-green-700' : cat.demandLevel === 'medium' ? 'bg-yellow-100 text-yellow-700' : 'bg-gray-100 text-gray-600'}`}>
                    {cat.demandLevel} demand
                  </span>
                </div>
                <p className="text-sm text-muted-foreground mb-4 leading-relaxed">{cat.description}</p>
                <div className="flex flex-wrap gap-1">
                  {cat.sampleSkills.slice(0, 3).map((skill) => (
                    <span key={skill} className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded">{skill}</span>
                  ))}
                </div>
              </Link>
            ))}
          </div>

          <div className="mt-16">
            <RelatedHubs currentPath="/categories" />
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
