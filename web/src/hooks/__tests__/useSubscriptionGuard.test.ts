/**
 * useSubscriptionGuard Tests - Week 7
 *
 * Testing Philosophy: Mock ONLY external services (useSubscription hook data), test real business logic
 * - Tests tier comparison, feature gates, limit enforcement
 * - Tests no-subscription handling (free tier removed — subscription required for all access)
 * - Tests edge cases: missing tiers, invalid statuses, case sensitivity
 *
 * Expected Bugs to Find:
 * - Tier not found crashes instead of graceful error
 * - PastDue status not handled (not in enum)
 * - Feature name typos silently pass
 */

import { renderHook, waitFor } from '@testing-library/react';
import { useSubscriptionGuard } from '../useSubscriptionGuard';
import { useSubscription } from '@/lib/subscription-api';
import { SubscriptionStatus, SubscriptionTier } from '@/types/subscription';

// Mock useSubscription hook
jest.mock('@/lib/subscription-api', () => ({
  useSubscription: jest.fn(),
}));

// Mock useRouter
jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
  }),
}));

const mockUseSubscription = useSubscription as jest.MockedFunction<typeof useSubscription>;

// Helper function to create complete mock tiers with all required properties
const createMockTier = (overrides?: Partial<SubscriptionTier>): SubscriptionTier => ({
  id: 'basic',
  name: 'Basic',
  price: 0,
  creditBonus: 0,
  maxActiveProjects: 1,
  maxTeamMembers: 1,
  maxMonthlyEarnings: 500,
  prioritySupport: false,
  apiAccess: false,
  advancedAnalytics: false,
  advancedFraudDetection: false,
  multiSignature: false,
  customIntegrations: false,
  features: [],
  sortOrder: 0,
  ...overrides
});

describe('useSubscriptionGuard - Week 7 (Business Logic)', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  // ==========================================================================
  // Suite 1: No Subscription Handling (6 tests)
  // Free tier has been removed — subscription required for all access
  // ==========================================================================

  describe('No Subscription Handling', () => {
    test('no subscription blocks access regardless of maxProjects', () => {
      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: [],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ maxProjects: 2 })
      );

      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toBe('Active subscription required');
      expect(result.current.upgradeRequired).toBe(true);
    });

    test('no subscription returns null limits', () => {
      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: [],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() => useSubscriptionGuard());

      expect(result.current.limits).toBeNull();
      expect(result.current.canAccess).toBe(false);
      expect(result.current.upgradeRequired).toBe(true);
    });

    test('no subscription blocks even with no additional options', () => {
      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: [],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() => useSubscriptionGuard());

      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toBe('Active subscription required');
    });

    test('no subscription blocks required tier check', () => {
      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: [],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ requiredTier: 'Pro' })
      );

      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toBe('Active subscription required');
      expect(result.current.upgradeRequired).toBe(true);
    });

    test('no subscription blocks required features check', () => {
      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: [],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ requiredFeatures: ['prioritySupport', 'apiAccess'] })
      );

      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toBe('Active subscription required');
      expect(result.current.upgradeRequired).toBe(true);
    });

    test('no subscription blocks team members check', () => {
      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: [],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ maxTeamMembers: 5 })
      );

      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toBe('Active subscription required');
      expect(result.current.upgradeRequired).toBe(true);
    });
  });

  // ==========================================================================
  // Suite 2: Tier Comparison Logic (7 tests)
  // ==========================================================================

  describe('Tier Comparison Logic', () => {
    const mockTiers = [
      createMockTier({ id: 'free', name: 'Free', sortOrder: 0, price: 0, creditBonus: 0, maxActiveProjects: 1, maxTeamMembers: 1, maxMonthlyEarnings: 500 }),
      createMockTier({ id: 'starter', name: 'Starter', sortOrder: 1, price: 10, creditBonus: 10, maxActiveProjects: 10, maxTeamMembers: 3, maxMonthlyEarnings: 5000 }),
      createMockTier({ id: 'pro', name: 'Pro', sortOrder: 2, price: 50, creditBonus: 50, maxActiveProjects: 50, maxTeamMembers: 10, maxMonthlyEarnings: 50000, prioritySupport: true, apiAccess: true }),
      createMockTier({ id: 'enterprise', name: 'Enterprise', sortOrder: 3, price: 200, creditBonus: 200, maxActiveProjects: -1, maxTeamMembers: -1, maxMonthlyEarnings: -1, prioritySupport: true, apiAccess: true, advancedAnalytics: true }),
    ];

    test('tier comparison: Free (0) < Starter (1) < Pro (2) < Enterprise (3)', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-123',
          tier: mockTiers[1], // Starter
          status: SubscriptionStatus.Active,
        } as any,
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      // Starter user trying to access Pro feature
      const { result } = renderHook(() =>
        useSubscriptionGuard({ requiredTier: 'Pro' })
      );

      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toBe('Pro plan required');
    });

    test('case-insensitive tier names (pro === Pro)', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-123',
          tier: mockTiers[2], // Pro
          status: SubscriptionStatus.Active,
        } as any,
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ requiredTier: 'pro' }) // lowercase
      );

      expect(result.current.canAccess).toBe(true); // Case-insensitive match
    });

    test('tier not found crashes with error', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-invalid',
          tier: createMockTier({ id: 'nonexistent', name: 'Ghost', sortOrder: 99 }),
          status: SubscriptionStatus.Active,
        } as any,
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() => useSubscriptionGuard());

      // EXPECT BUG: Should return graceful error, but crashes
      expect(result.current.canAccess).toBe(false);
      expect(result.current.error).toBe('Subscription tier not found');
      console.warn('BUG-TEST-038: Tier not found sets error state (correct behavior verified)');
    });

    test('duplicate tier names edge case', () => {
      const duplicateTiers = [
        ...mockTiers,
        createMockTier({ id: 'pro-duplicate', name: 'Pro', sortOrder: 5, maxActiveProjects: 100 }), // Duplicate "Pro"
      ];

      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-123',
          tier: mockTiers[2], // Original Pro (sortOrder=2)
          status: SubscriptionStatus.Active,
        } as any,
        tiers: duplicateTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ requiredTier: 'Pro' })
      );

      // EXPECT BUG: .find() returns first match, so comparison uses first Pro (sortOrder=2)
      // User has sortOrder=2, required is also 2, so canAccess=true
      expect(result.current.canAccess).toBe(true);
      console.warn('BUG-TEST-039: Duplicate tier names may cause unexpected behavior');
    });

    test('paid user can access free tier features', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-123',
          tier: mockTiers[2], // Pro
          status: SubscriptionStatus.Active,
        } as any,
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ requiredTier: 'Free' })
      );

      expect(result.current.canAccess).toBe(true); // Pro > Free
    });

    test('enterprise tier has unlimited limits (-1)', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-enterprise',
          tier: mockTiers[3], // Enterprise
          status: SubscriptionStatus.Active,
        } as any,
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ maxProjects: 1000 })
      );

      // maxActiveProjects=-1 means unlimited, should allow 1000 projects
      expect(result.current.canAccess).toBe(true);
    });

    test('user with no tier (paid-to-free downgrade) not handled', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-downgraded',
          tier: null, // EDGE CASE: User downgraded but subscription still exists
          status: SubscriptionStatus.Active,
        } as any,
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() => useSubscriptionGuard());

      // EXPECT BUG: tier is null, should handle gracefully
      // Code at line 142: tiers.find(t => t.id === subscription.tier?.id)
      // subscription.tier is null, so find() returns undefined
      expect(result.current.error).toBe('Subscription tier not found');
      console.warn('BUG-TEST-040: Paid-to-free downgrade (tier=null) causes error');
    });
  });

  // ==========================================================================
  // Suite 3: Subscription Status Handling (5 tests)
  // ==========================================================================

  describe('Subscription Status Handling', () => {
    const mockProTier = createMockTier({
      id: 'pro',
      name: 'Pro',
      sortOrder: 2,
      price: 50,
      creditBonus: 50,
      maxActiveProjects: 50,
      maxTeamMembers: 10,
      maxMonthlyEarnings: 50000,
      prioritySupport: true,
      apiAccess: true,
    });

    test('Active status allows access', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-active',
          tier: mockProTier,
          status: SubscriptionStatus.Active,
        } as any,
        tiers: [mockProTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() => useSubscriptionGuard());

      expect(result.current.canAccess).toBe(true);
    });

    test('Trial status allows access', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-trial',
          tier: mockProTier,
          status: SubscriptionStatus.Trial,
        } as any,
        tiers: [mockProTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() => useSubscriptionGuard());

      expect(result.current.canAccess).toBe(true);
    });

    test('Cancelled status blocks access', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-cancelled',
          tier: mockProTier,
          status: SubscriptionStatus.Cancelled,
        } as any,
        tiers: [mockProTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() => useSubscriptionGuard());

      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toContain('cancelled');
      expect(result.current.upgradeRequired).toBe(true);
    });

    test('Expired status blocks access', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-expired',
          tier: mockProTier,
          status: SubscriptionStatus.Expired,
        } as any,
        tiers: [mockProTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() => useSubscriptionGuard());

      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toContain('expired');
    });

    test('PastDue status not defined in enum - unexpected behavior', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-pastdue',
          tier: mockProTier,
          status: 'PastDue' as SubscriptionStatus, // NOT in enum
        } as any,
        tiers: [mockProTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() => useSubscriptionGuard());

      // EXPECT BUG: PastDue not handled - falls through to block access
      // Line 129: status !== Active && status !== Trial → blocks
      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toContain('pastdue');
      console.warn('BUG-TEST-041: PastDue status not in enum - blocks access');
    });
  });

  // ==========================================================================
  // Suite 4: Feature Checking (7 tests)
  // ==========================================================================

  describe('Feature Checking', () => {
    const mockProTier = createMockTier({
      id: 'pro',
      name: 'Pro',
      sortOrder: 2,
      price: 50,
      creditBonus: 50,
      maxActiveProjects: 50,
      maxTeamMembers: 10,
      maxMonthlyEarnings: 50000,
      prioritySupport: true,
      apiAccess: true,
      advancedAnalytics: false,
      advancedFraudDetection: false,
      multiSignature: false,
      customIntegrations: false,
      features: ['customReports', 'webhooks'],
    });

    test('requiredFeatures: prioritySupport blocks if missing', () => {
      const starterTier = { ...mockProTier, prioritySupport: false };

      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-starter',
          tier: starterTier,
          status: SubscriptionStatus.Active,
        } as any,
        tiers: [starterTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ requiredFeatures: ['prioritySupport'] })
      );

      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toContain('Missing features: prioritySupport');
    });

    test('requiredFeatures: apiAccess blocks if missing', () => {
      const basicTier = { ...mockProTier, apiAccess: false };

      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-basic',
          tier: basicTier,
          status: SubscriptionStatus.Active,
        } as any,
        tiers: [basicTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ requiredFeatures: ['apiAccess'] })
      );

      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toContain('apiAccess');
    });

    test('custom feature in features array', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-pro',
          tier: mockProTier, // Has features: ['customReports', 'webhooks']
          status: SubscriptionStatus.Active,
        } as any,
        tiers: [mockProTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ requiredFeatures: ['customReports'] })
      );

      expect(result.current.canAccess).toBe(true);
    });

    test('feature name typo silently passes', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-pro',
          tier: mockProTier,
          status: SubscriptionStatus.Active,
        } as any,
        tiers: [mockProTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ requiredFeatures: ['priortySupport'] }) // TYPO: priorty
      );

      // EXPECT BUG: Typo should fail validation, but silently passes (not in known features)
      // Line 205: if (!limits.features.includes(feature)) return true
      // "priortySupport" not in features array, so returns true (missing)
      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toContain('priortySupport');
      console.warn('BUG-TEST-042: Feature typo detected as missing (works correctly)');
    });

    test('multiple required features - all must be present', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-pro',
          tier: mockProTier,
          status: SubscriptionStatus.Active,
        } as any,
        tiers: [mockProTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({
          requiredFeatures: ['prioritySupport', 'apiAccess', 'advancedAnalytics'], // Pro has first 2, missing last
        })
      );

      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toContain('advancedAnalytics');
    });

    test('upgradeRequired message includes missing features', () => {
      const starterTier = { ...mockProTier, prioritySupport: false, apiAccess: false };

      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-starter',
          tier: starterTier,
          status: SubscriptionStatus.Active,
        } as any,
        tiers: [starterTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({
          requiredFeatures: ['prioritySupport', 'apiAccess'],
        })
      );

      expect(result.current.reason).toContain('prioritySupport');
      expect(result.current.reason).toContain('apiAccess');
      expect(result.current.upgradeRequired).toBe(true);
    });

    test('all features present allows access', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-pro',
          tier: mockProTier,
          status: SubscriptionStatus.Active,
        } as any,
        tiers: [mockProTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({
          requiredFeatures: ['prioritySupport', 'apiAccess', 'customReports'],
        })
      );

      expect(result.current.canAccess).toBe(true);
    });
  });

  // ==========================================================================
  // Suite 6: Loading and Error States (3 tests)
  // ==========================================================================

  describe('Loading and Error States', () => {
    test('shows loading state while subscription is loading', () => {
      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: [],
        loading: true, // Subscription is loading
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() => useSubscriptionGuard());

      expect(result.current.isLoading).toBe(true);
      expect(result.current.canAccess).toBe(false);
    });

    test('handles subscription error gracefully', () => {
      const errorMessage = 'Failed to load subscription';
      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: [],
        loading: false,
        error: errorMessage,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() => useSubscriptionGuard());

      expect(result.current.isLoading).toBe(false);
      expect(result.current.canAccess).toBe(false);
      expect(result.current.error).toBe(errorMessage);
    });

    test('redirectToUpgrade function is available', () => {
      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: [],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() => useSubscriptionGuard());

      expect(result.current.redirectToUpgrade).toBeDefined();
      expect(typeof result.current.redirectToUpgrade).toBe('function');
    });
  });

  // ==========================================================================
  // Suite 7: Custom Check Functionality (4 tests)
  // ==========================================================================

  describe('Custom Check Functionality', () => {
    const mockProTier = createMockTier({
      id: 'pro',
      name: 'Pro',
      sortOrder: 2,
      price: 50,
      creditBonus: 50,
      maxActiveProjects: 50,
      maxTeamMembers: 10,
      maxMonthlyEarnings: 50000,
      prioritySupport: true,
      apiAccess: true,
    });

    test('custom check with no subscription - always blocked before custom check runs', () => {
      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: [],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const customCheck = (sub: any) => sub === null; // Would allow null, but never reached

      const { result } = renderHook(() =>
        useSubscriptionGuard({ customCheck })
      );

      // Custom check is never reached — no-subscription guard fires first
      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toBe('Active subscription required');
    });

    test('custom check with no subscription - blocked with subscription required reason', () => {
      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: [],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const customCheck = (sub: any) => sub !== null; // Block null — same result as no-subscription guard

      const { result } = renderHook(() =>
        useSubscriptionGuard({ customCheck })
      );

      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toBe('Active subscription required');
      expect(result.current.upgradeRequired).toBe(true);
    });

    test('custom check passes for paid subscription', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-pro',
          tier: mockProTier,
          status: SubscriptionStatus.Active,
        } as any,
        tiers: [mockProTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const customCheck = (sub: any) => sub?.status === SubscriptionStatus.Active;

      const { result } = renderHook(() =>
        useSubscriptionGuard({ customCheck })
      );

      expect(result.current.canAccess).toBe(true);
    });

    test('custom check fails for paid subscription', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-pro',
          tier: mockProTier,
          status: SubscriptionStatus.Active,
        } as any,
        tiers: [mockProTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const customCheck = (sub: any) => sub?.tier?.name === 'Enterprise'; // Require Enterprise

      const { result } = renderHook(() =>
        useSubscriptionGuard({ customCheck })
      );

      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toBe('Custom access check failed');
      expect(result.current.upgradeRequired).toBe(true);
    });
  });

  // ==========================================================================
  // Suite 8: Numeric Limit Enforcement (6 tests)
  // ==========================================================================

  describe('Numeric Limit Enforcement', () => {
    const mockStarterTier = createMockTier({
      id: 'starter',
      name: 'Starter',
      sortOrder: 1,
      price: 10,
      creditBonus: 10,
      maxActiveProjects: 10,
      maxTeamMembers: 3,
      maxMonthlyEarnings: 5000,
    });

    test('enforces project limit', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-starter',
          tier: mockStarterTier,
          status: SubscriptionStatus.Active,
        } as any,
        tiers: [mockStarterTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ maxProjects: 15 }) // Exceeds limit of 10
      );

      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toContain('Exceeded project limit');
      expect(result.current.upgradeRequired).toBe(true);
    });

    test('allows within project limit', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-starter',
          tier: mockStarterTier,
          status: SubscriptionStatus.Active,
        } as any,
        tiers: [mockStarterTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ maxProjects: 5 }) // Within limit of 10
      );

      expect(result.current.canAccess).toBe(true);
    });

    test('enforces team member limit', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-starter',
          tier: mockStarterTier,
          status: SubscriptionStatus.Active,
        } as any,
        tiers: [mockStarterTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ maxTeamMembers: 10 }) // Exceeds limit of 3
      );

      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toContain('Exceeded team member limit');
      expect(result.current.upgradeRequired).toBe(true);
    });

    test('allows within team member limit', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-starter',
          tier: mockStarterTier,
          status: SubscriptionStatus.Active,
        } as any,
        tiers: [mockStarterTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ maxTeamMembers: 2 }) // Within limit of 3
      );

      expect(result.current.canAccess).toBe(true);
    });

    test('enforces monthly earnings limit', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-starter',
          tier: mockStarterTier,
          status: SubscriptionStatus.Active,
        } as any,
        tiers: [mockStarterTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ maxMonthlyEarnings: 10000 }) // Exceeds limit of 5000
      );

      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toContain('Exceeded monthly earnings limit');
      expect(result.current.upgradeRequired).toBe(true);
    });

    test('allows within monthly earnings limit', () => {
      mockUseSubscription.mockReturnValue({
        subscription: {
          id: 'sub-starter',
          tier: mockStarterTier,
          status: SubscriptionStatus.Active,
        } as any,
        tiers: [mockStarterTier],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });

      const { result } = renderHook(() =>
        useSubscriptionGuard({ maxMonthlyEarnings: 3000 }) // Within limit of 5000
      );

      expect(result.current.canAccess).toBe(true);
    });
  });

  // ==========================================================================
  // Suite 9: Convenience Hooks (4 tests)
  // ==========================================================================

  describe('Convenience Hooks', () => {
    beforeEach(() => {
      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: [],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
        refetch: jest.fn(),
      });
    });

    test('useProjectCreationGuard blocks when no subscription', () => {
      const { result } = renderHook(() => {
        // Import the hook inline to avoid module-level import issues
        const { useProjectCreationGuard } = require('../useSubscriptionGuard');
        return useProjectCreationGuard();
      });

      // No subscription — all access blocked (free tier removed)
      expect(result.current.canAccess).toBe(false);
      expect(result.current.reason).toBe('Active subscription required');
    });

    test('useAdvancedFeaturesGuard requires advancedAnalytics and apiAccess', () => {
      const { result } = renderHook(() => {
        const { useAdvancedFeaturesGuard } = require('../useSubscriptionGuard');
        return useAdvancedFeaturesGuard();
      });

      // Free tier doesn't have advanced features
      expect(result.current.canAccess).toBe(false);
      expect(result.current.upgradeRequired).toBe(true);
    });

    test('useApiAccessGuard requires apiAccess feature', () => {
      const { result } = renderHook(() => {
        const { useApiAccessGuard } = require('../useSubscriptionGuard');
        return useApiAccessGuard();
      });

      // Free tier doesn't have API access
      expect(result.current.canAccess).toBe(false);
      expect(result.current.upgradeRequired).toBe(true);
    });

    test('useUnlimitedProjectsGuard checks maxProjects: 999', () => {
      const { result } = renderHook(() => {
        const { useUnlimitedProjectsGuard } = require('../useSubscriptionGuard');
        return useUnlimitedProjectsGuard();
      });

      // Free tier only allows 1 project, so should fail
      expect(result.current.canAccess).toBe(false);
      expect(result.current.upgradeRequired).toBe(true);
    });
  });
});
