/**
 * Tests for Choose Plan page (src/app/subscription/choose-plan/page.tsx)
 *
 * Coverage target: 95%
 * Test strategy:
 * - Redirect to /register when not authenticated
 * - Redirect to /dashboard when already subscribed
 * - Show loading state during auth/subscription check
 * - Render plan selection UI when authenticated and no subscription
 */

import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import ChoosePlanPage from '../page'
import { useAuth } from '@/contexts/AuthContext'
import { useRouter } from 'next/navigation'
import { useSubscription } from '@/lib/subscription-api'

jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}))

jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}))

jest.mock('@/lib/subscription-api', () => ({
  useSubscription: jest.fn(),
}))

jest.mock('@/components/TierSelectionFlow', () => ({
  TierSelectionFlow: ({ onCheckoutSuccess, onCheckoutError }: { onCheckoutSuccess: unknown, onCheckoutError: unknown }) => (
    <div data-testid="tier-selection-flow">Tier Selection Flow</div>
  ),
}))

jest.mock('@/components/ThemeToggle', () => ({
  ThemeToggle: () => <button data-testid="theme-toggle">Toggle</button>,
}))

jest.mock('next/link', () => {
  const MockLink = ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  )
  MockLink.displayName = 'MockLink'
  return MockLink
})

const mockUseAuth = useAuth as jest.Mock
const mockUseRouter = useRouter as jest.Mock
const mockUseSubscription = useSubscription as jest.Mock
const mockPush = jest.fn()

describe('ChoosePlanPage', () => {
  beforeEach(() => {
    jest.clearAllMocks()

    mockUseRouter.mockReturnValue({ push: mockPush })
  })

  // ============================================
  // Loading State
  // ============================================
  describe('Loading State', () => {
    it('should show loading spinner while auth is loading', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: true,
      })
      mockUseSubscription.mockReturnValue({
        subscription: null,
        loading: false,
      })

      render(<ChoosePlanPage />)

      expect(screen.getByText('Loading plans...')).toBeInTheDocument()
    })

    it('should show loading spinner while subscription is loading', () => {
      mockUseAuth.mockReturnValue({
        user: { userName: 'johndoe' },
        isAuthenticated: true,
        isLoading: false,
      })
      mockUseSubscription.mockReturnValue({
        subscription: null,
        loading: true,
      })

      render(<ChoosePlanPage />)

      expect(screen.getByText('Loading plans...')).toBeInTheDocument()
    })
  })

  // ============================================
  // Authentication Guard
  // ============================================
  describe('Authentication Guard', () => {
    it('should redirect to /register when not authenticated', async () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
      })
      mockUseSubscription.mockReturnValue({
        subscription: null,
        loading: false,
      })

      render(<ChoosePlanPage />)

      await waitFor(() => {
        expect(mockPush).toHaveBeenCalledWith('/register')
      })
    })

    it('should return null while unauthenticated', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
      })
      mockUseSubscription.mockReturnValue({
        subscription: null,
        loading: false,
      })

      const { container } = render(<ChoosePlanPage />)

      expect(container.firstChild).toBeNull()
    })
  })

  // ============================================
  // Subscription Guard
  // ============================================
  describe('Subscription Guard', () => {
    it('should redirect to /dashboard when user already has a subscription', async () => {
      mockUseAuth.mockReturnValue({
        user: { userName: 'johndoe' },
        isAuthenticated: true,
        isLoading: false,
      })
      mockUseSubscription.mockReturnValue({
        subscription: { id: 'sub-123', status: 'Active' },
        loading: false,
      })

      render(<ChoosePlanPage />)

      await waitFor(() => {
        expect(mockPush).toHaveBeenCalledWith('/dashboard')
      })
    })

    it('should not redirect to dashboard when subscription is null', async () => {
      mockUseAuth.mockReturnValue({
        user: { userName: 'johndoe' },
        isAuthenticated: true,
        isLoading: false,
      })
      mockUseSubscription.mockReturnValue({
        subscription: null,
        loading: false,
      })

      render(<ChoosePlanPage />)

      await waitFor(() => {
        expect(mockPush).not.toHaveBeenCalledWith('/dashboard')
      })
    })
  })

  // ============================================
  // Authenticated, No Subscription — Main UI
  // ============================================
  describe('Main UI', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: { userName: 'johndoe' },
        isAuthenticated: true,
        isLoading: false,
      })
      mockUseSubscription.mockReturnValue({
        subscription: null,
        loading: false,
      })
    })

    it('should render "Choose Your Plan" heading', () => {
      render(<ChoosePlanPage />)

      expect(screen.getByText('Choose Your Plan')).toBeInTheDocument()
    })

    it('should display 30-day free trial badge', () => {
      render(<ChoosePlanPage />)

      expect(screen.getByText('All plans include a 30-day free trial')).toBeInTheDocument()
    })

    it('should render TierSelectionFlow component', () => {
      render(<ChoosePlanPage />)

      expect(screen.getByTestId('tier-selection-flow')).toBeInTheDocument()
    })

    it('should display trust indicators', () => {
      render(<ChoosePlanPage />)

      expect(screen.getByText('30-Day Trial')).toBeInTheDocument()
      expect(screen.getByText('Cancel Anytime')).toBeInTheDocument()
      expect(screen.getByText('Secure Checkout')).toBeInTheDocument()
    })

    it('should show Back to Home link', () => {
      render(<ChoosePlanPage />)

      const backLink = screen.getByText('Back to Home')
      expect(backLink.closest('a')).toHaveAttribute('href', '/')
    })

    it('should display user name in navigation', () => {
      render(<ChoosePlanPage />)

      expect(screen.getByText('johndoe')).toBeInTheDocument()
    })

    it('should not show "Back to Dashboard" link', () => {
      render(<ChoosePlanPage />)

      expect(screen.queryByText('Back to Dashboard')).not.toBeInTheDocument()
    })

    it('should show theme toggle', () => {
      render(<ChoosePlanPage />)

      expect(screen.getByTestId('theme-toggle')).toBeInTheDocument()
    })
  })
})
