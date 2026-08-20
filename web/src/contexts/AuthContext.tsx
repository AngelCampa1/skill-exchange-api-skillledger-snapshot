'use client'
import { logger } from '../utils/logger';
import { trackEvent } from '@/utils/analytics'
import { clearCsrfToken } from '../utils/apiClient'

import React, { createContext, useContext, useEffect, useState, useCallback, useRef, ReactNode } from 'react'
import { useRouter } from 'next/navigation'

export interface User {
  id: string
  email: string
  userName: string
  firstName?: string  // E2E-015 FIX: Added for display purposes
  lastName?: string   // E2E-015 FIX: Added for display purposes
  emailVerified: boolean
  phoneVerified?: boolean
  taxCompliant: boolean
  status: string
  roles: string[]
  permissions: string[]
}

export interface AuthContextType {
  user: User | null
  isLoading: boolean
  isInitialized: boolean  // BUG-HIGH-003 FIX: Export initialization state
  isAuthenticated: boolean
  login: (email: string, password: string, rememberMe?: boolean) => Promise<{ success: boolean; message?: string }>
  logout: (logoutFromAllDevices?: boolean) => Promise<void>
  refreshToken: () => Promise<boolean>
  updateUser: (user: User) => void
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isInitialized, setIsInitialized] = useState(false)

  // Refs for timers to prevent memory leaks
  const refreshTimerRef = useRef<NodeJS.Timeout | null>(null)
  const sessionTimeoutTimerRef = useRef<NodeJS.Timeout | null>(null)

  // Lock to prevent concurrent token refreshes
  const isRefreshingRef = useRef<boolean>(false)
  // BUG-FE-015 FIX: Store the in-flight refresh Promise so concurrent callers can await the same result
  const refreshPromiseRef = useRef<Promise<boolean> | null>(null)

  // BUG-FE-009 FIX: Ref to break circular dependency between refreshToken and scheduleTokenRefresh
  const scheduleTokenRefreshRef = useRef<(() => void) | null>(null)

  const router = useRouter()

  // SIMPLIFIED: Authentication is based solely on having a valid user (cookies handle the rest)
  const isAuthenticated = !!user

  // SIMPLIFIED: Fixed refresh interval - backend handles token expiration
  // Tokens expire in 15 minutes, refresh every 13 minutes (2 minutes before expiration)
  const TOKEN_REFRESH_INTERVAL = 13 * 60 * 1000 // 13 minutes

  // Session timeout for automatic logout on inactivity
  const SESSION_TIMEOUT_MS = 30 * 60 * 1000 // 30 minutes

  // Auth check timeout to prevent hanging requests on cold starts
  const AUTH_CHECK_TIMEOUT_MS = 10_000 // 10 seconds

  // Initialize auth state from cookie
  // BUG-FE-009 FIX: Dependencies intentionally omitted to run only once on mount
  // The init function captures validateTokenAndGetUser and scheduleTokenRefresh via closure
  // Adding them as dependencies would cause re-runs on every render, breaking authentication
  useEffect(() => {
    if (isInitialized) return // Already initialized

    const initializeAuth = async () => {
      try {
        // SIMPLIFIED: Check if user has valid session cookie
        const userInfo = await validateTokenAndGetUser()
        if (userInfo) {
          setUser(userInfo)
          // Use ref to avoid circular dependency
          if (scheduleTokenRefreshRef.current) {
            scheduleTokenRefreshRef.current()
          }
        } else {
          setUser(null)
        }
      } catch (error) {
        // E2E-008 FIX: Downgrade to warn since auth init "failure" is normal for unauthenticated users
        logger.warn('Auth initialization completed without user', { context: 'AuthContext' })
        setUser(null)
      } finally {
        setIsLoading(false)
        setIsInitialized(true)
      }
    }

    initializeAuth()
  }, []) // eslint-disable-line react-hooks/exhaustive-deps -- See BUG-FE-009 comment above

  // P1 SECURITY FIX: Clean up timers on unmount - runs only once
  // No dependencies = effect only runs on mount/unmount
  useEffect(() => {
    return () => {
      if (refreshTimerRef.current) {
        clearTimeout(refreshTimerRef.current)
        refreshTimerRef.current = null
      }
      if (sessionTimeoutTimerRef.current) {
        clearTimeout(sessionTimeoutTimerRef.current)
        sessionTimeoutTimerRef.current = null
      }
    }
  }, []) // Empty dependency array - only run on mount/unmount

  // P1 SECURITY FIX: Reset session timeout on user activity
  // Using refs prevents memory leaks and infinite loops
  // BUG-FE-009 FIX: logout dependency intentionally omitted to prevent re-creation on every render
  // logout is captured via closure and will use the latest version due to timer execution
  const resetSessionTimeout = useCallback(() => {
    if (sessionTimeoutTimerRef.current) {
      clearTimeout(sessionTimeoutTimerRef.current)
      sessionTimeoutTimerRef.current = null
    }

    if (isAuthenticated) {
      const timer = setTimeout(() => {
        // Session timed out due to inactivity
        logout()
      }, SESSION_TIMEOUT_MS)

      sessionTimeoutTimerRef.current = timer
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps -- See BUG-FE-009 comment above
  }, [isAuthenticated]) // SESSION_TIMEOUT_MS is constant, logout accessed via closure

  // Listen for user activity events to reset session timeout
  useEffect(() => {
    if (!isAuthenticated) return

    const activityEvents = ['mousedown', 'keydown', 'scroll', 'touchstart', 'click']

    activityEvents.forEach(event => {
      document.addEventListener(event, resetSessionTimeout)
    })

    // Initial session timeout
    resetSessionTimeout()

    return () => {
      activityEvents.forEach(event => {
        document.removeEventListener(event, resetSessionTimeout)
      })
    }
  }, [isAuthenticated, resetSessionTimeout])

  const getCsrfToken = async (): Promise<string> => {
    try {
      const response = await fetch('/api/auth/csrf-token', {
        method: 'GET',
        credentials: 'include', // Include httpOnly cookies
      })
      if (!response.ok) {
        throw new Error('Failed to fetch CSRF token')
      }
      const data = await response.json()
      return data.token
    } catch (error) {
      logger.error('CSRF token fetch failed', error, { context: 'AuthContext' })
      throw error
    }
  }

  // BUG-FE-009 FIX: Wrap in useCallback to create stable reference for useEffect dependencies
  // E2E-008 FIX: Don't log errors for expected 401 responses (user not logged in)
  const validateTokenAndGetUser = useCallback(async (): Promise<User | null> => {
    // E2E-008 FIX: Add AbortController with 10s timeout to prevent hanging requests
    const controller = new AbortController()
    const timeoutId = setTimeout(() => controller.abort(), AUTH_CHECK_TIMEOUT_MS)
    try {
      const response = await fetch('/api/auth/me', {
        method: 'GET',
        credentials: 'include', // Include httpOnly cookies
        signal: controller.signal,
      })

      if (response.ok) {
        const result = await response.json()
        if (result.success && result.user) {
          return result.user
        }
      }
      // E2E-008 FIX: 401 is expected when not authenticated, don't log as error
      // Other non-ok responses are still silently handled (return null)
      return null
    } catch (error) {
      // E2E-008 FIX: Only log network/unexpected errors, not expected auth failures
      // Network errors (e.g., offline) should still be logged for debugging
      if (error instanceof DOMException && error.name === 'AbortError') {
        logger.warn('Auth check timed out', { context: 'AuthContext' })
      } else {
        logger.warn('Token validation network error', { context: 'AuthContext', error: String(error) })
      }
      return null
    } finally {
      clearTimeout(timeoutId)
    }
  }, []) // No dependencies - function is stable

  const login = async (
    email: string,
    password: string,
    rememberMe = false
  ): Promise<{ success: boolean; message?: string }> => {
    try {
      setIsLoading(true)

      const csrfToken = await getCsrfToken()

      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-CSRF-TOKEN': csrfToken,
        },
        credentials: 'include', // Include cookies for httpOnly token
        body: JSON.stringify({
          email,
          password,
          rememberMe,
        }),
      })

      const result = await response.json()

      if (response.ok && result.success) {
        // SIMPLIFIED: Just set user, cookies handle authentication
        if (result.user) {
          setUser(result.user)
        }

        // Schedule token refresh
        scheduleTokenRefresh()

        logger.info('Login successful', { context: 'AuthContext', userId: result.user?.id })

        // Track successful login
        trackEvent({
          name: 'sign_in',
          category: 'authentication',
          priority: 'critical',
          properties: {
            method: 'email',
            success: true,
          },
        })

        return { success: true }
      } else {
        return {
          success: false,
          message: result.message || 'Login failed. Please check your credentials.'
        }
      }
    } catch (error) {
      logger.error('Login error', error, { context: 'AuthContext' })
      return {
        success: false,
        message: 'An unexpected error occurred. Please try again.'
      }
    } finally {
      setIsLoading(false)
    }
  }

  const refreshToken = async (): Promise<boolean> => {
    // BUG-FE-015 FIX: If a refresh is already in progress, return the same Promise
    // This ensures all concurrent callers get the same result and only 1 API call is made
    if (isRefreshingRef.current && refreshPromiseRef.current) {
      logger.debug('Token refresh already in progress, returning existing promise', { context: 'AuthContext' })
      return refreshPromiseRef.current
    }

    isRefreshingRef.current = true

    // BUG-FE-015 FIX: Create and store the refresh Promise so concurrent callers can await it
    refreshPromiseRef.current = (async (): Promise<boolean> => {
      try {
        // Get refresh token from httpOnly cookie (handled by browser)
        const response = await fetch('/api/auth/refresh', {
          method: 'POST',
          credentials: 'include', // Include httpOnly cookies
        })

        const result = await response.json()

        if (response.ok && result.success) {
          // SIMPLIFIED: Cookie refreshed by backend, just reschedule next refresh
          // BUG-FE-009 FIX: Use ref to avoid circular dependency
          if (scheduleTokenRefreshRef.current) {
            scheduleTokenRefreshRef.current()
          }
          return true
        } else {
          // Refresh failed, logout user
          await logout()
          return false
        }
      } catch (error) {
        logger.error('Token refresh failed', error, { context: 'AuthContext' })
        await logout()
        return false
      } finally {
        // BUG-FE-015 FIX: Always release lock and clear the Promise ref
        isRefreshingRef.current = false
        refreshPromiseRef.current = null
      }
    })()

    return refreshPromiseRef.current
  }

  const logout = async (logoutFromAllDevices = false): Promise<void> => {
    try {
      setIsLoading(true)

      // BUG-FE-003 FIX: Wait for any in-progress refresh to complete before logging out
      // This prevents race conditions where logout clears timers while refresh is updating them
      let waitCount = 0;
      while (isRefreshingRef.current && waitCount < 50) {
        await new Promise(resolve => setTimeout(resolve, 100));
        waitCount++;
      }

      // P1 SECURITY FIX: Clear all timers using refs
      if (refreshTimerRef.current) {
        clearTimeout(refreshTimerRef.current)
        refreshTimerRef.current = null
      }
      if (sessionTimeoutTimerRef.current) {
        clearTimeout(sessionTimeoutTimerRef.current)
        sessionTimeoutTimerRef.current = null
      }

      // BUG-FE-020 FIX: Clear cached CSRF token to prevent cross-session token reuse
      clearCsrfToken()

      // Call logout endpoint if user is authenticated
      if (user) {
        try {
          // Get CSRF token for logout
          const csrfToken = await getCsrfToken()

          await fetch('/api/auth/logout', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              'X-CSRF-TOKEN': csrfToken,
            },
            credentials: 'include', // Send httpOnly cookie
            body: JSON.stringify({
              logoutFromAllDevices,
            }),
          })
        } catch (error) {
          // Don't throw on logout API error, just log it
          logger.error('Logout API call failed', error, { context: 'AuthContext' })
        }
      }

      // Track logout event
      trackEvent({
        name: 'logout',
        category: 'authentication',
        priority: 'critical',
        properties: {
          logout_from_all_devices: logoutFromAllDevices,
        },
      })

      // Clear state
      setUser(null)

      // E2E-017 FIX: Use window.location.href instead of router.push for logout redirect
      // This ensures a full page reload which re-evaluates middleware without stale cookie
      window.location.href = '/login'
    } catch (error) {
      logger.error('Logout error', error, { context: 'AuthContext' })
    } finally {
      setIsLoading(false)
    }
  }

  const updateUser = (updatedUser: User) => {
    setUser(updatedUser)
  }

  const scheduleTokenRefresh = () => {
    // Clear any existing refresh timer
    if (refreshTimerRef.current) {
      clearTimeout(refreshTimerRef.current)
      refreshTimerRef.current = null
    }

    // SIMPLIFIED: Use constant refresh interval (13 minutes)
    const timer = setTimeout(() => {
      refreshToken()
    }, TOKEN_REFRESH_INTERVAL)

    refreshTimerRef.current = timer
  }

  // BUG-FE-009 FIX: Store function in ref to break circular dependency with initialization code
  scheduleTokenRefreshRef.current = scheduleTokenRefresh

  const value: AuthContextType = {
    user,
    isLoading,
    isInitialized,  // BUG-HIGH-003 FIX: Expose initialization state
    isAuthenticated,
    login,
    logout,
    refreshToken,
    updateUser,
  }

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}