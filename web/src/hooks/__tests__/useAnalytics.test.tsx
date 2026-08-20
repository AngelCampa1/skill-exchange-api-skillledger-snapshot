/**
 * useAnalytics Hook Tests
 *
 * TDD approach: These tests are written BEFORE the implementation.
 * They should fail initially (RED), then pass after implementation (GREEN).
 */

import React from 'react'
import { renderHook, act } from '@testing-library/react'
import { useAnalytics } from '../useAnalytics'
import { CookieConsentProvider } from '@/contexts/CookieConsentContext'
import * as analyticsModule from '@/utils/analytics'

// Mock the analytics utility
jest.mock('@/utils/analytics', () => ({
  trackEvent: jest.fn(),
  trackPageView: jest.fn(),
  setUserProperties: jest.fn(),
  isAnalyticsEnabled: jest.fn(() => true),
}))

// Mock AuthContext
const mockUser = {
  id: 'user_123',
  email: 'test@example.com',
  emailVerified: true,
  taxCompliant: true,
  roles: ['user'],
}

jest.mock('@/contexts/AuthContext', () => ({
  useAuth: jest.fn(() => ({
    user: mockUser,
    isAuthenticated: true,
  })),
}))

// Mock localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {}

  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => {
      store[key] = value.toString()
    },
    removeItem: (key: string) => {
      delete store[key]
    },
    clear: () => {
      store = {}
    },
  }
})()

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
})

// Mock gtag
const gtagMock = jest.fn()
Object.defineProperty(window, 'gtag', {
  value: gtagMock,
  writable: true,
})

function renderHookWithProvider<T>(hook: () => T) {
  return renderHook(hook, {
    wrapper: ({ children }: { children: React.ReactNode }) => (
      <CookieConsentProvider>{children}</CookieConsentProvider>
    ),
  })
}

describe('useAnalytics Hook', () => {
  beforeEach(() => {
    localStorage.clear()
    jest.clearAllMocks()
    gtagMock.mockClear()
    // Reset environment
    process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'true'
    // @ts-expect-error - Mocking NODE_ENV for test
    process.env.NODE_ENV = 'production'
    // Reset Do Not Track
    Object.defineProperty(navigator, 'doNotTrack', {
      value: '0',
      writable: true,
      configurable: true,
    })
  })

  describe('Hook Return Values', () => {
    it('should return trackEvent function', () => {
      const { result } = renderHookWithProvider(() => useAnalytics())

      expect(result.current.trackEvent).toBeDefined()
      expect(typeof result.current.trackEvent).toBe('function')
    })

    it('should return trackPageView function', () => {
      const { result } = renderHookWithProvider(() => useAnalytics())

      expect(result.current.trackPageView).toBeDefined()
      expect(typeof result.current.trackPageView).toBe('function')
    })

    it('should return identify function', () => {
      const { result } = renderHookWithProvider(() => useAnalytics())

      expect(result.current.identify).toBeDefined()
      expect(typeof result.current.identify).toBe('function')
    })
  })

  describe('Consent Checking', () => {
    it('should respect consent state when tracking events', () => {
      // No consent given
      const { result } = renderHookWithProvider(() => useAnalytics())

      const event = {
        name: 'test_event',
        category: 'navigation' as const,
        priority: 'low' as const,
      }

      act(() => {
        result.current.trackEvent(event)
      })

      // Should not track without consent
      expect(analyticsModule.trackEvent).not.toHaveBeenCalled()
    })

    it('should track events after consent is granted', () => {
      localStorage.setItem('cookie-consent', 'granted')

      const { result } = renderHookWithProvider(() => useAnalytics())

      const event = {
        name: 'test_event',
        category: 'navigation' as const,
        priority: 'low' as const,
      }

      act(() => {
        result.current.trackEvent(event)
      })

      expect(analyticsModule.trackEvent).toHaveBeenCalledWith(event)
    })

    it('should not track when consent is denied', () => {
      localStorage.setItem('cookie-consent', 'denied')

      const { result } = renderHookWithProvider(() => useAnalytics())

      const event = {
        name: 'test_event',
        category: 'navigation' as const,
        priority: 'low' as const,
      }

      act(() => {
        result.current.trackEvent(event)
      })

      expect(analyticsModule.trackEvent).not.toHaveBeenCalled()
    })
  })

  describe('Auto-Identify User', () => {
    it('should auto-identify user when authenticated and consent given', () => {
      localStorage.setItem('cookie-consent', 'granted')

      renderHookWithProvider(() => useAnalytics())

      // Should automatically set user properties
      expect(analyticsModule.setUserProperties).toHaveBeenCalledWith(
        expect.objectContaining({
          user_id: 'user_123',
          email_verified: true,
          tax_compliant: true,
        })
      )
    })

    it('should not identify user without consent', () => {
      renderHookWithProvider(() => useAnalytics())

      expect(analyticsModule.setUserProperties).not.toHaveBeenCalled()
    })

    it('should include roles in user properties', () => {
      localStorage.setItem('cookie-consent', 'granted')

      renderHookWithProvider(() => useAnalytics())

      expect(analyticsModule.setUserProperties).toHaveBeenCalledWith(
        expect.objectContaining({
          roles: 'user',
        })
      )
    })
  })

  describe('Manual Identify', () => {
    it('should allow manual user identification', () => {
      localStorage.setItem('cookie-consent', 'granted')

      const { result } = renderHookWithProvider(() => useAnalytics())

      act(() => {
        result.current.identify()
      })

      // Should be called at least once (auto + manual)
      expect(analyticsModule.setUserProperties).toHaveBeenCalled()
    })

    it('should not identify without consent', () => {
      const { result } = renderHookWithProvider(() => useAnalytics())

      act(() => {
        result.current.identify()
      })

      expect(analyticsModule.setUserProperties).not.toHaveBeenCalled()
    })
  })

  describe('trackEvent', () => {
    it('should call analytics.trackEvent with correct parameters', () => {
      localStorage.setItem('cookie-consent', 'granted')

      const { result } = renderHookWithProvider(() => useAnalytics())

      const event = {
        name: 'button_clicked',
        category: 'ui_interaction' as const,
        priority: 'medium' as const,
        properties: {
          button_name: 'submit',
        },
      }

      act(() => {
        result.current.trackEvent(event)
      })

      expect(analyticsModule.trackEvent).toHaveBeenCalledWith(event)
    })

    it('should handle multiple event calls', () => {
      localStorage.setItem('cookie-consent', 'granted')

      const { result } = renderHookWithProvider(() => useAnalytics())

      act(() => {
        result.current.trackEvent({
          name: 'event_1',
          category: 'navigation' as const,
          priority: 'low' as const,
        })
        result.current.trackEvent({
          name: 'event_2',
          category: 'navigation' as const,
          priority: 'low' as const,
        })
      })

      expect(analyticsModule.trackEvent).toHaveBeenCalledTimes(2)
    })
  })

  describe('trackPageView', () => {
    it('should call analytics.trackPageView with correct parameters', () => {
      localStorage.setItem('cookie-consent', 'granted')

      const { result } = renderHookWithProvider(() => useAnalytics())

      act(() => {
        result.current.trackPageView('/dashboard', 'Dashboard')
      })

      expect(analyticsModule.trackPageView).toHaveBeenCalledWith('/dashboard', 'Dashboard')
    })

    it('should not track page view without consent', () => {
      const { result } = renderHookWithProvider(() => useAnalytics())

      act(() => {
        result.current.trackPageView('/dashboard')
      })

      expect(analyticsModule.trackPageView).not.toHaveBeenCalled()
    })
  })

  describe('Hook Stability', () => {
    it('should return stable function references', () => {
      localStorage.setItem('cookie-consent', 'granted')

      const { result, rerender } = renderHookWithProvider(() => useAnalytics())

      const firstTrackEvent = result.current.trackEvent
      const firstTrackPageView = result.current.trackPageView
      const firstIdentify = result.current.identify

      rerender()

      // Function references should be stable (using useCallback)
      expect(result.current.trackEvent).toBe(firstTrackEvent)
      expect(result.current.trackPageView).toBe(firstTrackPageView)
      expect(result.current.identify).toBe(firstIdentify)
    })
  })

  describe('Edge Cases', () => {
    it('should handle user being null (not authenticated)', () => {
      // Clear all mocks including setUserProperties from previous tests
      jest.clearAllMocks()

      const useAuthMock = require('@/contexts/AuthContext').useAuth
      useAuthMock.mockImplementation(() => ({
        user: null,
        isAuthenticated: false,
      }))

      localStorage.setItem('cookie-consent', 'granted')

      renderHookWithProvider(() => useAnalytics())

      // Should not crash, but also shouldn't set user properties
      expect(analyticsModule.setUserProperties).not.toHaveBeenCalled()

      // Restore original mock
      useAuthMock.mockImplementation(() => ({
        user: mockUser,
        isAuthenticated: true,
      }))
    })

    it('should handle consent changing from denied to granted', () => {
      // Start with denied consent
      localStorage.setItem('cookie-consent', 'denied')

      const { result, unmount } = renderHookWithProvider(() => useAnalytics())

      const event = {
        name: 'test_event',
        category: 'navigation' as const,
        priority: 'low' as const,
      }

      act(() => {
        result.current.trackEvent(event)
      })

      expect(analyticsModule.trackEvent).not.toHaveBeenCalled()

      // Unmount and re-render with granted consent
      unmount()
      localStorage.setItem('cookie-consent', 'granted')

      const { result: newResult } = renderHookWithProvider(() => useAnalytics())

      act(() => {
        newResult.current.trackEvent(event)
      })

      // Now should track
      expect(analyticsModule.trackEvent).toHaveBeenCalledWith(event)
    })

    it('should handle missing user properties gracefully', () => {
      const useAuthMock = require('@/contexts/AuthContext').useAuth
      useAuthMock.mockReturnValueOnce({
        user: {
          id: 'user_456',
          // Missing other properties
        },
        isAuthenticated: true,
      })

      localStorage.setItem('cookie-consent', 'granted')

      expect(() => {
        renderHookWithProvider(() => useAnalytics())
      }).not.toThrow()
    })
  })
})
