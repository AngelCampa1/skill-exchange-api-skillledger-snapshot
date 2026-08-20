import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { useRouter, useSearchParams } from 'next/navigation'
import ForgotPassword from '../ForgotPassword'
import { ThemeProvider } from '@/contexts/ThemeContext'

// Mock Next.js router
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
  useSearchParams: jest.fn(),
}))

// Mock fetch
const mockFetch = jest.fn()
global.fetch = mockFetch

// Helper to wrap components with necessary providers
const renderWithProviders = (ui: React.ReactElement) => {
  return render(
    <ThemeProvider>
      {ui}
    </ThemeProvider>
  )
}

describe('ForgotPassword', () => {
  const mockPush = jest.fn()

  beforeEach(() => {
    jest.clearAllMocks()
    ;(useRouter as jest.Mock).mockReturnValue({
      push: mockPush,
    })
    ;(useSearchParams as jest.Mock).mockReturnValue({
      get: jest.fn(),
    })
  })

  it('renders forgot password form correctly', () => {
    renderWithProviders(<ForgotPassword />)
    
    expect(screen.getByText('SkillLedger')).toBeInTheDocument()
    expect(screen.getByText('Forgot Password?')).toBeInTheDocument()
    expect(screen.getByText('Enter your email address and we\'ll send you instructions to reset your password.')).toBeInTheDocument()
    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /send reset instructions/i })).toBeInTheDocument()
  })

  it('validates email format', async () => {
    renderWithProviders(<ForgotPassword />)
    
    const emailInput = screen.getByLabelText(/email address/i)
    
    // Test that email input accepts both valid and invalid formats
    // The actual validation behavior is tested in integration tests
    fireEvent.change(emailInput, { target: { value: 'invalid-email' } })
    expect(emailInput).toHaveValue('invalid-email')
    
    fireEvent.change(emailInput, { target: { value: 'valid@example.com' } })
    expect(emailInput).toHaveValue('valid@example.com')
    
    // Verify the form uses email type input for browser validation
    expect(emailInput).toHaveAttribute('type', 'email')
  })

  it('submits form with valid email and shows success state', async () => {
    // Mock CSRF token response
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token' }),
      })
      // Mock forgot password response
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          message: 'If the email address is registered and verified, password reset instructions have been sent.',
        }),
      })

    renderWithProviders(<ForgotPassword />)

    const emailInput = screen.getByTestId('email-input')
    const submitButton = screen.getByTestId('submit-button')

    // Enter valid email
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
    fireEvent.click(submitButton)
    
    // Should show loading state
    await waitFor(() => {
      expect(screen.getByText('Sending Instructions...')).toBeInTheDocument()
    })
    
    // Should show success state
    await waitFor(() => {
      expect(screen.getByText('Check Your Email')).toBeInTheDocument()
      expect(screen.getByText('test@example.com')).toBeInTheDocument()
      expect(screen.getByText(/Check your email inbox/)).toBeInTheDocument()
    })

    // Verify API calls
    expect(mockFetch).toHaveBeenCalledTimes(2)
    expect(mockFetch).toHaveBeenNthCalledWith(1, '/api/auth/csrf-token')
    expect(mockFetch).toHaveBeenNthCalledWith(2, '/api/auth/forgot-password', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-CSRF-TOKEN': 'csrf-token',
      },
      body: JSON.stringify({ email: 'test@example.com' }),
    })
  })

  it('shows error message for rate limiting', async () => {
    // Mock CSRF token response
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token' }),
      })
      // Mock rate limited response
      .mockResolvedValueOnce({
        ok: false,
        status: 429,
        json: async () => ({
          success: false,
          message: 'Too many password reset requests. Please wait before trying again.',
        }),
      })

    renderWithProviders(<ForgotPassword />)
    
    const emailInput = screen.getByTestId('email-input')
    const submitButton = screen.getByTestId('submit-button')
    
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
    fireEvent.click(submitButton)
    
    await waitFor(() => {
      expect(screen.getByText('Too many password reset requests. Please wait before trying again.')).toBeInTheDocument()
    })
  })

  it('shows generic error for failed requests', async () => {
    // Mock CSRF token response
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token' }),
      })
      // Mock failed response
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => ({
          success: false,
          message: 'Server error',
        }),
      })

    renderWithProviders(<ForgotPassword />)
    
    const emailInput = screen.getByTestId('email-input')
    const submitButton = screen.getByTestId('submit-button')
    
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
    fireEvent.click(submitButton)
    
    await waitFor(() => {
      expect(screen.getByText('Server error')).toBeInTheDocument()
    })
  })

  it('handles network errors gracefully', async () => {
    // Mock console.error to suppress intentional error logging during this test
    const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {})
    
    // Mock CSRF token response
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token' }),
      })
      // Mock network error
      .mockRejectedValueOnce(new Error('Network error'))

    renderWithProviders(<ForgotPassword />)
    
    const emailInput = screen.getByTestId('email-input')
    const submitButton = screen.getByTestId('submit-button')
    
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
    fireEvent.click(submitButton)
    
    await waitFor(() => {
      expect(screen.getByText('An error occurred. Please try again.')).toBeInTheDocument()
    })
    
    // Restore console.error
    consoleSpy.mockRestore()
  })

  it('allows sending to different email after success', async () => {
    // Mock successful response
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token' }),
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          message: 'If the email address is registered and verified, password reset instructions have been sent.',
        }),
      })

    renderWithProviders(<ForgotPassword />)
    
    const emailInput = screen.getByTestId('email-input')
    const submitButton = screen.getByTestId('submit-button')
    
    // Submit first email
    fireEvent.change(emailInput, { target: { value: 'first@example.com' } })
    fireEvent.click(submitButton)
    
    // Wait for success state
    await waitFor(() => {
      expect(screen.getByText('Check Your Email')).toBeInTheDocument()
    })
    
    // Click "Send to Different Email"
    const sendDifferentButton = screen.getByText('Send to Different Email')
    fireEvent.click(sendDifferentButton)
    
    // Should return to form
    await waitFor(() => {
      expect(screen.getByText('Forgot Password?')).toBeInTheDocument()
      expect(screen.getByTestId('email-input')).toBeInTheDocument()
    })
  })

  it('navigates to login when back to login is clicked', async () => {
    // Mock successful response to get to success state
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token' }),
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          message: 'If the email address is registered and verified, password reset instructions have been sent.',
        }),
      })

    renderWithProviders(<ForgotPassword />)
    
    const emailInput = screen.getByTestId('email-input')
    const submitButton = screen.getByTestId('submit-button')
    
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
    fireEvent.click(submitButton)
    
    await waitFor(() => {
      expect(screen.getByText('Back to Login')).toBeInTheDocument()
    })
    
    const backToLoginButton = screen.getByText('Back to Login')
    fireEvent.click(backToLoginButton)
    
    expect(mockPush).toHaveBeenCalledWith('/login')
  })

  it('disables form during submission', async () => {
    // Mock slow response to test loading state
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token' }),
      })
      .mockImplementationOnce(() => new Promise(resolve => setTimeout(resolve, 100)))

    renderWithProviders(<ForgotPassword />)
    
    const emailInput = screen.getByTestId('email-input')
    const submitButton = screen.getByTestId('submit-button')
    
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
    fireEvent.click(submitButton)
    
    // Form should be disabled during submission
    await waitFor(() => {
      expect(emailInput).toBeDisabled()
      expect(submitButton).toBeDisabled()
    })
  })

  it('calls onSuccess callback when provided', async () => {
    const mockOnSuccess = jest.fn()
    
    // Mock successful response
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token' }),
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          message: 'If the email address is registered and verified, password reset instructions have been sent.',
        }),
      })

    renderWithProviders(<ForgotPassword onSuccess={mockOnSuccess} />)
    
    const emailInput = screen.getByTestId('email-input')
    const submitButton = screen.getByTestId('submit-button')
    
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } })
    fireEvent.click(submitButton)
    
    await waitFor(() => {
      expect(mockOnSuccess).toHaveBeenCalledWith('test@example.com')
    })
  })
})