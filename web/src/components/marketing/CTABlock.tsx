'use client'

import Link from 'next/link'
import { trackEvent } from '@/utils/analytics'

interface CTABlockProps {
  title: string
  description: string
  primaryCta?: { label: string; href: string }
  secondaryCta?: { label: string; href: string }
  variant?: 'inline' | 'bottom'
  trustLine?: string
  analyticsLabel?: string
}

export function CTABlock({ title, description, primaryCta, secondaryCta, variant = 'bottom', trustLine, analyticsLabel }: CTABlockProps) {
  function handleClick(label: string) {
    trackEvent({
      name: 'cta_clicked',
      category: 'conversion',
      priority: 'high',
      properties: { location: analyticsLabel || 'unknown', label },
    })
  }

  return (
    <section className={variant === 'inline'
      ? 'border-t border-b border-border py-8 my-8 text-center'
      : 'bg-primary/5 rounded-2xl p-8 lg:p-12 text-center'
    }>
      <h2 className="text-2xl lg:text-3xl font-bold mb-4">{title}</h2>
      <p className="text-muted-foreground mb-8 max-w-xl mx-auto leading-relaxed">{description}</p>
      <div className="flex flex-col sm:flex-row gap-4 justify-center">
        {primaryCta && (
          <Link href={primaryCta.href} className="btn-primary" onClick={() => handleClick(primaryCta.label)}>
            {primaryCta.label}
          </Link>
        )}
        {secondaryCta && (
          <Link href={secondaryCta.href} className="btn-secondary" onClick={() => handleClick(secondaryCta.label)}>
            {secondaryCta.label}
          </Link>
        )}
      </div>
      {trustLine && (
        <p className="text-sm text-muted-foreground mt-4">{trustLine}</p>
      )}
    </section>
  )
}
