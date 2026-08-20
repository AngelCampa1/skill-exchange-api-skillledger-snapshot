import React from 'react'
import { render, screen } from '@testing-library/react'
import { useRouter } from 'next/navigation'
import { useAuth } from '@/contexts/AuthContext'
import ProtectedRoute from '../ProtectedRoute'

// Mock Next.js router
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}))

// Mock AuthContext
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}))

const mockPush = jest.fn()
const mockUseRouter = useRouter as jest.MockedFunction<typeof useRouter>
const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>

const TestComponent = () => <div data-testid="protected-content">Protected Content</div>

describe('ProtectedRoute', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    mockUseRouter.mockReturnValue({
      push: mockPush,
      back: jest.fn(),
      forward: jest.fn(),
      refresh: jest.fn(),
      replace: jest.fn(),
      prefetch: jest.fn(),
    })
  })

  describe('Authentication Checks', () => {
    it('shows loading spinner when auth is loading', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: true,
        isInitialized: false,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      render(
        <ProtectedRoute>
          <TestComponent />
        </ProtectedRoute>
      )

      expect(screen.getByText('Loading...')).toBeInTheDocument()
      expect(screen.getByText('Loading...')).toBeInTheDocument()
    })

    it('redirects to login when not authenticated', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      render(
        <ProtectedRoute>
          <TestComponent />
        </ProtectedRoute>
      )

      expect(mockPush).toHaveBeenCalledWith('/login')
      expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument()
    })

    it('redirects to custom redirect path when not authenticated', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      render(
        <ProtectedRoute redirectTo="/custom-login">
          <TestComponent />
        </ProtectedRoute>
      )

      expect(mockPush).toHaveBeenCalledWith('/custom-login')
    })

    it('renders children when authenticated and all requirements met', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: '1',
          email: 'test@example.com',
          userName: 'test@example.com',
          emailVerified: true,
          phoneVerified: true,
          taxCompliant: true,
          status: 'EmailVerified',
          roles: ['User'],
          permissions: [],
        },
        isAuthenticated: true,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      render(
        <ProtectedRoute>
          <TestComponent />
        </ProtectedRoute>
      )

      expect(screen.getByTestId('protected-content')).toBeInTheDocument()
      expect(mockPush).not.toHaveBeenCalled()
    })
  })

  describe('Email Verification Requirement', () => {
    it('redirects to email verification when required and not verified', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: '1',
          email: 'test@example.com',
          userName: 'test@example.com',
          emailVerified: false,
          phoneVerified: true,
          taxCompliant: true,
          status: 'EmailVerified',
          roles: ['User'],
          permissions: [],
        },
        isAuthenticated: true,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      render(
        <ProtectedRoute requireEmailVerification={true}>
          <TestComponent />
        </ProtectedRoute>
      )

      expect(mockPush).toHaveBeenCalledWith('/verify-email')
      expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument()
    })

    it('renders children when email verification not required', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: '1',
          email: 'test@example.com',
          userName: 'test@example.com',
          emailVerified: false,
          phoneVerified: true,
          taxCompliant: true,
          status: 'EmailVerified',
          roles: ['User'],
          permissions: [],
        },
        isAuthenticated: true,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      render(
        <ProtectedRoute requireEmailVerification={false}>
          <TestComponent />
        </ProtectedRoute>
      )

      expect(screen.getByTestId('protected-content')).toBeInTheDocument()
      expect(mockPush).not.toHaveBeenCalled()
    })
  })

  describe('Phone Verification Requirement', () => {
    it('redirects to phone verification when required and not verified', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: '1',
          email: 'test@example.com',
          userName: 'test@example.com',
          emailVerified: true,
          phoneVerified: false,
          taxCompliant: true,
          status: 'EmailVerified',
          roles: ['User'],
          permissions: [],
        },
        isAuthenticated: true,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      render(
        <ProtectedRoute requirePhoneVerification={true}>
          <TestComponent />
        </ProtectedRoute>
      )

      expect(mockPush).toHaveBeenCalledWith('/profile/me')
      expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument()
    })

    it('renders children when phone verification not required', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: '1',
          email: 'test@example.com',
          userName: 'test@example.com',
          emailVerified: true,
          phoneVerified: false,
          taxCompliant: true,
          status: 'EmailVerified',
          roles: ['User'],
          permissions: [],
        },
        isAuthenticated: true,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      render(
        <ProtectedRoute requirePhoneVerification={false}>
          <TestComponent />
        </ProtectedRoute>
      )

      expect(screen.getByTestId('protected-content')).toBeInTheDocument()
      expect(mockPush).not.toHaveBeenCalled()
    })
  })

  describe('Tax Compliance Requirement', () => {
    it('redirects to tax compliance when required and not compliant', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: '1',
          email: 'test@example.com',
          userName: 'test@example.com',
          emailVerified: true,
          phoneVerified: true,
          taxCompliant: false,
          status: 'EmailVerified',
          roles: ['User'],
          permissions: [],
        },
        isAuthenticated: true,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      render(
        <ProtectedRoute requireTaxCompliance={true}>
          <TestComponent />
        </ProtectedRoute>
      )

      expect(mockPush).toHaveBeenCalledWith('/dashboard')
      expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument()
    })

    it('renders children when tax compliance not required', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: '1',
          email: 'test@example.com',
          userName: 'test@example.com',
          emailVerified: true,
          phoneVerified: true,
          taxCompliant: false,
          status: 'EmailVerified',
          roles: ['User'],
          permissions: [],
        },
        isAuthenticated: true,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      render(
        <ProtectedRoute requireTaxCompliance={false}>
          <TestComponent />
        </ProtectedRoute>
      )

      expect(screen.getByTestId('protected-content')).toBeInTheDocument()
      expect(mockPush).not.toHaveBeenCalled()
    })
  })

  describe('Multiple Requirements', () => {
    it('redirects to email verification first when both email and phone verification required but email not verified', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: '1',
          email: 'test@example.com',
          userName: 'test@example.com',
          emailVerified: false,
          phoneVerified: false,
          taxCompliant: true,
          status: 'EmailVerified',
          roles: ['User'],
          permissions: [],
        },
        isAuthenticated: true,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      render(
        <ProtectedRoute requireEmailVerification={true} requirePhoneVerification={true}>
          <TestComponent />
        </ProtectedRoute>
      )

      expect(mockPush).toHaveBeenCalledWith('/verify-email')
    })

    it('redirects to phone verification when email verified but phone not verified', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: '1',
          email: 'test@example.com',
          userName: 'test@example.com',
          emailVerified: true,
          phoneVerified: false,
          taxCompliant: true,
          status: 'EmailVerified',
          roles: ['User'],
          permissions: [],
        },
        isAuthenticated: true,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      render(
        <ProtectedRoute requireEmailVerification={true} requirePhoneVerification={true}>
          <TestComponent />
        </ProtectedRoute>
      )

      expect(mockPush).toHaveBeenCalledWith('/profile/me')
    })

    it('renders children when all requirements are met', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: '1',
          email: 'test@example.com',
          userName: 'test@example.com',
          emailVerified: true,
          phoneVerified: true,
          taxCompliant: true,
          status: 'EmailVerified',
          roles: ['User'],
          permissions: [],
        },
        isAuthenticated: true,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      render(
        <ProtectedRoute 
          requireEmailVerification={true} 
          requirePhoneVerification={true}
          requireTaxCompliance={true}
        >
          <TestComponent />
        </ProtectedRoute>
      )

      expect(screen.getByTestId('protected-content')).toBeInTheDocument()
      expect(mockPush).not.toHaveBeenCalled()
    })
  })

  describe('Edge Cases', () => {
    it('handles null user when authenticated is true', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: true,
        isInitialized: true,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      render(
        <ProtectedRoute>
          <TestComponent />
        </ProtectedRoute>
      )

      expect(screen.getByTestId('protected-content')).toBeInTheDocument()
    })

    it('does not redirect when already loading', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: true,
        isInitialized: false,  // BUG-HIGH-003 FIX: Add isInitialized to tests
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        updateUser: jest.fn(),
      })

      render(
        <ProtectedRoute>
          <TestComponent />
        </ProtectedRoute>
      )

      expect(mockPush).not.toHaveBeenCalled()
      expect(screen.getByText('Loading...')).toBeInTheDocument()
    })
  })
})