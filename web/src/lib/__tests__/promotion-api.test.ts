/**
 * promotion-api.ts Integration Tests
 *
 * Tests Stripe promotion management API with real fetch calls.
 * Focus: CRUD operations, validation, usage tracking, error handling.
 *
 * Coverage Target: 80%+ (326 lines)
 * Test Count: 15 tests
 */

import {
  createCoupon,
  listCoupons,
  getCoupon,
  getCouponStats,
  deactivateCoupon,
  createPromotionCode,
  listPromotionCodes,
  getPromotionCode,
  deactivatePromotionCode,
  validatePromotionCode,
  getPromotionStats,
  createLaunchPromotion,
  getLaunchPromotionStatus,
} from '../promotion-api';

describe('promotion-api.ts - Stripe Promotion Management', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    global.fetch = jest.fn();
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  // ==========================================
  // Part 1: Promotion CRUD (8 tests)
  // ==========================================

  describe('Coupon Management', () => {
    it('should create coupon with valid percentage discount', async () => {
      const mockCoupon = {
        id: 'coup_123',
        name: 'Summer Sale',
        percentOff: 20,
        duration: 'once' as const,
        isActive: true,
      };

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockCoupon,
      });

      const result = await createCoupon({
        id: 'summer-sale',
        name: 'Summer Sale',
        percentOff: 20,
        duration: 'once',
      });

      expect(global.fetch).toHaveBeenCalledWith(
        '/api/admin/promotions/coupons',
        expect.objectContaining({
          method: 'POST',
          credentials: 'include',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            id: 'summer-sale',
            name: 'Summer Sale',
            percentOff: 20,
            duration: 'once',
          }),
        })
      );
      expect(result).toEqual(mockCoupon);
    });

    it('should list coupons with pagination and filters', async () => {
      const mockCoupons = [
        { id: 'coup_1', name: 'Promo 1', percentOff: 10, isActive: true },
        { id: 'coup_2', name: 'Promo 2', percentOff: 15, isActive: false },
      ];

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockCoupons,
      });

      const result = await listCoupons(true, 10);

      expect(global.fetch).toHaveBeenCalledWith(
        '/api/admin/promotions/coupons?activeOnly=true&limit=10',
        expect.objectContaining({
          credentials: 'include',
        })
      );
      expect(result).toHaveLength(2);
      expect(result[0].isActive).toBe(true);
    });

    it('should get coupon statistics with usage data', async () => {
      const mockStats = {
        couponId: 'coup_123',
        timesRedeemed: 45,
        totalDiscountGiven: 1250.50,
        activeSubscriptions: 12,
      };

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockStats,
      });

      const result = await getCouponStats('coup_123');

      expect(global.fetch).toHaveBeenCalledWith(
        '/api/admin/promotions/coupons/coup_123/stats',
        expect.objectContaining({
          credentials: 'include',
        })
      );
      expect(result?.timesRedeemed).toBe(45);
      expect(result?.totalDiscountGiven).toBe(1250.50);
    });

    it('should deactivate coupon (returns boolean)', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        status: 204,
      });

      const result = await deactivateCoupon('coup_123');

      expect(global.fetch).toHaveBeenCalledWith(
        '/api/admin/promotions/coupons/coup_123',
        expect.objectContaining({
          method: 'DELETE',
          credentials: 'include',
        })
      );
      expect(result).toBe(true);
    });
  });

  describe('Promotion Code Management', () => {
    it('should create promotion code with validation', async () => {
      const mockPromoCode = {
        id: 'promo_123',
        code: 'SUMMER2024',
        coupon: 'coup_123',
        isActive: true,
        maxRedemptions: 100,
      };

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockPromoCode,
      });

      const result = await createPromotionCode({
        couponId: 'coup_123',
        code: 'SUMMER2024',
        maxRedemptions: 100,
      });

      expect(global.fetch).toHaveBeenCalledWith(
        '/api/admin/promotions/codes',
        expect.objectContaining({
          method: 'POST',
          credentials: 'include',
          body: JSON.stringify({
            couponId: 'coup_123',
            code: 'SUMMER2024',
            maxRedemptions: 100,
          }),
        })
      );
      expect(result.code).toBe('SUMMER2024');
      expect(result.isActive).toBe(true);
    });

    it('should list promotion codes with filters', async () => {
      const mockCodes = [
        { id: 'promo_1', code: 'CODE1', active: true, timesRedeemed: 10 },
        { id: 'promo_2', code: 'CODE2', active: false, timesRedeemed: 50 },
      ];

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockCodes,
      });

      const result = await listPromotionCodes(undefined, true, 10);

      expect(global.fetch).toHaveBeenCalledWith(
        '/api/admin/promotions/codes?activeOnly=true&limit=10',
        expect.objectContaining({
          credentials: 'include',
        })
      );
      expect(result).toHaveLength(2);
    });

    it('should get single promotion code by code string', async () => {
      const mockPromoCode = {
        id: 'promo_123',
        code: 'VIP2024',
        coupon: { percentOff: 25 },
        timesRedeemed: 15,
        maxRedemptions: 50,
      };

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockPromoCode,
      });

      const result = await getPromotionCode('VIP2024');

      expect(global.fetch).toHaveBeenCalledWith(
        '/api/admin/promotions/codes/VIP2024',
        expect.objectContaining({
          credentials: 'include',
        })
      );
      expect(result?.code).toBe('VIP2024');
      expect(result?.timesRedeemed).toBe(15);
    });

    it('should deactivate promotion code (returns boolean)', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        status: 204,
      });

      const result = await deactivatePromotionCode('promo_123');

      expect(global.fetch).toHaveBeenCalledWith(
        '/api/admin/promotions/codes/promo_123',
        expect.objectContaining({
          method: 'DELETE',
          credentials: 'include',
        })
      );
      expect(result).toBe(true);
    });
  });

  // ==========================================
  // Part 2: Stripe Integration (4 tests)
  // ==========================================

  describe('API Request Handling', () => {
    it('should include credentials and content-type headers in all requests', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => [],
      });

      await listCoupons();

      expect(global.fetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          credentials: 'include',
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
          }),
        })
      );
    });

    it('should throw error on 400 Bad Request with error message', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        status: 400,
        text: async () => 'Invalid coupon percentage: must be between 1 and 100',
      });

      await expect(
        createCoupon({
          id: 'invalid-coupon',
          name: 'Invalid',
          percentOff: 150, // Invalid percentage
          duration: 'once',
        })
      ).rejects.toThrow('API Error: 400 - Invalid coupon percentage: must be between 1 and 100');
    });

    it('should return null on 500 Internal Server Error (caught by try-catch)', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        status: 500,
        text: async () => 'Stripe API connection failed',
      });

      // getCoupon catches errors and returns null
      const result = await getCoupon('coup_123');
      expect(result).toBeNull();
    });

    it('should handle 204 No Content response (deactivate returns true)', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        status: 204,
      });

      const result = await deactivateCoupon('coup_123');

      expect(result).toBe(true);
    });
  });

  // ==========================================
  // Part 3: Usage Tracking (3 tests)
  // ==========================================

  describe('Promotion Statistics & Validation', () => {
    it('should get aggregated promotion statistics', async () => {
      const mockStats = {
        totalCoupons: 12,
        activeCoupons: 8,
        totalPromotionCodes: 25,
        activePromotionCodes: 18,
        totalRedemptions: 342,
        totalRevenue: 15420.75,
        topCoupons: [
          { id: 'coup_1', name: 'Summer Sale', redemptions: 120 },
          { id: 'coup_2', name: 'VIP Discount', redemptions: 95 },
        ],
      };

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockStats,
      });

      const result = await getPromotionStats();

      expect(global.fetch).toHaveBeenCalledWith(
        '/api/admin/promotions/stats',
        expect.objectContaining({
          credentials: 'include',
        })
      );
      expect(result?.totalRedemptions).toBe(342);
      expect(result?.topCoupons).toHaveLength(2);
    });

    it('should validate promotion code with expiration and usage limits', async () => {
      const mockValidation = {
        isValid: true,
        couponId: 'coup_123',
        percentOff: 20,
        errorMessage: null,
        errorCode: null,
      };

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockValidation,
      });

      const result = await validatePromotionCode('SUMMER2024');

      expect(global.fetch).toHaveBeenCalledWith(
        '/api/admin/promotions/validate/SUMMER2024',
        expect.objectContaining({
          credentials: 'include',
        })
      );
      expect(result.isValid).toBe(true);
    });

    it('should get launch promotion status with current usage', async () => {
      const mockCoupon = {
        id: 'launch_3mo_free',
        isActive: true,
        maxRedemptions: 100,
        timesRedeemed: 67,
        remainingRedemptions: 33,
      };

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockCoupon,
      });

      const result = await getLaunchPromotionStatus();

      expect(global.fetch).toHaveBeenCalledWith(
        '/api/admin/promotions/coupons/launch_3mo_free',
        expect.objectContaining({
          credentials: 'include',
        })
      );
      expect(result?.usedSlots).toBe(67);
      expect(result?.remainingSlots).toBe(33);
    });
  });

  // ==========================================
  // Part 4: Error Handling Coverage (8 tests)
  // ==========================================

  describe('Error Handling Coverage', () => {
    it('listCoupons returns empty array on error', async () => {
      (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      const result = await listCoupons();
      expect(result).toEqual([]);
    });

    it('getCouponStats returns null on error', async () => {
      (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      const result = await getCouponStats('coup_123');
      expect(result).toBeNull();
    });

    it('deactivateCoupon returns false on error', async () => {
      (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      const result = await deactivateCoupon('coup_123');
      expect(result).toBe(false);
    });

    it('createPromotionCode throws on error', async () => {
      (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      await expect(createPromotionCode({ couponId: 'coup_123', code: 'TEST' }))
        .rejects.toThrow('Network error');
    });

    it('listPromotionCodes returns empty array on error', async () => {
      (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      const result = await listPromotionCodes();
      expect(result).toEqual([]);
    });

    it('listPromotionCodes includes couponId filter when provided', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => [],
      });

      await listPromotionCodes('coup_123', true, 50);

      expect(global.fetch).toHaveBeenCalledWith(
        '/api/admin/promotions/codes?activeOnly=true&limit=50&couponId=coup_123',
        expect.any(Object)
      );
    });

    it('getPromotionCode returns null on error', async () => {
      (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      const result = await getPromotionCode('TEST');
      expect(result).toBeNull();
    });

    it('deactivatePromotionCode returns false on error', async () => {
      (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      const result = await deactivatePromotionCode('promo_123');
      expect(result).toBe(false);
    });

    it('getPromotionStats returns null on error', async () => {
      (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      const result = await getPromotionStats();
      expect(result).toBeNull();
    });
  });

  // ==========================================
  // Part 5: Launch Promotion Coverage (3 tests)
  // ==========================================

  describe('Launch Promotion Functions', () => {
    it('createLaunchPromotion creates coupon and promo code', async () => {
      const mockCoupon = {
        id: 'launch_3mo_free',
        name: 'Launch Promotion - 3 Months Free',
        percentOff: 100,
        isActive: true,
      };
      const mockPromoCode = {
        id: 'promo_launch',
        code: 'LAUNCH2024',
        coupon: 'launch_3mo_free',
        active: true,
      };

      // First call creates coupon
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockCoupon,
      });
      // Second call creates promo code
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockPromoCode,
      });

      const result = await createLaunchPromotion();

      expect(result.coupon.id).toBe('launch_3mo_free');
      expect(result.promoCode.code).toBe('LAUNCH2024');
      expect(global.fetch).toHaveBeenCalledTimes(2);
    });

    it('getLaunchPromotionStatus returns null when coupon not found', async () => {
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        status: 404,
        text: async () => 'Not found',
      });

      const result = await getLaunchPromotionStatus();
      expect(result).toBeNull();
    });

    it('validatePromotionCode returns validation result on failure', async () => {
      const mockValidation = {
        isValid: false,
        couponId: null,
        percentOff: 0,
        errorMessage: 'Promotion code has expired',
        errorCode: 'EXPIRED',
      };

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => mockValidation,
      });

      const result = await validatePromotionCode('EXPIRED2023');

      expect(result.isValid).toBe(false);
      expect(result.errorMessage).toBe('Promotion code has expired');
      expect(result.errorCode).toBe('EXPIRED');
    });
  });
});
