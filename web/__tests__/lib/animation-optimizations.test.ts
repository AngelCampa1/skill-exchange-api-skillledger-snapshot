/**
 * Tests for animation-optimizations.ts utilities
 *
 * This file validates animation performance optimization utilities
 */

import {
  prefersReducedMotion,
  getDevicePerformanceTier,
  getAnimationSettings,
  debounce,
  throttle,
  setAnimationCSSVariables,
  initializeAnimationOptimizations,
  cleanupAnimations,
} from '@/lib/animation-optimizations'

describe('animation-optimizations utilities', () => {
  let matchMediaMock: jest.Mock

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

    // Clear any CSS variables
    document.documentElement.style.cssText = ''
  })

  afterEach(() => {
    jest.clearAllMocks()
    document.documentElement.style.cssText = ''
    document.body.innerHTML = ''
  })

  describe('prefersReducedMotion', () => {
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

      expect(prefersReducedMotion()).toBe(true)
    })

    it('should return false when reduced motion is not preferred', () => {
      expect(prefersReducedMotion()).toBe(false)
    })
  })

  describe('getDevicePerformanceTier', () => {
    beforeEach(() => {
      // Mock WebGL context
      HTMLCanvasElement.prototype.getContext = jest.fn((contextType) => {
        if (contextType === 'webgl' || contextType === 'experimental-webgl') {
          return {
            getExtension: jest.fn(() => ({
              UNMASKED_VENDOR_WEBGL: 0x9245,
              UNMASKED_RENDERER_WEBGL: 0x9246,
            })),
            getParameter: jest.fn(() => 'MockRenderer'),
          } as any
        }
        return null
      })

      // Mock navigator properties
      Object.defineProperty(navigator, 'hardwareConcurrency', {
        writable: true,
        value: 4,
      })

      Object.defineProperty(navigator, 'deviceMemory', {
        writable: true,
        value: 4,
      })
    })

    it('should return "medium" for average devices', () => {
      expect(getDevicePerformanceTier()).toBe('medium')
    })

    it('should return "high" for high-end devices', () => {
      Object.defineProperty(navigator, 'hardwareConcurrency', {
        writable: true,
        value: 8,
      })

      Object.defineProperty(navigator, 'deviceMemory', {
        writable: true,
        value: 8,
      })

      expect(getDevicePerformanceTier()).toBe('high')
    })

    it('should return "low" for low-end devices with low memory', () => {
      Object.defineProperty(navigator, 'deviceMemory', {
        writable: true,
        value: 2,
      })

      expect(getDevicePerformanceTier()).toBe('low')
    })

    it('should return "low" for devices with low CPU cores', () => {
      Object.defineProperty(navigator, 'hardwareConcurrency', {
        writable: true,
        value: 2,
      })

      expect(getDevicePerformanceTier()).toBe('low')
    })

    it('should return "low" for devices with Mali GPU', () => {
      HTMLCanvasElement.prototype.getContext = jest.fn((contextType) => {
        if (contextType === 'webgl' || contextType === 'experimental-webgl') {
          return {
            getExtension: jest.fn(() => ({
              UNMASKED_VENDOR_WEBGL: 0x9245,
              UNMASKED_RENDERER_WEBGL: 0x9246,
            })),
            getParameter: jest.fn(() => 'Mali-G72'),
          } as any
        }
        return null
      })

      expect(getDevicePerformanceTier()).toBe('low')
    })

    it('should handle devices without deviceMemory API', () => {
      const originalDeviceMemory = Object.getOwnPropertyDescriptor(navigator, 'deviceMemory')

      Object.defineProperty(navigator, 'deviceMemory', {
        writable: true,
        value: undefined,
      })

      const tier = getDevicePerformanceTier()

      expect(tier).toBeDefined()
      expect(['high', 'medium', 'low']).toContain(tier)

      if (originalDeviceMemory) {
        Object.defineProperty(navigator, 'deviceMemory', originalDeviceMemory)
      }
    })
  })

  describe('getAnimationSettings', () => {
    it('should return settings for high-end devices', () => {
      Object.defineProperty(navigator, 'hardwareConcurrency', {
        writable: true,
        value: 8,
      })

      Object.defineProperty(navigator, 'deviceMemory', {
        writable: true,
        value: 8,
      })

      const settings = getAnimationSettings()

      expect(settings.tier).toBe('high')
      expect(settings.reducedMotion).toBe(false)
      expect(settings.durations.fast).toBe(150)
      expect(settings.features.webgl).toBe(true)
      expect(settings.features.parallax).toBe(true)
      expect(settings.limits.concurrentAnimations).toBe(50)
    })

    it('should return settings for medium devices', () => {
      Object.defineProperty(navigator, 'hardwareConcurrency', {
        writable: true,
        value: 4,
      })

      Object.defineProperty(navigator, 'deviceMemory', {
        writable: true,
        value: 4,
      })

      const settings = getAnimationSettings()

      expect(settings.tier).toBe('medium')
      expect(settings.durations.fast).toBe(200)
      expect(settings.features.blur).toBe(false)
      expect(settings.limits.concurrentAnimations).toBe(25)
    })

    it('should return settings for low-end devices', () => {
      Object.defineProperty(navigator, 'hardwareConcurrency', {
        writable: true,
        value: 2,
      })

      Object.defineProperty(navigator, 'deviceMemory', {
        writable: true,
        value: 2,
      })

      const settings = getAnimationSettings()

      expect(settings.tier).toBe('low')
      expect(settings.durations.fast).toBe(300)
      expect(settings.features.shadows).toBe(false)
      expect(settings.limits.particles).toBe(0)
    })

    it('should disable all animations when reduced motion is preferred', () => {
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

      const settings = getAnimationSettings()

      expect(settings.reducedMotion).toBe(true)
      expect(settings.durations.fast).toBe(0)
      expect(settings.durations.normal).toBe(0)
      expect(settings.durations.slow).toBe(0)
      expect(settings.durations.extraSlow).toBe(0)
      expect(settings.features.transforms).toBe(false)
      expect(settings.features.hoverEffects).toBe(false)
    })

    it('should return all duration types', () => {
      const settings = getAnimationSettings()

      expect(settings.durations).toHaveProperty('fast')
      expect(settings.durations).toHaveProperty('normal')
      expect(settings.durations).toHaveProperty('slow')
      expect(settings.durations).toHaveProperty('extraSlow')
    })

    it('should return all feature flags', () => {
      const settings = getAnimationSettings()

      expect(settings.features).toHaveProperty('shadows')
      expect(settings.features).toHaveProperty('blur')
      expect(settings.features).toHaveProperty('transforms')
      expect(settings.features).toHaveProperty('complexAnimations')
      expect(settings.features).toHaveProperty('particleEffects')
      expect(settings.features).toHaveProperty('gradientAnimations')
      expect(settings.features).toHaveProperty('hoverEffects')
      expect(settings.features).toHaveProperty('staggerAnimations')
      expect(settings.features).toHaveProperty('magneticEffects')
      expect(settings.features).toHaveProperty('parallax')
      expect(settings.features).toHaveProperty('webgl')
    })

    it('should return all limits', () => {
      const settings = getAnimationSettings()

      expect(settings.limits).toHaveProperty('concurrentAnimations')
      expect(settings.limits).toHaveProperty('particles')
      expect(settings.limits).toHaveProperty('floatingElements')
      expect(settings.limits).toHaveProperty('staggerItems')
    })
  })

  describe('debounce', () => {
    beforeEach(() => {
      jest.useFakeTimers()
    })

    afterEach(() => {
      jest.useRealTimers()
    })

    it('should delay function execution', () => {
      const func = jest.fn()
      const debouncedFunc = debounce(func, 100)

      debouncedFunc()

      expect(func).not.toHaveBeenCalled()

      jest.advanceTimersByTime(50)
      expect(func).not.toHaveBeenCalled()

      jest.advanceTimersByTime(50)
      expect(func).toHaveBeenCalledTimes(1)
    })

    it('should only call function once after multiple rapid calls', () => {
      const func = jest.fn()
      const debouncedFunc = debounce(func, 100)

      debouncedFunc()
      debouncedFunc()
      debouncedFunc()

      jest.advanceTimersByTime(100)

      expect(func).toHaveBeenCalledTimes(1)
    })

    it('should pass arguments to the debounced function', () => {
      const func = jest.fn()
      const debouncedFunc = debounce(func, 100)

      debouncedFunc('arg1', 'arg2')

      jest.advanceTimersByTime(100)

      expect(func).toHaveBeenCalledWith('arg1', 'arg2')
    })

    it('should call immediately when immediate flag is true', () => {
      const func = jest.fn()
      const debouncedFunc = debounce(func, 100, true)

      debouncedFunc()

      expect(func).toHaveBeenCalledTimes(1)

      jest.advanceTimersByTime(100)

      expect(func).toHaveBeenCalledTimes(1)
    })

    it('should reset timer on subsequent calls', () => {
      const func = jest.fn()
      const debouncedFunc = debounce(func, 100)

      debouncedFunc()
      jest.advanceTimersByTime(50)

      debouncedFunc()
      jest.advanceTimersByTime(50)

      expect(func).not.toHaveBeenCalled()

      jest.advanceTimersByTime(50)

      expect(func).toHaveBeenCalledTimes(1)
    })
  })

  describe('throttle', () => {
    beforeEach(() => {
      jest.useFakeTimers()
    })

    afterEach(() => {
      jest.useRealTimers()
    })

    it('should call function immediately on first call', () => {
      const func = jest.fn()
      const throttledFunc = throttle(func, 100)

      throttledFunc()

      expect(func).toHaveBeenCalledTimes(1)
    })

    it('should ignore calls within throttle period', () => {
      const func = jest.fn()
      const throttledFunc = throttle(func, 100)

      throttledFunc()
      throttledFunc()
      throttledFunc()

      expect(func).toHaveBeenCalledTimes(1)
    })

    it('should allow call after throttle period expires', () => {
      const func = jest.fn()
      const throttledFunc = throttle(func, 100)

      throttledFunc()
      expect(func).toHaveBeenCalledTimes(1)

      jest.advanceTimersByTime(100)

      throttledFunc()
      expect(func).toHaveBeenCalledTimes(2)
    })

    it('should pass arguments to the throttled function', () => {
      const func = jest.fn()
      const throttledFunc = throttle(func, 100)

      throttledFunc('arg1', 'arg2')

      expect(func).toHaveBeenCalledWith('arg1', 'arg2')
    })

    it('should maintain throttle period across multiple calls', () => {
      const func = jest.fn()
      const throttledFunc = throttle(func, 100)

      throttledFunc() // Called immediately
      expect(func).toHaveBeenCalledTimes(1)

      throttledFunc() // Ignored (within throttle)
      throttledFunc() // Ignored (within throttle)
      expect(func).toHaveBeenCalledTimes(1)

      jest.advanceTimersByTime(100) // Throttle expires

      throttledFunc() // Called after throttle expires
      expect(func).toHaveBeenCalledTimes(2)
    })
  })

  describe('setAnimationCSSVariables', () => {
    it('should set CSS variables on document root', () => {
      setAnimationCSSVariables()

      const rootStyles = document.documentElement.style

      expect(rootStyles.getPropertyValue('--animation-duration-fast')).toBeTruthy()
      expect(rootStyles.getPropertyValue('--animation-duration-normal')).toBeTruthy()
      expect(rootStyles.getPropertyValue('--animation-duration-slow')).toBeTruthy()
      expect(rootStyles.getPropertyValue('--animation-duration-extra-slow')).toBeTruthy()
    })

    it('should set duration variables as milliseconds', () => {
      setAnimationCSSVariables()

      const rootStyles = document.documentElement.style
      const fastDuration = rootStyles.getPropertyValue('--animation-duration-fast')

      expect(fastDuration).toMatch(/\d+ms/)
    })

    it('should update variables when called multiple times', () => {
      setAnimationCSSVariables()

      const firstValue = document.documentElement.style.getPropertyValue('--animation-duration-fast')

      setAnimationCSSVariables()

      const secondValue = document.documentElement.style.getPropertyValue('--animation-duration-fast')

      expect(secondValue).toBe(firstValue)
    })
  })

  describe('initializeAnimationOptimizations', () => {
    it('should set CSS variables on initialization', () => {
      initializeAnimationOptimizations()

      const rootStyles = document.documentElement.style

      expect(rootStyles.getPropertyValue('--animation-duration-fast')).toBeTruthy()
    })

    it('should return AnimationPerformanceMonitor instance', () => {
      const monitor = initializeAnimationOptimizations()

      expect(monitor).toBeDefined()
      expect(monitor.constructor.name).toBe('AnimationPerformanceMonitor')
    })

    it('should add event listener for animationsDisabled', () => {
      const addEventListenerSpy = jest.spyOn(window, 'addEventListener')

      initializeAnimationOptimizations()

      expect(addEventListenerSpy).toHaveBeenCalledWith('animationsDisabled', expect.any(Function))
    })
  })

  describe('cleanupAnimations', () => {
    it('should clone and replace element', () => {
      const element = document.createElement('div')
      element.classList.add('animated')
      element.id = 'test-element'
      document.body.appendChild(element)

      const clonedElement = cleanupAnimations(element) as HTMLElement

      expect(clonedElement).not.toBe(element)
      expect(clonedElement.id).toBe('test-element')
      expect(document.body.contains(clonedElement)).toBe(true)
      expect(document.body.contains(element)).toBe(false)
    })

    it('should preserve element attributes in clone', () => {
      const element = document.createElement('div')
      element.setAttribute('data-test', 'value')
      element.className = 'test-class'
      document.body.appendChild(element)

      const clonedElement = cleanupAnimations(element) as HTMLElement

      expect(clonedElement.getAttribute('data-test')).toBe('value')
      expect(clonedElement.className).toBe('test-class')
    })

    it('should preserve children in clone', () => {
      const element = document.createElement('div')
      const child = document.createElement('span')
      child.textContent = 'Test content'
      element.appendChild(child)
      document.body.appendChild(element)

      const clonedElement = cleanupAnimations(element) as HTMLElement

      expect(clonedElement.children.length).toBe(1)
      expect(clonedElement.children[0].textContent).toBe('Test content')
    })

    it('should handle elements without parent gracefully', () => {
      const element = document.createElement('div')

      const result = cleanupAnimations(element)

      expect(result).toBeDefined()
    })

    it('should return clone that can be used', () => {
      const element = document.createElement('div')
      element.textContent = 'Original'
      document.body.appendChild(element)

      const clonedElement = cleanupAnimations(element) as HTMLElement

      expect(clonedElement.textContent).toBe('Original')
      clonedElement.textContent = 'Modified'
      expect(clonedElement.textContent).toBe('Modified')
    })
  })
})
