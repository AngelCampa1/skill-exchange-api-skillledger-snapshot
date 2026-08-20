/**
 * Tests for useFormTracking hook
 */

import { renderHook, act } from '@testing-library/react'
import { useFormTracking } from '../useFormTracking'
import { trackEvent } from '@/utils/analytics'

// Mock analytics
jest.mock('@/utils/analytics', () => ({
  trackEvent: jest.fn(),
}))

const mockTrackEvent = trackEvent as jest.MockedFunction<typeof trackEvent>

describe('useFormTracking', () => {
  beforeEach(() => {
    jest.clearAllMocks()
  })

  describe('trackFormStarted', () => {
    it('should track form_started event', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackFormStarted()
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'form_started',
        category: 'forms',
        priority: 'medium',
        properties: {
          form_name: 'test-form',
        },
      })
    })

    it('should only track form_started once', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackFormStarted()
        result.current.trackFormStarted()
        result.current.trackFormStarted()
      })

      expect(mockTrackEvent).toHaveBeenCalledTimes(1)
    })

    it('should use custom category when provided', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form', category: 'profile' })
      )

      act(() => {
        result.current.trackFormStarted()
      })

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          category: 'profile',
        })
      )
    })
  })

  describe('trackFieldChange', () => {
    it('should track form_started on first field change', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackFieldChange('email')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          name: 'form_started',
        })
      )
    })

    it('should track form_field_changed event', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackFieldChange('email')
      })

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          name: 'form_field_changed',
          properties: expect.objectContaining({
            field_name: 'email',
            fields_touched: 1,
          }),
        })
      )
    })

    it('should not track same field twice', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackFieldChange('email')
        result.current.trackFieldChange('email')
      })

      // Should track: form_started + form_field_changed (once)
      expect(mockTrackEvent).toHaveBeenCalledTimes(2)
    })

    it('should track multiple different fields', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackFieldChange('email')
        result.current.trackFieldChange('password')
      })

      // Should track: form_started + email + password
      expect(mockTrackEvent).toHaveBeenCalledTimes(3)
      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            field_name: 'password',
            fields_touched: 2,
          }),
        })
      )
    })

    it('should not track field changes when trackFieldChanges is false', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form', trackFieldChanges: false })
      )

      act(() => {
        result.current.trackFieldChange('email')
      })

      // Should only track form_started
      expect(mockTrackEvent).toHaveBeenCalledTimes(1)
      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          name: 'form_started',
        })
      )
    })
  })

  describe('trackValidationError', () => {
    it('should track validation errors', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackValidationError(['email', 'password'])
      })

      expect(mockTrackEvent).toHaveBeenCalledWith({
        name: 'form_validation_error',
        category: 'forms',
        priority: 'medium',
        properties: {
          form_name: 'test-form',
          error_fields: 'email,password',
          error_count: 2,
        },
      })
    })

    it('should handle single error field', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackValidationError(['email'])
      })

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            error_fields: 'email',
            error_count: 1,
          }),
        })
      )
    })

    it('should handle empty error array', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackValidationError([])
      })

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            error_fields: '',
            error_count: 0,
          }),
        })
      )
    })
  })

  describe('trackFormSubmit', () => {
    it('should track successful submission', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackFormSubmit(true)
      })

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          name: 'form_success',
          category: 'forms',
          priority: 'medium',
          properties: expect.objectContaining({
            form_name: 'test-form',
            attempt_count: 1,
            fields_touched: 0,
          }),
        })
      )
    })

    it('should track failed submission', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackFormSubmit(false)
      })

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          name: 'form_error',
        })
      )
    })

    it('should track completion time', () => {
      jest.useFakeTimers()
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackFormStarted()
      })

      // Advance time by 5 seconds
      act(() => {
        jest.advanceTimersByTime(5000)
      })

      act(() => {
        result.current.trackFormSubmit(true)
      })

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            completion_time: 5000,
          }),
        })
      )

      jest.useRealTimers()
    })

    it('should use provided completion time', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackFormSubmit(true, undefined, 10000)
      })

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            completion_time: 10000,
          }),
        })
      )
    })

    it('should use provided attempt count', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackFormSubmit(true, 3)
      })

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            attempt_count: 3,
          }),
        })
      )
    })

    it('should increment attempt count on multiple submissions', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackFormSubmit(false)
      })

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            attempt_count: 1,
          }),
        })
      )

      act(() => {
        result.current.trackFormSubmit(false)
      })

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            attempt_count: 2,
          }),
        })
      )
    })

    it('should track number of fields touched', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackFieldChange('email')
        result.current.trackFieldChange('password')
        result.current.trackFieldChange('confirmPassword')
      })

      act(() => {
        result.current.trackFormSubmit(true)
      })

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            fields_touched: 3,
          }),
        })
      )
    })

    it('should reset tracking state after successful submission', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackFieldChange('email')
        result.current.trackFormSubmit(true)
      })

      // Submit again - should start fresh
      act(() => {
        result.current.trackFormSubmit(true)
      })

      expect(mockTrackEvent).toHaveBeenLastCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            attempt_count: 1,
            fields_touched: 0,
          }),
        })
      )
    })

    it('should NOT reset tracking state after failed submission', () => {
      const { result } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackFieldChange('email')
        result.current.trackFormSubmit(false)
      })

      // Submit again - should maintain state
      act(() => {
        result.current.trackFormSubmit(false)
      })

      expect(mockTrackEvent).toHaveBeenLastCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            attempt_count: 2,
            fields_touched: 1,
          }),
        })
      )
    })
  })

  describe('cleanup', () => {
    it('should reset state on unmount', () => {
      const { result, unmount } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        result.current.trackFieldChange('email')
        result.current.trackFormSubmit(false)
      })

      unmount()

      // Re-mount and verify state is reset
      const { result: newResult } = renderHook(() =>
        useFormTracking({ formName: 'test-form' })
      )

      act(() => {
        newResult.current.trackFormSubmit(true)
      })

      expect(mockTrackEvent).toHaveBeenLastCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            attempt_count: 1,
            fields_touched: 0,
          }),
        })
      )
    })
  })
})
