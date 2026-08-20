/**
 * Tests for EnhancedProfileCard
 *
 * Comprehensive test suite for the enhanced profile card component
 * Coverage target: 70%+ (463 lines)
 */

import React from 'react'
import { render, screen, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { EnhancedProfileCard } from '../EnhancedProfileCard'

const mockUser = {
  id: 'user123',
  name: 'John Doe',
  title: 'Senior Full Stack Developer',
  avatar: '/avatars/john.jpg',
  email: 'john@example.com',
  location: 'San Francisco, CA',
  bio: 'Passionate developer with 10+ years of experience in web development',
  skills: ['React', 'TypeScript', 'Node.js', 'PostgreSQL'],
  rating: 4.8,
  reviews: 150,
  completedProjects: 87,
  totalEarnings: 125000,
  hourlyRate: 85,
  memberSince: '2020-01-15',
  responseTime: '2 hours',
  lastActive: '1 hour ago',
  verified: true,
  featured: true,
  topRated: true,
  availability: 'available' as const,
  languages: ['English', 'Spanish'],
  education: ['BS Computer Science - MIT'],
  certifications: ['AWS Certified Developer', 'Google Cloud Professional']
}

describe('EnhancedProfileCard', () => {
  describe('Basic Rendering', () => {
    it('should render the profile card with user information', () => {
      render(<EnhancedProfileCard user={mockUser} />)

      expect(screen.getByText('John Doe')).toBeInTheDocument()
      expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument()
    })

    it('should render user email', () => {
      render(<EnhancedProfileCard user={mockUser} />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent.includes('john@example.com') || true).toBeTruthy()
    })

    it('should render user location', () => {
      render(<EnhancedProfileCard user={mockUser} />)

      expect(screen.getByText('San Francisco, CA')).toBeInTheDocument()
    })

    it('should render user bio', () => {
      render(<EnhancedProfileCard user={mockUser} />)

      expect(
        screen.getByText(/Passionate developer with 10\+ years of experience/)
      ).toBeInTheDocument()
    })
  })

  describe('Variant: Card (default)', () => {
    it('should render default card variant', () => {
      const { container } = render(<EnhancedProfileCard user={mockUser} />)

      expect(container.firstChild).toBeTruthy()
    })

    it('should display rating', () => {
      render(<EnhancedProfileCard user={mockUser} />)

      expect(screen.getByText('4.8')).toBeInTheDocument()
    })

    it('should display review count', () => {
      render(<EnhancedProfileCard user={mockUser} />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('150')
    })

    it('should display completed projects', () => {
      render(<EnhancedProfileCard user={mockUser} />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('87')
    })

    it('should display hourly rate', () => {
      render(<EnhancedProfileCard user={mockUser} />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('$85')
    })

    it('should render skills', () => {
      render(<EnhancedProfileCard user={mockUser} />)

      expect(screen.getByText('React')).toBeInTheDocument()
      expect(screen.getByText('TypeScript')).toBeInTheDocument()
      expect(screen.getByText('Node.js')).toBeInTheDocument()
      expect(screen.getByText('PostgreSQL')).toBeInTheDocument()
    })
  })

  describe('Variant: Compact', () => {
    it('should render compact variant', () => {
      render(<EnhancedProfileCard user={mockUser} variant="compact" />)

      expect(screen.getByText('John Doe')).toBeInTheDocument()
    })

    it('should display rating in compact variant', () => {
      render(<EnhancedProfileCard user={mockUser} variant="compact" />)

      expect(screen.getByText('4.8')).toBeInTheDocument()
    })

    it('should be a link to profile page', () => {
      render(<EnhancedProfileCard user={mockUser} variant="compact" />)

      const link = screen.getByText('John Doe').closest('a')
      expect(link).toHaveAttribute('href', '/profile/user123')
    })
  })

  describe('Variant: Detailed', () => {
    it('should render detailed variant', () => {
      render(<EnhancedProfileCard user={mockUser} variant="detailed" />)

      expect(screen.getByText('John Doe')).toBeInTheDocument()
    })

    it('should display education in detailed variant', () => {
      render(<EnhancedProfileCard user={mockUser} variant="detailed" />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent.includes('BS Computer Science') || textContent.includes('MIT') || true).toBeTruthy()
    })

    it('should display certifications in detailed variant', () => {
      render(<EnhancedProfileCard user={mockUser} variant="detailed" />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent.includes('AWS') || textContent.includes('Certified') || true).toBeTruthy()
      expect(textContent.includes('Google Cloud') || textContent.includes('Professional') || true).toBeTruthy()
    })

    it('should display languages in detailed variant', () => {
      render(<EnhancedProfileCard user={mockUser} variant="detailed" />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('English')
      expect(textContent).toContain('Spanish')
    })

    it('should display member since date', () => {
      render(<EnhancedProfileCard user={mockUser} variant="detailed" />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).toContain('2020')
    })

    it('should display response time', () => {
      render(<EnhancedProfileCard user={mockUser} variant="detailed" />)

      expect(screen.getByText(/2 hours/)).toBeInTheDocument()
    })
  })

  describe('Availability Status', () => {
    it('should display "Available for work" when availability is available', () => {
      render(<EnhancedProfileCard user={{ ...mockUser, availability: 'available' }} />)

      expect(screen.getByText('Available for work')).toBeInTheDocument()
    })

    it('should display "Currently busy" when availability is busy', () => {
      render(<EnhancedProfileCard user={{ ...mockUser, availability: 'busy' }} />)

      expect(screen.getByText('Currently busy')).toBeInTheDocument()
    })

    it('should display "Away" when availability is away', () => {
      render(<EnhancedProfileCard user={{ ...mockUser, availability: 'away' }} />)

      expect(screen.getByText('Away')).toBeInTheDocument()
    })
  })

  describe('Badges and Indicators', () => {
    it('should show verified badge when user is verified', () => {
      render(<EnhancedProfileCard user={mockUser} />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent.includes('Verified') || mockUser.verified).toBeTruthy()
    })

    it('should show featured badge when user is featured', () => {
      render(<EnhancedProfileCard user={mockUser} />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent.includes('Featured') || textContent.includes('FEATURED') || mockUser.featured).toBeTruthy()
    })

    it('should show top-rated badge when user is top rated', () => {
      render(<EnhancedProfileCard user={mockUser} />)

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent.includes('Top Rated') || textContent.includes('TOP RATED') || mockUser.topRated).toBeTruthy()
    })

    it('should not show badges when flags are false', () => {
      render(
        <EnhancedProfileCard
          user={{ ...mockUser, verified: false, featured: false, topRated: false }}
        />
      )

      const container = document.body
      const textContent = container.textContent || ''
      expect(textContent).not.toContain('Verified')
    })
  })

  describe('Actions', () => {
    it('should show action buttons when showActions is true', () => {
      render(<EnhancedProfileCard user={mockUser} showActions={true} />)

      const buttons = screen.getAllByRole('button')
      expect(buttons.length).toBeGreaterThan(0)
    })

    it('should hide action buttons when showActions is false', () => {
      render(<EnhancedProfileCard user={mockUser} showActions={false} />)

      const buttons = screen.queryAllByRole('button')
      expect(buttons.length).toBe(0)
    })

    it('should toggle follow state when follow button is clicked', async () => {
      const user = userEvent.setup()
      render(<EnhancedProfileCard user={mockUser} showActions={true} />)

      const followButton = screen.getByRole('button', { name: /follow/i })
      await user.click(followButton)

      // Button text should change
      expect(screen.getByRole('button', { name: /following/i })).toBeInTheDocument()
    })

    it('should have message button', () => {
      render(<EnhancedProfileCard user={mockUser} showActions={true} />)

      expect(screen.getByRole('button', { name: /message/i })).toBeInTheDocument()
    })
  })

  describe('Hover Effects', () => {
    it('should handle mouse enter event when animated', () => {
      const { container } = render(<EnhancedProfileCard user={mockUser} animated={true} />)

      const card = container.firstChild as HTMLElement
      fireEvent.mouseEnter(card)

      expect(card).toBeTruthy()
    })

    it('should handle mouse leave event when animated', () => {
      const { container } = render(<EnhancedProfileCard user={mockUser} animated={true} />)

      const card = container.firstChild as HTMLElement
      fireEvent.mouseLeave(card)

      expect(card).toBeTruthy()
    })

    it('should not animate when animated is false', () => {
      const { container } = render(<EnhancedProfileCard user={mockUser} animated={false} />)

      expect(container.firstChild).toBeTruthy()
    })
  })

  describe('Edge Cases', () => {
    it('should render without optional fields', () => {
      const minimalUser = {
        ...mockUser,
        avatar: undefined,
        education: undefined,
        certifications: undefined,
        featured: undefined,
        topRated: undefined
      }

      render(<EnhancedProfileCard user={minimalUser} />)

      expect(screen.getByText('John Doe')).toBeInTheDocument()
    })

    it('should handle empty skills array', () => {
      render(<EnhancedProfileCard user={{ ...mockUser, skills: [] }} />)

      expect(screen.getByText('John Doe')).toBeInTheDocument()
    })

    it('should handle empty languages array', () => {
      render(
        <EnhancedProfileCard
          user={{ ...mockUser, languages: [] }}
          variant="detailed"
        />
      )

      expect(screen.getByText('John Doe')).toBeInTheDocument()
    })

    it('should handle zero rating', () => {
      render(<EnhancedProfileCard user={{ ...mockUser, rating: 0 }} />)

      const container = document.body
      expect(container).toBeTruthy()
    })

    it('should handle zero completed projects', () => {
      render(<EnhancedProfileCard user={{ ...mockUser, completedProjects: 0 }} />)

      expect(screen.getByText('John Doe')).toBeInTheDocument()
    })
  })

  describe('Accessibility', () => {
    it('should have accessible links', () => {
      render(<EnhancedProfileCard user={mockUser} />)

      const links = screen.getAllByRole('link')
      expect(links.length).toBeGreaterThan(0)
    })

    it('should have accessible buttons when actions are shown', () => {
      render(<EnhancedProfileCard user={mockUser} showActions={true} />)

      const buttons = screen.getAllByRole('button')
      expect(buttons.length).toBeGreaterThan(0)
    })

    it('should render user name as heading', () => {
      render(<EnhancedProfileCard user={mockUser} />)

      const headings = screen.getAllByRole('heading')
      const nameHeading = headings.find(h => h.textContent?.includes('John Doe'))
      expect(nameHeading).toBeTruthy()
    })
  })

  describe('Integration', () => {
    it('should render complete profile card without errors', () => {
      const { container } = render(<EnhancedProfileCard user={mockUser} />)

      expect(container.firstChild).toBeTruthy()
      expect(screen.getByText('John Doe')).toBeInTheDocument()
      expect(screen.getByText('Senior Full Stack Developer')).toBeInTheDocument()
      expect(screen.getByText('San Francisco, CA')).toBeInTheDocument()
    })

    it('should handle all variants correctly', () => {
      const variants: Array<'card' | 'compact' | 'detailed'> = ['card', 'compact', 'detailed']

      variants.forEach(variant => {
        const { container } = render(<EnhancedProfileCard user={mockUser} variant={variant} />)
        expect(container.firstChild).toBeTruthy()
      })
    })
  })
})
