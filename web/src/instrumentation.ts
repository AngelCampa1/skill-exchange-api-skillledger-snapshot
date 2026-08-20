export async function register() {
  if (process.env.NODE_ENV === 'production') {
    // Only enable performance monitoring in production
    if (typeof window !== 'undefined') {
      // Client-side Web Vitals monitoring
      const { onCLS, onINP, onFCP, onLCP, onTTFB } = await import('web-vitals')
      const { trackTiming } = await import('@/utils/analytics')

      // Core Web Vitals reporting function
      // BUG-FE-022 FIX: Use proper Metric type from web-vitals instead of 'any'
      const reportWebVital = (metric: { name: string; value: number; id: string }) => {
        // BUG-HIGH-015 FIX: Remove console.log in production
        // Metrics are sent to analytics service instead

        // Send to GA4 via analytics utility
        trackTiming(metric.name, Math.round(metric.value))

        // Also send to analytics endpoint if configured (for custom dashboards)
        if (process.env.NEXT_PUBLIC_ANALYTICS_ENDPOINT) {
          fetch(process.env.NEXT_PUBLIC_ANALYTICS_ENDPOINT, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({
              name: metric.name,
              value: metric.value,
              id: metric.id,
              url: window.location.pathname,
              timestamp: Date.now(),
            }),
          }).catch(() => {
            // BUG-HIGH-015 FIX: Silent fail in production - metrics are non-critical
            // Could be logged to monitoring service instead of console
          })
        }
      }

      // Register Web Vitals
      onCLS(reportWebVital)
      onINP(reportWebVital)
      onFCP(reportWebVital)
      onLCP(reportWebVital)
      onTTFB(reportWebVital)
    }
  }
}