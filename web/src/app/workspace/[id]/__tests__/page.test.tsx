import React from 'react'
import { render, screen, waitFor } from '@testing-library/react'
import '@testing-library/jest-dom'
import WorkspacePage from '../page'
import { useAuth } from '@/contexts/AuthContext'
import { useParams } from 'next/navigation'

// Mock next/navigation
jest.mock('next/navigation', () => ({
  useParams: jest.fn(),
}))

// Mock AuthContext
jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(),
}))

// Mock EnhancedWorkspaceDashboard
jest.mock('@/components/workspace/EnhancedWorkspaceDashboard', () => {
  return function MockEnhancedWorkspaceDashboard({ workspaceId, currentUserId, isClient }: any) {
    return (
      <div data-testid="enhanced-workspace-dashboard">
        <div data-testid="workspace-id">{workspaceId}</div>
        <div data-testid="current-user-id">{currentUserId}</div>
        <div data-testid="is-client">{isClient.toString()}</div>
      </div>
    )
  }
})

const mockUseParams = useParams as jest.MockedFunction<typeof useParams>
const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>

describe('WorkspacePage', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  // ========================================
  // Loading State Tests
  // ========================================
  describe('Loading State', () => {
    it('should show loading spinner when auth is loading', () => {
      mockUseParams.mockReturnValue({ id: 'workspace-123' })
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: true,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      const { container } = render(<WorkspacePage />)

      const spinner = container.querySelector('.animate-spin')
      expect(spinner).toBeInTheDocument()
      expect(spinner).toHaveClass('rounded-full', 'h-12', 'w-12', 'border-b-2', 'border-primary')
    })

    it('should have correct structure for loading container', () => {
      mockUseParams.mockReturnValue({ id: 'workspace-123' })
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: true,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      const { container } = render(<WorkspacePage />)

      const loadingContainer = container.querySelector('.min-h-screen.bg-muted')
      expect(loadingContainer).toBeInTheDocument()
      expect(loadingContainer).toHaveClass('flex', 'items-center', 'justify-center')
    })
  })

  // ========================================
  // Access Denied Tests
  // ========================================
  describe('Access Denied', () => {
    it('should show access denied when not authenticated', async () => {
      mockUseParams.mockReturnValue({ id: 'workspace-123' })
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      render(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByText('Access Denied')).toBeInTheDocument()
      })

      expect(screen.getByText('You must be logged in to access workspaces')).toBeInTheDocument()
      expect(screen.getByText('Go to Login')).toBeInTheDocument()
      expect(screen.getByText('Go to Login')).toHaveAttribute('href', '/login')
    })

    it('should show access denied when user is null', async () => {
      mockUseParams.mockReturnValue({ id: 'workspace-123' })
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: true, // Authenticated but no user object
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      render(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByText('Access Denied')).toBeInTheDocument()
      })

      expect(screen.getByText('You need to be logged in to access workspace features.')).toBeInTheDocument()
    })

    it('should apply correct classes to access denied container', async () => {
      mockUseParams.mockReturnValue({ id: 'workspace-123' })
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      const { container } = render(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByText('Access Denied')).toBeInTheDocument()
      })

      const outerDiv = container.querySelector('.min-h-screen.bg-muted')
      expect(outerDiv).toBeInTheDocument()
      expect(outerDiv).toHaveClass('flex', 'items-center', 'justify-center')

      const card = container.querySelector('.bg-card')
      expect(card).toHaveClass('rounded-lg', 'shadow-sm', 'border', 'border-border', 'p-8', 'max-w-md', 'w-full', 'mx-4')
    })

    it('should show login link with correct styles', async () => {
      mockUseParams.mockReturnValue({ id: 'workspace-123' })
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      render(<WorkspacePage />)

      await waitFor(() => {
        const link = screen.getByText('Go to Login')
        expect(link).toHaveClass('inline-flex', 'items-center', 'px-4', 'py-2', 'bg-primary', 'text-primary-foreground', 'font-medium', 'rounded-lg', 'hover:bg-primary/90')
      })
    })
  })

  // ========================================
  // Invalid Workspace Tests
  // ========================================
  describe('Invalid Workspace', () => {
    it('should show invalid workspace when workspace ID is missing', async () => {
      mockUseParams.mockReturnValue({}) // No id parameter
      mockUseAuth.mockReturnValue({
        user: { id: 'user-123', email: 'test@example.com', userName: 'testuser', emailVerified: true, taxCompliant: true, status: 'active', roles: [], permissions: [] },
        isAuthenticated: true,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      render(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByText('Invalid Workspace')).toBeInTheDocument()
      })

      expect(screen.getByText('The workspace ID is missing or invalid.')).toBeInTheDocument()
      expect(screen.getByText('Go to Dashboard')).toBeInTheDocument()
      expect(screen.getByText('Go to Dashboard')).toHaveAttribute('href', '/dashboard')
    })

    it('should show invalid workspace when id is null', async () => {
      mockUseParams.mockReturnValue({ id: '' } as any)
      mockUseAuth.mockReturnValue({
        user: { id: 'user-123', email: 'test@example.com', userName: 'testuser', emailVerified: true, taxCompliant: true, status: 'active', roles: [], permissions: [] },
        isAuthenticated: true,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      render(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByText('Invalid Workspace')).toBeInTheDocument()
      })
    })

    it('should show invalid workspace when id is undefined', async () => {
      mockUseParams.mockReturnValue({} as any)
      mockUseAuth.mockReturnValue({
        user: { id: 'user-123', email: 'test@example.com', userName: 'testuser', emailVerified: true, taxCompliant: true, status: 'active', roles: [], permissions: [] },
        isAuthenticated: true,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      render(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByText('Invalid Workspace')).toBeInTheDocument()
      })
    })

    it('should apply correct classes to invalid workspace container', async () => {
      mockUseParams.mockReturnValue({})
      mockUseAuth.mockReturnValue({
        user: { id: 'user-123', email: 'test@example.com', userName: 'testuser', emailVerified: true, taxCompliant: true, status: 'active', roles: [], permissions: [] },
        isAuthenticated: true,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      const { container } = render(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByText('Invalid Workspace')).toBeInTheDocument()
      })

      const card = container.querySelector('.bg-card')
      expect(card).toHaveClass('rounded-lg', 'shadow-sm', 'border', 'border-border', 'p-8', 'max-w-md', 'w-full', 'mx-4')
    })
  })

  // ========================================
  // Successful Workspace Rendering Tests
  // ========================================
  describe('Successful Workspace Rendering', () => {
    it('should render EnhancedWorkspaceDashboard for authenticated user with workspace ID', async () => {
      mockUseParams.mockReturnValue({ id: 'workspace-123' })
      mockUseAuth.mockReturnValue({
        user: {
          id: 'user-456',
          userName: 'Test User',
          email: 'test@example.com',
          emailVerified: true,
          taxCompliant: true,
          status: 'Active',
          roles: ['Freelancer'],
          permissions: []
        },
        isAuthenticated: true,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      render(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByTestId('enhanced-workspace-dashboard')).toBeInTheDocument()
      })

      expect(screen.getByTestId('workspace-id')).toHaveTextContent('workspace-123')
      expect(screen.getByTestId('current-user-id')).toHaveTextContent('user-456')
      expect(screen.getByTestId('is-client')).toHaveTextContent('false')
    })

    it('should identify user as client when they have Client role', async () => {
      mockUseParams.mockReturnValue({ id: 'workspace-789' })
      mockUseAuth.mockReturnValue({
        user: {
          id: 'user-999',
          userName: 'Client User',
          email: 'client@example.com',
          emailVerified: true,
          taxCompliant: true,
          status: 'Active',
          roles: ['Client'],
          permissions: []
        },
        isAuthenticated: true,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      render(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByTestId('enhanced-workspace-dashboard')).toBeInTheDocument()
      })

      expect(screen.getByTestId('is-client')).toHaveTextContent('true')
    })

    it('should identify user as client when they have multiple roles including Client', async () => {
      mockUseParams.mockReturnValue({ id: 'workspace-789' })
      mockUseAuth.mockReturnValue({
        user: {
          id: 'user-999',
          userName: 'Multi-role User',
          email: 'client@example.com',
          emailVerified: true,
          taxCompliant: true,
          status: 'Active',
          roles: ['Client', 'Freelancer'],
          permissions: []
        },
        isAuthenticated: true,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      render(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByTestId('enhanced-workspace-dashboard')).toBeInTheDocument()
      })

      expect(screen.getByTestId('is-client')).toHaveTextContent('true')
    })

    it('should identify user as non-client when roles is undefined', async () => {
      mockUseParams.mockReturnValue({ id: 'workspace-789' })
      mockUseAuth.mockReturnValue({
        user: {
          id: 'user-999',
          userName: 'No Role User',
          email: 'user@example.com',
          emailVerified: true,
          taxCompliant: true,
          status: 'Active',
          roles: [],
          permissions: []
        }, // No roles
        isAuthenticated: true,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      render(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByTestId('enhanced-workspace-dashboard')).toBeInTheDocument()
      })

      expect(screen.getByTestId('is-client')).toHaveTextContent('false')
    })

    it('should identify user as non-client when roles is empty array', async () => {
      mockUseParams.mockReturnValue({ id: 'workspace-789' })
      mockUseAuth.mockReturnValue({
        user: {
          id: 'user-999',
          userName: 'Empty Roles User',
          email: 'user@example.com',
          emailVerified: true,
          taxCompliant: true,
          status: 'Active',
          roles: [],
          permissions: []
        },
        isAuthenticated: true,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      render(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByTestId('enhanced-workspace-dashboard')).toBeInTheDocument()
      })

      expect(screen.getByTestId('is-client')).toHaveTextContent('false')
    })

    it('should apply correct classes to successful workspace container', async () => {
      mockUseParams.mockReturnValue({ id: 'workspace-123' })
      mockUseAuth.mockReturnValue({
        user: { id: 'user-456', userName: 'Test User', email: 'test@example.com', emailVerified: true, taxCompliant: true, status: 'Active', roles: [], permissions: [] },
        isAuthenticated: true,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      const { container } = render(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByTestId('enhanced-workspace-dashboard')).toBeInTheDocument()
      })

      const outerDiv = container.querySelector('.min-h-screen.bg-muted')
      expect(outerDiv).toBeInTheDocument()
    })
  })

  // ========================================
  // Edge Cases Tests
  // ========================================
  describe('Edge Cases', () => {
    it('should handle workspace ID as number string', async () => {
      mockUseParams.mockReturnValue({ id: '12345' })
      mockUseAuth.mockReturnValue({
        user: { id: 'user-456', userName: 'Test User', email: 'test@example.com', emailVerified: true, taxCompliant: true, status: 'Active', roles: [], permissions: [] },
        isAuthenticated: true,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      render(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByTestId('workspace-id')).toHaveTextContent('12345')
      })
    })

    it('should handle workspace ID with special characters', async () => {
      mockUseParams.mockReturnValue({ id: 'workspace-abc-123-xyz' })
      mockUseAuth.mockReturnValue({
        user: { id: 'user-456', userName: 'Test User', email: 'test@example.com', emailVerified: true, taxCompliant: true, status: 'Active', roles: [], permissions: [] },
        isAuthenticated: true,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      render(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByTestId('workspace-id')).toHaveTextContent('workspace-abc-123-xyz')
      })
    })

    it('should handle empty roles array correctly', async () => {
      mockUseParams.mockReturnValue({ id: 'workspace-123' })
      mockUseAuth.mockReturnValue({
        user: { id: 'user-456', userName: 'Test User', email: 'test@example.com', emailVerified: true, taxCompliant: true, status: 'Active', roles: [], permissions: [] },
        isAuthenticated: true,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      render(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByTestId('is-client')).toHaveTextContent('false')
      })
    })

    it('should handle transition from loading to authenticated', async () => {
      mockUseParams.mockReturnValue({ id: 'workspace-123' })

      const { rerender } = render(<WorkspacePage />)

      // Start with loading
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: true,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      rerender(<WorkspacePage />)
      expect(document.querySelector('.animate-spin')).toBeInTheDocument()

      // Transition to authenticated
      mockUseAuth.mockReturnValue({
        user: { id: 'user-456', userName: 'Test User', email: 'test@example.com', emailVerified: true, taxCompliant: true, status: 'Active', roles: [], permissions: [] },
        isAuthenticated: true,
        isLoading: false,
        login: jest.fn(),
        logout: jest.fn(),
        refreshToken: jest.fn(),
        isInitialized: true,
        updateUser: jest.fn(),
      })

      rerender(<WorkspacePage />)

      await waitFor(() => {
        expect(screen.getByTestId('enhanced-workspace-dashboard')).toBeInTheDocument()
      })
    })
  })
})
