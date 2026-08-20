import React from 'react'
import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ProjectCompletionForm from '../ProjectCompletionForm'

// Mock fetch globally
global.fetch = jest.fn()
const mockFetch = global.fetch as jest.Mock

describe('ProjectCompletionForm', () => {
  const defaultProps = {
    projectId: 'project-123',
    projectTitle: 'Build Mobile App',
    onSuccess: jest.fn(),
    onCancel: jest.fn(),
  }

  beforeEach(() => {
    jest.clearAllMocks()
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => ({ token: 'mock-csrf-token' }),
    } as Response)
  })

  // ============================================
  // Content Display (4 tests)
  // ============================================
  describe('Content Display', () => {
    it('should display form heading', () => {
      render(<ProjectCompletionForm {...defaultProps} />)

      expect(screen.getByText('Complete Project')).toBeInTheDocument()
    })

    it('should display project title', () => {
      render(<ProjectCompletionForm {...defaultProps} />)

      expect(screen.getByText('Build Mobile App')).toBeInTheDocument()
    })

    it('should display completion warning message', () => {
      render(<ProjectCompletionForm {...defaultProps} />)

      expect(screen.getByText(/Once you mark the project as complete, the provider will be notified/)).toBeInTheDocument()
    })

    it('should display instructions heading', () => {
      render(<ProjectCompletionForm {...defaultProps} />)

      expect(screen.getByText('Before completing this project:')).toBeInTheDocument()
    })
  })

  // ============================================
  // Form Elements (5 tests)
  // ============================================
  describe('Form Elements', () => {
    it('should display deliverables confirmation checkbox', () => {
      render(<ProjectCompletionForm {...defaultProps} />)

      expect(screen.getByLabelText(/All project deliverables have been completed and reviewed/)).toBeInTheDocument()
    })

    it('should display quality confirmation checkbox', () => {
      render(<ProjectCompletionForm {...defaultProps} />)

      expect(screen.getByLabelText(/The work meets the agreed-upon quality standards/)).toBeInTheDocument()
    })

    it('should display optional notes textarea', () => {
      render(<ProjectCompletionForm {...defaultProps} />)

      expect(screen.getByLabelText(/Completion Notes \(Optional\)/)).toBeInTheDocument()
      expect(screen.getByPlaceholderText(/Add any final notes about the project completion/)).toBeInTheDocument()
    })

    it('should display character counter for notes', () => {
      render(<ProjectCompletionForm {...defaultProps} />)

      expect(screen.getByText('0 / 500 characters')).toBeInTheDocument()
    })

    it('should display submit button', () => {
      render(<ProjectCompletionForm {...defaultProps} />)

      expect(screen.getByTestId('complete-project-button')).toBeInTheDocument()
      expect(screen.getByText('Mark as Complete')).toBeInTheDocument()
    })
  })

  // ============================================
  // Checkbox Functionality (5 tests)
  // ============================================
  describe('Checkbox Functionality', () => {
    it('should toggle deliverables checkbox when clicked', async () => {
      const user = userEvent.setup()
      render(<ProjectCompletionForm {...defaultProps} />)

      const checkbox = screen.getByLabelText(/All project deliverables have been completed and reviewed/)
      expect(checkbox).not.toBeChecked()

      await user.click(checkbox)
      expect(checkbox).toBeChecked()

      await user.click(checkbox)
      expect(checkbox).not.toBeChecked()
    })

    it('should toggle quality checkbox when clicked', async () => {
      const user = userEvent.setup()
      render(<ProjectCompletionForm {...defaultProps} />)

      const checkbox = screen.getByLabelText(/The work meets the agreed-upon quality standards/)
      expect(checkbox).not.toBeChecked()

      await user.click(checkbox)
      expect(checkbox).toBeChecked()

      await user.click(checkbox)
      expect(checkbox).not.toBeChecked()
    })

    it('should allow both checkboxes to be checked simultaneously', async () => {
      const user = userEvent.setup()
      render(<ProjectCompletionForm {...defaultProps} />)

      const deliverablesCheckbox = screen.getByLabelText(/All project deliverables have been completed and reviewed/)
      const qualityCheckbox = screen.getByLabelText(/The work meets the agreed-upon quality standards/)

      await user.click(deliverablesCheckbox)
      await user.click(qualityCheckbox)

      expect(deliverablesCheckbox).toBeChecked()
      expect(qualityCheckbox).toBeChecked()
    })

    it('should have submit button disabled when checkboxes are not checked', () => {
      render(<ProjectCompletionForm {...defaultProps} />)

      const submitButton = screen.getByTestId('complete-project-button')
      expect(submitButton).toBeDisabled()
    })

    it('should enable submit button when both checkboxes are checked', async () => {
      const user = userEvent.setup()
      render(<ProjectCompletionForm {...defaultProps} />)

      const deliverablesCheckbox = screen.getByLabelText(/All project deliverables have been completed and reviewed/)
      const qualityCheckbox = screen.getByLabelText(/The work meets the agreed-upon quality standards/)
      const submitButton = screen.getByTestId('complete-project-button')

      await user.click(deliverablesCheckbox)
      await user.click(qualityCheckbox)

      expect(submitButton).not.toBeDisabled()
    })
  })

  // ============================================
  // Notes Field (4 tests)
  // ============================================
  describe('Notes Field', () => {
    it('should update notes value when typing', async () => {
      const user = userEvent.setup()
      render(<ProjectCompletionForm {...defaultProps} />)

      const textarea = screen.getByPlaceholderText(/Add any final notes about the project completion/)
      await user.type(textarea, 'Project completed successfully')

      expect(textarea).toHaveValue('Project completed successfully')
    })

    it('should update character counter as user types', async () => {
      const user = userEvent.setup()
      render(<ProjectCompletionForm {...defaultProps} />)

      const textarea = screen.getByPlaceholderText(/Add any final notes about the project completion/)
      await user.type(textarea, 'Test note')

      expect(screen.getByText('9 / 500 characters')).toBeInTheDocument()
    })

    it('should enforce 500 character limit', () => {
      render(<ProjectCompletionForm {...defaultProps} />)

      const textarea = screen.getByPlaceholderText(/Add any final notes about the project completion/) as HTMLTextAreaElement
      expect(textarea.maxLength).toBe(500)
    })

    it('should handle large text input', () => {
      render(<ProjectCompletionForm {...defaultProps} />)

      const textarea = screen.getByPlaceholderText(/Add any final notes about the project completion/)
      const longText = 'a'.repeat(500)

      fireEvent.change(textarea, { target: { value: longText } })

      expect(screen.getByText('500 / 500 characters')).toBeInTheDocument()
    })
  })

  // ============================================
  // Form Submission (8 tests)
  // ============================================
  describe('Form Submission', () => {
    it('should prevent submission when checkboxes are not checked', () => {
      render(<ProjectCompletionForm {...defaultProps} />)

      const submitButton = screen.getByTestId('complete-project-button')

      // Button should be disabled, preventing submission
      expect(submitButton).toBeDisabled()
      expect(submitButton).toHaveClass('cursor-not-allowed')
    })

    it('should fetch CSRF token before submission', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({ success: true }),
        } as Response)
      })

      render(<ProjectCompletionForm {...defaultProps} />)

      const deliverablesCheckbox = screen.getByLabelText(/All project deliverables have been completed and reviewed/)
      const qualityCheckbox = screen.getByLabelText(/The work meets the agreed-upon quality standards/)

      await user.click(deliverablesCheckbox)
      await user.click(qualityCheckbox)

      const submitButton = screen.getByTestId('complete-project-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/auth/csrf-token', expect.any(Object))
      })
    })

    it('should call API with correct data when submitting', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        if (url.includes('/api/project/')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({ ok: false } as Response)
      })

      render(<ProjectCompletionForm {...defaultProps} />)

      const deliverablesCheckbox = screen.getByLabelText(/All project deliverables have been completed and reviewed/)
      const qualityCheckbox = screen.getByLabelText(/The work meets the agreed-upon quality standards/)
      const textarea = screen.getByPlaceholderText(/Add any final notes about the project completion/)

      await user.click(deliverablesCheckbox)
      await user.click(qualityCheckbox)
      await user.type(textarea, 'All done!')

      const submitButton = screen.getByTestId('complete-project-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/project/project-123/complete',
          expect.objectContaining({
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              'X-CSRF-TOKEN': 'mock-csrf-token',
            },
            credentials: 'include',
            body: JSON.stringify({ notes: 'All done!' }),
          })
        )
      })
    })

    it('should call onSuccess callback when submission succeeds', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        if (url.includes('/api/project/')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({ ok: false } as Response)
      })

      render(<ProjectCompletionForm {...defaultProps} />)

      const deliverablesCheckbox = screen.getByLabelText(/All project deliverables have been completed and reviewed/)
      const qualityCheckbox = screen.getByLabelText(/The work meets the agreed-upon quality standards/)

      await user.click(deliverablesCheckbox)
      await user.click(qualityCheckbox)

      const submitButton = screen.getByTestId('complete-project-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(defaultProps.onSuccess).toHaveBeenCalledTimes(1)
      }, { timeout: 3000 })
    })

    it('should show error message when API returns error', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        if (url.includes('/api/project/')) {
          return Promise.resolve({
            ok: false,
            json: async () => ({ message: 'Project already completed' }),
          } as Response)
        }
        return Promise.resolve({ ok: false } as Response)
      })

      render(<ProjectCompletionForm {...defaultProps} />)

      const deliverablesCheckbox = screen.getByLabelText(/All project deliverables have been completed and reviewed/)
      const qualityCheckbox = screen.getByLabelText(/The work meets the agreed-upon quality standards/)

      await user.click(deliverablesCheckbox)
      await user.click(qualityCheckbox)

      const submitButton = screen.getByTestId('complete-project-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Project already completed')).toBeInTheDocument()
      })
    })

    it('should show error when CSRF token fetch fails', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: false,
            json: async () => ({}),
          } as Response)
        }
        return Promise.resolve({ ok: false } as Response)
      })

      render(<ProjectCompletionForm {...defaultProps} />)

      const deliverablesCheckbox = screen.getByLabelText(/All project deliverables have been completed and reviewed/)
      const qualityCheckbox = screen.getByLabelText(/The work meets the agreed-upon quality standards/)

      await user.click(deliverablesCheckbox)
      await user.click(qualityCheckbox)

      const submitButton = screen.getByTestId('complete-project-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText(/Network error/)).toBeInTheDocument()
      })
    })

    it('should show error when network request fails', async () => {
      const user = userEvent.setup()
      mockFetch.mockRejectedValue(new Error('Network error'))

      render(<ProjectCompletionForm {...defaultProps} />)

      const deliverablesCheckbox = screen.getByLabelText(/All project deliverables have been completed and reviewed/)
      const qualityCheckbox = screen.getByLabelText(/The work meets the agreed-upon quality standards/)

      await user.click(deliverablesCheckbox)
      await user.click(qualityCheckbox)

      const submitButton = screen.getByTestId('complete-project-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText(/Network error. Please check your connection and try again./)).toBeInTheDocument()
      })
    })

    it('should omit notes from request when empty', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        if (url.includes('/api/project/')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({ ok: false } as Response)
      })

      render(<ProjectCompletionForm {...defaultProps} />)

      const deliverablesCheckbox = screen.getByLabelText(/All project deliverables have been completed and reviewed/)
      const qualityCheckbox = screen.getByLabelText(/The work meets the agreed-upon quality standards/)

      await user.click(deliverablesCheckbox)
      await user.click(qualityCheckbox)

      const submitButton = screen.getByTestId('complete-project-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/project/project-123/complete',
          expect.objectContaining({
            body: JSON.stringify({ notes: undefined }),
          })
        )
      })
    })
  })

  // ============================================
  // Loading State (3 tests)
  // ============================================
  describe('Loading State', () => {
    it('should show loading state when submitting', async () => {
      const user = userEvent.setup()
      let resolvePromise: (value: any) => void
      const delayedPromise = new Promise((resolve) => {
        resolvePromise = resolve
      })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        return delayedPromise as Promise<Response>
      })

      render(<ProjectCompletionForm {...defaultProps} />)

      const deliverablesCheckbox = screen.getByLabelText(/All project deliverables have been completed and reviewed/)
      const qualityCheckbox = screen.getByLabelText(/The work meets the agreed-upon quality standards/)

      await user.click(deliverablesCheckbox)
      await user.click(qualityCheckbox)

      const submitButton = screen.getByTestId('complete-project-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Processing...')).toBeInTheDocument()
      })

      // Clean up
      resolvePromise!({
        ok: true,
        json: async () => ({ success: true }),
      })
    })

    it('should disable checkboxes when loading', async () => {
      const user = userEvent.setup()
      let resolvePromise: (value: any) => void
      const delayedPromise = new Promise((resolve) => {
        resolvePromise = resolve
      })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        return delayedPromise as Promise<Response>
      })

      render(<ProjectCompletionForm {...defaultProps} />)

      const deliverablesCheckbox = screen.getByLabelText(/All project deliverables have been completed and reviewed/)
      const qualityCheckbox = screen.getByLabelText(/The work meets the agreed-upon quality standards/)

      await user.click(deliverablesCheckbox)
      await user.click(qualityCheckbox)

      const submitButton = screen.getByTestId('complete-project-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(deliverablesCheckbox).toBeDisabled()
        expect(qualityCheckbox).toBeDisabled()
      })

      // Clean up
      resolvePromise!({
        ok: true,
        json: async () => ({ success: true }),
      })
    })

    it('should disable textarea when loading', async () => {
      const user = userEvent.setup()
      let resolvePromise: (value: any) => void
      const delayedPromise = new Promise((resolve) => {
        resolvePromise = resolve
      })

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        return delayedPromise as Promise<Response>
      })

      render(<ProjectCompletionForm {...defaultProps} />)

      const deliverablesCheckbox = screen.getByLabelText(/All project deliverables have been completed and reviewed/)
      const qualityCheckbox = screen.getByLabelText(/The work meets the agreed-upon quality standards/)
      const textarea = screen.getByPlaceholderText(/Add any final notes about the project completion/)

      await user.click(deliverablesCheckbox)
      await user.click(qualityCheckbox)

      const submitButton = screen.getByTestId('complete-project-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(textarea).toBeDisabled()
      })

      // Clean up
      resolvePromise!({
        ok: true,
        json: async () => ({ success: true }),
      })
    })
  })

  // ============================================
  // Cancel Button (3 tests)
  // ============================================
  describe('Cancel Button', () => {
    it('should display cancel button when onCancel is provided', () => {
      render(<ProjectCompletionForm {...defaultProps} />)

      expect(screen.getByText('Cancel')).toBeInTheDocument()
    })

    it('should not display cancel button when onCancel is not provided', () => {
      const propsWithoutCancel = {
        projectId: 'project-123',
        projectTitle: 'Build Mobile App',
      }

      render(<ProjectCompletionForm {...propsWithoutCancel} />)

      expect(screen.queryByText('Cancel')).not.toBeInTheDocument()
    })

    it('should call onCancel when cancel button is clicked', async () => {
      const user = userEvent.setup()
      render(<ProjectCompletionForm {...defaultProps} />)

      const cancelButton = screen.getByText('Cancel')
      await user.click(cancelButton)

      expect(defaultProps.onCancel).toHaveBeenCalledTimes(1)
    })
  })

  // ============================================
  // Integration (2 tests)
  // ============================================
  describe('Integration', () => {
    it('should handle complete successful submission flow', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        if (url.includes('/api/project/')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true, message: 'Project marked as complete' }),
          } as Response)
        }
        return Promise.resolve({ ok: false } as Response)
      })

      render(<ProjectCompletionForm {...defaultProps} />)

      // Verify initial state
      expect(screen.getByText('Build Mobile App')).toBeInTheDocument()
      const submitButton = screen.getByTestId('complete-project-button')
      expect(submitButton).toBeDisabled()

      // Fill form
      const deliverablesCheckbox = screen.getByLabelText(/All project deliverables have been completed and reviewed/)
      const qualityCheckbox = screen.getByLabelText(/The work meets the agreed-upon quality standards/)
      const textarea = screen.getByPlaceholderText(/Add any final notes about the project completion/)

      await user.click(deliverablesCheckbox)
      await user.click(qualityCheckbox)
      await user.type(textarea, 'Excellent work!')

      // Verify button enabled
      expect(submitButton).not.toBeDisabled()

      // Submit
      await user.click(submitButton)

      // Verify API calls
      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/auth/csrf-token', expect.any(Object))
      })

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/project/project-123/complete',
          expect.objectContaining({
            method: 'POST',
            body: JSON.stringify({ notes: 'Excellent work!' }),
          })
        )
      })

      // Verify success callback
      await waitFor(() => {
        expect(defaultProps.onSuccess).toHaveBeenCalledTimes(1)
      }, { timeout: 3000 })
    })

    it('should handle complete error flow with recovery', async () => {
      const user = userEvent.setup()
      let attemptCount = 0

      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        if (url.includes('/api/project/')) {
          attemptCount++
          if (attemptCount === 1) {
            // First attempt fails
            return Promise.resolve({
              ok: false,
              json: async () => ({ message: 'Server error, please try again' }),
            } as Response)
          } else {
            // Second attempt succeeds
            return Promise.resolve({
              ok: true,
              json: async () => ({ success: true }),
            } as Response)
          }
        }
        return Promise.resolve({ ok: false } as Response)
      })

      render(<ProjectCompletionForm {...defaultProps} />)

      const deliverablesCheckbox = screen.getByLabelText(/All project deliverables have been completed and reviewed/)
      const qualityCheckbox = screen.getByLabelText(/The work meets the agreed-upon quality standards/)

      await user.click(deliverablesCheckbox)
      await user.click(qualityCheckbox)

      const submitButton = screen.getByTestId('complete-project-button')

      // First submission - fails
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Server error, please try again')).toBeInTheDocument()
      })

      // Second submission - succeeds
      await user.click(submitButton)

      await waitFor(() => {
        expect(defaultProps.onSuccess).toHaveBeenCalledTimes(1)
      }, { timeout: 3000 })
    })
  })
})
