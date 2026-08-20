/**
 * Integration tests for Homepage
 *
 * Architecture:
 * - Home (server component) — renders static landing page + AuthenticatedHomeWrapper
 * - AuthenticatedHome (client, ssr:false) — overlays dashboard when authenticated
 *
 * We test them separately because server components don't use useAuth,
 * and the dynamic import with ssr:false means AuthenticatedHome never
 * renders inside the server component test.
 */

import React from 'react'
import { render, screen } from '@testing-library/react'
import Home from '../(home)/page'
import AuthenticatedHome from '../(home)/AuthenticatedHome'
import { useAuth } from '@/contexts/AuthContext'

// Mock useAuth
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}))

// Mock Next.js components
jest.mock('next/link', () => {
  const MockLink = ({ children, href }: any) => <a href={href}>{children}</a>
  MockLink.displayName = 'MockLink'
  return MockLink
})

jest.mock('next/dynamic', () => {
  return (importFunc: any) => {
    const Component = () => <div data-testid="authenticated-home-wrapper">AuthenticatedHomeWrapper</div>
    return Component
  }
})

// Mock child components
jest.mock('@/components/Logo', () => ({
  Logo: ({ size, showText }: any) => (
    <div data-testid="logo" data-size={size} data-show-text={showText}>
      Logo
    </div>
  ),
}))

jest.mock('@/components/ThemeToggle', () => ({
  ThemeToggle: () => <div data-testid="theme-toggle">ThemeToggle</div>,
}))

jest.mock('@/components/LogoutButton', () => ({
  __esModule: true,
  default: ({ showAllDevicesOption }: any) => (
    <button data-testid="logout-button" data-show-all-devices={showAllDevicesOption}>
      Logout
    </button>
  ),
}))

jest.mock('@/components/ProtectedRoute', () => ({
  __esModule: true,
  default: ({ children }: any) => <div data-testid="protected-route">{children}</div>,
}))

// Mock content loader to avoid fs reads in test
jest.mock('@/lib/content', () => ({
  getAllArticles: () => [],
}))

// Mock cross-links (they lazy-import content)
jest.mock('@/lib/cross-links', () => ({
  skillToCategorySlug: () => null,
  findScenariosForCategory: () => [],
  findArticlesForCategory: () => [],
  findIndustriesForCategory: () => [],
  findCategoriesForArticle: () => [],
  findTradePairsForArticle: () => [],
}))

const mockUseAuth = useAuth as jest.Mock

describe('Home Page', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    // Default: unauthenticated
    mockUseAuth.mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
    })
  })

  // ============================================
  // Server Component: Static Landing Page
  // ============================================
  describe('Unauthenticated State - Hero Section', () => {
    it('should display SkillLedger logo in hero', () => {
      render(<Home />)

      const logos = screen.getAllByTestId('logo')
      expect(logos.length).toBeGreaterThan(0)
      const heroLogo = logos.find(logo => logo.getAttribute('data-size') === 'hero')
      expect(heroLogo).toBeTruthy()
      expect(heroLogo?.getAttribute('data-show-text')).toBe('false')
    })

    it('should display main heading "SkillLedger"', () => {
      render(<Home />)

      const heading = screen.getByText('SkillLedger')
      expect(heading).toBeInTheDocument()
      expect(heading.tagName).toBe('H1')
    })

    it('should display tagline', () => {
      render(<Home />)

      expect(screen.getByText('Trade Your Skills. Skip the Invoice.')).toBeInTheDocument()
      expect(screen.getByText(/Join 19 skill categories across 50\+ US cities/)).toBeInTheDocument()
    })

    it('should display "Start Exchanging Free" CTA button', () => {
      render(<Home />)

      const ctaButton = screen.getByText('Start Exchanging Free')
      expect(ctaButton).toBeInTheDocument()
      expect(ctaButton.closest('a')).toHaveAttribute('href', '/register')
    })

    it('should not display "Sign In to Account" in hero (moved to navbar)', () => {
      render(<Home />)

      expect(screen.queryByText('Sign In to Account')).not.toBeInTheDocument()
    })

    it('should display theme toggle in hero', () => {
      render(<Home />)

      expect(screen.getByTestId('theme-toggle')).toBeInTheDocument()
    })

    it('should display decorative background elements', () => {
      const { container } = render(<Home />)

      const decorativeElements = container.querySelectorAll('[aria-hidden="true"]')
      expect(decorativeElements.length).toBeGreaterThan(0)
      const floatingElements = container.querySelectorAll('.animate-float')
      expect(floatingElements.length).toBeGreaterThan(0)
    })

    it('should display "Why Choose SkillLedger?" section heading', () => {
      render(<Home />)

      expect(screen.getByText('Why Choose SkillLedger?')).toBeInTheDocument()
    })

    it('should display all 3 feature cards', () => {
      render(<Home />)

      expect(screen.getByText('Project Management')).toBeInTheDocument()
      expect(screen.getByText('Talent Discovery')).toBeInTheDocument()
      expect(screen.getByText('Credit Exchange')).toBeInTheDocument()
    })

    it('should display feature descriptions', () => {
      render(<Home />)

      expect(screen.getByText(/Set up exchange projects with clear deliverables/)).toBeInTheDocument()
      expect(screen.getByText(/Search by skill, location, or credit rate/)).toBeInTheDocument()
      expect(screen.getByText(/Track your credit balance, earnings history/)).toBeInTheDocument()
    })
  })

  describe('Landing Page Structure', () => {
    it('should include AuthenticatedHomeWrapper', () => {
      render(<Home />)
      expect(screen.getByTestId('authenticated-home-wrapper')).toBeInTheDocument()
    })

    it('should have responsive container classes', () => {
      const { container } = render(<Home />)
      const responsiveGrids = container.querySelectorAll('.grid.grid-cols-1')
      expect(responsiveGrids.length).toBeGreaterThan(0)
    })
  })

  describe('Accessibility', () => {
    it('should have proper ARIA labels for decorative elements', () => {
      const { container } = render(<Home />)
      const decorativeElements = container.querySelectorAll('[aria-hidden="true"]')
      expect(decorativeElements.length).toBeGreaterThan(0)
    })

    it('should have proper semantic HTML structure', () => {
      const { container } = render(<Home />)
      const sections = container.querySelectorAll('section')
      expect(sections.length).toBeGreaterThan(0)
    })
  })

  describe('Integration', () => {
    it('should render complete unauthenticated homepage without errors', () => {
      const { container } = render(<Home />)

      expect(container.firstChild).toBeTruthy()
      expect(screen.getByText('SkillLedger')).toBeInTheDocument()
      expect(screen.getByText('Start Exchanging Free')).toBeInTheDocument()
      expect(screen.getByText('Why Choose SkillLedger?')).toBeInTheDocument()
    })
  })
})

// ============================================
// Client Component: AuthenticatedHome (dashboard overlay)
// ============================================
describe('AuthenticatedHome (Dashboard)', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  const mockUser = {
    id: 'user-123',
    email: 'john.doe@example.com',
    userName: 'johndoe',
    firstName: 'John',
    lastName: 'Doe',
    status: 'active',
    emailVerified: true,
    roles: ['User', 'Premium'],
  }

  describe('Loading / Unauthenticated', () => {
    it('should return null when loading', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: true,
      })

      const { container } = render(<AuthenticatedHome />)
      expect(container.innerHTML).toBe('')
    })

    it('should return null when not authenticated', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
      })

      const { container } = render(<AuthenticatedHome />)
      expect(container.innerHTML).toBe('')
    })
  })

  describe('Authenticated Dashboard', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        isLoading: false,
      })
    })

    it('should display navigation bar with logo', () => {
      render(<AuthenticatedHome />)

      const logos = screen.getAllByTestId('logo')
      const navLogo = logos.find(logo => logo.getAttribute('data-size') === 'medium')
      expect(navLogo).toBeTruthy()
      expect(navLogo?.getAttribute('data-show-text')).toBe('true')
    })

    it('should display Dashboard link in navigation', () => {
      render(<AuthenticatedHome />)

      const dashboardLinks = screen.getAllByText('Dashboard')
      expect(dashboardLinks.length).toBeGreaterThan(0)
    })

    it('should display welcome message with user firstName - E2E-019 FIX', () => {
      render(<AuthenticatedHome />)

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

      render(<AuthenticatedHome />)

      const usernameElements = screen.getAllByText('johndoe')
      expect(usernameElements.length).toBeGreaterThan(0)
    })

    it('should display logout button without all devices dropdown - E2E-003 FIX', () => {
      render(<AuthenticatedHome />)

      const logoutButton = screen.getByTestId('logout-button')
      expect(logoutButton).toBeInTheDocument()
      expect(logoutButton.getAttribute('data-show-all-devices')).toBe('false')
    })

    it('should display Dashboard heading', () => {
      const { container } = render(<AuthenticatedHome />)

      const h2 = container.querySelector('h2')
      expect(h2?.textContent).toContain('Dashboard')
    })

    it('should display "Live workspace" status indicator', () => {
      render(<AuthenticatedHome />)

      expect(screen.getByText('Live workspace')).toBeInTheDocument()
    })

    it('should display Profile Overview section', () => {
      render(<AuthenticatedHome />)

      expect(screen.getByText('Profile Overview')).toBeInTheDocument()
      expect(screen.getByText('Active Account')).toBeInTheDocument()
    })

    it('should display user email in profile', () => {
      render(<AuthenticatedHome />)

      expect(screen.getByText('Email Address')).toBeInTheDocument()
      expect(screen.getByText('john.doe@example.com')).toBeInTheDocument()
    })

    it('should display user username in profile', () => {
      render(<AuthenticatedHome />)

      expect(screen.getByText('Username')).toBeInTheDocument()
      expect(screen.getByText('johndoe')).toBeInTheDocument()
    })

    it('should display account status in profile', () => {
      render(<AuthenticatedHome />)

      expect(screen.getByText('Account Status')).toBeInTheDocument()
      expect(screen.getByText('active')).toBeInTheDocument()
    })
  })

  describe('Email Verification Status', () => {
    it('should show verified status when email is verified', () => {
      mockUseAuth.mockReturnValue({
        user: { id: 'user-123', email: 'test@example.com', emailVerified: true },
        isAuthenticated: true,
        isLoading: false,
      })

      render(<AuthenticatedHome />)

      expect(screen.getByText('Email')).toBeInTheDocument()
      expect(screen.getByText('Verified')).toBeInTheDocument()
    })

    it('should show pending status when email is not verified', () => {
      mockUseAuth.mockReturnValue({
        user: { id: 'user-123', email: 'test@example.com', emailVerified: false },
        isAuthenticated: true,
        isLoading: false,
      })

      render(<AuthenticatedHome />)

      expect(screen.getByText('Email')).toBeInTheDocument()
      expect(screen.getByText('Pending')).toBeInTheDocument()
    })

    it('should display appropriate icons for verification status', () => {
      mockUseAuth.mockReturnValue({
        user: { id: 'user-123', email: 'test@example.com', emailVerified: true },
        isAuthenticated: true,
        isLoading: false,
      })

      const { container } = render(<AuthenticatedHome />)

      const successIcon = container.querySelector('.text-success')
      expect(successIcon).toBeInTheDocument()
    })
  })

  describe('User Roles Display', () => {
    it('should display user roles when available', () => {
      mockUseAuth.mockReturnValue({
        user: { id: 'user-123', email: 'test@example.com', roles: ['User', 'Premium', 'Admin'] },
        isAuthenticated: true,
        isLoading: false,
      })

      render(<AuthenticatedHome />)

      expect(screen.getByText('Account Roles')).toBeInTheDocument()
      expect(screen.getByText('User')).toBeInTheDocument()
      expect(screen.getByText('Premium')).toBeInTheDocument()
      expect(screen.getByText('Admin')).toBeInTheDocument()
    })

    it('should not display roles section when user has no roles', () => {
      mockUseAuth.mockReturnValue({
        user: { id: 'user-123', email: 'test@example.com', roles: [] },
        isAuthenticated: true,
        isLoading: false,
      })

      render(<AuthenticatedHome />)

      expect(screen.queryByText('Account Roles')).not.toBeInTheDocument()
    })
  })

  describe('Quick Actions Section', () => {
    beforeEach(() => {
      mockUseAuth.mockReturnValue({
        user: { id: 'user-123', email: 'test@example.com' },
        isAuthenticated: true,
        isLoading: false,
      })
    })

    it('should display Quick Actions heading', () => {
      render(<AuthenticatedHome />)

      expect(screen.getByText('Quick Actions')).toBeInTheDocument()
      expect(screen.getByText('Access key features from your vibrant workspace')).toBeInTheDocument()
    })

    it('should display Create Project action card', () => {
      render(<AuthenticatedHome />)

      expect(screen.getByText('Create Project')).toBeInTheDocument()
      expect(screen.getByText(/Set up exchange projects with clear deliverables/)).toBeInTheDocument()
    })

    it('should display Browse Projects action card', () => {
      render(<AuthenticatedHome />)

      expect(screen.getByText('Browse Projects')).toBeInTheDocument()
    })

    it('should display Premium Wallet action card', () => {
      render(<AuthenticatedHome />)

      expect(screen.getByText('Premium Wallet')).toBeInTheDocument()
    })
  })

  describe('Responsive Layout', () => {
    it('should hide welcome message on mobile (sm:block)', () => {
      mockUseAuth.mockReturnValue({
        user: { id: 'user-123', email: 'test@example.com', firstName: 'John' },
        isAuthenticated: true,
        isLoading: false,
      })

      const { container } = render(<AuthenticatedHome />)

      const welcomeContainer = container.querySelector('.sm\\:block')
      expect(welcomeContainer).toBeInTheDocument()
    })
  })

  describe('Edge Cases', () => {
    it('should handle user with null/undefined roles', () => {
      mockUseAuth.mockReturnValue({
        user: { id: 'user-123', email: 'test@example.com', userName: 'testuser', roles: undefined },
        isAuthenticated: true,
        isLoading: false,
      })

      render(<AuthenticatedHome />)

      expect(screen.queryByText('Account Roles')).not.toBeInTheDocument()
    })

    it('should display "Available Soon" for all quick actions', () => {
      mockUseAuth.mockReturnValue({
        user: { id: 'user-123', email: 'test@example.com' },
        isAuthenticated: true,
        isLoading: false,
      })

      render(<AuthenticatedHome />)

      const availableSoonButtons = screen.getAllByText('Available Soon')
      expect(availableSoonButtons).toHaveLength(3)
    })

    it('should handle user without lastName', () => {
      mockUseAuth.mockReturnValue({
        user: { id: 'user-123', email: 'test@example.com', userName: 'johndoe', firstName: 'John', lastName: undefined },
        isAuthenticated: true,
        isLoading: false,
      })

      render(<AuthenticatedHome />)

      expect(screen.getByText('John')).toBeInTheDocument()
    })
  })

  describe('Integration', () => {
    it('should render complete authenticated dashboard without errors', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: 'user-123', email: 'test@example.com', userName: 'testuser',
          firstName: 'Test', status: 'active', emailVerified: true, roles: ['User'],
        },
        isAuthenticated: true,
        isLoading: false,
      })

      const { container } = render(<AuthenticatedHome />)

      expect(container.firstChild).toBeTruthy()
      expect(screen.getAllByText('Dashboard').length).toBeGreaterThan(0)
      expect(screen.getByText('Profile Overview')).toBeInTheDocument()
      expect(screen.getByText('Quick Actions')).toBeInTheDocument()
    })
  })
})
