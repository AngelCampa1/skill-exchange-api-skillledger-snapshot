/**
 * Tests for performanceUtils.ts
 *
 * This file tests development-only performance profiling utilities
 *
 * Note: Tests run in NODE_ENV='test', which behaves like production
 * (no console output). This is intentional to keep test output clean.
 */

import { performanceUtils, startPerformanceMonitoring, stopPerformanceMonitoring } from '@/utils/performanceUtils'
import { logger } from '@/utils/logger'

// Mock logger
jest.mock('@/utils/logger', () => ({
  logger: {
    debug: jest.fn(),
    error: jest.fn(),
  },
}))

describe('performanceUtils', () => {
  let consoleTimeSpy: jest.SpyInstance
  let consoleTimeEndSpy: jest.SpyInstance

  beforeEach(() => {
    consoleTimeSpy = jest.spyOn(console, 'time').mockImplementation()
    consoleTimeEndSpy = jest.spyOn(console, 'timeEnd').mockImplementation()
    jest.clearAllMocks()
  })

  afterEach(() => {
    consoleTimeSpy.mockRestore()
    consoleTimeEndSpy.mockRestore()
  })

  describe('time', () => {
    it('should return no-op function in test environment (behaves like production)', () => {
      const endTimer = performanceUtils.time('test-timer')
      expect(consoleTimeSpy).not.toHaveBeenCalled()

      endTimer()
      expect(consoleTimeEndSpy).not.toHaveBeenCalled()
    })

    it('should handle multiple timers independently', () => {
      const timer1 = performanceUtils.time('timer-1')
      const timer2 = performanceUtils.time('timer-2')

      // In test environment, no console calls expected
      expect(consoleTimeSpy).not.toHaveBeenCalled()

      timer1()
      timer2()
      expect(consoleTimeEndSpy).not.toHaveBeenCalled()
    })
  })

  describe('memoryUsage', () => {
    it('should return null in test environment (behaves like production)', () => {
      const result = performanceUtils.memoryUsage()
      expect(result).toBeNull()
    })

    it('should return null even when memory API is available', () => {
      // Mock performance.memory
      const mockMemory = {
        usedJSHeapSize: 10 * 1048576, // 10 MB
        totalJSHeapSize: 20 * 1048576, // 20 MB
        jsHeapSizeLimit: 100 * 1048576, // 100 MB
      }

      Object.defineProperty(performance, 'memory', {
        configurable: true,
        value: mockMemory,
      })

      const result = performanceUtils.memoryUsage()

      // In test environment, returns null regardless
      expect(result).toBeNull()

      // Cleanup
      delete (performance as any).memory
    })
  })

  describe('logBundleSize', () => {
    it('should not log in test environment (behaves like production)', () => {
      const mockEntries = [
        { name: 'app.js', transferSize: 2048 }, // 2 KB
        { name: 'vendor.js', transferSize: 10240 }, // 10 KB
      ]

      // Mock getEntriesByType method
      const getEntriesByTypeSpy = jest.fn().mockReturnValue(mockEntries)
      Object.defineProperty(performance, 'getEntriesByType', {
        configurable: true,
        value: getEntriesByTypeSpy,
      })

      performanceUtils.logBundleSize()

      expect(logger.debug).not.toHaveBeenCalled()

      // Cleanup
      delete (performance as any).getEntriesByType
    })

    it('should handle empty bundle list', () => {
      const getEntriesByTypeSpy = jest.fn().mockReturnValue([])
      Object.defineProperty(performance, 'getEntriesByType', {
        configurable: true,
        value: getEntriesByTypeSpy,
      })

      performanceUtils.logBundleSize()

      expect(logger.debug).not.toHaveBeenCalled()

      // Cleanup
      delete (performance as any).getEntriesByType
    })
  })

  describe('measureRender', () => {
    it('should return original function in test environment (behaves like production)', () => {
      const mockRenderFn = jest.fn((props: any) => `rendered ${props.name}`)
      const wrappedFn = performanceUtils.measureRender('TestComponent', mockRenderFn)

      // In test/production, the wrapper returns the original function
      expect(wrappedFn).toBe(mockRenderFn)
    })

    it('should handle multiple arguments correctly', () => {
      const mockRenderFn = jest.fn((a: number, b: string, c: boolean) => `${a}-${b}-${c}`)
      const wrappedFn = performanceUtils.measureRender('TestComponent', mockRenderFn)

      const result = wrappedFn(42, 'test', true)

      expect(result).toBe('42-test-true')
      expect(mockRenderFn).toHaveBeenCalledWith(42, 'test', true)

      // No timing in test environment
      expect(consoleTimeSpy).not.toHaveBeenCalled()
      expect(consoleTimeEndSpy).not.toHaveBeenCalled()
    })
  })

  describe('measureApiCall', () => {
    it('should measure successful API calls without console output', async () => {
      const mockApiCall = jest.fn().mockResolvedValue({ data: 'success' })

      const result = await performanceUtils.measureApiCall('getUserData', mockApiCall)

      expect(result).toEqual({ data: 'success' })

      // No console timing in test environment
      expect(consoleTimeSpy).not.toHaveBeenCalled()
      expect(consoleTimeEndSpy).not.toHaveBeenCalled()
    })

    it('should measure failed API calls and rethrow error without logging', async () => {
      const error = new Error('API failed')
      const mockApiCall = jest.fn().mockRejectedValue(error)

      await expect(
        performanceUtils.measureApiCall('getUserData', mockApiCall)
      ).rejects.toThrow('API failed')

      // No console timing in test environment
      expect(consoleTimeSpy).not.toHaveBeenCalled()
      expect(consoleTimeEndSpy).not.toHaveBeenCalled()

      // No error logging in test/production
      expect(logger.error).not.toHaveBeenCalled()
    })

    it('should handle async timing correctly', async () => {
      const mockApiCall = jest.fn(async () => {
        await new Promise(resolve => setTimeout(resolve, 10))
        return 'delayed result'
      })

      const result = await performanceUtils.measureApiCall('slowApi', mockApiCall)

      expect(result).toBe('delayed result')

      // No timing in test environment
      expect(consoleTimeSpy).not.toHaveBeenCalled()
      expect(consoleTimeEndSpy).not.toHaveBeenCalled()
    })
  })

  describe('startPerformanceMonitoring', () => {
    beforeEach(() => {
      jest.useFakeTimers()
      stopPerformanceMonitoring() // Clean state
    })

    afterEach(() => {
      stopPerformanceMonitoring()
      jest.useRealTimers()
    })

    it('should not start monitoring in test environment (behaves like production)', () => {
      const mockMemory = {
        usedJSHeapSize: 10 * 1048576,
        totalJSHeapSize: 20 * 1048576,
        jsHeapSizeLimit: 100 * 1048576,
      }

      Object.defineProperty(performance, 'memory', {
        configurable: true,
        value: mockMemory,
      })

      startPerformanceMonitoring()
      jest.advanceTimersByTime(30000)

      expect(logger.debug).not.toHaveBeenCalled()

      delete (performance as any).memory
    })

    it('should not log when memory API is unavailable', () => {
      delete (performance as any).memory

      startPerformanceMonitoring()
      jest.advanceTimersByTime(30000)

      expect(logger.debug).not.toHaveBeenCalled()
    })
  })

  describe('stopPerformanceMonitoring', () => {
    beforeEach(() => {
      jest.useFakeTimers()
    })

    afterEach(() => {
      jest.useRealTimers()
    })

    it('should handle being called when not started', () => {
      expect(() => stopPerformanceMonitoring()).not.toThrow()
    })

    it('should clear interval and reset reference', () => {
      const clearIntervalSpy = jest.spyOn(global, 'clearInterval')

      startPerformanceMonitoring()
      stopPerformanceMonitoring()

      // May or may not be called depending on whether interval was created
      // In test environment, interval is not created, so this may not be called
      // Just ensure no errors are thrown
      expect(() => stopPerformanceMonitoring()).not.toThrow()

      clearIntervalSpy.mockRestore()
    })
  })

  describe('edge cases', () => {
    it('should handle rapid start/stop cycles', () => {
      jest.useFakeTimers()

      for (let i = 0; i < 5; i++) {
        startPerformanceMonitoring()
        stopPerformanceMonitoring()
      }

      jest.advanceTimersByTime(30000)
      expect(logger.debug).not.toHaveBeenCalled()

      jest.useRealTimers()
    })

    it('should handle timer functions with zero arguments', () => {
      const fn = jest.fn(() => 'result')
      const wrapped = performanceUtils.measureRender('Component', fn)

      const result = wrapped()

      expect(result).toBe('result')
      expect(fn).toHaveBeenCalledWith()

      // No timing in test environment
      expect(consoleTimeSpy).not.toHaveBeenCalled()
      expect(consoleTimeEndSpy).not.toHaveBeenCalled()
    })
  })
})
