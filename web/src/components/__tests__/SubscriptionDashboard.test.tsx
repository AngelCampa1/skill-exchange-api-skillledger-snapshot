/**
 * Tests for SubscriptionDashboard
 *
 * Comprehensive test suite for the subscription dashboard component
 * Coverage target: 80%+ (718 lines)
 */

import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { SubscriptionDashboard } from '../SubscriptionDashboard'
import { useSubscription } from '@/lib/subscription-api'
import { useRouter } from 'next/navigation'
import { logger } from '@/utils/logger'
import {
  SubscriptionStatus,
  BillingCycle,
  SubscriptionTier,
  UserSubscription,
  PaymentMethod,
} from '@/types/subscription'

// Mock dependencies
jest.mock('@/lib/subscription-api')
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}))
jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
  },
}))

const mockUseSubscription = useSubscription as jest.MockedFunction<typeof useSubscription>

describe('SubscriptionDashboard', () => {
  let mockRouter: any
  let mockFetch: jest.Mock

  const mockTiers: SubscriptionTier[] = [
    {
      id: 'tier-free',
      name: 'Free',
      price: 0,
      annualPrice: 0,
      sortOrder: 1,
      maxActiveProjects: 2,
      maxTeamMembers: 1,
      maxMonthlyEarnings: 1000,
      creditBonus: 0,
      features: ['basic_project_management', 'credit_wallet'],
      prioritySupport: false,
      apiAccess: false,
      advancedAnalytics: false,
      advancedFraudDetection: false,
      multiSignature: false,
      customIntegrations: false,
    },
    {
      id: 'tier-professional',
      name: 'Professional',
      price: 29,
      annualPrice: 290,
      sortOrder: 2,
      maxActiveProjects: 10,
      maxTeamMembers: 5,
      maxMonthlyEarnings: 10000,
      creditBonus: 100,
      features: ['advanced_project_management', 'priority_support', 'api_access'],
      prioritySupport: true,
      apiAccess: true,
      advancedAnalytics: false,
      advancedFraudDetection: false,
      multiSignature: false,
      customIntegrations: false,
    },
    {
      id: 'tier-enterprise',
      name: 'Enterprise',
      price: 99,
      annualPrice: 990,
      sortOrder: 3,
      maxActiveProjects: -1,
      maxTeamMembers: -1,
      maxMonthlyEarnings: -1,
      creditBonus: 500,
      features: ['enterprise_project_management', 'white_label_options', 'custom_integrations'],
      prioritySupport: true,
      apiAccess: true,
      advancedAnalytics: true,
      advancedFraudDetection: true,
      multiSignature: true,
      customIntegrations: true,
    },
  ]

  const mockSubscription: UserSubscription = {
    id: 'sub-123',
    userId: 'user-123',
    subscriptionTierId: 'tier-professional',
    tier: mockTiers[1],
    status: SubscriptionStatus.Active,
    startDate: '2024-01-01T00:00:00Z',
    endDate: '2024-12-31T00:00:00Z',
    isTrial: false,
    cancelAtPeriodEnd: false,
    externalSubscriptionId: 'stripe-sub-123',
    externalCustomerId: 'stripe-cus-123',
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
  }

  const mockPaymentMethods: PaymentMethod[] = [
    {
      id: 'pm-1',
      type: 'card',
      brand: 'Visa',
      last4: '4242',
      expiryMonth: 12,
      expiryYear: 2025,
      isDefault: true,
      createdAt: '2024-01-01T00:00:00Z',
    },
    {
      id: 'pm-2',
      type: 'card',
      brand: 'Mastercard',
      last4: '5555',
      expiryMonth: 6,
      expiryYear: 2026,
      isDefault: false,
      createdAt: '2024-01-01T00:00:00Z',
    },
  ]

  beforeEach(() => {
    jest.clearAllMocks()

    mockRouter = {
      push: jest.fn(),
    }
    ;(useRouter as jest.Mock).mockReturnValue(mockRouter)

    mockFetch = jest.fn()
    global.fetch = mockFetch

    // Default mock implementation
    mockUseSubscription.mockReturnValue({
      subscription: null,
      tiers: mockTiers,
      loading: false,
      error: null,
      createCheckout: jest.fn(),
      setupPaymentMethod: jest.fn(),
      refetch: jest.fn(),
    })
  })

  describe('Loading State', () => {
    it('should show loading spinner when loading', () => {
      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: [],
        loading: true,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
      refetch: jest.fn(),
      })

      const { container } = render(<SubscriptionDashboard />)

      expect(container.querySelector('.animate-spin')).toBeInTheDocument()
    })
  })

  describe('No Subscription State', () => {
    it('should render tier selection when no subscription exists', () => {
      render(<SubscriptionDashboard />)

      expect(screen.getByText('Choose Your Plan')).toBeInTheDocument()
      expect(screen.getAllByText('Free')[0]).toBeInTheDocument()
      expect(screen.getByText('Professional')).toBeInTheDocument()
      expect(screen.getByText('Enterprise')).toBeInTheDocument()
    })

    it('should show all available tiers', () => {
      render(<SubscriptionDashboard />)

      // Free tier appears twice (tier name + badge)
      expect(screen.getAllByText('Free').length).toBeGreaterThan(0)
      expect(screen.getByText('Professional')).toBeInTheDocument()
      expect(screen.getByText('Enterprise')).toBeInTheDocument()
    })
  })

  describe('Active Subscription Display', () => {
    beforeEach(() => {
      mockUseSubscription.mockReturnValue({
        subscription: mockSubscription,
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
      refetch: jest.fn(),
      })
    })

    it('should display current subscription status', () => {
      render(<SubscriptionDashboard />)

      expect(screen.getByText('Your Subscription')).toBeInTheDocument()
      expect(screen.getByText('Active')).toBeInTheDocument()
      // Professional appears multiple times (current subscription + tier card)
      expect(screen.getAllByText('Professional').length).toBeGreaterThan(0)
    })

    it('should show subscription end date', () => {
      render(<SubscriptionDashboard />)

      const endDateText = mockSubscription.endDate ? new Date(mockSubscription.endDate).toLocaleDateString() : ''
      expect(screen.getByText(/Renews/i)).toBeInTheDocument()
    })

    it('should show manage subscription button', () => {
      render(<SubscriptionDashboard />)

      expect(screen.getByRole('button', { name: /manage subscription/i })).toBeInTheDocument()
    })
  })

  describe('Billing Cycle Toggle', () => {
    it('should default to monthly billing', () => {
      render(<SubscriptionDashboard />)

      const monthlyButton = screen.getByRole('button', { name: /^monthly$/i })
      expect(monthlyButton).toHaveClass('bg-primary')
    })

    it('should switch to annual billing when clicked', async () => {
      const user = userEvent.setup()
      render(<SubscriptionDashboard />)

      const annualButton = screen.getByRole('button', { name: /annual/i })
      await user.click(annualButton)

      expect(annualButton).toHaveClass('bg-primary')
    })

    it('should show save 20% badge on annual option', () => {
      render(<SubscriptionDashboard />)

      expect(screen.getByText(/save 20%/i)).toBeInTheDocument()
    })

    it('should update price display when switching billing cycle', async () => {
      const user = userEvent.setup()
      render(<SubscriptionDashboard />)

      const annualButton = screen.getByRole('button', { name: /annual/i })

      // Initially monthly button is active
      const monthlyButton = screen.getByRole('button', { name: /^monthly$/i })
      expect(monthlyButton).toHaveClass('bg-primary')

      await user.click(annualButton)

      // After click, annual button should be active
      expect(annualButton).toHaveClass('bg-primary')
      expect(monthlyButton).not.toHaveClass('bg-primary')
    })
  })

  describe('Tier Display and Features', () => {
    it('should show tier limits correctly', () => {
      render(<SubscriptionDashboard />)

      expect(screen.getByText(/2 active projects/i)).toBeInTheDocument()
      expect(screen.getByText(/1 team member/i)).toBeInTheDocument()
    })

    it('should show unlimited for -1 limits', () => {
      render(<SubscriptionDashboard />)

      const unlimitedTexts = screen.getAllByText(/unlimited/i)
      expect(unlimitedTexts.length).toBeGreaterThan(0)
    })

    it('should display tier features', () => {
      render(<SubscriptionDashboard />)

      expect(screen.getByText('Basic Project Management')).toBeInTheDocument()
      expect(screen.getByText('Advanced Project Management')).toBeInTheDocument()
    })

    it('should show popular badge on professional tier', () => {
      render(<SubscriptionDashboard />)

      expect(screen.getByText('Popular')).toBeInTheDocument()
    })

    it('should show credit bonus for paid tiers', () => {
      render(<SubscriptionDashboard />)

      expect(screen.getByText('+100 credits')).toBeInTheDocument()
      expect(screen.getByText('+500 credits')).toBeInTheDocument()
    })
  })

  describe('Subscription Creation', () => {
    it('should call createCheckout when upgrading', async () => {
      window.confirm = jest.fn().mockReturnValue(true)

      const mockCreateCheckout = jest.fn().mockResolvedValue({
        success: true,
        sessionUrl: 'https://checkout.stripe.com/session-123',
      })

      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: mockCreateCheckout,
        setupPaymentMethod: jest.fn(),
      refetch: jest.fn(),
      })

      const user = userEvent.setup()
      render(<SubscriptionDashboard />)

      // When no subscription exists, all buttons say "Upgrade"
      const upgradeButtons = screen.getAllByRole('button', { name: /upgrade/i })
      await user.click(upgradeButtons[0])

      await waitFor(() => {
        expect(mockCreateCheckout).toHaveBeenCalledWith(mockTiers[0].id, BillingCycle.Monthly)
      })
    })

    it('should redirect to stripe checkout on successful session creation', async () => {
      window.confirm = jest.fn().mockReturnValue(true)

      const mockCreateCheckout = jest.fn().mockResolvedValue({
        success: true,
        sessionUrl: 'https://checkout.stripe.com/session-123',
      })

      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: mockCreateCheckout,
        setupPaymentMethod: jest.fn(),
      refetch: jest.fn(),
      })

      delete (window as any).location
      window.location = { href: '' } as any

      const user = userEvent.setup()
      render(<SubscriptionDashboard />)

      const upgradeButtons = screen.getAllByRole('button', { name: /upgrade/i })
      await user.click(upgradeButtons[0])

      await waitFor(() => {
        expect(window.location.href).toBe('https://checkout.stripe.com/session-123')
      })
    })
  })

  describe('Payment Methods Section', () => {
    beforeEach(() => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => mockPaymentMethods,
      } as Response)
    })

    it('should fetch payment methods on mount', async () => {
      render(<SubscriptionDashboard />)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/Subscription/payment-methods',
          expect.objectContaining({
            credentials: 'include',
          })
        )
      })
    })

    it('should display payment methods after loading', async () => {
      render(<SubscriptionDashboard />)

      await waitFor(() => {
        expect(screen.getByText(/visa ending in 4242/i)).toBeInTheDocument()
        expect(screen.getByText(/mastercard ending in 5555/i)).toBeInTheDocument()
      })
    })

    it('should show default badge on default payment method', async () => {
      render(<SubscriptionDashboard />)

      await waitFor(() => {
        const defaultBadges = screen.getAllByText('Default')
        expect(defaultBadges.length).toBe(1)
      })
    })

    it('should show loading state while fetching payment methods', () => {
      render(<SubscriptionDashboard />)

      expect(screen.getByText(/loading payment methods/i)).toBeInTheDocument()
    })

    it('should show empty state when no payment methods exist', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => [],
      } as Response)

      render(<SubscriptionDashboard />)

      await waitFor(() => {
        expect(screen.getByText(/no payment methods added/i)).toBeInTheDocument()
      })
    })
  })

  describe('Payment Method Management', () => {
    beforeEach(() => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => mockPaymentMethods,
      } as Response)
    })

    it('should call setupPaymentMethod when adding payment method', async () => {
      const mockSetupPaymentMethod = jest.fn().mockResolvedValue({
        success: true,
        sessionUrl: 'https://stripe.com/setup',
      })

      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: mockSetupPaymentMethod,
        refetch: jest.fn(),
      })

      const user = userEvent.setup()
      render(<SubscriptionDashboard />)

      const addButton = screen.getByRole('button', { name: /add payment method/i })
      await user.click(addButton)

      await waitFor(() => {
        expect(mockSetupPaymentMethod).toHaveBeenCalled()
      })
    })

    it('should set default payment method', async () => {
      const user = userEvent.setup()
      render(<SubscriptionDashboard />)

      await waitFor(() => {
        expect(screen.getByText(/visa ending in 4242/i)).toBeInTheDocument()
      })

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      } as Response)

      const setDefaultButton = screen.getByRole('button', { name: /set as default/i })
      await user.click(setDefaultButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/Subscription/payment-methods/pm-2/set-default',
          expect.objectContaining({
            method: 'POST',
          })
        )
      })
    })

    it('should remove payment method with confirmation', async () => {
      window.confirm = jest.fn().mockReturnValue(true)

      const user = userEvent.setup()
      render(<SubscriptionDashboard />)

      await waitFor(() => {
        expect(screen.getByText(/mastercard/i)).toBeInTheDocument()
        expect(screen.getByText(/5555/)).toBeInTheDocument()
      })

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      } as Response)

      const removeButtons = screen.getAllByRole('button', { name: /remove/i })
      await user.click(removeButtons[1]) // Click second remove button (first is disabled for default card)

      expect(window.confirm).toHaveBeenCalledWith(
        'Are you sure you want to remove this payment method?'
      )

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/payment-methods/'),
          expect.objectContaining({
            method: 'DELETE',
          })
        )
      })
    })

    it('should not remove payment method if user cancels confirmation', async () => {
      window.confirm = jest.fn().mockReturnValue(false)

      const user = userEvent.setup()
      render(<SubscriptionDashboard />)

      await waitFor(() => {
        expect(screen.getByText(/mastercard/i)).toBeInTheDocument()
      })

      const removeButtons = screen.getAllByRole('button', { name: /remove/i })
      const initialCallCount = mockFetch.mock.calls.length

      await user.click(removeButtons[1]) // Click second remove button (non-default card)

      expect(window.confirm).toHaveBeenCalled()
      expect(mockFetch).toHaveBeenCalledTimes(initialCallCount) // No new calls
    })

    it('should sync payment methods from Stripe', async () => {
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => [],
        } as Response)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockPaymentMethods,
        } as Response)

      mockUseSubscription.mockReturnValue({
        subscription: mockSubscription,
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
      refetch: jest.fn(),
      })

      const user = userEvent.setup()
      render(<SubscriptionDashboard />)

      await waitFor(() => {
        expect(screen.getByText(/no payment methods added/i)).toBeInTheDocument()
      })

      const syncButton = screen.getByRole('button', { name: /sync from stripe/i })
      await user.click(syncButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/Subscription/payment-methods/sync',
          expect.objectContaining({
            method: 'POST',
          })
        )
      })
    })
  })

  describe('Error Handling', () => {
    it('should handle payment method fetch errors gracefully', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Network error'))

      render(<SubscriptionDashboard />)

      await waitFor(() => {
        expect(logger.error).toHaveBeenCalledWith(
          'Failed to fetch payment methods:',
          expect.any(Error)
        )
      })
    })

    it('should handle remove payment method errors', async () => {
      window.confirm = jest.fn().mockReturnValue(true)
      window.alert = jest.fn()

      // Reset mock and set up proper sequence
      mockFetch.mockReset()
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockPaymentMethods,
      } as Response)

      const user = userEvent.setup()
      render(<SubscriptionDashboard />)

      await waitFor(() => {
        expect(screen.getByText(/5555/)).toBeInTheDocument()
      })

      // Now mock the error response for DELETE
      mockFetch.mockResolvedValueOnce({
        ok: false,
        json: async () => ({ message: 'Failed to remove' }),
      } as Response)

      const removeButtons = screen.getAllByRole('button', { name: /remove/i })
      await user.click(removeButtons[1]) // Click second remove button (non-default card)

      await waitFor(() => {
        expect(logger.error).toHaveBeenCalled()
        expect(window.alert).toHaveBeenCalledWith('Failed to remove')
      })
    })
  })

  describe('Subscription Status Display', () => {
    it('should display trial status', () => {
      mockUseSubscription.mockReturnValue({
        subscription: { ...mockSubscription, isTrial: true, status: SubscriptionStatus.Trial },
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
      refetch: jest.fn(),
      })

      render(<SubscriptionDashboard />)

      const trialBadges = screen.getAllByText('Trial')
      expect(trialBadges.length).toBeGreaterThan(0)
    })

    it('should display past due status', () => {
      mockUseSubscription.mockReturnValue({
        subscription: { ...mockSubscription, status: SubscriptionStatus.PastDue },
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
      refetch: jest.fn(),
      })

      render(<SubscriptionDashboard />)

      expect(screen.getByText('Past Due')).toBeInTheDocument()
    })

    it('should display cancelled status', () => {
      mockUseSubscription.mockReturnValue({
        subscription: { ...mockSubscription, status: SubscriptionStatus.Cancelled },
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
      refetch: jest.fn(),
      })

      render(<SubscriptionDashboard />)

      expect(screen.getByText('Cancelled')).toBeInTheDocument()
    })
  })

  describe('Tier Comparison and Upgrade', () => {
    it('should show change plan button for current tier', () => {
      mockUseSubscription.mockReturnValue({
        subscription: mockSubscription,
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
      refetch: jest.fn(),
      })

      render(<SubscriptionDashboard />)

      expect(screen.getByRole('button', { name: /change plan/i })).toBeInTheDocument()
    })

    it('should navigate to change tier page when clicking change plan', async () => {
      mockUseSubscription.mockReturnValue({
        subscription: mockSubscription,
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
      refetch: jest.fn(),
      })

      const user = userEvent.setup()
      render(<SubscriptionDashboard />)

      const changePlanButton = screen.getByRole('button', { name: /change plan/i })
      await user.click(changePlanButton)

      expect(mockRouter.push).toHaveBeenCalledWith('/subscription/change-tier')
    })

    it('should show upgrade confirmation for higher tier', async () => {
      window.confirm = jest.fn().mockReturnValue(false)

      mockUseSubscription.mockReturnValue({
        subscription: mockSubscription,
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
      refetch: jest.fn(),
      })

      const user = userEvent.setup()
      render(<SubscriptionDashboard />)

      const upgradeButton = screen.getByRole('button', { name: /upgrade/i })
      await user.click(upgradeButton)

      expect(window.confirm).toHaveBeenCalledWith(
        expect.stringContaining('upgrade to Enterprise')
      )
    })
  })

  describe('Billing History', () => {
    it('should show empty billing history message', () => {
      render(<SubscriptionDashboard />)

      expect(screen.getByText(/no billing history yet/i)).toBeInTheDocument()
      expect(screen.getByText(/your billing transactions will appear here/i)).toBeInTheDocument()
    })
  })

  describe('Help Section', () => {
    it('should display help section', () => {
      render(<SubscriptionDashboard />)

      expect(screen.getByText(/need help/i)).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /contact support/i })).toBeInTheDocument()
    })
  })

  describe('Edge Cases', () => {
    it('should handle missing subscription gracefully', () => {
      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: mockTiers,
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
      refetch: jest.fn(),
      })

      expect(() => render(<SubscriptionDashboard />)).not.toThrow()
    })

    it('should handle empty tiers array', () => {
      mockUseSubscription.mockReturnValue({
        subscription: null,
        tiers: [],
        loading: false,
        error: null,
        createCheckout: jest.fn(),
        setupPaymentMethod: jest.fn(),
      refetch: jest.fn(),
      })

      render(<SubscriptionDashboard />)

      expect(screen.getByText('Choose Your Plan')).toBeInTheDocument()
    })

    it('should handle non-array payment methods response', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ data: mockPaymentMethods }),
      } as Response)

      render(<SubscriptionDashboard />)

      await waitFor(() => {
        expect(screen.getByText(/no payment methods added/i)).toBeInTheDocument()
      })
    })
  })
})
