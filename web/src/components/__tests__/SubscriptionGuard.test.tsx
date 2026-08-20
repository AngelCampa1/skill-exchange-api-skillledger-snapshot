import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {
  SubscriptionGuardFallback,
  SubscriptionGuard,
  ProjectCreationGuard,
  AdvancedFeaturesGuard,
  ApiAccessGuard,
  EnterpriseGuard,
} from '../SubscriptionGuard'
import { useSubscriptionGuard } from '@/hooks/useSubscriptionGuard'
import { useRouter } from 'next/navigation'

// Mock dependencies
jest.mock('@/hooks/useSubscriptionGuard')
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}))

const mockUseSubscriptionGuard = useSubscriptionGuard as jest.Mock
const mockUseRouter = useRouter as jest.Mock

const mockRouter = {
  push: jest.fn(),
  back: jest.fn(),
  forward: jest.fn(),
  refresh: jest.fn(),
  replace: jest.fn(),
  prefetch: jest.fn(),
}

global.fetch = jest.fn()
const mockFetch = global.fetch as jest.Mock

describe('SubscriptionGuardFallback', () => {
  const mockRedirect = jest.fn()

  beforeEach(() => {
    jest.clearAllMocks()
  })

  // ============================================
  // Custom Message Display (2 tests)
  // ============================================
  describe('Custom Message Display', () => {
    it('should display custom message when provided', () => {
      render(
        <SubscriptionGuardFallback
          customMessage="You need to upgrade to access this feature"
          redirectToUpgrade={mockRedirect}
        />
      )

      expect(screen.getByText('Access Restricted')).toBeInTheDocument()
      expect(screen.getByText('You need to upgrade to access this feature')).toBeInTheDocument()
    })

    it('should show error icon with custom message', () => {
      const { container } = render(
        <SubscriptionGuardFallback
          customMessage="Custom error message"
          redirectToUpgrade={mockRedirect}
        />
      )

      const errorIcon = container.querySelector('.text-error')
      expect(errorIcon).toBeInTheDocument()
    })
  })

  // ============================================
  // Default Display (3 tests)
  // ============================================
  describe('Default Display', () => {
    it('should display default "Premium Feature" heading', () => {
      render(<SubscriptionGuardFallback redirectToUpgrade={mockRedirect} />)

      expect(screen.getByText('Premium Feature')).toBeInTheDocument()
    })

    it('should display custom reason when provided', () => {
      render(
        <SubscriptionGuardFallback
          reason="You need a Pro plan for this"
          redirectToUpgrade={mockRedirect}
        />
      )

      expect(screen.getByText('You need a Pro plan for this')).toBeInTheDocument()
    })

    it('should display default reason when not provided', () => {
      render(<SubscriptionGuardFallback redirectToUpgrade={mockRedirect} />)

      expect(screen.getByText('This feature requires an active subscription')).toBeInTheDocument()
    })
  })

  // ============================================
  // Upgrade Prompt Display (4 tests)
  // ============================================
  describe('Upgrade Prompt Display', () => {
    it('should show upgrade section when upgradeRequired is true', () => {
      render(
        <SubscriptionGuardFallback
          upgradeRequired={true}
          redirectToUpgrade={mockRedirect}
        />
      )

      expect(screen.getByText('Unlock This Feature')).toBeInTheDocument()
      expect(screen.getByText('Upgrade Now')).toBeInTheDocument()
    })

    it('should not show upgrade section when upgradeRequired is false', () => {
      render(
        <SubscriptionGuardFallback
          upgradeRequired={false}
          redirectToUpgrade={mockRedirect}
        />
      )

      expect(screen.queryByText('Unlock This Feature')).not.toBeInTheDocument()
    })

    it('should not show upgrade section when showUpgradePrompt is false', () => {
      render(
        <SubscriptionGuardFallback
          upgradeRequired={true}
          showUpgradePrompt={false}
          redirectToUpgrade={mockRedirect}
        />
      )

      expect(screen.queryByText('Unlock This Feature')).not.toBeInTheDocument()
    })

    it('should display all 4 premium features in upgrade section', () => {
      render(
        <SubscriptionGuardFallback
          upgradeRequired={true}
          redirectToUpgrade={mockRedirect}
        />
      )

      expect(screen.getByText(/Unlimited projects and collaborations/)).toBeInTheDocument()
      expect(screen.getByText(/Advanced analytics and reporting/)).toBeInTheDocument()
      expect(screen.getByText(/Priority customer support/)).toBeInTheDocument()
      expect(screen.getByText(/API access and integrations/)).toBeInTheDocument()
    })
  })

  // ============================================
  // Button Interactions (2 tests)
  // ============================================
  describe('Button Interactions', () => {
    it('should call redirectToUpgrade when Upgrade Now button clicked', async () => {
      const user = userEvent.setup()

      render(
        <SubscriptionGuardFallback
          upgradeRequired={true}
          redirectToUpgrade={mockRedirect}
        />
      )

      const upgradeButton = screen.getByText('Upgrade Now')
      await user.click(upgradeButton)

      expect(mockRedirect).toHaveBeenCalledTimes(1)
    })

    it('should have link to subscription page for View All Plans', () => {
      render(
        <SubscriptionGuardFallback
          upgradeRequired={true}
          redirectToUpgrade={mockRedirect}
        />
      )

      const viewPlansLink = screen.getByText('View All Plans')
      expect(viewPlansLink.closest('a')).toHaveAttribute('href', '/subscription')
    })
  })
})

describe('SubscriptionGuard', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    mockUseRouter.mockReturnValue(mockRouter)
  })

  // ============================================
  // Loading State (2 tests)
  // ============================================
  describe('Loading State', () => {
    it('should display loading spinner when isLoading is true', () => {
      mockUseSubscriptionGuard.mockReturnValue({
        canAccess: false,
        isLoading: true,
        error: null,
        reason: null,
        upgradeRequired: false,
        redirectToUpgrade: jest.fn(),
      })

      render(
        <SubscriptionGuard>
          <div>Protected Content</div>
        </SubscriptionGuard>
      )

      expect(screen.getByText('Verifying access...')).toBeInTheDocument()
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
    })

    it('should apply custom className to loading container', () => {
      mockUseSubscriptionGuard.mockReturnValue({
        canAccess: false,
        isLoading: true,
        error: null,
        reason: null,
        upgradeRequired: false,
        redirectToUpgrade: jest.fn(),
      })

      const { container } = render(
        <SubscriptionGuard className="custom-class">
          <div>Protected Content</div>
        </SubscriptionGuard>
      )

      const loadingContainer = container.querySelector('.custom-class')
      expect(loadingContainer).toBeInTheDocument()
    })
  })

  // ============================================
  // Error State (2 tests)
  // ============================================
  describe('Error State', () => {
    it('should display error message when error exists', () => {
      mockUseSubscriptionGuard.mockReturnValue({
        canAccess: false,
        isLoading: false,
        error: 'Failed to verify subscription',
        reason: null,
        upgradeRequired: false,
        redirectToUpgrade: jest.fn(),
      })

      render(
        <SubscriptionGuard>
          <div>Protected Content</div>
        </SubscriptionGuard>
      )

      expect(screen.getByText(/Error verifying subscription: Failed to verify subscription/)).toBeInTheDocument()
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
    })

    it('should show error icon in error state', () => {
      mockUseSubscriptionGuard.mockReturnValue({
        canAccess: false,
        isLoading: false,
        error: 'Network error',
        reason: null,
        upgradeRequired: false,
        redirectToUpgrade: jest.fn(),
      })

      const { container } = render(
        <SubscriptionGuard>
          <div>Protected Content</div>
        </SubscriptionGuard>
      )

      const errorIcon = container.querySelector('.text-error')
      expect(errorIcon).toBeInTheDocument()
    })
  })

  // ============================================
  // Access Granted (2 tests)
  // ============================================
  describe('Access Granted', () => {
    it('should render children when canAccess is true', () => {
      mockUseSubscriptionGuard.mockReturnValue({
        canAccess: true,
        isLoading: false,
        error: null,
        reason: null,
        upgradeRequired: false,
        redirectToUpgrade: jest.fn(),
      })

      render(
        <SubscriptionGuard>
          <div>Protected Content</div>
        </SubscriptionGuard>
      )

      expect(screen.getByText('Protected Content')).toBeInTheDocument()
    })

    it('should not show fallback when access is granted', () => {
      mockUseSubscriptionGuard.mockReturnValue({
        canAccess: true,
        isLoading: false,
        error: null,
        reason: null,
        upgradeRequired: false,
        redirectToUpgrade: jest.fn(),
      })

      render(
        <SubscriptionGuard>
          <div>Protected Content</div>
        </SubscriptionGuard>
      )

      expect(screen.queryByText('Premium Feature')).not.toBeInTheDocument()
    })
  })

  // ============================================
  // Access Denied (4 tests)
  // ============================================
  describe('Access Denied', () => {
    it('should show custom fallback when access denied and fallback provided', () => {
      mockUseSubscriptionGuard.mockReturnValue({
        canAccess: false,
        isLoading: false,
        error: null,
        reason: 'Subscription required',
        upgradeRequired: true,
        redirectToUpgrade: jest.fn(),
      })

      render(
        <SubscriptionGuard fallback={<div>Custom Fallback</div>}>
          <div>Protected Content</div>
        </SubscriptionGuard>
      )

      expect(screen.getByText('Custom Fallback')).toBeInTheDocument()
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
    })

    it('should show default SubscriptionGuardFallback when access denied without fallback', () => {
      mockUseSubscriptionGuard.mockReturnValue({
        canAccess: false,
        isLoading: false,
        error: null,
        reason: 'Premium subscription required',
        upgradeRequired: true,
        redirectToUpgrade: jest.fn(),
      })

      render(
        <SubscriptionGuard>
          <div>Protected Content</div>
        </SubscriptionGuard>
      )

      expect(screen.getByText('Premium Feature')).toBeInTheDocument()
      expect(screen.getByText('Premium subscription required')).toBeInTheDocument()
    })

    it('should pass upgradeRequired to fallback', () => {
      const mockRedirect = jest.fn()
      mockUseSubscriptionGuard.mockReturnValue({
        canAccess: false,
        isLoading: false,
        error: null,
        reason: 'Upgrade needed',
        upgradeRequired: true,
        redirectToUpgrade: mockRedirect,
      })

      render(
        <SubscriptionGuard>
          <div>Protected Content</div>
        </SubscriptionGuard>
      )

      expect(screen.getByText('Unlock This Feature')).toBeInTheDocument()
    })

    it('should respect showUpgradePrompt prop', () => {
      mockUseSubscriptionGuard.mockReturnValue({
        canAccess: false,
        isLoading: false,
        error: null,
        reason: 'Upgrade needed',
        upgradeRequired: true,
        redirectToUpgrade: jest.fn(),
      })

      render(
        <SubscriptionGuard showUpgradePrompt={false}>
          <div>Protected Content</div>
        </SubscriptionGuard>
      )

      expect(screen.queryByText('Unlock This Feature')).not.toBeInTheDocument()
    })
  })

  // ============================================
  // Options Passing (1 test)
  // ============================================
  describe('Options Passing', () => {
    it('should pass options to useSubscriptionGuard hook', () => {
      const options = { maxProjects: 5, requiredFeatures: ['analytics'] }

      mockUseSubscriptionGuard.mockReturnValue({
        canAccess: true,
        isLoading: false,
        error: null,
        reason: null,
        upgradeRequired: false,
        redirectToUpgrade: jest.fn(),
      })

      render(
        <SubscriptionGuard options={options}>
          <div>Protected Content</div>
        </SubscriptionGuard>
      )

      expect(mockUseSubscriptionGuard).toHaveBeenCalledWith(options)
    })
  })
})

describe('ProjectCreationGuard', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    mockFetch.mockClear()
    mockUseSubscriptionGuard.mockReturnValue({
      canAccess: true,
      isLoading: false,
      error: null,
      reason: null,
      upgradeRequired: false,
      redirectToUpgrade: jest.fn(),
    })
  })

  // ============================================
  // Profile Check Loading (1 test)
  // ============================================
  describe('Profile Check Loading', () => {
    it('should show loading state while checking profile', async () => {
      mockFetch.mockImplementation(() => new Promise(() => {})) // Never resolves

      render(
        <ProjectCreationGuard>
          <div>Create Project Form</div>
        </ProjectCreationGuard>
      )

      await waitFor(() => {
        expect(screen.getByText('Checking profile status...')).toBeInTheDocument()
      }, { timeout: 500 })
    })
  })

  // ============================================
  // Profile Incomplete (3 tests) - BUG-003
  // ============================================
  describe('Profile Incomplete - BUG-003 Fix', () => {
    it('should show profile completion message when profile does not exist', async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 404,
      } as Response)

      render(
        <ProjectCreationGuard>
          <div>Create Project Form</div>
        </ProjectCreationGuard>
      )

      await waitFor(() => {
        expect(screen.getByText('Complete Your Profile First')).toBeInTheDocument()
      })

      expect(screen.getByText(/Before creating a project, you need to complete your profile/)).toBeInTheDocument()
    })

    it('should show profile completion when missing required fields', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          profile: {
            id: 'user-123',
            firstName: 'John',
            // Missing lastName and skills
          },
        }),
      } as Response)

      render(
        <ProjectCreationGuard>
          <div>Create Project Form</div>
        </ProjectCreationGuard>
      )

      await waitFor(() => {
        expect(screen.getByText('Complete Your Profile First')).toBeInTheDocument()
      })
    })

    it('should have links to complete profile and return to dashboard', async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 404,
      } as Response)

      render(
        <ProjectCreationGuard>
          <div>Create Project Form</div>
        </ProjectCreationGuard>
      )

      await waitFor(() => {
        const completeProfileLink = screen.getByText('Complete Profile')
        expect(completeProfileLink.closest('a')).toHaveAttribute('href', '/profile/create')
      })

      const dashboardLink = screen.getByText('Return to Dashboard')
      expect(dashboardLink.closest('a')).toHaveAttribute('href', '/')
    })
  })

  // ============================================
  // Profile Complete with Skills Array (2 tests)
  // ============================================
  describe('Profile Complete with Skills', () => {
    it('should allow access when profile has all required fields with skills array', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          profile: {
            id: 'user-123',
            firstName: 'John',
            lastName: 'Doe',
            skills: ['JavaScript', 'React'],
          },
        }),
      } as Response)

      render(
        <ProjectCreationGuard>
          <div>Create Project Form</div>
        </ProjectCreationGuard>
      )

      await waitFor(() => {
        expect(screen.getByText('Create Project Form')).toBeInTheDocument()
      })
    })

    it('should allow access when profile has userSkills array', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          profile: {
            id: 'user-123',
            firstName: 'John',
            lastName: 'Doe',
            userSkills: [{ skill: 'JavaScript' }],
          },
        }),
      } as Response)

      render(
        <ProjectCreationGuard>
          <div>Create Project Form</div>
        </ProjectCreationGuard>
      )

      await waitFor(() => {
        expect(screen.getByText('Create Project Form')).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Error Handling (2 tests) - BUG-003
  // ============================================
  describe('Error Handling - BUG-003 Fix', () => {
    it('should allow access on API error to not block users', async () => {
      mockFetch.mockRejectedValue(new Error('Network error'))

      render(
        <ProjectCreationGuard>
          <div>Create Project Form</div>
        </ProjectCreationGuard>
      )

      await waitFor(() => {
        expect(screen.getByText('Create Project Form')).toBeInTheDocument()
      })
    })

    it('should allow access on 500 error to not block users', async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 500,
      } as Response)

      render(
        <ProjectCreationGuard>
          <div>Create Project Form</div>
        </ProjectCreationGuard>
      )

      await waitFor(() => {
        expect(screen.getByText('Create Project Form')).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Subscription Guard Integration (1 test)
  // ============================================
  describe('Subscription Guard Integration', () => {
    it('should check subscription limits after profile check passes', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({
          profile: {
            id: 'user-123',
            firstName: 'John',
            lastName: 'Doe',
            skills: ['JavaScript'],
          },
        }),
      } as Response)

      mockUseSubscriptionGuard.mockReturnValue({
        canAccess: false,
        isLoading: false,
        error: null,
        reason: 'Max projects reached',
        upgradeRequired: true,
        redirectToUpgrade: jest.fn(),
      })

      render(
        <ProjectCreationGuard>
          <div>Create Project Form</div>
        </ProjectCreationGuard>
      )

      await waitFor(() => {
        expect(screen.getByText('Project Limit Reached')).toBeInTheDocument()
      })

      expect(mockUseSubscriptionGuard).toHaveBeenCalledWith({ maxProjects: 1 })
    })
  })
})

describe('AdvancedFeaturesGuard', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  it('should render children when access is granted', () => {
    mockUseSubscriptionGuard.mockReturnValue({
      canAccess: true,
      isLoading: false,
      error: null,
      reason: null,
      upgradeRequired: false,
      redirectToUpgrade: jest.fn(),
    })

    render(
      <AdvancedFeaturesGuard>
        <div>Advanced Feature Content</div>
      </AdvancedFeaturesGuard>
    )

    expect(screen.getByText('Advanced Feature Content')).toBeInTheDocument()
  })

  it('should show custom fallback when access is denied', () => {
    mockUseSubscriptionGuard.mockReturnValue({
      canAccess: false,
      isLoading: false,
      error: null,
      reason: 'Advanced plan required',
      upgradeRequired: true,
      redirectToUpgrade: jest.fn(),
    })

    render(
      <AdvancedFeaturesGuard>
        <div>Advanced Feature Content</div>
      </AdvancedFeaturesGuard>
    )

    expect(screen.getByText('Premium Feature')).toBeInTheDocument()
    expect(screen.queryByText('Advanced Feature Content')).not.toBeInTheDocument()
  })

  it('should pass correct options to useSubscriptionGuard', () => {
    mockUseSubscriptionGuard.mockReturnValue({
      canAccess: true,
      isLoading: false,
      error: null,
      reason: null,
      upgradeRequired: false,
      redirectToUpgrade: jest.fn(),
    })

    render(
      <AdvancedFeaturesGuard>
        <div>Advanced Feature Content</div>
      </AdvancedFeaturesGuard>
    )

    expect(mockUseSubscriptionGuard).toHaveBeenCalledWith({
      requiredFeatures: ['advancedAnalytics', 'apiAccess'],
    })
  })
})

describe('ApiAccessGuard', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  it('should render children when access is granted', () => {
    mockUseSubscriptionGuard.mockReturnValue({
      canAccess: true,
      isLoading: false,
      error: null,
      reason: null,
      upgradeRequired: false,
      redirectToUpgrade: jest.fn(),
    })

    render(
      <ApiAccessGuard>
        <div>API Access Content</div>
      </ApiAccessGuard>
    )

    expect(screen.getByText('API Access Content')).toBeInTheDocument()
  })

  it('should show custom fallback when access is denied', () => {
    mockUseSubscriptionGuard.mockReturnValue({
      canAccess: false,
      isLoading: false,
      error: null,
      reason: 'Professional plan required',
      upgradeRequired: true,
      redirectToUpgrade: jest.fn(),
    })

    render(
      <ApiAccessGuard>
        <div>API Access Content</div>
      </ApiAccessGuard>
    )

    expect(screen.getByText('API Access Required')).toBeInTheDocument()
  })

  it('should pass correct options to useSubscriptionGuard', () => {
    mockUseSubscriptionGuard.mockReturnValue({
      canAccess: true,
      isLoading: false,
      error: null,
      reason: null,
      upgradeRequired: false,
      redirectToUpgrade: jest.fn(),
    })

    render(
      <ApiAccessGuard>
        <div>API Access Content</div>
      </ApiAccessGuard>
    )

    expect(mockUseSubscriptionGuard).toHaveBeenCalledWith({
      requiredFeatures: ['apiAccess'],
    })
  })
})

describe('EnterpriseGuard', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  it('should render children when access is granted', () => {
    mockUseSubscriptionGuard.mockReturnValue({
      canAccess: true,
      isLoading: false,
      error: null,
      reason: null,
      upgradeRequired: false,
      redirectToUpgrade: jest.fn(),
    })

    render(
      <EnterpriseGuard>
        <div>Enterprise Feature Content</div>
      </EnterpriseGuard>
    )

    expect(screen.getByText('Enterprise Feature Content')).toBeInTheDocument()
  })

  it('should show custom fallback when access is denied', () => {
    mockUseSubscriptionGuard.mockReturnValue({
      canAccess: false,
      isLoading: false,
      error: null,
      reason: 'Enterprise plan required',
      upgradeRequired: true,
      redirectToUpgrade: jest.fn(),
    })

    render(
      <EnterpriseGuard>
        <div>Enterprise Feature Content</div>
      </EnterpriseGuard>
    )

    expect(screen.getByText('Enterprise Feature')).toBeInTheDocument()
    expect(screen.getByText(/This feature is available only with an Enterprise plan/)).toBeInTheDocument()
  })

  it('should pass correct options to useSubscriptionGuard', () => {
    mockUseSubscriptionGuard.mockReturnValue({
      canAccess: true,
      isLoading: false,
      error: null,
      reason: null,
      upgradeRequired: false,
      redirectToUpgrade: jest.fn(),
    })

    render(
      <EnterpriseGuard>
        <div>Enterprise Feature Content</div>
      </EnterpriseGuard>
    )

    expect(mockUseSubscriptionGuard).toHaveBeenCalledWith({
      requiredTier: 'Enterprise',
    })
  })
})
