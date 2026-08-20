/**
 * Accessibility Utilities for Enhanced Visual Components
 *
 * This file provides utilities for ensuring animations and interactions
 * are accessible to all users, including those with motion sensitivity
 * and other accessibility needs.
 */

// Detect user preferences for reduced motion
export const getAccessibilityPreferences = () => {
  if (typeof window === 'undefined') {
    return {
      prefersReducedMotion: false,
      prefersHighContrast: false,
      prefersLargeText: false,
      screenReaderActive: false
    }
  }

  const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches
  const prefersHighContrast = window.matchMedia('(prefers-contrast: high)').matches
  const prefersLargeText = window.matchMedia('(prefers-reduced-data: reduce)').matches

  // Simple screen reader detection
  const screenReaderActive = window.speechSynthesis?.getVoices?.().length > 0 ||
                            navigator.userAgent.includes('NVDA') ||
                            navigator.userAgent.includes('JAWS') ||
                            navigator.userAgent.includes('VOICEOVER')

  return {
    prefersReducedMotion,
    prefersHighContrast,
    prefersLargeText,
    screenReaderActive
  }
}

// Apply accessibility classes to document
export const applyAccessibilityClasses = () => {
  const prefs = getAccessibilityPreferences()
  const root = document.documentElement

  // Reduced motion
  if (prefs.prefersReducedMotion) {
    root.classList.add('reduce-motion')
    root.classList.add('no-animations')
  }

  // High contrast
  if (prefs.prefersHighContrast) {
    root.classList.add('high-contrast')
  }

  // Large text
  if (prefs.prefersLargeText) {
    root.classList.add('large-text')
  }

  // Screen reader
  if (prefs.screenReaderActive) {
    root.classList.add('screen-reader')
  }

  return prefs
}

// Accessible animation utilities
export const createAccessibleAnimation = (
  element: HTMLElement,
  animationOptions: {
    duration?: number
    easing?: string
    delay?: number
    respectReducedMotion?: boolean
  } = {}
): Animation | null => {
  const prefs = getAccessibilityPreferences()

  // Skip animation if user prefers reduced motion and respectReducedMotion is true
  if (animationOptions.respectReducedMotion !== false && prefs.prefersReducedMotion) {
    return null
  }

  // Adjust duration for accessibility
  let duration = animationOptions.duration || 300
  if (prefs.prefersReducedMotion) {
    duration = 0 // Instant transition
  } else if (prefs.screenReaderActive) {
    duration = Math.min(duration, 200) // Shorter duration for screen readers
  }

  // Create simple, accessible animation using element.animate
  const dummyElement = document.createElement('div')
  const animation = dummyElement.animate(
    [
      { opacity: 0, transform: 'translateY(10px)' },
      { opacity: 1, transform: 'translateY(0)' }
    ],
    {
      duration,
      easing: animationOptions.easing || 'ease-out',
      delay: animationOptions.delay || 0,
      fill: 'both'
    }
  )
  return animation
}

// Accessible focus management
export const manageFocus = (
  container: HTMLElement,
  options: {
    trapFocus?: boolean
    restoreFocus?: boolean
    initialFocus?: HTMLElement
  } = {}
) => {
  const prefs = getAccessibilityPreferences()
  const {
    trapFocus = true,
    restoreFocus = true,
    initialFocus
  } = options

  let previousActiveElement: HTMLElement | null = null

  // Store current focus
  if (restoreFocus) {
    previousActiveElement = document.activeElement as HTMLElement
  }

  // Get focusable elements
  const getFocusableElements = () => {
    return Array.from(
      container.querySelectorAll(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
      )
    ) as HTMLElement[]
  }

  // Trap focus within container
  if (trapFocus && !prefs.screenReaderActive) {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Tab') {
        const focusableElements = getFocusableElements()
        const firstElement = focusableElements[0]
        const lastElement = focusableElements[focusableElements.length - 1]

        if (e.shiftKey) {
          if (document.activeElement === firstElement) {
            e.preventDefault()
            lastElement?.focus()
          }
        } else {
          if (document.activeElement === lastElement) {
            e.preventDefault()
            firstElement?.focus()
          }
        }
      }
    }

    container.addEventListener('keydown', handleKeyDown)

    return () => {
      container.removeEventListener('keydown', handleKeyDown)
    }
  }

  // Set initial focus
  if (initialFocus) {
    setTimeout(() => initialFocus.focus(), 100)
  } else {
    const focusableElements = getFocusableElements()
    if (focusableElements.length > 0) {
      setTimeout(() => focusableElements[0].focus(), 100)
    }
  }

  // Restore focus
  return () => {
    if (restoreFocus && previousActiveElement) {
      previousActiveElement.focus()
    }
  }
}

// Accessible ARIA live regions
export const createLiveRegion = (
  message: string,
  options: {
    priority?: 'polite' | 'assertive' | 'off'
    timeout?: number
  } = {}
): HTMLElement => {
  const { priority = 'polite', timeout = 5000 } = options

  const liveRegion = document.createElement('div')
  liveRegion.setAttribute('aria-live', priority)
  liveRegion.setAttribute('aria-atomic', 'true')
  liveRegion.className = 'sr-only' // Hide visually but keep available to screen readers

  document.body.appendChild(liveRegion)

  // Announce message
  liveRegion.textContent = message

  // Clear message after timeout
  if (timeout > 0) {
    setTimeout(() => {
      liveRegion.textContent = ''
    }, timeout)
  }

  return liveRegion
}

// Screen reader announcements
export const announceToScreenReader = (message: string, priority: 'polite' | 'assertive' = 'polite') => {
  const prefs = getAccessibilityPreferences()

  if (!prefs.screenReaderActive) {
    return
  }

  const liveRegion = createLiveRegion(message, { priority, timeout: 1000 })

  // Clean up after announcement
  setTimeout(() => {
    document.body.removeChild(liveRegion)
  }, 1500)
}

// Accessible color utilities
export const getAccessibleColor = (baseColor: string, context: 'text' | 'background' | 'border') => {
  const prefs = getAccessibilityPreferences()

  if (prefs.prefersHighContrast) {
    // High contrast color mapping
    const highContrastColors = {
      text: '#000000',
      background: '#FFFFFF',
      border: '#000000',
      primary: '#0000FF',
      secondary: '#FF0000',
      success: '#008000',
      warning: '#FF8C00',
      error: '#FF0000'
    }

    return highContrastColors[context as keyof typeof highContrastColors] || baseColor
  }

  return baseColor
}

// Accessible spacing utilities
export const getAccessibleSpacing = (baseSize: number): string => {
  const prefs = getAccessibilityPreferences()

  // Increase spacing for users who prefer larger text
  const multiplier = prefs.prefersLargeText ? 1.5 : 1

  return `${baseSize * multiplier}px`
}

// Keyboard navigation utilities
export const setupKeyboardNavigation = (
  element: HTMLElement,
  handlers: {
    onEnter?: () => void
    onSpace?: () => void
    onEscape?: () => void
    onArrow?: (direction: 'up' | 'down' | 'left' | 'right') => void
    onFocus?: () => void
    onBlur?: () => void
  }
) => {
  const handleKeyDown = (e: KeyboardEvent) => {
    switch (e.key) {
      case 'Enter':
        e.preventDefault()
        handlers.onEnter?.()
        break
      case ' ':
        e.preventDefault()
        handlers.onSpace?.()
        break
      case 'Escape':
        e.preventDefault()
        handlers.onEscape?.()
        break
      case 'ArrowUp':
        e.preventDefault()
        handlers.onArrow?.('up')
        break
      case 'ArrowDown':
        e.preventDefault()
        handlers.onArrow?.('down')
        break
      case 'ArrowLeft':
        e.preventDefault()
        handlers.onArrow?.('left')
        break
      case 'ArrowRight':
        e.preventDefault()
        handlers.onArrow?.('right')
        break
    }
  }

  const handleFocus = () => handlers.onFocus?.()
  const handleBlur = () => handlers.onBlur?.()

  element.addEventListener('keydown', handleKeyDown)
  element.addEventListener('focus', handleFocus)
  element.addEventListener('blur', handleBlur)

  return () => {
    element.removeEventListener('keydown', handleKeyDown)
    element.removeEventListener('focus', handleFocus)
    element.removeEventListener('blur', handleBlur)
  }
}

// BUG-FIX: Tooltip result interface with cleanup function
interface TooltipResult {
  tooltip: HTMLElement
  cleanup: () => void
}

// Accessible tooltip utilities
export const createAccessibleTooltip = (
  trigger: HTMLElement,
  content: string,
  options: {
    placement?: 'top' | 'bottom' | 'left' | 'right'
    persistent?: boolean
  } = {}
): TooltipResult => {
  const { placement = 'top', persistent = false } = options

  const tooltip = document.createElement('div')
  tooltip.setAttribute('role', 'tooltip')
  tooltip.setAttribute('id', `tooltip-${Date.now()}`)
  tooltip.className = 'tooltip'
  tooltip.textContent = content

  // Link trigger and tooltip
  trigger.setAttribute('aria-describedby', tooltip.id)

  document.body.appendChild(tooltip)

  const showTooltip = () => {
    tooltip.classList.add('visible')
    announceToScreenReader(content)
  }

  const hideTooltip = () => {
    if (!persistent) {
      tooltip.classList.remove('visible')
    }
  }

  // Position tooltip
  const positionTooltip = () => {
    const triggerRect = trigger.getBoundingClientRect()
    const tooltipRect = tooltip.getBoundingClientRect()

    let left = triggerRect.left + (triggerRect.width - tooltipRect.width) / 2
    let top = triggerRect.top - tooltipRect.height - 8

    switch (placement) {
      case 'bottom':
        top = triggerRect.bottom + 8
        break
      case 'left':
        left = triggerRect.left - tooltipRect.width - 8
        top = triggerRect.top + (triggerRect.height - tooltipRect.height) / 2
        break
      case 'right':
        left = triggerRect.right + 8
        top = triggerRect.top + (triggerRect.height - tooltipRect.height) / 2
        break
    }

    tooltip.style.left = `${left}px`
    tooltip.style.top = `${top}px`
  }

  // BUG-FIX: Use named functions for event listeners to allow cleanup
  const handleMouseEnter = () => {
    positionTooltip()
    showTooltip()
  }

  const handleFocus = () => {
    positionTooltip()
    showTooltip()
  }

  // Event listeners
  trigger.addEventListener('mouseenter', handleMouseEnter)
  trigger.addEventListener('mouseleave', hideTooltip)
  trigger.addEventListener('focus', handleFocus)
  trigger.addEventListener('blur', hideTooltip)

  // BUG-FIX: Return cleanup function to remove event listeners and tooltip
  const cleanup = () => {
    trigger.removeEventListener('mouseenter', handleMouseEnter)
    trigger.removeEventListener('mouseleave', hideTooltip)
    trigger.removeEventListener('focus', handleFocus)
    trigger.removeEventListener('blur', hideTooltip)
    trigger.removeAttribute('aria-describedby')
    if (tooltip.parentNode) {
      tooltip.parentNode.removeChild(tooltip)
    }
  }

  return { tooltip, cleanup }
}

// Accessible progress indicator
export const createAccessibleProgress = (
  value: number,
  max: number,
  label: string,
  options: {
    showPercentage?: boolean
    animated?: boolean
  } = {}
): HTMLElement => {
  const { showPercentage = true, animated = true } = options
  const prefs = getAccessibilityPreferences()

  const progress = document.createElement('div')
  progress.setAttribute('role', 'progressbar')
  progress.setAttribute('aria-valuenow', value.toString())
  progress.setAttribute('aria-valuemin', '0')
  progress.setAttribute('aria-valuemax', max.toString())
  progress.setAttribute('aria-label', label)

  progress.className = 'accessible-progress'

  const progressBar = document.createElement('div')
  progressBar.className = 'accessible-progress-bar'
  progressBar.style.width = `${(value / max) * 100}%`

  progress.appendChild(progressBar)

  if (showPercentage) {
    const percentage = document.createElement('span')
    percentage.className = 'accessible-progress-percentage'
    percentage.textContent = `${Math.round((value / max) * 100)}%`
    progress.appendChild(percentage)
  }

  return progress
}

// BUG-FIX: Track cleanup functions for accessibility initialization
interface AccessibilityCleanup {
  cleanup: () => void
}

// Initialize accessibility features
export const initializeAccessibility = (): AccessibilityCleanup & ReturnType<typeof applyAccessibilityClasses> => {
  const prefs = applyAccessibilityClasses()

  // Listen for preference changes
  const motionQuery = window.matchMedia('(prefers-reduced-motion: reduce)')
  const contrastQuery = window.matchMedia('(prefers-contrast: high)')

  const handlePreferenceChange = () => {
    applyAccessibilityClasses()
    announceToScreenReader('Accessibility preferences updated')
  }

  motionQuery.addEventListener('change', handlePreferenceChange)
  contrastQuery.addEventListener('change', handlePreferenceChange)

  // VULN-008 FIX: Use DOM manipulation instead of innerHTML
  // Even with static content, DOM APIs are safer and more explicit
  const skipLinks = document.createElement('div')
  skipLinks.className = 'skip-links'

  const mainContentLink = document.createElement('a')
  mainContentLink.href = '#main-content'
  mainContentLink.textContent = 'Skip to main content'
  skipLinks.appendChild(mainContentLink)

  const navigationLink = document.createElement('a')
  navigationLink.href = '#navigation'
  navigationLink.textContent = 'Skip to navigation'
  skipLinks.appendChild(navigationLink)

  document.body.insertBefore(skipLinks, document.body.firstChild)

  // BUG-FIX: Return cleanup function to remove event listeners
  const cleanup = () => {
    motionQuery.removeEventListener('change', handlePreferenceChange)
    contrastQuery.removeEventListener('change', handlePreferenceChange)
    if (skipLinks.parentNode) {
      skipLinks.parentNode.removeChild(skipLinks)
    }
  }

  return { ...prefs, cleanup }
}

// Utility to check if animation should be reduced
export const shouldReduceAnimation = (animationType?: string): boolean => {
  const prefs = getAccessibilityPreferences()

  // Always respect user preferences
  if (prefs.prefersReducedMotion) {
    return true
  }

  // Certain animations should always be reduced for screen readers
  if (prefs.screenReaderActive && ['parallax', 'floating', 'particle'].includes(animationType || '')) {
    return true
  }

  return false
}

const accessibilityUtils = {
  getAccessibilityPreferences,
  applyAccessibilityClasses,
  createAccessibleAnimation,
  manageFocus,
  createLiveRegion,
  announceToScreenReader,
  getAccessibleColor,
  getAccessibleSpacing,
  setupKeyboardNavigation,
  createAccessibleTooltip,
  createAccessibleProgress,
  initializeAccessibility,
  shouldReduceAnimation
}

export default accessibilityUtils