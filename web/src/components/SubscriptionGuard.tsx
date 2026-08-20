'use client'

import { ReactNode, useState, useEffect } from 'react'
import Link from 'next/link'
import {
  Lock,
  Crown,
  Star,
  AlertCircle,
  Zap,
  ArrowRight
} from 'lucide-react'
import { SubscriptionGuardOptions, useSubscriptionGuard } from '@/hooks/useSubscriptionGuard'

interface SubscriptionGuardProps {
  children: ReactNode
  options?: SubscriptionGuardOptions
  fallback?: ReactNode
  showUpgradePrompt?: boolean
  className?: string
}

interface SubscriptionGuardFallbackProps {
  reason?: string
  upgradeRequired?: boolean
  redirectToUpgrade: () => void
  customMessage?: string
  showUpgradePrompt?: boolean
}

export function SubscriptionGuardFallback({
  reason,
  upgradeRequired,
  redirectToUpgrade,
  customMessage,
  showUpgradePrompt = true
}: SubscriptionGuardFallbackProps) {
  if (customMessage) {
    return (
      <div className="card-error p-6 text-center">
        <AlertCircle className="w-12 h-12 text-error mx-auto mb-4" />
        <h3 className="text-subheading text-foreground mb-2">Access Restricted</h3>
        <p className="text-body text-muted-foreground">{customMessage}</p>
      </div>
    )
  }

  return (
    <div className="card-interactive p-8 text-center space-y-6">
      <div className="inline-flex p-4 bg-gradient-to-br from-primary/20 to-secondary/20 rounded-2xl">
        <Lock className="w-8 h-8 text-primary" />
      </div>

      <div className="space-y-3">
        <h3 className="text-2xl font-black text-foreground">
          Premium Feature
        </h3>

        <p className="text-body text-muted-foreground">
          {reason || 'This feature requires an active subscription'}
        </p>
      </div>

      {upgradeRequired && showUpgradePrompt && (
        <div className="space-y-4">
          <div className="bg-muted/50 rounded-xl p-6 border border-border/50">
            <h4 className="text-subheading text-foreground mb-3">Unlock This Feature</h4>
            <p className="text-body text-muted-foreground text-sm mb-4">
              Upgrade to a premium plan to access this feature and many more powerful tools for your professional collaboration needs.
            </p>

            <div className="space-y-3 text-left">
              <div className="flex items-center space-golden-sm text-sm">
                <Star className="w-4 h-4 text-success" />
                <span className="text-foreground">Unlimited projects and collaborations</span>
              </div>
              <div className="flex items-center space-golden-sm text-sm">
                <Star className="w-4 h-4 text-success" />
                <span className="text-foreground">Advanced analytics and reporting</span>
              </div>
              <div className="flex items-center space-golden-sm text-sm">
                <Star className="w-4 h-4 text-success" />
                <span className="text-foreground">Priority customer support</span>
              </div>
              <div className="flex items-center space-golden-sm text-sm">
                <Star className="w-4 h-4 text-success" />
                <span className="text-foreground">API access and integrations</span>
              </div>
            </div>
          </div>

          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <button
              onClick={redirectToUpgrade}
              className="btn-primary flex items-center justify-center space-golden-sm"
            >
              <Crown className="w-4 h-4" />
              Upgrade Now
              <ArrowRight className="w-4 h-4" />
            </button>

            <Link
              href="/subscription"
              className="btn-secondary flex items-center justify-center space-golden-sm"
            >
              <Zap className="w-4 h-4" />
              View All Plans
            </Link>
          </div>
        </div>
      )}
    </div>
  )
}

export function SubscriptionGuard({
  children,
  options,
  fallback,
  showUpgradePrompt = true,
  className = ''
}: SubscriptionGuardProps) {
  const {
    canAccess,
    isLoading,
    error,
    reason,
    upgradeRequired,
    redirectToUpgrade
  } = useSubscriptionGuard(options)

  if (isLoading) {
    return (
      <div className={`flex flex-col items-center justify-center py-12 ${className}`}>
        <div className="loading-spinner mx-auto animate-glow mb-4"></div>
        <p className="text-body text-muted-foreground">Verifying access...</p>
      </div>
    )
  }

  if (error) {
    return (
      <div className={`card-error p-6 ${className}`}>
        <div className="flex items-center space-golden-sm">
          <AlertCircle className="w-5 h-5 text-error" />
          <span className="text-body text-error">Error verifying subscription: {error}</span>
        </div>
      </div>
    )
  }

  if (canAccess) {
    return <>{children}</>
  }

  if (fallback) {
    return <>{fallback}</>
  }

  return (
    <div className={className}>
      <SubscriptionGuardFallback
        reason={reason}
        upgradeRequired={upgradeRequired}
        redirectToUpgrade={redirectToUpgrade}
        showUpgradePrompt={showUpgradePrompt}
      />
    </div>
  )
}

// BUG-003 FIX: Profile completion check for project creation
function useProfileCheck() {
  const [isProfileComplete, setIsProfileComplete] = useState<boolean | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const checkProfile = async () => {
      try {
        const response = await fetch('/api/profile/me', {
          credentials: 'include',
        })

        if (response.ok) {
          const data = await response.json()
          // Check if profile exists and has required fields
          const profile = data.profile || data
          const hasProfile = !!(profile && profile.id)
          const hasRequiredFields = !!(
            profile?.firstName &&
            profile?.lastName &&
            (profile?.skills?.length >= 1 || profile?.userSkills?.length >= 1)
          )
          setIsProfileComplete(hasProfile && hasRequiredFields)
        } else if (response.status === 404) {
          setIsProfileComplete(false)
        } else {
          // Don't block on error - allow access
          setIsProfileComplete(true)
        }
      } catch {
        // Don't block on error - allow access
        setIsProfileComplete(true)
      } finally {
        setIsLoading(false)
      }
    }

    checkProfile()
  }, [])

  return { isProfileComplete, isLoading }
}

// Specific guard components for common scenarios
export function ProjectCreationGuard({ children }: { children: ReactNode }) {
  const { isProfileComplete, isLoading: profileLoading } = useProfileCheck()

  // BUG-003 FIX: Check profile completion first
  if (profileLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-12">
        <div className="loading-spinner mx-auto animate-glow mb-4"></div>
        <p className="text-body text-muted-foreground">Checking profile status...</p>
      </div>
    )
  }

  if (!isProfileComplete) {
    return (
      <div className="card-elevated p-8 text-center max-w-xl mx-auto">
        <div className="inline-flex p-4 bg-gradient-to-br from-warning/20 to-warning/10 rounded-2xl mb-4">
          <AlertCircle className="w-8 h-8 text-warning" />
        </div>
        <h3 className="text-2xl font-bold text-foreground mb-2">Complete Your Profile First</h3>
        <p className="text-body text-muted-foreground mb-6">
          Before creating a project, you need to complete your profile with your basic information and at least one skill.
        </p>
        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <Link href="/profile/create" className="btn-primary">
            Complete Profile
          </Link>
          <Link href="/" className="btn-secondary">
            Return to Dashboard
          </Link>
        </div>
      </div>
    )
  }

  return (
    <SubscriptionGuard
      options={{
        maxProjects: 1 // Check if user can create more projects
      }}
      fallback={
        <div className="card-error p-6 text-center">
          <h3 className="text-subheading text-foreground mb-2">Project Limit Reached</h3>
          <p className="text-body text-muted-foreground mb-4">
            You've reached the maximum number of projects for your current plan.
          </p>
          <Link href="/subscription" className="btn-primary">
            Upgrade to Create More Projects
          </Link>
        </div>
      }
    >
      {children}
    </SubscriptionGuard>
  )
}

export function AdvancedFeaturesGuard({ children }: { children: ReactNode }) {
  return (
    <SubscriptionGuard
      options={{
        requiredFeatures: ['advancedAnalytics', 'apiAccess']
      }}
      fallback={
        <div className="card-error p-6 text-center">
          <h3 className="text-subheading text-foreground mb-2">Premium Feature</h3>
          <p className="text-body text-muted-foreground mb-4">
            This feature requires an Advanced plan or higher
          </p>
          <a href="/subscription" className="btn-primary">
            Upgrade Plan
          </a>
        </div>
      }
    >
      {children}
    </SubscriptionGuard>
  )
}

export function ApiAccessGuard({ children }: { children: ReactNode }) {
  return (
    <SubscriptionGuard
      options={{
        requiredFeatures: ['apiAccess']
      }}
      fallback={
        <div className="card-error p-6 text-center">
          <h3 className="text-subheading text-foreground mb-2">API Access Required</h3>
          <p className="text-body text-muted-foreground mb-4">
            API access requires a Professional plan or higher
          </p>
          <a href="/subscription" className="btn-primary">
            Upgrade Plan
          </a>
        </div>
      }
    >
      {children}
    </SubscriptionGuard>
  )
}

export function EnterpriseGuard({ children }: { children: ReactNode }) {
  return (
    <SubscriptionGuard
      options={{
        requiredTier: 'Enterprise'
      }}
      fallback={
        <div className="card-error p-6 text-center">
          <h3 className="text-subheading text-foreground mb-2">Enterprise Feature</h3>
          <p className="text-body text-muted-foreground mb-4">
            This feature is available only with an Enterprise plan
          </p>
          <a href="/subscription" className="btn-primary">
            Contact Sales
          </a>
        </div>
      }
    >
      {children}
    </SubscriptionGuard>
  )
}