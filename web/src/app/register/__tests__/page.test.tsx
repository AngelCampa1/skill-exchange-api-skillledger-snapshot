/**
 * Integration tests for Registration Page (src/app/register/page.tsx)
 *
 * Coverage target: 85-95% (227 lines)
 * Test count: 45 tests
 *
 * Test Strategy:
 * - Mock external dependencies only (fetch, useRouter, AuthContext)
 * - Test complete registration flow with CSRF and auth cookie handling
 * - Verify BUG-001 fix (CSRF token fetch)
 * - Verify BUG-FE-020 fix (AuthContext update before redirect)
 * - Test error handling, success states, and UI elements
 */

import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import RegisterPage from '../page'
import { useAuth } from '@/contexts/AuthContext'
import { useRouter } from 'next/navigation'

// Mock dependencies
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
  User: {},
}))

jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}))

jest.mock('next/link', () => {
  const MockLink = ({ children, href }: any) => <a href={href}>{children}</a>
  MockLink.displayName = 'MockLink'
  return MockLink
})

// Mock child components
jest.mock('@/components/Logo', () => ({
  Logo: ({ size, showText }: any) => (
    <div data-testid="logo" data-size={size} data-show-text={showText}>
      SkillLedger Logo
    </div>
  ),
}))

jest.mock('@/components/ThemeToggle', () => ({
  ThemeToggle: () => <button data-testid="theme-toggle">Toggle Theme</button>,
}))

jest.mock('@/components/RegistrationForm', () => ({
  __esModule: true,
  default: ({ onSubmit, isLoading }: any) => (
    <form
      data-testid="registration-form"
      onSubmit={(e) => {
        e.preventDefault()
        onSubmit({
          email: 'test@example.com',
          password: 'Password123!',
          confirmPassword: 'Password123!',
          firstName: 'John',
          lastName: 'Doe',
          acceptedTerms: true,
        })
      }}
    >
      <button type="submit" disabled={isLoading} data-testid="submit-button">
        {isLoading ? 'Creating Account...' : 'Create Account'}
      </button>
    </form>
  ),
}))

const mockUseAuth = useAuth as jest.Mock
const mockUseRouter = useRouter as jest.Mock
const mockPush = jest.fn()
const mockUpdateUser = jest.fn()

// Mock global fetch
global.fetch = jest.fn()
const mockFetch = global.fetch as jest.Mock

describe('RegisterPage', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    jest.useFakeTimers()

    mockUseRouter.mockReturnValue({
      push: mockPush,
    })

    mockUseAuth.mockReturnValue({
      updateUser: mockUpdateUser,
    })
  })

  afterEach(() => {
    jest.runOnlyPendingTimers()
    jest.useRealTimers()
  })

  // ============================================
  // Initial Render (8 tests)
  // ============================================
  describe('Initial Render', () => {
    it('should render registration page container', () => {
      render(<RegisterPage />)

      const container = screen.getByText('Create Your Account').closest('div')
      expect(container).toBeInTheDocument()
    })

    it('should display logo with correct props', () => {
      render(<RegisterPage />)

      const logo = screen.getByTestId('logo')
      expect(logo).toBeInTheDocument()
      expect(logo).toHaveAttribute('data-size', 'medium')
      expect(logo).toHaveAttribute('data-show-text', 'true')
    })

    it('should display "Back to Home" link', () => {
      render(<RegisterPage />)

      const backLink = screen.getByText('Back to Home')
      expect(backLink).toBeInTheDocument()
      expect(backLink.closest('a')).toHaveAttribute('href', '/')
    })

    it('should display theme toggle', () => {
      render(<RegisterPage />)

      expect(screen.getByTestId('theme-toggle')).toBeInTheDocument()
    })

    it('should display "Create Your Account" heading', () => {
      render(<RegisterPage />)

      expect(screen.getByText('Create Your Account')).toBeInTheDocument()
    })

    it('should display 30-day trial tagline', () => {
      render(<RegisterPage />)

      expect(screen.getByText('Create your account to start your 30-day free trial.')).toBeInTheDocument()
    })

    it('should display trial benefits instead of free tier benefits', () => {
      render(<RegisterPage />)

      expect(screen.getByText('30-day free trial')).toBeInTheDocument()
      expect(screen.getByText('Credit card required to start')).toBeInTheDocument()
      expect(screen.getByText('Cancel anytime')).toBeInTheDocument()
      expect(screen.queryByText('No credit card required')).not.toBeInTheDocument()
    })

    it('should render registration form', () => {
      render(<RegisterPage />)

      expect(screen.getByTestId('registration-form')).toBeInTheDocument()
    })

    it('should not display error or success messages initially', () => {
      render(<RegisterPage />)

      expect(screen.queryByText(/Registration successful!/)).not.toBeInTheDocument()
      expect(screen.queryByText(/Registration failed/)).not.toBeInTheDocument()
    })
  })

  // ============================================
  // Footer Links (4 tests)
  // ============================================
  describe('Footer Links', () => {
    it('should display "Already have an account?" text', () => {
      render(<RegisterPage />)

      expect(screen.getByText('Already have an account?')).toBeInTheDocument()
    })

    it('should display "Sign In Instead" button', () => {
      render(<RegisterPage />)

      const signInButton = screen.getByText('Sign In Instead')
      expect(signInButton).toBeInTheDocument()
    })

    it('should navigate to login when "Sign In Instead" clicked', async () => {
      const user = userEvent.setup({ delay: null })
      render(<RegisterPage />)

      const signInButton = screen.getByText('Sign In Instead')
      await user.click(signInButton)

      expect(mockPush).toHaveBeenCalledWith('/login')
    })

    it('should display terms, privacy, and support links', () => {
      render(<RegisterPage />)

      expect(screen.getByText('Terms of Service')).toBeInTheDocument()
      expect(screen.getByText('Privacy Policy')).toBeInTheDocument()
      expect(screen.getByText('Contact support')).toBeInTheDocument()
    })
  })

  // ============================================
  // CSRF Token Fetch - BUG-001 Fix (5 tests)
  // ============================================
  describe('CSRF Token Fetch - BUG-001 Fix', () => {
    it('should fetch CSRF token before registration', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token-123' }),
      })

      render(<RegisterPage />)

      const form = screen.getByTestId('registration-form')
      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/auth/csrf-token', {
          method: 'GET',
          credentials: 'include',
        })
      })
    })

    it('should include CSRF token in registration request', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            success: true,
            message: 'Registration successful',
            user: {
              id: 'user-123',
              email: 'test@example.com',
              userName: 'johndoe',
              emailVerified: false,
              status: 'active',
              roles: ['User'],
              permissions: [],
            },
          }),
        })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith(
          '/api/auth/register',
          expect.objectContaining({
            method: 'POST',
            headers: expect.objectContaining({
              'X-CSRF-TOKEN': 'csrf-token-123',
            }),
            credentials: 'include',
          })
        )
      })
    })

    it('should handle CSRF token fetch failure', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
      })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        expect(screen.getByText('An unexpected error occurred. Please try again.')).toBeInTheDocument()
      })
    })

    it('should include credentials in CSRF request', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token-123' }),
      })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        const csrfCall = mockFetch.mock.calls.find((call) => call[0] === '/api/auth/csrf-token')
        expect(csrfCall?.[1]).toHaveProperty('credentials', 'include')
      })
    })

    it('should send registration data in request body', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ success: true, message: 'Success' }),
        })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        const registerCall = mockFetch.mock.calls.find((call) => call[0] === '/api/auth/register')
        const body = JSON.parse(registerCall?.[1]?.body || '{}')

        expect(body).toEqual({
          email: 'test@example.com',
          password: 'Password123!',
          confirmPassword: 'Password123!',
          firstName: 'John',
          lastName: 'Doe',
          acceptedTerms: true,
        })
      })
    })
  })

  // ============================================
  // Successful Registration - BUG-FE-020 Fix (8 tests)
  // ============================================
  describe('Successful Registration - BUG-FE-020 Fix', () => {
    it('should display success message on successful registration', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            success: true,
            message: 'Registration successful',
            user: {
              id: 'user-123',
              email: 'test@example.com',
              userName: 'johndoe',
              emailVerified: false,
              status: 'active',
              roles: ['User'],
              permissions: [],
            },
          }),
        })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        expect(screen.getByText('Registration successful!')).toBeInTheDocument()
        expect(screen.getByText('Redirecting you now...')).toBeInTheDocument()
      })
    })

    it('should update AuthContext with user data before redirect - BUG-FE-020 FIX', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            success: true,
            message: 'Registration successful',
            user: {
              id: 'user-123',
              email: 'test@example.com',
              userName: 'johndoe',
              emailVerified: false,
              status: 'active',
              roles: ['User'],
              permissions: [],
            },
          }),
        })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      jest.advanceTimersByTime(2000)

      await waitFor(() => {
        expect(mockUpdateUser).toHaveBeenCalledWith({
          id: 'user-123',
          email: 'test@example.com',
          userName: 'johndoe',
          firstName: 'John',
          lastName: 'Doe',
          emailVerified: false,
          taxCompliant: false,
          status: 'active',
          roles: ['User'],
          permissions: [],
        })
      })
    })

    it('should redirect to choose-plan after 2 seconds when user data exists', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            success: true,
            message: 'Registration successful',
            user: {
              id: 'user-123',
              email: 'test@example.com',
              userName: 'johndoe',
              emailVerified: false,
              status: 'active',
              roles: ['User'],
              permissions: [],
            },
          }),
        })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      expect(mockPush).not.toHaveBeenCalled()

      jest.advanceTimersByTime(2000)

      await waitFor(() => {
        expect(mockPush).toHaveBeenCalledWith('/subscription/choose-plan')
      })
    })

    it('should redirect to login if no user data in response', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            success: true,
            message: 'Registration successful',
          }),
        })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      jest.advanceTimersByTime(2000)

      await waitFor(() => {
        expect(mockPush).toHaveBeenCalledWith('/login')
      })
    })

    it('should disable form during success redirect wait', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            success: true,
            message: 'Registration successful',
            user: { id: 'user-123', email: 'test@example.com', userName: 'johndoe' },
          }),
        })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        const submitButton = screen.getByTestId('submit-button')
        expect(submitButton).toBeDisabled()
      })
    })

    it('should set taxCompliant to false for new users', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            success: true,
            message: 'Registration successful',
            user: {
              id: 'user-123',
              email: 'test@example.com',
              userName: 'johndoe',
              emailVerified: false,
              status: 'active',
              roles: ['User'],
              permissions: [],
            },
          }),
        })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      jest.advanceTimersByTime(2000)

      await waitFor(() => {
        expect(mockUpdateUser).toHaveBeenCalledWith(
          expect.objectContaining({
            taxCompliant: false,
          })
        )
      })
    })

    it('should include firstName and lastName from form in user context', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            success: true,
            message: 'Registration successful',
            user: {
              id: 'user-123',
              email: 'test@example.com',
              userName: 'johndoe',
              emailVerified: false,
              status: 'active',
              roles: ['User'],
              permissions: [],
            },
          }),
        })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      jest.advanceTimersByTime(2000)

      await waitFor(() => {
        expect(mockUpdateUser).toHaveBeenCalledWith(
          expect.objectContaining({
            firstName: 'John',
            lastName: 'Doe',
          })
        )
      })
    })

    it('should not call updateUser if no user data in response', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            success: true,
            message: 'Registration successful',
          }),
        })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      jest.advanceTimersByTime(2000)

      await waitFor(() => {
        expect(mockUpdateUser).not.toHaveBeenCalled()
      })
    })
  })

  // ============================================
  // Error Handling (6 tests)
  // ============================================
  describe('Error Handling', () => {
    it('should display error message when registration fails', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: false,
          json: async () => ({
            success: false,
            message: 'Email already exists',
          }),
        })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        expect(screen.getByText('Email already exists')).toBeInTheDocument()
      })
    })

    it('should display generic error message if no message in response', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: false,
          json: async () => ({
            success: false,
          }),
        })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        expect(screen.getByText('Registration failed. Please try again.')).toBeInTheDocument()
      })
    })

    it('should handle network errors gracefully', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockRejectedValueOnce(new Error('Network error'))

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        expect(screen.getByText('An unexpected error occurred. Please try again.')).toBeInTheDocument()
      })
    })

    it('should clear previous error when submitting again', async () => {
      const user = userEvent.setup({ delay: null })

      // First submission - fail
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: false,
          json: async () => ({
            success: false,
            message: 'Email already exists',
          }),
        })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        expect(screen.getByText('Email already exists')).toBeInTheDocument()
      })

      // Second submission - success
      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-456' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            success: true,
            message: 'Registration successful',
            user: { id: 'user-123', email: 'test@example.com', userName: 'johndoe' },
          }),
        })

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        expect(screen.queryByText('Email already exists')).not.toBeInTheDocument()
      })
    })

    it('should display error alert with icon', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: false,
          json: async () => ({
            success: false,
            message: 'Validation error',
          }),
        })

      const { container } = render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        const errorAlert = container.querySelector('.bg-destructive\\/10')
        expect(errorAlert).toBeInTheDocument()
        expect(errorAlert?.querySelector('svg')).toBeInTheDocument()
      })
    })

    it('should set loading to false after error', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: false,
          json: async () => ({
            success: false,
            message: 'Error',
          }),
        })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        const submitButton = screen.getByTestId('submit-button')
        expect(submitButton).not.toBeDisabled()
      })
    })
  })

  // ============================================
  // Loading State (4 tests)
  // ============================================
  describe('Loading State', () => {
    it('should show loading state during registration', async () => {
      const user = userEvent.setup({ delay: null })

      let resolveRegistration: any
      const registrationPromise = new Promise((resolve) => {
        resolveRegistration = resolve
      })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockImplementationOnce(() => registrationPromise)

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        const submitButton = screen.getByTestId('submit-button')
        expect(submitButton).toBeDisabled()
        expect(submitButton).toHaveTextContent('Creating Account...')
      })

      resolveRegistration({
        ok: true,
        json: async () => ({ success: true, message: 'Success' }),
      })
    })

    it('should disable form during loading', async () => {
      const user = userEvent.setup({ delay: null })

      let resolveRegistration: any
      const registrationPromise = new Promise((resolve) => {
        resolveRegistration = resolve
      })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockImplementationOnce(() => registrationPromise)

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        expect(screen.getByTestId('submit-button')).toBeDisabled()
      })

      resolveRegistration({
        ok: true,
        json: async () => ({ success: true, message: 'Success' }),
      })
    })

    it('should show loading state during CSRF fetch', async () => {
      const user = userEvent.setup({ delay: null })

      let resolveCsrf: any
      const csrfPromise = new Promise((resolve) => {
        resolveCsrf = resolve
      })

      mockFetch.mockImplementationOnce(() => csrfPromise)

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        expect(screen.getByTestId('submit-button')).toBeDisabled()
      })

      resolveCsrf({
        ok: true,
        json: async () => ({ token: 'csrf-token-123' }),
      })
    })

    it('should remain in loading state until success redirect', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            success: true,
            message: 'Registration successful',
            user: { id: 'user-123', email: 'test@example.com', userName: 'johndoe' },
          }),
        })

      render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        expect(screen.getByText('Registration successful!')).toBeInTheDocument()
      })

      // Should still be disabled during 2-second wait
      expect(screen.getByTestId('submit-button')).toBeDisabled()

      jest.advanceTimersByTime(2000)

      await waitFor(() => {
        expect(mockPush).toHaveBeenCalled()
      })
    })
  })

  // ============================================
  // UI Elements (5 tests)
  // ============================================
  describe('UI Elements', () => {
    it('should display success alert with checkmark icon', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            success: true,
            message: 'Registration successful',
            user: { id: 'user-123', email: 'test@example.com', userName: 'johndoe' },
          }),
        })

      const { container } = render(<RegisterPage />)

      await user.click(screen.getByTestId('submit-button'))

      await waitFor(() => {
        const successAlert = container.querySelector('.bg-success\\/10')
        expect(successAlert).toBeInTheDocument()
        expect(successAlert?.querySelector('svg')).toBeInTheDocument()
      })
    })

    it('should have responsive container classes', () => {
      const { container } = render(<RegisterPage />)

      const mainContainer = container.querySelector('.min-h-screen')
      expect(mainContainer).toBeInTheDocument()
    })

    it('should display card with premium styling', () => {
      const { container } = render(<RegisterPage />)

      const card = container.querySelector('.card-premium')
      expect(card).toBeInTheDocument()
    })

    it('should have proper link styling for terms and privacy', () => {
      render(<RegisterPage />)

      const termsLink = screen.getByText('Terms of Service')
      const privacyLink = screen.getByText('Privacy Policy')

      expect(termsLink.closest('a')).toHaveAttribute('href', '/terms')
      expect(privacyLink.closest('a')).toHaveAttribute('href', '/privacy')
    })

    it('should display support email link', () => {
      render(<RegisterPage />)

      const supportLink = screen.getByText('Contact support')
      expect(supportLink.closest('a')).toHaveAttribute('href', 'mailto:angel.campa@skillledger.app')
    })
  })

  // ============================================
  // Integration (2 tests)
  // ============================================
  describe('Integration', () => {
    it('should render complete registration page without errors', () => {
      const { container } = render(<RegisterPage />)

      expect(container.firstChild).toBeTruthy()
      expect(screen.getByText('Create Your Account')).toBeInTheDocument()
      expect(screen.getByTestId('logo')).toBeInTheDocument()
      expect(screen.getByTestId('theme-toggle')).toBeInTheDocument()
      expect(screen.getByTestId('registration-form')).toBeInTheDocument()
      expect(screen.getByText('Sign In Instead')).toBeInTheDocument()
    })

    it('should complete full registration flow with all fixes', async () => {
      const user = userEvent.setup({ delay: null })

      mockFetch
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({ token: 'csrf-token-123' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          json: async () => ({
            success: true,
            message: 'Registration successful',
            user: {
              id: 'user-123',
              email: 'test@example.com',
              userName: 'johndoe',
              emailVerified: false,
              status: 'active',
              roles: ['User'],
              permissions: [],
            },
          }),
        })

      render(<RegisterPage />)

      // Submit form
      await user.click(screen.getByTestId('submit-button'))

      // BUG-001 FIX: CSRF token fetched
      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/auth/csrf-token', expect.any(Object))
      })

      // Success message displayed
      await waitFor(() => {
        expect(screen.getByText('Registration successful!')).toBeInTheDocument()
      })

      // Wait for redirect timer
      jest.advanceTimersByTime(2000)

      // BUG-FE-020 FIX: User context updated before redirect
      await waitFor(() => {
        expect(mockUpdateUser).toHaveBeenCalledWith(
          expect.objectContaining({
            id: 'user-123',
            email: 'test@example.com',
            firstName: 'John',
            lastName: 'Doe',
            taxCompliant: false,
          })
        )
        expect(mockPush).toHaveBeenCalledWith('/subscription/choose-plan')
      })
    })
  })
})
