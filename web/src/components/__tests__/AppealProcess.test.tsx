/**
 * Tests for AppealProcess
 *
 * Comprehensive test suite for the appeal process component
 * Coverage target: 80%+ (340 lines)
 */

import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import AppealProcess from '../AppealProcess'

// Mock logger
jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
  },
}))

// Mock UI components
jest.mock('../ui/card', () => ({
  Card: ({ children, className }: any) => <div className={className}>{children}</div>,
  CardHeader: ({ children }: any) => <div>{children}</div>,
  CardTitle: ({ children, className }: any) => <h3 className={className}>{children}</h3>,
  CardContent: ({ children, className }: any) => <div className={className}>{children}</div>,
}))

jest.mock('../ui/button', () => ({
  Button: ({ children, onClick, disabled, variant }: any) => (
    <button onClick={onClick} disabled={disabled} data-variant={variant}>
      {children}
    </button>
  ),
}))

describe('AppealProcess', () => {
  let mockFetch: jest.MockedFunction<typeof fetch>

  const mockAppeals = [
    {
      id: 'appeal-1',
      sanctionId: 'sanction-1',
      sanctionType: 'AccountWarning',
      reason: 'Inappropriate content in profile',
      appealText: 'I believe this was a misunderstanding. The content was professional.',
      status: 'Pending' as const,
      submittedAt: '2024-01-15T10:00:00Z',
    },
    {
      id: 'appeal-2',
      sanctionId: 'sanction-2',
      sanctionType: 'TemporarySuspension',
      reason: 'Multiple policy violations',
      appealText: 'I have reviewed the policies and understand my mistakes.',
      status: 'UnderReview' as const,
      submittedAt: '2024-01-10T10:00:00Z',
    },
    {
      id: 'appeal-3',
      sanctionId: 'sanction-3',
      sanctionType: 'ContentRemoval',
      reason: 'Spam content',
      appealText: 'This was not spam, it was legitimate project information.',
      status: 'Approved' as const,
      submittedAt: '2024-01-05T10:00:00Z',
      reviewedAt: '2024-01-08T14:30:00Z',
      reviewNotes: 'After review, we agree this was not spam. Penalty reversed.',
      reviewedBy: 'Admin Team',
    },
    {
      id: 'appeal-4',
      sanctionId: 'sanction-4',
      sanctionType: 'RatingRestriction',
      reason: 'Gaming the rating system',
      appealText: 'I did not intentionally game the system.',
      status: 'Rejected' as const,
      submittedAt: '2024-01-01T10:00:00Z',
      reviewedAt: '2024-01-03T16:00:00Z',
      reviewNotes: 'Evidence shows clear pattern of rating manipulation. Appeal denied.',
      reviewedBy: 'Review Team',
    },
  ]

  const mockSanctions = [
    {
      id: 'sanction-5',
      sanctionType: 'ProfileRestriction',
      reason: 'Unverified credentials',
      severity: 2,
      issuedAt: '2024-01-20T10:00:00Z',
    },
    {
      id: 'sanction-6',
      sanctionType: 'MessageRestriction',
      reason: 'Spam messages',
      severity: 3,
      issuedAt: '2024-01-18T10:00:00Z',
    },
  ]

  beforeEach(() => {
    jest.clearAllMocks()

    // Mock global fetch
    mockFetch = jest.fn()
    global.fetch = mockFetch

    // Default successful responses
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: async () => mockAppeals,
      } as Response)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => mockSanctions,
      } as Response)
  })

  afterEach(() => {
    jest.clearAllMocks()
  })

  describe('Initial Loading', () => {
    it('should show loading state initially', () => {
      render(<AppealProcess />)

      const loadingElement = document.querySelector('.animate-pulse')
      expect(loadingElement).toBeTruthy()
    })

    it('should fetch appeals on mount', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/user/appeals')
      })
    })

    it('should fetch available sanctions on mount', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/user/penalties/sanctions?appealable=true')
      })
    })
  })

  describe('Page Content', () => {
    it('should display page title and description', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText('Appeal Process')).toBeInTheDocument()
        expect(screen.getByText('Submit appeals for account penalties and track their progress')).toBeInTheDocument()
      })
    })

    it('should display appeal guidelines', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText('Appeal Guidelines')).toBeInTheDocument()
        expect(screen.getByText('Before submitting an appeal:')).toBeInTheDocument()
        expect(screen.getByText('What to include:')).toBeInTheDocument()
      })
    })

    it('should display guideline items', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText(/Review our platform policies/)).toBeInTheDocument()
        expect(screen.getByText(/Gather any evidence/)).toBeInTheDocument()
        expect(screen.getByText(/Appeals are reviewed within 5-7 business days/)).toBeInTheDocument()
      })
    })
  })

  describe('New Appeal Form', () => {
    it('should show New Appeal button when sanctions are available', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText('New Appeal')).toBeInTheDocument()
      })
    })

    it('should toggle form when clicking New Appeal button', async () => {
      const user = userEvent.setup()
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText('New Appeal')).toBeInTheDocument()
      })

      const newAppealButton = screen.getByText('New Appeal')
      await user.click(newAppealButton)

      expect(screen.getByText('Select Penalty to Appeal')).toBeInTheDocument()
      expect(screen.getByText('Appeal Statement *')).toBeInTheDocument()
    })

    it('should show Cancel button when form is open', async () => {
      const user = userEvent.setup()
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText('New Appeal')).toBeInTheDocument()
      })

      const newAppealButton = screen.getByText('New Appeal')
      await user.click(newAppealButton)

      const cancelButtons = screen.getAllByText('Cancel')
      expect(cancelButtons.length).toBeGreaterThan(0)
    })

    it('should close form when clicking Cancel in header', async () => {
      const user = userEvent.setup()
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText('New Appeal')).toBeInTheDocument()
      })

      const newAppealButton = screen.getByText('New Appeal')
      await user.click(newAppealButton)

      expect(screen.getByText('Select Penalty to Appeal')).toBeInTheDocument()

      // Click the header Cancel button (the one that says "Cancel" instead of "New Appeal")
      await user.click(newAppealButton) // Now it shows "Cancel"

      expect(screen.queryByText('Select Penalty to Appeal')).not.toBeInTheDocument()
    })

    it('should display available sanctions in dropdown', async () => {
      const user = userEvent.setup()
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText('New Appeal')).toBeInTheDocument()
      })

      const newAppealButton = screen.getByText('New Appeal')
      await user.click(newAppealButton)

      expect(screen.getByText(/Profile Restriction - Unverified credentials/)).toBeInTheDocument()
      expect(screen.getByText(/Message Restriction - Spam messages/)).toBeInTheDocument()
    })

    it('should update appeal text when typing in textarea', async () => {
      const user = userEvent.setup()
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText('New Appeal')).toBeInTheDocument()
      })

      const newAppealButton = screen.getByText('New Appeal')
      await user.click(newAppealButton)

      const textarea = screen.getByPlaceholderText(/Please explain why you believe/)
      await user.type(textarea, 'My appeal statement')

      expect(textarea).toHaveValue('My appeal statement')
    })

    it('should show character count for appeal text', async () => {
      const user = userEvent.setup()
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText('New Appeal')).toBeInTheDocument()
      })

      const newAppealButton = screen.getByText('New Appeal')
      await user.click(newAppealButton)

      expect(screen.getByText('0/2000 characters')).toBeInTheDocument()

      const textarea = screen.getByPlaceholderText(/Please explain why you believe/)
      await user.type(textarea, 'Test')

      expect(screen.getByText('4/2000 characters')).toBeInTheDocument()
    })

    it('should disable submit button when form is incomplete', async () => {
      const user = userEvent.setup()
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText('New Appeal')).toBeInTheDocument()
      })

      const newAppealButton = screen.getByText('New Appeal')
      await user.click(newAppealButton)

      const submitButton = screen.getByText('Submit Appeal')
      expect(submitButton).toBeDisabled()
    })

    it('should enable submit button when form is complete', async () => {
      const user = userEvent.setup()
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText('New Appeal')).toBeInTheDocument()
      })

      const newAppealButton = screen.getByText('New Appeal')
      await user.click(newAppealButton)

      const select = screen.getByRole('combobox')
      await user.selectOptions(select, 'sanction-5')

      const textarea = screen.getByPlaceholderText(/Please explain why you believe/)
      await user.type(textarea, 'My appeal statement')

      const submitButton = screen.getByText('Submit Appeal')
      expect(submitButton).not.toBeDisabled()
    })

    it('should submit appeal when clicking Submit Appeal', async () => {
      const user = userEvent.setup()

      // Setup mocks for initial load
      mockFetch.mockClear()
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockAppeals,
        } as Response)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockSanctions,
        } as Response)
        // Mock for submit
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({}),
        } as Response)
        // Mocks for reload after submit
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockAppeals,
        } as Response)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockSanctions,
        } as Response)

      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText('New Appeal')).toBeInTheDocument()
      })

      const newAppealButton = screen.getByText('New Appeal')
      await user.click(newAppealButton)

      const select = screen.getByRole('combobox')
      await user.selectOptions(select, 'sanction-5')

      const textarea = screen.getByPlaceholderText(/Please explain why you believe/)
      await user.type(textarea, 'My appeal statement')

      const submitButton = screen.getByText('Submit Appeal')
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/user/appeals', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            sanctionId: 'sanction-5',
            appealText: 'My appeal statement',
            supportingEvidence: [],
          }),
        })
      })
    })

    it('should show submitting state while submitting', async () => {
      const user = userEvent.setup()

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockAppeals,
        } as Response)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockSanctions,
        } as Response)
        .mockImplementationOnce(() => new Promise(resolve => setTimeout(resolve, 1000)))

      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText('New Appeal')).toBeInTheDocument()
      })

      const newAppealButton = screen.getByText('New Appeal')
      await user.click(newAppealButton)

      const select = screen.getByRole('combobox')
      await user.selectOptions(select, 'sanction-5')

      const textarea = screen.getByPlaceholderText(/Please explain why you believe/)
      await user.type(textarea, 'My appeal statement')

      const submitButton = screen.getByText('Submit Appeal')
      await user.click(submitButton)

      expect(screen.getByText('Submitting...')).toBeInTheDocument()
    })
  })

  describe('Appeals List', () => {
    it('should display existing appeals', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText('Your Appeals')).toBeInTheDocument()
        expect(screen.getByText(/Account Warning Appeal/)).toBeInTheDocument()
        expect(screen.getByText(/Temporary Suspension Appeal/)).toBeInTheDocument()
      })
    })

    it('should display empty state when no appeals', async () => {
      mockFetch.mockReset()
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => [],
        } as Response)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockSanctions,
        } as Response)

      render(<AppealProcess />)

      // Wait for page to load
      await waitFor(() => {
        expect(screen.getByText('Your Appeals')).toBeInTheDocument()
      })

      // Check for empty state
      await waitFor(() => {
        expect(screen.getByText('No appeals submitted yet')).toBeInTheDocument()
      })
    })

    it('should display appeal details', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        const penaltyReasons = screen.getAllByText('Original Penalty Reason:')
        expect(penaltyReasons.length).toBeGreaterThan(0)
        expect(screen.getByText('Inappropriate content in profile')).toBeInTheDocument()
        const yourAppeals = screen.getAllByText('Your Appeal:')
        expect(yourAppeals.length).toBeGreaterThan(0)
      })
    })

    it('should display appeal submission dates', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        const dates = screen.getAllByText(/Submitted/)
        expect(dates.length).toBeGreaterThan(0)
      })
    })
  })

  describe('Appeal Status Display', () => {
    it('should display Pending status with icon', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText(/⏳ Pending/)).toBeInTheDocument()
      })
    })

    it('should display UnderReview status with icon', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText(/👀 UnderReview/)).toBeInTheDocument()
      })
    })

    it('should display Approved status with icon', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText(/✅ Approved/)).toBeInTheDocument()
      })
    })

    it('should display Rejected status with icon', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText(/❌ Rejected/)).toBeInTheDocument()
      })
    })

    it('should show pending message for pending appeals', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText(/Your appeal is in queue for review/)).toBeInTheDocument()
      })
    })

    it('should show under review message for appeals under review', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText(/Your appeal is currently under review/)).toBeInTheDocument()
      })
    })

    it('should show approved message for approved appeals', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText(/Your appeal has been approved/)).toBeInTheDocument()
      })
    })

    it('should show rejected message for rejected appeals', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText(/Your appeal was not approved/)).toBeInTheDocument()
      })
    })

    it('should display review notes for reviewed appeals', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        const reviewDecisions = screen.getAllByText('Review Decision:')
        expect(reviewDecisions.length).toBeGreaterThan(0)
        expect(screen.getByText('After review, we agree this was not spam. Penalty reversed.')).toBeInTheDocument()
        expect(screen.getByText('Evidence shows clear pattern of rating manipulation. Appeal denied.')).toBeInTheDocument()
      })
    })

    it('should display reviewer information', async () => {
      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText(/by Admin Team/)).toBeInTheDocument()
        expect(screen.getByText(/by Review Team/)).toBeInTheDocument()
      })
    })
  })

  describe('Error Handling', () => {
    it('should handle appeals fetch error gracefully', async () => {
      mockFetch.mockClear()
      mockFetch
        .mockRejectedValueOnce(new Error('Network error'))
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockSanctions,
        } as Response)

      render(<AppealProcess />)

      await waitFor(() => {
        // Should still render after error
        expect(screen.getByText('Appeal Process')).toBeInTheDocument()
      })
    })

    it('should handle sanctions fetch error gracefully', async () => {
      mockFetch.mockClear()
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockAppeals,
        } as Response)
        .mockRejectedValueOnce(new Error('Network error'))

      render(<AppealProcess />)

      await waitFor(() => {
        // Should still render after error
        expect(screen.getByText('Appeal Process')).toBeInTheDocument()
      })
    })

    it('should handle submit error gracefully', async () => {
      const user = userEvent.setup()

      mockFetch.mockReset()
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockAppeals,
        } as Response)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockSanctions,
        } as Response)
        .mockRejectedValueOnce(new Error('Submit failed'))

      render(<AppealProcess />)

      await waitFor(() => {
        expect(screen.getByText('New Appeal')).toBeInTheDocument()
      })

      const newAppealButton = screen.getByText('New Appeal')
      await user.click(newAppealButton)

      // Wait for form to open
      await waitFor(() => {
        expect(screen.getByText('Select Penalty to Appeal')).toBeInTheDocument()
      })

      const select = screen.getByRole('combobox')
      await user.selectOptions(select, 'sanction-5')

      const textarea = screen.getByPlaceholderText(/Please explain why you believe/)
      await user.type(textarea, 'My appeal statement')

      const submitButton = screen.getByText('Submit Appeal')
      await user.click(submitButton)

      // Form should still be there after error
      await waitFor(() => {
        expect(screen.getByText('Select Penalty to Appeal')).toBeInTheDocument()
      })
    })
  })

  describe('Integration', () => {
    it('should render complete component without errors', () => {
      const { container } = render(<AppealProcess />)

      expect(container.firstChild).toBeTruthy()
    })

    it('should handle full workflow', async () => {
      const user = userEvent.setup()

      mockFetch.mockClear()
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockAppeals,
        } as Response)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => mockSanctions,
        } as Response)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({}),
        } as Response)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => [...mockAppeals, { id: 'new-appeal' }],
        } as Response)
        .mockResolvedValueOnce({
          ok: true,
          json: async () => [],
        } as Response)

      render(<AppealProcess />)

      // Wait for initial load
      await waitFor(() => {
        expect(screen.getByText('Your Appeals')).toBeInTheDocument()
      })

      // Open form
      const newAppealButton = screen.getByText('New Appeal')
      await user.click(newAppealButton)

      // Fill form
      const select = screen.getByRole('combobox')
      await user.selectOptions(select, 'sanction-5')

      const textarea = screen.getByPlaceholderText(/Please explain why you believe/)
      await user.type(textarea, 'My appeal statement')

      // Submit
      const submitButton = screen.getByText('Submit Appeal')
      await user.click(submitButton)

      // Should reload appeals
      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledTimes(5)
      })
    })
  })
})
