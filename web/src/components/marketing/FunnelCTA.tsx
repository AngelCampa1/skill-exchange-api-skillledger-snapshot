'use client'

import Link from 'next/link'
import { trackEvent } from '@/utils/analytics'
import { FUNNEL_CTA_PRESETS, type FunnelStage } from '@/lib/funnel'

interface FunnelCTAProps {
  stage: FunnelStage
  /** Optional page context string to personalize the heading (e.g. city name). */
  pageContext?: string
}

export function FunnelCTA({ stage, pageContext }: FunnelCTAProps) {
  const preset = FUNNEL_CTA_PRESETS[stage]

  // pageContext personalization only applies to TOFU — the TOFU heading contains
  // 'Your' as a substitution point. MOFU/BOFU headings don't have this token.
  const heading =
    stage === 'tofu' && pageContext
      ? preset.heading.replace('Your', `Your ${pageContext}`)
      : preset.heading

  function handleClick(label: string) {
    trackEvent({
      name: 'cta_clicked',
      category: 'conversion',
      priority: 'high',
      properties: { location: `funnel-${stage}`, label },
    })
  }

  return (
    <section className="bg-primary/5 rounded-2xl p-8 lg:p-12 text-center">
      <h2 className="text-2xl lg:text-3xl font-bold mb-4">{heading}</h2>
      <p className="text-muted-foreground mb-8 max-w-xl mx-auto leading-relaxed">{preset.subheading}</p>
      <div className="flex flex-col sm:flex-row gap-4 justify-center">
        <Link
          href={preset.primary.href}
          className="btn-primary"
          onClick={() => handleClick(preset.primary.label)}
        >
          {preset.primary.label}
        </Link>
        <Link
          href={preset.secondary.href}
          className="btn-secondary"
          onClick={() => handleClick(preset.secondary.label)}
        >
          {preset.secondary.label}
        </Link>
      </div>
    </section>
  )
}
