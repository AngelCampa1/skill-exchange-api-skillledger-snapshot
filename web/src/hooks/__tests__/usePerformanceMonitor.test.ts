/**
 * usePerformanceMonitor.ts Tests
 *
 * Tests performance monitoring hook with PerformanceObserver integration.
 * Focus: Production-only behavior, metric collection, analytics integration, measure utility.
 *
 * Coverage Target: 85%+ (87 lines)
 * Test Count: 15 tests
 */

import { renderHook, act } from '@testing-library/react';
import { usePerformanceMonitor } from '../usePerformanceMonitor';
import { logger } from '@/utils/logger';

// Mock logger
jest.mock('@/utils/logger', () => ({
  logger: {
    debug: jest.fn(),
    error: jest.fn(),
    warn: jest.fn(),
  },
}));

// Mock fetch
global.fetch = jest.fn();

describe('usePerformanceMonitor - Production Behavior', () => {
  const originalEnv = process.env.NODE_ENV;
  let mockPerformanceObserver: jest.Mock;
  let mockObserverInstance: {
    observe: jest.Mock;
    disconnect: jest.Mock;
  };
  let performanceCallback: ((list: PerformanceObserverEntryList) => void) | null;

  beforeEach(() => {
    // Set production environment
    (process.env as any).NODE_ENV = 'production';

    // Initialize callback as null
    performanceCallback = null;

    // Create mock observer instance
    mockObserverInstance = {
      observe: jest.fn(),
      disconnect: jest.fn(),
    };

    // Mock PerformanceObserver constructor
    mockPerformanceObserver = jest.fn((callback) => {
      performanceCallback = callback;
      return mockObserverInstance;
    });
    global.PerformanceObserver = mockPerformanceObserver as any;

    jest.clearAllMocks();
  });

  afterEach(() => {
    (process.env as any).NODE_ENV = originalEnv;
  });

  it('should create PerformanceObserver in production', () => {
    renderHook(() => usePerformanceMonitor('TestComponent'));

    expect(mockPerformanceObserver).toHaveBeenCalledWith(expect.any(Function));
    expect(mockObserverInstance.observe).toHaveBeenCalledWith({
      entryTypes: ['measure', 'navigation', 'resource', 'paint'],
    });
  });

  it('should log performance metrics when entries are observed', () => {
    renderHook(() => usePerformanceMonitor('TestComponent'));

    // Simulate performance entries
    const mockEntries = [
      {
        name: 'test-measure',
        duration: 123.45,
        startTime: 0,
      },
      {
        name: 'test-navigation',
        duration: 0,
        startTime: 456.78,
      },
    ];

    const mockList = {
      getEntries: () => mockEntries,
    } as PerformanceObserverEntryList;

    // Trigger the observer callback
    act(() => {
      if (performanceCallback) {
        performanceCallback(mockList);
      }
    });

    expect(logger.debug).toHaveBeenCalledTimes(2);
    expect(logger.debug).toHaveBeenCalledWith('Performance Metric', {
      metric: {
        name: 'test-measure',
        value: 123.45,
        component: 'TestComponent',
        timestamp: expect.any(Number),
      },
    });
    expect(logger.debug).toHaveBeenCalledWith('Performance Metric', {
      metric: {
        name: 'test-navigation',
        value: 456.78, // startTime used when duration is 0
        component: 'TestComponent',
        timestamp: expect.any(Number),
      },
    });
  });

  it('should send metrics to analytics endpoint when configured', async () => {
    const originalAnalyticsEndpoint = process.env.NEXT_PUBLIC_ANALYTICS_ENDPOINT;
    process.env.NEXT_PUBLIC_ANALYTICS_ENDPOINT = 'https://analytics.example.com/metrics';

    (global.fetch as jest.Mock).mockResolvedValue({ ok: true });

    renderHook(() => usePerformanceMonitor('TestComponent'));

    const mockEntries = [
      {
        name: 'test-metric',
        duration: 100,
        startTime: 0,
      },
    ];

    const mockList = {
      getEntries: () => mockEntries,
    } as PerformanceObserverEntryList;

    act(() => {
      if (performanceCallback) {
        performanceCallback(mockList);
      }
    });

    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 0));
    });

    expect(global.fetch).toHaveBeenCalledTimes(1);
    const fetchCall = (global.fetch as jest.Mock).mock.calls[0];
    expect(fetchCall[0]).toBe('https://analytics.example.com/metrics');
    expect(fetchCall[1].method).toBe('POST');
    expect(fetchCall[1].headers).toEqual({ 'Content-Type': 'application/json' });

    const body = JSON.parse(fetchCall[1].body);
    expect(body).toEqual({
      name: 'test-metric',
      value: 100,
      component: 'TestComponent',
      timestamp: expect.any(Number),
    });

    process.env.NEXT_PUBLIC_ANALYTICS_ENDPOINT = originalAnalyticsEndpoint;
  });

  it('should handle analytics endpoint fetch errors gracefully', async () => {
    const originalAnalyticsEndpoint = process.env.NEXT_PUBLIC_ANALYTICS_ENDPOINT;
    process.env.NEXT_PUBLIC_ANALYTICS_ENDPOINT = 'https://analytics.example.com/metrics';

    const fetchError = new Error('Network error');
    (global.fetch as jest.Mock).mockRejectedValue(fetchError);

    renderHook(() => usePerformanceMonitor('TestComponent'));

    const mockEntries = [
      {
        name: 'test-metric',
        duration: 100,
        startTime: 0,
      },
    ];

    const mockList = {
      getEntries: () => mockEntries,
    } as PerformanceObserverEntryList;

    act(() => {
      if (performanceCallback) {
        performanceCallback(mockList);
      }
    });

    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 0));
    });

    expect(logger.error).toHaveBeenCalledWith(
      'Performance monitoring failed',
      fetchError,
      { hook: 'usePerformanceMonitor' }
    );

    process.env.NEXT_PUBLIC_ANALYTICS_ENDPOINT = originalAnalyticsEndpoint;
  });

  it('should disconnect observer on unmount', () => {
    const { unmount } = renderHook(() => usePerformanceMonitor('TestComponent'));

    expect(mockObserverInstance.disconnect).not.toHaveBeenCalled();

    unmount();

    expect(mockObserverInstance.disconnect).toHaveBeenCalledTimes(1);
  });

  it('should handle PerformanceObserver.observe errors gracefully', () => {
    mockObserverInstance.observe.mockImplementation(() => {
      throw new Error('entryTypes not supported');
    });

    renderHook(() => usePerformanceMonitor('TestComponent'));

    expect(logger.warn).toHaveBeenCalledWith('PerformanceObserver not supported', {
      hook: 'usePerformanceMonitor',
      error: expect.any(Error),
    });
  });

  it('should work without componentName parameter', () => {
    renderHook(() => usePerformanceMonitor());

    const mockEntries = [
      {
        name: 'test-measure',
        duration: 100,
        startTime: 0,
      },
    ];

    const mockList = {
      getEntries: () => mockEntries,
    } as PerformanceObserverEntryList;

    act(() => {
      if (performanceCallback) {
        performanceCallback(mockList);
      }
    });

    expect(logger.debug).toHaveBeenCalledWith('Performance Metric', {
      metric: {
        name: 'test-measure',
        value: 100,
        component: undefined,
        timestamp: expect.any(Number),
      },
    });
  });
});

describe('usePerformanceMonitor - Non-Production Behavior', () => {
  const originalEnv = process.env.NODE_ENV;

  beforeEach(() => {
    (process.env as any).NODE_ENV = 'development';
    jest.clearAllMocks();
  });

  afterEach(() => {
    (process.env as any).NODE_ENV = originalEnv;
  });

  it('should not create PerformanceObserver in non-production', () => {
    const mockObserve = jest.fn();
    const mockPerformanceObserver = jest.fn(() => ({
      observe: mockObserve,
      disconnect: jest.fn(),
    }));
    global.PerformanceObserver = mockPerformanceObserver as any;

    renderHook(() => usePerformanceMonitor('TestComponent'));

    expect(mockPerformanceObserver).not.toHaveBeenCalled();
    expect(mockObserve).not.toHaveBeenCalled();
  });
});

describe('usePerformanceMonitor - measurePerformance Utility', () => {
  const originalEnv = process.env.NODE_ENV;
  let mockMark: jest.Mock;
  let mockMeasure: jest.Mock;

  beforeEach(() => {
    (process.env as any).NODE_ENV = 'production';

    // Mock performance.mark and performance.measure
    mockMark = jest.fn();
    mockMeasure = jest.fn();
    global.performance.mark = mockMark;
    global.performance.measure = mockMeasure;

    // Mock PerformanceObserver to prevent actual observer creation
    global.PerformanceObserver = jest.fn(() => ({
      observe: jest.fn(),
      disconnect: jest.fn(),
    })) as any;

    jest.clearAllMocks();
  });

  afterEach(() => {
    (process.env as any).NODE_ENV = originalEnv;
  });

  it('should measure synchronous function performance', () => {
    const { result } = renderHook(() => usePerformanceMonitor());

    const syncFn = jest.fn(() => 'result');

    const returnValue = result.current.measurePerformance('sync-operation', syncFn as any);

    expect(syncFn).toHaveBeenCalledTimes(1);
    expect(returnValue).toBe('result');
    expect(mockMark).toHaveBeenCalledWith('sync-operation-start');
    expect(mockMark).toHaveBeenCalledWith('sync-operation-end');
    expect(mockMeasure).toHaveBeenCalledWith(
      'sync-operation-duration',
      'sync-operation-start',
      'sync-operation-end'
    );
  });

  it('should measure asynchronous function performance on success', async () => {
    const { result } = renderHook(() => usePerformanceMonitor());

    const asyncFn = jest.fn(async () => {
      await new Promise((resolve) => setTimeout(resolve, 10));
      return 'async-result';
    });

    const promise = result.current.measurePerformance('async-operation', asyncFn as any);

    expect(promise).toBeInstanceOf(Promise);

    const returnValue = await promise;

    expect(asyncFn).toHaveBeenCalledTimes(1);
    expect(returnValue).toBe('async-result');
    expect(mockMark).toHaveBeenCalledWith('async-operation-start');
    expect(mockMark).toHaveBeenCalledWith('async-operation-end');
    expect(mockMeasure).toHaveBeenCalledWith(
      'async-operation-duration',
      'async-operation-start',
      'async-operation-end'
    );
  });

  it('should measure asynchronous function performance on error', async () => {
    const { result } = renderHook(() => usePerformanceMonitor());

    const error = new Error('Async error');
    const asyncFn = jest.fn(async () => {
      await new Promise((resolve) => setTimeout(resolve, 10));
      throw error;
    });

    const promise = result.current.measurePerformance('async-error-operation', asyncFn);

    await expect(promise).rejects.toThrow('Async error');

    expect(asyncFn).toHaveBeenCalledTimes(1);
    expect(mockMark).toHaveBeenCalledWith('async-error-operation-start');
    expect(mockMark).toHaveBeenCalledWith('async-error-operation-end');
    expect(mockMeasure).toHaveBeenCalledWith(
      'async-error-operation-duration',
      'async-error-operation-start',
      'async-error-operation-end'
    );
  });

  it('should not measure performance in non-production', () => {
    (process.env as any).NODE_ENV = 'development';

    const { result } = renderHook(() => usePerformanceMonitor());

    const syncFn = jest.fn(() => 'result');

    const returnValue = result.current.measurePerformance('dev-operation', syncFn as any);

    expect(syncFn).toHaveBeenCalledTimes(1);
    expect(returnValue).toBe('result');
    expect(mockMark).not.toHaveBeenCalled();
    expect(mockMeasure).not.toHaveBeenCalled();
  });

  it('should not measure async performance in non-production', async () => {
    (process.env as any).NODE_ENV = 'development';

    const { result } = renderHook(() => usePerformanceMonitor());

    const asyncFn = jest.fn(async () => 'result');

    const returnValue = await result.current.measurePerformance('dev-async-operation', asyncFn as any);

    expect(asyncFn).toHaveBeenCalledTimes(1);
    expect(returnValue).toBe('result');
    expect(mockMark).not.toHaveBeenCalled();
    expect(mockMeasure).not.toHaveBeenCalled();
  });
});
