/**
 * Subscription API service for SkillLedger frontend
 * Handles all subscription-related API calls to the backend
 */

import { useRef } from 'react'
import { logger } from '../utils/logger'
import {
  SubscriptionTier,
  UserSubscription,
  CheckoutSessionResult,
  CreateSubscriptionRequest,
  PaymentMethodSetupRequest,
  PaymentMethod,
  BillingHistory,
  SubscriptionUsage,
  ApiResponse,
  BillingCycle
} from '@/types/subscription'
import { AUTH_CONFIG } from '@/constants/auth'

/**
 * Helper to make authenticated API requests
 * Uses Next.js API proxy to route requests to the backend
 */
async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  // Use Next.js proxy at /api/* which routes to the backend
  // This ensures consistent handling of cookies, CORS, and auth across all environments
  const response = await fetch(`/api${endpoint}`, {
    credentials: AUTH_CONFIG.CREDENTIALS,
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
    ...options,
  })

  if (!response.ok) {
    const errorText = await response.text()
    throw new Error(`API Error: ${response.status} - ${errorText}`)
  }

  const data = await response.json()
  return {
    success: true,
    data,
    message: 'Success'
  }
}

/**
 * Get current user's subscription
 */
export async function getUserSubscription(): Promise<UserSubscription | null> {
  try {
    const response = await apiRequest<UserSubscription>('/subscription/current')
    return response.data || null
  } catch (error) {
    logger.error('Error fetching user subscription', error, { api: 'subscription' })
    return null
  }
}

/**
 * Get available subscription tiers
 */
export async function getSubscriptionTiers(): Promise<SubscriptionTier[]> {
  try {
    const response = await apiRequest<SubscriptionTier[]>('/subscription/tiers')
    return response.data || []
  } catch (error) {
    logger.error('Error fetching subscription tiers', error, { api: 'subscription' })
    return []
  }
}

/**
 * Create a subscription checkout session
 */
export async function createSubscriptionCheckout(
  request: CreateSubscriptionRequest
): Promise<CheckoutSessionResult> {
  try {
    const response = await apiRequest<CheckoutSessionResult>('/checkout/create-subscription', {
      method: 'POST',
      body: JSON.stringify(request),
    })
    return response.data!
  } catch (error) {
    logger.error('Error creating subscription checkout', error, { api: 'subscription' })
    throw error
  }
}

/**
 * Create a payment method setup session
 */
export async function createPaymentMethodSetup(
  request: PaymentMethodSetupRequest
): Promise<CheckoutSessionResult> {
  try {
    const response = await apiRequest<CheckoutSessionResult>('/checkout/setup-payment-method', {
      method: 'POST',
      body: JSON.stringify(request),
    })
    return response.data!
  } catch (error) {
    logger.error('Error creating payment method setup', error, { api: 'subscription' })
    throw error
  }
}

/**
 * Get user's payment methods
 */
export async function getPaymentMethods(): Promise<PaymentMethod[]> {
  try {
    const response = await apiRequest<PaymentMethod[]>('/subscription/payment-methods')
    return response.data || []
  } catch (error) {
    logger.error('Error fetching payment methods', error, { api: 'subscription' })
    return []
  }
}

/**
 * Get billing history
 */
export async function getBillingHistory(): Promise<BillingHistory[]> {
  try {
    const response = await apiRequest<BillingHistory[]>('/subscription/billing-history')
    return response.data || []
  } catch (error) {
    logger.error('Error fetching billing history', error, { api: 'subscription' })
    return []
  }
}

/**
 * Get subscription usage statistics
 */
export async function getSubscriptionUsage(): Promise<SubscriptionUsage | null> {
  try {
    const response = await apiRequest<SubscriptionUsage>('/subscription/usage')
    return response.data || null
  } catch (error) {
    logger.error('Error fetching subscription usage', error, { api: 'subscription' })
    return null
  }
}

/**
 * Cancel subscription
 */
export async function cancelSubscription(reason?: string): Promise<boolean> {
  try {
    await apiRequest('/subscription/cancel', {
      method: 'POST',
      body: JSON.stringify({ reason }),
    })
    return true
  } catch (error) {
    logger.error('Error canceling subscription', error, { api: 'subscription' })
    return false
  }
}

/**
 * Upgrade/downgrade subscription tier
 */
export async function changeSubscriptionTier(
  newTierId: string,
  immediateCharge = false
): Promise<boolean> {
  try {
    await apiRequest('/subscription/change-tier', {
      method: 'POST',
      body: JSON.stringify({
        newTierId,
        immediateCharge
      }),
    })
    return true
  } catch (error) {
    logger.error('Error changing subscription tier', error, { api: 'subscription' })
    return false
  }
}

/**
 * React hook for subscription data
 */
export function useSubscription() {
  const [subscription, setSubscription] = useState<UserSubscription | null>(null)
  const [tiers, setTiers] = useState<SubscriptionTier[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const hasLoadedRef = useRef(false)

  const loadSubscriptionData = useCallback(async () => {
    // Prevent duplicate calls
    if (hasLoadedRef.current) {
      return
    }
    hasLoadedRef.current = true

    try {
      setLoading(true)
      setError(null)

      const [subscriptionData, tiersData] = await Promise.all([
        getUserSubscription(),
        getSubscriptionTiers()
      ])

      setSubscription(subscriptionData)
      setTiers(tiersData)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load subscription data')
      // Reset ref on error to allow retry
      hasLoadedRef.current = false
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    loadSubscriptionData()
  }, [loadSubscriptionData])

  const createCheckout = useCallback(async (tierId: string, billingCycle: BillingCycle) => {
    const request: CreateSubscriptionRequest = {
      tierId,
      billingCycle,
      successUrl: `${window.location.origin}/dashboard?subscription_success=true`,
      cancelUrl: `${window.location.origin}/subscription/choose-plan`,
    }

    return await createSubscriptionCheckout(request)
  }, [])

  const setupPaymentMethod = useCallback(async () => {
    const request: PaymentMethodSetupRequest = {
      successUrl: `${window.location.origin}/dashboard?payment_method_setup=true`,
      cancelUrl: `${window.location.origin}/dashboard?payment_method_canceled=true`,
      setAsDefault: true,
    }

    return await createPaymentMethodSetup(request)
  }, [])

  return {
    subscription,
    tiers,
    loading,
    error,
    createCheckout,
    setupPaymentMethod,
    refetch: () => {
      // Implement refetch logic
      loadSubscriptionData()
    }
  }
}

// Import React hooks for the hook above
import { useState, useEffect, useCallback } from 'react'