'use client'

import { logger } from '@/utils/logger';

import React, { createContext, useContext, useEffect } from 'react'

interface PerformanceContextType {
  trackEvent: (name: string, value?: number) => void
  trackError: (error: Error, context?: string) => void
}

const PerformanceContext = createContext<PerformanceContextType | undefined>(undefined)

export function PerformanceProvider({ children }: { children: React.ReactNode }) {
  // Note: In tests, process.env.NODE_ENV cannot be reliably mocked due to Next.js
  // build-time replacement. Tests should mock this module if needed.
  const isProduction = process.env.NODE_ENV === 'production'

  const trackEvent = (name: string, value: number = 1) => {
    if (isProduction) {
      // In production, send to analytics
      const metric = {
        name,
        value,
        url: window.location.pathname,
        timestamp: Date.now(),
        userAgent: navigator.userAgent,
      }

      logger.debug('Performance Event', { metric })

      if (process.env.NEXT_PUBLIC_ANALYTICS_ENDPOINT) {
        fetch(process.env.NEXT_PUBLIC_ANALYTICS_ENDPOINT, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(metric),
        }).catch((error: unknown) => logger.error('Performance tracking failed', error, { context: 'PerformanceContext' }))
      }
    }
  }

  const trackError = (error: Error, context?: string) => {
    if (isProduction) {
      const errorMetric = {
        name: 'error',
        message: error.message,
        stack: error.stack,
        context,
        url: window.location.pathname,
        timestamp: Date.now(),
        userAgent: navigator.userAgent,
      }

      logger.error('Performance Error', undefined, { errorMetric, context: 'PerformanceContext' })

      if (process.env.NEXT_PUBLIC_ANALYTICS_ENDPOINT) {
        fetch(process.env.NEXT_PUBLIC_ANALYTICS_ENDPOINT, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(errorMetric),
        }).catch((error: unknown) => logger.error('Performance tracking failed', error, { context: 'PerformanceContext' }))
      }
    }
  }

  useEffect(() => {
    if (isProduction) {
      // Define handlers outside so they can be removed
      const handleLoad = () => {
        // BUG-HIGH-008 FIX: Safely access array index with bounds check
        const navEntries = performance.getEntriesByType('navigation')
        const navTiming = navEntries.length > 0
          ? navEntries[0] as PerformanceNavigationTiming
          : null

        if (navTiming) {
          const metrics = {
            'page-load-time': navTiming.loadEventEnd - navTiming.fetchStart,
            'dns-lookup-time': navTiming.domainLookupEnd - navTiming.domainLookupStart,
            'connection-time': navTiming.connectEnd - navTiming.connectStart,
            'request-time': navTiming.responseEnd - navTiming.requestStart,
            'dom-load-time': navTiming.domContentLoadedEventEnd - navTiming.domContentLoadedEventStart,
          }

          Object.entries(metrics).forEach(([name, value]) => {
            if (value > 0) {
              trackEvent(name, value)
            }
          })
        }
      }

      const handleError = (event: ErrorEvent) => {
        trackError(new Error(event.message), event.filename)
      }

      const handleUnhandledRejection = (event: PromiseRejectionEvent) => {
        trackError(new Error(event.reason), 'unhandled-promise')
      }

      // Track page load performance
      window.addEventListener('load', handleLoad)

      // Track unhandled errors for performance impact
      window.addEventListener('error', handleError)
      window.addEventListener('unhandledrejection', handleUnhandledRejection)

      // Cleanup function to prevent memory leaks
      return () => {
        window.removeEventListener('load', handleLoad)
        window.removeEventListener('error', handleError)
        window.removeEventListener('unhandledrejection', handleUnhandledRejection)
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <PerformanceContext.Provider value={{ trackEvent, trackError }}>
      {children}
    </PerformanceContext.Provider>
  )
}

export function usePerformance() {
  const context = useContext(PerformanceContext)
  if (context === undefined) {
    throw new Error('usePerformance must be used within a PerformanceProvider')
  }
  return context
}