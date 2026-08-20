/**
 * Funnel stage definitions and CTA presets for vertical marketing funnel linking.
 * TOFU → MOFU → BOFU guides visitors from awareness through decision.
 */

export type FunnelStage = 'tofu' | 'mofu' | 'bofu'

export interface FunnelCTAConfig {
  heading: string
  subheading: string
  primary: { label: string; href: string }
  secondary: { label: string; href: string }
}

export const FUNNEL_CTA_PRESETS: Record<FunnelStage, FunnelCTAConfig> = {
  tofu: {
    heading: 'Discover Your Perfect Skill Exchange',
    subheading: 'Not sure where to start? Take the quiz to find your best match, or use the calculator to see what your skills are worth.',
    primary: { label: 'Find Your Skill Match', href: '/skill-match' },
    secondary: { label: 'Try the Calculator', href: '/tools/barter-valuation-calculator' },
  },
  mofu: {
    heading: 'See How SkillLedger Compares',
    subheading: 'Evaluate your options side by side, then check pricing to find the plan that fits.',
    primary: { label: 'See How We Compare', href: '/compare' },
    secondary: { label: 'View Pricing', href: '/pricing' },
  },
  bofu: {
    heading: 'Start Exchanging Skills Today',
    subheading: 'Join professionals already trading on SkillLedger. 30-day free trial, escrow-protected.',
    primary: { label: 'Start Free Trial', href: '/register' },
    secondary: { label: 'See Pricing', href: '/pricing' },
  },
}

/**
 * Maps article buyerStage frontmatter values to FunnelStage.
 * Articles default to 'awareness' which maps to TOFU.
 */
export function buyerStageToFunnel(buyerStage: 'awareness' | 'consideration' | 'decision'): FunnelStage {
  switch (buyerStage) {
    case 'awareness':
      return 'tofu'
    case 'consideration':
      return 'mofu'
    case 'decision':
      return 'bofu'
  }
}
