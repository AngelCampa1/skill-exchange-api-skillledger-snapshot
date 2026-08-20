import React from 'react'
import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import ReviewForm from '../ReviewForm'

// Mock fetch globally
global.fetch = jest.fn()
const mockFetch = global.fetch as jest.Mock

describe('ReviewForm', () => {
  const defaultProps = {
    projectId: 'proj-123',
    projectTitle: 'Build E-commerce Website',
    providerName: 'John Doe',
    onSuccess: jest.fn(),
    onCancel: jest.fn(),
  }

  beforeEach(() => {
    jest.clearAllMocks()
    // Default CSRF token mock
    mockFetch.mockImplementation((url) => {
      if (url === '/api/auth/csrf-token') {
        return Promise.resolve({
          ok: true,
          json: async () => ({ token: 'mock-csrf-token' }),
        } as Response)
      }
      return Promise.resolve({
        ok: true,
        json: async () => ({}),
      } as Response)
    })
  })

  // ============================================
  // Content Display (3 tests)
  // ============================================
  describe('Content Display', () => {
    it('should display heading and project title', () => {
      render(<ReviewForm {...defaultProps} />)

      expect(screen.getByText('Leave a Review')).toBeInTheDocument()
      expect(screen.getByText('Build E-commerce Website')).toBeInTheDocument()
    })

    it('should display provider name when provided', () => {
      render(<ReviewForm {...defaultProps} />)

      expect(screen.getByText(/Provider: John Doe/)).toBeInTheDocument()
    })

    it('should not display provider line when providerName is undefined', () => {
      render(<ReviewForm {...defaultProps} providerName={undefined} />)

      expect(screen.queryByText(/Provider:/)).not.toBeInTheDocument()
    })
  })

  // ============================================
  // Star Rating (6 tests)
  // ============================================
  describe('Star Rating', () => {
    it('should display 5 star buttons', () => {
      render(<ReviewForm {...defaultProps} />)

      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })

      expect(starButtons).toHaveLength(5)
    })

    it('should show "Select rating" text initially', () => {
      render(<ReviewForm {...defaultProps} />)

      expect(screen.getByText('Select rating')).toBeInTheDocument()
    })

    it('should update rating text when star is clicked', async () => {
      const user = userEvent.setup()
      render(<ReviewForm {...defaultProps} />)

      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })

      await user.click(starButtons[2]) // Click 3rd star (3 stars)

      expect(screen.getByText('3 stars')).toBeInTheDocument()
    })

    it('should show singular "star" for rating of 1', async () => {
      const user = userEvent.setup()
      render(<ReviewForm {...defaultProps} />)

      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })

      await user.click(starButtons[0]) // Click 1st star

      expect(screen.getByText('1 star')).toBeInTheDocument()
    })

    it('should have disabled submit button when rating is not selected', async () => {
      const user = userEvent.setup()
      render(<ReviewForm {...defaultProps} />)

      const textarea = screen.getByTestId('review-text')
      await user.click(textarea)
      await user.paste('a'.repeat(100)) // Valid length

      await waitFor(() => {
        const submitButton = screen.getByTestId('submit-review-button')
        // Button is disabled when rating is 0, preventing submission
        expect(submitButton).toBeDisabled()
      })
    })

    it('should disable star buttons during submission', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        // Delay the review submission
        return new Promise((resolve) => {
          setTimeout(() => {
            resolve({
              ok: true,
              json: async () => ({}),
            } as Response)
          }, 100)
        })
      })

      render(<ReviewForm {...defaultProps} />)

      // Set valid rating and review
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4]) // 5 stars

      const textarea = screen.getByTestId('review-text')
      await user.click(textarea)
      await user.paste('a'.repeat(100))

      const submitButton = screen.getByTestId('submit-review-button')
      await user.click(submitButton)

      // Check stars are disabled during loading
      await waitFor(() => {
        expect(starButtons[0]).toBeDisabled()
      })
    })
  })

  // ============================================
  // Review Text Validation (9 tests)
  // ============================================
  describe('Review Text Validation', () => {
    it('should display character counter starting at 0', () => {
      render(<ReviewForm {...defaultProps} />)

      expect(screen.getByText(/0 \/ 1000 characters/)).toBeInTheDocument()
    })

    it('should update character count as user types', async () => {
      const user = userEvent.setup()
      render(<ReviewForm {...defaultProps} />)

      const textarea = screen.getByTestId('review-text')
      await user.type(textarea, 'Hello')

      await waitFor(() => {
        expect(screen.getByText(/5 \/ 1000 characters/)).toBeInTheDocument()
      })
    })

    it('should show minimum character hint when below 100', () => {
      render(<ReviewForm {...defaultProps} />)

      expect(screen.getByText(/\(minimum 100\)/)).toBeInTheDocument()
    })

    it('should have disabled submit button for review under 100 characters', async () => {
      const user = userEvent.setup()
      render(<ReviewForm {...defaultProps} />)

      // Set rating first
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4])

      const textarea = screen.getByTestId('review-text')
      await user.type(textarea, 'Too short')

      await waitFor(() => {
        const submitButton = screen.getByTestId('submit-review-button')
        // Button is disabled when text length is invalid
        expect(submitButton).toBeDisabled()
      })
    })

    it('should accept review with exactly 100 characters', async () => {
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
          json: async () => ({}),
        } as Response)
      })

      render(<ReviewForm {...defaultProps} />)

      // Set rating
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4])

      const textarea = screen.getByTestId('review-text')
      await user.click(textarea)
      await user.paste('a'.repeat(100)) // Exactly 100

      await waitFor(() => {
        expect(screen.getByText(/100 \/ 1000 characters/)).toBeInTheDocument()
        expect(screen.getByText('✓ Valid')).toBeInTheDocument()
      })

      const submitButton = screen.getByTestId('submit-review-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(defaultProps.onSuccess).toHaveBeenCalled()
      })
    })

    it('should have disabled submit button for review over 1000 characters', async () => {
      const user = userEvent.setup()
      render(<ReviewForm {...defaultProps} />)

      // Set rating
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4])

      const textarea = screen.getByTestId('review-text')
      // Use fireEvent for large text to avoid timeout
      fireEvent.change(textarea, { target: { value: 'a'.repeat(1001) } })

      await waitFor(() => {
        const submitButton = screen.getByTestId('submit-review-button')
        // Button is disabled when text exceeds 1000 chars
        expect(submitButton).toBeDisabled()
      })
    })

    it('should accept review with exactly 1000 characters', async () => {
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
          json: async () => ({}),
        } as Response)
      })

      render(<ReviewForm {...defaultProps} />)

      // Set rating
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4])

      const textarea = screen.getByTestId('review-text')
      // Use fireEvent for large text to avoid timeout
      fireEvent.change(textarea, { target: { value: 'a'.repeat(1000) } })

      await waitFor(() => {
        expect(screen.getByText(/1000 \/ 1000 characters/)).toBeInTheDocument()
        expect(screen.getByText('✓ Valid')).toBeInTheDocument()
      })

      const submitButton = screen.getByTestId('submit-review-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(defaultProps.onSuccess).toHaveBeenCalled()
      })
    })

    it('should show valid checkmark for text between 100-1000 chars', async () => {
      render(<ReviewForm {...defaultProps} />)

      const textarea = screen.getByTestId('review-text')
      // Use fireEvent to avoid timeout
      fireEvent.change(textarea, { target: { value: 'a'.repeat(500) } })

      await waitFor(() => {
        expect(screen.getByText('✓ Valid')).toBeInTheDocument()
      })
    })

    it('should disable textarea during submission', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        // Delay submission
        return new Promise((resolve) => {
          setTimeout(() => {
            resolve({
              ok: true,
              json: async () => ({}),
            } as Response)
          }, 100)
        })
      })

      render(<ReviewForm {...defaultProps} />)

      // Set valid rating and review
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4])

      const textarea = screen.getByTestId('review-text')
      await user.click(textarea)
      await user.paste('a'.repeat(100))

      const submitButton = screen.getByTestId('submit-review-button')
      await user.click(submitButton)

      // Textarea should be disabled during loading
      await waitFor(() => {
        expect(textarea).toBeDisabled()
      })
    })
  })

  // ============================================
  // Form Submission (7 tests)
  // ============================================
  describe('Form Submission', () => {
    it('should fetch CSRF token before submission', async () => {
      const user = userEvent.setup()
      render(<ReviewForm {...defaultProps} />)

      // Set valid rating and review
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4])

      const textarea = screen.getByTestId('review-text')
      await user.click(textarea)
      await user.paste('a'.repeat(100))

      const submitButton = screen.getByTestId('submit-review-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/auth/csrf-token', {
          credentials: 'include',
        })
      })
    })

    it('should call onSuccess callback on successful submission', async () => {
      const user = userEvent.setup()
      render(<ReviewForm {...defaultProps} />)

      // Set valid rating and review
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4])

      const textarea = screen.getByTestId('review-text')
      await user.click(textarea)
      await user.paste('a'.repeat(100))

      const submitButton = screen.getByTestId('submit-review-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(defaultProps.onSuccess).toHaveBeenCalled()
      })
    })

    it('should display error message on API failure', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        return Promise.resolve({
          ok: false,
          json: async () => ({ message: 'You have already reviewed this project' }),
        } as Response)
      })

      render(<ReviewForm {...defaultProps} />)

      // Set valid rating and review
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4])

      const textarea = screen.getByTestId('review-text')
      await user.click(textarea)
      await user.paste('a'.repeat(100))

      const submitButton = screen.getByTestId('submit-review-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('You have already reviewed this project')).toBeInTheDocument()
      })
    })

    it('should display network error on fetch failure', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        return Promise.reject(new Error('Network error'))
      })

      render(<ReviewForm {...defaultProps} />)

      // Set valid rating and review
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4])

      const textarea = screen.getByTestId('review-text')
      await user.click(textarea)
      await user.paste('a'.repeat(100))

      const submitButton = screen.getByTestId('submit-review-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Network error. Please check your connection and try again.')).toBeInTheDocument()
      })
    })

    it('should display error when CSRF token fetch fails', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: false,
            json: async () => ({}),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({}),
        } as Response)
      })

      render(<ReviewForm {...defaultProps} />)

      // Set valid rating and review
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4])

      const textarea = screen.getByTestId('review-text')
      fireEvent.change(textarea, { target: { value: 'a'.repeat(100) } })

      const submitButton = screen.getByTestId('submit-review-button')
      await user.click(submitButton)

      await waitFor(() => {
        // CSRF token failure throws error, caught as network error
        expect(screen.getByText(/Network error/)).toBeInTheDocument()
      })
    })

    it('should send correct review data to API', async () => {
      const user = userEvent.setup()
      render(<ReviewForm {...defaultProps} />)

      // Set 4 stars
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[3]) // 4 stars

      const textarea = screen.getByTestId('review-text')
      const reviewText = 'Excellent work! Very professional and delivered on time. Would definitely work with again and recommend to others.'
      fireEvent.change(textarea, { target: { value: reviewText } })

      const submitButton = screen.getByTestId('submit-review-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/review/project/proj-123',
          expect.objectContaining({
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              'X-CSRF-TOKEN': 'mock-csrf-token',
            },
            credentials: 'include',
            body: JSON.stringify({
              rating: 4,
              reviewText: reviewText,
            }),
          })
        )
      })
    })

    it('should not call onSuccess when submission fails', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        return Promise.resolve({
          ok: false,
          json: async () => ({ message: 'Error' }),
        } as Response)
      })

      render(<ReviewForm {...defaultProps} />)

      // Set valid rating and review
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4])

      const textarea = screen.getByTestId('review-text')
      await user.click(textarea)
      await user.paste('a'.repeat(100))

      const submitButton = screen.getByTestId('submit-review-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Error')).toBeInTheDocument()
      })

      expect(defaultProps.onSuccess).not.toHaveBeenCalled()
    })
  })

  // ============================================
  // Button States (5 tests)
  // ============================================
  describe('Button States', () => {
    it('should disable submit button when rating is not selected', () => {
      render(<ReviewForm {...defaultProps} />)

      const submitButton = screen.getByTestId('submit-review-button')
      expect(submitButton).toBeDisabled()
    })

    it('should disable submit button when review text is invalid', async () => {
      const user = userEvent.setup()
      render(<ReviewForm {...defaultProps} />)

      // Set rating but not text
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4])

      const submitButton = screen.getByTestId('submit-review-button')
      expect(submitButton).toBeDisabled()
    })

    it('should enable submit button when form is valid', async () => {
      const user = userEvent.setup()
      render(<ReviewForm {...defaultProps} />)

      // Set rating
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4])

      // Set valid text
      const textarea = screen.getByTestId('review-text')
      await user.click(textarea)
      await user.paste('a'.repeat(100))

      await waitFor(() => {
        const submitButton = screen.getByTestId('submit-review-button')
        expect(submitButton).not.toBeDisabled()
      })
    })

    it('should show loading text during submission', async () => {
      const user = userEvent.setup()
      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        // Delay submission
        return new Promise((resolve) => {
          setTimeout(() => {
            resolve({
              ok: true,
              json: async () => ({}),
            } as Response)
          }, 100)
        })
      })

      render(<ReviewForm {...defaultProps} />)

      // Set valid form
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4])

      const textarea = screen.getByTestId('review-text')
      await user.click(textarea)
      await user.paste('a'.repeat(100))

      const submitButton = screen.getByTestId('submit-review-button')
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText('Submitting...')).toBeInTheDocument()
      })
    })

    it('should call onCancel when cancel button is clicked', async () => {
      const user = userEvent.setup()
      render(<ReviewForm {...defaultProps} />)

      const cancelButton = screen.getByText('Cancel')
      await user.click(cancelButton)

      expect(defaultProps.onCancel).toHaveBeenCalledTimes(1)
    })
  })

  // ============================================
  // Cancel Button (2 tests)
  // ============================================
  describe('Cancel Button', () => {
    it('should display cancel button when onCancel is provided', () => {
      render(<ReviewForm {...defaultProps} />)

      expect(screen.getByText('Cancel')).toBeInTheDocument()
    })

    it('should not display cancel button when onCancel is undefined', () => {
      render(<ReviewForm {...defaultProps} onCancel={undefined} />)

      expect(screen.queryByText('Cancel')).not.toBeInTheDocument()
    })
  })

  // ============================================
  // Integration (2 tests)
  // ============================================
  describe('Integration', () => {
    it('should handle complete successful review submission flow', async () => {
      const user = userEvent.setup()
      // Explicit mock for this test
      mockFetch.mockClear()
      mockFetch.mockImplementation((url) => {
        if (url === '/api/auth/csrf-token') {
          return Promise.resolve({
            ok: true,
            json: async () => ({ token: 'mock-csrf-token' }),
          } as Response)
        }
        if (url.includes('/api/review/project/')) {
          return Promise.resolve({
            ok: true,
            json: async () => ({ success: true }),
          } as Response)
        }
        return Promise.resolve({
          ok: true,
          json: async () => ({}),
        } as Response)
      })

      render(<ReviewForm {...defaultProps} />)

      // Verify initial state
      expect(screen.getByText('Leave a Review')).toBeInTheDocument()
      expect(screen.getByText('Select rating')).toBeInTheDocument()
      expect(screen.getByText(/0 \/ 1000 characters/)).toBeInTheDocument()

      // Select 5 stars
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4])
      expect(screen.getByText('5 stars')).toBeInTheDocument()

      // Type valid review
      const textarea = screen.getByTestId('review-text')
      const reviewText = 'Outstanding provider! Delivered high-quality work ahead of schedule. Great communication throughout the project.'
      fireEvent.change(textarea, { target: { value: reviewText } })

      await waitFor(() => {
        expect(screen.getByText(`${reviewText.length} / 1000 characters`)).toBeInTheDocument()
        expect(screen.getByText('✓ Valid')).toBeInTheDocument()
      })

      // Submit form
      const submitButton = screen.getByTestId('submit-review-button')
      expect(submitButton).not.toBeDisabled()
      await user.click(submitButton)

      // Verify API calls and success callback
      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/auth/csrf-token', expect.any(Object))
      })

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/review/project/proj-123',
          expect.objectContaining({
            method: 'POST',
            body: JSON.stringify({
              rating: 5,
              reviewText: reviewText,
            }),
          })
        )
      })

      await waitFor(() => {
        expect(defaultProps.onSuccess).toHaveBeenCalled()
      }, { timeout: 3000 })
    })

    it('should enable submit button when validation issues are corrected', async () => {
      const user = userEvent.setup()
      render(<ReviewForm {...defaultProps} />)

      // Type valid text but no rating - button should be disabled
      const textarea = screen.getByTestId('review-text')
      fireEvent.change(textarea, { target: { value: 'a'.repeat(100) } })

      const submitButton = screen.getByTestId('submit-review-button')
      expect(submitButton).toBeDisabled()

      // Now select a rating - button should become enabled
      const starButtons = screen.getAllByRole('button').filter((btn) => {
        const svg = btn.querySelector('svg')
        return svg?.classList.contains('lucide-star')
      })
      await user.click(starButtons[4])

      await waitFor(() => {
        // Button should now be enabled
        expect(submitButton).not.toBeDisabled()
      })
    })
  })
})
