import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import DashboardPage from '../page'
import { useAuth } from '@/contexts/AuthContext'
import { useRouter } from 'next/navigation'
import { useSubscription } from '@/lib/subscription-api'

// Mock Next.js router
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}))

// Mock AuthContext
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}))

// Mock useSubscription — authenticated tests need an active subscription
jest.mock('@/lib/subscription-api', () => ({
  useSubscription: jest.fn(),
}))

// Mock Next.js Link
jest.mock('next/link', () => {
  const MockLink = ({ children, href }: any) => <a href={href}>{children}</a>
  MockLink.displayName = 'MockLink'
  return MockLink
})

// Mock child components
jest.mock('@/components/LogoutButton', () => ({
  __esModule: true,
  default: ({ showAllDevicesOption }: any) => (
    <button data-testid="logout-button">
      Logout ({showAllDevicesOption ? 'with devices' : 'no devices'})
    </button>
  ),
}))

jest.mock('@/components/ThemeToggle', () => ({
  ThemeToggle: () => <div data-testid="theme-toggle">ThemeToggle</div>,
}))

jest.mock('@/components/EnhancedNavigation', () => ({
  EnhancedNavigation: () => <div data-testid="enhanced-navigation">EnhancedNavigation</div>,
}))

jest.mock('@/components/EnhancedDashboardContent', () => ({
  EnhancedDashboardContent: () => <div data-testid="enhanced-dashboard-content">EnhancedDashboardContent</div>,
}))

jest.mock('@/components/SubscriptionDashboard', () => ({
  SubscriptionDashboard: () => <div data-testid="subscription-dashboard">SubscriptionDashboard</div>,
}))

jest.mock('@/components/MobileNav', () => ({
  MobileNav: ({ items }: any) => (
    <div data-testid="mobile-nav">
      {items.map((item: any, i: number) => (
        <a key={i} href={item.href}>
          {item.label}
        </a>
      ))}
    </div>
  ),
}))

// Mock logger
jest.mock('@/utils/logger', () => ({
  logger: {
    debug: jest.fn(),
    error: jest.fn(),
  },
}))

const mockRouter = {
  push: jest.fn(),
  back: jest.fn(),
  forward: jest.fn(),
  refresh: jest.fn(),
  replace: jest.fn(),
  prefetch: jest.fn(),
}

const mockUseRouter = useRouter as jest.Mock
const mockUseAuth = useAuth as jest.Mock
const mockUseSubscription = useSubscription as jest.Mock

const mockActiveSubscription = {
  id: 'sub-123',
  status: 'Active',
  tier: { id: 'tier-1', name: 'Professional', sortOrder: 1 },
}

const mockUser = {
  id: 'user-123',
  email: 'john@example.com',
  userName: 'johndoe',
  firstName: 'John',
  lastName: 'Doe',
  emailVerified: true,
  status: 'active',
  roles: ['user', 'premium'],
}

describe('DashboardPage', () => {
  let mockFetch: jest.SpyInstance

  beforeEach(() => {
    jest.clearAllMocks()
    mockUseRouter.mockReturnValue(mockRouter)
    // Default: active subscription — authenticated tests need this to pass subscription guard
    mockUseSubscription.mockReturnValue({
      subscription: mockActiveSubscription,
      tiers: [],
      loading: false,
      error: null,
      createCheckout: jest.fn(),
      setupPaymentMethod: jest.fn(),
      refetch: jest.fn(),
    })
    mockFetch = jest.spyOn(global, 'fetch') as jest.SpyInstance
    delete (window as any).location
    ;(window as any).location = { href: '' }
  })

  afterEach(() => {
    mockFetch.mockRestore()
  })

  // ============================================
  // Loading State (2 tests)
  // ============================================
  describe('Loading State', () => {
    it('should show loading spinner when auth is loading', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: true,
      })

      render(<DashboardPage />)

      expect(screen.getByText('Loading your workspace...')).toBeInTheDocument()
      const spinner = document.querySelector('.loading-spinner')
      expect(spinner).toBeInTheDocument()
    })

    it('should have loading spinner with animation classes', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: true,
      })

      const { container } = render(<DashboardPage />)

      const spinner = container.querySelector('.loading-spinner.animate-glow')
      expect(spinner).toBeInTheDocument()
    })
  })

  // ============================================
  // Unauthenticated State - E2E-017 Fix (4 tests)
  // ============================================
  describe('Unauthenticated State - E2E-017 Fix', () => {
    it('should show redirecting message when not authenticated', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
      })

      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({}),
      } as Response)

      render(<DashboardPage />)

      expect(screen.getByText('Redirecting to login...')).toBeInTheDocument()
    })

    it('should call logout API before redirecting when unauthenticated - E2E-017 FIX', async () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
      })

      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({}),
      } as Response)

      render(<DashboardPage />)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/auth/logout', {
          method: 'POST',
          credentials: 'include',
        })
      })

      expect(window.location.href).toBe('/login')
    })

    it('should redirect to login even if logout API fails', async () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
      })

      mockFetch.mockImplementation(() =>
        Promise.reject(new Error('Network error')).catch(() => {})
      )

      const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {})

      render(<DashboardPage />)

      await waitFor(() => {
        expect(window.location.href).toBe('/login')
      }, { timeout: 5000 })

      consoleSpy.mockRestore()
    })

    it('should not render dashboard content when unauthenticated', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
      })

      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => ({}),
      } as Response)

      render(<DashboardPage />)

      expect(screen.queryByText('Dashboard')).not.toBeInTheDocument()
      expect(screen.queryByText('Profile Overview')).not.toBeInTheDocument()
    })
  })

  // ============================================
  // Authenticated State - Navigation (6 tests)
  // ============================================
  describe('Authenticated State - Navigation', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        isLoading: false,
      })
    })

    it('should display SkillLedger logo in navigation', () => {
      render(<DashboardPage />)

      const logo = screen.getByText('SkillLedger')
      expect(logo).toBeInTheDocument()
      expect(logo.closest('a')).toHaveAttribute('href', '/dashboard')
    })

    it('should display navigation links (Browse Projects, Create Project, Marketplace)', () => {
      render(<DashboardPage />)

      const browseElements = screen.getAllByText('Browse Projects')
      expect(browseElements.length).toBeGreaterThan(0)

      const createElements = screen.getAllByText('Create Project')
      expect(createElements.length).toBeGreaterThan(0)

      const marketplaceElements = screen.getAllByText('Marketplace')
      expect(marketplaceElements.length).toBeGreaterThan(0)
    })

    it('should display mobile navigation', () => {
      render(<DashboardPage />)

      expect(screen.getByTestId('mobile-nav')).toBeInTheDocument()
    })

    it('should display ThemeToggle component', () => {
      render(<DashboardPage />)

      expect(screen.getByTestId('theme-toggle')).toBeInTheDocument()
    })

    it('should display welcome message with firstName - E2E-019 FIX', () => {
      render(<DashboardPage />)

      expect(screen.getByText('Welcome back,')).toBeInTheDocument()
      expect(screen.getByText('John')).toBeInTheDocument()
    })

    it('should fallback to userName when firstName is missing - E2E-015 FIX', () => {
      const userWithoutFirstName = { ...mockUser, firstName: undefined }
      mockUseAuth.mockReturnValue({
        user: userWithoutFirstName,
        isAuthenticated: true,
        isLoading: false,
      })

      render(<DashboardPage />)

      const usernameElements = screen.getAllByText('johndoe')
      expect(usernameElements.length).toBeGreaterThan(0)
    })
  })

  // ============================================
  // Dashboard Header (3 tests)
  // ============================================
  describe('Dashboard Header', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        isLoading: false,
      })
    })

    it('should display Dashboard heading', () => {
      render(<DashboardPage />)

      const heading = screen.getByRole('heading', { name: /Dashboard/i, level: 1 })
      expect(heading).toBeInTheDocument()
    })

    it('should display workspace description', () => {
      render(<DashboardPage />)

      expect(
        screen.getByText('Your vibrant workspace for professional collaboration and project management')
      ).toBeInTheDocument()
    })

    it('should display live workspace indicator', () => {
      render(<DashboardPage />)

      expect(screen.getByText('Live workspace')).toBeInTheDocument()
    })
  })

  // ============================================
  // Profile Overview Section (8 tests)
  // ============================================
  describe('Profile Overview Section', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        isLoading: false,
      })
    })

    it('should display Profile Overview heading', () => {
      render(<DashboardPage />)

      expect(screen.getByText('Profile Overview')).toBeInTheDocument()
    })

    it('should display Active Account status', () => {
      render(<DashboardPage />)

      expect(screen.getByText('Active Account')).toBeInTheDocument()
    })

    it('should display user email', () => {
      render(<DashboardPage />)

      expect(screen.getByText('Email Address')).toBeInTheDocument()
      expect(screen.getByText('john@example.com')).toBeInTheDocument()
    })

    it('should display username', () => {
      render(<DashboardPage />)

      expect(screen.getByText('Username')).toBeInTheDocument()
      const usernameElements = screen.getAllByText('johndoe')
      expect(usernameElements.length).toBeGreaterThan(0)
    })

    it('should display account status', () => {
      render(<DashboardPage />)

      expect(screen.getByText('Account Status')).toBeInTheDocument()
      expect(screen.getByText('active')).toBeInTheDocument()
    })

    it('should display verified email status', () => {
      render(<DashboardPage />)

      expect(screen.getByText('Email')).toBeInTheDocument()
      expect(screen.getByText('Verified')).toBeInTheDocument()
    })

    it('should display pending email status when not verified', () => {
      const unverifiedUser = { ...mockUser, emailVerified: false }
      mockUseAuth.mockReturnValue({
        user: unverifiedUser,
        isAuthenticated: true,
        isLoading: false,
      })

      render(<DashboardPage />)

      expect(screen.getByText('Pending')).toBeInTheDocument()
    })

    it('should display user roles', () => {
      render(<DashboardPage />)

      expect(screen.getByText('Account Roles')).toBeInTheDocument()
      expect(screen.getByText('user')).toBeInTheDocument()
      expect(screen.getByText('premium')).toBeInTheDocument()
    })
  })

  // ============================================
  // Quick Actions Section (4 tests)
  // ============================================
  describe('Quick Actions Section', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        isLoading: false,
      })
    })

    it('should display Quick Actions heading', () => {
      render(<DashboardPage />)

      expect(screen.getByText('Quick Actions')).toBeInTheDocument()
      expect(screen.getByText('Access key features from your vibrant workspace')).toBeInTheDocument()
    })

    it('should display Create Project card', () => {
      render(<DashboardPage />)

      const createProjectHeading = screen.getAllByText('Create Project').find(el => el.tagName === 'H3')
      expect(createProjectHeading).toBeInTheDocument()
      expect(screen.getByText('Set up exchange projects with clear deliverables and milestones')).toBeInTheDocument()
      expect(screen.getByText('Get Started')).toBeInTheDocument()
    })

    it('should display Browse Projects card', () => {
      render(<DashboardPage />)

      expect(screen.getByText('Browse verified professionals and find exchange partners')).toBeInTheDocument()
      expect(screen.getByText('Explore Now')).toBeInTheDocument()
    })

    it('should display SubscriptionDashboard component', () => {
      render(<DashboardPage />)

      expect(screen.getByTestId('subscription-dashboard')).toBeInTheDocument()
    })
  })

  // ============================================
  // Decorative Elements (2 tests)
  // ============================================
  describe('Decorative Elements', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        isLoading: false,
      })
    })

    it('should render decorative gradient orbs', () => {
      const { container } = render(<DashboardPage />)

      const decorativeElements = container.querySelectorAll('[aria-hidden="true"]')
      expect(decorativeElements.length).toBeGreaterThan(0)
    })

    it('should have gradient background', () => {
      const { container } = render(<DashboardPage />)

      const wrapper = container.querySelector('.bg-gradient-to-br')
      expect(wrapper).toBeInTheDocument()
    })
  })

  // ============================================
  // Accessibility & Semantic HTML (3 tests)
  // ============================================
  describe('Accessibility & Semantic HTML', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        isLoading: false,
      })
    })

    it('should have semantic HTML structure with nav and main', () => {
      const { container } = render(<DashboardPage />)

      const nav = container.querySelector('nav')
      expect(nav).toBeInTheDocument()

      const main = container.querySelector('main')
      expect(main).toBeInTheDocument()
      expect(main).toHaveAttribute('role', 'main')
      expect(main).toHaveAttribute('aria-label', 'Dashboard content')
    })

    it('should have proper heading hierarchy', () => {
      const { container } = render(<DashboardPage />)

      const h1 = container.querySelector('h1')
      expect(h1).toHaveTextContent('Dashboard')

      const h2Elements = container.querySelectorAll('h2')
      expect(h2Elements.length).toBeGreaterThan(0)
    })

    it('should have sticky navigation', () => {
      const { container } = render(<DashboardPage />)

      const nav = container.querySelector('nav.sticky')
      expect(nav).toBeInTheDocument()
      expect(nav?.className).toContain('top-0')
      expect(nav?.className).toContain('z-50')
    })
  })

  // ============================================
  // Responsive Layout (2 tests)
  // ============================================
  describe('Responsive Layout', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        isLoading: false,
      })
    })

    it('should have responsive grid layout for profile details', () => {
      const { container } = render(<DashboardPage />)

      const grid = container.querySelector('.grid.grid-cols-1.md\\:grid-cols-2.lg\\:grid-cols-3')
      expect(grid).toBeInTheDocument()
    })

    it('should have responsive grid layout for quick actions', () => {
      const { container } = render(<DashboardPage />)

      const quickActionsGrid = container.querySelectorAll('.grid.grid-cols-1.md\\:grid-cols-2.lg\\:grid-cols-3')
      expect(quickActionsGrid.length).toBeGreaterThan(0)
    })
  })

  // ============================================
  // Integration (2 tests)
  // ============================================
  describe('Integration', () => {
    it('should render complete authenticated dashboard without errors', () => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        isLoading: false,
      })

      const { container } = render(<DashboardPage />)

      expect(container.firstChild).toBeTruthy()
      expect(screen.getByText('SkillLedger')).toBeInTheDocument()
      expect(screen.getByRole('heading', { name: /Dashboard/i })).toBeInTheDocument()
      expect(screen.getByText('Profile Overview')).toBeInTheDocument()
      expect(screen.getByText('Quick Actions')).toBeInTheDocument()
      expect(screen.getByTestId('theme-toggle')).toBeInTheDocument()
      expect(screen.getByTestId('logout-button')).toBeInTheDocument()
      expect(screen.getByTestId('subscription-dashboard')).toBeInTheDocument()
    })

    it('should not render roles section when user has no roles', () => {
      const userWithoutRoles = { ...mockUser, roles: [] }
      mockUseAuth.mockReturnValue({
        user: userWithoutRoles,
        isAuthenticated: true,
        isLoading: false,
      })

      render(<DashboardPage />)

      expect(screen.queryByText('Account Roles')).not.toBeInTheDocument()
    })
  })
})
