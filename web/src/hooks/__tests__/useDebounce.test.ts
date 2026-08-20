/**
 * Tests for useDebounce hook
 *
 * This file validates the debounce hook functionality
 */

import { renderHook, act } from '@testing-library/react'
import { useDebounce } from '../useDebounce'

describe('useDebounce', () => {
  beforeEach(() => {
    jest.useFakeTimers()
  })

  afterEach(() => {
    jest.useRealTimers()
  })

  it('should return initial value immediately', () => {
    const { result } = renderHook(() => useDebounce('initial', 300))

    expect(result.current).toBe('initial')
  })

  it('should debounce value changes', () => {
    const { result, rerender } = renderHook(
      ({ value, delay }) => useDebounce(value, delay),
      { initialProps: { value: 'initial', delay: 300 } }
    )

    expect(result.current).toBe('initial')

    // Update value
    rerender({ value: 'updated', delay: 300 })

    // Value should not update immediately
    expect(result.current).toBe('initial')

    // Fast-forward time by 150ms (half the delay)
    act(() => {
      jest.advanceTimersByTime(150)
    })

    // Value should still not be updated
    expect(result.current).toBe('initial')

    // Fast-forward time by remaining 150ms
    act(() => {
      jest.advanceTimersByTime(150)
    })

    // Value should now be updated
    expect(result.current).toBe('updated')
  })

  it('should use default delay of 300ms', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value),
      { initialProps: { value: 'initial' } }
    )

    rerender({ value: 'updated' })

    expect(result.current).toBe('initial')

    act(() => {
      jest.advanceTimersByTime(299)
    })

    expect(result.current).toBe('initial')

    act(() => {
      jest.advanceTimersByTime(1)
    })

    expect(result.current).toBe('updated')
  })

  it('should reset timer on rapid value changes', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: 'initial' } }
    )

    // First update
    rerender({ value: 'update1' })

    act(() => {
      jest.advanceTimersByTime(100)
    })

    // Second update before first completes
    rerender({ value: 'update2' })

    act(() => {
      jest.advanceTimersByTime(100)
    })

    // Third update before second completes
    rerender({ value: 'final' })

    // Value should still be initial
    expect(result.current).toBe('initial')

    // Wait for full delay
    act(() => {
      jest.advanceTimersByTime(300)
    })

    // Value should be the last value
    expect(result.current).toBe('final')
  })

  it('should handle custom delay values', () => {
    const { result, rerender } = renderHook(
      ({ value, delay }) => useDebounce(value, delay),
      { initialProps: { value: 'initial', delay: 500 } }
    )

    rerender({ value: 'updated', delay: 500 })

    act(() => {
      jest.advanceTimersByTime(499)
    })

    expect(result.current).toBe('initial')

    act(() => {
      jest.advanceTimersByTime(1)
    })

    expect(result.current).toBe('updated')
  })

  it('should work with different data types', () => {
    // Test with number
    const { result: numberResult, rerender: numberRerender } = renderHook(
      ({ value }) => useDebounce(value, 100),
      { initialProps: { value: 0 } }
    )

    numberRerender({ value: 42 })

    act(() => {
      jest.advanceTimersByTime(100)
    })

    expect(numberResult.current).toBe(42)

    // Test with boolean
    const { result: boolResult, rerender: boolRerender } = renderHook(
      ({ value }) => useDebounce(value, 100),
      { initialProps: { value: false } }
    )

    boolRerender({ value: true })

    act(() => {
      jest.advanceTimersByTime(100)
    })

    expect(boolResult.current).toBe(true)

    // Test with object
    const { result: objResult, rerender: objRerender } = renderHook(
      ({ value }) => useDebounce(value, 100),
      { initialProps: { value: { key: 'initial' } } }
    )

    const newObj = { key: 'updated' }
    objRerender({ value: newObj })

    act(() => {
      jest.advanceTimersByTime(100)
    })

    expect(objResult.current).toEqual(newObj)

    // Test with array
    const { result: arrResult, rerender: arrRerender } = renderHook(
      ({ value }) => useDebounce(value, 100),
      { initialProps: { value: [1, 2, 3] } }
    )

    const newArr = [4, 5, 6]
    arrRerender({ value: newArr })

    act(() => {
      jest.advanceTimersByTime(100)
    })

    expect(arrResult.current).toEqual(newArr)
  })

  it('should handle null and undefined values', () => {
    const { result: nullResult, rerender: nullRerender } = renderHook(
      ({ value }) => useDebounce(value, 100),
      { initialProps: { value: 'initial' as string | null } }
    )

    nullRerender({ value: null })

    act(() => {
      jest.advanceTimersByTime(100)
    })

    expect(nullResult.current).toBeNull()

    const { result: undefinedResult, rerender: undefinedRerender } = renderHook(
      ({ value }) => useDebounce(value, 100),
      { initialProps: { value: 'initial' as string | undefined } }
    )

    undefinedRerender({ value: undefined })

    act(() => {
      jest.advanceTimersByTime(100)
    })

    expect(undefinedResult.current).toBeUndefined()
  })

  it('should cleanup timeout on unmount', () => {
    const { result, rerender, unmount } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: 'initial' } }
    )

    rerender({ value: 'updated' })

    // Unmount before delay completes
    unmount()

    // Advance timers
    act(() => {
      jest.advanceTimersByTime(300)
    })

    // Value should still be initial since component was unmounted
    expect(result.current).toBe('initial')
  })

  it('should update immediately if delay is 0', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 0),
      { initialProps: { value: 'initial' } }
    )

    rerender({ value: 'updated' })

    act(() => {
      jest.advanceTimersByTime(0)
    })

    expect(result.current).toBe('updated')
  })

  it('should handle delay changes', () => {
    const { result, rerender } = renderHook(
      ({ value, delay }) => useDebounce(value, delay),
      { initialProps: { value: 'initial', delay: 300 } }
    )

    // Change both value and delay
    rerender({ value: 'updated', delay: 100 })

    act(() => {
      jest.advanceTimersByTime(99)
    })

    expect(result.current).toBe('initial')

    act(() => {
      jest.advanceTimersByTime(1)
    })

    expect(result.current).toBe('updated')
  })

  it('should handle empty string values', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 100),
      { initialProps: { value: 'initial' } }
    )

    rerender({ value: '' })

    act(() => {
      jest.advanceTimersByTime(100)
    })

    expect(result.current).toBe('')
  })

  it('should work correctly with search input scenario', () => {
    // Simulating a real-world search input scenario
    const { result, rerender } = renderHook(
      ({ searchQuery }) => useDebounce(searchQuery, 300),
      { initialProps: { searchQuery: '' } }
    )

    // User types "h"
    rerender({ searchQuery: 'h' })
    act(() => {
      jest.advanceTimersByTime(100)
    })

    // User types "he"
    rerender({ searchQuery: 'he' })
    act(() => {
      jest.advanceTimersByTime(100)
    })

    // User types "hel"
    rerender({ searchQuery: 'hel' })
    act(() => {
      jest.advanceTimersByTime(100)
    })

    // User types "hell"
    rerender({ searchQuery: 'hell' })
    act(() => {
      jest.advanceTimersByTime(100)
    })

    // User types "hello"
    rerender({ searchQuery: 'hello' })

    // Value should still be empty
    expect(result.current).toBe('')

    // Wait for full delay
    act(() => {
      jest.advanceTimersByTime(300)
    })

    // Value should now be "hello"
    expect(result.current).toBe('hello')
  })
})
