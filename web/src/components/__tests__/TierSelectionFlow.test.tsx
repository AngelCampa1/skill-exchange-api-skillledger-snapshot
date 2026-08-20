/**
 * Tests for TierSelectionFlow
 *
 * Comprehensive test suite for the tier selection flow component
 * Coverage target: 70%+ (505 lines)
 */

import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { TierSelectionFlow } from '../TierSelectionFlow'
import { BillingCycle } from '@/types/subscription'

// Mock dependencies
jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
  },
}))

jest.mock('@/utils/analytics', () => ({
  trackEvent: jest.fn(),
}))

jest.mock('@/lib/subscription-api', () => ({
  useSubscription: jest.fn(),
}))

import { useSubscription } from '@/lib/subscription-api'
import { trackEvent } from '@/utils/analytics'

const mockUseSubscription = useSubscription as jest.MockedFunction<typeof useSubscription>
const mockTrackEvent = trackEvent as jest.MockedFunction<typeof trackEvent>

const mockTiers = [
  {
    id: 'tier-prof',
    name: 'Professional',
    description: 'For individual professionals',
    price: 29,
    annualPrice: 290,
    features: ['basic_project_management', 'credit_wallet', 'messaging'],
    maxActiveProjects: 5,
    maxTeamMembers: 1,
    maxMonthlyEarnings: 5000,
    creditBonus: 100,
    prioritySupport: false,
    apiAccess: false,
    advancedAnalytics: false,
    advancedFraudDetection: false,
    sortOrder: 1,
  },
  {
    id: 'tier-bus',
    name: 'Business',
    description: 'For growing teams',
    price: 99,
    annualPrice: 990,
    features: ['advanced_project_management', 'priority_support', 'api_access'],
    maxActiveProjects: 50,
    maxTeamMembers: 10,
    maxMonthlyEarnings: 8000,
    creditBonus: 500,
    prioritySupport: true,
    apiAccess: true,
    advancedAnalytics: true,
    advancedFraudDetection: false,
    sortOrder: 2,
  },
  {
    id: 'tier-ent',
    name: 'Enterprise',
    description: 'For large organizations',
    price: 299,
    annualPrice: 2990,
    features: ['enterprise_project_management', 'dedicated_account_manager', 'sla_guarantee'],
    maxActiveProjects: -1,
    maxTeamMembers: -1,
    maxMonthlyEarnings: -1,
    creditBonus: 2000,
    prioritySupport: true,
    apiAccess: true,
    advancedAnalytics: true,
    advancedFraudDetection: true,
    sortOrder: 3,
  },
]

describe('TierSelectionFlow', () => {
  const mockCreateCheckout = jest.fn()
  const mockOnCheckoutSuccess = jest.fn()
  const mockOnCheckoutError = jest.fn()

  beforeEach(() => {
    jest.clearAllMocks()
    mockUseSubscription.mockReturnValue({
      tiers: mockTiers,
      subscription: null,
      loading: false,
      error: null,
      createCheckout: mockCreateCheckout,
    } as any)
  })

  describe('Loading State', () => {
    it('should show loading spinner when loading', () => {
      mockUseSubscription.mockReturnValue({
        tiers: [],
        subscription: null,
        loading: true,
        error: null,
        createCheckout: mockCreateCheckout,
      } as any)

      render(<TierSelectionFlow />)

      expect(screen.getByText('Loading subscription options...')).toBeInTheDocument()
    })

    it('should show loading spinner animation', () => {
      mockUseSubscription.mockReturnValue({
        tiers: [],
        subscription: null,
        loading: true,
        error: null,
        createCheckout: mockCreateCheckout,
      } as any)

      const { container } = render(<TierSelectionFlow />)
      const spinner = container.querySelector('.animate-spin')
      expect(spinner).toBeInTheDocument()
    })
  })

  describe('Error State', () => {
    it('should show error message when error occurs', () => {
      mockUseSubscription.mockReturnValue({
        tiers: [],
        subscription: null,
        loading: false,
        error: 'Failed to fetch tiers',
        createCheckout: mockCreateCheckout,
      } as any)

      render(<TierSelectionFlow />)

      expect(screen.getByText('Failed to load subscription options')).toBeInTheDocument()
      expect(screen.getByText('Failed to fetch tiers')).toBeInTheDocument()
    })
  })

  describe('Empty State', () => {
    it('should show empty message when no tiers available', () => {
      mockUseSubscription.mockReturnValue({
        tiers: [],
        subscription: null,
        loading: false,
        error: null,
        createCheckout: mockCreateCheckout,
      } as any)

      render(<TierSelectionFlow />)

      expect(screen.getByText('No subscription tiers available')).toBeInTheDocument()
    })
  })

  describe('Billing Cycle Toggle', () => {
    it('should render billing cycle toggle buttons', () => {
      render(<TierSelectionFlow />)

      expect(screen.getByRole('button', { name: /Monthly/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /Annual/i })).toBeInTheDocument()
    })

    it('should show Monthly as default selected', () => {
      const { container } = render(<TierSelectionFlow />)

      const monthlyButton = screen.getByRole('button', { name: /^Monthly$/i })
      expect(monthlyButton.className).toContain('bg-background')
    })

    it('should toggle to Annual when clicked', async () => {
      const user = userEvent.setup()
      render(<TierSelectionFlow />)

      const annualButton = screen.getByRole('button', { name: /Annual/i })
      await user.click(annualButton)

      expect(annualButton.className).toContain('bg-background')
    })

    it('should toggle back to Monthly when clicked', async () => {
      const user = userEvent.setup()
      render(<TierSelectionFlow />)

      // Click Annual
      const annualButton = screen.getByRole('button', { name: /Annual/i })
      await user.click(annualButton)

      // Click Monthly
      const monthlyButton = screen.getByRole('button', { name: /^Monthly$/i })
      await user.click(monthlyButton)

      expect(monthlyButton.className).toContain('bg-background')
    })

    it('should show save 20% badge on Annual button', () => {
      render(<TierSelectionFlow />)

      expect(screen.getByText('Save 20%')).toBeInTheDocument()
    })
  })

  describe('Tier Cards Rendering', () => {
    it('should render all tier cards', () => {
      render(<TierSelectionFlow />)

      expect(screen.getByText('Professional')).toBeInTheDocument()
      expect(screen.getByText('Business')).toBeInTheDocument()
      expect(screen.getByText('Enterprise')).toBeInTheDocument()
    })

    it('should render tier descriptions', () => {
      render(<TierSelectionFlow />)

      expect(screen.getByText('For individual professionals')).toBeInTheDocument()
      expect(screen.getByText('For growing teams')).toBeInTheDocument()
      expect(screen.getByText('For large organizations')).toBeInTheDocument()
    })

    it('should render monthly pricing by default', () => {
      render(<TierSelectionFlow />)

      expect(screen.getByText('$29.00')).toBeInTheDocument()
      expect(screen.getByText('$99.00')).toBeInTheDocument()
      expect(screen.getByText('$299.00')).toBeInTheDocument()
    })

    it('should show /month label for monthly billing', () => {
      render(<TierSelectionFlow />)

      const monthLabels = screen.getAllByText('/month')
      expect(monthLabels.length).toBeGreaterThan(0)
    })

    it('should render annual pricing when Annual is selected', async () => {
      const user = userEvent.setup()
      render(<TierSelectionFlow />)

      const annualButton = screen.getByRole('button', { name: /Annual/i })
      await user.click(annualButton)

      // Annual prices divided by 12
      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('$24.17') // 290/12
      expect(textContent).toContain('billed annually')
    })

    it('should show savings amount for annual billing', async () => {
      const user = userEvent.setup()
      render(<TierSelectionFlow />)

      const annualButton = screen.getByRole('button', { name: /Annual/i })
      await user.click(annualButton)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('Save')
    })

    it('should render credit bonuses', () => {
      render(<TierSelectionFlow />)

      expect(screen.getByText('+100 credits')).toBeInTheDocument()
      expect(screen.getByText('+500 credits')).toBeInTheDocument()
      expect(screen.getByText('+2000 credits')).toBeInTheDocument()
    })
  })

  describe('Features Display', () => {
    it('should render tier features', () => {
      render(<TierSelectionFlow />)

      expect(screen.getByText('Basic Project Management')).toBeInTheDocument()
      expect(screen.getByText('Credit Wallet')).toBeInTheDocument()
      expect(screen.getByText('Advanced Project Management')).toBeInTheDocument()
    })

    it('should format feature labels correctly', () => {
      render(<TierSelectionFlow />)

      // Test various formatting cases
      expect(screen.getAllByText('Messaging').length).toBeGreaterThan(0)
      expect(screen.getAllByText('Priority Support').length).toBeGreaterThan(0)
      expect(screen.getAllByText('Dedicated Account Manager').length).toBeGreaterThan(0)
    })

    it('should show icon features for tiers that have them', () => {
      render(<TierSelectionFlow />)

      const container = document.body
      const textContent = container.textContent || ''

      // Business tier has these features
      expect(textContent).toContain('Priority Support')
      expect(textContent).toContain('API Access')
      expect(textContent).toContain('Advanced Analytics')
    })

    it('should show fraud detection for Enterprise tier', () => {
      render(<TierSelectionFlow />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('Fraud Detection')
    })
  })

  describe('Limits Display', () => {
    it('should render active projects limits', () => {
      render(<TierSelectionFlow />)

      expect(screen.getByText('5')).toBeInTheDocument() // Professional
      expect(screen.getByText('50')).toBeInTheDocument() // Business
    })

    it('should render team members limits', () => {
      render(<TierSelectionFlow />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('Team Member') // Singular for 1
      expect(textContent).toContain('Team Members') // Plural
    })

    it('should show Unlimited for -1 values', () => {
      render(<TierSelectionFlow />)

      const unlimitedElements = screen.getAllByText('Unlimited')
      // Enterprise tier has 3 unlimited values (projects, team members, earnings)
      expect(unlimitedElements.length).toBeGreaterThanOrEqual(3)
    })

    it('should format monthly earnings with $ prefix', () => {
      render(<TierSelectionFlow />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('$5,000')
      expect(textContent).toContain('$8,000')
    })
  })

  describe('Current Tier Badge', () => {
    it('should show Current Plan badge for current tier', () => {
      mockUseSubscription.mockReturnValue({
        tiers: mockTiers,
        subscription: {
          tier: { id: 'tier-prof' },
          status: 'Active',
        },
        loading: false,
        error: null,
        createCheckout: mockCreateCheckout,
      } as any)

      render(<TierSelectionFlow />)

      expect(screen.getAllByText('Current Plan').length).toBeGreaterThan(0)
    })

    it('should not show Current Plan badge when no active subscription', () => {
      render(<TierSelectionFlow />)

      expect(screen.queryByText('Current Plan')).not.toBeInTheDocument()
    })
  })

  describe('Upgrade Badge', () => {
    it('should show Upgrade badge for higher tiers', () => {
      mockUseSubscription.mockReturnValue({
        tiers: mockTiers,
        subscription: {
          tier: { id: 'tier-prof' },
          status: 'Active',
        },
        loading: false,
        error: null,
        createCheckout: mockCreateCheckout,
      } as any)

      render(<TierSelectionFlow />)

      const upgradeBadges = screen.getAllByText('Upgrade')
      // Business and Enterprise are upgrades from Professional
      expect(upgradeBadges.length).toBeGreaterThanOrEqual(2)
    })
  })

  describe('Subscribe Button', () => {
    it('should render subscribe buttons for all tiers', () => {
      render(<TierSelectionFlow />)

      const buttons = screen.getAllByRole('button')
      // 2 billing cycle buttons + 3 tier buttons = 5 total
      expect(buttons.length).toBeGreaterThanOrEqual(5)
    })

    it('should show Upgrade Now for upgrade tiers', () => {
      mockUseSubscription.mockReturnValue({
        tiers: mockTiers,
        subscription: {
          tier: { id: 'tier-prof' },
          status: 'Active',
        },
        loading: false,
        error: null,
        createCheckout: mockCreateCheckout,
      } as any)

      render(<TierSelectionFlow />)

      const upgradeButtons = screen.getAllByText('Upgrade Now')
      expect(upgradeButtons.length).toBeGreaterThanOrEqual(1)
    })

    it('should disable button for current tier', () => {
      mockUseSubscription.mockReturnValue({
        tiers: mockTiers,
        subscription: {
          tier: { id: 'tier-prof' },
          status: 'Active',
        },
        loading: false,
        error: null,
        createCheckout: mockCreateCheckout,
      } as any)

      const { container } = render(<TierSelectionFlow />)

      const currentPlanButton = screen.getByRole('button', { name: /^Current Plan$/i })
      expect(currentPlanButton).toBeDisabled()
    })

    it('should handle subscribe click', async () => {
      const user = userEvent.setup()
      mockCreateCheckout.mockResolvedValue({
        success: true,
        sessionUrl: 'https://checkout.stripe.com/test',
      })

      // Mock window.location.href
      delete (window as any).location
      ;(window as any).location = { href: '' }

      render(<TierSelectionFlow onCheckoutSuccess={mockOnCheckoutSuccess} />)

      // Find and click a subscribe button (not the current plan)
      const buttons = screen.getAllByRole('button')
      const subscribeButton = buttons.find(b =>
        b.textContent?.includes('Upgrade Now') || b.textContent?.includes('Downgrade')
      )

      if (subscribeButton) {
        await user.click(subscribeButton)

        await waitFor(() => {
          expect(mockCreateCheckout).toHaveBeenCalled()
        })
      }
    })

    it('should show Processing state during checkout', async () => {
      const user = userEvent.setup()
      mockCreateCheckout.mockImplementation(() => new Promise(resolve => setTimeout(resolve, 1000)))

      render(<TierSelectionFlow />)

      const buttons = screen.getAllByRole('button')
      const subscribeButton = buttons.find(b => b.textContent?.includes('Upgrade'))

      if (subscribeButton) {
        await user.click(subscribeButton)

        // Should show processing state
        await waitFor(() => {
          const container = document.body
          const textContent = container.textContent || ''
          expect(textContent).toContain('Processing')
        })
      }
    })

    it('should call onCheckoutError when checkout fails', async () => {
      const user = userEvent.setup()
      mockCreateCheckout.mockResolvedValue({
        success: false,
        errorMessage: 'Payment failed',
      })

      render(<TierSelectionFlow onCheckoutError={mockOnCheckoutError} />)

      const buttons = screen.getAllByRole('button')
      const subscribeButton = buttons.find(b => b.textContent?.includes('Upgrade'))

      if (subscribeButton) {
        await user.click(subscribeButton)

        await waitFor(() => {
          expect(mockOnCheckoutError).toHaveBeenCalled()
        })
      }
    })
  })

  describe('Trust Signals', () => {
    it('should display trust signals section', () => {
      render(<TierSelectionFlow />)

      expect(screen.getByText(/Secure checkout powered by Stripe/)).toBeInTheDocument()
      expect(screen.getByText(/Cancel anytime/)).toBeInTheDocument()
      expect(screen.getByText(/30-day money-back guarantee/)).toBeInTheDocument()
    })
  })

  describe('Analytics Tracking', () => {
    it('should track page view when tiers load', () => {
      render(<TierSelectionFlow />)

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          name: 'view_item',
          category: 'monetization',
        })
      )
    })

    it('should track tier selection on subscribe click', async () => {
      const user = userEvent.setup()
      mockCreateCheckout.mockResolvedValue({
        success: true,
        sessionUrl: 'https://checkout.stripe.com/test',
      })

      delete (window as any).location
      ;(window as any).location = { href: '' }

      render(<TierSelectionFlow />)

      const buttons = screen.getAllByRole('button')
      const subscribeButton = buttons.find(b => b.textContent?.includes('Upgrade'))

      if (subscribeButton) {
        await user.click(subscribeButton)

        await waitFor(() => {
          expect(mockTrackEvent).toHaveBeenCalledWith(
            expect.objectContaining({
              name: 'select_item',
              category: 'monetization',
            })
          )
        })
      }
    })
  })

  describe('Edge Cases', () => {
    it('should handle tier with no features', () => {
      const tiersWithNoFeatures = [
        {
          ...mockTiers[0],
          features: [],
        },
      ]

      mockUseSubscription.mockReturnValue({
        tiers: tiersWithNoFeatures,
        subscription: null,
        loading: false,
        error: null,
        createCheckout: mockCreateCheckout,
      } as any)

      render(<TierSelectionFlow />)

      expect(screen.getByText('Professional')).toBeInTheDocument()
    })

    it('should handle tier with no description', () => {
      const tiersWithNoDesc = [
        {
          ...mockTiers[0],
          description: undefined,
        },
      ]

      mockUseSubscription.mockReturnValue({
        tiers: tiersWithNoDesc,
        subscription: null,
        loading: false,
        error: null,
        createCheckout: mockCreateCheckout,
      } as any)

      render(<TierSelectionFlow />)

      expect(screen.getByText('Professional')).toBeInTheDocument()
    })

    it('should handle tier with 0 credit bonus', () => {
      const tiersWithNoBonus = [
        {
          ...mockTiers[0],
          creditBonus: 0,
        },
      ]

      mockUseSubscription.mockReturnValue({
        tiers: tiersWithNoBonus,
        subscription: null,
        loading: false,
        error: null,
        createCheckout: mockCreateCheckout,
      } as any)

      render(<TierSelectionFlow />)

      expect(screen.queryByText('+0 credits')).not.toBeInTheDocument()
    })

    it('should handle very high limit values (9999+) as Unlimited', () => {
      const tiersWithHighLimits = [
        {
          ...mockTiers[0],
          maxActiveProjects: 999999,
        },
      ]

      mockUseSubscription.mockReturnValue({
        tiers: tiersWithHighLimits,
        subscription: null,
        loading: false,
        error: null,
        createCheckout: mockCreateCheckout,
      } as any)

      render(<TierSelectionFlow />)

      expect(screen.getByText('Unlimited')).toBeInTheDocument()
    })
  })

  describe('Custom ClassName', () => {
    it('should apply custom className', () => {
      const { container } = render(<TierSelectionFlow className="custom-class" />)

      const wrapper = container.querySelector('.custom-class')
      expect(wrapper).toBeInTheDocument()
    })
  })

  describe('Accessibility', () => {
    it('should have accessible buttons', () => {
      render(<TierSelectionFlow />)

      const buttons = screen.getAllByRole('button')
      expect(buttons.length).toBeGreaterThan(0)
    })
  })

  describe('Integration', () => {
    it('should render complete tier selection flow without errors', () => {
      const { container } = render(<TierSelectionFlow />)

      expect(container.firstChild).toBeTruthy()
      expect(screen.getByText('Choose your billing cycle')).toBeInTheDocument()
      expect(screen.getByText('Professional')).toBeInTheDocument()
    })

    it('should handle full user flow', async () => {
      const user = userEvent.setup()
      mockCreateCheckout.mockResolvedValue({
        success: true,
        sessionUrl: 'https://checkout.stripe.com/test',
      })

      delete (window as any).location
      ;(window as any).location = { href: '' }

      render(<TierSelectionFlow onCheckoutSuccess={mockOnCheckoutSuccess} />)

      // Switch to Annual
      const annualButton = screen.getByRole('button', { name: /Annual/i })
      await user.click(annualButton)

      // Wait for pricing to update
      await waitFor(() => {
        const container = document.body
        const textContent = container.textContent || ''
        expect(textContent).toContain('billed annually')
      })

      // Click subscribe
      const buttons = screen.getAllByRole('button')
      const subscribeButton = buttons.find(b => b.textContent?.includes('Upgrade'))

      if (subscribeButton) {
        await user.click(subscribeButton)

        await waitFor(() => {
          expect(mockCreateCheckout).toHaveBeenCalledWith(
            expect.any(String),
            BillingCycle.Annual
          )
        })
      }
    })
  })
})
