import { render, screen, waitFor, act } from '@testing-library/react'
import { useRouter } from 'next/navigation'
import { AuthProvider, useAuth } from '../AuthContext'

// Mock Next.js router
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}))

// Mock fetch globally
const mockFetch = jest.fn()
global.fetch = mockFetch

const mockPush = jest.fn()
;(useRouter as jest.Mock).mockReturnValue({
  push: mockPush,
})

// E2E-017 FIX: Mock window.location.href for logout redirect testing
// The logout function now uses window.location.href instead of router.push for full page reload
let mockLocationHref = ''
const originalLocation = window.location
beforeAll(() => {
  // Delete and redefine location for testing
  delete (window as { location?: Location }).location
  Object.defineProperty(window, 'location', {
    value: {
      ...originalLocation,
      href: '',
      assign: jest.fn(),
      replace: jest.fn(),
      reload: jest.fn(),
    },
    writable: true,
    configurable: true,
  })
  Object.defineProperty(window.location, 'href', {
    get: () => mockLocationHref,
    set: (value: string) => { mockLocationHref = value },
    configurable: true,
  })
})

afterAll(() => {
  Object.defineProperty(window, 'location', {
    value: originalLocation,
    writable: true,
    configurable: true,
  })
})

// Test component to access auth context
const TestComponent = () => {
  const { user, isAuthenticated, isLoading, login, logout } = useAuth()

  return (
    <div>
      <div data-testid="loading">{isLoading ? 'loading' : 'loaded'}</div>
      <div data-testid="authenticated">{isAuthenticated ? 'authenticated' : 'not-authenticated'}</div>
      <div data-testid="user-email">{user?.email || 'no-email'}</div>
      <button data-testid="login-btn" onClick={() => login('test@example.com', 'password123')}>
        Login
      </button>
      <button data-testid="logout-btn" onClick={() => logout()}>
        Logout
      </button>
    </div>
  )
}

describe('AuthContext', () => {
  beforeEach(() => {
    mockFetch.mockClear()
    mockPush.mockClear()
    mockLocationHref = '' // E2E-017 FIX: Reset location mock
    localStorage.clear()
    jest.clearAllMocks()
  })

  it('should initialize and complete loading', async () => {
    // Mock localStorage to be empty
    Object.defineProperty(window, 'localStorage', {
      value: {
        getItem: jest.fn(() => null),
        setItem: jest.fn(),
        removeItem: jest.fn(),
        clear: jest.fn(),
      },
      writable: true,
    })

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    )

    // Wait for initialization to complete (no stored token)
    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('loaded')
    })
    
    expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated')
    expect(screen.getByTestId('user-email')).toHaveTextContent('no-email')
  })

  it('should validate stored token on initialization', async () => {
    const mockToken = 'stored-jwt-token'
    const mockUser = {
      id: '1',
      email: 'test@example.com',
      userName: 'test',
      emailVerified: true,
      phoneVerified: false,
      taxCompliant: false,
      status: 'Active',
      roles: ['User'],
    }

    // Mock localStorage with token
    Object.defineProperty(window, 'localStorage', {
      value: {
        getItem: jest.fn(() => mockToken),
        setItem: jest.fn(),
        removeItem: jest.fn(),
        clear: jest.fn(),
      },
      writable: true,
    })

    // Mock successful token validation
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, user: mockUser }),
    })

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    )

    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('loaded')
      expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated')
      expect(screen.getByTestId('user-email')).toHaveTextContent('test@example.com')
    })

    expect(mockFetch).toHaveBeenCalledWith('/api/auth/me', expect.objectContaining({
      method: 'GET',
      credentials: 'include',
      signal: expect.any(AbortSignal),
    }))
  })

  it('should handle successful login', async () => {
    const mockUser = {
      id: '1',
      email: 'test@example.com',
      userName: 'test',
      emailVerified: true,
      phoneVerified: false,
      taxCompliant: false,
      status: 'Active',
      roles: ['User'],
    }

    // Mock initialization call (no user initially)
    mockFetch.mockResolvedValueOnce({
      ok: false,
      json: async () => ({ success: false }),
    })

    // Mock CSRF token request
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token' }),
      })
      // Mock successful login
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          success: true,
          user: mockUser,
        }),
      })

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    )

    // Wait for initialization to complete
    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('loaded')
    })

    // Perform login
    await act(async () => {
      screen.getByTestId('login-btn').click()
    })

    await waitFor(() => {
      expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated')
      expect(screen.getByTestId('user-email')).toHaveTextContent('test@example.com')
    })

    // Verify CSRF token was fetched
    expect(mockFetch).toHaveBeenCalledWith('/api/auth/csrf-token', {
      method: 'GET',
      credentials: 'include',
    })

    // Verify login request
    expect(mockFetch).toHaveBeenCalledWith('/api/auth/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-CSRF-TOKEN': 'csrf-token',
      },
      credentials: 'include',
      body: JSON.stringify({
        email: 'test@example.com',
        password: 'password123',
        rememberMe: false,
      }),
    })

    // Note: The implementation uses HTTP-only cookies, not localStorage
    // Token is stored as a cookie by the server
  })

  it('should handle login failure', async () => {
    // Mock initialization call (no user initially)
    mockFetch.mockResolvedValueOnce({
      ok: false,
      json: async () => ({ success: false }),
    })

    // Mock CSRF token request
    mockFetch
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token' }),
      })
      // Mock failed login
      .mockResolvedValueOnce({
        ok: false,
        json: async () => ({
          success: false,
          message: 'Invalid email or password.',
        }),
      })

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    )

    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('loaded')
    })

    await act(async () => {
      screen.getByTestId('login-btn').click()
    })

    // Wait for login to complete
    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('loaded')
    })

    // Should remain unauthenticated
    expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated')
    expect(screen.getByTestId('user-email')).toHaveTextContent('no-email')

    // Note: The implementation uses HTTP-only cookies, not localStorage
    // No token storage occurs on login failure
  })

  it('should handle logout', async () => {
    const mockUser = {
      id: '1',
      email: 'test@example.com',
      userName: 'test',
      emailVerified: true,
      phoneVerified: false,
      taxCompliant: false,
      status: 'Active',
      roles: ['User'],
    }

    // Mock token validation during initialization (/api/auth/me call) FIRST
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true, user: mockUser }),
    })

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    )

    await waitFor(() => {
      expect(screen.getByTestId('authenticated')).toHaveTextContent('authenticated')
    })

    // Clear previous mocks and set up CSRF token and logout responses
    mockFetch.mockClear()
    
    // Mock CSRF token request
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ token: 'csrf-token' }),
    })
    
    // Mock the logout API call
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ success: true }),
    })

    await act(async () => {
      screen.getByTestId('logout-btn').click()
    })

    await waitFor(() => {
      expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated')
      expect(screen.getByTestId('user-email')).toHaveTextContent('no-email')
    })

    // Wait a bit for async logout operations
    await new Promise(resolve => setTimeout(resolve, 100))

    // Note: CSRF token fetching and logout API behavior depends on implementation details
    // The main goal is to test that logout clears state and redirects
    // E2E-017 FIX: Verify redirect to login via window.location.href (not router.push)
    // The implementation uses window.location.href for full page reload to avoid stale cookie issues
    expect(mockLocationHref).toBe('/login')
  })

  it('should handle invalid stored token', async () => {
    // Mock failed token validation
    mockFetch.mockResolvedValueOnce({
      ok: false,
      json: async () => ({ success: false }),
    })

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    )

    await waitFor(() => {
      expect(screen.getByTestId('loading')).toHaveTextContent('loaded')
      expect(screen.getByTestId('authenticated')).toHaveTextContent('not-authenticated')
    })

    // Note: The implementation uses HTTP-only cookies, not localStorage
    // Token validation failure is handled by not setting the user
  })
})