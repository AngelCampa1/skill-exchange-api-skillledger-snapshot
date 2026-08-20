import React from 'react'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import { useRouter, useSearchParams } from 'next/navigation'
import ResetPassword from '../ResetPassword'

// Mock Next.js router
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
  useSearchParams: jest.fn(),
}))

// Mock fetch
const mockFetch = jest.fn()
global.fetch = mockFetch

describe('ResetPassword', () => {
  const mockPush = jest.fn()
  const mockGet = jest.fn()
  
  beforeEach(() => {
    jest.clearAllMocks()
    ;(useRouter as jest.Mock).mockReturnValue({
      push: mockPush,
    })
    ;(useSearchParams as jest.Mock).mockReturnValue({
      get: mockGet,
    })
  })

  it('renders reset password form correctly with valid token', async () => {
    mockGet.mockReturnValue('valid-token')
    
    // Mock successful token validation
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        valid: true,
        message: 'Token is valid',
      }),
    })

    render(<ResetPassword />)
    
    // Wait for token validation to complete and form to render
    await waitFor(() => {
      expect(screen.getByText('Reset Your Password')).toBeInTheDocument()
    }, { timeout: 3000 })
    
    await waitFor(() => {
      expect(screen.getByTestId('new-password-input')).toBeInTheDocument()
      expect(screen.getByTestId('confirm-password-input')).toBeInTheDocument()
      expect(screen.getByTestId('submit-button')).toBeInTheDocument()
    })
  })

  it('shows error message for invalid token', async () => {
    mockGet.mockReturnValue('invalid-token')
    
    // Mock invalid token response
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 400,
      json: async () => ({
        valid: false,
        message: 'Invalid reset token',
      }),
    })

    render(<ResetPassword />)
    
    await waitFor(() => {
      expect(screen.getByText('Reset Link Expired')).toBeInTheDocument()
      expect(screen.getByText(/password reset link has expired or has already been used/i)).toBeInTheDocument()
    })
  })

  it('shows error message for expired token', async () => {
    mockGet.mockReturnValue('expired-token')
    
    // Mock expired token response
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 410,
      json: async () => ({
        valid: false,
        message: 'Reset token has expired',
      }),
    })

    render(<ResetPassword />)
    
    await waitFor(() => {
      expect(screen.getByText('Reset Link Expired')).toBeInTheDocument()
      expect(screen.getByText(/password reset link has expired or has already been used/i)).toBeInTheDocument()
      expect(screen.getByText('Request New Reset')).toBeInTheDocument()
    })
  })

  it('shows invalid state when no token provided', async () => {
    mockGet.mockReturnValue(null)

    render(<ResetPassword />)

    await waitFor(() => {
      expect(screen.getByText('Invalid Reset Link')).toBeInTheDocument()
    })
  })

  it('validates password requirements', async () => {
    mockGet.mockReturnValue('valid-token')
    
    // Mock token validation response
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        valid: true,
        message: 'Token is valid',
      }),
    })

    render(<ResetPassword />)
    
    await waitFor(() => {
      expect(screen.getByTestId('new-password-input')).toBeInTheDocument()
    })

    const passwordInput = screen.getByTestId('new-password-input')
    
    // Test weak password
    act(() => {
      fireEvent.change(passwordInput, { target: { value: 'weak' } })
      fireEvent.blur(passwordInput)
    })
    
    await waitFor(() => {
      expect(screen.getByText('Password must be at least 12 characters')).toBeInTheDocument()
    })
  })

  it('validates password confirmation match', async () => {
    mockGet.mockReturnValue('valid-token')
    
    // Mock token validation response
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        valid: true,
        message: 'Token is valid',
      }),
    })

    render(<ResetPassword />)
    
    await waitFor(() => {
      expect(screen.getByTestId('new-password-input')).toBeInTheDocument()
    })

    const passwordInput = screen.getByTestId('new-password-input')
    const confirmInput = screen.getByTestId('confirm-password-input')
    
    // Enter different passwords
    act(() => {
      fireEvent.change(passwordInput, { target: { value: 'ValidPassword123!' } })
      fireEvent.change(confirmInput, { target: { value: 'DifferentPassword123!' } })
      fireEvent.blur(confirmInput)
    })
    
    await waitFor(() => {
      expect(screen.getByText("Passwords don't match")).toBeInTheDocument()
    })
  })

  it('shows password strength indicator', async () => {
    mockGet.mockReturnValue('valid-token')
    
    // Mock token validation response
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        valid: true,
        message: 'Token is valid',
      }),
    })

    render(<ResetPassword />)
    
    await waitFor(() => {
      expect(screen.getByTestId('new-password-input')).toBeInTheDocument()
    })

    const passwordInput = screen.getByTestId('new-password-input')
    
    // Test password strength progression
    act(() => {
      fireEvent.change(passwordInput, { target: { value: 'weak' } })
    })
    
    await waitFor(() => {
      expect(screen.getByTestId('strength-text')).toHaveTextContent('Weak')
    })
    
    act(() => {
      fireEvent.change(passwordInput, { target: { value: 'StrongPassword123!' } })
    })
    
    await waitFor(() => {
      expect(screen.getByTestId('strength-text')).toHaveTextContent('Strong')
    })
  })

  it('submits form with valid data and shows success state', async () => {
    mockGet.mockReturnValue('valid-token')
    
    // Mock token validation response
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        valid: true,
        message: 'Token is valid',
      }),
    })

    // Mock CSRF token response (for form submission)
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        token: 'csrf-token-123'
      }),
    })

    // Mock successful password reset
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        success: true,
        message: 'Password reset successful',
      }),
    })

    render(<ResetPassword />)
    
    await waitFor(() => {
      expect(screen.getByTestId('new-password-input')).toBeInTheDocument()
    })

    const passwordInput = screen.getByTestId('new-password-input')
    const confirmInput = screen.getByTestId('confirm-password-input')
    const submitButton = screen.getByTestId('submit-button')
    
    // Fill form with valid data
    await act(async () => {
      fireEvent.change(passwordInput, { target: { value: 'ValidPassword123!' } })
      fireEvent.change(confirmInput, { target: { value: 'ValidPassword123!' } })
      fireEvent.click(submitButton)
    })
    
    await waitFor(() => {
      expect(screen.getByText('Password Reset Successful!')).toBeInTheDocument()
    })
  })

  it('shows error for already used token', async () => {
    mockGet.mockReturnValue('used-token')
    
    // Mock used token response
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 410,
      json: async () => ({
        valid: false,
        message: 'Reset token has already been used',
      }),
    })

    render(<ResetPassword />)
    
    await waitFor(() => {
      expect(screen.getByText('Reset Link Expired')).toBeInTheDocument()
      expect(screen.getByText(/password reset link has expired or has already been used/i)).toBeInTheDocument()
    })
  })

  it('shows error for maximum attempts exceeded', async () => {
    mockGet.mockReturnValue('valid-token')
    
    // Mock token validation response
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        valid: true,
        message: 'Token is valid',
      }),
    })

    // Mock too many attempts error
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 429,
      json: async () => ({
        success: false,
        message: 'Too many reset attempts. Please try again later.',
      }),
    })

    render(<ResetPassword />)
    
    await waitFor(() => {
      expect(screen.getByTestId('new-password-input')).toBeInTheDocument()
    })

    const passwordInput = screen.getByTestId('new-password-input')
    const confirmInput = screen.getByTestId('confirm-password-input')
    const submitButton = screen.getByTestId('submit-button')
    
    // Fill form and submit
    await act(async () => {
      fireEvent.change(passwordInput, { target: { value: 'ValidPassword123!' } })
      fireEvent.change(confirmInput, { target: { value: 'ValidPassword123!' } })
      fireEvent.click(submitButton)
    })
    
    await waitFor(() => {
      expect(screen.getByText(/an error occurred/i)).toBeInTheDocument()
    })
  })

  it('handles network errors gracefully', async () => {
    mockGet.mockReturnValue('valid-token')
    
    // Mock token validation response
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        valid: true,
        message: 'Token is valid',
      }),
    })

    // Mock network error
    mockFetch.mockRejectedValueOnce(new Error('Network error'))

    render(<ResetPassword />)
    
    await waitFor(() => {
      expect(screen.getByTestId('new-password-input')).toBeInTheDocument()
    })

    const passwordInput = screen.getByTestId('new-password-input')
    const confirmInput = screen.getByTestId('confirm-password-input')
    const submitButton = screen.getByTestId('submit-button')
    
    // Fill form and submit
    await act(async () => {
      fireEvent.change(passwordInput, { target: { value: 'ValidPassword123!' } })
      fireEvent.change(confirmInput, { target: { value: 'ValidPassword123!' } })
      fireEvent.click(submitButton)
    })
    
    await waitFor(() => {
      expect(screen.getByText(/an error occurred/i)).toBeInTheDocument()
    })
  })

  it('navigates to login after successful reset', async () => {
    mockGet.mockReturnValue('valid-token')
    
    // Mock token validation response
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        valid: true,
        message: 'Token is valid',
      }),
    })

    // Mock CSRF token response (for form submission)
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        token: 'csrf-token-123'
      }),
    })

    // Mock successful password reset
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        success: true,
        message: 'Password reset successful',
      }),
    })

    render(<ResetPassword />)
    
    await waitFor(() => {
      expect(screen.getByTestId('new-password-input')).toBeInTheDocument()
    })

    const passwordInput = screen.getByTestId('new-password-input')
    const confirmInput = screen.getByTestId('confirm-password-input')
    const submitButton = screen.getByTestId('submit-button')
    
    // Fill form and submit
    await act(async () => {
      fireEvent.change(passwordInput, { target: { value: 'ValidPassword123!' } })
      fireEvent.change(confirmInput, { target: { value: 'ValidPassword123!' } })
      fireEvent.click(submitButton)
    })
    
    await waitFor(() => {
      expect(screen.getByText('Password Reset Successful!')).toBeInTheDocument()
    })

    // Check for navigation button
    expect(screen.getByRole('button', { name: /go to login/i })).toBeInTheDocument()
  })

  it('navigates to forgot password from invalid token page', async () => {
    mockGet.mockReturnValue('invalid-token')
    
    // Mock invalid token response
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 400,
      json: async () => ({
        valid: false,
        message: 'Invalid reset token',
      }),
    })

    render(<ResetPassword />)
    
    await waitFor(() => {
      expect(screen.getByText('Reset Link Expired')).toBeInTheDocument()
    })

    const requestNewLink = screen.getByRole('button', { name: /request new reset/i })
    await act(async () => {
      fireEvent.click(requestNewLink)
    })
    
    expect(mockPush).toHaveBeenCalledWith('/forgot-password')
  })

  it('disables form during submission', async () => {
    mockGet.mockReturnValue('valid-token')
    
    // Mock token validation response
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        valid: true,
        message: 'Token is valid',
      }),
    })

    // Mock delayed password reset response
    mockFetch.mockImplementationOnce(
      () => new Promise((resolve) => setTimeout(() => resolve({
        ok: true,
        json: async () => ({
          success: true,
          message: 'Password reset successful',
        }),
      }), 100))
    )

    render(<ResetPassword />)
    
    await waitFor(() => {
      expect(screen.getByTestId('new-password-input')).toBeInTheDocument()
    })

    const passwordInput = screen.getByTestId('new-password-input')
    const confirmInput = screen.getByTestId('confirm-password-input')
    const submitButton = screen.getByTestId('submit-button')
    
    // Fill form and submit
    await act(async () => {
      fireEvent.change(passwordInput, { target: { value: 'ValidPassword123!' } })
      fireEvent.change(confirmInput, { target: { value: 'ValidPassword123!' } })
      fireEvent.click(submitButton)
    })
    
    // Check button is disabled during submission
    expect(submitButton).toBeDisabled()
  })

  it('calls onSuccess callback when provided', async () => {
    const mockOnSuccess = jest.fn()
    mockGet.mockReturnValue('valid-token')
    
    // Mock token validation response
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        valid: true,
        message: 'Token is valid',
      }),
    })

    // Mock CSRF token response (for form submission)
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        token: 'csrf-token-123'
      }),
    })

    // Mock successful password reset
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        success: true,
        message: 'Password reset successful',
      }),
    })

    render(<ResetPassword onSuccess={mockOnSuccess} />)
    
    await waitFor(() => {
      expect(screen.getByTestId('new-password-input')).toBeInTheDocument()
    })

    const passwordInput = screen.getByTestId('new-password-input')
    const confirmInput = screen.getByTestId('confirm-password-input')
    const submitButton = screen.getByTestId('submit-button')
    
    // Fill form and submit
    await act(async () => {
      fireEvent.change(passwordInput, { target: { value: 'ValidPassword123!' } })
      fireEvent.change(confirmInput, { target: { value: 'ValidPassword123!' } })
      fireEvent.click(submitButton)
    })
    
    await waitFor(() => {
      expect(screen.getByText('Password Reset Successful!')).toBeInTheDocument()
    })
    
    await act(async () => {
      // Wait for the setTimeout callback to execute
      await new Promise(resolve => setTimeout(resolve, 10))
    })
    
    expect(mockOnSuccess).toHaveBeenCalled()
  })
})