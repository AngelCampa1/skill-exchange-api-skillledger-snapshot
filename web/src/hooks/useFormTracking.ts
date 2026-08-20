/**
 * useFormTracking - Reusable hook for tracking form interactions
 *
 * Automatically tracks:
 * - Form started (first field interaction)
 * - Field changes (with field names)
 * - Validation errors
 * - Form submission (success/failure)
 */

import { useCallback, useRef, useEffect } from 'react'
import { trackEvent } from '@/utils/analytics'
import { EventCategory } from '@/types/analytics'

interface FormTrackingOptions {
  formName: string
  category?: EventCategory
  trackFieldChanges?: boolean
}

interface FormTrackingReturn {
  trackFormStarted: () => void
  trackFieldChange: (fieldName: string) => void
  trackValidationError: (errorFields: string[]) => void
  trackFormSubmit: (success: boolean, attemptCount?: number, completionTime?: number) => void
}

export function useFormTracking(options: FormTrackingOptions): FormTrackingReturn {
  const { formName, category = 'forms', trackFieldChanges = true } = options

  const formStartedRef = useRef(false)
  const formStartTimeRef = useRef<number | null>(null)
  const attemptCountRef = useRef(0)
  const touchedFieldsRef = useRef<Set<string>>(new Set())

  // Reset on unmount
  useEffect(() => {
    // Capture the current ref value to use in cleanup
    const touchedFields = touchedFieldsRef.current
    return () => {
      formStartedRef.current = false
      formStartTimeRef.current = null
      attemptCountRef.current = 0
      touchedFields.clear()
    }
  }, [])

  const trackFormStarted = useCallback(() => {
    if (!formStartedRef.current) {
      formStartedRef.current = true
      formStartTimeRef.current = Date.now()

      trackEvent({
        name: 'form_started',
        category,
        priority: 'medium',
        properties: {
          form_name: formName,
        },
      })
    }
  }, [formName, category])

  const trackFieldChange = useCallback((fieldName: string) => {
    // Track form started on first field interaction
    if (!formStartedRef.current) {
      trackFormStarted()
    }

    // Track field as touched
    if (!touchedFieldsRef.current.has(fieldName)) {
      touchedFieldsRef.current.add(fieldName)

      if (trackFieldChanges) {
        trackEvent({
          name: 'form_field_changed',
          category,
          priority: 'low',
          properties: {
            form_name: formName,
            field_name: fieldName,
            fields_touched: touchedFieldsRef.current.size,
          },
        })
      }
    }
  }, [formName, category, trackFieldChanges, trackFormStarted])

  const trackValidationError = useCallback((errorFields: string[]) => {
    trackEvent({
      name: 'form_validation_error',
      category,
      priority: 'medium',
      properties: {
        form_name: formName,
        error_fields: errorFields.join(','),
        error_count: errorFields.length,
      },
    })
  }, [formName, category])

  const trackFormSubmit = useCallback((
    success: boolean,
    attemptCount?: number,
    completionTime?: number
  ) => {
    const actualAttemptCount = attemptCount ?? (attemptCountRef.current + 1)
    attemptCountRef.current = actualAttemptCount

    const actualCompletionTime = completionTime ??
      (formStartTimeRef.current ? Date.now() - formStartTimeRef.current : undefined)

    const eventName = success ? 'form_success' : 'form_error'

    trackEvent({
      name: eventName,
      category,
      priority: 'medium',
      properties: {
        form_name: formName,
        completion_time: actualCompletionTime,
        attempt_count: actualAttemptCount,
        fields_touched: touchedFieldsRef.current.size,
      },
    })

    // Reset tracking state after submission
    if (success) {
      formStartedRef.current = false
      formStartTimeRef.current = null
      attemptCountRef.current = 0
      touchedFieldsRef.current.clear()
    }
  }, [formName, category])

  return {
    trackFormStarted,
    trackFieldChange,
    trackValidationError,
    trackFormSubmit,
  }
}
