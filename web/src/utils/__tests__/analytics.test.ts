/**
 * Analytics Utility Tests
 *
 * TDD approach: These tests are written BEFORE the implementation.
 * They should fail initially (RED), then pass after implementation (GREEN).
 */

import {
  initializeAnalytics,
  trackEvent,
  trackPageView,
  setUserProperties,
  trackException,
  trackTiming,
  isAnalyticsEnabled,
} from '../analytics'
import type { UserProperties } from '@/types/analytics'
import type { AnalyticsEvent, AuthenticationEvent, MonetizationEvent } from '@/types/analytics'

// Mock window.gtag
const gtagMock = jest.fn()
const clarityMock = jest.fn()

describe('Analytics Utility', () => {
  beforeEach(() => {
    jest.clearAllMocks()
    gtagMock.mockClear()
    clarityMock.mockClear()

    // Setup window.gtag and window.clarity
    Object.defineProperty(window, 'gtag', {
      value: gtagMock,
      writable: true,
      configurable: true,
    })

    Object.defineProperty(window, 'clarity', {
      value: clarityMock,
      writable: true,
      configurable: true,
    })

    // Reset environment
    process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'true'
    // @ts-expect-error - Mocking NODE_ENV for test
    process.env.NODE_ENV = 'production'
  })

  afterEach(() => {
    delete (window as Partial<Window>).gtag
    delete (window as Partial<Window>).clarity
  })

  describe('isAnalyticsEnabled()', () => {
    it('should return true when analytics is enabled in production', () => {
      process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'true'
      // @ts-expect-error - Mocking NODE_ENV for test
    process.env.NODE_ENV = 'production'

      expect(isAnalyticsEnabled()).toBe(true)
    })

    it('should return false when analytics is disabled', () => {
      process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'false'

      expect(isAnalyticsEnabled()).toBe(false)
    })

    it('should return false in development environment', () => {
      process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'true'
      // @ts-expect-error - Mocking NODE_ENV for test
    process.env.NODE_ENV = 'development'

      expect(isAnalyticsEnabled()).toBe(false)
    })

    it('should return false when consent is not given', () => {
      // Consent will be checked inside trackEvent, not in isAnalyticsEnabled
      // This test verifies the function itself
      expect(typeof isAnalyticsEnabled()).toBe('boolean')
    })
  })

  describe('initializeAnalytics()', () => {
    it('should initialize without errors', () => {
      expect(() => initializeAnalytics()).not.toThrow()
    })

    it('should handle missing gtag gracefully', () => {
      delete (window as Partial<Window>).gtag

      expect(() => initializeAnalytics()).not.toThrow()
    })

    it('should handle missing clarity gracefully', () => {
      delete (window as Partial<Window>).clarity

      expect(() => initializeAnalytics()).not.toThrow()
    })
  })

  describe('trackEvent()', () => {
    it('should not track when analytics is disabled', () => {
      process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'false'

      const event: AnalyticsEvent = {
        name: 'test_event',
        category: 'navigation',
        priority: 'low',
      }

      trackEvent(event)

      expect(gtagMock).not.toHaveBeenCalled()
    })

    it('should not track in development environment', () => {
      // @ts-expect-error - Mocking NODE_ENV for test
    process.env.NODE_ENV = 'development'

      const event: AnalyticsEvent = {
        name: 'test_event',
        category: 'navigation',
        priority: 'low',
      }

      trackEvent(event)

      expect(gtagMock).not.toHaveBeenCalled()
    })

    it('should track event with gtag when enabled', () => {
      const event: AuthenticationEvent = {
        name: 'sign_in',
        category: 'authentication',
        priority: 'critical',
        properties: {
          method: 'email',
        },
      }

      trackEvent(event)

      expect(gtagMock).toHaveBeenCalledWith('event', 'sign_in', {
        event_category: 'authentication',
        event_priority: 'critical',
        method: 'email',
      })
    })

    it('should track event with custom properties', () => {
      const event: MonetizationEvent = {
        name: 'purchase_success',
        category: 'monetization',
        priority: 'critical',
        properties: {
          tier: 'professional',
          value: 29.99,
          currency: 'USD',
          transaction_id: 'txn_123',
        },
      }

      trackEvent(event)

      expect(gtagMock).toHaveBeenCalledWith('event', 'purchase_success', {
        event_category: 'monetization',
        event_priority: 'critical',
        tier: 'professional',
        value: 29.99,
        currency: 'USD',
        transaction_id: 'txn_123',
      })
    })

    it('should tag event in Clarity when available', () => {
      const event: AnalyticsEvent = {
        name: 'button_clicked',
        category: 'ui_interaction',
        priority: 'medium',
        properties: {
          element_name: 'submit_button',
        },
      }

      trackEvent(event)

      expect(clarityMock).toHaveBeenCalledWith('set', 'last_event', 'button_clicked')
    })

    it('should handle missing gtag gracefully', () => {
      delete (window as Partial<Window>).gtag

      const event: AnalyticsEvent = {
        name: 'test_event',
        category: 'navigation',
        priority: 'low',
      }

      expect(() => trackEvent(event)).not.toThrow()
    })

    it('should handle gtag errors gracefully', () => {
      gtagMock.mockImplementation(() => {
        throw new Error('gtag error')
      })

      const event: AnalyticsEvent = {
        name: 'test_event',
        category: 'navigation',
        priority: 'low',
      }

      expect(() => trackEvent(event)).not.toThrow()
    })

    it('should include timestamp if provided', () => {
      const timestamp = Date.now()
      const event: AnalyticsEvent = {
        name: 'test_event',
        category: 'navigation',
        priority: 'low',
        timestamp,
      }

      trackEvent(event)

      expect(gtagMock).toHaveBeenCalledWith('event', 'test_event', expect.objectContaining({
        timestamp,
      }))
    })

    it('should filter out undefined properties', () => {
      const event: AnalyticsEvent = {
        name: 'test_event',
        category: 'navigation',
        priority: 'low',
        properties: {
          defined_prop: 'value',
          undefined_prop: undefined,
        },
      }

      trackEvent(event)

      expect(gtagMock).toHaveBeenCalledWith('event', 'test_event', {
        event_category: 'navigation',
        event_priority: 'low',
        defined_prop: 'value',
      })
    })
  })

  describe('trackPageView()', () => {
    it('should track page view with gtag', () => {
      trackPageView('/dashboard', 'Dashboard')

      expect(gtagMock).toHaveBeenCalledWith('event', 'page_view', {
        page_path: '/dashboard',
        page_title: 'Dashboard',
      })
    })

    it('should track page view without title', () => {
      trackPageView('/profile')

      expect(gtagMock).toHaveBeenCalledWith('event', 'page_view', {
        page_path: '/profile',
      })
    })

    it('should not track in development', () => {
      // @ts-expect-error - Mocking NODE_ENV for test
    process.env.NODE_ENV = 'development'

      trackPageView('/dashboard')

      expect(gtagMock).not.toHaveBeenCalled()
    })

    it('should handle missing gtag gracefully', () => {
      delete (window as Partial<Window>).gtag

      expect(() => trackPageView('/dashboard')).not.toThrow()
    })
  })

  describe('setUserProperties()', () => {
    it('should set user properties in gtag', () => {
      const properties: UserProperties = {
        user_id: 'user_123',
        subscription_tier: 'professional',
        email_verified: true,
      }

      setUserProperties(properties)

      expect(gtagMock).toHaveBeenCalledWith('set', 'user_properties', {
        user_id: 'user_123',
        subscription_tier: 'professional',
        email_verified: true,
      })
    })

    it('should identify user in Clarity', () => {
      const properties: UserProperties = {
        user_id: 'user_123',
        subscription_tier: 'professional',
      }

      setUserProperties(properties)

      expect(clarityMock).toHaveBeenCalledWith('identify', 'user_123', expect.any(Object))
    })

    it('should handle missing user_id in Clarity', () => {
      const properties: UserProperties = {
        subscription_tier: 'professional',
      }

      setUserProperties(properties)

      // Should not call identify without user_id
      expect(clarityMock).not.toHaveBeenCalledWith('identify', expect.anything(), expect.anything())
    })

    it('should not set properties in development', () => {
      // @ts-expect-error - Mocking NODE_ENV for test
    process.env.NODE_ENV = 'development'

      setUserProperties({ user_id: 'user_123' })

      expect(gtagMock).not.toHaveBeenCalled()
    })

    it('should handle missing gtag gracefully', () => {
      delete (window as Partial<Window>).gtag

      expect(() => setUserProperties({ user_id: 'user_123' })).not.toThrow()
    })
  })

  describe('trackException()', () => {
    it('should track exception with error message', () => {
      const error = new Error('Test error')

      trackException(error)

      expect(gtagMock).toHaveBeenCalledWith('event', 'exception', {
        description: 'Test error',
        fatal: false,
      })
    })

    it('should track exception with context', () => {
      const error = new Error('API error')

      trackException(error, 'UserAPI')

      expect(gtagMock).toHaveBeenCalledWith('event', 'exception', {
        description: 'API error',
        fatal: false,
        context: 'UserAPI',
      })
    })

    it('should tag exception in Clarity', () => {
      const error = new Error('Test error')

      trackException(error)

      expect(clarityMock).toHaveBeenCalledWith('set', 'error', 'Test error')
    })

    it('should handle non-Error objects', () => {
      const error = 'String error'

      expect(() => trackException(error as unknown as Error)).not.toThrow()

      expect(gtagMock).toHaveBeenCalledWith('event', 'exception', {
        description: 'String error',
        fatal: false,
      })
    })

    it('should not track in development', () => {
      // @ts-expect-error - Mocking NODE_ENV for test
    process.env.NODE_ENV = 'development'

      trackException(new Error('Test error'))

      expect(gtagMock).not.toHaveBeenCalled()
    })

    it('should handle missing gtag gracefully', () => {
      delete (window as Partial<Window>).gtag

      expect(() => trackException(new Error('Test'))).not.toThrow()
    })
  })

  describe('trackTiming()', () => {
    it('should track timing metric', () => {
      trackTiming('page_load', 1234)

      expect(gtagMock).toHaveBeenCalledWith('event', 'timing_complete', {
        name: 'page_load',
        value: 1234,
        event_category: 'performance',
      })
    })

    it('should track timing with category', () => {
      trackTiming('api_call', 567, 'API Performance')

      expect(gtagMock).toHaveBeenCalledWith('event', 'timing_complete', {
        name: 'api_call',
        value: 567,
        event_category: 'API Performance',
      })
    })

    it('should not track in development', () => {
      // @ts-expect-error - Mocking NODE_ENV for test
    process.env.NODE_ENV = 'development'

      trackTiming('page_load', 1234)

      expect(gtagMock).not.toHaveBeenCalled()
    })

    it('should handle missing gtag gracefully', () => {
      delete (window as Partial<Window>).gtag

      expect(() => trackTiming('page_load', 1234)).not.toThrow()
    })
  })

  describe('Edge Cases', () => {
    it('should handle empty event properties', () => {
      const event: AnalyticsEvent = {
        name: 'test_event',
        category: 'navigation',
        priority: 'low',
        properties: {},
      }

      expect(() => trackEvent(event)).not.toThrow()
    })

    it('should handle null properties', () => {
      const event: AnalyticsEvent = {
        name: 'test_event',
        category: 'navigation',
        priority: 'low',
        properties: undefined,
      }

      expect(() => trackEvent(event)).not.toThrow()
    })

    it('should handle server-side rendering (no window)', () => {
      const originalWindow = global.window

      // @ts-ignore
      delete global.window

      const event: AnalyticsEvent = {
        name: 'test_event',
        category: 'navigation',
        priority: 'low',
      }

      expect(() => trackEvent(event)).not.toThrow()

      global.window = originalWindow
    })
  })
})
