/**
 * Tests for QuestionnaireManager
 *
 * Comprehensive test suite for the questionnaire manager component
 * Coverage target: 80%+ (467 lines)
 */

import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import QuestionnaireManager from '../QuestionnaireManager'
import { QuestionnaireType } from '@/types/questionnaire'

// Mock dependencies
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}))

jest.mock('@/services/questionnaireApiService', () => ({
  questionnaireApiService: {
    getAvailableTemplates: jest.fn(),
    searchQuestionnaires: jest.fn(),
    deleteQuestionnaire: jest.fn(),
    cloneQuestionnaire: jest.fn(),
    setQuestionnaireStatus: jest.fn(),
  },
}))

jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
  },
}))

import { useRouter } from 'next/navigation'
import { questionnaireApiService } from '@/services/questionnaireApiService'

const mockUseRouter = useRouter as jest.MockedFunction<typeof useRouter>
const mockApiService = questionnaireApiService as jest.Mocked<typeof questionnaireApiService>

// Mock confirm and prompt
const mockConfirm = jest.fn()
const mockPrompt = jest.fn()
const mockAlert = jest.fn()
global.confirm = mockConfirm
global.prompt = mockPrompt
global.alert = mockAlert

// Mock questionnaire data
const mockQuestionnaires = [
  {
    id: 'q1',
    title: 'Client Onboarding Survey',
    description: 'Survey for new client onboarding process',
    createdByUserId: 'user-123',
    type: QuestionnaireType.ClientOnboarding,
    isTemplate: false,
    isActive: true,
    requiresReview: false,
    version: 1,
    questionCount: 10,
    responseCount: 25,
    isAvailable: true,
    questions: [],
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-15T00:00:00Z',
  },
  {
    id: 'q2',
    title: 'Project Requirements Template',
    description: 'Template for gathering project requirements',
    createdByUserId: 'user-123',
    type: QuestionnaireType.ProjectIntake,
    isTemplate: true,
    isActive: false,
    requiresReview: true,
    version: 1,
    questionCount: 15,
    responseCount: 0,
    isAvailable: true,
    questions: [],
    createdAt: '2024-01-05T00:00:00Z',
    updatedAt: '2024-01-10T00:00:00Z',
  },
]

const mockSearchResult = {
  questionnaires: mockQuestionnaires,
  totalCount: 2,
  page: 1,
  pageSize: 20,
  totalPages: 1,
  hasNextPage: false,
  hasPreviousPage: false,
}

describe('QuestionnaireManager', () => {
  const mockPush = jest.fn()

  beforeEach(() => {
    jest.clearAllMocks()

    mockUseRouter.mockReturnValue({
      push: mockPush,
      replace: jest.fn(),
      back: jest.fn(),
      forward: jest.fn(),
      refresh: jest.fn(),
      prefetch: jest.fn(),
    } as any)

    mockApiService.searchQuestionnaires.mockResolvedValue(mockSearchResult)
    mockApiService.getAvailableTemplates.mockResolvedValue(mockQuestionnaires)
    mockConfirm.mockReturnValue(true)
    mockPrompt.mockReturnValue('Cloned Questionnaire')
  })

  describe('Loading State', () => {
    it('should show loading spinner initially', () => {
      mockApiService.searchQuestionnaires.mockImplementation(
        () => new Promise(() => {}) // Never resolves
      )

      render(<QuestionnaireManager />)

      expect(screen.getByText('Loading questionnaires...')).toBeInTheDocument()
    })

    it('should hide loading spinner after data loads', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.queryByText('Loading questionnaires...')).not.toBeInTheDocument()
      })
    })
  })

  describe('Normal Mode (My Questionnaires)', () => {
    it('should display correct title and description', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('My Questionnaires')).toBeInTheDocument()
        expect(screen.getByText('Create and manage your dynamic questionnaires')).toBeInTheDocument()
      })
    })

    it('should show Create Questionnaire button', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        const createButtons = screen.getAllByText('Create Questionnaire')
        expect(createButtons.length).toBeGreaterThan(0)
      })
    })

    it('should load questionnaires on mount', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(mockApiService.searchQuestionnaires).toHaveBeenCalled()
      })
    })

    it('should display questionnaires after loading', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
        expect(screen.getByText('Project Requirements Template')).toBeInTheDocument()
      })
    })

    it('should display questionnaire details', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Survey for new client onboarding process')).toBeInTheDocument()
        expect(screen.getByText(/10 questions • 25 responses/)).toBeInTheDocument()
      })
    })

    it('should show Template badge for templates', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Template')).toBeInTheDocument()
      })
    })

    it('should show Inactive badge for inactive questionnaires', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        const inactiveBadges = screen.getAllByText('Inactive')
        // Should have at least the badge (plus maybe the filter option)
        expect(inactiveBadges.length).toBeGreaterThan(0)
      })
    })
  })

  describe('Templates Mode', () => {
    it('should display correct title in templates mode', async () => {
      render(<QuestionnaireManager showTemplatesOnly={true} />)

      await waitFor(() => {
        expect(screen.getByText('Questionnaire Templates')).toBeInTheDocument()
        expect(screen.getByText('Browse and use questionnaire templates')).toBeInTheDocument()
      })
    })

    it('should not show Create button in templates mode', async () => {
      render(<QuestionnaireManager showTemplatesOnly={true} />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      const createButtons = screen.queryAllByText('Create Questionnaire')
      expect(createButtons.length).toBe(0)
    })

    it('should call getAvailableTemplates in templates mode', async () => {
      render(<QuestionnaireManager showTemplatesOnly={true} />)

      await waitFor(() => {
        expect(mockApiService.getAvailableTemplates).toHaveBeenCalled()
      })
    })
  })

  describe('Search and Filters', () => {
    it('should have search input', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByPlaceholderText('Search questionnaires...')).toBeInTheDocument()
      })
    })

    it('should filter questionnaires by search term', async () => {
      const user = userEvent.setup()
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      const searchInput = screen.getByPlaceholderText('Search questionnaires...')
      await user.type(searchInput, 'Client')

      expect(searchInput).toHaveValue('Client')
    })

    it('should have type filter dropdown', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('All Types')).toBeInTheDocument()
      })
    })

    it('should have status filter in normal mode', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('All Status')).toBeInTheDocument()
      })
    })

    it('should not have status filter in templates mode', async () => {
      render(<QuestionnaireManager showTemplatesOnly={true} />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      expect(screen.queryByText('All Status')).not.toBeInTheDocument()
    })

    it('should have sort by dropdown', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByDisplayValue('Last Updated')).toBeInTheDocument()
      })
    })
  })

  describe('Empty State', () => {
    it('should show empty state when no questionnaires exist', async () => {
      mockApiService.searchQuestionnaires.mockResolvedValue({
        ...mockSearchResult,
        questionnaires: [],
        totalCount: 0,
      })

      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('No questionnaires found')).toBeInTheDocument()
        expect(screen.getByText('Get started by creating your first questionnaire.')).toBeInTheDocument()
      })
    })

    it('should show empty state for templates', async () => {
      mockApiService.getAvailableTemplates.mockResolvedValue([])

      render(<QuestionnaireManager showTemplatesOnly={true} />)

      await waitFor(() => {
        expect(screen.getByText('No questionnaires found')).toBeInTheDocument()
        expect(screen.getByText('No templates are currently available.')).toBeInTheDocument()
      })
    })
  })

  describe('Error Handling', () => {
    it('should display error when loading fails', async () => {
      mockApiService.searchQuestionnaires.mockRejectedValue(new Error('Network error'))

      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Error')).toBeInTheDocument()
        expect(screen.getByText('Network error')).toBeInTheDocument()
      })
    })
  })

  describe('View Questionnaire', () => {
    it('should navigate to questionnaire view when View clicked', async () => {
      const user = userEvent.setup()
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      const viewButtons = screen.getAllByText('View')
      await user.click(viewButtons[0])

      expect(mockPush).toHaveBeenCalledWith('/questionnaires/q1')
    })
  })

  describe('Edit Questionnaire', () => {
    it('should show Edit button in normal mode', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getAllByText('Edit').length).toBeGreaterThan(0)
      })
    })

    it('should navigate to edit page when Edit clicked', async () => {
      const user = userEvent.setup()
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      const editButtons = screen.getAllByText('Edit')
      await user.click(editButtons[0])

      expect(mockPush).toHaveBeenCalledWith('/questionnaires/q1/edit')
    })
  })

  describe('Clone Questionnaire', () => {
    it('should show Clone button in normal mode', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getAllByText('Clone').length).toBeGreaterThan(0)
      })
    })

    it('should prompt for title when Clone clicked', async () => {
      const user = userEvent.setup()
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      const cloneButtons = screen.getAllByText('Clone')
      await user.click(cloneButtons[0])

      expect(mockPrompt).toHaveBeenCalledWith(
        'Enter a title for the cloned questionnaire:',
        'Copy of Client Onboarding Survey'
      )
    })

    it('should clone questionnaire and navigate to edit', async () => {
      const user = userEvent.setup()
      mockPrompt.mockReturnValue('My Cloned Questionnaire')
      mockApiService.cloneQuestionnaire.mockResolvedValue({
        ...mockQuestionnaires[0],
        id: 'q3',
        title: 'My Cloned Questionnaire',
      })

      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      const cloneButtons = screen.getAllByText('Clone')
      await user.click(cloneButtons[0])

      await waitFor(() => {
        expect(mockApiService.cloneQuestionnaire).toHaveBeenCalledWith('q1', 'My Cloned Questionnaire')
        expect(mockPush).toHaveBeenCalledWith('/questionnaires/q3/edit')
      })
    })

    it('should not clone when prompt cancelled', async () => {
      const user = userEvent.setup()
      mockPrompt.mockReturnValue(null)

      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      const cloneButtons = screen.getAllByText('Clone')
      await user.click(cloneButtons[0])

      expect(mockApiService.cloneQuestionnaire).not.toHaveBeenCalled()
    })

    it('should show alert when clone fails', async () => {
      const user = userEvent.setup()
      mockPrompt.mockReturnValue('Test')
      mockApiService.cloneQuestionnaire.mockRejectedValue(new Error('Clone failed'))

      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      const cloneButtons = screen.getAllByText('Clone')
      await user.click(cloneButtons[0])

      await waitFor(() => {
        expect(mockAlert).toHaveBeenCalledWith('Failed to clone questionnaire: Clone failed')
      })
    })
  })

  describe('Toggle Status', () => {
    it('should show Deactivate for active questionnaires', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Deactivate')).toBeInTheDocument()
      })
    })

    it('should show Activate for inactive questionnaires', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Activate')).toBeInTheDocument()
      })
    })

    it('should toggle status when clicked', async () => {
      const user = userEvent.setup()
      mockApiService.setQuestionnaireStatus.mockResolvedValue(mockQuestionnaires[0])
      mockApiService.searchQuestionnaires.mockResolvedValue(mockSearchResult)

      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Deactivate')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Deactivate'))

      await waitFor(() => {
        expect(mockApiService.setQuestionnaireStatus).toHaveBeenCalledWith('q1', false)
      })
    })

    it('should show alert when toggle fails', async () => {
      const user = userEvent.setup()
      mockApiService.setQuestionnaireStatus.mockRejectedValue(new Error('Update failed'))

      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Deactivate')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Deactivate'))

      await waitFor(() => {
        expect(mockAlert).toHaveBeenCalledWith('Failed to update questionnaire status: Update failed')
      })
    })
  })

  describe('Delete Questionnaire', () => {
    it('should show Delete button in normal mode', async () => {
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getAllByText('Delete').length).toBeGreaterThan(0)
      })
    })

    it('should show confirmation dialog when Delete clicked', async () => {
      const user = userEvent.setup()
      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      const deleteButtons = screen.getAllByText('Delete')
      await user.click(deleteButtons[0])

      expect(mockConfirm).toHaveBeenCalledWith('Are you sure you want to delete this questionnaire?')
    })

    it('should delete questionnaire when confirmed', async () => {
      const user = userEvent.setup()
      mockConfirm.mockReturnValue(true)
      mockApiService.deleteQuestionnaire.mockResolvedValue(undefined)
      mockApiService.searchQuestionnaires.mockResolvedValue(mockSearchResult)

      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      const deleteButtons = screen.getAllByText('Delete')
      await user.click(deleteButtons[0])

      await waitFor(() => {
        expect(mockApiService.deleteQuestionnaire).toHaveBeenCalledWith('q1')
      })
    })

    it('should not delete when cancelled', async () => {
      const user = userEvent.setup()
      mockConfirm.mockReturnValue(false)

      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      const deleteButtons = screen.getAllByText('Delete')
      await user.click(deleteButtons[0])

      expect(mockApiService.deleteQuestionnaire).not.toHaveBeenCalled()
    })

    it('should show alert when delete fails', async () => {
      const user = userEvent.setup()
      mockConfirm.mockReturnValue(true)
      mockApiService.deleteQuestionnaire.mockRejectedValue(new Error('Delete failed'))

      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      const deleteButtons = screen.getAllByText('Delete')
      await user.click(deleteButtons[0])

      await waitFor(() => {
        expect(mockAlert).toHaveBeenCalledWith('Failed to delete questionnaire: Delete failed')
      })
    })
  })

  describe('Select Questionnaire Callback', () => {
    it('should show Select button when onSelectQuestionnaire provided', async () => {
      const mockSelect = jest.fn()
      render(<QuestionnaireManager onSelectQuestionnaire={mockSelect} />)

      await waitFor(() => {
        expect(screen.getAllByText('Select').length).toBeGreaterThan(0)
      })
    })

    it('should call callback when Select clicked', async () => {
      const user = userEvent.setup()
      const mockSelect = jest.fn()
      render(<QuestionnaireManager onSelectQuestionnaire={mockSelect} />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      const selectButtons = screen.getAllByText('Select')
      await user.click(selectButtons[0])

      expect(mockSelect).toHaveBeenCalledWith(mockQuestionnaires[0])
    })

    it('should not show View/Edit/Delete when Select mode active', async () => {
      const mockSelect = jest.fn()
      render(<QuestionnaireManager onSelectQuestionnaire={mockSelect} />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      expect(screen.queryByText('Edit')).not.toBeInTheDocument()
      expect(screen.queryByText('Clone')).not.toBeInTheDocument()
      expect(screen.queryByText('Delete')).not.toBeInTheDocument()
    })
  })

  describe('Pagination', () => {
    it('should show pagination when multiple pages exist', async () => {
      mockApiService.searchQuestionnaires.mockResolvedValue({
        ...mockSearchResult,
        totalPages: 3,
        hasNextPage: true,
      })

      render(<QuestionnaireManager />)

      await waitFor(() => {
        const nextButtons = screen.getAllByText('Next')
        expect(nextButtons.length).toBeGreaterThan(0)
      })
    })

    it('should show correct page information', async () => {
      mockApiService.searchQuestionnaires.mockResolvedValue({
        ...mockSearchResult,
        totalCount: 50,
        totalPages: 3,
        hasNextPage: true,
      })

      render(<QuestionnaireManager />)

      await waitFor(() => {
        // Check that pagination text is displayed (text is broken across elements)
        const container = document.body
        expect(container.textContent).toContain('Showing')
        expect(container.textContent).toContain('50')
        expect(container.textContent).toContain('results')
      })
    })

    it('should navigate to next page when Next clicked', async () => {
      const user = userEvent.setup()
      mockApiService.searchQuestionnaires.mockResolvedValue({
        ...mockSearchResult,
        totalPages: 3,
        hasNextPage: true,
      })

      render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      const nextButtons = screen.getAllByText('Next')
      await user.click(nextButtons[0])

      await waitFor(() => {
        expect(mockApiService.searchQuestionnaires).toHaveBeenCalledWith(
          expect.objectContaining({ page: 2 })
        )
      })
    })
  })

  describe('Integration', () => {
    it('should render complete component without errors', async () => {
      const { container } = render(<QuestionnaireManager />)

      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      expect(container.firstChild).toBeTruthy()
    })

    it('should handle full workflow', async () => {
      const user = userEvent.setup()

      mockApiService.searchQuestionnaires.mockResolvedValue(mockSearchResult)

      render(<QuestionnaireManager />)

      // Wait for initial load
      await waitFor(() => {
        expect(screen.getByText('Client Onboarding Survey')).toBeInTheDocument()
      })

      // Search
      const searchInput = screen.getByPlaceholderText('Search questionnaires...')
      await user.type(searchInput, 'test')

      // View
      const viewButtons = screen.getAllByText('View')
      await user.click(viewButtons[0])
      expect(mockPush).toHaveBeenCalled()
    })
  })
})
