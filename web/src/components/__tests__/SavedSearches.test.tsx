/**
 * Tests for SavedSearches
 *
 * Comprehensive test suite for the saved searches component
 * Coverage target: 80%+ (476 lines)
 */

import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import SavedSearches from '../SavedSearches'

// Mock dependencies
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}))

jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
  },
}))

import { useAuth } from '@/contexts/AuthContext'

const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>

// Mock fetch globally
global.fetch = jest.fn()
const mockFetch = global.fetch as jest.MockedFunction<typeof fetch>

// Mock confirm
const mockConfirm = jest.fn()
global.confirm = mockConfirm

// Mock saved search data
const mockSavedSearches = [
  {
    id: '1',
    name: 'React Developer Projects',
    description: 'Looking for React development opportunities',
    searchCriteria: {
      query: 'React developer',
      skillIds: ['skill1', 'skill2'],
      minBudget: 1000,
      maxBudget: 5000,
      clientLocation: 'San Francisco, CA',
    },
    emailNotifications: true,
    notificationFrequency: 'Daily' as const,
    isActive: true,
    createdAt: '2024-01-01T00:00:00Z',
    lastExecutedAt: '2024-01-15T00:00:00Z',
    resultsCount: 25,
  },
  {
    id: '2',
    name: 'Node.js Backend Work',
    description: 'Backend development with Node.js',
    searchCriteria: {
      query: 'Node.js',
      skillIds: ['skill3'],
    },
    emailNotifications: false,
    notificationFrequency: 'Weekly' as const,
    isActive: false,
    createdAt: '2024-01-10T00:00:00Z',
  },
]

describe('SavedSearches', () => {
  const mockUser = {
    id: 'user-123',
    userName: 'Test User',
    email: 'test@example.com',
    emailVerified: true,
    taxCompliant: true,
    status: 'Active' as const,
    roles: ['Freelancer'],
    permissions: []
  }

  beforeEach(() => {
    jest.clearAllMocks()
    mockConfirm.mockReturnValue(true)

    // Default: authenticated user
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

    // Default: successful fetch
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => mockSavedSearches,
    } as Response)
  })

  describe('Authentication State', () => {
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

      render(<SavedSearches />)

      expect(screen.getByText('Authentication Required')).toBeInTheDocument()
      expect(screen.getByText('Please log in to manage saved searches.')).toBeInTheDocument()
    })

    it('should load saved searches when authenticated', async () => {
      render(<SavedSearches />)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/project-search/saved-searches',
          expect.objectContaining({ credentials: 'include' })
        )
      })
    })
  })

  describe('Loading State', () => {
    it('should show loading spinner initially', () => {
      render(<SavedSearches />)

      expect(screen.getByText('Loading saved searches...')).toBeInTheDocument()
    })

    it('should hide loading spinner after data loads', async () => {
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.queryByText('Loading saved searches...')).not.toBeInTheDocument()
      })
    })
  })

  describe('Empty State', () => {
    it('should show empty state when no searches exist', async () => {
      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => [],
      } as Response)

      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('No saved searches')).toBeInTheDocument()
        expect(
          screen.getByText(/Create your first saved search to get notified/i)
        ).toBeInTheDocument()
      })
    })
  })

  describe('Saved Searches Display', () => {
    it('should display saved searches after loading', async () => {
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('React Developer Projects')).toBeInTheDocument()
        expect(screen.getByText('Node.js Backend Work')).toBeInTheDocument()
      })
    })

    it('should display search descriptions', async () => {
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('Looking for React development opportunities')).toBeInTheDocument()
        expect(screen.getByText('Backend development with Node.js')).toBeInTheDocument()
      })
    })

    it('should display formatted search criteria', async () => {
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText(/"React developer"/)).toBeInTheDocument()
        expect(screen.getByText(/2 skills/)).toBeInTheDocument()
        expect(screen.getByText(/\$1000-5000/)).toBeInTheDocument()
        expect(screen.getByText(/San Francisco, CA/)).toBeInTheDocument()
      })
    })

    it('should show inactive badge for inactive searches', async () => {
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('Inactive')).toBeInTheDocument()
      })
    })

    it('should show email notification badge with frequency', async () => {
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText(/📧 Daily/)).toBeInTheDocument()
      })
    })

    it('should display created date', async () => {
      render(<SavedSearches />)

      await waitFor(() => {
        const createdLabels = screen.getAllByText(/Created:/)
        expect(createdLabels.length).toBeGreaterThan(0)
      })
    })

    it('should display last executed date when available', async () => {
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText(/Last used:/)).toBeInTheDocument()
      })
    })

    it('should display results count when available', async () => {
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText(/25 projects/)).toBeInTheDocument()
      })
    })
  })

  describe('Create Saved Search Modal', () => {
    it('should open create modal when Save Current Search button clicked', async () => {
      const user = userEvent.setup()
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('React Developer Projects')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Save Current Search'))

      expect(screen.getByRole('heading', { name: 'Save Search' })).toBeInTheDocument()
      expect(screen.getByPlaceholderText('e.g., React Developer Projects')).toBeInTheDocument()
    })

    it('should close create modal when Cancel clicked', async () => {
      const user = userEvent.setup()
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('React Developer Projects')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Save Current Search'))
      expect(screen.getByRole('heading', { name: 'Save Search' })).toBeInTheDocument()

      const cancelButtons = screen.getAllByText('Cancel')
      await user.click(cancelButtons[0])

      await waitFor(() => {
        expect(screen.queryByRole('heading', { name: 'Save Search' })).not.toBeInTheDocument()
      })
    })

    it('should fill out create form fields', async () => {
      const user = userEvent.setup()
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('Save Current Search')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Save Current Search'))

      const nameInput = screen.getByPlaceholderText('e.g., React Developer Projects')
      await user.type(nameInput, 'My New Search')
      expect(nameInput).toHaveValue('My New Search')

      const descInput = screen.getByPlaceholderText('Brief description of this search...')
      await user.type(descInput, 'Test description')
      expect(descInput).toHaveValue('Test description')
    })

    it('should toggle email notifications checkbox', async () => {
      const user = userEvent.setup()
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('Save Current Search')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Save Current Search'))

      const checkbox = screen.getByRole('checkbox', {
        name: /email notifications/i,
      })
      await user.click(checkbox)

      expect(checkbox).toBeChecked()
    })

    it('should show notification frequency dropdown when email notifications enabled', async () => {
      const user = userEvent.setup()
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('Save Current Search')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Save Current Search'))

      const checkbox = screen.getByRole('checkbox', {
        name: /email notifications/i,
      })
      await user.click(checkbox)

      await waitFor(() => {
        expect(screen.getByDisplayValue('Daily digest')).toBeInTheDocument()
      })
    })

    it('should disable save button when name is empty', async () => {
      const user = userEvent.setup()
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('Save Current Search')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Save Current Search'))

      const saveButton = screen.getByRole('button', { name: /save search/i })
      expect(saveButton).toBeDisabled()
    })

    it('should create saved search when form submitted', async () => {
      const user = userEvent.setup()
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockSavedSearches,
      } as Response)
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockSavedSearches,
      } as Response)

      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('Save Current Search')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Save Current Search'))

      await user.type(screen.getByPlaceholderText('e.g., React Developer Projects'), 'Test Search')

      const saveButton = screen.getByRole('button', { name: /save search/i })
      await user.click(saveButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/project-search/saved-searches',
          expect.objectContaining({
            method: 'POST',
            body: expect.stringContaining('Test Search'),
          })
        )
      })
    })
  })

  describe('Edit Saved Search Modal', () => {
    it('should open edit modal when Edit button clicked', async () => {
      const user = userEvent.setup()
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('React Developer Projects')).toBeInTheDocument()
      })

      const editButtons = screen.getAllByText('Edit')
      await user.click(editButtons[0])

      expect(screen.getByText('Edit Saved Search')).toBeInTheDocument()
    })

    it('should pre-fill edit form with existing data', async () => {
      const user = userEvent.setup()
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('React Developer Projects')).toBeInTheDocument()
      })

      const editButtons = screen.getAllByText('Edit')
      await user.click(editButtons[0])

      await waitFor(() => {
        const nameInputs = screen.getAllByDisplayValue('React Developer Projects')
        expect(nameInputs.length).toBeGreaterThan(0)
      })
    })

    it('should update saved search when form submitted', async () => {
      const user = userEvent.setup()
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockSavedSearches,
      } as Response)
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockSavedSearches,
      } as Response)

      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('React Developer Projects')).toBeInTheDocument()
      })

      const editButtons = screen.getAllByText('Edit')
      await user.click(editButtons[0])

      await waitFor(() => {
        expect(screen.getByText('Edit Saved Search')).toBeInTheDocument()
      })

      const updateButton = screen.getByRole('button', { name: /update search/i })
      await user.click(updateButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/project-search/saved-searches/1',
          expect.objectContaining({
            method: 'PUT',
          })
        )
      })
    })
  })

  describe('Delete Saved Search', () => {
    it('should show confirmation dialog when Delete clicked', async () => {
      const user = userEvent.setup()
      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('React Developer Projects')).toBeInTheDocument()
      })

      const deleteButtons = screen.getAllByText('Delete')
      await user.click(deleteButtons[0])

      expect(mockConfirm).toHaveBeenCalledWith('Are you sure you want to delete this saved search?')
    })

    it('should delete search when confirmed', async () => {
      const user = userEvent.setup()
      mockConfirm.mockReturnValue(true)
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockSavedSearches,
      } as Response)
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockSavedSearches,
      } as Response)

      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('React Developer Projects')).toBeInTheDocument()
      })

      const deleteButtons = screen.getAllByText('Delete')
      await user.click(deleteButtons[0])

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/project-search/saved-searches/1',
          expect.objectContaining({
            method: 'DELETE',
          })
        )
      })
    })

    it('should not delete when cancelled', async () => {
      const user = userEvent.setup()
      mockConfirm.mockReturnValue(false)

      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('React Developer Projects')).toBeInTheDocument()
      })

      const deleteButtons = screen.getAllByText('Delete')
      await user.click(deleteButtons[0])

      // Should not call DELETE endpoint
      expect(mockFetch).not.toHaveBeenCalledWith(
        expect.anything(),
        expect.objectContaining({ method: 'DELETE' })
      )
    })
  })

  describe('Execute Search', () => {
    it('should call onExecuteSearch callback when Execute clicked', async () => {
      const user = userEvent.setup()
      const mockExecute = jest.fn()

      render(<SavedSearches onExecuteSearch={mockExecute} />)

      await waitFor(() => {
        expect(screen.getByText('React Developer Projects')).toBeInTheDocument()
      })

      const executeButtons = screen.getAllByText('Execute')
      await user.click(executeButtons[0])

      expect(mockExecute).toHaveBeenCalledWith(mockSavedSearches[0].searchCriteria)
    })

    it('should update lastExecutedAt when search executed', async () => {
      const user = userEvent.setup()
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockSavedSearches,
      } as Response)
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockSavedSearches,
      } as Response)

      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('React Developer Projects')).toBeInTheDocument()
      })

      const executeButtons = screen.getAllByText('Execute')
      await user.click(executeButtons[0])

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/project-search/saved-searches/1',
          expect.objectContaining({
            method: 'PUT',
            body: expect.stringContaining('lastExecutedAt'),
          })
        )
      })
    })
  })

  describe('Error Handling', () => {
    it('should show error when fetch fails', async () => {
      mockFetch.mockRejectedValue(new Error('Network error'))

      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('Failed to load saved searches')).toBeInTheDocument()
      })
    })

    it('should show error for 401 unauthorized', async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 401,
      } as Response)

      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('Please log in to view saved searches')).toBeInTheDocument()
      })
    })

    it('should show error when create fails', async () => {
      const user = userEvent.setup()
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockSavedSearches,
      } as Response)
      mockFetch.mockResolvedValueOnce({
        ok: false,
      } as Response)

      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('Save Current Search')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Save Current Search'))
      await user.type(screen.getByPlaceholderText('e.g., React Developer Projects'), 'Test')

      const saveButton = screen.getByRole('button', { name: /save search/i })
      await user.click(saveButton)

      await waitFor(() => {
        expect(screen.getByText('Failed to save search')).toBeInTheDocument()
      })
    })
  })

  describe('Format Search Criteria', () => {
    it('should format criteria with all fields', async () => {
      render(<SavedSearches />)

      await waitFor(() => {
        const criteriaText = screen.getByText(/"React developer", 2 skills, \$1000-5000, San Francisco, CA/)
        expect(criteriaText).toBeInTheDocument()
      })
    })

    it('should show "All projects" when no criteria specified', async () => {
      const emptySearch = [{
        ...mockSavedSearches[0],
        id: '3',
        name: 'All Projects',
        searchCriteria: {},
      }]

      mockFetch.mockResolvedValue({
        ok: true,
        json: async () => emptySearch,
      } as Response)

      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('All projects')).toBeInTheDocument()
      })
    })
  })

  describe('Integration', () => {
    it('should render complete component without errors', async () => {
      const { container } = render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('React Developer Projects')).toBeInTheDocument()
      })

      expect(container.firstChild).toBeTruthy()
    })

    it('should handle full CRUD workflow', async () => {
      const user = userEvent.setup()

      // Initial load
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockSavedSearches,
      } as Response)

      render(<SavedSearches />)

      await waitFor(() => {
        expect(screen.getByText('React Developer Projects')).toBeInTheDocument()
      })

      // Create
      await user.click(screen.getByText('Save Current Search'))
      await user.type(screen.getByPlaceholderText('e.g., React Developer Projects'), 'New Search')

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockSavedSearches,
      } as Response)
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockSavedSearches,
      } as Response)

      await user.click(screen.getByRole('button', { name: /save search/i }))

      // Edit
      await waitFor(() => {
        expect(screen.queryByText('Save Search')).not.toBeInTheDocument()
      })

      const editButtons = screen.getAllByText('Edit')
      await user.click(editButtons[0])

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => mockSavedSearches,
      } as Response)

      await user.click(screen.getByRole('button', { name: /update search/i }))

      // Delete
      await waitFor(() => {
        expect(screen.queryByText('Edit Saved Search')).not.toBeInTheDocument()
      })

      mockConfirm.mockReturnValue(true)
      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => [],
      } as Response)

      const deleteButtons = screen.getAllByText('Delete')
      await user.click(deleteButtons[0])

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          expect.stringContaining('/api/project-search/saved-searches/'),
          expect.objectContaining({ method: 'DELETE' })
        )
      })
    })
  })
})
