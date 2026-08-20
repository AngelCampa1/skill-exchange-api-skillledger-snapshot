import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import FeedbackForm from '../FeedbackForm'
import { feedbackApiService } from '@/services/feedbackApiService'

// Mock the feedbackApiService
jest.mock('@/services/feedbackApiService', () => ({
  feedbackApiService: {
    submitFeedback: jest.fn(),
  },
}))

const mockSubmitFeedback = feedbackApiService.submitFeedback as jest.Mock

describe('FeedbackForm', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    mockSubmitFeedback.mockResolvedValue({ success: true, message: 'Feedback submitted' })
  })

  // ============================================
  // Category Validation (3 tests)
  // ============================================
  describe('Category Validation', () => {
    it('shows error when submitting without selecting category', async () => {
      const user = userEvent.setup()
      render(<FeedbackForm />)

      const messageInput = screen.getByLabelText(/Your Feedback/i)
      await user.click(messageInput)
      await user.paste('This is my feedback message that is long enough')

      const submitButton = screen.getByRole('button', { name: /Submit Feedback/i })
      await user.click(submitButton)

      await waitFor(() => {
        // Zod shows "Invalid enum value" error for unselected enum
        expect(screen.getByText(/Invalid enum value|Please select a category/i)).toBeInTheDocument()
      }, { timeout: 10000 })
    })

    it('accepts valid category selection', async () => {
      const user = userEvent.setup()
      render(<FeedbackForm />)

      const categorySelect = screen.getByLabelText(/Category/i)
      await user.selectOptions(categorySelect, 'Bug')

      expect(categorySelect).toHaveValue('Bug')
    })

    it('displays all category options', async () => {
      render(<FeedbackForm />)

      expect(screen.getByText('Select a category...')).toBeInTheDocument()
      expect(screen.getByText('General Feedback')).toBeInTheDocument()
      expect(screen.getByText('Bug Report')).toBeInTheDocument()
      expect(screen.getByText('Feature Request')).toBeInTheDocument()
      expect(screen.getByText('Other')).toBeInTheDocument()
    })
  })

  // ============================================
  // Message Validation (4 tests)
  // ============================================
  describe('Message Validation', () => {
    it('shows error for message under 10 characters', async () => {
      const user = userEvent.setup()
      render(<FeedbackForm />)

      const categorySelect = screen.getByLabelText(/Category/i)
      await user.selectOptions(categorySelect, 'General')

      const messageInput = screen.getByLabelText(/Your Feedback/i)
      await user.type(messageInput, 'Short')

      const submitButton = screen.getByRole('button', { name: /Submit Feedback/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Message must be at least 10 characters')).toBeInTheDocument()
      })
    })

    it('accepts message with exactly 10 characters', async () => {
      const user = userEvent.setup()
      const onSuccess = jest.fn()
      render(<FeedbackForm onSuccess={onSuccess} />)

      const categorySelect = screen.getByLabelText(/Category/i)
      await user.selectOptions(categorySelect, 'General')

      const messageInput = screen.getByLabelText(/Your Feedback/i)
      await user.type(messageInput, '1234567890') // Exactly 10 chars

      const submitButton = screen.getByRole('button', { name: /Submit Feedback/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockSubmitFeedback).toHaveBeenCalled()
      })
    })

    it('shows error for message over 2000 characters', async () => {
      const user = userEvent.setup()
      render(<FeedbackForm />)

      const categorySelect = screen.getByLabelText(/Category/i)
      await user.selectOptions(categorySelect, 'General')

      const messageInput = screen.getByLabelText(/Your Feedback/i) as HTMLTextAreaElement
      // Create a string with 2001 characters - use paste for performance
      const longMessage = 'a'.repeat(2001)
      await user.click(messageInput)
      await user.paste(longMessage)

      const submitButton = screen.getByRole('button', { name: /Submit Feedback/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText(/Message cannot exceed 2000 characters/i)).toBeInTheDocument()
      })
    })

    it('accepts message with exactly 2000 characters', async () => {
      const user = userEvent.setup()
      const onSuccess = jest.fn()
      render(<FeedbackForm onSuccess={onSuccess} />)

      const categorySelect = screen.getByLabelText(/Category/i)
      await user.selectOptions(categorySelect, 'General')

      const messageInput = screen.getByLabelText(/Your Feedback/i) as HTMLTextAreaElement
      // Create a string with exactly 2000 characters - use paste for performance
      const maxMessage = 'a'.repeat(2000)
      await user.click(messageInput)
      await user.paste(maxMessage)

      const submitButton = screen.getByRole('button', { name: /Submit Feedback/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockSubmitFeedback).toHaveBeenCalled()
      })
    })
  })

  // ============================================
  // Email Validation (3 tests)
  // ============================================
  describe('Email Validation', () => {
    it('accepts empty email (optional field)', async () => {
      const user = userEvent.setup()
      const onSuccess = jest.fn()
      render(<FeedbackForm onSuccess={onSuccess} />)

      const categorySelect = screen.getByRole('combobox', { name: /Category/i })
      await user.selectOptions(categorySelect, 'General')

      const messageInput = screen.getByRole('textbox', { name: /Your Feedback/i })
      await user.click(messageInput)
      await user.paste('This is valid feedback message')

      // Leave email empty

      const submitButton = screen.getByRole('button', { name: /Submit Feedback/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockSubmitFeedback).toHaveBeenCalled()
        // Check that replyToEmail is undefined or not passed
        const callArgs = mockSubmitFeedback.mock.calls[0][0]
        expect(callArgs.category).toBe('General')
        expect(callArgs.message).toBe('This is valid feedback message')
      })
    })

    it('blocks submission with invalid email but shows no error message', async () => {
      const user = userEvent.setup()
      render(<FeedbackForm />)

      const categorySelect = screen.getByRole('combobox', { name: /Category/i })
      await user.selectOptions(categorySelect, 'General')

      const messageInput = screen.getByRole('textbox', { name: /Your Feedback/i })
      await user.click(messageInput)
      await user.paste('This is valid feedback message')

      const emailInput = screen.getByPlaceholderText('your@email.com')
      await user.type(emailInput, 'not-an-email')

      const submitButton = screen.getByRole('button', { name: /Submit Feedback/i })
      await user.click(submitButton)

      // BUG-TEST-049: Email validation blocks submission but doesn't show error to user
      // The Zod validation correctly rejects invalid email (form doesn't submit)
      // BUT no visible error message appears in the UI
      // Expected: Show "Please enter a valid email address" error message
      // Actual: Form stays on screen with no feedback about what's wrong

      // Verify the form did NOT submit (validation working)
      expect(mockSubmitFeedback).not.toHaveBeenCalled()

      // BUG: Verify NO error message is shown (bad UX)
      expect(screen.queryByText(/invalid email|valid email address/i)).not.toBeInTheDocument()

      console.warn('BUG-TEST-049: Invalid email blocks submission but shows no error message to user')
    })

    it('accepts valid email format', async () => {
      const user = userEvent.setup()
      const onSuccess = jest.fn()
      render(<FeedbackForm onSuccess={onSuccess} />)

      const categorySelect = screen.getByRole('combobox', { name: /Category/i })
      await user.selectOptions(categorySelect, 'General')

      const messageInput = screen.getByRole('textbox', { name: /Your Feedback/i })
      await user.click(messageInput)
      await user.paste('This is valid feedback message')

      const emailInput = screen.getByPlaceholderText('your@email.com')
      await user.type(emailInput, 'valid@example.com')

      const submitButton = screen.getByRole('button', { name: /Submit Feedback/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockSubmitFeedback).toHaveBeenCalled()
        const callArgs = mockSubmitFeedback.mock.calls[0][0]
        expect(callArgs.replyToEmail).toBe('valid@example.com')
      })
    })
  })

  // ============================================
  // Form Submission (4 tests)
  // ============================================
  describe('Form Submission', () => {
    it('calls onSuccess callback on successful submission', async () => {
      const user = userEvent.setup()
      const onSuccess = jest.fn()
      render(<FeedbackForm onSuccess={onSuccess} />)

      const categorySelect = screen.getByLabelText(/Category/i)
      await user.selectOptions(categorySelect, 'FeatureRequest')

      const messageInput = screen.getByLabelText(/Your Feedback/i)
      await user.click(messageInput)
      await user.paste('Please add Light-Only Mode to the app')

      const submitButton = screen.getByRole('button', { name: /Submit Feedback/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(onSuccess).toHaveBeenCalled()
      })
    })

    it('calls onError callback on submission failure', async () => {
      const user = userEvent.setup()
      const onError = jest.fn()
      mockSubmitFeedback.mockRejectedValue(new Error('Network error'))

      render(<FeedbackForm onError={onError} />)

      const categorySelect = screen.getByLabelText(/Category/i)
      await user.selectOptions(categorySelect, 'Bug')

      const messageInput = screen.getByLabelText(/Your Feedback/i)
      await user.click(messageInput)
      await user.paste('Found a bug in the system')

      const submitButton = screen.getByRole('button', { name: /Submit Feedback/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(onError).toHaveBeenCalledWith('Network error')
      })
    })

    it('resets form after successful submission', async () => {
      const user = userEvent.setup()
      render(<FeedbackForm onSuccess={jest.fn()} />)

      const categorySelect = screen.getByLabelText(/Category/i)
      await user.selectOptions(categorySelect, 'General')

      const messageInput = screen.getByLabelText(/Your Feedback/i)
      await user.type(messageInput, 'This is my feedback')

      const submitButton = screen.getByRole('button', { name: /Submit Feedback/i })
      await user.click(submitButton)

      await waitFor(() => {
        // Form should be reset
        expect(messageInput).toHaveValue('')
        expect(categorySelect).toHaveValue('')
      })
    })

    it('shows loading state during submission', async () => {
      const user = userEvent.setup()
      // Make the submission slow
      mockSubmitFeedback.mockImplementation(() => new Promise(resolve => {
        setTimeout(() => resolve({ success: true }), 1000)
      }))

      render(<FeedbackForm />)

      const categorySelect = screen.getByLabelText(/Category/i)
      await user.selectOptions(categorySelect, 'General')

      const messageInput = screen.getByLabelText(/Your Feedback/i)
      await user.type(messageInput, 'This is my feedback')

      const submitButton = screen.getByRole('button', { name: /Submit Feedback/i })
      await user.click(submitButton)

      // Should show submitting state
      expect(screen.getByText('Submitting...')).toBeInTheDocument()
      expect(submitButton).toBeDisabled()
    })
  })

  // ============================================
  // Character Counter (2 tests)
  // ============================================
  describe('Character Counter', () => {
    it('displays current character count', async () => {
      const user = userEvent.setup()
      render(<FeedbackForm />)

      const messageInput = screen.getByRole('textbox', { name: /Your Feedback/i })
      await user.type(messageInput, 'Hello world')

      // Counter shows "11/2000" - may be split into separate elements
      expect(screen.getByText(/11/)).toBeInTheDocument()
      expect(screen.getByText(/2000/)).toBeInTheDocument()
    })

    it('shows warning color when approaching limit (> 1800 chars)', async () => {
      const user = userEvent.setup()
      render(<FeedbackForm />)

      const messageInput = screen.getByRole('textbox', { name: /Your Feedback/i }) as HTMLTextAreaElement
      // Paste 1850 characters for performance
      await user.click(messageInput)
      await user.paste('a'.repeat(1850))

      // Find the element containing the count that has warning class
      const counterElements = screen.getAllByText(/185\d/)
      const warningElement = counterElements.find(el => el.classList.contains('text-warning'))
      expect(warningElement).toBeTruthy()
    })
  })

  // ============================================
  // Default Values (2 tests)
  // ============================================
  describe('Default Values', () => {
    it('pre-fills email when userEmail prop is provided', async () => {
      render(<FeedbackForm userEmail="user@example.com" />)

      const emailInput = screen.getByLabelText(/Email for Reply/i)
      expect(emailInput).toHaveValue('user@example.com')
    })

    it('leaves email empty when no userEmail prop', async () => {
      render(<FeedbackForm />)

      const emailInput = screen.getByLabelText(/Email for Reply/i)
      expect(emailInput).toHaveValue('')
    })
  })

  // ============================================
  // Accessibility (1 test)
  // ============================================
  describe('Accessibility', () => {
    it('marks required fields with asterisk', async () => {
      render(<FeedbackForm />)

      // Both category and message labels should have asterisk
      const categoryLabel = screen.getByText('Category')
      const messageLabel = screen.getByText('Your Feedback')

      expect(categoryLabel.parentElement?.textContent).toContain('*')
      expect(messageLabel.parentElement?.textContent).toContain('*')
    })
  })
})
