/**
 * Promotion Admin API service for SkillLedger frontend
 * Handles all Stripe promotion-related API calls to the backend
 * These endpoints require Admin role authorization
 */

import { logger } from '../utils/logger'
import {
  CreateCouponRequest,
  StripeCouponResult,
  CreatePromoCodeRequest,
  StripePromoCodeResult,
  PromoValidationResult,
  CouponStatsResult,
  PromotionStatsResult,
} from '@/types/promotion'
import { AUTH_CONFIG } from '@/constants/auth'

/**
 * Helper to make authenticated API requests
 * Uses Next.js API proxy to route requests to the backend
 * BUG-API-018 FIX: Standardized to match subscription-api.ts pattern
 * Endpoints should NOT include the /api prefix (e.g., /admin/promotions/...)
 */
async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  // BUG-API-018 FIX: Add /api prefix consistently like subscription-api.ts
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

  // Handle 204 No Content responses
  if (response.status === 204) {
    return undefined as T
  }

  return await response.json()
}

// ============================================
// Coupon Management Endpoints (Admin Only)
// ============================================

/**
 * Create a new Stripe coupon
 * @requires Admin role
 */
export async function createCoupon(
  request: CreateCouponRequest
): Promise<StripeCouponResult> {
  try {
    return await apiRequest<StripeCouponResult>('/admin/promotions/coupons', {
      method: 'POST',
      body: JSON.stringify(request),
    })
  } catch (error) {
    logger.error('Error creating coupon', error, { api: 'promotion' })
    throw error
  }
}

/**
 * List all coupons
 * @requires Admin role
 */
export async function listCoupons(
  activeOnly = true,
  limit = 100
): Promise<StripeCouponResult[]> {
  try {
    const params = new URLSearchParams({
      activeOnly: activeOnly.toString(),
      limit: limit.toString(),
    })
    return await apiRequest<StripeCouponResult[]>(
      `/admin/promotions/coupons?${params}`
    )
  } catch (error) {
    logger.error('Error listing coupons', error, { api: 'promotion' })
    return []
  }
}

/**
 * Get a specific coupon by ID
 * @requires Admin role
 */
export async function getCoupon(couponId: string): Promise<StripeCouponResult | null> {
  try {
    return await apiRequest<StripeCouponResult>(
      `/admin/promotions/coupons/${encodeURIComponent(couponId)}`
    )
  } catch (error) {
    logger.error('Error getting coupon', error, { api: 'promotion', couponId })
    return null
  }
}

/**
 * Get coupon statistics
 * @requires Admin role
 */
export async function getCouponStats(couponId: string): Promise<CouponStatsResult | null> {
  try {
    return await apiRequest<CouponStatsResult>(
      `/admin/promotions/coupons/${encodeURIComponent(couponId)}/stats`
    )
  } catch (error) {
    logger.error('Error getting coupon stats', error, { api: 'promotion', couponId })
    return null
  }
}

/**
 * Deactivate (delete) a coupon
 * Note: This does not affect customers who have already applied the coupon
 * @requires Admin role
 */
export async function deactivateCoupon(couponId: string): Promise<boolean> {
  try {
    await apiRequest<void>(
      `/admin/promotions/coupons/${encodeURIComponent(couponId)}`,
      { method: 'DELETE' }
    )
    return true
  } catch (error) {
    logger.error('Error deactivating coupon', error, { api: 'promotion', couponId })
    return false
  }
}

// ============================================
// Promotion Code Management Endpoints (Admin Only)
// ============================================

/**
 * Create a new promotion code for an existing coupon
 * @requires Admin role
 */
export async function createPromotionCode(
  request: CreatePromoCodeRequest
): Promise<StripePromoCodeResult> {
  try {
    return await apiRequest<StripePromoCodeResult>('/admin/promotions/codes', {
      method: 'POST',
      body: JSON.stringify(request),
    })
  } catch (error) {
    logger.error('Error creating promotion code', error, { api: 'promotion' })
    throw error
  }
}

/**
 * List all promotion codes
 * @requires Admin role
 */
export async function listPromotionCodes(
  couponId?: string,
  activeOnly = true,
  limit = 100
): Promise<StripePromoCodeResult[]> {
  try {
    const params = new URLSearchParams({
      activeOnly: activeOnly.toString(),
      limit: limit.toString(),
    })
    if (couponId) {
      params.set('couponId', couponId)
    }
    return await apiRequest<StripePromoCodeResult[]>(
      `/admin/promotions/codes?${params}`
    )
  } catch (error) {
    logger.error('Error listing promotion codes', error, { api: 'promotion' })
    return []
  }
}

/**
 * Get a specific promotion code by code string
 * @requires Admin role
 */
export async function getPromotionCode(
  code: string
): Promise<StripePromoCodeResult | null> {
  try {
    return await apiRequest<StripePromoCodeResult>(
      `/admin/promotions/codes/${encodeURIComponent(code)}`
    )
  } catch (error) {
    logger.error('Error getting promotion code', error, { api: 'promotion', code })
    return null
  }
}

/**
 * Deactivate a promotion code
 * @requires Admin role
 */
export async function deactivatePromotionCode(promoCodeId: string): Promise<boolean> {
  try {
    await apiRequest<void>(
      `/admin/promotions/codes/${encodeURIComponent(promoCodeId)}`,
      { method: 'DELETE' }
    )
    return true
  } catch (error) {
    logger.error('Error deactivating promotion code', error, {
      api: 'promotion',
      promoCodeId,
    })
    return false
  }
}

// ============================================
// Validation Endpoints (Public)
// ============================================

/**
 * Validate a promotion code
 * This can be used to check if a code is valid before checkout
 * @note This endpoint is publicly accessible (AllowAnonymous)
 */
export async function validatePromotionCode(
  code: string
): Promise<PromoValidationResult> {
  try {
    return await apiRequest<PromoValidationResult>(
      `/admin/promotions/validate/${encodeURIComponent(code)}`
    )
  } catch (error) {
    logger.error('Error validating promotion code', error, { api: 'promotion', code })
    return {
      isValid: false,
      errorMessage: 'Failed to validate promotion code',
      errorCode: 'VALIDATION_ERROR',
    }
  }
}

// ============================================
// Statistics Endpoints (Admin Only)
// ============================================

/**
 * Get overall promotion statistics
 * @requires Admin role
 */
export async function getPromotionStats(): Promise<PromotionStatsResult | null> {
  try {
    return await apiRequest<PromotionStatsResult>('/admin/promotions/stats')
  } catch (error) {
    logger.error('Error getting promotion stats', error, { api: 'promotion' })
    return null
  }
}

// ============================================
// Helper Functions for Launch Promotion
// ============================================

/**
 * Creates the default launch promotion: "3 months free, limited to 100 redemptions"
 * @requires Admin role
 */
export async function createLaunchPromotion(): Promise<{
  coupon: StripeCouponResult
  promoCode: StripePromoCodeResult
}> {
  // Create the coupon
  const coupon = await createCoupon({
    id: 'launch_3mo_free',
    name: 'Launch Promotion - 3 Months Free',
    percentOff: 100,
    duration: 'repeating',
    durationInMonths: 3,
    maxRedemptions: 100,
  })

  // Create the promotion code
  const promoCode = await createPromotionCode({
    couponId: coupon.id,
    code: 'LAUNCH2024',
    firstTimeTransactionOnly: true,
  })

  return { coupon, promoCode }
}

/**
 * Check remaining slots for the launch promotion
 * @requires Admin role
 */
export async function getLaunchPromotionStatus(): Promise<{
  isActive: boolean
  totalSlots: number
  usedSlots: number
  remainingSlots: number
} | null> {
  const coupon = await getCoupon('launch_3mo_free')
  if (!coupon) {
    return null
  }

  return {
    isActive: coupon.isActive,
    totalSlots: coupon.maxRedemptions || 0,
    usedSlots: coupon.timesRedeemed,
    remainingSlots: coupon.remainingRedemptions,
  }
}
