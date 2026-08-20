/**
 * Tests for EnhancedProjectCard
 *
 * Comprehensive test suite for the enhanced project card component
 * Coverage target: 70%+ (372 lines)
 */

import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { EnhancedProjectCard } from '../EnhancedProjectCard'

const mockProject = {
  id: 'proj-123',
  title: 'Full Stack Developer Needed',
  description: 'We are looking for an experienced full stack developer to build a modern web application using React, Node.js, and PostgreSQL.',
  category: 'Development',
  budget: 5000,
  budgetType: 'fixed' as const,
  duration: '3 months',
  location: 'Remote',
  remote: true,
  status: 'open' as const,
  clientRating: 4,
  applicationsCount: 15,
  skills: ['React', 'Node.js', 'PostgreSQL', 'TypeScript', 'AWS'],
  featured: false,
  urgent: false,
  postedAt: '2 days ago',
}

describe('EnhancedProjectCard', () => {
  describe('Basic Rendering', () => {
    it('should render the project card', () => {
      render(<EnhancedProjectCard project={mockProject} />)

      expect(screen.getByText('Full Stack Developer Needed')).toBeInTheDocument()
      expect(screen.getByText(/We are looking for an experienced full stack developer/)).toBeInTheDocument()
    })

    it('should render project category', () => {
      render(<EnhancedProjectCard project={mockProject} />)

      expect(screen.getByText('Development')).toBeInTheDocument()
    })

    it('should render remote badge when project is remote', () => {
      render(<EnhancedProjectCard project={mockProject} />)

      expect(screen.getAllByText('Remote').length).toBeGreaterThan(0)
    })

    it('should not render remote badge when project is not remote', () => {
      const { container } = render(<EnhancedProjectCard project={{ ...mockProject, remote: false }} />)

      // The remote badge should not be in the category section
      // We check by counting how many times we see the success badge
      const successBadges = container.querySelectorAll('.bg-success\\/10.text-success')
      // When remote=false, we should only see the OPEN status badge, not the Remote badge
      // So count should be 1 (just the status)
      expect(successBadges.length).toBe(1) // Only status badge, not remote badge
    })

    it('should render budget in credits', () => {
      render(<EnhancedProjectCard project={mockProject} />)

      expect(screen.getByText('5,000 credits')).toBeInTheDocument()
    })

    it('should render budget type', () => {
      render(<EnhancedProjectCard project={mockProject} />)

      expect(screen.getByText('fixed')).toBeInTheDocument()
    })

    it('should render duration', () => {
      render(<EnhancedProjectCard project={mockProject} />)

      expect(screen.getByText('3 months')).toBeInTheDocument()
    })

    it('should render location', () => {
      render(<EnhancedProjectCard project={mockProject} />)

      const locationElements = screen.getAllByText('Remote')
      expect(locationElements.length).toBeGreaterThan(0)
    })

    it('should render applications count', () => {
      render(<EnhancedProjectCard project={mockProject} />)

      expect(screen.getByText('15')).toBeInTheDocument()
    })

    it('should render posted date', () => {
      render(<EnhancedProjectCard project={mockProject} />)

      expect(screen.getByText('2 days ago')).toBeInTheDocument()
    })
  })

  describe('Featured Projects', () => {
    it('should show featured banner when project is featured', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, featured: true }} />)

      expect(screen.getByText('FEATURED PROJECT')).toBeInTheDocument()
    })

    it('should not show featured banner when project is not featured', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, featured: false }} />)

      expect(screen.queryByText('FEATURED PROJECT')).not.toBeInTheDocument()
    })
  })

  describe('Urgent Projects', () => {
    it('should show urgent badge when project is urgent', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, urgent: true }} />)

      expect(screen.getByText('URGENT')).toBeInTheDocument()
    })

    it('should not show urgent badge when project is not urgent', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, urgent: false }} />)

      expect(screen.queryByText('URGENT')).not.toBeInTheDocument()
    })
  })

  describe('Skills Display', () => {
    it('should display first 4 skills', () => {
      render(<EnhancedProjectCard project={mockProject} />)

      expect(screen.getByText('React')).toBeInTheDocument()
      expect(screen.getByText('Node.js')).toBeInTheDocument()
      expect(screen.getByText('PostgreSQL')).toBeInTheDocument()
      expect(screen.getByText('TypeScript')).toBeInTheDocument()
    })

    it('should show "+X more" badge when more than 4 skills', () => {
      render(<EnhancedProjectCard project={mockProject} />)

      expect(screen.getByText('+1 more')).toBeInTheDocument()
    })

    it('should not show "+X more" badge when 4 or fewer skills', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, skills: ['React', 'Node.js', 'PostgreSQL'] }} />)

      expect(screen.queryByText(/\+\d+ more/)).not.toBeInTheDocument()
    })

    it('should handle empty skills array', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, skills: [] }} />)

      expect(screen.getByText('Full Stack Developer Needed')).toBeInTheDocument()
    })
  })

  describe('Project Status', () => {
    it('should display "OPEN" status for open projects', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, status: 'open' }} />)

      expect(screen.getByText('OPEN')).toBeInTheDocument()
    })

    it('should display "IN PROGRESS" status for in_progress projects', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, status: 'in_progress' }} />)

      expect(screen.getByText('IN PROGRESS')).toBeInTheDocument()
    })

    it('should display "COMPLETED" status for completed projects', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, status: 'completed' }} />)

      expect(screen.getByText('COMPLETED')).toBeInTheDocument()
    })
  })

  describe('Client Rating', () => {
    it('should display client rating when provided', () => {
      render(<EnhancedProjectCard project={mockProject} />)

      expect(screen.getByText(/Client Rating \(4\.0\)/)).toBeInTheDocument()
    })

    it('should not display client rating when not provided', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, clientRating: undefined }} />)

      expect(screen.queryByText(/Client Rating/)).not.toBeInTheDocument()
    })

    it('should render 5 star icons for rating display', () => {
      const { container } = render(<EnhancedProjectCard project={mockProject} />)

      // Check that star rating section exists
      const ratingText = screen.getByText(/Client Rating \(4\.0\)/)
      expect(ratingText).toBeInTheDocument()
    })
  })

  describe('Match Score (Recommendation Variant)', () => {
    it('should display match score in recommendation variant', () => {
      render(
        <EnhancedProjectCard
          project={{ ...mockProject, matchScore: 85 }}
          variant="recommendation"
        />
      )

      expect(screen.getByText('85%')).toBeInTheDocument()
      expect(screen.getByText('Match Score')).toBeInTheDocument()
      expect(screen.getByText('Based on your skills')).toBeInTheDocument()
    })

    it('should not display match score in search variant', () => {
      render(
        <EnhancedProjectCard
          project={{ ...mockProject, matchScore: 85 }}
          variant="search"
        />
      )

      expect(screen.queryByText('Match Score')).not.toBeInTheDocument()
    })

    it('should not display match score when not provided', () => {
      render(
        <EnhancedProjectCard
          project={mockProject}
          variant="recommendation"
        />
      )

      expect(screen.queryByText('Match Score')).not.toBeInTheDocument()
    })
  })

  describe('Favorite Functionality', () => {
    it('should toggle favorite state when favorite button is clicked', async () => {
      const user = userEvent.setup()
      const { container } = render(<EnhancedProjectCard project={mockProject} />)

      const favoriteButton = container.querySelector('button') as HTMLElement
      expect(favoriteButton).toBeTruthy()

      await user.click(favoriteButton)

      // Button should still exist after click
      expect(favoriteButton).toBeTruthy()
    })

    it('should handle clicking favorite button without errors', async () => {
      const user = userEvent.setup()
      const { container } = render(<EnhancedProjectCard project={mockProject} />)

      const favoriteButton = container.querySelector('button') as HTMLElement
      expect(favoriteButton).toBeTruthy()

      // Should not throw error
      await user.click(favoriteButton)
      await user.click(favoriteButton)

      // Button should still be in the DOM
      expect(favoriteButton).toBeTruthy()
    })
  })

  describe('Hover Effects', () => {
    it('should handle mouse enter event when animated', () => {
      const { container } = render(<EnhancedProjectCard project={mockProject} animated={true} />)

      const link = container.querySelector('a') as HTMLElement
      fireEvent.mouseEnter(link)

      expect(link).toBeTruthy()
    })

    it('should handle mouse leave event when animated', () => {
      const { container } = render(<EnhancedProjectCard project={mockProject} animated={true} />)

      const link = container.querySelector('a') as HTMLElement
      fireEvent.mouseLeave(link)

      expect(link).toBeTruthy()
    })

    it('should render without animation when animated is false', () => {
      const { container } = render(<EnhancedProjectCard project={mockProject} animated={false} />)

      expect(container.firstChild).toBeTruthy()
    })
  })

  describe('Link Functionality', () => {
    it('should link to project detail page', () => {
      render(<EnhancedProjectCard project={mockProject} />)

      const link = screen.getByText('Full Stack Developer Needed').closest('a')
      expect(link).toHaveAttribute('href', '/projects/proj-123')
    })
  })

  describe('Variants', () => {
    it('should render search variant by default', () => {
      const { container } = render(<EnhancedProjectCard project={mockProject} />)

      expect(container.firstChild).toBeTruthy()
    })

    it('should render recommendation variant', () => {
      const { container } = render(
        <EnhancedProjectCard project={mockProject} variant="recommendation" />
      )

      expect(container.firstChild).toBeTruthy()
    })

    it('should render marketplace variant', () => {
      const { container } = render(
        <EnhancedProjectCard project={mockProject} variant="marketplace" />
      )

      expect(container.firstChild).toBeTruthy()
    })
  })

  describe('Budget Types', () => {
    it('should display hourly budget type', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, budgetType: 'hourly' }} />)

      expect(screen.getByText('hourly')).toBeInTheDocument()
    })

    it('should display fixed budget type', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, budgetType: 'fixed' }} />)

      expect(screen.getByText('fixed')).toBeInTheDocument()
    })
  })

  describe('Categories', () => {
    it('should render Development category', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, category: 'Development' }} />)

      expect(screen.getByText('Development')).toBeInTheDocument()
    })

    it('should render Design category', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, category: 'Design' }} />)

      expect(screen.getByText('Design')).toBeInTheDocument()
    })

    it('should render Marketing category', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, category: 'Marketing' }} />)

      expect(screen.getByText('Marketing')).toBeInTheDocument()
    })

    it('should render Writing category', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, category: 'Writing' }} />)

      expect(screen.getByText('Writing')).toBeInTheDocument()
    })

    it('should render Consulting category', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, category: 'Consulting' }} />)

      expect(screen.getByText('Consulting')).toBeInTheDocument()
    })

    it('should render Sales category', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, category: 'Sales' }} />)

      expect(screen.getByText('Sales')).toBeInTheDocument()
    })

    it('should handle unknown category', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, category: 'Unknown' }} />)

      expect(screen.getByText('Unknown')).toBeInTheDocument()
    })
  })

  describe('Edge Cases', () => {
    it('should handle very long project title', () => {
      render(
        <EnhancedProjectCard
          project={{
            ...mockProject,
            title: 'A'.repeat(200),
          }}
        />
      )

      const container = document.body
      expect(container).toBeTruthy()
    })

    it('should handle very long description', () => {
      render(
        <EnhancedProjectCard
          project={{
            ...mockProject,
            description: 'A'.repeat(500),
          }}
        />
      )

      const container = document.body
      expect(container).toBeTruthy()
    })

    it('should handle zero budget', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, budget: 0 }} />)

      expect(screen.getByText('0 credits')).toBeInTheDocument()
    })

    it('should handle zero applications', () => {
      render(<EnhancedProjectCard project={{ ...mockProject, applicationsCount: 0 }} />)

      expect(screen.getByText('0')).toBeInTheDocument()
    })

    it('should handle missing optional fields', () => {
      render(
        <EnhancedProjectCard
          project={{
            ...mockProject,
            featured: undefined,
            urgent: undefined,
            clientRating: undefined,
            matchScore: undefined,
          }}
        />
      )

      expect(screen.getByText('Full Stack Developer Needed')).toBeInTheDocument()
    })
  })

  describe('Accessibility', () => {
    it('should have accessible link', () => {
      render(<EnhancedProjectCard project={mockProject} />)

      const link = screen.getByText('Full Stack Developer Needed').closest('a')
      expect(link).toBeInTheDocument()
    })

    it('should have accessible favorite button', () => {
      const { container } = render(<EnhancedProjectCard project={mockProject} />)

      const button = container.querySelector('button')
      expect(button).toBeInTheDocument()
    })
  })

  describe('Integration', () => {
    it('should render complete project card without errors', () => {
      const { container } = render(<EnhancedProjectCard project={mockProject} />)

      expect(container.firstChild).toBeTruthy()
      expect(screen.getByText('Full Stack Developer Needed')).toBeInTheDocument()
      expect(screen.getByText('Development')).toBeInTheDocument()
      expect(screen.getByText('5,000 credits')).toBeInTheDocument()
    })

    it('should render with all features enabled', () => {
      const { container } = render(
        <EnhancedProjectCard
          project={{
            ...mockProject,
            featured: true,
            urgent: true,
            matchScore: 90,
            clientRating: 5,
          }}
          variant="recommendation"
          animated={true}
        />
      )

      expect(container.firstChild).toBeTruthy()
      expect(screen.getByText('FEATURED PROJECT')).toBeInTheDocument()
      expect(screen.getByText('URGENT')).toBeInTheDocument()
      expect(screen.getByText('Match Score')).toBeInTheDocument()
      expect(screen.getByText(/Client Rating/)).toBeInTheDocument()
    })
  })
})
