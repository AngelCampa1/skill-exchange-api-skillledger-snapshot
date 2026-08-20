import { logger } from '@/utils/logger';
/**
 * Animation Performance Optimization Utilities
 *
 * This file provides utilities and hooks for optimizing animations
 * to ensure smooth performance across all devices.
 */

import { useState, useEffect } from 'react'

// Detect if user prefers reduced motion
export const prefersReducedMotion = () => {
  if (typeof window === 'undefined') return false

  return window.matchMedia('(prefers-reduced-motion: reduce)').matches
}

// Check device capabilities for animation performance
export const getDevicePerformanceTier = (): 'high' | 'medium' | 'low' => {
  if (typeof window === 'undefined') return 'medium'

  // Check for hardware acceleration
  const canvas = document.createElement('canvas')
  const gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl') as WebGLRenderingContext | null
  const debugInfo = gl?.getExtension('WEBGL_debug_renderer_info') as { UNMASKED_VENDOR_WEBGL: number; UNMASKED_RENDERER_WEBGL: number } | null

  const renderer = debugInfo ? (gl?.getParameter(debugInfo.UNMASKED_RENDERER_WEBGL) || '') : ''
  const isLowEnd = renderer.includes('Mali') ||
                   renderer.includes('Adreno 3') ||
                   renderer.includes('PowerVR SGX') ||
                   navigator.hardwareConcurrency <= 2

  // BUG-HIGH-005 FIX: Properly type deviceMemory instead of using 'any'
  // deviceMemory is part of the Device Memory API
  const memory = ('deviceMemory' in navigator ? (navigator as Navigator & { deviceMemory?: number }).deviceMemory : undefined) || 4
  const isLowMemory = memory <= 2

  if (isLowEnd || isLowMemory) return 'low'
  if (memory >= 8 && navigator.hardwareConcurrency >= 8) return 'high'
  return 'medium'
}

// Optimized animation settings based on device capabilities
export const getAnimationSettings = () => {
  const tier = getDevicePerformanceTier()
  const reducedMotion = prefersReducedMotion()

  return {
    tier,
    reducedMotion,
    // Animation durations in milliseconds
    durations: {
      fast: reducedMotion ? 0 : tier === 'high' ? 150 : tier === 'medium' ? 200 : 300,
      normal: reducedMotion ? 0 : tier === 'high' ? 250 : tier === 'medium' ? 350 : 500,
      slow: reducedMotion ? 0 : tier === 'high' ? 400 : tier === 'medium' ? 600 : 800,
      extraSlow: reducedMotion ? 0 : tier === 'high' ? 600 : tier === 'medium' ? 900 : 1200
    },
    // Enable/disable features based on performance
    features: {
      shadows: tier !== 'low' && !reducedMotion,
      blur: tier === 'high' && !reducedMotion,
      transforms: !reducedMotion,
      complexAnimations: tier === 'high' && !reducedMotion,
      particleEffects: tier === 'high' && !reducedMotion,
      gradientAnimations: tier !== 'low' && !reducedMotion,
      hoverEffects: !reducedMotion,
      staggerAnimations: tier !== 'low' && !reducedMotion,
      magneticEffects: tier !== 'low' && !reducedMotion,
      parallax: tier === 'high' && !reducedMotion,
      webgl: tier === 'high' && !reducedMotion
    },
    // Maximum number of concurrent animated elements
    limits: {
      concurrentAnimations: tier === 'high' ? 50 : tier === 'medium' ? 25 : 10,
      particles: tier === 'high' ? 100 : tier === 'medium' ? 50 : 0,
      floatingElements: tier === 'high' ? 20 : tier === 'medium' ? 10 : 5,
      staggerItems: tier === 'high' ? 50 : tier === 'medium' ? 25 : 10
    }
  }
}

// Debounce function for performance
export const debounce = <T extends (...args: any[]) => void>(
  func: T,
  wait: number,
  immediate?: boolean
): ((...args: Parameters<T>) => void) => {
  let timeout: NodeJS.Timeout | null = null

  return function executedFunction(...args: Parameters<T>) {
    const later = () => {
      timeout = null
      if (!immediate) func(...args)
    }

    const callNow = immediate && !timeout

    if (timeout) clearTimeout(timeout)
    timeout = setTimeout(later, wait)

    if (callNow) func(...args)
  }
}

// Throttle function for performance
export const throttle = <T extends (...args: any[]) => void>(
  func: T,
  limit: number
): ((...args: Parameters<T>) => void) => {
  let inThrottle: boolean

  return function executedFunction(...args: Parameters<T>) {
    if (!inThrottle) {
      func(...args)
      inThrottle = true
      setTimeout(() => inThrottle = false, limit)
    }
  }
}

// Intersection Observer for lazy loading animations
export const createLazyAnimationObserver = (
  callback: (entries: IntersectionObserverEntry[]) => void,
  options?: IntersectionObserverInit
) => {
  const settings = getAnimationSettings()

  // Disable lazy loading for reduced motion
  if (settings.reducedMotion) {
    return null
  }

  const defaultOptions: IntersectionObserverInit = {
    root: null,
    rootMargin: '50px',
    threshold: 0.1,
    ...options
  }

  return new IntersectionObserver(callback, defaultOptions)
}

// Performance monitoring for animations
export class AnimationPerformanceMonitor {
  private frameCount = 0
  private lastTime = performance.now()
  private fps = 60
  private animationDrops = 0
  private maxAnimationDrops = 3
  // BUG-HIGH-007 FIX: Add automatic stop after initialization period
  private monitoringDuration = 10000 // Monitor for 10 seconds then stop
  private startTime = performance.now()

  constructor() {
    this.startMonitoring()
  }

  private startMonitoring() {
    const measure = () => {
      const currentTime = performance.now()
      const delta = currentTime - this.lastTime

      // BUG-HIGH-007 FIX: Auto-stop after monitoring duration
      if (currentTime - this.startTime >= this.monitoringDuration) {
        this.stop()
        return
      }

      if (delta >= 1000) {
        this.fps = Math.round((this.frameCount * 1000) / delta)

        // Check if performance is degrading
        if (this.fps < 30) {
          this.animationDrops++

          // Disable animations if performance is consistently poor
          if (this.animationDrops >= this.maxAnimationDrops) {
            this.disableAnimations()
            // BUG-HIGH-007 FIX: Stop monitoring after disabling animations
            this.stop()
            return
          }
        } else {
          this.animationDrops = Math.max(0, this.animationDrops - 1)
        }

        this.frameCount = 0
        this.lastTime = currentTime
      }

      this.frameCount++

      if (!this.stopped) {
        requestAnimationFrame(measure)
      }
    }

    requestAnimationFrame(measure)
  }

  private stopped = false

  private disableAnimations() {
    // Add a CSS class to disable animations
    document.documentElement.classList.add('reduce-animations')

    // Fire custom event
    window.dispatchEvent(new CustomEvent('animationsDisabled'))
  }

  public stop() {
    this.stopped = true
  }

  public getFPS() {
    return this.fps
  }

  public getAnimationDrops() {
    return this.animationDrops
  }
}

// CSS-in-JS animation utilities with performance considerations
export const createOptimizedAnimation = (
  keyframes: Keyframe[],
  options: KeyframeAnimationOptions = {}
): Animation => {
  const settings = getAnimationSettings()

  // Disable animation if reduced motion is preferred
  if (settings.reducedMotion) {
    const dummyElement = document.createElement('div')
    const animation = dummyElement.animate([], { duration: 0 })
    return animation
  }

  // Adjust duration based on device performance
  const duration = options.duration || settings.durations.normal

  const dummyElement = document.createElement('div')
  return dummyElement.animate(keyframes, {
    duration,
    easing: 'ease-out',
    fill: 'both',
    ...options
  })
}

// Optimized spring animations
export const createSpringAnimation = (
  target: HTMLElement,
  to: Record<string, number>,
  config: {
    tension?: number
    friction?: number
    mass?: number
  } = {}
): Animation => {
  const settings = getAnimationSettings()

  if (settings.reducedMotion) {
    // Instant update for reduced motion
    Object.entries(to).forEach(([property, value]) => {
      target.style.setProperty(property, `${value}px`)
    })
    const dummyElement = document.createElement('div')
    return dummyElement.animate([], { duration: 0 })
  }

  const tension = config.tension || (settings.tier === 'high' ? 300 : 200)
  const friction = config.friction || (settings.tier === 'high' ? 20 : 30)
  const mass = config.mass || 1

  // Simple spring implementation
  const dummyElement = document.createElement('div')
  return dummyElement.animate([
    { transform: 'translate3d(0, 0, 0)' },
    { transform: `translate3d(${to.x || 0}px, ${to.y || 0}px, 0)` }
  ], {
    duration: 600,
    easing: `cubic-bezier(0.68, -0.55, 0.265, 1.55)`, // Custom spring easing
    fill: 'both'
  })
}

// Magnetic effect utility
export const createMagneticEffect = (
  element: HTMLElement,
  strength: number = 0.3
): (() => void) => {
  const settings = getAnimationSettings()

  if (settings.reducedMotion || !settings.features.magneticEffects) {
    return () => {} // No-op for reduced motion
  }

  let isAnimating = false
  let currentX = 0
  let currentY = 0
  // BUG-MED-005 FIX: Track cleanup state and pending timers
  let isCleanedUp = false
  let pendingResetTimer: NodeJS.Timeout | null = null

  const handleMouseMove = throttle((e: MouseEvent) => {
    // BUG-MED-005 FIX: Check cleanup state before executing
    if (isCleanedUp || isAnimating) return

    const rect = element.getBoundingClientRect()
    const centerX = rect.left + rect.width / 2
    const centerY = rect.top + rect.height / 2

    const deltaX = (e.clientX - centerX) * strength
    const deltaY = (e.clientY - centerY) * strength

    currentX = deltaX
    currentY = deltaY

    element.style.transform = `translate3d(${deltaX}px, ${deltaY}px, 0) scale(1.05)`
  }, 16) // ~60fps throttling

  const handleMouseLeave = () => {
    // BUG-MED-005 FIX: Check cleanup state before executing
    if (isCleanedUp) return

    isAnimating = true

    element.style.transform = `translate3d(0, 0, 0) scale(1)`

    // BUG-MED-005 FIX: Clear any existing timer before setting new one
    if (pendingResetTimer) {
      clearTimeout(pendingResetTimer)
    }

    pendingResetTimer = setTimeout(() => {
      // BUG-MED-005 FIX: Check cleanup state in async callback
      if (!isCleanedUp) {
        isAnimating = false
        currentX = 0
        currentY = 0
      }
      pendingResetTimer = null
    }, 300)
  }

  element.addEventListener('mousemove', handleMouseMove)
  element.addEventListener('mouseleave', handleMouseLeave)

  // BUG-MED-005 FIX: Enhanced cleanup function with state tracking
  return () => {
    // Mark as cleaned up to prevent any pending operations
    isCleanedUp = true

    // Clear any pending reset timer
    if (pendingResetTimer) {
      clearTimeout(pendingResetTimer)
      pendingResetTimer = null
    }

    // Remove event listeners
    element.removeEventListener('mousemove', handleMouseMove)
    element.removeEventListener('mouseleave', handleMouseLeave)

    // Reset element transform
    element.style.transform = ''
  }
}

// Stagger animation utility
export const createStaggerAnimation = (
  elements: Element[],
  staggerDelay: number = 100,
  animationClass: string = 'animate-slide-up'
): void => {
  const settings = getAnimationSettings()

  if (settings.reducedMotion || !settings.features.staggerAnimations) {
    // Show all elements immediately
    elements.forEach(element => {
      element.classList.remove('opacity-0')
      element.classList.add('opacity-100')
    })
    return
  }

  elements.forEach((element, index) => {
    // Hide elements initially
    element.classList.add('opacity-0')

    // Stagger the animation
    setTimeout(() => {
      element.classList.remove('opacity-0')
      element.classList.add(animationClass)

      // Clean up animation classes after completion
      setTimeout(() => {
        element.classList.remove(animationClass)
      }, 1000)
    }, index * staggerDelay)
  })
}

// Parallax effect utility
export const createParallaxEffect = (
  elements: NodeListOf<HTMLElement>,
  speed: number = 0.5
): (() => void) => {
  const settings = getAnimationSettings()

  if (settings.reducedMotion || !settings.features.parallax) {
    return () => {} // No-op for reduced motion
  }

  const handleScroll = throttle(() => {
    const scrollY = window.scrollY

    elements.forEach(element => {
      const rect = element.getBoundingClientRect()
      const elementTop = rect.top + scrollY
      const windowHeight = window.innerHeight

      // Only animate elements in viewport
      if (rect.top < windowHeight && rect.bottom > 0) {
        const yPos = -(scrollY - elementTop) * speed
        element.style.transform = `translate3d(0, ${yPos}px, 0)`
      }
    })
  }, 16) // ~60fps throttling

  window.addEventListener('scroll', handleScroll)

  return () => {
    window.removeEventListener('scroll', handleScroll)
    elements.forEach(element => {
      element.style.transform = ''
    })
  }
}

// Performance-aware animation hook
export const useOptimizedAnimation = (
  animationType: 'hover' | 'stagger' | 'parallax' | 'magnetic' | 'complex',
  enabled: boolean = true
) => {
  const [isSupported, setIsSupported] = useState(true)
  const settings = getAnimationSettings()

  useEffect(() => {
    const shouldEnable = enabled &&
                      !settings.reducedMotion &&
                      settings.features[`${animationType}Effects` as keyof typeof settings.features]

    setIsSupported(shouldEnable)
  }, [enabled, animationType, settings])

  return {
    isSupported,
    settings,
    shouldAnimate: isSupported && enabled,
    cssClass: settings.reducedMotion ? 'no-animation' : '',
    duration: settings.durations
  }
}

// CSS variables for dynamic animation control
export const setAnimationCSSVariables = () => {
  const settings = getAnimationSettings()

  const root = document.documentElement

  // Set CSS custom properties
  root.style.setProperty('--animation-duration-fast', `${settings.durations.fast}ms`)
  root.style.setProperty('--animation-duration-normal', `${settings.durations.normal}ms`)
  root.style.setProperty('--animation-duration-slow', `${settings.durations.slow}ms`)
  root.style.setProperty('--animation-duration-extra-slow', `${settings.durations.extraSlow}ms`)

  // Set feature flags
  root.style.setProperty('--enable-shadows', settings.features.shadows ? '1' : '0')
  root.style.setProperty('--enable-blur', settings.features.blur ? '1' : '0')
  root.style.setProperty('--enable-complex-animations', settings.features.complexAnimations ? '1' : '0')
  root.style.setProperty('--enable-gradient-animations', settings.features.gradientAnimations ? '1' : '0')

  // Set performance class
  root.classList.add(`performance-${settings.tier}`)

  if (settings.reducedMotion) {
    root.classList.add('reduce-motion')
  }
}

// Initialize performance optimizations
export const initializeAnimationOptimizations = () => {
  // Set CSS variables
  setAnimationCSSVariables()

  // Start performance monitoring
  const monitor = new AnimationPerformanceMonitor()

  // Listen for performance changes
  window.addEventListener('animationsDisabled', () => {
    logger.debug('Animations disabled due to performance constraints')
  })

  return monitor
}

// Cleanup utility for animations
export const cleanupAnimations = (element: Element) => {
  // Remove all animation-related event listeners
  const clone = element.cloneNode(true)
  element.parentNode?.replaceChild(clone, element)

  return clone
}

const animationUtils = {
  prefersReducedMotion,
  getDevicePerformanceTier,
  getAnimationSettings,
  debounce,
  throttle,
  createLazyAnimationObserver,
  AnimationPerformanceMonitor,
  createOptimizedAnimation,
  createSpringAnimation,
  createMagneticEffect,
  createStaggerAnimation,
  createParallaxEffect,
  useOptimizedAnimation,
  setAnimationCSSVariables,
  initializeAnimationOptimizations,
  cleanupAnimations
}

export default animationUtils