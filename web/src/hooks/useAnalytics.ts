'use client'

/**
 * useAnalytics Hook
 *
 * React hook for tracking analytics events with consent checking.
 * Auto-identifies authenticated users when consent is given.
 */

import { useCallback, useEffect } from 'react'
import { useAuth } from '@/contexts/AuthContext'
import { useCookieConsent } from '@/contexts/CookieConsentContext'
import { trackEvent as analyticsTrackEvent, trackPageView as analyticsTrackPageView, setUserProperties } from '@/utils/analytics'
import type { AnalyticsEvent } from '@/types/analytics'

export function useAnalytics() {
  const { user } = useAuth()
  const { consentGiven } = useCookieConsent()

  /**
   * Identify the current user with analytics
   */
  const identify = useCallback(() => {
    if (!consentGiven || !user) return

    setUserProperties({
      user_id: user.id,
      email_verified: user.emailVerified,
      tax_compliant: user.taxCompliant,
      roles: user.roles?.join(','),
    })
  }, [consentGiven, user])

  /**
   * Track a custom event
   */
  const trackEvent = useCallback(
    (event: AnalyticsEvent) => {
      if (!consentGiven) return

      analyticsTrackEvent(event)
    },
    [consentGiven]
  )

  /**
   * Track a page view
   */
  const trackPageView = useCallback(
    (url: string, title?: string) => {
      if (!consentGiven) return

      analyticsTrackPageView(url, title)
    },
    [consentGiven]
  )

  // Auto-identify user when they authenticate and consent is given
  useEffect(() => {
    if (consentGiven && user) {
      identify()
    }
  }, [consentGiven, user, identify])

  return {
    trackEvent,
    trackPageView,
    identify,
  }
}
