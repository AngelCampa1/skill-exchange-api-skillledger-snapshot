import Link from 'next/link'

interface HubEntry {
  path: string
  label: string
  description: string
  stage: 'tofu' | 'mofu' | 'bofu'
}

const HUB_REGISTRY: HubEntry[] = [
  { path: '/categories', label: 'Skill Categories', description: 'Browse 19 professional skill categories', stage: 'tofu' },
  { path: '/industries', label: 'Industries', description: 'Skill exchange by industry vertical', stage: 'tofu' },
  { path: '/trade', label: 'Trade Pairings', description: 'All skill exchange combinations', stage: 'tofu' },
  { path: '/locations', label: 'Locations', description: 'Find exchanges in your city', stage: 'tofu' },
  { path: '/skill-exchange', label: 'Skill Exchange by City', description: 'Browse 50+ US cities', stage: 'tofu' },
  { path: '/glossary', label: 'Glossary', description: 'Terms and definitions for skill exchange', stage: 'tofu' },
  { path: '/compare', label: 'Platform Comparisons', description: 'SkillLedger vs. alternatives, side by side', stage: 'mofu' },
  { path: '/how-to', label: 'How-To Guides', description: 'Step-by-step exchange tutorials', stage: 'mofu' },
  { path: '/features', label: 'Platform Features', description: 'What you get on SkillLedger', stage: 'mofu' },
  { path: '/resources', label: 'Resources', description: 'Articles, guides, and tools', stage: 'mofu' },
  { path: '/tools', label: 'Tools & Calculators', description: 'Free valuation and planning tools', stage: 'mofu' },
  { path: '/pricing', label: 'Pricing', description: 'Simple plans, 30-day free trial', stage: 'bofu' },
]

interface RelatedHubsProps {
  currentPath: string
}

/**
 * Compact hub discovery grid shown on hub pages for horizontal cross-linking.
 * TOFU pages → 3 other TOFU + 2 MOFU hubs (5 total)
 * MOFU pages → 2 other MOFU + 2 TOFU + 1 BOFU (pricing) (5 total)
 * Other pages → 3 TOFU + 2 MOFU (5 total)
 */
export function RelatedHubs({ currentPath }: RelatedHubsProps) {
  const current = HUB_REGISTRY.find((h) => h.path === currentPath)
  const currentStage = current?.stage ?? 'tofu'

  const others = HUB_REGISTRY.filter((h) => h.path !== currentPath)

  let selected: HubEntry[]

  if (currentStage === 'tofu') {
    const tofuHubs = others.filter((h) => h.stage === 'tofu').slice(0, 3)
    const mofuHubs = others.filter((h) => h.stage === 'mofu').slice(0, 2)
    selected = [...tofuHubs, ...mofuHubs]
  } else if (currentStage === 'mofu') {
    const mofuHubs = others.filter((h) => h.stage === 'mofu').slice(0, 2)
    const tofuHubs = others.filter((h) => h.stage === 'tofu').slice(0, 2)
    const bofuHubs = others.filter((h) => h.stage === 'bofu').slice(0, 1)
    selected = [...mofuHubs, ...tofuHubs, ...bofuHubs]
  } else {
    const tofuHubs = others.filter((h) => h.stage === 'tofu').slice(0, 3)
    const mofuHubs = others.filter((h) => h.stage === 'mofu').slice(0, 2)
    selected = [...tofuHubs, ...mofuHubs]
  }

  return (
    <section className="mb-16" aria-labelledby="related-hubs-heading">
      <h2 id="related-hubs-heading" className="text-lg font-bold mb-4">Explore Related Sections</h2>
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
        {selected.map((hub) => (
          <Link
            key={hub.path}
            href={hub.path}
            className="card-feature p-4 hover:border-primary/40 transition-all group"
          >
            <p className="font-semibold text-sm group-hover:text-primary transition-colors mb-1">{hub.label}</p>
            <p className="text-xs text-muted-foreground line-clamp-2">{hub.description}</p>
          </Link>
        ))}
      </div>
    </section>
  )
}
