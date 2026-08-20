'use client'

import { useState } from 'react'
import Link from 'next/link'
import type { SubscriptionTier } from '@/types/subscription'

interface PricingCardsProps {
  tiers: SubscriptionTier[]
}

export function PricingCards({ tiers }: PricingCardsProps) {
  const [isAnnual, setIsAnnual] = useState(false)

  const sortedTiers = [...tiers].sort((a, b) => a.sortOrder - b.sortOrder)

  return (
    <div>
      {/* Billing Toggle */}
      <div className="flex items-center justify-center gap-3 mb-12">
        <button
          type="button"
          onClick={() => setIsAnnual(false)}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
            !isAnnual
              ? 'bg-primary text-primary-foreground'
              : 'bg-muted text-muted-foreground hover:text-foreground'
          }`}
        >
          Monthly
        </button>
        <button
          type="button"
          onClick={() => setIsAnnual(true)}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
            isAnnual
              ? 'bg-primary text-primary-foreground'
              : 'bg-muted text-muted-foreground hover:text-foreground'
          }`}
        >
          Annual (Save 20%)
        </button>
      </div>

      {/* Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-8 max-w-5xl mx-auto">
        {sortedTiers.map((tier, index) => {
          const isPopular = index === 1 || tier.name.toLowerCase() === 'business'
          const displayPrice = isAnnual && tier.annualPrice
            ? Math.round(tier.annualPrice / 12)
            : tier.price

          const annualSavings = tier.annualPrice
            ? Math.round(tier.price * 12 - tier.annualPrice)
            : null

          return (
            <div
              key={tier.id}
              className={`card-feature p-8 flex flex-col relative ${
                isPopular ? 'border-primary/30 border-2' : ''
              }`}
            >
              {isPopular && (
                <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                  <span className="text-xs font-semibold rounded-full bg-primary text-primary-foreground px-3 py-1">
                    Most Popular
                  </span>
                </div>
              )}

              <div className="mb-6">
                <h2 className="text-2xl font-bold mb-2">{tier.name}</h2>
                <div className="flex items-baseline gap-1">
                  <span className="text-4xl font-black">${displayPrice}</span>
                  <span className="text-muted-foreground">
                    {isAnnual && tier.annualPrice ? '/mo billed annually' : '/month'}
                  </span>
                </div>
                {isAnnual && annualSavings && annualSavings > 0 && (
                  <p className="text-sm text-primary font-medium mt-1">
                    ${tier.annualPrice}/year — save ${annualSavings}
                  </p>
                )}
                {tier.description && (
                  <p className="text-sm text-muted-foreground mt-2">{tier.description}</p>
                )}
              </div>

              <ul className="space-y-3 mb-8 flex-1">
                {tier.features.map((feature) => (
                  <li key={feature} className="flex items-start gap-3 text-sm">
                    <span className="text-primary font-bold mt-0.5">&#10003;</span>
                    <span>{feature}</span>
                  </li>
                ))}
              </ul>

              <Link
                href="/register"
                className="btn-primary inline-block text-center"
              >
                Start Free Trial
              </Link>
            </div>
          )
        })}

        {sortedTiers.length === 0 && (
          <div className="col-span-3 text-center py-12 text-muted-foreground">
            <p>Plans loading...</p>
            <Link href="/register" className="btn-primary inline-block mt-4">
              Start Free Trial
            </Link>
          </div>
        )}
      </div>
    </div>
  )
}
