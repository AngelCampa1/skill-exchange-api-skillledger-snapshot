'use client'

import { logger } from '@/utils/logger';
import { trackEvent } from '@/utils/analytics';

import { useState, useEffect } from 'react'
import {
  Crown,
  Star,
  Gem,
  Shield,
  Check,
  ChevronRight,
  Zap,
  Users,
  TrendingUp,
  AlertCircle,
  Loader2
} from 'lucide-react'
import {
  SubscriptionTier,
  BillingCycle,
  CheckoutSessionResult
} from '@/types/subscription'
import { useSubscription } from '@/lib/subscription-api'

// BUG-010 FIX: Map technical feature keys to human-readable labels
const FEATURE_LABELS: Record<string, string> = {
  // Professional tier features
  'basic_project_management': 'Basic Project Management',
  'credit_wallet': 'Credit Wallet',
  'messaging': 'Messaging',
  'file_sharing': 'File Sharing',
  'basic_analytics': 'Basic Analytics',

  // Business tier features
  'PrioritySupport': 'Priority Support',
  'ApiAccess': 'API Access',
  'AdvancedAnalytics': 'Advanced Analytics',
  'advanced_project_management': 'Advanced Project Management',
  'priority_support': 'Priority Support',
  'api_access': 'API Access',
  'advanced_analytics': 'Advanced Analytics',
  'team_collaboration': 'Team Collaboration',
  'custom_workflows': 'Custom Workflows',
  'priority_messaging': 'Priority Messaging',
  'advanced_file_sharing': 'Advanced File Sharing',
  'performance_analytics': 'Performance Analytics',
  'export_reports': 'Export Reports',

  // Enterprise tier features
  'AdvancedFraudDetection': 'Advanced Fraud Detection',
  'MultiSignature': 'Multi-Signature Transactions',
  'CustomIntegrations': 'Custom Integrations',
  'enterprise_project_management': 'Enterprise Project Management',
  'white_label_options': 'White Label Options',
  'advanced_fraud_detection': 'Advanced Fraud Detection',
  'multi_signature_transactions': 'Multi-Signature Transactions',
  'custom_integrations': 'Custom Integrations',
  'dedicated_account_manager': 'Dedicated Account Manager',
  'sla_guarantee': 'SLA Guarantee',
  'advanced_compliance': 'Advanced Compliance',
  'audit_logs': 'Audit Logs',
  'custom_analytics': 'Custom Analytics',
  'api_rate_limits_high': 'High API Rate Limits',
  'priority_queue': 'Priority Queue',
  'custom_reporting': 'Custom Reporting',
  'data_export_api': 'Data Export API',
  'integration_support': 'Integration Support',
}

/**
 * Convert technical feature key to human-readable label
 */
function formatFeatureLabel(feature: string): string {
  // Check if we have a mapping for this feature
  if (FEATURE_LABELS[feature]) {
    return FEATURE_LABELS[feature]
  }

  // Fallback: convert snake_case or camelCase to Title Case
  return feature
    .replace(/_/g, ' ')
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .split(' ')
    .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
    .join(' ')
}

// BUG-015 FIX: Helper function to format "unlimited" values for display
// Values of -1 or very high numbers (9999+, 999999+) should display as "Unlimited"
function formatLimitValue(value: number, prefix: string = ''): string {
  if (value === -1 || value >= 9999) {
    return 'Unlimited'
  }
  return `${prefix}${value.toLocaleString()}`
}

// BUG-016 FIX: Helper function to handle singular/plural labels based on count
function formatLimitLabel(value: number, singular: string, plural: string): string {
  if (value === -1 || value >= 9999) {
    return plural // "Unlimited" values use plural form (e.g., "Team Members")
  }
  return value === 1 ? singular : plural
}

interface TierSelectionFlowProps {
  onCheckoutSuccess?: (result: CheckoutSessionResult) => void
  onCheckoutError?: (error: Error) => void
  className?: string
}

export function TierSelectionFlow({
  onCheckoutSuccess,
  onCheckoutError,
  className = ''
}: TierSelectionFlowProps) {
  const [billingCycle, setBillingCycle] = useState<BillingCycle>(BillingCycle.Monthly)
  const [selectedTier, setSelectedTier] = useState<SubscriptionTier | null>(null)
  const [isProcessing, setIsProcessing] = useState(false)

  const {
    tiers,
    subscription,
    loading,
    error,
    createCheckout
  } = useSubscription()

  // Track subscription page view when tiers are loaded
  useEffect(() => {
    if (tiers && tiers.length > 0) {
      trackEvent({
        name: 'view_item',  // GA4 recommended e-commerce event
        category: 'monetization',
        priority: 'critical',
        properties: {
          item_category: 'subscription',
          tier_count: tiers.length,
        },
      })
    }
  }, [tiers])

  const getTierIcon = (tierName: string) => {
    if (tierName.toLowerCase().includes('enterprise')) return <Crown className="w-6 h-6" />
    if (tierName.toLowerCase().includes('professional')) return <Star className="w-6 h-6" />
    if (tierName.toLowerCase().includes('business')) return <Gem className="w-6 h-6" />
    return <Shield className="w-6 h-6" />
  }

  const getTierGradient = (tierName: string) => {
    if (tierName.toLowerCase().includes('enterprise')) {
      return 'from-primary to-primary/80'
    }
    if (tierName.toLowerCase().includes('professional')) {
      return 'from-info to-info/80'
    }
    if (tierName.toLowerCase().includes('business')) {
      return 'from-success to-success/80'
    }
    return 'from-muted to-muted/80'
  }

  const formatPrice = (tier: SubscriptionTier) => {
    const price = billingCycle === BillingCycle.Annual ? tier.annualPrice ?? tier.price : tier.price
    const monthlyPrice = billingCycle === BillingCycle.Annual ? price / 12 : price

    if (billingCycle === BillingCycle.Annual) {
      return (
        <div className="text-center">
          <div className="text-3xl font-black text-foreground">
            ${monthlyPrice.toFixed(2)}
          </div>
          <div className="text-sm text-muted-foreground">/month (billed annually)</div>
          <div className="text-xs text-success mt-1">
            Save ${((tier.price * 12 - (tier.annualPrice ?? tier.price * 12)) / 12).toFixed(2)}/mo
          </div>
        </div>
      )
    }

    return (
      <div className="text-center">
        <div className="text-3xl font-black text-foreground">
          ${price.toFixed(2)}
        </div>
        <div className="text-sm text-muted-foreground">/month</div>
      </div>
    )
  }

  const isCurrentTier = (tierId: string) => {
    return subscription?.tier?.id === tierId && subscription.status === 'Active'
  }

  const isUpgrade = (tierId: string) => {
    if (!subscription) return true

    const currentTier = tiers.find(t => t.id === subscription.tier?.id)
    const selectedTier = tiers.find(t => t.id === tierId)

    if (!currentTier || !selectedTier) return false

    return selectedTier.sortOrder > currentTier.sortOrder
  }

  const handleSubscribe = async (tier: SubscriptionTier) => {
    if (isCurrentTier(tier.id)) return

    try {
      setIsProcessing(true)
      setSelectedTier(tier)

      // Calculate the actual price based on billing cycle
      const price = billingCycle === BillingCycle.Annual ? tier.annualPrice ?? tier.price : tier.price
      const monthlyPrice = billingCycle === BillingCycle.Annual ? price / 12 : price

      // Track tier selection
      trackEvent({
        name: 'select_item',  // GA4 recommended e-commerce event
        category: 'monetization',
        priority: 'critical',
        properties: {
          item_name: tier.name,
          tier: tier.id,
          billing_cycle: billingCycle,
          currency: 'USD',
          value: monthlyPrice,
          is_upgrade: isUpgrade(tier.id),
        },
      })

      const result = await createCheckout(tier.id, billingCycle)

      if (result.success && result.sessionUrl) {
        // Track beginning of checkout
        trackEvent({
          name: 'begin_checkout',  // GA4 recommended e-commerce event
          category: 'monetization',
          priority: 'critical',
          properties: {
            currency: 'USD',
            value: monthlyPrice,
            item_name: tier.name,
            tier: tier.id,
            billing_cycle: billingCycle,
          },
        })

        // Redirect to Stripe Checkout
        window.location.href = result.sessionUrl
      } else {
        throw new Error(result.errorMessage || 'Failed to create checkout session')
      }

      onCheckoutSuccess?.(result)
    } catch (error) {
      logger.error('Checkout error:', error)
      onCheckoutError?.(error instanceof Error ? error : new Error('Checkout failed'))
    } finally {
      setIsProcessing(false)
      setSelectedTier(null)
    }
  }

  if (loading) {
    return (
      <div className={`flex flex-col items-center justify-center py-20 ${className}`}>
        <Loader2 className="w-8 h-8 animate-spin text-primary mb-4" />
        <p className="text-body text-muted-foreground">Loading subscription options...</p>
      </div>
    )
  }

  if (error) {
    return (
      <div className={`flex flex-col items-center justify-center py-20 ${className}`}>
        <AlertCircle className="w-8 h-8 text-error mb-4" />
        <p className="text-body text-error mb-2">Failed to load subscription options</p>
        <p className="text-caption text-muted-foreground">{error}</p>
      </div>
    )
  }

  if (tiers.length === 0) {
    return (
      <div className={`flex flex-col items-center justify-center py-20 ${className}`}>
        <Shield className="w-8 h-8 text-muted mb-4" />
        <p className="text-body text-muted-foreground">No subscription tiers available</p>
      </div>
    )
  }

  return (
    <div className={`space-y-8 ${className}`}>
      {/* Billing Cycle Toggle - BUG-018/019 FIX: Improved mobile responsiveness */}
      <div className="flex flex-col items-center space-y-4">
        <h3 className="text-subheading text-foreground">Choose your billing cycle</h3>
        <div className="inline-flex flex-col sm:flex-row items-stretch sm:items-center rounded-xl bg-muted p-1 w-full sm:w-auto max-w-xs sm:max-w-none">
          <button
            onClick={() => setBillingCycle(BillingCycle.Monthly)}
            className={`px-4 sm:px-6 py-3 rounded-lg text-sm font-medium transition-all duration-200 ${
              billingCycle === BillingCycle.Monthly
                ? 'bg-background text-foreground shadow-sm'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            Monthly
          </button>
          <button
            onClick={() => setBillingCycle(BillingCycle.Annual)}
            className={`px-4 sm:px-6 py-3 rounded-lg text-sm font-medium transition-all duration-200 flex items-center justify-center gap-2 ${
              billingCycle === BillingCycle.Annual
                ? 'bg-background text-foreground shadow-sm'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            <span>Annual</span>
            {/* BUG-018 FIX: Use gap-2 instead of ml-2 for consistent spacing */}
            <span className="px-2 py-0.5 bg-success/20 text-success text-xs rounded-full whitespace-nowrap">
              Save 20%
            </span>
          </button>
        </div>
      </div>

      {/* Subscription Tiers Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
        {tiers.map((tier) => {
          const isCurrent = isCurrentTier(tier.id)
          const isUpgradeTier = isUpgrade(tier.id)
          const isSelected = selectedTier?.id === tier.id

          return (
            <div
              key={tier.id}
              className={`relative card-interactive p-8 transition-all duration-300 ${
                isCurrent
                  ? 'ring-2 ring-success ring-offset-2 ring-offset-background'
                  : isUpgradeTier
                  ? 'ring-2 ring-primary ring-offset-2 ring-offset-background'
                  : ''
              } ${isSelected ? 'scale-105 shadow-2xl' : ''}`}
            >
              {/* Current/Upgrade Badge */}
              {isCurrent && (
                <div className="absolute -top-4 left-1/2 transform -translate-x-1/2">
                  <span className="status-success">Current Plan</span>
                </div>
              )}

              {isUpgradeTier && !isCurrent && (
                <div className="absolute -top-4 left-1/2 transform -translate-x-1/2">
                  <span className="status-info">Upgrade</span>
                </div>
              )}

              {/* Tier Header */}
              <div className="text-center mb-8">
                <div className={`inline-flex p-4 rounded-2xl bg-gradient-to-br ${getTierGradient(tier.name)} mb-4`}>
                  <div className="text-primary-foreground">
                    {getTierIcon(tier.name)}
                  </div>
                </div>

                <h3 className="text-2xl font-black text-foreground mb-2">
                  {tier.name}
                </h3>

                {tier.description && (
                  <p className="text-body text-muted-foreground mb-6">
                    {tier.description}
                  </p>
                )}

                {/* Pricing */}
                {formatPrice(tier)}

                {/* Credit Bonus */}
                {tier.creditBonus > 0 && (
                  <div className="mt-4 inline-flex items-center px-3 py-1 bg-primary/10 text-primary rounded-full text-sm">
                    <Zap className="w-4 h-4 mr-1" />
                    +{tier.creditBonus} credits
                  </div>
                )}
              </div>

              {/* Features */}
              <div className="space-y-4 mb-8">
                {tier.features && tier.features.length > 0 && (
                  <div className="space-y-3">
                    {tier.features.map((feature, index) => (
                      <div key={index} className="flex items-start space-golden-sm">
                        <Check className="w-5 h-5 text-success mt-0.5 flex-shrink-0" />
                        {/* BUG-010 FIX: Format feature labels for human readability */}
                        <span className="text-body text-foreground">{formatFeatureLabel(feature)}</span>
                      </div>
                    ))}
                  </div>
                )}

                {/* Additional Features as Icons */}
                <div className="grid grid-cols-2 gap-4 pt-4 border-t border-border">
                  {tier.prioritySupport && (
                    <div className="flex items-center space-golden-sm">
                      <Users className="w-4 h-4 text-primary" />
                      <span className="text-caption text-foreground">Priority Support</span>
                    </div>
                  )}

                  {tier.apiAccess && (
                    <div className="flex items-center space-golden-sm">
                      <TrendingUp className="w-4 h-4 text-primary" />
                      <span className="text-caption text-foreground">API Access</span>
                    </div>
                  )}

                  {tier.advancedAnalytics && (
                    <div className="flex items-center space-golden-sm">
                      <Star className="w-4 h-4 text-primary" />
                      <span className="text-caption text-foreground">Advanced Analytics</span>
                    </div>
                  )}

                  {tier.advancedFraudDetection && (
                    <div className="flex items-center space-golden-sm">
                      <Shield className="w-4 h-4 text-primary" />
                      <span className="text-caption text-foreground">Fraud Detection</span>
                    </div>
                  )}
                </div>
              </div>

              {/* CTA Button */}
              <button
                onClick={() => handleSubscribe(tier)}
                disabled={isCurrent || isProcessing}
                className={`w-full py-4 px-6 rounded-xl font-semibold transition-all duration-200 flex items-center justify-center space-golden-sm ${
                  isCurrent
                    ? 'bg-muted text-muted-foreground cursor-not-allowed'
                    : isUpgradeTier
                    ? 'btn-primary'
                    : 'btn-secondary'
                } ${isProcessing && isSelected ? 'opacity-75' : ''}`}
              >
                {isProcessing && isSelected ? (
                  <>
                    <Loader2 className="w-5 h-5 animate-spin mr-2" />
                    Processing...
                  </>
                ) : isCurrent ? (
                  'Current Plan'
                ) : isUpgradeTier ? (
                  <>
                    Upgrade Now
                    <ChevronRight className="w-5 h-5" />
                  </>
                ) : (
                  'Downgrade'
                )}
              </button>

              {/* Limits Info - BUG-015 FIX: Use formatLimitValue to display "Unlimited" for high values */}
              <div className="mt-6 pt-6 border-t border-border">
                <div className="space-y-2 text-sm text-muted-foreground">
                  <div className="flex justify-between">
                    <span>Active Projects</span>
                    <span className="font-medium text-foreground">
                      {formatLimitValue(tier.maxActiveProjects)}
                    </span>
                  </div>
                  {/* BUG-016 FIX: Use dynamic singular/plural label based on value */}
                  <div className="flex justify-between">
                    <span>{formatLimitLabel(tier.maxTeamMembers, 'Team Member', 'Team Members')}</span>
                    <span className="font-medium text-foreground">
                      {formatLimitValue(tier.maxTeamMembers)}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span>Monthly Earnings</span>
                    <span className="font-medium text-foreground">
                      {formatLimitValue(tier.maxMonthlyEarnings, '$')}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          )
        })}
      </div>

      {/* Trust Signals */}
      <div className="text-center py-8">
        <div className="inline-flex items-center space-golden-md text-sm text-muted-foreground">
          <Shield className="w-5 h-5 text-success" />
          <span>Secure checkout powered by Stripe</span>
          <ChevronRight className="w-4 h-4" />
          <span>Cancel anytime</span>
          <ChevronRight className="w-4 h-4" />
          <span>30-day money-back guarantee</span>
        </div>
      </div>
    </div>
  )
}