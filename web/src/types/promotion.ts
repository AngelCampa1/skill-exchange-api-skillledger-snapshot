/**
 * Stripe Promotion System Types for SkillLedger Frontend
 * Based on the backend PromotionDtos and Stripe API responses
 */

// Coupon Types
export interface CreateCouponRequest {
  id: string;
  name: string;
  percentOff?: number;
  amountOffCents?: number;
  currency?: string;
  duration: CouponDuration;
  durationInMonths?: number;
  maxRedemptions?: number;
  redeemBy?: string;
  appliesTo?: string[];
  metadata?: Record<string, string>;
}

export type CouponDuration = 'once' | 'repeating' | 'forever';

export interface StripeCouponResult {
  id: string;
  name: string;
  percentOff?: number;
  amountOff?: number;
  currency?: string;
  duration: CouponDuration;
  durationInMonths?: number;
  maxRedemptions?: number;
  timesRedeemed: number;
  remainingRedemptions: number;
  isActive: boolean;
  redeemBy?: string;
  created: string;
  metadata?: Record<string, string>;
}

// Promotion Code Types
export interface CreatePromoCodeRequest {
  couponId: string;
  code?: string;
  maxRedemptions?: number;
  expiresAt?: string;
  firstTimeTransactionOnly?: boolean;
  minimumAmountCents?: number;
  customerId?: string;
  metadata?: Record<string, string>;
}

export interface StripePromoCodeResult {
  id: string;
  code: string;
  couponId: string;
  coupon?: StripeCouponResult;
  isActive: boolean;
  maxRedemptions?: number;
  timesRedeemed: number;
  remainingRedemptions: number;
  expiresAt?: string;
  firstTimeTransactionOnly: boolean;
  minimumAmount?: number;
  customerId?: string;
  created: string;
  metadata?: Record<string, string>;
}

// Validation Types
export interface PromoValidationResult {
  isValid: boolean;
  code?: string;
  couponId?: string;
  discountDescription?: string;
  percentOff?: number;
  amountOff?: number;
  duration?: CouponDuration;
  durationInMonths?: number;
  errorMessage?: string;
  errorCode?: string;
}

// Statistics Types
export interface CouponStatsResult {
  couponId: string;
  couponName: string;
  timesRedeemed: number;
  maxRedemptions?: number;
  remainingRedemptions: number;
  totalDiscountGiven: number;
  currency: string;
  isActive: boolean;
  recentRedemptions: RedemptionInfo[];
}

export interface RedemptionInfo {
  customerId: string;
  customerEmail?: string;
  subscriptionId: string;
  redeemedAt: string;
  discountAmount: number;
}

export interface PromotionStatsResult {
  totalActiveCoupons: number;
  totalActivePromoCodes: number;
  totalRedemptions: number;
  totalDiscountGiven: number;
  currency: string;
  topCoupons: CouponSummary[];
}

export interface CouponSummary {
  couponId: string;
  couponName: string;
  timesRedeemed: number;
  totalDiscountGiven: number;
}

// Subscription with Promotion Info
export interface SubscriptionPromotionInfo {
  appliedCouponId?: string;
  appliedPromoCode?: string;
  discountEndsAt?: string;
  percentOff?: number;
  amountOff?: number;
  duration?: CouponDuration;
  durationInMonths?: number;
}

// API Response helpers
export interface PromotionApiResponse<T> {
  data?: T;
  message?: string;
  success: boolean;
}

// Launch Promotion Helper Types
export interface LaunchPromotionConfig {
  couponId: string;
  couponName: string;
  percentOff: number;
  durationInMonths: number;
  maxRedemptions: number;
  promoCode?: string;
  firstTimeOnly?: boolean;
}

// Default launch promotion configuration
export const DEFAULT_LAUNCH_PROMOTION: LaunchPromotionConfig = {
  couponId: 'launch_3mo_free',
  couponName: 'Launch Promotion - 3 Months Free',
  percentOff: 100,
  durationInMonths: 3,
  maxRedemptions: 100,
  promoCode: 'LAUNCH2024',
  firstTimeOnly: true,
};
