'use client'

import { logger } from '@/utils/logger';

import { useState, useEffect } from 'react'
import Link from 'next/link'
// BUG-MED-007 FIX: Import Next.js router for internal navigation
import { useRouter } from 'next/navigation'
import {
  CreditCard,
  TrendingUp,
  Shield,
  Star,
  Users,
  Zap,
  Settings,
  ChevronRight,
  Check,
  X,
  AlertCircle,
  Crown,
  Gem,
  RefreshCw,
  Loader2
} from 'lucide-react'
import { AUTH_CONFIG } from '@/constants/auth'
import {
  SubscriptionTier,
  UserSubscription,
  SubscriptionStatus,
  BillingCycle,
  CheckoutSessionResult,
  CreateSubscriptionRequest,
  PaymentMethod,
  SubscriptionLimits
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

// BUG-016 FIX: Helper function to format limit with proper singular/plural label
function formatLimitLabel(value: number, singularLabel: string, pluralLabel: string): string {
  if (value === -1 || value >= 9999) {
    return `Unlimited ${pluralLabel}`
  }
  const label = value === 1 ? singularLabel : pluralLabel
  return `${value.toLocaleString()} ${label}`
}

interface SubscriptionDashboardProps {
  user?: any
}

export function SubscriptionDashboard({ user }: SubscriptionDashboardProps) {
  // BUG-MED-007 FIX: Use Next.js router for internal navigation
  const router = useRouter()
  const [billingCycle, setBillingCycle] = useState<BillingCycle>(BillingCycle.Monthly)
  const [paymentMethods, setPaymentMethods] = useState<PaymentMethod[]>([])
  const [isManagingPayment, setIsManagingPayment] = useState(false)
  const [isLoadingPaymentMethods, setIsLoadingPaymentMethods] = useState(true)
  const [isSyncingPaymentMethods, setIsSyncingPaymentMethods] = useState(false)

  const {
    subscription,
    tiers,
    loading,
    error,
    createCheckout,
    setupPaymentMethod
  } = useSubscription()

  // Fetch payment methods on mount
  useEffect(() => {
    fetchPaymentMethods()
  }, [])

  const fetchPaymentMethods = async () => {
    try {
      setIsLoadingPaymentMethods(true)
      const response = await fetch('/api/Subscription/payment-methods', {
        credentials: AUTH_CONFIG.CREDENTIALS,
      })

      if (response.ok) {
        const data = await response.json()
        // API returns array directly
        setPaymentMethods(Array.isArray(data) ? data : [])
      }
    } catch (err) {
      logger.error('Failed to fetch payment methods:', err)
    } finally {
      setIsLoadingPaymentMethods(false)
    }
  }

  const syncPaymentMethods = async () => {
    try {
      setIsSyncingPaymentMethods(true)
      const response = await fetch('/api/Subscription/payment-methods/sync', {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
      })

      if (response.ok) {
        const data = await response.json()
        // API returns array directly
        setPaymentMethods(Array.isArray(data) ? data : [])
      }
    } catch (err) {
      logger.error('Failed to sync payment methods:', err)
    } finally {
      setIsSyncingPaymentMethods(false)
    }
  }

  const [isRemovingPaymentMethod, setIsRemovingPaymentMethod] = useState<string | null>(null)
  const [isSettingDefaultPaymentMethod, setIsSettingDefaultPaymentMethod] = useState<string | null>(null)

  const handleSetDefaultPaymentMethod = async (paymentMethodId: string) => {
    try {
      setIsSettingDefaultPaymentMethod(paymentMethodId)
      const response = await fetch(`/api/Subscription/payment-methods/${paymentMethodId}/set-default`, {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
      })

      if (response.ok) {
        // Refresh payment methods to show updated default status
        await fetchPaymentMethods()
      } else {
        const errorData = await response.json().catch(() => ({}))
        logger.error('Failed to set default payment method:', errorData.message || 'Unknown error')
      }
    } catch (err) {
      logger.error('Failed to set default payment method:', err)
    } finally {
      setIsSettingDefaultPaymentMethod(null)
    }
  }

  const handleRemovePaymentMethod = async (paymentMethodId: string) => {
    if (!confirm('Are you sure you want to remove this payment method?')) {
      return
    }

    try {
      setIsRemovingPaymentMethod(paymentMethodId)
      const response = await fetch(`/api/Subscription/payment-methods/${paymentMethodId}`, {
        method: 'DELETE',
        credentials: AUTH_CONFIG.CREDENTIALS,
      })

      if (response.ok) {
        // Remove from local state
        setPaymentMethods(prev => prev.filter(pm => pm.id !== paymentMethodId))
      } else {
        const errorData = await response.json().catch(() => ({}))
        logger.error('Failed to remove payment method:', errorData.message || 'Unknown error')
        alert(errorData.message || 'Failed to remove payment method')
      }
    } catch (err) {
      logger.error('Failed to remove payment method:', err)
      alert('Failed to remove payment method')
    } finally {
      setIsRemovingPaymentMethod(null)
    }
  }

  const handleCreateSubscription = async (tierId: string): Promise<CheckoutSessionResult> => {
    return await createCheckout(tierId, billingCycle)
  }

  const handleManagePaymentMethods = async () => {
    try {
      const result = await setupPaymentMethod()

      if (result.success && result.sessionUrl) {
        window.location.href = result.sessionUrl
      }
    } catch (error) {
      logger.error('Error managing payment methods:', error)
    }
  }

  const getTierIcon = (tierName: string) => {
    if (tierName.toLowerCase().includes('enterprise')) return <Crown className="w-5 h-5" />
    if (tierName.toLowerCase().includes('professional')) return <Star className="w-5 h-5" />
    if (tierName.toLowerCase().includes('business')) return <Gem className="w-5 h-5" />
    return <Shield className="w-5 h-5" />
  }

  const formatPrice = (price: number, isAnnual: boolean = false) => {
    const displayPrice = isAnnual ? (price / 12) : price
    return `$${displayPrice.toFixed(2)}/mo`
  }

  const getTrialBadge = (tier: SubscriptionTier) => {
    if (tier.name.toLowerCase().includes('free')) {
      return <span className="status-neutral">Free</span>
    }
    if (tier.creditBonus > 0) {
      return <span className="status-info">+{tier.creditBonus} credits</span>
    }
    return null
  }

  const isSubscribed = (tierId: string) => {
    return subscription?.tier?.id === tierId && subscription.status === SubscriptionStatus.Active
  }

  const isUpgrade = (tierId: string) => {
    if (!subscription) return true

    const currentTier = tiers.find(t => t.id === subscription.tier?.id)
    const selectedTier = tiers.find(t => t.id === tierId)

    if (!currentTier || !selectedTier) return false

    return selectedTier.sortOrder > currentTier.sortOrder
  }

  const getStatusColor = (status: SubscriptionStatus) => {
    switch (status) {
      case SubscriptionStatus.Active:
        return 'text-success'
      case SubscriptionStatus.Trial:
        return 'text-info'
      case SubscriptionStatus.PastDue:
        return 'text-warning'
      case SubscriptionStatus.Cancelled:
      case SubscriptionStatus.Expired:
        return 'text-destructive'
      case SubscriptionStatus.Suspended:
        return 'text-warning'
      default:
        return 'text-muted-foreground'
    }
  }

  const getStatusText = (status: SubscriptionStatus) => {
    switch (status) {
      case SubscriptionStatus.Active:
        return 'Active'
      case SubscriptionStatus.Trial:
        return 'Trial'
      case SubscriptionStatus.PastDue:
        return 'Past Due'
      case SubscriptionStatus.Cancelled:
        return 'Cancelled'
      case SubscriptionStatus.Expired:
        return 'Expired'
      case SubscriptionStatus.Suspended:
        return 'Suspended'
      default:
        return 'Unknown'
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center p-12">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
      </div>
    )
  }

  return (
    <div className="space-y-8">
      {/* Current Subscription Status */}
      {subscription && (
        <div className="card-elevated p-6">
          <div className="flex items-start justify-between mb-4">
            <div>
              <h2 className="text-2xl font-bold text-foreground mb-2">
                Your Subscription
              </h2>
              <div className="flex items-center gap-2">
                <span className={`inline-flex items-center px-3 py-1 rounded-full text-sm font-medium ${getStatusColor(subscription.status)}`}>
                  <div className={`w-2 h-2 rounded-full mr-2 ${
                    subscription.status === SubscriptionStatus.Active ? 'bg-success' :
                    subscription.status === SubscriptionStatus.Trial ? 'bg-info' : 'bg-destructive'
                  }`}></div>
                  {getStatusText(subscription.status)}
                </span>
                {subscription.isTrial && (
                  <span className="status-info">Trial</span>
                )}
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="space-y-2">
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Crown className="w-4 h-4" />
                  <span className="text-sm">Current Plan</span>
                </div>
                <p className="text-lg font-semibold text-foreground">
                  {subscription.tier.name}
                </p>
                {subscription.endDate && (
                  <p className="text-sm text-muted-foreground">
                    Renews {new Date(subscription.endDate).toLocaleDateString()}
                  </p>
                )}
              </div>

              <div className="space-y-2">
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Zap className="w-4 h-4" />
                  <span className="text-sm">Credits Used</span>
                </div>
                <p className="text-lg font-semibold text-foreground">
                  {/* BUG-HIGH-012 FIX: Mock data - integrate with API once usage tracking is implemented */}
                  12 / 50
                </p>
              </div>

              <div className="space-y-2">
                <div className="flex items-center gap-2 text-muted-foreground">
                  <TrendingUp className="w-4 h-4" />
                  <span className="text-sm">Monthly Earnings</span>
                </div>
                <p className="text-lg font-semibold text-foreground">
                  {/* BUG-HIGH-012 FIX: Mock data - integrate with API once earnings tracking is implemented */}
                  2,450 credits
                </p>
              </div>
            </div>

            {/* Action Buttons */}
            <div className="flex gap-3 pt-4 border-t border-border">
              <button
                onClick={() => window.location.href = subscription.externalSubscriptionId ?
                  `https://dashboard.stripe.com/subscriptions/${subscription.externalCustomerId}` :
                  '/subscription/manage'
                }
                className="btn-secondary"
              >
                <Settings className="w-4 h-4 mr-2" />
                Manage Subscription
              </button>
              {subscription.cancelAtPeriodEnd && (
                <button className="btn-secondary">
                  <X className="w-4 h-4 mr-2" />
                  Cancel at Period End
                </button>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Available Tiers */}
      <div className="space-y-6">
        {/* BUG-018 & BUG-019 FIX: Improved responsive layout for billing toggle */}
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
          <h2 className="text-2xl font-bold text-foreground">
            Choose Your Plan
          </h2>
          <div className="flex items-center gap-2 sm:gap-4">
            <span className="text-sm text-muted-foreground whitespace-nowrap">Billing Cycle:</span>
            <button
              onClick={() => setBillingCycle(BillingCycle.Monthly)}
              className={`px-3 py-1 rounded-lg text-sm font-medium transition-colors whitespace-nowrap ${
                billingCycle === BillingCycle.Monthly
                  ? 'bg-primary text-primary-foreground'
                  : 'bg-secondary text-secondary-foreground hover:bg-secondary/80'
              }`}
            >
              Monthly
            </button>
            <button
              onClick={() => setBillingCycle(BillingCycle.Annual)}
              className={`px-3 py-1 rounded-lg text-sm font-medium transition-colors flex items-center gap-1 ${
                billingCycle === BillingCycle.Annual
                  ? 'bg-primary text-primary-foreground'
                  : 'bg-secondary text-secondary-foreground hover:bg-secondary/80'
              }`}
            >
              <span>Annual</span>
              <span className={`text-xs whitespace-nowrap ${billingCycle === BillingCycle.Annual ? 'text-primary-foreground/80' : 'text-primary'}`}>(Save 20%)</span>
            </button>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {tiers.map((tier) => (
            <div
              key={tier.id}
              className={`card-interactive p-6 space-y-4 relative ${
                isSubscribed(tier.id)
                  ? 'ring-2 ring-primary/50 bg-primary/5'
                  : ''
              }`}
            >
              {/* Popular Badge - BUG-019 FIX: Adjusted positioning to prevent truncation on mobile */}
              {tier.name.toLowerCase().includes('professional') && (
                <div className="absolute -top-2 right-2 sm:-right-2 z-10">
                  <span className="status-warning whitespace-nowrap">Popular</span>
                </div>
              )}

              {/* Header */}
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <div className={`p-2 rounded-lg ${
                    tier.name.toLowerCase().includes('enterprise') ? 'bg-gradient-to-br from-primary/20 to-primary/10' :
                    tier.name.toLowerCase().includes('professional') ? 'bg-gradient-to-br from-info/20 to-info/10' :
                    'bg-gradient-to-br from-success/20 to-success/10'
                  }`}>
                    {getTierIcon(tier.name)}
                  </div>
                  <div>
                    <h3 className="text-xl font-bold text-foreground">
                      {tier.name}
                    </h3>
                    {getTrialBadge(tier)}
                  </div>
                </div>
              </div>

              {/* Price */}
              <div className="space-y-2">
                <div className="flex items-baseline gap-1">
                  <span className="text-3xl font-bold text-foreground">
                    {formatPrice(tier.price, billingCycle === BillingCycle.Annual)}
                  </span>
                </div>
                {tier.annualPrice && (
                  <p className="text-sm text-muted-foreground line-through">
                    {formatPrice(tier.annualPrice, false)}
                  </p>
                )}
              </div>

              {/* Features */}
              <div className="space-y-3">
                {/* BUG-015 & BUG-016 FIX: Use formatLimitLabel for proper singular/plural handling */}
                <div className="space-y-2">
                  <div className="flex items-center gap-2">
                    <Check className="w-4 h-4 text-success" />
                    <span className="text-sm text-foreground">
                      {formatLimitLabel(tier.maxActiveProjects, 'active project', 'active projects')}
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Check className="w-4 h-4 text-success" />
                    <span className="text-sm text-foreground">
                      {formatLimitLabel(tier.maxTeamMembers, 'team member', 'team members')}
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Check className="w-4 h-4 text-success" />
                    <span className="text-sm text-foreground">
                      {formatLimitValue(tier.maxMonthlyEarnings, '$')} monthly earnings limit
                    </span>
                  </div>
                </div>

                {/* Additional Features */}
                {tier.features && tier.features.length > 0 && (
                  <div className="space-y-1">
                    {tier.features.slice(0, 3).map((feature, index) => (
                      <div key={index} className="flex items-center gap-2">
                        <Check className="w-3 h-3 text-success" />
                        {/* BUG-010 FIX: Format feature labels for human readability */}
                        <span className="text-xs text-muted-foreground">{formatFeatureLabel(feature)}</span>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* CTA Button */}
              <div className="space-y-3 pt-4">
                {isSubscribed(tier.id) ? (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-success font-medium">
                      <Check className="w-4 h-4 mr-1" />
                      Subscribed
                    </span>
                    <button
                      onClick={() => router.push('/subscription/change-tier')}
                      className="btn-secondary text-sm"
                    >
                      Change Plan
                    </button>
                  </div>
                ) : (
                  <button
                    onClick={async () => {
                      if (isUpgrade(tier.id)) {
                        // Show upgrade confirmation dialog
                        if (confirm(`Are you sure you want to upgrade to ${tier.name}?`)) {
                          const result = await handleCreateSubscription(tier.id)
                          if (result.success && result.sessionUrl) {
                            window.location.href = result.sessionUrl
                          }
                        }
                      } else {
                        const result = await handleCreateSubscription(tier.id)
                        if (result.success && result.sessionUrl) {
                          window.location.href = result.sessionUrl
                        }
                      }
                    }}
                    className={`w-full ${
                      isUpgrade(tier.id)
                        ? 'btn-secondary'
                        : 'btn-primary'
                    }`}
                  >
                    {isUpgrade(tier.id) ? 'Upgrade' : 'Get Started'}
                    {isUpgrade(tier.id) && <ChevronRight className="w-4 h-4 ml-2" />}
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Payment Methods Section */}
      <div className="card-elevated p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-xl font-bold text-foreground">
            Payment Methods
          </h2>
          <button
            onClick={async () => {
              setIsManagingPayment(true)
              try {
                await handleManagePaymentMethods()
              } finally {
                setIsManagingPayment(false)
              }
            }}
            className="btn-secondary"
            disabled={isManagingPayment}
          >
            <CreditCard className="w-4 h-4 mr-2" />
            Add Payment Method
          </button>
        </div>

        {isLoadingPaymentMethods ? (
          <div className="text-center py-8">
            <Loader2 className="w-8 h-8 animate-spin text-primary mx-auto" />
            <p className="text-sm text-muted-foreground mt-2">Loading payment methods...</p>
          </div>
        ) : paymentMethods.length === 0 ? (
          <div className="text-center py-8 space-y-3">
            <CreditCard className="w-12 h-12 text-muted-foreground mx-auto" />
            <p className="text-muted-foreground">No payment methods added</p>
            <p className="text-sm text-muted-foreground/70">
              Add a payment method to upgrade your subscription
            </p>
            {subscription && (
              <button
                onClick={syncPaymentMethods}
                disabled={isSyncingPaymentMethods}
                className="btn-ghost text-sm flex items-center gap-2 mx-auto mt-4"
              >
                {isSyncingPaymentMethods ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <RefreshCw className="w-4 h-4" />
                )}
                Sync from Stripe
              </button>
            )}
          </div>
        ) : (
          <div className="space-y-3">
            {paymentMethods.map((method) => (
              <div
                key={method.id}
                className="flex items-center justify-between p-4 border border-border/50 rounded-lg"
              >
                <div className="flex items-center gap-4">
                  <div className="p-2 bg-muted rounded-lg">
                    <CreditCard className="w-6 h-6" />
                  </div>
                  <div>
                    <p className="text-sm font-medium text-foreground">
                      {method.brand} ending in {method.last4}
                    </p>
                    {method.isDefault && (
                      <span className="status-success text-xs">Default</span>
                    )}
                  </div>
                </div>
                <div className="flex gap-2">
                  {!method.isDefault && (
                    <button
                      onClick={() => handleSetDefaultPaymentMethod(method.id)}
                      disabled={isSettingDefaultPaymentMethod === method.id}
                      className="btn-secondary text-sm disabled:opacity-50"
                    >
                      {isSettingDefaultPaymentMethod === method.id ? 'Setting...' : 'Set as Default'}
                    </button>
                  )}
                  <button
                    onClick={() => handleRemovePaymentMethod(method.id)}
                    disabled={isRemovingPaymentMethod === method.id || method.isDefault}
                    className="btn-ghost text-sm text-destructive disabled:opacity-50 disabled:cursor-not-allowed"
                    title={method.isDefault ? 'Cannot remove default payment method' : 'Remove payment method'}
                  >
                    {isRemovingPaymentMethod === method.id ? 'Removing...' : 'Remove'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Billing History */}
      <div className="card-elevated p-6">
        <h2 className="text-xl font-bold text-foreground mb-6">
          Billing History
        </h2>
        <div className="text-center py-8 space-y-3">
          <AlertCircle className="w-12 h-12 text-muted-foreground mx-auto" />
          <p className="text-muted-foreground">No billing history yet</p>
          <p className="text-sm text-muted-foreground/70">
            Your billing transactions will appear here
          </p>
        </div>
      </div>

      {/* Help Section */}
      <div className="card-ghost p-6">
        <div className="text-center space-y-4">
          <div className="flex items-center justify-center gap-2 text-muted-foreground">
            <AlertCircle className="w-5 h-5" />
            <span className="text-sm">Need help?</span>
          </div>
          <p className="text-sm text-muted-foreground/70">
            Contact our support team for assistance with your subscription
          </p>
          <button className="btn-ghost text-sm">
            Contact Support
          </button>
        </div>
      </div>
    </div>
  )
}