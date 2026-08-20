'use client'

import { useState, useEffect, useCallback, useRef } from 'react'

const SESSION_KEY = 'exit_intent_shown'
const DISMISS_KEY = 'exit_intent_dismissed_at'
const DISMISS_DAYS = 7
const MIN_TIME_ON_SITE_MS = 20_000
const MOBILE_INACTIVITY_MS = 30_000

export function useExitIntent() {
  const [showPopup, setShowPopup] = useState(false)
  const firedRef = useRef(false)
  const pageLoadTimeRef = useRef(Date.now())
  const lastActivityRef = useRef(Date.now())
  const mobileTimerRef = useRef<ReturnType<typeof setInterval> | null>(null)

  const canShow = useCallback((): boolean => {
    if (typeof window === 'undefined') return false
    if (firedRef.current) return false

    // Not shown in current session
    if (sessionStorage.getItem(SESSION_KEY)) return false

    // Not dismissed in last 7 days
    const dismissedAt = localStorage.getItem(DISMISS_KEY)
    if (dismissedAt) {
      const elapsed = Date.now() - Number(dismissedAt)
      if (elapsed < DISMISS_DAYS * 24 * 60 * 60 * 1000) return false
    }

    // Must have been on site for at least 20 seconds
    if (Date.now() - pageLoadTimeRef.current < MIN_TIME_ON_SITE_MS) return false

    return true
  }, [])

  const triggerPopup = useCallback(() => {
    if (!canShow()) return
    // Don't show if another modal/dialog is already open
    if (document.querySelector('[role="dialog"][aria-modal="true"], [role="alertdialog"][aria-modal="true"], [data-radix-dialog-overlay]')) return
    firedRef.current = true
    sessionStorage.setItem(SESSION_KEY, 'true')
    setShowPopup(true)
    // Clear mobile inactivity timer once popup fires
    if (mobileTimerRef.current) {
      clearInterval(mobileTimerRef.current)
      mobileTimerRef.current = null
    }
  }, [canShow])

  const dismissPopup = useCallback(() => {
    setShowPopup(false)
    localStorage.setItem(DISMISS_KEY, String(Date.now()))
  }, [])

  useEffect(() => {
    if (typeof window === 'undefined') return

    // Desktop: mouseleave on documentElement
    const handleMouseLeave = (e: MouseEvent) => {
      if (e.clientY <= 0) {
        triggerPopup()
      }
    }

    // Mobile: track activity
    const handleActivity = () => {
      lastActivityRef.current = Date.now()
    }

    document.documentElement.addEventListener('mouseleave', handleMouseLeave)
    window.addEventListener('scroll', handleActivity, { passive: true })
    window.addEventListener('touchstart', handleActivity, { passive: true })

    // Mobile inactivity timer
    mobileTimerRef.current = setInterval(() => {
      if (Date.now() - lastActivityRef.current >= MOBILE_INACTIVITY_MS) {
        triggerPopup()
      }
    }, 5000)

    return () => {
      document.documentElement.removeEventListener('mouseleave', handleMouseLeave)
      window.removeEventListener('scroll', handleActivity)
      window.removeEventListener('touchstart', handleActivity)
      if (mobileTimerRef.current) clearInterval(mobileTimerRef.current)
    }
  }, [triggerPopup])

  return { showPopup, dismissPopup }
}
