/**
 * Tests for EnhancedNavigation
 *
 * Comprehensive test suite for the enhanced navigation component
 * Coverage target: 70%+ (362 lines)
 */

import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { EnhancedNavigation } from '../EnhancedNavigation'

// Mock dependencies
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}))

jest.mock('next/navigation', () => ({
  usePathname: jest.fn(),
}))

jest.mock('../ThemeToggle', () => ({
  ThemeToggle: () => <div data-testid="theme-toggle">ThemeToggle</div>,
}))

jest.mock('../Logo', () => ({
  Logo: () => <div data-testid="logo">Logo</div>,
}))

import { useAuth } from '@/contexts/AuthContext'
import { usePathname } from 'next/navigation'

const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>
const mockUsePathname = usePathname as jest.MockedFunction<typeof usePathname>

describe('EnhancedNavigation', () => {
  const mockUser = {
    id: 'user-123',
    userName: 'John Doe',
    email: 'john@example.com',
    emailVerified: true,
    taxCompliant: true,
    status: 'Active' as const,
    roles: ['Freelancer'],
    permissions: []
  }

  const mockLogout = jest.fn()

  beforeEach(() => {
    jest.clearAllMocks()

    mockUseAuth.mockReturnValue({
      user: mockUser,
      isAuthenticated: true,
      logout: mockLogout,
      login: jest.fn(),
      isLoading: false,
        isInitialized: true,
        refreshToken: jest.fn(),
      updateUser: jest.fn(),
    })

    mockUsePathname.mockReturnValue('/')

    // Mock window.scrollY
    Object.defineProperty(window, 'scrollY', {
      writable: true,
      configurable: true,
      value: 0,
    })

    // Mock requestAnimationFrame
    global.requestAnimationFrame = jest.fn((cb) => {
      cb(0)
      return 0
    })
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  describe('Authentication State', () => {
    it('should not render when user is not authenticated', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        logout: jest.fn(),
        login: jest.fn(),
        isLoading: false,
        isInitialized: true,
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      const { container } = render(<EnhancedNavigation />)

      expect(container.firstChild).toBeNull()
    })

    it('should render when user is authenticated', () => {
      render(<EnhancedNavigation />)

      expect(screen.getByTestId('logo')).toBeInTheDocument()
      expect(screen.getByTestId('theme-toggle')).toBeInTheDocument()
    })
  })

  describe('Navigation Items', () => {
    it('should render all navigation items', () => {
      render(<EnhancedNavigation />)

      expect(screen.getAllByText('Dashboard').length).toBeGreaterThan(0)
      expect(screen.getAllByText('Projects').length).toBeGreaterThan(0)
      expect(screen.getAllByText('Browse').length).toBeGreaterThan(0)
      expect(screen.getAllByText('Wallet').length).toBeGreaterThan(0)
    })

    it('should highlight active route', () => {
      mockUsePathname.mockReturnValue('/projects')
      const { container } = render(<EnhancedNavigation />)

      // Active route should have bg-primary class
      const projectsLinks = container.querySelectorAll('a[href="/projects"]')
      const activeLink = Array.from(projectsLinks).find((link) =>
        link.className.includes('bg-primary')
      )

      expect(activeLink).toBeTruthy()
    })

    it('should show badge on active dashboard route', () => {
      mockUsePathname.mockReturnValue('/')
      render(<EnhancedNavigation />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('●')
    })
  })

  describe('User Menu', () => {
    it('should display user name and email', () => {
      render(<EnhancedNavigation />)

      expect(screen.getAllByText('John Doe').length).toBeGreaterThan(0)
      expect(screen.getAllByText('john@example.com').length).toBeGreaterThan(0)
    })

    it('should toggle user dropdown when clicking user menu', async () => {
      const user = userEvent.setup()
      render(<EnhancedNavigation />)

      const userMenuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(userMenuButton)

      await waitFor(() => {
        expect(screen.getByRole('menu', { name: /user menu/i })).toBeInTheDocument()
      })

      expect(screen.getAllByText('My Profile').length).toBeGreaterThan(0)
      expect(screen.getAllByText('Settings').length).toBeGreaterThan(0)
      expect(screen.getAllByText('Logout').length).toBeGreaterThan(0)
    })

    it('should close dropdown when clicking user menu button again', async () => {
      const user = userEvent.setup()
      render(<EnhancedNavigation />)

      const userMenuButton = screen.getByRole('button', { name: /user menu/i })

      // Open
      await user.click(userMenuButton)
      await waitFor(() => {
        expect(screen.getByRole('menu', { name: /user menu/i })).toBeInTheDocument()
      })

      // Close
      await user.click(userMenuButton)
      await waitFor(() => {
        expect(screen.queryByRole('menu', { name: /user menu/i })).not.toBeInTheDocument()
      })
    })

    it('should have My Profile link in dropdown', async () => {
      const user = userEvent.setup()
      render(<EnhancedNavigation />)

      const userMenuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(userMenuButton)

      await waitFor(() => {
        const profileLinks = screen.getAllByText('My Profile')
        expect(profileLinks.length).toBeGreaterThan(0)
      })
    })

    it('should have Settings link in dropdown pointing to /subscription', async () => {
      const user = userEvent.setup()
      render(<EnhancedNavigation />)

      const userMenuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(userMenuButton)

      await waitFor(() => {
        const settingsLinks = screen.getAllByText('Settings')
        expect(settingsLinks.length).toBeGreaterThan(0)
        const settingsAnchor = settingsLinks[0].closest('a')
        expect(settingsAnchor).toHaveAttribute('href', '/subscription')
      })
    })
  })

  describe('Logout Functionality', () => {
    it('should call logout when clicking logout button', async () => {
      const user = userEvent.setup()
      render(<EnhancedNavigation />)

      const userMenuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(userMenuButton)

      await waitFor(() => {
        expect(screen.getByRole('menu', { name: /user menu/i })).toBeInTheDocument()
      })

      const logoutButtons = screen.getAllByText('Logout')
      await user.click(logoutButtons[0])

      expect(mockLogout).toHaveBeenCalled()
    })
  })

  describe('Mobile Menu', () => {
    it('should show mobile menu toggle button', () => {
      render(<EnhancedNavigation />)

      expect(screen.getByRole('button', { name: /toggle mobile menu/i })).toBeInTheDocument()
    })

    it('should open mobile menu when clicking toggle button', async () => {
      const user = userEvent.setup()
      render(<EnhancedNavigation />)

      const toggleButton = screen.getByRole('button', { name: /toggle mobile menu/i })
      await user.click(toggleButton)

      await waitFor(() => {
        // Mobile menu should be visible with user info
        const userNameElements = screen.getAllByText('John Doe')
        expect(userNameElements.length).toBeGreaterThan(1) // Desktop + Mobile
      })
    })

    it('should close mobile menu when clicking backdrop', async () => {
      const user = userEvent.setup()
      const { container } = render(<EnhancedNavigation />)

      const toggleButton = screen.getByRole('button', { name: /toggle mobile menu/i })
      await user.click(toggleButton)

      await waitFor(() => {
        const backdrop = container.querySelector('.bg-overlay\\/70')
        expect(backdrop).toBeTruthy()
      })

      const backdrop = container.querySelector('.bg-overlay\\/70') as HTMLElement
      await user.click(backdrop)

      await waitFor(() => {
        const backdropAfter = container.querySelector('.bg-overlay\\/70')
        expect(backdropAfter).not.toBeInTheDocument()
      })
    })

    it('should toggle mobile menu state', async () => {
      const user = userEvent.setup()
      render(<EnhancedNavigation />)

      const toggleButton = screen.getByRole('button', { name: /toggle mobile menu/i })

      // Open
      await user.click(toggleButton)
      await waitFor(() => {
        expect(toggleButton).toHaveAttribute('aria-expanded', 'true')
      })

      // Close
      await user.click(toggleButton)
      await waitFor(() => {
        expect(toggleButton).toHaveAttribute('aria-expanded', 'false')
      })
    })
  })

  describe('Scroll Effects', () => {
    it('should update isScrolled state when scrolling', async () => {
      const { container } = render(<EnhancedNavigation />)

      // Simulate scroll
      Object.defineProperty(window, 'scrollY', { value: 100, writable: true })
      fireEvent.scroll(window)

      await waitFor(() => {
        const header = container.querySelector('header')
        expect(header?.className).toContain('backdrop-blur-xl')
      })
    })

    it('should apply different styles when not scrolled', () => {
      const { container } = render(<EnhancedNavigation />)

      const header = container.querySelector('header')
      expect(header?.className).toContain('backdrop-blur-md')
    })
  })

  describe('Keyboard Shortcuts', () => {
    it('should close mobile menu on Escape key', async () => {
      const user = userEvent.setup()
      const { container } = render(<EnhancedNavigation />)

      const toggleButton = screen.getByRole('button', { name: /toggle mobile menu/i })
      await user.click(toggleButton)

      await waitFor(() => {
        const backdrop = container.querySelector('.bg-overlay\\/70')
        expect(backdrop).toBeTruthy()
      })

      fireEvent.keyDown(document, { key: 'Escape' })

      await waitFor(() => {
        expect(container.querySelector('.bg-overlay\\/70')).not.toBeInTheDocument()
      })
    })

    it('should close user dropdown on Escape key', async () => {
      const user = userEvent.setup()
      render(<EnhancedNavigation />)

      const userMenuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(userMenuButton)

      await waitFor(() => {
        expect(screen.getByRole('menu', { name: /user menu/i })).toBeInTheDocument()
      })

      fireEvent.keyDown(document, { key: 'Escape' })

      await waitFor(() => {
        expect(screen.queryByRole('menu', { name: /user menu/i })).not.toBeInTheDocument()
      })
    })
  })

  describe('Click Outside', () => {
    it('should close dropdown when clicking outside', async () => {
      const user = userEvent.setup()
      render(<EnhancedNavigation />)

      const userMenuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(userMenuButton)

      await waitFor(() => {
        expect(screen.getByRole('menu', { name: /user menu/i })).toBeInTheDocument()
      })

      // Click outside
      fireEvent.mouseDown(document.body)

      await waitFor(() => {
        expect(screen.queryByRole('menu', { name: /user menu/i })).not.toBeInTheDocument()
      })
    })
  })

  describe('Accessibility', () => {
    it('should have proper ARIA labels', () => {
      render(<EnhancedNavigation />)

      expect(screen.getByRole('button', { name: /user menu/i })).toHaveAttribute('aria-haspopup', 'menu')
      expect(screen.getByRole('button', { name: /toggle mobile menu/i })).toHaveAttribute('aria-expanded')
    })

    it('should have proper roles for menu items', async () => {
      const user = userEvent.setup()
      render(<EnhancedNavigation />)

      const userMenuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(userMenuButton)

      await waitFor(() => {
        const menuItems = screen.getAllByRole('menuitem')
        expect(menuItems.length).toBeGreaterThan(0)
      })
    })

    it('should show keyboard shortcut help', async () => {
      const user = userEvent.setup()
      render(<EnhancedNavigation />)

      const userMenuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(userMenuButton)

      await waitFor(() => {
        expect(screen.getByText(/Press/)).toBeInTheDocument()
        expect(screen.getByText('Esc')).toBeInTheDocument()
      })
    })
  })

  describe('Logo and Branding', () => {
    it('should render logo component', () => {
      render(<EnhancedNavigation />)

      expect(screen.getByTestId('logo')).toBeInTheDocument()
    })

    it('should have logo link to home page', () => {
      const { container } = render(<EnhancedNavigation />)

      const logoLink = container.querySelector('a[href="/"]')
      expect(logoLink).toBeTruthy()
    })
  })

  describe('Theme Toggle Integration', () => {
    it('should render theme toggle component', () => {
      render(<EnhancedNavigation />)

      expect(screen.getByTestId('theme-toggle')).toBeInTheDocument()
    })
  })

  describe('Edge Cases', () => {
    it('should handle missing user data gracefully', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: '123',
          userName: '',
          email: '',
          emailVerified: false,
          taxCompliant: false,
          status: 'Active',
          roles: [],
          permissions: []
        },
        isAuthenticated: true,
        logout: jest.fn(),
        login: jest.fn(),
        isLoading: false,
        isInitialized: true,
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      render(<EnhancedNavigation />)

      expect(screen.getByTestId('logo')).toBeInTheDocument()
    })

    it('should handle navigation to different routes', () => {
      mockUsePathname.mockReturnValue('/wallet')
      render(<EnhancedNavigation />)

      expect(screen.getAllByText('Wallet').length).toBeGreaterThan(0)
    })
  })

  describe('Integration', () => {
    it('should render complete navigation without errors', () => {
      const { container } = render(<EnhancedNavigation />)

      expect(container.firstChild).toBeTruthy()
      expect(screen.getByTestId('logo')).toBeInTheDocument()
      expect(screen.getAllByText('Dashboard').length).toBeGreaterThan(0)
      expect(screen.getByRole('button', { name: /user menu/i })).toBeInTheDocument()
    })

    it('should handle all navigation interactions', async () => {
      const user = userEvent.setup()
      render(<EnhancedNavigation />)

      // Open user menu
      const userMenuButton = screen.getByRole('button', { name: /user menu/i })
      await user.click(userMenuButton)
      await waitFor(() => {
        expect(screen.getByRole('menu', { name: /user menu/i })).toBeInTheDocument()
      })

      // Close with Escape
      fireEvent.keyDown(document, { key: 'Escape' })
      await waitFor(() => {
        expect(screen.queryByRole('menu', { name: /user menu/i })).not.toBeInTheDocument()
      })

      // Open mobile menu
      const mobileToggle = screen.getByRole('button', { name: /toggle mobile menu/i })
      await user.click(mobileToggle)

      // Close with Escape
      fireEvent.keyDown(document, { key: 'Escape' })
      await waitFor(() => {
        expect(mobileToggle).toHaveAttribute('aria-expanded', 'false')
      })
    })
  })
})
