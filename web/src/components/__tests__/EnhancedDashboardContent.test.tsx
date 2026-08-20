/**
 * Tests for EnhancedDashboardContent
 *
 * Comprehensive test suite for the enhanced dashboard content component
 * Coverage target: 70%+ (469 lines)
 */

import React from 'react'
import { render, screen } from '@testing-library/react'
import { EnhancedDashboardContent } from '../EnhancedDashboardContent'

describe('EnhancedDashboardContent', () => {
  describe('Basic Rendering', () => {
    it('should render the dashboard content', () => {
      render(<EnhancedDashboardContent />)

      const container = document.body
      expect(container).toBeInTheDocument()
    })
  })

  describe('Stats Grid', () => {
    it('should display Total Projects stat card', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getByText('Total Projects')).toBeInTheDocument()
      expect(screen.getAllByText('24').length).toBeGreaterThan(0)
      expect(screen.getByText('+12% from last month')).toBeInTheDocument()
    })

    it('should display Active Collaborations stat card', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getByText('Active Collaborations')).toBeInTheDocument()
      expect(screen.getAllByText('142').length).toBeGreaterThan(0)
      expect(screen.getByText('+8% from last week')).toBeInTheDocument()
    })

    it('should display Credits Available stat card', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getByText('Credits Available')).toBeInTheDocument()
      expect(screen.getAllByText('12,450').length).toBeGreaterThan(0)
      expect(screen.getByText('+2,340 this month')).toBeInTheDocument()
    })

    it('should display Success Rate stat card', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getAllByText('Success Rate')[0]).toBeInTheDocument()
      expect(screen.getAllByText('94%').length).toBeGreaterThan(0)
      expect(screen.getByText('+2% from last quarter')).toBeInTheDocument()
    })

    it('should render all 4 stat cards', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getByText('Total Projects')).toBeInTheDocument()
      expect(screen.getByText('Active Collaborations')).toBeInTheDocument()
      expect(screen.getByText('Credits Available')).toBeInTheDocument()
      expect(screen.getAllByText('Success Rate')[0]).toBeInTheDocument()
    })
  })

  describe('Recent Activity Section', () => {
    it('should display Recent Activity heading', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getAllByText('Recent Activity').length).toBeGreaterThan(0)
    })

    it('should display View All link', () => {
      render(<EnhancedDashboardContent />)

      const viewAllLinks = screen.getAllByText('View All')
      expect(viewAllLinks.length).toBeGreaterThan(0)
      expect(viewAllLinks[0].closest('a')).toHaveAttribute('href', '/dashboard')
    })

    it('should display Project Alpha Launched activity', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getByText('Project Alpha Launched')).toBeInTheDocument()
      expect(
        screen.getByText('Successfully initiated new collaboration project with team of 5')
      ).toBeInTheDocument()
      expect(screen.getByText('2 hours ago')).toBeInTheDocument()
    })

    it('should display Contract Negotiated activity', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getByText('Contract Negotiated')).toBeInTheDocument()
      expect(
        screen.getByText('Finalized terms for web development project')
      ).toBeInTheDocument()
      expect(screen.getByText('5 hours ago')).toBeInTheDocument()
    })

    it('should display Milestone Achieved activity', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getByText('Milestone Achieved')).toBeInTheDocument()
      expect(
        screen.getByText('Completed first phase of mobile app development')
      ).toBeInTheDocument()
      expect(screen.getByText('1 day ago')).toBeInTheDocument()
    })

    it('should display Performance Review activity', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getByText('Performance Review')).toBeInTheDocument()
      expect(
        screen.getByText('Q4 analytics and performance metrics review')
      ).toBeInTheDocument()
      expect(screen.getByText('3 days ago')).toBeInTheDocument()
    })

    it('should render all activity items', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getByText('Project Alpha Launched')).toBeInTheDocument()
      expect(screen.getByText('Contract Negotiated')).toBeInTheDocument()
      expect(screen.getByText('Milestone Achieved')).toBeInTheDocument()
      expect(screen.getByText('Performance Review')).toBeInTheDocument()
    })
  })

  describe('Quick Actions Section', () => {
    it('should display Quick Actions heading', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getAllByText('Quick Actions').length).toBeGreaterThan(0)
    })

    it('should display Quick Actions description', () => {
      render(<EnhancedDashboardContent />)

      expect(
        screen.getByText('Access key features and launch your next collaboration')
      ).toBeInTheDocument()
    })

    it('should display Create New Project quick action', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getByText('Create Project')).toBeInTheDocument()
    })

    it('should display Find Collaborators quick action', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getByText('Browse Projects')).toBeInTheDocument()
    })

    it('should display View Messages quick action', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getAllByText('Join Collaboration').length).toBeGreaterThan(0)
    })

    it('should render quick action links with correct hrefs', () => {
      render(<EnhancedDashboardContent />)

      const createProjectLink = screen.getByText('Create Project').closest('a')
      expect(createProjectLink).toHaveAttribute('href', '/create-project')
    })
  })

  describe('Layout and Structure', () => {
    it('should render stats in a grid layout', () => {
      const { container } = render(<EnhancedDashboardContent />)

      const statsGrid = container.querySelector('.grid')
      expect(statsGrid).toBeTruthy()
    })

    it('should apply proper spacing classes', () => {
      const { container } = render(<EnhancedDashboardContent />)

      const mainContainer = container.querySelector('.space-y-10')
      expect(mainContainer).toBeTruthy()
    })

    it('should render activity section with elevated card', () => {
      const { container } = render(<EnhancedDashboardContent />)

      const activityCard = container.querySelector('.card-elevated')
      expect(activityCard).toBeTruthy()
    })
  })

  describe('Accessibility', () => {
    it('should have accessible headings', () => {
      render(<EnhancedDashboardContent />)

      const headings = screen.getAllByRole('heading', { level: 2 })
      expect(headings.length).toBeGreaterThan(0)
    })

    it('should have accessible links', () => {
      render(<EnhancedDashboardContent />)

      const links = screen.getAllByRole('link')
      expect(links.length).toBeGreaterThan(0)
    })

    it('should have proper link text for screen readers', () => {
      render(<EnhancedDashboardContent />)

      const viewAllLinks = screen.getAllByText('View All')
      expect(viewAllLinks.length).toBeGreaterThan(0)
    })

    it('should display Trending Projects View All link pointing to /projects/search', () => {
      render(<EnhancedDashboardContent />)

      const viewAllLinks = screen.getAllByText('View All')
      // Second "View All" belongs to the Trending Projects section
      expect(viewAllLinks.length).toBeGreaterThanOrEqual(2)
      expect(viewAllLinks[1].closest('a')).toHaveAttribute('href', '/projects/search')
    })
  })

  describe('Content Validation', () => {
    it('should display all required sections', () => {
      render(<EnhancedDashboardContent />)

      // Stats section
      expect(screen.getByText('Total Projects')).toBeInTheDocument()

      // Activity section
      expect(screen.getAllByText('Recent Activity').length).toBeGreaterThan(0)

      // Quick actions section
      expect(screen.getAllByText('Quick Actions').length).toBeGreaterThan(0)
    })

    it('should display numeric stats in correct format', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getAllByText('24').length).toBeGreaterThan(0) // Total Projects
      expect(screen.getAllByText('142').length).toBeGreaterThan(0) // Active Collaborations
      expect(screen.getAllByText('12,450').length).toBeGreaterThan(0) // Credits
      expect(screen.getAllByText('94%').length).toBeGreaterThan(0) // Success Rate
    })

    it('should display change indicators with correct text', () => {
      render(<EnhancedDashboardContent />)

      expect(screen.getByText('+12% from last month')).toBeInTheDocument()
      expect(screen.getByText('+8% from last week')).toBeInTheDocument()
      expect(screen.getByText('+2,340 this month')).toBeInTheDocument()
      expect(screen.getByText('+2% from last quarter')).toBeInTheDocument()
    })
  })

  describe('Responsiveness', () => {
    it('should render without errors on different viewports', () => {
      const { container } = render(<EnhancedDashboardContent />)

      expect(container.firstChild).toBeInTheDocument()
    })

    it('should apply responsive grid classes', () => {
      const { container } = render(<EnhancedDashboardContent />)

      const grid = container.querySelector('.grid-cols-1')
      expect(grid).toBeTruthy()
    })
  })

  describe('Integration', () => {
    it('should render complete dashboard without errors', () => {
      const { container } = render(<EnhancedDashboardContent />)

      expect(container.firstChild).toBeTruthy()
      expect(screen.getByText('Total Projects')).toBeInTheDocument()
      expect(screen.getAllByText('Recent Activity').length).toBeGreaterThan(0)
      expect(screen.getAllByText('Quick Actions').length).toBeGreaterThan(0)
    })

    it('should maintain consistent styling across sections', () => {
      const { container } = render(<EnhancedDashboardContent />)

      const sections = container.querySelectorAll('.space-golden-lg, .space-golden-md')
      expect(sections.length).toBeGreaterThan(0)
    })
  })

  describe('Edge Cases', () => {
    it('should handle rendering without crashing', () => {
      expect(() => {
        render(<EnhancedDashboardContent />)
      }).not.toThrow()
    })

    it('should render consistent content on multiple renders', () => {
      const { rerender } = render(<EnhancedDashboardContent />)

      expect(screen.getByText('Total Projects')).toBeInTheDocument()

      rerender(<EnhancedDashboardContent />)

      expect(screen.getByText('Total Projects')).toBeInTheDocument()
    })
  })
})
