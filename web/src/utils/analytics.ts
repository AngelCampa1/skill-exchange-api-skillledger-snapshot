/**
 * Analytics Utility
 *
 * Core analytics tracking functions for Google Analytics 4 and Microsoft Clarity.
 * Implements consent-first architecture and production-only tracking.
 */

/* eslint-disable no-console */
// Console errors are intentional for analytics error tracking and debugging

import type { AnalyticsEvent, UserProperties } from '@/types/analytics'

/**
 * Check if analytics is enabled
 * Only enabled in production with NEXT_PUBLIC_ENABLE_ANALYTICS=true
 */
export function isAnalyticsEnabled(): boolean {
  if (typeof window === 'undefined') return false

  const enabled = process.env.NEXT_PUBLIC_ENABLE_ANALYTICS === 'true'
  const isProduction = process.env.NODE_ENV === 'production'

  return enabled && isProduction
}

/**
 * Initialize analytics (currently a no-op, initialization happens via scripts)
 */
export function initializeAnalytics(): void {
  // Analytics scripts are loaded via AnalyticsScripts component
  // This function exists for API compatibility and future extensions
  if (!isAnalyticsEnabled()) return

  try {
    // Future: Set default config, initialize custom dimensions, etc.
  } catch (error) {
    console.error('Failed to initialize analytics:', error)
  }
}

/**
 * Filter out undefined values from an object
 */
function filterUndefined(obj: Record<string, unknown>): Record<string, unknown> {
  const filtered: Record<string, unknown> = {}

  for (const [key, value] of Object.entries(obj)) {
    if (value !== undefined) {
      filtered[key] = value
    }
  }

  return filtered
}

/**
 * Track a custom event
 */
export function trackEvent(event: AnalyticsEvent): void {
  if (!isAnalyticsEnabled()) return

  try {
    // Prepare event parameters
    const params: Record<string, unknown> = {
      event_category: event.category,
      event_priority: event.priority,
      ...event.properties,
    }

    if (event.timestamp) {
      params.timestamp = event.timestamp
    }

    // Filter out undefined values
    const filteredParams = filterUndefined(params)

    // Send to Google Analytics
    if (typeof window !== 'undefined' && window.gtag) {
      window.gtag('event', event.name, filteredParams)
    }

    // Tag in Microsoft Clarity
    if (typeof window !== 'undefined' && window.clarity) {
      window.clarity('set', 'last_event', event.name)
    }
  } catch (error) {
    // Analytics errors should not break the application
    console.error('Failed to track event:', error)
  }
}

/**
 * Track a page view
 */
export function trackPageView(url: string, title?: string): void {
  if (!isAnalyticsEnabled()) return

  try {
    const params: Record<string, string> = {
      page_path: url,
    }

    if (title) {
      params.page_title = title
    }

    // Send to Google Analytics
    if (typeof window !== 'undefined' && window.gtag) {
      window.gtag('event', 'page_view', params)
    }
  } catch (error) {
    console.error('Failed to track page view:', error)
  }
}

/**
 * Set user properties for analytics
 */
export function setUserProperties(properties: UserProperties): void {
  if (!isAnalyticsEnabled()) return

  // Set in Google Analytics
  try {
    if (typeof window !== 'undefined' && window.gtag) {
      window.gtag('set', 'user_properties', properties)
    }
  } catch (error) {
    console.error('Failed to set user properties in GA:', error)
  }

  // Identify in Microsoft Clarity (separate try-catch to not be affected by gtag errors)
  try {
    if (typeof window !== 'undefined' && window.clarity && properties.user_id) {
      // Extract user_id and convert to string
      const userId = String(properties.user_id)
      // Pass remaining properties as session data
      const { user_id, ...sessionData } = properties
      window.clarity('identify', userId, sessionData)
    }
  } catch (error) {
    console.error('Failed to identify user in Clarity:', error)
  }
}

/**
 * Track an exception/error
 */
export function trackException(error: Error | unknown, context?: string): void {
  if (!isAnalyticsEnabled()) return

  try {
    const errorMessage = error instanceof Error ? error.message : String(error)

    const params: Record<string, unknown> = {
      description: errorMessage,
      fatal: false,
    }

    if (context) {
      params.context = context
    }

    // Send to Google Analytics
    if (typeof window !== 'undefined' && window.gtag) {
      window.gtag('event', 'exception', params)
    }
  } catch (err) {
    console.error('Failed to track exception:', err)
  }

  // Tag in Microsoft Clarity (separate try-catch to not be affected by gtag errors)
  try {
    const errorMessage = error instanceof Error ? error.message : String(error)

    if (typeof window !== 'undefined' && window.clarity) {
      window.clarity('set', 'error', errorMessage)
    }
  } catch (err) {
    console.error('Failed to tag error in Clarity:', err)
  }
}

/**
 * Track a timing/performance metric
 */
export function trackTiming(name: string, value: number, category: string = 'performance'): void {
  if (!isAnalyticsEnabled()) return

  try {
    // Send to Google Analytics
    if (typeof window !== 'undefined' && window.gtag) {
      window.gtag('event', 'timing_complete', {
        name,
        value,
        event_category: category,
      })
    }
  } catch (error) {
    console.error('Failed to track timing:', error)
  }
}
