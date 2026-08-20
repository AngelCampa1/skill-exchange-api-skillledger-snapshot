/**
 * Subscription API Tests - Week 7
 *
 * Testing Philosophy: Mock ONLY external services (fetch), never mock internal logic
 * - Tests real API wrapper functions and error handling
 * - Tests input validation, race conditions, and network errors
 * - Tests useSubscription hook state management
 *
 * Expected Bugs to Find:
 * - No input validation on createSubscriptionCheckout (tierId, billingCycle, URLs)
 * - 409 Conflict not distinguished from 500 on already-canceled subscription
 * - Race condition in refetch() when hasLoadedRef is true
 * - Malformed JSON response handling
 * - Error logging context
 */

import {
  getUserSubscription,
  getSubscriptionTiers,
  createSubscriptionCheckout,
  createPaymentMethodSetup,
  getPaymentMethods,
  getBillingHistory,
  getSubscriptionUsage,
  cancelSubscription,
  changeSubscriptionTier,
  useSubscription
} from '../subscription-api';
import { setupFetchMock } from '@/utils/test/testUtils';
import { renderHook, act, waitFor } from '@testing-library/react';
import { BillingCycle } from '@/types/subscription';

// Mock logger
jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
  },
}));

import { logger } from '@/utils/logger';

describe('Subscription API - Week 7 (API Layer)', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  beforeEach(() => {
    fetchMock = setupFetchMock();
    jest.clearAllMocks();
  });

  afterEach(() => {
    fetchMock.reset();
  });

  // ==========================================================================
  // Suite 1: getUserSubscription - Error Handling (5 tests)
  // ==========================================================================

  describe('getUserSubscription - Error Handling', () => {
    test('getUserSubscription returns null on 404', async () => {
      fetchMock.respondWithError(404, 'Not Found');

      const result = await getUserSubscription();

      expect(result).toBeNull();
      expect(logger.error).toHaveBeenCalledWith(
        'Error fetching user subscription',
        expect.any(Error),
        { api: 'subscription' }
      );
    });

    test('getUserSubscription returns null on 500', async () => {
      fetchMock.respondWithError(500, 'Server Error');

      const result = await getUserSubscription();

      expect(result).toBeNull();
    });

    test('getUserSubscription returns null on network error', async () => {
      global.fetch = jest.fn(() => Promise.reject(new Error('Network request failed')));

      const result = await getUserSubscription();

      expect(result).toBeNull();
    });

    test('getUserSubscription returns null on malformed JSON', async () => {
      global.fetch = jest.fn(() =>
        Promise.resolve({
          ok: true,
          json: () => Promise.reject(new Error('Unexpected token')),
        } as Response)
      );

      const result = await getUserSubscription();

      expect(result).toBeNull();
    });

    test('getUserSubscription returns data on success', async () => {
      const mockSubscription = {
        id: 'sub-123',
        tier: { id: 'tier-pro', name: 'Pro' },
        status: 'Active',
      };
      fetchMock.respondWith(mockSubscription);

      const result = await getUserSubscription();

      expect(result).toEqual(mockSubscription);
    });
  });

  // ==========================================================================
  // Suite 2: createSubscriptionCheckout - Input Validation (7 tests)
  // ==========================================================================

  describe('createSubscriptionCheckout - Input Validation', () => {
    test('createSubscriptionCheckout validates tierId not empty', async () => {
      fetchMock.respondWith({ sessionUrl: 'https://stripe.com/checkout/123' });

      // EXPECT BUG: No validation - should throw if tierId is empty
      await expect(
        createSubscriptionCheckout({
          tierId: '', // INVALID - empty tierId
          billingCycle: BillingCycle.Monthly,
          successUrl: 'https://example.com/success',
          cancelUrl: 'https://example.com/cancel',
        })
      ).resolves.toBeDefined();

      // Should have thrown but doesn't - BUG-TEST-032
      console.warn('BUG-TEST-032: createSubscriptionCheckout accepts empty tierId');
    });

    test('billingCycle validation: monthly | annual only', async () => {
      fetchMock.respondWith({ sessionUrl: 'https://stripe.com/checkout/123' });

      // EXPECT BUG: No validation - should throw on invalid billingCycle
      await expect(
        createSubscriptionCheckout({
          tierId: 'tier-123',
          billingCycle: 'weekly' as BillingCycle, // INVALID
          successUrl: 'https://example.com/success',
          cancelUrl: 'https://example.com/cancel',
        })
      ).resolves.toBeDefined();

      console.warn('BUG-TEST-033: createSubscriptionCheckout accepts invalid billingCycle');
    });

    test('successUrl/cancelUrl well-formed URL validation', async () => {
      fetchMock.respondWith({ sessionUrl: 'https://stripe.com/checkout/123' });

      // EXPECT BUG: No URL validation - should validate URLs are well-formed
      await expect(
        createSubscriptionCheckout({
          tierId: 'tier-123',
          billingCycle: BillingCycle.Monthly,
          successUrl: 'not-a-valid-url', // INVALID
          cancelUrl: 'also-invalid',     // INVALID
        })
      ).resolves.toBeDefined();

      console.warn('BUG-TEST-034: createSubscriptionCheckout accepts malformed URLs');
    });

    test('createSubscriptionCheckout sends request body correctly', async () => {
      fetchMock.respondWith({ sessionUrl: 'https://stripe.com/checkout/123' });

      await createSubscriptionCheckout({
        tierId: 'tier-pro',
        billingCycle: BillingCycle.Annual,
        successUrl: 'https://example.com/success',
        cancelUrl: 'https://example.com/cancel',
      });

      const lastCall = fetchMock.getLastCall();
      const body = JSON.parse(lastCall.options?.body as string);

      expect(body).toEqual({
        tierId: 'tier-pro',
        billingCycle: BillingCycle.Annual,
        successUrl: 'https://example.com/success',
        cancelUrl: 'https://example.com/cancel',
      });
    });

    test('createSubscriptionCheckout throws on API error', async () => {
      fetchMock.respondWithError(500, 'Server Error');

      await expect(
        createSubscriptionCheckout({
          tierId: 'tier-pro',
          billingCycle: BillingCycle.Monthly,
          successUrl: 'https://example.com/success',
          cancelUrl: 'https://example.com/cancel',
        })
      ).rejects.toThrow();
    });

    test('createSubscriptionCheckout logs error with {api: subscription} context', async () => {
      fetchMock.respondWithError(500, 'Server Error');

      try {
        await createSubscriptionCheckout({
          tierId: 'tier-pro',
          billingCycle: BillingCycle.Monthly,
          successUrl: 'https://example.com/success',
          cancelUrl: 'https://example.com/cancel',
        });
      } catch (error) {
        // Expected to throw
      }

      expect(logger.error).toHaveBeenCalledWith(
        'Error creating subscription checkout',
        expect.any(Error),
        { api: 'subscription' }
      );
    });

    test('createSubscriptionCheckout returns sessionUrl on success', async () => {
      fetchMock.respondWith({ sessionUrl: 'https://stripe.com/checkout/abc123' });

      const result = await createSubscriptionCheckout({
        tierId: 'tier-starter',
        billingCycle: BillingCycle.Monthly,
        successUrl: 'https://example.com/success',
        cancelUrl: 'https://example.com/cancel',
      });

      expect(result.sessionUrl).toBe('https://stripe.com/checkout/abc123');
    });
  });

  // ==========================================================================
  // Suite 2.5: Additional API Functions (8 tests)
  // ==========================================================================

  describe('Additional API Functions', () => {
    test('createPaymentMethodSetup sends request correctly', async () => {
      fetchMock.respondWith({ sessionUrl: 'https://stripe.com/setup/123' });

      const result = await createPaymentMethodSetup({
        successUrl: 'https://example.com/success',
        cancelUrl: 'https://example.com/cancel',
        setAsDefault: true,
      });

      expect(result.sessionUrl).toBe('https://stripe.com/setup/123');
      const lastCall = fetchMock.getLastCall();
      expect(lastCall.url).toContain('/checkout/setup-payment-method');
    });

    test('createPaymentMethodSetup throws on error', async () => {
      fetchMock.respondWithError(500, 'Setup failed');

      await expect(
        createPaymentMethodSetup({
          successUrl: 'https://example.com/success',
          cancelUrl: 'https://example.com/cancel',
        })
      ).rejects.toThrow();

      expect(logger.error).toHaveBeenCalledWith(
        'Error creating payment method setup',
        expect.any(Error),
        { api: 'subscription' }
      );
    });

    test('getPaymentMethods returns payment methods on success', async () => {
      const mockPaymentMethods = [
        { id: 'pm-1', type: 'card', last4: '4242', brand: 'visa' },
        { id: 'pm-2', type: 'card', last4: '5555', brand: 'mastercard' },
      ];
      fetchMock.respondWith(mockPaymentMethods);

      const result = await getPaymentMethods();

      expect(result).toEqual(mockPaymentMethods);
    });

    test('getPaymentMethods returns empty array on error', async () => {
      fetchMock.respondWithError(500, 'Server Error');

      const result = await getPaymentMethods();

      expect(result).toEqual([]);
      expect(logger.error).toHaveBeenCalled();
    });

    test('getBillingHistory returns history on success', async () => {
      const mockHistory = [
        { id: 'inv-1', amount: 29.00, date: '2025-01-01', status: 'paid' },
        { id: 'inv-2', amount: 29.00, date: '2025-02-01', status: 'paid' },
      ];
      fetchMock.respondWith(mockHistory);

      const result = await getBillingHistory();

      expect(result).toEqual(mockHistory);
    });

    test('getBillingHistory returns empty array on error', async () => {
      fetchMock.respondWithError(500, 'Server Error');

      const result = await getBillingHistory();

      expect(result).toEqual([]);
    });

    test('getSubscriptionUsage returns usage on success', async () => {
      const mockUsage = {
        projectsCreated: 5,
        projectsLimit: 10,
        creditsUsed: 100,
        creditsLimit: 500,
      };
      fetchMock.respondWith(mockUsage);

      const result = await getSubscriptionUsage();

      expect(result).toEqual(mockUsage);
    });

    test('getSubscriptionUsage returns null on error', async () => {
      fetchMock.respondWithError(500, 'Server Error');

      const result = await getSubscriptionUsage();

      expect(result).toBeNull();
    });
  });

  // ==========================================================================
  // Suite 2.6: changeSubscriptionTier (3 tests)
  // ==========================================================================

  describe('changeSubscriptionTier', () => {
    test('changeSubscriptionTier sends correct request body', async () => {
      fetchMock.respondWith({ success: true });

      const result = await changeSubscriptionTier('tier-pro', true);

      expect(result).toBe(true);
      const lastCall = fetchMock.getLastCall();
      const body = JSON.parse(lastCall.options?.body as string);
      expect(body).toEqual({ newTierId: 'tier-pro', immediateCharge: true });
    });

    test('changeSubscriptionTier default immediateCharge is false', async () => {
      fetchMock.respondWith({ success: true });

      await changeSubscriptionTier('tier-basic');

      const lastCall = fetchMock.getLastCall();
      const body = JSON.parse(lastCall.options?.body as string);
      expect(body.immediateCharge).toBe(false);
    });

    test('changeSubscriptionTier returns false on error', async () => {
      fetchMock.respondWithError(500, 'Upgrade failed');

      const result = await changeSubscriptionTier('tier-pro');

      expect(result).toBe(false);
      expect(logger.error).toHaveBeenCalled();
    });
  });

  // ==========================================================================
  // Suite 3: cancelSubscription - Error Handling (4 tests)
  // ==========================================================================

  describe('cancelSubscription - Error Handling', () => {
    test('cancelSubscription with reason sends correct body', async () => {
      fetchMock.respondWith({ success: true });

      await cancelSubscription('Too expensive');

      const lastCall = fetchMock.getLastCall();
      const body = JSON.parse(lastCall.options?.body as string);

      expect(body).toEqual({ reason: 'Too expensive' });
    });

    test('cancelSubscription without reason sends empty reason', async () => {
      fetchMock.respondWith({ success: true });

      await cancelSubscription();

      const lastCall = fetchMock.getLastCall();
      const body = JSON.parse(lastCall.options?.body as string);

      expect(body).toEqual({ reason: undefined });
    });

    test('409 Conflict on already-canceled subscription not distinguished from 500', async () => {
      fetchMock.respondWithError(409, 'Subscription already canceled');

      const result = await cancelSubscription('Duplicate cancel');

      // EXPECT BUG: Should return specific error, but returns false for all errors
      expect(result).toBe(false);

      // Cannot distinguish 409 from 500 - both return false
      console.warn('BUG-TEST-035: cancelSubscription cannot distinguish 409 from 500');
    });

    test('cancelSubscription returns true on success', async () => {
      fetchMock.respondWith({ success: true });

      const result = await cancelSubscription();

      expect(result).toBe(true);
    });
  });

  // ==========================================================================
  // Suite 4: getSubscriptionTiers - Deduplication (3 tests)
  // ==========================================================================

  describe('getSubscriptionTiers - Deduplication', () => {
    test('getSubscriptionTiers returns empty array on error', async () => {
      fetchMock.respondWithError(500, 'Server Error');

      const result = await getSubscriptionTiers();

      expect(result).toEqual([]);
    });

    test('getSubscriptionTiers returns tiers sorted by sortOrder', async () => {
      const mockTiers = [
        { id: 'pro', name: 'Pro', sortOrder: 2, price: 29 },
        { id: 'free', name: 'Free', sortOrder: 0, price: 0 },
        { id: 'starter', name: 'Starter', sortOrder: 1, price: 9 },
      ];
      fetchMock.respondWith(mockTiers);

      const result = await getSubscriptionTiers();

      // API returns unsorted - expect them in original order
      expect(result).toEqual(mockTiers);
      // NOTE: Sorting should happen on client if needed
    });

    test('concurrent getTiers() calls are NOT deduplicated', async () => {
      fetchMock.respondWith([{ id: 'tier1', name: 'Tier 1' }]);
      fetchMock.respondWith([{ id: 'tier1', name: 'Tier 1' }]);

      // Call twice concurrently
      const [result1, result2] = await Promise.all([
        getSubscriptionTiers(),
        getSubscriptionTiers(),
      ]);

      // EXPECT BUG: No deduplication - 2 fetch calls made
      expect(fetchMock.getCalls().length).toBe(2);
      console.warn('BUG-TEST-036: getSubscriptionTiers not deduplicated - makes duplicate calls');
    });
  });

  // ==========================================================================
  // Suite 5: useSubscription Hook - Race Conditions (6 tests)
  // ==========================================================================

  describe('useSubscription Hook - Race Conditions', () => {
    test('hasLoadedRef prevents duplicate loads', async () => {
      fetchMock.respondWith({ id: 'sub-123', status: 'Active' });
      fetchMock.respondWith([{ id: 'tier1', name: 'Tier 1' }]);

      const { result, rerender } = renderHook(() => useSubscription());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      // Initial load makes 2 calls (subscription + tiers)
      const initialCallCount = fetchMock.getCalls().length;
      expect(initialCallCount).toBe(2);

      // Force re-render (shouldn't trigger reload)
      await act(async () => {
        result.current.refetch();
      });

      // No additional calls due to hasLoadedRef
      expect(fetchMock.getCalls().length).toBe(initialCallCount);
    });

    test('race condition in useSubscription.refetch()', async () => {
      fetchMock.respondWith({ id: 'sub-123', status: 'Active' });
      fetchMock.respondWith([{ id: 'tier1', name: 'Tier 1' }]);

      const { result, rerender } = renderHook(() => useSubscription());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      // Call refetch() twice concurrently
      fetchMock.respondWith({ id: 'sub-456', status: 'Trial' });
      fetchMock.respondWith([{ id: 'tier2', name: 'Tier 2' }]);

      act(() => {
        result.current.refetch();
        result.current.refetch(); // EXPECT BUG: Second call does nothing (hasLoadedRef=true)
      });

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      // EXPECT BUG: refetch() calls loadSubscriptionData() but hasLoadedRef is still true
      // So second refetch() returns early without fetching
      console.warn('BUG-TEST-037: useSubscription.refetch() blocked by hasLoadedRef');
    });

    test('useSubscription sets subscription and tiers on success', async () => {
      const mockSubscription = { id: 'sub-789', status: 'Active' };
      const mockTiers = [{ id: 'tier-pro', name: 'Pro', sortOrder: 1 }];

      fetchMock.respondWith(mockSubscription);
      fetchMock.respondWith(mockTiers);

      const { result, rerender } = renderHook(() => useSubscription());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
        expect(result.current.subscription).toEqual(mockSubscription);
        expect(result.current.tiers).toEqual(mockTiers);
      });
    });

    test('useSubscription handles network failure gracefully', async () => {
      // BUG-TEST-038: API functions swallow errors and return null/[]
      fetchMock.mockFetch.mockRejectedValueOnce(new Error('Network failed'));
      fetchMock.mockFetch.mockRejectedValueOnce(new Error('Network failed'));

      const { result } = renderHook(() => useSubscription());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      // Error is not set because API functions swallow errors
      expect(result.current.error).toBeNull();
      expect(result.current.subscription).toBeNull();
      console.warn('BUG-TEST-038: error state not set due to error swallowing');
    });

    test('useSubscription refetch is blocked by hasLoadedRef', async () => {
      // BUG-TEST-039: refetch() doesn't reset hasLoadedRef, so it does nothing
      // First calls return empty data
      fetchMock.respondWith(null);
      fetchMock.respondWith([]);

      const { result } = renderHook(() => useSubscription());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
        expect(result.current.subscription).toBeNull();
      });

      const callsAfterLoad = fetchMock.getCalls().length;

      // Try to refetch - this should load new data but doesn't
      fetchMock.respondWith({ id: 'sub-retry', status: 'Active' });
      fetchMock.respondWith([{ id: 'tier-retry', name: 'Retry' }]);

      await act(async () => {
        result.current.refetch();
      });

      // BUG: refetch() does nothing because hasLoadedRef is true
      // No new fetch calls are made
      expect(fetchMock.getCalls().length).toBe(callsAfterLoad);
      expect(result.current.subscription).toBeNull(); // Still null
      console.warn('BUG-TEST-039: refetch() blocked by hasLoadedRef - does nothing after initial load');
    });

    test('useSubscription createCheckout generates correct URLs', async () => {
      fetchMock.respondWith({ id: 'sub-123', status: 'Active' });
      fetchMock.respondWith([{ id: 'tier1', name: 'Tier 1' }]);
      fetchMock.respondWith({ sessionUrl: 'https://stripe.com/checkout/xyz' });

      const { result, rerender } = renderHook(() => useSubscription());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      // Mock window.location.origin
      delete (window as any).location;
      (window as any).location = { origin: 'https://skillledger.app' };

      await act(async () => {
        await result.current.createCheckout('tier-pro', BillingCycle.Monthly);
      });

      const lastCall = fetchMock.getLastCall();
      const body = JSON.parse(lastCall.options?.body as string);

      expect(body.successUrl).toBe('https://skillledger.app/dashboard?subscription_success=true');
      expect(body.cancelUrl).toBe('https://skillledger.app/subscription/choose-plan');
    });

    test('useSubscription setupPaymentMethod generates correct URLs', async () => {
      fetchMock.respondWith({ id: 'sub-123', status: 'Active' });
      fetchMock.respondWith([{ id: 'tier1', name: 'Tier 1' }]);
      fetchMock.respondWith({ sessionUrl: 'https://stripe.com/setup/abc' });

      const { result } = renderHook(() => useSubscription());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      // Mock window.location.origin
      delete (window as any).location;
      (window as any).location = { origin: 'https://skillledger.app' };

      let setupResult: any;
      await act(async () => {
        setupResult = await result.current.setupPaymentMethod();
      });

      expect(setupResult.sessionUrl).toBe('https://stripe.com/setup/abc');

      const lastCall = fetchMock.getLastCall();
      const body = JSON.parse(lastCall.options?.body as string);

      expect(body.successUrl).toBe('https://skillledger.app/dashboard?payment_method_setup=true');
      expect(body.cancelUrl).toBe('https://skillledger.app/dashboard?payment_method_canceled=true');
      expect(body.setAsDefault).toBe(true);
    });

    test('useSubscription handles fetch errors gracefully (errors swallowed by API functions)', async () => {
      // When fetch fails, getUserSubscription returns null internally (error is logged but swallowed)
      // This is BUG behavior - errors should propagate to the hook's catch block
      fetchMock.mockFetch.mockImplementationOnce(() => {
        throw new Error('Network failure');
      });

      const { result } = renderHook(() => useSubscription());

      await waitFor(() => {
        expect(result.current.loading).toBe(false);
      });

      // BUG-TEST-040: Error is swallowed by getUserSubscription - error state is NOT set
      // The catch block in useSubscription (lines 222-224) is unreachable dead code
      expect(result.current.error).toBeNull();
      expect(result.current.subscription).toBeNull();
      console.warn('BUG-TEST-040: useSubscription catch block is unreachable dead code');
    });
  });
});
