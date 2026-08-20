import React from 'react'
import { render, screen } from '@testing-library/react'
import MessagesPage from '../page'

describe('MessagesPage', () => {
  // ============================================
  // Page Content (5 tests)
  // ============================================
  describe('Page Content', () => {
    it('should display "Select a Conversation" heading', () => {
      render(<MessagesPage />)

      expect(screen.getByText('Select a Conversation')).toBeInTheDocument()
    })

    it('should display description text', () => {
      render(<MessagesPage />)

      expect(
        screen.getByText(/Choose a conversation from the sidebar to start messaging/)
      ).toBeInTheDocument()
    })

    it('should display message icon', () => {
      const { container } = render(<MessagesPage />)

      // Check for message icon container
      const iconContainers = container.querySelectorAll('[class*="rounded-full"]')
      expect(iconContainers.length).toBeGreaterThanOrEqual(1)
    })

    it('should have proper container styling', () => {
      const { container } = render(<MessagesPage />)

      const mainContainer = container.querySelector('.h-full.flex.items-center.justify-center')
      expect(mainContainer).toBeInTheDocument()
    })

    it('should display card with content', () => {
      const { container } = render(<MessagesPage />)

      // Card should be present
      const card = container.querySelector('[class*="border-border"][class*="rounded"]')
      expect(card).toBeInTheDocument()
    })
  })

  // ============================================
  // Features Section (3 tests)
  // ============================================
  describe('Features Section', () => {
    it('should display "Real-time Messaging" feature', () => {
      render(<MessagesPage />)

      expect(screen.getByText('Real-time Messaging')).toBeInTheDocument()
      expect(screen.getByText(/Instant communication with typing indicators/)).toBeInTheDocument()
    })

    it('should display "Project Collaboration" feature', () => {
      render(<MessagesPage />)

      expect(screen.getByText('Project Collaboration')).toBeInTheDocument()
      expect(screen.getByText(/Connect with clients and service providers/)).toBeInTheDocument()
    })

    it('should display features in a styled container', () => {
      const { container } = render(<MessagesPage />)

      const featuresContainer = container.querySelector('[class*="bg-muted"]')
      expect(featuresContainer).toBeInTheDocument()
    })
  })

  // ============================================
  // Navigation Links (3 tests)
  // ============================================
  describe('Navigation Links', () => {
    it('should display "Browse Projects" button', () => {
      render(<MessagesPage />)

      const projectsButton = screen.getByRole('button', { name: /Browse Projects/i })
      expect(projectsButton).toBeInTheDocument()
    })

    it('should display "Go to Dashboard" button', () => {
      render(<MessagesPage />)

      const dashboardButton = screen.getByRole('button', { name: /Go to Dashboard/i })
      expect(dashboardButton).toBeInTheDocument()
    })

    it('should have correct link destinations', () => {
      const { container } = render(<MessagesPage />)

      const projectsLink = container.querySelector('a[href="/projects/search"]')
      const dashboardLink = container.querySelector('a[href="/dashboard"]')

      expect(projectsLink).toBeInTheDocument()
      expect(dashboardLink).toBeInTheDocument()
    })
  })

  // ============================================
  // Integration (1 test)
  // ============================================
  describe('Integration', () => {
    it('should render complete page without errors', () => {
      const { container } = render(<MessagesPage />)

      // Verify major elements are present
      expect(container.firstChild).toBeTruthy()
      expect(screen.getByText('Select a Conversation')).toBeInTheDocument()
      expect(screen.getByText('Real-time Messaging')).toBeInTheDocument()
      expect(screen.getByText('Project Collaboration')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /Browse Projects/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /Go to Dashboard/i })).toBeInTheDocument()
    })
  })
})
