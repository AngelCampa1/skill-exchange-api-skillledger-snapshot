import { renderHook, act } from '@testing-library/react'
import { useExitIntent } from '../useExitIntent'

describe('useExitIntent', () => {
  beforeEach(() => {
    sessionStorage.clear()
    localStorage.clear()
    jest.useFakeTimers()
    document.querySelectorAll('[role="dialog"]').forEach(el => el.remove())
    document.querySelectorAll('[data-radix-dialog-overlay]').forEach(el => el.remove())
  })

  afterEach(() => {
    jest.useRealTimers()
  })

  it('returns showPopup as false initially', () => {
    const { result } = renderHook(() => useExitIntent())
    expect(result.current.showPopup).toBe(false)
  })

  it('does not trigger before minimum time on site', () => {
    const { result } = renderHook(() => useExitIntent())

    // Simulate mouseleave before 20s
    act(() => {
      const event = new MouseEvent('mouseleave', { clientY: -1 })
      document.documentElement.dispatchEvent(event)
    })

    expect(result.current.showPopup).toBe(false)
  })

  it('triggers popup on mouseleave after minimum time on site', () => {
    const { result } = renderHook(() => useExitIntent())

    // Advance past MIN_TIME_ON_SITE_MS (20s)
    jest.advanceTimersByTime(21_000)

    act(() => {
      const event = new MouseEvent('mouseleave', { clientY: -1 })
      document.documentElement.dispatchEvent(event)
    })

    expect(result.current.showPopup).toBe(true)
  })

  it('does not trigger when a dialog modal is already open', () => {
    const { result } = renderHook(() => useExitIntent())

    // Add a dialog to the DOM
    const dialog = document.createElement('div')
    dialog.setAttribute('role', 'dialog')
    dialog.setAttribute('aria-modal', 'true')
    document.body.appendChild(dialog)

    // Advance past minimum time
    jest.advanceTimersByTime(21_000)

    act(() => {
      const event = new MouseEvent('mouseleave', { clientY: -1 })
      document.documentElement.dispatchEvent(event)
    })

    expect(result.current.showPopup).toBe(false)

    // Cleanup
    document.body.removeChild(dialog)
  })

  it('does not trigger when an alertdialog is already open', () => {
    const { result } = renderHook(() => useExitIntent())

    const dialog = document.createElement('div')
    dialog.setAttribute('role', 'alertdialog')
    dialog.setAttribute('aria-modal', 'true')
    document.body.appendChild(dialog)

    jest.advanceTimersByTime(21_000)

    act(() => {
      const event = new MouseEvent('mouseleave', { clientY: -1 })
      document.documentElement.dispatchEvent(event)
    })

    expect(result.current.showPopup).toBe(false)

    document.body.removeChild(dialog)
  })

  it('does not trigger when a radix dialog overlay is present', () => {
    const { result } = renderHook(() => useExitIntent())

    const overlay = document.createElement('div')
    overlay.setAttribute('data-radix-dialog-overlay', '')
    document.body.appendChild(overlay)

    jest.advanceTimersByTime(21_000)

    act(() => {
      const event = new MouseEvent('mouseleave', { clientY: -1 })
      document.documentElement.dispatchEvent(event)
    })

    expect(result.current.showPopup).toBe(false)

    document.body.removeChild(overlay)
  })

  it('triggers popup when no modal is open', () => {
    const { result } = renderHook(() => useExitIntent())

    jest.advanceTimersByTime(21_000)

    act(() => {
      const event = new MouseEvent('mouseleave', { clientY: -1 })
      document.documentElement.dispatchEvent(event)
    })

    expect(result.current.showPopup).toBe(true)
  })

  it('does not fire more than once', () => {
    const { result } = renderHook(() => useExitIntent())

    jest.advanceTimersByTime(21_000)

    act(() => {
      const event = new MouseEvent('mouseleave', { clientY: -1 })
      document.documentElement.dispatchEvent(event)
    })

    expect(result.current.showPopup).toBe(true)

    // Dismiss
    act(() => {
      result.current.dismissPopup()
    })

    expect(result.current.showPopup).toBe(false)

    // Try again — should not fire
    act(() => {
      const event = new MouseEvent('mouseleave', { clientY: -1 })
      document.documentElement.dispatchEvent(event)
    })

    expect(result.current.showPopup).toBe(false)
  })

  it('does not trigger if already shown in session', () => {
    sessionStorage.setItem('exit_intent_shown', 'true')

    const { result } = renderHook(() => useExitIntent())

    jest.advanceTimersByTime(21_000)

    act(() => {
      const event = new MouseEvent('mouseleave', { clientY: -1 })
      document.documentElement.dispatchEvent(event)
    })

    expect(result.current.showPopup).toBe(false)
  })

  it('does not trigger if dismissed within 7 days', () => {
    localStorage.setItem('exit_intent_dismissed_at', String(Date.now()))

    const { result } = renderHook(() => useExitIntent())

    jest.advanceTimersByTime(21_000)

    act(() => {
      const event = new MouseEvent('mouseleave', { clientY: -1 })
      document.documentElement.dispatchEvent(event)
    })

    expect(result.current.showPopup).toBe(false)
  })

  it('dismissPopup sets localStorage and hides popup', () => {
    const { result } = renderHook(() => useExitIntent())

    jest.advanceTimersByTime(21_000)

    act(() => {
      const event = new MouseEvent('mouseleave', { clientY: -1 })
      document.documentElement.dispatchEvent(event)
    })

    expect(result.current.showPopup).toBe(true)

    act(() => {
      result.current.dismissPopup()
    })

    expect(result.current.showPopup).toBe(false)
    expect(localStorage.getItem('exit_intent_dismissed_at')).toBeTruthy()
  })
})
