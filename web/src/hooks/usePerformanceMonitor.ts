import { logger } from '@/utils/logger';
import { useEffect } from 'react'

interface PerformanceMetric {
  name: string
  value: number
  component?: string
  timestamp: number
}

export function usePerformanceMonitor(componentName?: string) {
  useEffect(() => {
    if (process.env.NODE_ENV !== 'production') {
      return
    }

    const observer = new PerformanceObserver((list) => {
      const entries = list.getEntries()
      entries.forEach((entry) => {
        const metric: PerformanceMetric = {
          name: entry.name,
          value: entry.duration || entry.startTime,
          component: componentName,
          timestamp: Date.now(),
        }

        // Log performance metrics (in production, send to analytics)
        logger.debug('Performance Metric', { metric })
        
        // Example: Send to analytics endpoint
        if (process.env.NEXT_PUBLIC_ANALYTICS_ENDPOINT) {
          fetch(process.env.NEXT_PUBLIC_ANALYTICS_ENDPOINT, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify(metric),
          }).catch((error: unknown) => logger.error('Performance monitoring failed', error, { hook: 'usePerformanceMonitor' }))
        }
      })
    })

    // Observe different types of performance entries
    try {
      observer.observe({ entryTypes: ['measure', 'navigation', 'resource', 'paint'] })
    } catch (error) {
      logger.warn('PerformanceObserver not supported', { hook: 'usePerformanceMonitor', error })
    }

    return () => {
      observer.disconnect()
    }
  }, [componentName])

  // Utility function to measure custom metrics
  const measurePerformance = (name: string, fn: () => void | Promise<void>) => {
    if (process.env.NODE_ENV !== 'production') {
      return fn()
    }

    const startMark = `${name}-start`
    const endMark = `${name}-end`
    const measureName = `${name}-duration`

    performance.mark(startMark)
    
    const result = fn()
    
    if (result instanceof Promise) {
      return result.then((value) => {
        performance.mark(endMark)
        performance.measure(measureName, startMark, endMark)
        return value
      }).catch((error) => {
        performance.mark(endMark)
        performance.measure(measureName, startMark, endMark)
        throw error
      })
    } else {
      performance.mark(endMark)
      performance.measure(measureName, startMark, endMark)
      return result
    }
  }

  return { measurePerformance }
}