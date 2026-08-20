import Link from 'next/link'
import type { FunnelStage } from '@/lib/funnel'

interface ComparisonLink {
  slug: string
  title: string
  description: string
}

interface HowToLink {
  slug: string
  title: string
  skillOffered: string
  skillNeeded: string
}

interface FeatureLink {
  slug: string
  name: string
  tagline: string
}

interface ArticleLink {
  slug: string
  title: string
  description: string
}

interface FunnelLinksProps {
  stage: FunnelStage
  comparisons?: ComparisonLink[]
  howToGuides?: HowToLink[]
  features?: FeatureLink[]
  articles?: ArticleLink[]
}

/**
 * "Next Steps" section that surfaces the next funnel stage's key content.
 * TOFU pages → shows comparison + how-to links ("Dig Deeper")
 * MOFU pages → shows feature + pricing links ("Ready to Decide?")
 * BOFU pages → renders nothing (FunnelCTA handles conversion)
 */
export function FunnelLinks({ stage, comparisons = [], howToGuides = [], features = [], articles = [] }: FunnelLinksProps) {
  if (stage === 'bofu') return null

  if (stage === 'tofu') {
    const hasContent = comparisons.length > 0 || howToGuides.length > 0 || articles.length > 0
    if (!hasContent) return null

    return (
      <section className="mb-16 border border-border rounded-2xl p-8" aria-labelledby="funnel-links-heading">
        <div className="mb-6">
          <span className="text-xs font-bold uppercase tracking-wider text-primary">Dig Deeper</span>
          <h2 id="funnel-links-heading" className="text-xl font-bold mt-1">Evaluate Your Options</h2>
          <p className="text-sm text-muted-foreground mt-1">Before you sign up, see how SkillLedger compares and read the how-to guides.</p>
        </div>

        <div className={`grid grid-cols-1 gap-8 ${[comparisons, howToGuides, articles].filter((a) => a.length > 0).length === 3 ? 'lg:grid-cols-3' : 'lg:grid-cols-2'}`}>
          {comparisons.length > 0 && (
            <div>
              <h3 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground mb-3">Platform Comparisons</h3>
              <div className="space-y-3">
                {comparisons.map((c) => (
                  <Link
                    key={c.slug}
                    href={`/compare/${c.slug}`}
                    className="block card-feature p-4 hover:shadow-md transition-all duration-200 group"
                  >
                    <p className="font-medium group-hover:text-primary transition-colors text-sm line-clamp-1">{c.title}</p>
                    <p className="text-xs text-muted-foreground mt-1 line-clamp-2">{c.description}</p>
                  </Link>
                ))}
              </div>
              <Link href="/compare" className="text-xs text-primary font-medium hover:underline mt-3 inline-block">
                All comparisons →
              </Link>
            </div>
          )}

          {howToGuides.length > 0 && (
            <div>
              <h3 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground mb-3">How-To Guides</h3>
              <div className="space-y-3">
                {howToGuides.map((h) => (
                  <Link
                    key={h.slug}
                    href={`/how-to/${h.slug}`}
                    className="block card-feature p-4 hover:shadow-md transition-all duration-200 group"
                  >
                    <p className="font-medium group-hover:text-primary transition-colors text-sm line-clamp-1">{h.title}</p>
                    <div className="flex items-center gap-1.5 text-xs text-muted-foreground mt-1">
                      <span>{h.skillOffered}</span>
                      <span>↔</span>
                      <span>{h.skillNeeded}</span>
                    </div>
                  </Link>
                ))}
              </div>
              <Link href="/how-to" className="text-xs text-primary font-medium hover:underline mt-3 inline-block">
                All how-to guides →
              </Link>
            </div>
          )}

          {articles.length > 0 && (
            <div>
              <h3 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground mb-3">Related Articles</h3>
              <div className="space-y-3">
                {articles.map((a) => (
                  <Link
                    key={a.slug}
                    href={`/resources/${a.slug}`}
                    className="block card-feature p-4 hover:shadow-md transition-all duration-200 group"
                  >
                    <p className="font-medium group-hover:text-primary transition-colors text-sm line-clamp-1">{a.title}</p>
                    <p className="text-xs text-muted-foreground mt-1 line-clamp-2">{a.description}</p>
                  </Link>
                ))}
              </div>
              <Link href="/resources" className="text-xs text-primary font-medium hover:underline mt-3 inline-block">
                All resources →
              </Link>
            </div>
          )}
        </div>
      </section>
    )
  }

  // mofu stage
  const hasContent = features.length > 0
  if (!hasContent) return null

  return (
    <section className="mb-16 border border-border rounded-2xl p-8" aria-labelledby="funnel-links-heading">
      <div className="mb-6">
        <span className="text-xs font-bold uppercase tracking-wider text-primary">Ready to Decide?</span>
        <h2 id="funnel-links-heading" className="text-xl font-bold mt-1">What You Get on SkillLedger</h2>
        <p className="text-sm text-muted-foreground mt-1">Review the platform features, then see pricing to find the right plan.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-6">
        {features.map((f) => (
          <Link
            key={f.slug}
            href={`/features/${f.slug}`}
            className="card-feature p-4 hover:shadow-md transition-all duration-200 group"
          >
            <p className="font-medium group-hover:text-primary transition-colors text-sm">{f.name}</p>
            <p className="text-xs text-muted-foreground mt-1 line-clamp-2">{f.tagline}</p>
          </Link>
        ))}
      </div>

      <div className="flex gap-4">
        <Link href="/features" className="text-xs text-primary font-medium hover:underline">
          All features →
        </Link>
        <Link href="/pricing" className="text-xs text-primary font-medium hover:underline">
          View pricing →
        </Link>
      </div>

      {articles.length > 0 && (
        <div className="mt-6 pt-6 border-t border-border">
          <h3 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground mb-3">Supplementary Reading</h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {articles.map((a) => (
              <Link
                key={a.slug}
                href={`/resources/${a.slug}`}
                className="block card-feature p-3 hover:shadow-md transition-all duration-200 group"
              >
                <p className="font-medium group-hover:text-primary transition-colors text-sm line-clamp-1">{a.title}</p>
                <p className="text-xs text-muted-foreground mt-0.5 line-clamp-2">{a.description}</p>
              </Link>
            ))}
          </div>
        </div>
      )}
    </section>
  )
}
