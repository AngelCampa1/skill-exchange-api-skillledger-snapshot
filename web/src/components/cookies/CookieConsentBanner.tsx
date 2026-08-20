'use client'

/**
 * Cookie Consent Banner Component
 *
 * GDPR-compliant cookie consent banner with accessibility support.
 * Displays at the bottom of the screen on first visit.
 */

import React, { useEffect } from'react'
import Link from'next/link'
import { useCookieConsent } from'@/contexts/CookieConsentContext'

export default function CookieConsentBanner() {
  const { consentGiven, hasAsked, giveConsent, revokeConsent, dismissBanner } = useCookieConsent()

  // Handle Escape key to dismiss banner
  useEffect(() => {
    if (consentGiven !== null || hasAsked) return

    const handleEscape = (event: KeyboardEvent) => {
      if (event.key ==='Escape') {
        dismissBanner()
      }
    }

    window.addEventListener('keydown', handleEscape)
    return () => window.removeEventListener('keydown', handleEscape)
  }, [consentGiven, hasAsked, dismissBanner])

  // Don't show banner if consent has been decided or user was already asked
  if (consentGiven !== null || hasAsked) {
    return null
  }

  return (
    <div
      role="banner"
      aria-label="Cookie consent banner"
      className="fixed bottom-0 left-0 right-0 z-50 bg-white  border-t border-gray-200  shadow-lg"
    >
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
          {/* Message */}
          <div className="flex-1 text-sm text-gray-700">
            <p>
              <strong>We use cookies and analytics</strong> to improve your experience, understand how you use
              our platform, and make data-driven improvements. We use Google Analytics and Microsoft Clarity to
              collect anonymized usage data.{''}
              <Link
                href="/privacy"
                className="text-blue-600  hover:underline focus:outline-none focus:ring-2 focus:ring-blue-500 rounded"
              >
                Privacy Policy
              </Link>
            </p>
          </div>

          {/* Action Buttons */}
          <div className="flex gap-3 flex-shrink-0">
            <button
              onClick={revokeConsent}
              className="px-4 py-2 text-sm font-medium text-gray-700  bg-gray-100  hover:bg-gray-200  rounded-full focus:outline-none focus:ring-2 focus:ring-gray-500 transition-colors"
              aria-label="Decline analytics cookies"
            >
              Decline
            </button>
            <button
              onClick={giveConsent}
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-full focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 transition-colors"
              aria-label="Accept analytics cookies"
            >
              Accept
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
