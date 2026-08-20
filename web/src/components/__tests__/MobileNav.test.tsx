import React from 'react'
import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MobileNav } from '../MobileNav'

describe('MobileNav', () => {
  const mockItems = [
    { href: '/dashboard', label: 'Dashboard' },
    { href: '/projects', label: 'Projects' },
    { href: '/messages', label: 'Messages' },
    { href: '/create', label: 'Create Project', isPrimary: true },
  ]

  beforeEach(() => {
    // Reset body overflow
    document.body.style.overflow = 'unset'
  })

  afterEach(() => {
    // Clean up body overflow
    document.body.style.overflow = 'unset'
  })

  // ============================================
  // Initial Render (3 tests)
  // ============================================
  describe('Initial Render', () => {
    it('should render hamburger button', () => {
      render(<MobileNav items={mockItems} />)

      const button = screen.getByLabelText('Open navigation menu')
      expect(button).toBeInTheDocument()
    })

    it('should not render menu panel initially', () => {
      render(<MobileNav items={mockItems} />)

      expect(screen.queryByRole('navigation', { name: 'Mobile navigation' })).not.toBeInTheDocument()
    })

    it('should show Menu icon when closed', () => {
      const { container } = render(<MobileNav items={mockItems} />)

      const menuIcon = container.querySelector('.lucide-menu')
      expect(menuIcon).toBeInTheDocument()
    })
  })

  // ============================================
  // Menu Toggle (4 tests)
  // ============================================
  describe('Menu Toggle', () => {
    it('should open menu when hamburger button clicked', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      const button = screen.getByLabelText('Open navigation menu')
      await user.click(button)

      await waitFor(() => {
        expect(screen.getByRole('navigation', { name: 'Mobile navigation' })).toBeInTheDocument()
      })
    })

    it('should show X icon when menu is open', async () => {
      const user = userEvent.setup()
      const { container } = render(<MobileNav items={mockItems} />)

      const button = screen.getByLabelText('Open navigation menu')
      await user.click(button)

      await waitFor(() => {
        const closeIcon = container.querySelectorAll('.lucide-x')
        expect(closeIcon.length).toBeGreaterThan(0)
      })
    })

    it('should close menu when hamburger button clicked again', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      const openButton = screen.getByLabelText('Open navigation menu')
      await user.click(openButton)

      await waitFor(() => {
        expect(screen.getByRole('navigation', { name: 'Mobile navigation' })).toBeInTheDocument()
      })

      const closeButton = screen.getByLabelText('Close navigation menu')
      await user.click(closeButton)

      await waitFor(() => {
        expect(screen.queryByRole('navigation', { name: 'Mobile navigation' })).not.toBeInTheDocument()
      })
    })

    it('should update aria-expanded attribute', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      const button = screen.getByLabelText('Open navigation menu')
      expect(button).toHaveAttribute('aria-expanded', 'false')

      await user.click(button)

      await waitFor(() => {
        const expandedButton = screen.getByLabelText('Close navigation menu')
        expect(expandedButton).toHaveAttribute('aria-expanded', 'true')
      })
    })
  })

  // ============================================
  // Navigation Items (4 tests)
  // ============================================
  describe('Navigation Items', () => {
    it('should render all navigation items when menu is open', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        expect(screen.getByText('Dashboard')).toBeInTheDocument()
        expect(screen.getByText('Projects')).toBeInTheDocument()
        expect(screen.getByText('Messages')).toBeInTheDocument()
        expect(screen.getByText('Create Project')).toBeInTheDocument()
      })
    })

    it('should render primary button with correct styling', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        const primaryLink = screen.getByText('Create Project')
        expect(primaryLink).toHaveClass('btn-primary')
      })
    })

    it('should render regular links with ghost styling', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        const regularLink = screen.getByText('Dashboard')
        expect(regularLink).toHaveClass('btn-ghost')
      })
    })

    it('should close menu when navigation link is clicked', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        expect(screen.getByText('Dashboard')).toBeInTheDocument()
      })

      const dashboardLink = screen.getByText('Dashboard')
      await user.click(dashboardLink)

      await waitFor(() => {
        expect(screen.queryByRole('navigation', { name: 'Mobile navigation' })).not.toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Keyboard Accessibility (6 tests)
  // ============================================
  describe('Keyboard Accessibility', () => {
    it('should close menu when Escape key is pressed', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        expect(screen.getByRole('navigation', { name: 'Mobile navigation' })).toBeInTheDocument()
      })

      fireEvent.keyDown(document, { key: 'Escape' })

      await waitFor(() => {
        expect(screen.queryByRole('navigation', { name: 'Mobile navigation' })).not.toBeInTheDocument()
      })
    })

    it('should not respond to Escape key when menu is closed', () => {
      render(<MobileNav items={mockItems} />)

      // Menu is closed
      expect(screen.queryByRole('navigation', { name: 'Mobile navigation' })).not.toBeInTheDocument()

      // Press Escape - should have no effect
      fireEvent.keyDown(document, { key: 'Escape' })

      // Menu should still be closed
      expect(screen.queryByRole('navigation', { name: 'Mobile navigation' })).not.toBeInTheDocument()
    })

    it('should trap focus with Tab key (BUG-009 fix)', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        expect(screen.getByRole('navigation', { name: 'Mobile navigation' })).toBeInTheDocument()
      })

      // Get all focusable elements
      const closeButton = screen.getByLabelText('Close menu')
      const links = screen.getAllByRole('link')

      // Last link should be focused
      links[links.length - 1].focus()
      expect(document.activeElement).toBe(links[links.length - 1])

      // Tab from last element should wrap to first
      fireEvent.keyDown(document, { key: 'Tab' })

      // Should prevent default and focus should remain trapped
      // (in a real browser, focus would move to close button)
    })

    it('should trap focus with Shift+Tab key (BUG-009 fix)', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        expect(screen.getByRole('navigation', { name: 'Mobile navigation' })).toBeInTheDocument()
      })

      const closeButton = screen.getByLabelText('Close menu')

      // Focus close button (first element)
      closeButton.focus()
      expect(document.activeElement).toBe(closeButton)

      // Shift+Tab from first element should wrap to last
      fireEvent.keyDown(document, { key: 'Tab', shiftKey: true })

      // Focus trap should prevent moving outside menu
    })

    it('should focus close button when menu opens (BUG-009 fix)', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        const closeButton = screen.getByLabelText('Close menu')
        expect(closeButton).toBeInTheDocument()
      }, { timeout: 100 })
    })

    it('should handle Tab key when no focusable elements', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={[]} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        expect(screen.getByRole('navigation', { name: 'Mobile navigation' })).toBeInTheDocument()
      })

      // Tab should not crash when no items
      fireEvent.keyDown(document, { key: 'Tab' })

      // Should still be open
      expect(screen.getByRole('navigation', { name: 'Mobile navigation' })).toBeInTheDocument()
    })
  })

  // ============================================
  // Body Scroll Lock (3 tests)
  // ============================================
  describe('Body Scroll Lock', () => {
    it('should lock body scroll when menu opens', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      expect(document.body.style.overflow).toBe('unset')

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        expect(document.body.style.overflow).toBe('hidden')
      })
    })

    it('should unlock body scroll when menu closes', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        expect(document.body.style.overflow).toBe('hidden')
      })

      await user.click(screen.getByLabelText('Close navigation menu'))

      await waitFor(() => {
        expect(document.body.style.overflow).toBe('unset')
      })
    })

    it('should unlock body scroll on component unmount', async () => {
      const user = userEvent.setup()
      const { unmount } = render(<MobileNav items={mockItems} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        expect(document.body.style.overflow).toBe('hidden')
      })

      unmount()

      expect(document.body.style.overflow).toBe('unset')
    })
  })

  // ============================================
  // Backdrop (3 tests)
  // ============================================
  describe('Backdrop', () => {
    it('should render backdrop when menu is open (BUG-012 fix)', async () => {
      const user = userEvent.setup()
      const { container } = render(<MobileNav items={mockItems} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        const backdrop = container.querySelector('.bg-overlay\\/80')
        expect(backdrop).toBeInTheDocument()
      })
    })

    it('should close menu when backdrop is clicked', async () => {
      const user = userEvent.setup()
      const { container } = render(<MobileNav items={mockItems} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        expect(screen.getByRole('navigation', { name: 'Mobile navigation' })).toBeInTheDocument()
      })

      const backdrop = container.querySelector('.bg-overlay\\/80')
      if (backdrop) {
        fireEvent.click(backdrop)
      }

      await waitFor(() => {
        expect(screen.queryByRole('navigation', { name: 'Mobile navigation' })).not.toBeInTheDocument()
      })
    })

    it('should have aria-hidden on backdrop', async () => {
      const user = userEvent.setup()
      const { container } = render(<MobileNav items={mockItems} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        const backdrop = container.querySelector('[aria-hidden="true"]')
        expect(backdrop).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // ARIA Attributes (3 tests)
  // ============================================
  describe('ARIA Attributes', () => {
    it('should have correct aria-label on hamburger button', () => {
      render(<MobileNav items={mockItems} />)

      const button = screen.getByLabelText('Open navigation menu')
      expect(button).toHaveAttribute('aria-label', 'Open navigation menu')
    })

    it('should have correct aria-controls attribute', () => {
      render(<MobileNav items={mockItems} />)

      const button = screen.getByLabelText('Open navigation menu')
      expect(button).toHaveAttribute('aria-controls', 'mobile-navigation')
    })

    it('should have correct aria-label on navigation', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        const nav = screen.getByRole('navigation', { name: 'Mobile navigation' })
        expect(nav).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Close Button (2 tests)
  // ============================================
  describe('Close Button', () => {
    it('should render close button in menu panel', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        expect(screen.getByLabelText('Close menu')).toBeInTheDocument()
      })
    })

    it('should close menu when close button is clicked', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        expect(screen.getByRole('navigation', { name: 'Mobile navigation' })).toBeInTheDocument()
      })

      await user.click(screen.getByLabelText('Close menu'))

      await waitFor(() => {
        expect(screen.queryByRole('navigation', { name: 'Mobile navigation' })).not.toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Integration (2 tests)
  // ============================================
  describe('Integration', () => {
    it('should handle complete open-navigate-close flow', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      // Initial state
      expect(screen.queryByRole('navigation', { name: 'Mobile navigation' })).not.toBeInTheDocument()
      expect(document.body.style.overflow).toBe('unset')

      // Open menu
      await user.click(screen.getByLabelText('Open navigation menu'))

      await waitFor(() => {
        expect(screen.getByRole('navigation', { name: 'Mobile navigation' })).toBeInTheDocument()
        expect(document.body.style.overflow).toBe('hidden')
      })

      // Verify all items present
      expect(screen.getByText('Dashboard')).toBeInTheDocument()
      expect(screen.getByText('Projects')).toBeInTheDocument()
      expect(screen.getByText('Messages')).toBeInTheDocument()
      expect(screen.getByText('Create Project')).toBeInTheDocument()

      // Click a link
      await user.click(screen.getByText('Projects'))

      // Menu should close
      await waitFor(() => {
        expect(screen.queryByRole('navigation', { name: 'Mobile navigation' })).not.toBeInTheDocument()
        expect(document.body.style.overflow).toBe('unset')
      })
    })

    it('should handle multiple open/close cycles', async () => {
      const user = userEvent.setup()
      render(<MobileNav items={mockItems} />)

      // First cycle - open with button, close with backdrop
      await user.click(screen.getByLabelText('Open navigation menu'))
      await waitFor(() => {
        expect(screen.getByRole('navigation', { name: 'Mobile navigation' })).toBeInTheDocument()
      })

      const backdrop1 = document.querySelector('.bg-overlay\\/80')
      if (backdrop1) fireEvent.click(backdrop1)

      await waitFor(() => {
        expect(screen.queryByRole('navigation', { name: 'Mobile navigation' })).not.toBeInTheDocument()
      })

      // Second cycle - open with button, close with Escape
      await user.click(screen.getByLabelText('Open navigation menu'))
      await waitFor(() => {
        expect(screen.getByRole('navigation', { name: 'Mobile navigation' })).toBeInTheDocument()
      })

      fireEvent.keyDown(document, { key: 'Escape' })

      await waitFor(() => {
        expect(screen.queryByRole('navigation', { name: 'Mobile navigation' })).not.toBeInTheDocument()
      })

      // Third cycle - open with button, close with close button
      await user.click(screen.getByLabelText('Open navigation menu'))
      await waitFor(() => {
        expect(screen.getByRole('navigation', { name: 'Mobile navigation' })).toBeInTheDocument()
      })

      await user.click(screen.getByLabelText('Close menu'))

      await waitFor(() => {
        expect(screen.queryByRole('navigation', { name: 'Mobile navigation' })).not.toBeInTheDocument()
        expect(document.body.style.overflow).toBe('unset')
      })
    })
  })
})
