'use client'

import { useState, useEffect, useCallback } from 'react'
import { useRouter } from 'next/navigation'
import {
  UserSubscription,
  SubscriptionStatus,
  SubscriptionLimits
} from '@/types/subscription'
import { useSubscription } from '@/lib/subscription-api'

export interface SubscriptionGuardOptions {
  requiredTier?: string
  requiredFeatures?: string[]
  maxProjects?: number
  maxTeamMembers?: number
  maxMonthlyEarnings?: number
  redirectTo?: string
  customCheck?: (subscription: UserSubscription | null) => boolean
}

export interface SubscriptionGuardResult {
  canAccess: boolean
  isLoading: boolean
  subscription: UserSubscription | null
  limits: SubscriptionLimits | null
  error: string | null
  reason?: string
  upgradeRequired?: boolean
  redirectToUpgrade: () => void
}

export function useSubscriptionGuard(options: SubscriptionGuardOptions = {}): SubscriptionGuardResult {
  const router = useRouter()
  const [result, setResult] = useState<SubscriptionGuardResult>({
    canAccess: false,
    isLoading: true,
    subscription: null,
    limits: null,
    error: null,
    redirectToUpgrade: () => router.push('/subscription/choose-plan')
  })

  const {
    subscription,
    tiers,
    loading: subscriptionLoading,
    error: subscriptionError
  } = useSubscription()

  // FIX BUG-TEST-043: Destructure options into stable primitives to prevent infinite re-renders
  // The options object gets a new reference on every render, so we extract the primitive values
  const {
    requiredTier,
    requiredFeatures,
    maxProjects,
    maxTeamMembers,
    maxMonthlyEarnings,
    redirectTo,
    customCheck
  } = options

  // Stringify requiredFeatures for stable dependency (array also creates new reference each render)
  const requiredFeaturesKey = requiredFeatures ? JSON.stringify(requiredFeatures) : ''

  const checkAccess = useCallback(() => {
    if (subscriptionLoading) {
      setResult(prev => ({ ...prev, isLoading: true }))
      return
    }

    if (subscriptionError) {
      setResult(prev => ({
        ...prev,
        isLoading: false,
        error: subscriptionError,
        canAccess: false
      }))
      return
    }

    // No active subscription — all plans are paid, redirect to choose-plan
    if (!subscription) {
      setResult(prev => ({
        ...prev,
        isLoading: false,
        canAccess: false,
        limits: null,
        reason: 'Active subscription required',
        upgradeRequired: true
      }))
      return
    }

    // Check subscription status
    if (subscription.status !== SubscriptionStatus.Active && subscription.status !== SubscriptionStatus.Trial) {
      const reason = `Subscription is ${subscription.status.toLowerCase()}`
      setResult(prev => ({
        ...prev,
        isLoading: false,
        canAccess: false,
        reason,
        upgradeRequired: true
      }))
      return
    }

    // Get current tier limits
    const currentTier = tiers.find(t => t.id === subscription.tier?.id)
    if (!currentTier) {
      setResult(prev => ({
        ...prev,
        isLoading: false,
        error: 'Subscription tier not found',
        canAccess: false
      }))
      return
    }

    const limits: SubscriptionLimits = {
      maxActiveProjects: currentTier.maxActiveProjects,
      maxTeamMembers: currentTier.maxTeamMembers,
      maxMonthlyEarnings: currentTier.maxMonthlyEarnings,
      prioritySupport: currentTier.prioritySupport,
      apiAccess: currentTier.apiAccess,
      advancedAnalytics: currentTier.advancedAnalytics,
      advancedFraudDetection: currentTier.advancedFraudDetection,
      multiSignature: currentTier.multiSignature,
      customIntegrations: currentTier.customIntegrations,
      features: currentTier.features || []
    }

    // Apply custom check if provided
    if (customCheck) {
      const canAccess = customCheck(subscription)
      setResult(prev => ({
        ...prev,
        isLoading: false,
        canAccess,
        limits,
        reason: canAccess ? undefined : 'Custom access check failed',
        upgradeRequired: !canAccess
      }));
      return
    }

    // Check required tier
    if (requiredTier) {
      const foundRequiredTier = tiers.find(t => t.name.toLowerCase() === requiredTier.toLowerCase())
      if (foundRequiredTier && currentTier.sortOrder < foundRequiredTier.sortOrder) {
        setResult(prev => ({
          ...prev,
          isLoading: false,
          canAccess: false,
          limits,
          reason: `${requiredTier} plan required`,
          upgradeRequired: true
        }));
        return
      }
    }

    // Check required features
    if (requiredFeatures && requiredFeatures.length > 0) {
      // FIX BUG-TEST-044: Boolean features need early return to avoid being checked against features array
      const missingFeatures = requiredFeatures.filter(feature => {
        // Check boolean property features - return early with result to avoid checking features array
        if (feature === 'prioritySupport') return !limits.prioritySupport
        if (feature === 'apiAccess') return !limits.apiAccess
        if (feature === 'advancedAnalytics') return !limits.advancedAnalytics
        if (feature === 'advancedFraudDetection') return !limits.advancedFraudDetection
        if (feature === 'multiSignature') return !limits.multiSignature
        if (feature === 'customIntegrations') return !limits.customIntegrations
        // For custom features, check the features array
        return !limits.features.includes(feature)
      })

      if (missingFeatures.length > 0) {
        setResult(prev => ({
          ...prev,
          isLoading: false,
          canAccess: false,
          limits,
          reason: `Missing features: ${missingFeatures.join(', ')}`,
          upgradeRequired: true
        }));
        return
      }
    }

    // Check numeric limits
    const limitReasons: string[] = []

    if (maxProjects !== undefined && limits.maxActiveProjects !== -1 && maxProjects > limits.maxActiveProjects) {
      limitReasons.push(`Exceeded project limit (${maxProjects} > ${limits.maxActiveProjects})`)
    }

    if (maxTeamMembers !== undefined && limits.maxTeamMembers !== -1 && maxTeamMembers > limits.maxTeamMembers) {
      limitReasons.push(`Exceeded team member limit (${maxTeamMembers} > ${limits.maxTeamMembers})`)
    }

    if (maxMonthlyEarnings !== undefined && maxMonthlyEarnings > limits.maxMonthlyEarnings) {
      limitReasons.push(`Exceeded monthly earnings limit ($${maxMonthlyEarnings} > $${limits.maxMonthlyEarnings})`)
    }

    if (limitReasons.length > 0) {
      setResult(prev => ({
        ...prev,
        isLoading: false,
        canAccess: false,
        limits,
        reason: limitReasons.join('; '),
        upgradeRequired: true
      }));
      return
    }

    // All checks passed
    setResult(prev => ({
      ...prev,
      isLoading: false,
      canAccess: true,
      limits,
      reason: undefined,
      upgradeRequired: false
    }));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    subscription,
    tiers,
    subscriptionLoading,
    subscriptionError,
    // Use primitives and stringified array instead of options object to prevent infinite re-renders
    requiredTier,
    requiredFeaturesKey, // Stringified requiredFeatures array
    maxProjects,
    maxTeamMembers,
    maxMonthlyEarnings,
    redirectTo,
    customCheck
  ]);

  useEffect(() => {
    checkAccess();
  }, [checkAccess]);

  return result;
}

// Specific hooks for common guard scenarios
export function useProjectCreationGuard() {
  return useSubscriptionGuard({
    maxProjects: 1 // Check if user can create more projects
  })
}

export function useAdvancedFeaturesGuard() {
  return useSubscriptionGuard({
    requiredFeatures: ['advancedAnalytics', 'apiAccess']
  })
}

export function useApiAccessGuard() {
  return useSubscriptionGuard({
    requiredFeatures: ['apiAccess']
  })
}

export function useUnlimitedProjectsGuard() {
  return useSubscriptionGuard({
    maxProjects: 999 // Effectively checks for unlimited
  })
}