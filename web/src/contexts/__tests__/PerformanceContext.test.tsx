/**
 * Tests for PerformanceContext
 *
 * This file validates performance monitoring and error tracking functionality
 */

import React from 'react'
import { renderHook, waitFor } from '@testing-library/react'
import { PerformanceProvider, usePerformance } from '../PerformanceContext'
import { logger } from '@/utils/logger'

// Mock logger
jest.mock('@/utils/logger', () => ({
  logger: {
    debug: jest.fn(),
    error: jest.fn(),
  },
}))

describe('PerformanceContext', () => {
  let fetchMock: jest.Mock
  let performanceGetEntriesByTypeMock: jest.Mock
  let addEventListenerMock: jest.SpyInstance
  let removeEventListenerMock: jest.SpyInstance

  beforeEach(() => {
    // Mock fetch
    fetchMock = jest.fn(() =>
      Promise.resolve({
        ok: true,
        json: async () => ({}),
      } as Response)
    )
    global.fetch = fetchMock

    // Mock performance.getEntriesByType
    performanceGetEntriesByTypeMock = jest.fn()
    Object.defineProperty(performance, 'getEntriesByType', {
      value: performanceGetEntriesByTypeMock,
      writable: true,
      configurable: true,
    })

    // Spy on addEventListener and removeEventListener
    addEventListenerMock = jest.spyOn(window, 'addEventListener')
    removeEventListenerMock = jest.spyOn(window, 'removeEventListener')

    jest.clearAllMocks()
  })

  afterEach(() => {
    jest.restoreAllMocks()
  })

  describe('Provider initialization', () => {
    it('should provide context value', () => {
      const { result} = renderHook(() => usePerformance(), {
        wrapper: PerformanceProvider,
      })

      expect(result.current).toHaveProperty('trackEvent')
      expect(result.current).toHaveProperty('trackError')
      expect(typeof result.current.trackEvent).toBe('function')
      expect(typeof result.current.trackError).toBe('function')
    })

    it('should not register event listeners in test environment', () => {
      // In Jest, NODE_ENV is 'test', not 'production', so no listeners should be registered
      renderHook(() => usePerformance(), {
        wrapper: PerformanceProvider,
      })

      expect(addEventListenerMock).not.toHaveBeenCalledWith('load', expect.any(Function))
      expect(addEventListenerMock).not.toHaveBeenCalledWith('error', expect.any(Function))
      expect(addEventListenerMock).not.toHaveBeenCalledWith(
        'unhandledrejection',
        expect.any(Function)
      )
    })
  })

  describe('trackEvent', () => {
    it('should not track events in test environment', () => {
      // In test environment (NODE_ENV='test'), tracking should be disabled
      const { result } = renderHook(() => usePerformance(), {
        wrapper: PerformanceProvider,
      })

      result.current.trackEvent('test-event', 100)

      expect(logger.debug).not.toHaveBeenCalled()
      expect(fetchMock).not.toHaveBeenCalled()
    })

    it('should provide trackEvent function', () => {
      const { result } = renderHook(() => usePerformance(), {
        wrapper: PerformanceProvider,
      })

      // Verify the function exists and can be called without errors
      expect(() => result.current.trackEvent('test-event', 100)).not.toThrow()
      expect(() => result.current.trackEvent('test-event')).not.toThrow()
    })
  })

  describe('trackError', () => {
    it('should not track errors in test environment', () => {
      const { result } = renderHook(() => usePerformance(), {
        wrapper: PerformanceProvider,
      })

      const testError = new Error('Test error')
      result.current.trackError(testError, 'test-context')

      expect(logger.error).not.toHaveBeenCalled()
      expect(fetchMock).not.toHaveBeenCalled()
    })

    it('should provide trackError function', () => {
      const { result } = renderHook(() => usePerformance(), {
        wrapper: PerformanceProvider,
      })

      const testError = new Error('Test error')
      // Verify the function exists and can be called without errors
      expect(() => result.current.trackError(testError, 'context')).not.toThrow()
      expect(() => result.current.trackError(testError)).not.toThrow()
    })
  })

  describe('Lifecycle', () => {
    it('should not register event listeners in test environment', () => {
      renderHook(() => usePerformance(), {
        wrapper: PerformanceProvider,
      })

      // In test environment, no listeners should be registered
      expect(addEventListenerMock).not.toHaveBeenCalled()
    })

    it('should cleanup properly on unmount', () => {
      const { unmount } = renderHook(() => usePerformance(), {
        wrapper: PerformanceProvider,
      })

      unmount()

      // In test environment, no listeners were added, so none should be removed
      expect(removeEventListenerMock).not.toHaveBeenCalled()
    })
  })

  describe('usePerformance hook', () => {
    it('should throw error when used outside provider', () => {
      const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation()

      expect(() => {
        renderHook(() => usePerformance())
      }).toThrow('usePerformance must be used within a PerformanceProvider')

      consoleErrorSpy.mockRestore()
    })
  })
})
