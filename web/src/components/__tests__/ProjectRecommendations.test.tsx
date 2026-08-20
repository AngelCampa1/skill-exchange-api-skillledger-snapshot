/**
 * Tests for ProjectRecommendations
 *
 * Comprehensive test suite for the project recommendations component
 * Coverage target: 80%+ (387 lines)
 */

import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ProjectRecommendations from '../ProjectRecommendations'

// Mock dependencies
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}))

jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}))

jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
  },
}))

import { useAuth } from '@/contexts/AuthContext'
import { useRouter } from 'next/navigation'

const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>
const mockUseRouter = useRouter as jest.MockedFunction<typeof useRouter>

describe('ProjectRecommendations', () => {
  let mockFetch: jest.MockedFunction<typeof fetch>
  let mockPush: jest.Mock

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

  const mockProjects = [
    {
      id: 'project-1',
      title: 'React Development',
      description: 'Build a modern web app with React',
      creditBudget: 500,
      status: 'Open',
      skills: [
        { skillId: 'skill-1', skillName: 'React', proficiencyRequired: 4, weight: 0.8 },
      ],
      client: {
        id: 'client-1',
        userName: 'Client Name',
        profileComplete: true,
      },
      createdAt: '2024-01-01T00:00:00Z',
      isUrgent: false,
      isFeatured: true,
      complexityScore: 0.7,
      matchScore: 0.9,
      matchReasons: ['Matching skills: React', 'Budget fits your rate'],
    },
    {
      id: 'project-2',
      title: 'Backend API Development',
      description: 'Create REST API with Node.js',
      creditBudget: 300,
      status: 'Open',
      location: {
        city: 'San Francisco',
        state: 'CA',
        country: 'USA',
      },
      skills: [
        { skillId: 'skill-2', skillName: 'Node.js', proficiencyRequired: 3, weight: 0.6 },
      ],
      client: {
        id: 'client-2',
        userName: 'Another Client',
        profileComplete: true,
      },
      createdAt: '2024-01-02T00:00:00Z',
      isUrgent: true,
      isFeatured: false,
      complexityScore: 0.5,
      matchScore: 0.75,
      matchReasons: ['Near your location'],
    },
  ]

  beforeEach(() => {
    jest.clearAllMocks()

    mockPush = jest.fn()
    mockUseRouter.mockReturnValue({
      push: mockPush,
    } as any)

    mockUseAuth.mockReturnValue({
      user: mockUser,
      isAuthenticated: true,
      logout: jest.fn(),
      login: jest.fn(),
      isLoading: false,
        isInitialized: true,
        refreshToken: jest.fn(),
      updateUser: jest.fn(),
    })

    // Mock global fetch
    mockFetch = jest.fn()
    global.fetch = mockFetch

    // Default successful response
    mockFetch.mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => mockProjects,
    } as Response)
  })

  afterEach(() => {
    jest.clearAllMocks()
  })

  describe('Authentication Required', () => {
    it('should show login message when not authenticated', () => {
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

      render(<ProjectRecommendations />)

      expect(screen.getByText('Authentication Required')).toBeInTheDocument()
      expect(screen.getByText('Please log in to see personalized project recommendations.')).toBeInTheDocument()
    })

    it('should have Sign In button when not authenticated', () => {
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

      render(<ProjectRecommendations />)

      const signInButton = screen.getByText('Sign In')
      expect(signInButton).toBeInTheDocument()
    })

    it('should navigate to login when clicking Sign In', async () => {
      const user = userEvent.setup()
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

      render(<ProjectRecommendations />)

      const signInButton = screen.getByText('Sign In')
      await user.click(signInButton)

      expect(mockPush).toHaveBeenCalledWith('/login')
    })
  })

  describe('Initial Loading', () => {
    it('should fetch recommendations on mount when authenticated', async () => {
      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/project-search/recommendations?limit=6',
          { credentials: 'include' }
        )
      })
    })

    it('should not fetch recommendations when not authenticated', () => {
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

      render(<ProjectRecommendations />)

      expect(mockFetch).not.toHaveBeenCalled()
    })

    it('should use custom limit when provided', async () => {
      render(<ProjectRecommendations limit={10} />)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/project-search/recommendations?limit=10',
          { credentials: 'include' }
        )
      })
    })

    it('should exclude specified project IDs', async () => {
      render(<ProjectRecommendations excludeProjectIds={['proj-1', 'proj-2']} />)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/project-search/recommendations?limit=6&exclude=proj-1%2Cproj-2',
          { credentials: 'include' }
        )
      })
    })
  })

  describe('Recommendations Display', () => {
    it('should display header and description', async () => {
      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('Recommended for You')).toBeInTheDocument()
        expect(screen.getByText('Projects that match your skills and preferences')).toBeInTheDocument()
      })
    })

    it('should display project titles', async () => {
      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('React Development')).toBeInTheDocument()
        expect(screen.getByText('Backend API Development')).toBeInTheDocument()
      })
    })

    it('should display project descriptions', async () => {
      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('Build a modern web app with React')).toBeInTheDocument()
        expect(screen.getByText('Create REST API with Node.js')).toBeInTheDocument()
      })
    })

    it('should display project credit budgets', async () => {
      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText(/500 credits/)).toBeInTheDocument()
        expect(screen.getByText(/300 credits/)).toBeInTheDocument()
      })
    })

    it('should display match scores', async () => {
      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('90% match')).toBeInTheDocument()
        expect(screen.getByText('75% match')).toBeInTheDocument()
      })
    })

    it('should display match reasons when enabled', async () => {
      render(<ProjectRecommendations showMatchReasons={true} />)

      await waitFor(() => {
        expect(screen.getByText('Matching skills: React')).toBeInTheDocument()
        expect(screen.getByText('Budget fits your rate')).toBeInTheDocument()
        expect(screen.getByText('Near your location')).toBeInTheDocument()
      })
    })

    it('should not display match reasons when disabled', async () => {
      render(<ProjectRecommendations showMatchReasons={false} />)

      await waitFor(() => {
        expect(screen.getByText('React Development')).toBeInTheDocument()
      })

      expect(screen.queryByText('Matching skills: React')).not.toBeInTheDocument()
    })

    it('should display urgent badge for urgent projects', async () => {
      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('Urgent')).toBeInTheDocument()
      })
    })

    it('should display featured badge for featured projects', async () => {
      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('Featured')).toBeInTheDocument()
      })
    })
  })

  describe('Refresh Functionality', () => {
    it('should have refresh button', async () => {
      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('Refresh')).toBeInTheDocument()
      })
    })

    it('should refresh recommendations when clicking refresh button', async () => {
      const user = userEvent.setup()
      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('Refresh')).toBeInTheDocument()
      })

      const refreshButton = screen.getByText('Refresh')
      await user.click(refreshButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledTimes(2)
      })
    })

    it('should show refreshing state while refreshing', async () => {
      const user = userEvent.setup()

      // Add delay to fetch to capture refreshing state
      mockFetch.mockImplementation(() =>
        new Promise(resolve =>
          setTimeout(() =>
            resolve({
              ok: true,
              status: 200,
              json: async () => mockProjects,
            } as Response),
            100
          )
        )
      )

      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('Refresh')).toBeInTheDocument()
      })

      const refreshButton = screen.getByText('Refresh')
      await user.click(refreshButton)

      expect(screen.getByText('Refreshing...')).toBeInTheDocument()
    })

    it('should disable refresh button while refreshing', async () => {
      const user = userEvent.setup()

      // Add delay to fetch to capture refreshing state
      mockFetch.mockImplementation(() =>
        new Promise(resolve =>
          setTimeout(() =>
            resolve({
              ok: true,
              status: 200,
              json: async () => mockProjects,
            } as Response),
            100
          )
        )
      )

      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('Refresh')).toBeInTheDocument()
      })

      const refreshButton = screen.getByText('Refresh')
      await user.click(refreshButton)

      const refreshingButton = screen.getByText('Refreshing...')
      expect(refreshingButton).toBeDisabled()
    })
  })

  describe('Project Click Handling', () => {
    it('should navigate to project detail when clicking project', async () => {
      const user = userEvent.setup()
      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('React Development')).toBeInTheDocument()
      })

      const projectCard = screen.getByText('React Development').closest('div')
      await user.click(projectCard!)

      expect(mockPush).toHaveBeenCalledWith('/projects/project-1')
    })

    it('should call custom onProjectClick when provided', async () => {
      const user = userEvent.setup()
      const mockOnProjectClick = jest.fn()
      render(<ProjectRecommendations onProjectClick={mockOnProjectClick} />)

      await waitFor(() => {
        expect(screen.getByText('React Development')).toBeInTheDocument()
      })

      const projectCard = screen.getByText('React Development').closest('div')
      await user.click(projectCard!)

      expect(mockOnProjectClick).toHaveBeenCalledWith('project-1')
      expect(mockPush).not.toHaveBeenCalled()
    })
  })

  describe('Error Handling', () => {
    it('should display error message on fetch failure', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Network error'))

      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('Failed to load recommendations')).toBeInTheDocument()
      })
    })

    it('should display auth error for 401 response', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 401,
      } as Response)

      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('Please log in to see personalized recommendations')).toBeInTheDocument()
      })
    })

    it('should handle 404 response gracefully (no recommendations)', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 404,
      } as Response)

      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.queryByText('Failed to load recommendations')).not.toBeInTheDocument()
      })
    })

    it('should display generic error for other error statuses', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
      } as Response)

      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('Failed to load recommendations')).toBeInTheDocument()
      })
    })
  })

  describe('Empty State', () => {
    it('should display empty message when no recommendations', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => [],
      } as Response)

      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('No recommendations available')).toBeInTheDocument()
      })
    })
  })

  describe('Match Score Coloring', () => {
    it('should use success color for high match scores (80%+)', async () => {
      const { container } = render(<ProjectRecommendations />)

      await waitFor(() => {
        const matchBadge = screen.getByText('90% match').closest('div')
        expect(matchBadge).toHaveClass('bg-success/10')
      })
    })

    it('should use info color for good match scores (60-79%)', async () => {
      render(<ProjectRecommendations />)

      await waitFor(() => {
        const matchBadge = screen.getByText('75% match').closest('div')
        expect(matchBadge).toHaveClass('bg-info/10')
      })
    })

    it('should use warning color for medium match scores (40-59%)', async () => {
      const lowScoreProjects = [{
        ...mockProjects[0],
        matchScore: 0.5,
      }]

      mockFetch.mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => lowScoreProjects,
      } as Response)

      render(<ProjectRecommendations />)

      await waitFor(() => {
        const matchBadge = screen.getByText('50% match').closest('div')
        expect(matchBadge).toHaveClass('bg-warning/10')
      })
    })
  })

  describe('Match Reason Icons', () => {
    it('should display skill icon for skill-related reasons', async () => {
      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('Matching skills: React')).toBeInTheDocument()
      })

      const container = document.body
      const skillIcon = container.querySelector('.text-info')
      expect(skillIcon).toBeTruthy()
    })

    it('should display budget icon for budget-related reasons', async () => {
      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('Budget fits your rate')).toBeInTheDocument()
      })

      const container = document.body
      const budgetIcon = container.querySelector('.text-success')
      expect(budgetIcon).toBeTruthy()
    })

    it('should display location icon for location-related reasons', async () => {
      render(<ProjectRecommendations />)

      await waitFor(() => {
        expect(screen.getByText('Near your location')).toBeInTheDocument()
      })

      const container = document.body
      const locationIcon = container.querySelector('.text-primary')
      expect(locationIcon).toBeTruthy()
    })
  })

  describe('Integration', () => {
    it('should render complete component without errors', () => {
      const { container } = render(<ProjectRecommendations />)

      expect(container.firstChild).toBeTruthy()
    })

    it('should handle full user workflow', async () => {
      const user = userEvent.setup()

      // Add delay to fetch to capture refreshing state
      mockFetch.mockImplementation(() =>
        new Promise(resolve =>
          setTimeout(() =>
            resolve({
              ok: true,
              status: 200,
              json: async () => mockProjects,
            } as Response),
            100
          )
        )
      )

      render(<ProjectRecommendations />)

      // Wait for initial load
      await waitFor(() => {
        expect(screen.getByText('React Development')).toBeInTheDocument()
      })

      // Refresh recommendations
      const refreshButton = screen.getByText('Refresh')
      await user.click(refreshButton)

      await waitFor(() => {
        expect(screen.getByText('Refreshing...')).toBeInTheDocument()
      })

      await waitFor(() => {
        expect(screen.getByText('Refresh')).toBeInTheDocument()
      })

      // Click on a project
      const projectCard = screen.getByText('React Development').closest('div')
      await user.click(projectCard!)

      expect(mockPush).toHaveBeenCalledWith('/projects/project-1')
    })
  })
})
