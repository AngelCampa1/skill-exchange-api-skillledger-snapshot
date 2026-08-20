'use client'

/**
 * Page View Tracker Component
 *
 * Automatically tracks page views and route changes using Next.js navigation.
 */

import { useEffect, useRef } from 'react'
import { usePathname, useSearchParams } from 'next/navigation'
import { useAnalytics } from '@/hooks/useAnalytics'
import { trackEvent } from '@/utils/analytics'

export default function PageViewTracker() {
  const pathname = usePathname()
  const searchParams = useSearchParams()
  const { trackPageView } = useAnalytics()
  const previousPathRef = useRef<string | null>(null)
  const navigationStartTimeRef = useRef<number>(Date.now())

  useEffect(() => {
    if (pathname) {
      // Construct full URL with search params
      const url = searchParams.toString()
        ? `${pathname}?${searchParams.toString()}`
        : pathname

      // Get page title from document
      const title = typeof document !== 'undefined' ? document.title : undefined

      // Track page view (existing functionality)
      trackPageView(url, title)

      // Track route change if not first page
      if (previousPathRef.current && previousPathRef.current !== pathname) {
        const navigationTime = Date.now() - navigationStartTimeRef.current

        trackEvent({
          name: 'route_change',
          category: 'navigation',
          priority: 'medium',
          properties: {
            from_path: previousPathRef.current,
            to_path: pathname,
            navigation_time: navigationTime,
            has_search_params: !!searchParams.toString(),
          },
        })
      }

      // Update refs for next navigation
      previousPathRef.current = pathname
      navigationStartTimeRef.current = Date.now()
    }
  }, [pathname, searchParams, trackPageView])

  // This component doesn't render anything
  return null
}
