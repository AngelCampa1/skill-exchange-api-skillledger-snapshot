import { Metadata } from 'next'
import Link from 'next/link'
import { Wallet, ShieldCheck, Award, MessageSquare, Search } from 'lucide-react'
import { featuresData } from '@/lib/data/features-data'
import { buildPublicPageMetadata, generateBreadcrumbSchema, generateItemListSchema, SITE_CONFIG } from '@/lib/seo'
import { JsonLd } from '@/components/marketing/JsonLd'
import { RelatedHubs } from '@/components/marketing/RelatedHubs'
import { FunnelLinks } from '@/components/marketing/FunnelLinks'
import { FunnelCTA } from '@/components/marketing/FunnelCTA'

const iconMap: Record<string, React.ElementType> = {
  Wallet,
  ShieldCheck,
  Award,
  MessageSquare,
  Search,
}

export const metadata: Metadata = buildPublicPageMetadata(
  'Platform Features',
  'Explore SkillLedger features: credit wallet exchange, project escrow, reputation badges, real-time collaboration, and a skill marketplace for professionals.',
  '/features',
  ['skillledger features', 'skill exchange platform features', 'professional barter tools', 'credit exchange features']
)

export default function FeaturesPage() {
  const breadcrumbSchema = generateBreadcrumbSchema([
    { name: 'Home', url: SITE_CONFIG.url },
    { name: 'Features', url: `${SITE_CONFIG.url}/features` },
  ])
  const listSchema = generateItemListSchema(
    featuresData.map((f) => ({
      name: f.name,
      url: `${SITE_CONFIG.url}/features/${f.slug}`,
      description: f.description,
    })),
    'SkillLedger Platform Features'
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
            <span>Features</span>
          </nav>

          <header className="mb-16 text-center">
            <h1 className="text-4xl lg:text-5xl font-black tracking-tight mb-6">Platform Features</h1>
            <p className="text-xl text-muted-foreground max-w-2xl mx-auto leading-relaxed">
              Everything you need to exchange professional services through a secure, credit-based barter system.
            </p>
          </header>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
            {featuresData.map((feature) => {
              const Icon = iconMap[feature.icon] || Wallet
              return (
                <Link
                  key={feature.slug}
                  href={`/features/${feature.slug}`}
                  className="card-feature p-8 hover:shadow-lg transition-all duration-200 group"
                >
                  <div className="flex items-center gap-4 mb-4">
                    <div className="p-3 bg-gradient-to-br from-primary/20 to-primary/10 rounded-xl shrink-0">
                      <Icon className="w-6 h-6 text-primary" />
                    </div>
                    <h2 className="text-xl font-bold group-hover:text-primary transition-colors">{feature.name}</h2>
                  </div>
                  <p className="text-sm text-muted-foreground leading-relaxed mb-4">{feature.tagline}</p>
                  <ul className="space-y-1.5">
                    {feature.benefits.slice(0, 3).map((b, i) => (
                      <li key={i} className="flex items-start gap-2 text-sm text-muted-foreground">
                        <span className="text-primary mt-0.5 shrink-0">&#10003;</span>
                        <span className="line-clamp-1">{b}</span>
                      </li>
                    ))}
                  </ul>
                </Link>
              )
            })}
          </div>

          <div className="mt-20">
            <RelatedHubs currentPath="/features" />
            <FunnelLinks stage="mofu" features={featuresData.slice(0, 3)} />
            <FunnelCTA stage="mofu" />
          </div>
        </div>
      </div>
    </>
  )
}
