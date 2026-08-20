/**
 * Development utilities for performance analysis
 *
 * ESLint note: console.time/timeEnd usage is intentional for performance profiling
 * These methods are only used in development mode for debugging performance issues
 */

/* eslint-disable no-console */

import { logger } from './logger'

export const performanceUtils = {
  // Simple timing utility
  time: (name: string) => {
    if (process.env.NODE_ENV === 'development') {
      console.time(name)
      return () => console.timeEnd(name)
    }
    return () => {}
  },

  // Memory usage tracking (development only)
  memoryUsage: () => {
    if (process.env.NODE_ENV === 'development' && 'memory' in performance) {
      const memory = (performance as any).memory
      return {
        used: Math.round(memory.usedJSHeapSize / 1048576),
        total: Math.round(memory.totalJSHeapSize / 1048576),
        limit: Math.round(memory.jsHeapSizeLimit / 1048576),
      }
    }
    return null
  },

  // Bundle size analysis helper
  logBundleSize: () => {
    if (process.env.NODE_ENV === 'development') {
      // This will help identify large chunks during development
      const entries = performance.getEntriesByType('resource')
      const jsEntries = entries.filter(entry => 
        entry.name.includes('.js') && !entry.name.includes('hot-update')
      )
      
      logger.debug('Bundle Analysis', {
        bundles: jsEntries.map(entry => ({
          name: entry.name,
          sizeKB: (entry.transferSize / 1024).toFixed(2)
        }))
      })
    }
  },

  // Component render timing
  measureRender: <T extends (...args: any[]) => any>(
    componentName: string, 
    renderFn: T
  ): T => {
    if (process.env.NODE_ENV === 'development') {
      return ((...args: Parameters<T>) => {
        const endTimer = performanceUtils.time(`🎨 ${componentName} render`)
        const result = renderFn(...args)
        endTimer()
        return result
      }) as T
    }
    return renderFn
  },

  // API call timing
  measureApiCall: async <T>(
    apiName: string,
    apiCall: () => Promise<T>
  ): Promise<T> => {
    const endTimer = performanceUtils.time(`🌐 API: ${apiName}`)
    try {
      const result = await apiCall()
      endTimer()
      return result
    } catch (error) {
      endTimer()
      if (process.env.NODE_ENV === 'development') {
        logger.error(`API error in ${apiName}`, error, { api: apiName })
      }
      throw error
    }
  }
}

// BUG-FIX: Store interval ID to allow cleanup during hot module replacement
let performanceMonitoringInterval: NodeJS.Timeout | null = null

// Development-only performance monitoring with proper cleanup
export const startPerformanceMonitoring = () => {
  if (process.env.NODE_ENV !== 'development') return

  // Clear any existing interval to prevent duplicates during HMR
  if (performanceMonitoringInterval) {
    clearInterval(performanceMonitoringInterval)
  }

  // Log performance metrics periodically
  performanceMonitoringInterval = setInterval(() => {
    const memory = performanceUtils.memoryUsage()
    if (memory) {
      logger.debug('Memory usage', {
        usedMB: memory.used,
        totalMB: memory.total,
        limitMB: memory.limit
      })
    }
  }, 30000) // Every 30 seconds
}

export const stopPerformanceMonitoring = () => {
  if (performanceMonitoringInterval) {
    clearInterval(performanceMonitoringInterval)
    performanceMonitoringInterval = null
  }
}

// Auto-start in development (can be stopped if needed)
if (process.env.NODE_ENV === 'development') {
  startPerformanceMonitoring()

  // Support for webpack/next.js hot module replacement cleanup
  if (typeof module !== 'undefined' && (module as any).hot) {
    (module as any).hot.dispose(() => {
      stopPerformanceMonitoring()
    })
  }
}