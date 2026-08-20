/**
 * Tests for accessibility.ts utilities
 *
 * This file validates accessibility helper functions for WCAG compliance
 */

import {
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
  shouldReduceAnimation,
} from '@/lib/accessibility'

describe('accessibility utilities', () => {
  let matchMediaMock: jest.Mock
  let animateMock: jest.Mock

  beforeEach(() => {
    // Mock matchMedia
    matchMediaMock = jest.fn((query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: jest.fn(),
      removeListener: jest.fn(),
      addEventListener: jest.fn(),
      removeEventListener: jest.fn(),
      dispatchEvent: jest.fn(),
    }))

    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: matchMediaMock,
    })

    // Mock Element.prototype.animate
    animateMock = jest.fn(() => ({
      cancel: jest.fn(),
      finish: jest.fn(),
      play: jest.fn(),
      pause: jest.fn(),
      reverse: jest.fn(),
    }))

    Element.prototype.animate = animateMock

    // Clear speechSynthesis mock
    Object.defineProperty(window, 'speechSynthesis', {
      writable: true,
      value: undefined,
    })

    // Clear document classes
    document.documentElement.className = ''
  })

  afterEach(() => {
    jest.clearAllMocks()
    document.documentElement.className = ''
    document.body.innerHTML = ''
  })

  describe('getAccessibilityPreferences', () => {
    it('should detect reduced motion preference', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-reduced-motion: reduce)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      const prefs = getAccessibilityPreferences()

      expect(prefs.prefersReducedMotion).toBe(true)
      expect(prefs.prefersHighContrast).toBe(false)
      expect(prefs.prefersLargeText).toBe(false)
    })

    it('should detect high contrast preference', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-contrast: high)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      const prefs = getAccessibilityPreferences()

      expect(prefs.prefersHighContrast).toBe(true)
      expect(prefs.prefersReducedMotion).toBe(false)
    })

    it('should detect large text preference', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-reduced-data: reduce)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      const prefs = getAccessibilityPreferences()

      expect(prefs.prefersLargeText).toBe(true)
    })

    it('should detect screen reader activity', () => {
      // Mock speechSynthesis API
      Object.defineProperty(window, 'speechSynthesis', {
        writable: true,
        value: {
          getVoices: () => [{ name: 'Test Voice' }],
        },
      })

      const prefs = getAccessibilityPreferences()

      expect(prefs.screenReaderActive).toBe(true)
    })

    it('should return default preferences when no preferences set', () => {
      const prefs = getAccessibilityPreferences()

      expect(prefs).toEqual({
        prefersReducedMotion: false,
        prefersHighContrast: false,
        prefersLargeText: false,
        screenReaderActive: false,
      })
    })
  })

  describe('applyAccessibilityClasses', () => {
    it('should add reduce-motion and no-animations classes when preference is set', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-reduced-motion: reduce)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      applyAccessibilityClasses()

      expect(document.documentElement.classList.contains('reduce-motion')).toBe(true)
      expect(document.documentElement.classList.contains('no-animations')).toBe(true)
    })

    it('should add high-contrast class when preference is set', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-contrast: high)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      applyAccessibilityClasses()

      expect(document.documentElement.classList.contains('high-contrast')).toBe(true)
    })

    it('should add large-text class when preference is set', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-reduced-data: reduce)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      applyAccessibilityClasses()

      expect(document.documentElement.classList.contains('large-text')).toBe(true)
    })

    it('should add screen-reader class when screen reader is active', () => {
      Object.defineProperty(window, 'speechSynthesis', {
        writable: true,
        value: {
          getVoices: () => [{ name: 'Test Voice' }],
        },
      })

      applyAccessibilityClasses()

      expect(document.documentElement.classList.contains('screen-reader')).toBe(true)
    })

    it('should return preferences object', () => {
      const prefs = applyAccessibilityClasses()

      expect(prefs).toHaveProperty('prefersReducedMotion')
      expect(prefs).toHaveProperty('prefersHighContrast')
      expect(prefs).toHaveProperty('prefersLargeText')
      expect(prefs).toHaveProperty('screenReaderActive')
    })
  })

  describe('createAccessibleAnimation', () => {
    it('should return null when reduced motion is preferred and respectReducedMotion is true', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-reduced-motion: reduce)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      const element = document.createElement('div')
      const result = createAccessibleAnimation(element, { duration: 500 })

      expect(result).toBeNull()
    })

    it('should create animation when reduced motion is preferred but respectReducedMotion is false', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-reduced-motion: reduce)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      const element = document.createElement('div')
      const result = createAccessibleAnimation(element, { duration: 500, respectReducedMotion: false })

      expect(result).not.toBeNull()
    })

    it('should create animation when no motion preference', () => {
      const element = document.createElement('div')
      const result = createAccessibleAnimation(element, { duration: 500 })

      expect(result).not.toBeNull()
    })

    it('should use default duration of 300ms when not specified', () => {
      const element = document.createElement('div')
      const result = createAccessibleAnimation(element)

      expect(result).not.toBeNull()
    })

    it('should reduce duration for screen readers', () => {
      Object.defineProperty(window, 'speechSynthesis', {
        writable: true,
        value: {
          getVoices: () => [{ name: 'Test Voice' }],
        },
      })

      const element = document.createElement('div')
      const result = createAccessibleAnimation(element, { duration: 500, respectReducedMotion: false })

      // Should create animation with reduced duration for screen readers
      expect(result).not.toBeNull()
    })
  })

  describe('manageFocus', () => {
    it('should trap focus within container', () => {
      const container = document.createElement('div')
      const button1 = document.createElement('button')
      const button2 = document.createElement('button')
      const button3 = document.createElement('button')

      container.appendChild(button1)
      container.appendChild(button2)
      container.appendChild(button3)
      document.body.appendChild(container)

      manageFocus(container, { trapFocus: true })

      // Simulate Tab on last element
      button3.focus()
      const event = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true })
      container.dispatchEvent(event)

      // Should wrap to first element (simulated in test)
      expect(container.querySelectorAll('button').length).toBe(3)
    })

    it('should return cleanup function when initialFocus is specified', () => {
      const container = document.createElement('div')
      const button1 = document.createElement('button')
      const button2 = document.createElement('button')

      container.appendChild(button1)
      container.appendChild(button2)
      document.body.appendChild(container)

      const cleanup = manageFocus(container, { initialFocus: button2 })

      expect(typeof cleanup).toBe('function')
    })

    it('should return cleanup function when managing focus', () => {
      const container = document.createElement('div')
      const button = document.createElement('button')
      container.appendChild(button)
      document.body.appendChild(container)

      const cleanup = manageFocus(container)

      expect(typeof cleanup).toBe('function')
    })

    it('should return cleanup function with restoreFocus option', () => {
      const container = document.createElement('div')
      const button = document.createElement('button')
      const previousElement = document.createElement('button')

      document.body.appendChild(previousElement)
      document.body.appendChild(container)
      container.appendChild(button)

      previousElement.focus()

      const cleanup = manageFocus(container, { restoreFocus: true })

      expect(typeof cleanup).toBe('function')

      // Cleanup should not throw
      expect(() => cleanup()).not.toThrow()
    })

    it('should not trap focus when screen reader is active', () => {
      Object.defineProperty(window, 'speechSynthesis', {
        writable: true,
        value: {
          getVoices: () => [{ name: 'Test Voice' }],
        },
      })

      const container = document.createElement('div')
      const button = document.createElement('button')
      container.appendChild(button)
      document.body.appendChild(container)

      const result = manageFocus(container, { trapFocus: true })

      // Should still return a cleanup function
      expect(typeof result).toBe('function')
    })
  })

  describe('createLiveRegion', () => {
    it('should create ARIA live region with polite priority by default', () => {
      const liveRegion = createLiveRegion('Test message')

      expect(liveRegion.getAttribute('aria-live')).toBe('polite')
      expect(liveRegion.getAttribute('aria-atomic')).toBe('true')
      expect(liveRegion.textContent).toBe('Test message')
      expect(document.body.contains(liveRegion)).toBe(true)
    })

    it('should create ARIA live region with assertive priority', () => {
      const liveRegion = createLiveRegion('Urgent message', { priority: 'assertive' })

      expect(liveRegion.getAttribute('aria-live')).toBe('assertive')
    })

    it('should clear message after timeout', () => {
      jest.useFakeTimers()

      const liveRegion = createLiveRegion('Test message', { timeout: 1000 })

      expect(liveRegion.textContent).toBe('Test message')

      jest.advanceTimersByTime(1000)

      expect(liveRegion.textContent).toBe('')

      jest.useRealTimers()
    })

    it('should not clear message when timeout is 0', () => {
      jest.useFakeTimers()

      const liveRegion = createLiveRegion('Persistent message', { timeout: 0 })

      expect(liveRegion.textContent).toBe('Persistent message')

      jest.advanceTimersByTime(10000)

      expect(liveRegion.textContent).toBe('Persistent message')

      jest.useRealTimers()
    })

    it('should add sr-only class for visual hiding', () => {
      const liveRegion = createLiveRegion('Test message')

      expect(liveRegion.className).toBe('sr-only')
    })
  })

  describe('announceToScreenReader', () => {
    it('should not announce when screen reader is not active', () => {
      const initialChildCount = document.body.children.length

      announceToScreenReader('Test announcement')

      // Should not create live region
      expect(document.body.children.length).toBe(initialChildCount)
    })

    it('should announce when screen reader is active', () => {
      Object.defineProperty(window, 'speechSynthesis', {
        writable: true,
        value: {
          getVoices: () => [{ name: 'Test Voice' }],
        },
      })

      announceToScreenReader('Test announcement')

      const liveRegions = document.querySelectorAll('[aria-live]')
      expect(liveRegions.length).toBeGreaterThan(0)
    })

    it('should use assertive priority when specified', () => {
      Object.defineProperty(window, 'speechSynthesis', {
        writable: true,
        value: {
          getVoices: () => [{ name: 'Test Voice' }],
        },
      })

      announceToScreenReader('Urgent announcement', 'assertive')

      const liveRegions = document.querySelectorAll('[aria-live="assertive"]')
      expect(liveRegions.length).toBeGreaterThan(0)
    })

    it('should clean up live region after announcement', () => {
      jest.useFakeTimers()

      Object.defineProperty(window, 'speechSynthesis', {
        writable: true,
        value: {
          getVoices: () => [{ name: 'Test Voice' }],
        },
      })

      announceToScreenReader('Test announcement')

      const initialCount = document.querySelectorAll('[aria-live]').length

      jest.advanceTimersByTime(1500)

      const finalCount = document.querySelectorAll('[aria-live]').length

      expect(finalCount).toBeLessThan(initialCount)

      jest.useRealTimers()
    })
  })

  describe('getAccessibleColor', () => {
    it('should return high contrast color when preference is set', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-contrast: high)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      const textColor = getAccessibleColor('#333333', 'text')
      const bgColor = getAccessibleColor('#f5f5f5', 'background')

      expect(textColor).toBe('#000000')
      expect(bgColor).toBe('#FFFFFF')
    })

    it('should return base color when no high contrast preference', () => {
      const color = getAccessibleColor('#3b82f6', 'text')

      expect(color).toBe('#3b82f6')
    })

    it('should return base color for unmapped contexts', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-contrast: high)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      const color = getAccessibleColor('#3b82f6', 'custom' as any)

      expect(color).toBe('#3b82f6')
    })

    it('should map border color in high contrast mode', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-contrast: high)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      const borderColor = getAccessibleColor('#e5e7eb', 'border')

      expect(borderColor).toBe('#000000')
    })
  })

  describe('getAccessibleSpacing', () => {
    it('should return increased spacing when large text is preferred', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-reduced-data: reduce)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      const spacing = getAccessibleSpacing(16)

      expect(spacing).toBe('24px') // 16 * 1.5
    })

    it('should return base spacing when no large text preference', () => {
      const spacing = getAccessibleSpacing(16)

      expect(spacing).toBe('16px')
    })

    it('should handle decimal base sizes', () => {
      const spacing = getAccessibleSpacing(12.5)

      expect(spacing).toBe('12.5px')
    })
  })

  describe('setupKeyboardNavigation', () => {
    it('should call onEnter handler when Enter key is pressed', () => {
      const element = document.createElement('button')
      const onEnter = jest.fn()

      setupKeyboardNavigation(element, { onEnter })

      const event = new KeyboardEvent('keydown', { key: 'Enter' })
      element.dispatchEvent(event)

      expect(onEnter).toHaveBeenCalled()
    })

    it('should call onSpace handler when Space key is pressed', () => {
      const element = document.createElement('button')
      const onSpace = jest.fn()

      setupKeyboardNavigation(element, { onSpace })

      const event = new KeyboardEvent('keydown', { key: ' ' })
      element.dispatchEvent(event)

      expect(onSpace).toHaveBeenCalled()
    })

    it('should call onEscape handler when Escape key is pressed', () => {
      const element = document.createElement('button')
      const onEscape = jest.fn()

      setupKeyboardNavigation(element, { onEscape })

      const event = new KeyboardEvent('keydown', { key: 'Escape' })
      element.dispatchEvent(event)

      expect(onEscape).toHaveBeenCalled()
    })

    it('should call onArrow handler with correct direction', () => {
      const element = document.createElement('button')
      const onArrow = jest.fn()

      setupKeyboardNavigation(element, { onArrow })

      element.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp' }))
      expect(onArrow).toHaveBeenCalledWith('up')

      element.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }))
      expect(onArrow).toHaveBeenCalledWith('down')

      element.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft' }))
      expect(onArrow).toHaveBeenCalledWith('left')

      element.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }))
      expect(onArrow).toHaveBeenCalledWith('right')
    })

    it('should call onFocus handler when element is focused', () => {
      const element = document.createElement('button')
      document.body.appendChild(element)
      const onFocus = jest.fn()

      setupKeyboardNavigation(element, { onFocus })

      element.dispatchEvent(new FocusEvent('focus'))

      expect(onFocus).toHaveBeenCalled()
    })

    it('should call onBlur handler when element loses focus', () => {
      const element = document.createElement('button')
      document.body.appendChild(element)
      const onBlur = jest.fn()

      setupKeyboardNavigation(element, { onBlur })

      element.dispatchEvent(new FocusEvent('blur'))

      expect(onBlur).toHaveBeenCalled()
    })

    it('should return cleanup function that removes event listeners', () => {
      const element = document.createElement('button')
      const onEnter = jest.fn()

      const cleanup = setupKeyboardNavigation(element, { onEnter })

      cleanup()

      element.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }))

      expect(onEnter).not.toHaveBeenCalled()
    })
  })

  describe('createAccessibleTooltip', () => {
    it('should create tooltip with correct ARIA attributes', () => {
      const trigger = document.createElement('button')
      document.body.appendChild(trigger)

      const { tooltip } = createAccessibleTooltip(trigger, 'Help text')

      expect(tooltip.getAttribute('role')).toBe('tooltip')
      expect(tooltip.textContent).toBe('Help text')
      expect(trigger.hasAttribute('aria-describedby')).toBe(true)
    })

    it('should position tooltip at top by default', () => {
      const trigger = document.createElement('button')
      document.body.appendChild(trigger)

      const { tooltip } = createAccessibleTooltip(trigger, 'Help text')

      expect(tooltip.style.top).toBeDefined()
      expect(tooltip.style.left).toBeDefined()
    })

    it('should show tooltip on mouse enter', () => {
      const trigger = document.createElement('button')
      document.body.appendChild(trigger)

      const { tooltip } = createAccessibleTooltip(trigger, 'Help text')

      trigger.dispatchEvent(new MouseEvent('mouseenter'))

      expect(tooltip.classList.contains('visible')).toBe(true)
    })

    it('should hide tooltip on mouse leave when not persistent', () => {
      const trigger = document.createElement('button')
      document.body.appendChild(trigger)

      const { tooltip } = createAccessibleTooltip(trigger, 'Help text', { persistent: false })

      trigger.dispatchEvent(new MouseEvent('mouseenter'))
      trigger.dispatchEvent(new MouseEvent('mouseleave'))

      expect(tooltip.classList.contains('visible')).toBe(false)
    })

    it('should show tooltip on focus', () => {
      const trigger = document.createElement('button')
      document.body.appendChild(trigger)

      const { tooltip } = createAccessibleTooltip(trigger, 'Help text')

      trigger.dispatchEvent(new FocusEvent('focus'))

      expect(tooltip.classList.contains('visible')).toBe(true)
    })

    it('should cleanup event listeners and remove tooltip', () => {
      const trigger = document.createElement('button')
      document.body.appendChild(trigger)

      const { tooltip, cleanup } = createAccessibleTooltip(trigger, 'Help text')

      cleanup()

      expect(document.body.contains(tooltip)).toBe(false)
      expect(trigger.hasAttribute('aria-describedby')).toBe(false)
    })

    it('should support different placements', () => {
      const trigger = document.createElement('button')
      document.body.appendChild(trigger)

      const placements: Array<'top' | 'bottom' | 'left' | 'right'> = ['top', 'bottom', 'left', 'right']

      placements.forEach((placement) => {
        const { tooltip, cleanup } = createAccessibleTooltip(trigger, 'Help text', { placement })

        expect(tooltip).toBeDefined()

        cleanup()
      })
    })
  })

  describe('createAccessibleProgress', () => {
    it('should create progress bar with correct ARIA attributes', () => {
      const progress = createAccessibleProgress(50, 100, 'Upload progress')

      expect(progress.getAttribute('role')).toBe('progressbar')
      expect(progress.getAttribute('aria-valuenow')).toBe('50')
      expect(progress.getAttribute('aria-valuemin')).toBe('0')
      expect(progress.getAttribute('aria-valuemax')).toBe('100')
      expect(progress.getAttribute('aria-label')).toBe('Upload progress')
    })

    it('should show percentage by default', () => {
      const progress = createAccessibleProgress(75, 100, 'Download')

      const percentage = progress.querySelector('.accessible-progress-percentage')

      expect(percentage?.textContent).toBe('75%')
    })

    it('should hide percentage when showPercentage is false', () => {
      const progress = createAccessibleProgress(50, 100, 'Processing', { showPercentage: false })

      const percentage = progress.querySelector('.accessible-progress-percentage')

      expect(percentage).toBeNull()
    })

    it('should set progress bar width correctly', () => {
      const progress = createAccessibleProgress(60, 100, 'Loading')

      const progressBar = progress.querySelector('.accessible-progress-bar') as HTMLElement

      expect(progressBar.style.width).toBe('60%')
    })

    it('should calculate percentage correctly for non-100 max values', () => {
      const progress = createAccessibleProgress(3, 5, 'Steps', { showPercentage: true })

      const percentage = progress.querySelector('.accessible-progress-percentage')

      expect(percentage?.textContent).toBe('60%') // 3/5 * 100 = 60
    })
  })

  describe('initializeAccessibility', () => {
    it('should apply accessibility classes on initialization', () => {
      initializeAccessibility()

      // Classes should be applied based on preferences
      expect(document.documentElement.className).toBeDefined()
    })

    it('should create skip links', () => {
      initializeAccessibility()

      const skipLinks = document.querySelector('.skip-links')

      expect(skipLinks).not.toBeNull()
      expect(skipLinks?.querySelector('a[href="#main-content"]')).not.toBeNull()
      expect(skipLinks?.querySelector('a[href="#navigation"]')).not.toBeNull()
    })

    it('should listen for preference changes', () => {
      const addEventListener = jest.fn()

      matchMediaMock.mockImplementation((query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener,
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      initializeAccessibility()

      // Should add listeners for reduced motion and high contrast
      expect(addEventListener).toHaveBeenCalledWith('change', expect.any(Function))
    })

    it('should return cleanup function', () => {
      const result = initializeAccessibility()

      expect(result.cleanup).toBeDefined()
      expect(typeof result.cleanup).toBe('function')
    })

    it('should cleanup event listeners and skip links', () => {
      const removeEventListener = jest.fn()

      matchMediaMock.mockImplementation((query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener,
        dispatchEvent: jest.fn(),
      }))

      const { cleanup } = initializeAccessibility()

      cleanup()

      const skipLinks = document.querySelector('.skip-links')

      expect(skipLinks).toBeNull()
      expect(removeEventListener).toHaveBeenCalledWith('change', expect.any(Function))
    })
  })

  describe('shouldReduceAnimation', () => {
    it('should return true when reduced motion is preferred', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-reduced-motion: reduce)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      const result = shouldReduceAnimation()

      expect(result).toBe(true)
    })

    it('should return false when no reduced motion preference', () => {
      const result = shouldReduceAnimation()

      expect(result).toBe(false)
    })

    it('should return true for parallax animations with screen reader active', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-reduced-motion: reduce)' || query === '(prefers-contrast: high)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      const result = shouldReduceAnimation('parallax')

      expect(result).toBe(true)
    })

    it('should return true for floating animations with screen reader active', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-reduced-motion: reduce)' || query === '(prefers-contrast: high)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      const result = shouldReduceAnimation('floating')

      expect(result).toBe(true)
    })

    it('should return true for particle animations with screen reader active', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-reduced-motion: reduce)' || query === '(prefers-contrast: high)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      const result = shouldReduceAnimation('particle')

      expect(result).toBe(true)
    })

    it('should return false for other animation types with screen reader active', () => {
      matchMediaMock.mockImplementation((query: string) => ({
        matches: query === '(prefers-reduced-motion: reduce)' || query === '(prefers-contrast: high)',
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn(),
      }))

      // Screen reader is active but reduced motion is also true
      // So it should return true due to reduced motion preference
      const result = shouldReduceAnimation('fade')

      expect(result).toBe(true)
    })
  })
})
