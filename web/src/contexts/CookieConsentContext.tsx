'use client'

/**
 * Cookie Consent Context
 *
 * Manages GDPR-compliant cookie consent for analytics tracking.
 * Implements consent-first architecture - no tracking before explicit user consent.
 */

/* eslint-disable no-console */
// Console warnings are intentional for consent tracking and debugging

import React, { createContext, useContext, useState, useEffect, useCallback } from 'react'
import type { ConsentState } from '@/types/analytics'

interface CookieConsentContextType {
  /** Current consent state: null = not asked, true = granted, false = denied */
  consentGiven: boolean | null
  /** Whether the user has been asked for consent */
  hasAsked: boolean
  /** Grant analytics consent */
  giveConsent: () => void
  /** Revoke analytics consent */
  revokeConsent: () => void
  /** Dismiss banner without making a choice */
  dismissBanner: () => void
}

const CookieConsentContext = createContext<CookieConsentContextType | undefined>(undefined)

const STORAGE_KEY = 'cookie-consent'

/**
 * Emit consent state to Google Analytics consent mode
 */
function emitConsentToGA(consent: 'granted' | 'denied'): void {
  if (typeof window !== 'undefined' && window.gtag) {
    try {
      window.gtag('consent', 'update', {
        analytics_storage: consent,
      })
    } catch (error) {
      // Silently fail - analytics is non-critical
      console.error('Failed to update GA consent:', error)
    }
  }
}

/**
 * Save consent to localStorage
 */
function saveConsent(value: boolean | null): void {
  if (typeof window === 'undefined') return

  try {
    if (value === null) {
      localStorage.removeItem(STORAGE_KEY)
    } else {
      localStorage.setItem(STORAGE_KEY, value ? 'granted' : 'denied')
    }
  } catch (error) {
    // LocalStorage might be unavailable (private browsing, etc.)
    console.error('Failed to save consent to localStorage:', error)
  }
}

/**
 * Load consent from localStorage
 */
function loadConsent(): boolean | null {
  if (typeof window === 'undefined') return null

  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored === 'granted') return true
    if (stored === 'denied') return false
    return null
  } catch (error) {
    console.error('Failed to load consent from localStorage:', error)
    return null
  }
}

/**
 * Check if Do Not Track is enabled
 */
function isDoNotTrackEnabled(): boolean {
  if (typeof navigator === 'undefined') return false

  // Check various DNT headers/properties
  return (
    navigator.doNotTrack === '1' ||
    (window as Window & { doNotTrack?: string }).doNotTrack === '1' ||
    (navigator as Navigator & { msDoNotTrack?: string }).msDoNotTrack === '1'
  )
}

export function CookieConsentProvider({ children }: { children: React.ReactNode }) {
  const [consentGiven, setConsentGiven] = useState<boolean | null>(null)
  const [hasAsked, setHasAsked] = useState(false)
  const [initialized, setInitialized] = useState(false)

  // Initialize consent state on mount
  useEffect(() => {
    if (initialized) return

    // Check Do Not Track first
    if (isDoNotTrackEnabled()) {
      setConsentGiven(false)
      setHasAsked(true)
      saveConsent(false)
      emitConsentToGA('denied')
      setInitialized(true)
      return
    }

    // Load from localStorage
    const stored = loadConsent()
    if (stored !== null) {
      setConsentGiven(stored)
      setHasAsked(true)
      emitConsentToGA(stored ? 'granted' : 'denied')
    }

    setInitialized(true)
  }, [initialized])

  const giveConsent = useCallback(() => {
    setConsentGiven(true)
    setHasAsked(true)
    saveConsent(true)
    emitConsentToGA('granted')
  }, [])

  const revokeConsent = useCallback(() => {
    setConsentGiven(false)
    setHasAsked(true)
    saveConsent(false)
    emitConsentToGA('denied')
  }, [])

  const dismissBanner = useCallback(() => {
    setHasAsked(true)
  }, [])

  const value: CookieConsentContextType = {
    consentGiven,
    hasAsked,
    giveConsent,
    revokeConsent,
    dismissBanner,
  }

  return <CookieConsentContext.Provider value={value}>{children}</CookieConsentContext.Provider>
}

/**
 * Hook to access cookie consent state
 */
export function useCookieConsent(): CookieConsentContextType {
  const context = useContext(CookieConsentContext)

  if (context === undefined) {
    throw new Error('useCookieConsent must be used within a CookieConsentProvider')
  }

  return context
}
