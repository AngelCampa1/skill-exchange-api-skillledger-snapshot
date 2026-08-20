import { useState, useEffect, useRef, useCallback } from 'react'

const STORAGE_PREFIX = 'skillledger_form_'
const PASSWORD_FIELDS = ['password', 'confirmPassword', 'confirm_password', 'currentPassword', 'newPassword']

/**
 * Custom hook that persists form field values to localStorage.
 * Restores values on mount. Auto-saves on each field change (debounced 500ms).
 * NEVER stores password fields.
 */
export function useFormPersistence(
  formName: string,
  fieldNames: string[]
): {
  persistedValues: Record<string, string>
  updateField: (name: string, value: string) => void
  clearPersistedData: () => void
  hasPersistedData: boolean
} {
  const storageKey = `${STORAGE_PREFIX}${formName}`

  // Filter out password fields
  const safeFieldNames = fieldNames.filter(
    (name) => !PASSWORD_FIELDS.includes(name)
  )

  const [persistedValues, setPersistedValues] = useState<Record<string, string>>(() => {
    if (typeof window === 'undefined') return {}
    try {
      const stored = localStorage.getItem(storageKey)
      if (stored) {
        const parsed = JSON.parse(stored) as Record<string, string>
        // Only return values for safe field names
        const filtered: Record<string, string> = {}
        for (const name of safeFieldNames) {
          if (parsed[name]) {
            filtered[name] = parsed[name]
          }
        }
        return filtered
      }
    } catch {
      // Ignore parse errors
    }
    return {}
  })

  const [hasPersistedData, setHasPersistedData] = useState<boolean>(() => {
    if (typeof window === 'undefined') return false
    try {
      const stored = localStorage.getItem(storageKey)
      if (stored) {
        const parsed = JSON.parse(stored) as Record<string, string>
        return Object.values(parsed).some((v) => v.length > 0)
      }
    } catch {
      // Ignore parse errors
    }
    return false
  })

  const pendingValues = useRef<Record<string, string>>({ ...persistedValues })
  const debounceTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  const saveToStorage = useCallback(
    (values: Record<string, string>) => {
      try {
        localStorage.setItem(storageKey, JSON.stringify(values))
        setHasPersistedData(Object.values(values).some((v) => v.length > 0))
      } catch {
        // localStorage may be full or unavailable
      }
    },
    [storageKey]
  )

  const updateField = useCallback(
    (name: string, value: string) => {
      // Never store password fields
      if (PASSWORD_FIELDS.includes(name)) return

      pendingValues.current = { ...pendingValues.current, [name]: value }
      setPersistedValues({ ...pendingValues.current })

      // Debounced save to localStorage
      if (debounceTimer.current) {
        clearTimeout(debounceTimer.current)
      }
      debounceTimer.current = setTimeout(() => {
        saveToStorage(pendingValues.current)
      }, 500)
    },
    [saveToStorage]
  )

  const clearPersistedData = useCallback(() => {
    try {
      localStorage.removeItem(storageKey)
    } catch {
      // Ignore
    }
    pendingValues.current = {}
    setPersistedValues({})
    setHasPersistedData(false)
  }, [storageKey])

  // BUG-55 FIX: On unmount, cancel the pending debounce timer to prevent calling
  // setPersistedValues (state update) on an unmounted component, and synchronously
  // flush any buffered values to localStorage so no keystrokes are silently lost.
  useEffect(() => {
    return () => {
      if (debounceTimer.current) {
        clearTimeout(debounceTimer.current)
        debounceTimer.current = null
        // Flush pending values synchronously so they aren't lost
        const pending = pendingValues.current
        if (Object.keys(pending).length > 0) {
          try {
            localStorage.setItem(storageKey, JSON.stringify(pending))
          } catch {
            // localStorage may be full or unavailable — discard silently
          }
        }
      }
    }
  }, [storageKey])

  return {
    persistedValues,
    updateField,
    clearPersistedData,
    hasPersistedData,
  }
}
