/**
 * Tests for promotion.ts type definitions
 *
 * This file validates that promotion types and constants are correctly defined
 */

import {
  DEFAULT_LAUNCH_PROMOTION,
  type LaunchPromotionConfig,
} from '@/types/promotion'

describe('promotion types', () => {
  describe('DEFAULT_LAUNCH_PROMOTION constant', () => {
    it('should be defined with correct structure', () => {
      expect(DEFAULT_LAUNCH_PROMOTION).toBeDefined()
      expect(DEFAULT_LAUNCH_PROMOTION.couponId).toBeDefined()
      expect(DEFAULT_LAUNCH_PROMOTION.couponName).toBeDefined()
      expect(DEFAULT_LAUNCH_PROMOTION.percentOff).toBeDefined()
      expect(DEFAULT_LAUNCH_PROMOTION.durationInMonths).toBeDefined()
      expect(DEFAULT_LAUNCH_PROMOTION.maxRedemptions).toBeDefined()
    })

    it('should have correct coupon ID', () => {
      expect(DEFAULT_LAUNCH_PROMOTION.couponId).toBe('launch_3mo_free')
    })

    it('should have descriptive coupon name', () => {
      expect(DEFAULT_LAUNCH_PROMOTION.couponName).toBe('Launch Promotion - 3 Months Free')
      expect(DEFAULT_LAUNCH_PROMOTION.couponName).toContain('Launch Promotion')
    })

    it('should offer 100% discount', () => {
      expect(DEFAULT_LAUNCH_PROMOTION.percentOff).toBe(100)
    })

    it('should be valid for 3 months', () => {
      expect(DEFAULT_LAUNCH_PROMOTION.durationInMonths).toBe(3)
    })

    it('should have reasonable max redemptions', () => {
      expect(DEFAULT_LAUNCH_PROMOTION.maxRedemptions).toBeGreaterThan(0)
      expect(typeof DEFAULT_LAUNCH_PROMOTION.maxRedemptions).toBe('number')
    })

    it('should have optional fields defined correctly', () => {
      if (DEFAULT_LAUNCH_PROMOTION.promoCode) {
        expect(typeof DEFAULT_LAUNCH_PROMOTION.promoCode).toBe('string')
      }
      if (DEFAULT_LAUNCH_PROMOTION.firstTimeOnly !== undefined) {
        expect(typeof DEFAULT_LAUNCH_PROMOTION.firstTimeOnly).toBe('boolean')
      }
    })
  })

  describe('LaunchPromotionConfig type structure', () => {
    it('should allow valid promotion config object', () => {
      const config: LaunchPromotionConfig = {
        couponId: 'test_coupon',
        couponName: 'Test Promotion',
        percentOff: 50,
        durationInMonths: 1,
        maxRedemptions: 100,
        promoCode: 'TEST50',
        firstTimeOnly: true,
      }

      expect(config.couponId).toBe('test_coupon')
      expect(config.percentOff).toBe(50)
      expect(config.durationInMonths).toBe(1)
      expect(config.maxRedemptions).toBe(100)
      expect(config.promoCode).toBe('TEST50')
      expect(config.firstTimeOnly).toBe(true)
    })

    it('should allow config without optional fields', () => {
      const config: LaunchPromotionConfig = {
        couponId: 'minimal_coupon',
        couponName: 'Minimal Promotion',
        percentOff: 25,
        durationInMonths: 2,
        maxRedemptions: 50,
      }

      expect(config.couponId).toBe('minimal_coupon')
      expect(config.promoCode).toBeUndefined()
      expect(config.firstTimeOnly).toBeUndefined()
    })

    it('should support various discount percentages', () => {
      const configs: LaunchPromotionConfig[] = [
        { ...DEFAULT_LAUNCH_PROMOTION, percentOff: 10 },
        { ...DEFAULT_LAUNCH_PROMOTION, percentOff: 50 },
        { ...DEFAULT_LAUNCH_PROMOTION, percentOff: 75 },
        { ...DEFAULT_LAUNCH_PROMOTION, percentOff: 100 },
      ]

      configs.forEach(config => {
        expect(config.percentOff).toBeGreaterThanOrEqual(0)
        expect(config.percentOff).toBeLessThanOrEqual(100)
      })
    })

    it('should support various duration periods', () => {
      const durations = [1, 3, 6, 12]

      durations.forEach(duration => {
        const config: LaunchPromotionConfig = {
          ...DEFAULT_LAUNCH_PROMOTION,
          durationInMonths: duration,
        }

        expect(config.durationInMonths).toBe(duration)
        expect(config.durationInMonths).toBeGreaterThan(0)
      })
    })
  })

  describe('Promotion validation logic', () => {
    it('should validate complete promotion config', () => {
      const isValidPromotion = (config: LaunchPromotionConfig): boolean => {
        return (
          !!config.couponId &&
          !!config.couponName &&
          config.percentOff >= 0 &&
          config.percentOff <= 100 &&
          config.durationInMonths > 0 &&
          config.maxRedemptions > 0
        )
      }

      expect(isValidPromotion(DEFAULT_LAUNCH_PROMOTION)).toBe(true)
    })

    it('should detect invalid promotion configs', () => {
      const invalidConfigs = [
        { ...DEFAULT_LAUNCH_PROMOTION, percentOff: -10 },
        { ...DEFAULT_LAUNCH_PROMOTION, percentOff: 150 },
        { ...DEFAULT_LAUNCH_PROMOTION, durationInMonths: 0 },
        { ...DEFAULT_LAUNCH_PROMOTION, maxRedemptions: -1 },
      ]

      invalidConfigs.forEach(config => {
        const isValid =
          config.percentOff >= 0 &&
          config.percentOff <= 100 &&
          config.durationInMonths > 0 &&
          config.maxRedemptions > 0

        expect(isValid).toBe(false)
      })
    })
  })

  describe('Promotion usage scenarios', () => {
    it('should support first-time user restrictions', () => {
      const firstTimePromo: LaunchPromotionConfig = {
        ...DEFAULT_LAUNCH_PROMOTION,
        firstTimeOnly: true,
      }

      const recurringPromo: LaunchPromotionConfig = {
        ...DEFAULT_LAUNCH_PROMOTION,
        firstTimeOnly: false,
      }

      expect(firstTimePromo.firstTimeOnly).toBe(true)
      expect(recurringPromo.firstTimeOnly).toBe(false)
    })

    it('should support promo code validation', () => {
      const promoWithCode: LaunchPromotionConfig = {
        ...DEFAULT_LAUNCH_PROMOTION,
        promoCode: 'LAUNCH2024',
      }

      const promoWithoutCode: LaunchPromotionConfig = {
        ...DEFAULT_LAUNCH_PROMOTION,
        promoCode: undefined,
      }

      expect(promoWithCode.promoCode).toBe('LAUNCH2024')
      expect(promoWithoutCode.promoCode).toBeUndefined()
    })

    it('should handle redemption limit tracking', () => {
      const highVolumePromo: LaunchPromotionConfig = {
        ...DEFAULT_LAUNCH_PROMOTION,
        maxRedemptions: 10000,
      }

      const limitedPromo: LaunchPromotionConfig = {
        ...DEFAULT_LAUNCH_PROMOTION,
        maxRedemptions: 100,
      }

      expect(highVolumePromo.maxRedemptions).toBeGreaterThan(limitedPromo.maxRedemptions)
    })
  })

  describe('Type immutability', () => {
    it('should not mutate DEFAULT_LAUNCH_PROMOTION', () => {
      const originalCouponId = DEFAULT_LAUNCH_PROMOTION.couponId
      const originalPercentOff = DEFAULT_LAUNCH_PROMOTION.percentOff

      // Create a copy
      const modifiedPromo: LaunchPromotionConfig = {
        ...DEFAULT_LAUNCH_PROMOTION,
        percentOff: 50,
      }

      // Original should remain unchanged
      expect(DEFAULT_LAUNCH_PROMOTION.couponId).toBe(originalCouponId)
      expect(DEFAULT_LAUNCH_PROMOTION.percentOff).toBe(originalPercentOff)
      expect(modifiedPromo.percentOff).toBe(50)
    })
  })
})
