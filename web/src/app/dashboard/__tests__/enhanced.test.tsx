import React from 'react'
import { render, screen } from '@testing-library/react'
import EnhancedDashboardPage from '../enhanced'
import { useAuth } from '@/contexts/AuthContext'

// Mock the AuthContext
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}))

// Mock the child components
jest.mock('@/components/ThemeToggle', () => ({
  ThemeToggle: () => <div data-testid="theme-toggle">ThemeToggle</div>,
}))

jest.mock('@/components/EnhancedNavigation', () => ({
  EnhancedNavigation: () => <div data-testid="enhanced-navigation">EnhancedNavigation</div>,
}))

jest.mock('@/components/EnhancedDashboardContent', () => ({
  EnhancedDashboardContent: () => <div data-testid="enhanced-dashboard-content">EnhancedDashboardContent</div>,
}))

const mockUseAuth = useAuth as jest.Mock

describe('EnhancedDashboardPage', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  // ============================================
  // Loading State (2 tests)
  // ============================================
  describe('Loading State', () => {
    it('should display loading spinner when isLoading is true', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: true,
      })

      render(<EnhancedDashboardPage />)

      expect(screen.getByText('Loading your workspace...')).toBeInTheDocument()
    })

    it('should show loading spinner with correct styling', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: true,
      })

      const { container } = render(<EnhancedDashboardPage />)

      const spinner = container.querySelector('.loading-spinner')
      expect(spinner).toBeInTheDocument()
      expect(spinner?.className).toContain('animate-pulse-glow')
    })
  })

  // ============================================
  // Unauthenticated State (2 tests)
  // ============================================
  describe('Unauthenticated State', () => {
    it('should return null when not authenticated (middleware will redirect)', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
      })

      const { container } = render(<EnhancedDashboardPage />)

      expect(container.firstChild).toBeNull()
    })

    it('should not render navigation or content when unauthenticated', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
      })

      render(<EnhancedDashboardPage />)

      expect(screen.queryByTestId('enhanced-navigation')).not.toBeInTheDocument()
      expect(screen.queryByTestId('enhanced-dashboard-content')).not.toBeInTheDocument()
    })
  })

  // ============================================
  // Authenticated State (4 tests)
  // ============================================
  describe('Authenticated State', () => {
    const mockUser = {
      id: 'user-123',
      email: 'user@example.com',
      firstName: 'John',
      lastName: 'Doe',
    }

    it('should render EnhancedNavigation when authenticated', () => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        isLoading: false,
      })

      render(<EnhancedDashboardPage />)

      expect(screen.getByTestId('enhanced-navigation')).toBeInTheDocument()
    })

    it('should render EnhancedDashboardContent when authenticated', () => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        isLoading: false,
      })

      render(<EnhancedDashboardPage />)

      expect(screen.getByTestId('enhanced-dashboard-content')).toBeInTheDocument()
    })

    it('should render main content area with correct ARIA label', () => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        isLoading: false,
      })

      render(<EnhancedDashboardPage />)

      const main = screen.getByRole('main')
      expect(main).toHaveAttribute('aria-label', 'Dashboard content')
    })

    it('should render background decorative elements', () => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        isLoading: false,
      })

      const { container } = render(<EnhancedDashboardPage />)

      // Check for decorative background elements
      const decorativeElements = container.querySelectorAll('[aria-hidden="true"]')
      expect(decorativeElements.length).toBeGreaterThan(0)

      // Verify animations are applied
      const floatingElement = container.querySelector('.animate-float-3d')
      expect(floatingElement).toBeInTheDocument()

      const pendulumElement = container.querySelector('.animate-pendulum')
      expect(pendulumElement).toBeInTheDocument()
    })
  })

  // ============================================
  // Styling and Layout (2 tests)
  // ============================================
  describe('Styling and Layout', () => {
    const mockUser = {
      id: 'user-123',
      email: 'user@example.com',
      firstName: 'John',
      lastName: 'Doe',
    }

    it('should have gradient background when authenticated', () => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        isLoading: false,
      })

      const { container } = render(<EnhancedDashboardPage />)

      const wrapper = container.querySelector('.bg-gradient-to-br')
      expect(wrapper).toBeInTheDocument()
      expect(wrapper?.className).toContain('from-background')
    })

    it('should have proper container styling', () => {
      mockUseAuth.mockReturnValue({
        user: mockUser,
        isAuthenticated: true,
        isLoading: false,
      })

      const { container } = render(<EnhancedDashboardPage />)

      const mainContent = container.querySelector('.container-premium')
      expect(mainContent).toBeInTheDocument()
    })
  })

  // ============================================
  // Integration (1 test)
  // ============================================
  describe('Integration', () => {
    it('should render complete authenticated dashboard without errors', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: 'user-123',
          email: 'test@example.com',
          firstName: 'Test',
          lastName: 'User',
        },
        isAuthenticated: true,
        isLoading: false,
      })

      const { container } = render(<EnhancedDashboardPage />)

      expect(container.firstChild).toBeTruthy()
      expect(screen.getByTestId('enhanced-navigation')).toBeInTheDocument()
      expect(screen.getByTestId('enhanced-dashboard-content')).toBeInTheDocument()
      expect(screen.getByRole('main')).toBeInTheDocument()
    })
  })
})
