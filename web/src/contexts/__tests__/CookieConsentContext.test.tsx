/**
 * Cookie Consent Context Tests
 *
 * TDD approach: These tests are written BEFORE the implementation.
 * They should fail initially (RED), then pass after implementation (GREEN).
 */

import { render, screen, act, waitFor } from '@testing-library/react'
import { renderHook } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { CookieConsentProvider, useCookieConsent } from '../CookieConsentContext'

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

// Mock gtag function
const gtagMock = jest.fn()
Object.defineProperty(window, 'gtag', {
  value: gtagMock,
  writable: true,
})

describe('CookieConsentContext', () => {
  beforeEach(() => {
    localStorage.clear()
    jest.clearAllMocks()
    gtagMock.mockClear()
    // Reset Do Not Track
    Object.defineProperty(navigator, 'doNotTrack', {
      value: '0',
      writable: true,
      configurable: true,
    })
  })

  describe('Initial State', () => {
    it('should start with consent as null when not previously set', () => {
      const { result } = renderHook(() => useCookieConsent(), {
        wrapper: CookieConsentProvider,
      })

      expect(result.current.consentGiven).toBeNull()
      expect(result.current.hasAsked).toBe(false)
    })

    it('should load consent from localStorage if previously granted', () => {
      localStorage.setItem('cookie-consent', 'granted')

      const { result } = renderHook(() => useCookieConsent(), {
        wrapper: CookieConsentProvider,
      })

      expect(result.current.consentGiven).toBe(true)
      expect(result.current.hasAsked).toBe(true)
    })

    it('should load consent from localStorage if previously denied', () => {
      localStorage.setItem('cookie-consent', 'denied')

      const { result } = renderHook(() => useCookieConsent(), {
        wrapper: CookieConsentProvider,
      })

      expect(result.current.consentGiven).toBe(false)
      expect(result.current.hasAsked).toBe(true)
    })

    it('should auto-decline consent when Do Not Track is enabled', () => {
      Object.defineProperty(navigator, 'doNotTrack', {
        value: '1',
        writable: true,
        configurable: true,
      })

      const { result } = renderHook(() => useCookieConsent(), {
        wrapper: CookieConsentProvider,
      })

      expect(result.current.consentGiven).toBe(false)
      expect(result.current.hasAsked).toBe(true)
      expect(localStorage.getItem('cookie-consent')).toBe('denied')
    })
  })

  describe('giveConsent()', () => {
    it('should set consent to true', () => {
      const { result } = renderHook(() => useCookieConsent(), {
        wrapper: CookieConsentProvider,
      })

      act(() => {
        result.current.giveConsent()
      })

      expect(result.current.consentGiven).toBe(true)
      expect(result.current.hasAsked).toBe(true)
    })

    it('should save granted consent to localStorage', () => {
      const { result } = renderHook(() => useCookieConsent(), {
        wrapper: CookieConsentProvider,
      })

      act(() => {
        result.current.giveConsent()
      })

      expect(localStorage.getItem('cookie-consent')).toBe('granted')
    })

    it('should emit consent granted event to gtag when available', () => {
      const { result } = renderHook(() => useCookieConsent(), {
        wrapper: CookieConsentProvider,
      })

      act(() => {
        result.current.giveConsent()
      })

      expect(gtagMock).toHaveBeenCalledWith('consent', 'update', {
        analytics_storage: 'granted',
      })
    })
  })

  describe('revokeConsent()', () => {
    it('should set consent to false', () => {
      const { result } = renderHook(() => useCookieConsent(), {
        wrapper: CookieConsentProvider,
      })

      act(() => {
        result.current.revokeConsent()
      })

      expect(result.current.consentGiven).toBe(false)
      expect(result.current.hasAsked).toBe(true)
    })

    it('should save denied consent to localStorage', () => {
      const { result } = renderHook(() => useCookieConsent(), {
        wrapper: CookieConsentProvider,
      })

      act(() => {
        result.current.revokeConsent()
      })

      expect(localStorage.getItem('cookie-consent')).toBe('denied')
    })

    it('should emit consent denied event to gtag when available', () => {
      const { result } = renderHook(() => useCookieConsent(), {
        wrapper: CookieConsentProvider,
      })

      act(() => {
        result.current.revokeConsent()
      })

      expect(gtagMock).toHaveBeenCalledWith('consent', 'update', {
        analytics_storage: 'denied',
      })
    })

    it('should revoke previously granted consent', () => {
      localStorage.setItem('cookie-consent', 'granted')

      const { result } = renderHook(() => useCookieConsent(), {
        wrapper: CookieConsentProvider,
      })

      expect(result.current.consentGiven).toBe(true)

      act(() => {
        result.current.revokeConsent()
      })

      expect(result.current.consentGiven).toBe(false)
      expect(localStorage.getItem('cookie-consent')).toBe('denied')
    })
  })

  describe('dismissBanner()', () => {
    it('should mark as asked without changing consent', () => {
      const { result } = renderHook(() => useCookieConsent(), {
        wrapper: CookieConsentProvider,
      })

      expect(result.current.consentGiven).toBeNull()
      expect(result.current.hasAsked).toBe(false)

      act(() => {
        result.current.dismissBanner()
      })

      expect(result.current.consentGiven).toBeNull()
      expect(result.current.hasAsked).toBe(true)
    })
  })

  describe('Edge Cases', () => {
    it('should handle missing gtag gracefully', () => {
      // Remove gtag
      delete (window as Partial<Window>).gtag

      const { result } = renderHook(() => useCookieConsent(), {
        wrapper: CookieConsentProvider,
      })

      expect(() => {
        act(() => {
          result.current.giveConsent()
        })
      }).not.toThrow()

      expect(result.current.consentGiven).toBe(true)
    })

    it('should handle invalid localStorage value', () => {
      localStorage.setItem('cookie-consent', 'invalid-value')

      const { result } = renderHook(() => useCookieConsent(), {
        wrapper: CookieConsentProvider,
      })

      // Should default to null for invalid values
      expect(result.current.consentGiven).toBeNull()
    })

    it('should handle localStorage being unavailable', () => {
      // Mock localStorage methods to throw
      const setItemSpy = jest.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
        throw new Error('localStorage unavailable')
      })

      const { result } = renderHook(() => useCookieConsent(), {
        wrapper: CookieConsentProvider,
      })

      expect(() => {
        act(() => {
          result.current.giveConsent()
        })
      }).not.toThrow()

      setItemSpy.mockRestore()
    })
  })

  describe('Context Provider', () => {
    it('should throw error when used outside provider', () => {
      // Suppress console.error for this test
      const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {})

      expect(() => {
        renderHook(() => useCookieConsent())
      }).toThrow('useCookieConsent must be used within a CookieConsentProvider')

      consoleSpy.mockRestore()
    })
  })
})
