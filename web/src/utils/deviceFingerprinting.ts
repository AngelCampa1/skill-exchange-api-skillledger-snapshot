import { logger } from './logger';
/**
 * Device fingerprinting utility for fraud prevention
 * Collects browser characteristics to generate unique device signatures
 *
 * BUG-HIGH-009 FIX: GDPR/CCPA Compliance
 * - Basic fingerprints (userAgent, language, timezone) collected for security
 * - Advanced fingerprints (canvas, audio, fonts) require explicit consent
 * - Users can opt out via privacy settings
 */

// Consent storage key
const FINGERPRINT_CONSENT_KEY = 'skillledger_fingerprint_consent'

/**
 * Check if user has consented to device fingerprinting
 */
export function hasDeviceFingerprintConsent(): boolean {
  if (typeof window === 'undefined') return false
  try {
    const consent = localStorage.getItem(FINGERPRINT_CONSENT_KEY)
    return consent === 'granted'
  } catch {
    return false
  }
}

/**
 * Set device fingerprint consent status
 */
export function setDeviceFingerprintConsent(granted: boolean): void {
  if (typeof window === 'undefined') return
  try {
    if (granted) {
      localStorage.setItem(FINGERPRINT_CONSENT_KEY, 'granted')
    } else {
      localStorage.removeItem(FINGERPRINT_CONSENT_KEY)
    }
  } catch (error) {
    logger.warn('Failed to save fingerprint consent', { error })
  }
}

/**
 * Clear device fingerprint consent
 */
export function clearDeviceFingerprintConsent(): void {
  setDeviceFingerprintConsent(false)
}

export interface DeviceFingerprint {
  userAgent: string
  timezone: string
  screenResolution: string
  acceptLanguage: string
  platform: string
  colorDepth: number
  deviceMemory?: number
  hardwareConcurrency: number
  canvasFingerprint?: string
  webGLFingerprint?: string
  audioFingerprint?: string
  touchSupport: boolean
  cookieEnabled: boolean
  doNotTrack: string | null
  installedPlugins: string[]
  availableFonts: string[]
}

/**
 * Generate canvas fingerprint
 */
function generateCanvasFingerprint(): string | undefined {
  try {
    const canvas = document.createElement('canvas')
    canvas.width = 200
    canvas.height = 50
    const ctx = canvas.getContext('2d')
    
    if (!ctx) return undefined
    
    // Draw text with different fonts and styles
    ctx.textBaseline = 'top'
    ctx.font = '14px Arial'
    ctx.fillStyle = '#f60'
    ctx.fillRect(125, 1, 62, 20)
    ctx.fillStyle = '#069'
    ctx.fillText('SkillLedger Security 🔒', 2, 15)
    
    // Add gradient
    const gradient = ctx.createLinearGradient(0, 0, 200, 50)
    gradient.addColorStop(0, 'red')
    gradient.addColorStop(1, 'blue')
    ctx.fillStyle = gradient
    ctx.fillText('Device ID', 4, 35)
    
    return canvas.toDataURL()
  } catch (error) {
    logger.warn('Canvas fingerprinting failed', { util: 'deviceFingerprinting', error })
    return undefined
  }
}

/**
 * Generate WebGL fingerprint
 */
function generateWebGLFingerprint(): string | undefined {
  try {
    const canvas = document.createElement('canvas')
    const gl = canvas.getContext('webgl') as WebGLRenderingContext | null || 
               canvas.getContext('experimental-webgl') as WebGLRenderingContext | null
    
    if (!gl) return undefined
    
    const debugInfo = gl.getExtension('WEBGL_debug_renderer_info')
    if (!debugInfo) return undefined
    
    const vendor = gl.getParameter(debugInfo.UNMASKED_VENDOR_WEBGL)
    const renderer = gl.getParameter(debugInfo.UNMASKED_RENDERER_WEBGL)
    
    return `${vendor}~${renderer}`
  } catch (error) {
    logger.warn('WebGL fingerprinting failed', { util: 'deviceFingerprinting', error })
    return undefined
  }
}

/**
 * Generate audio context fingerprint
 */
function generateAudioFingerprint(): Promise<string | undefined> {
  return new Promise((resolve) => {
    try {
      // BUG-HIGH-005 FIX: Properly type webkitAudioContext instead of using 'any'
      const AudioContextConstructor = window.AudioContext || (window as Window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext
      if (!AudioContextConstructor) {
        resolve(undefined)
        return
      }
      const audioContext = new AudioContextConstructor()
      const oscillator = audioContext.createOscillator()
      const analyser = audioContext.createAnalyser()
      const gainNode = audioContext.createGain()
      const scriptProcessor = audioContext.createScriptProcessor(4096, 1, 1)
      
      oscillator.type = 'triangle'
      oscillator.frequency.value = 10000
      
      gainNode.gain.value = 0
      
      oscillator.connect(analyser)
      analyser.connect(scriptProcessor)
      scriptProcessor.connect(gainNode)
      gainNode.connect(audioContext.destination)
      
      scriptProcessor.onaudioprocess = (event) => {
        const buffer = event.inputBuffer.getChannelData(0)
        let sum = 0
        
        for (let i = 0; i < buffer.length; i++) {
          sum += Math.abs(buffer[i])
        }
        
        const audioFingerprint = sum.toString()
        
        // Cleanup
        oscillator.disconnect()
        scriptProcessor.disconnect()
        audioContext.close()
        
        resolve(audioFingerprint)
      }
      
      oscillator.start()
      
      // Timeout fallback
      setTimeout(() => {
        try {
          audioContext.close()
        } catch (closeError) {
          // BUG-FIX: Log audio context close error instead of silently ignoring
          // This can happen if context is already closed, which is expected in timeout scenario
          logger.debug('Audio context close during timeout fallback', {
            util: 'deviceFingerprinting',
            error: closeError instanceof Error ? closeError.message : 'Unknown error'
          })
        }
        resolve(undefined)
      }, 1000)
      
    } catch (error) {
      logger.warn('Audio fingerprinting failed', { util: 'deviceFingerprinting', error })
      resolve(undefined)
    }
  })
}

/**
 * BUG-MED-004 FIX: Detect available fonts with performance optimizations
 * - Reduced font list to essential fonts only
 * - Early exit optimization for faster detection
 * - Minimal canvas operations to reduce blocking
 */
function detectAvailableFonts(): string[] {
  // BUG-MED-004 FIX: Reduce test set to essential fonts to minimize blocking time
  // Original had 20 fonts, reduced to 10 most common fonts
  const baseFonts = ['monospace', 'sans-serif', 'serif']
  const testFonts = [
    'Arial', 'Helvetica', 'Times New Roman', 'Courier New', 'Verdana',
    'Georgia', 'Trebuchet MS', 'Impact', 'Tahoma', 'Lucida Console'
  ]

  const canvas = document.createElement('canvas')
  const ctx = canvas.getContext('2d')
  if (!ctx) return []

  const testText = 'mmmmmmmmmmlli'
  const baseSizes: { [key: string]: number } = {}

  // Get baseline measurements
  baseFonts.forEach(font => {
    ctx.font = `72px ${font}`
    baseSizes[font] = ctx.measureText(testText).width
  })

  const availableFonts: string[] = []

  // BUG-MED-004 FIX: Early exit optimization - stop checking once we find a match
  testFonts.forEach(font => {
    for (const baseFont of baseFonts) {
      ctx.font = `72px ${font}, ${baseFont}`
      const width = ctx.measureText(testText).width

      if (width !== baseSizes[baseFont]) {
        availableFonts.push(font)
        break // Early exit - found the font, no need to check other baseFonts
      }
    }
  })

  return availableFonts
}

// BUG-HIGH-005 FIX: Define extended Navigator interface for non-standard properties
interface ExtendedNavigator extends Navigator {
  userLanguage?: string
  browserLanguage?: string
  systemLanguage?: string
  deviceMemory?: number
}

/**
 * Collect comprehensive device fingerprint
 * BUG-HIGH-009 FIX: Respects user consent for advanced fingerprinting
 */
export async function collectDeviceFingerprint(): Promise<DeviceFingerprint> {
  const nav = navigator as ExtendedNavigator
  const hasConsent = hasDeviceFingerprintConsent()

  // Basic browser information (always collected for security/fraud prevention)
  // These are considered "strictly necessary" under GDPR
  const fingerprint: DeviceFingerprint = {
    userAgent: navigator.userAgent,
    timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    screenResolution: `${screen.width}x${screen.height}x${screen.colorDepth}`,
    acceptLanguage: navigator.language || nav.userLanguage || nav.browserLanguage || nav.systemLanguage || 'en-US',
    platform: navigator.platform,
    colorDepth: screen.colorDepth,
    deviceMemory: nav.deviceMemory,
    hardwareConcurrency: navigator.hardwareConcurrency || 0,
    touchSupport: 'ontouchstart' in window || navigator.maxTouchPoints > 0,
    cookieEnabled: navigator.cookieEnabled,
    doNotTrack: navigator.doNotTrack,
    // BUG-HIGH-009 FIX: Only collect plugins/fonts if user has consented
    installedPlugins: hasConsent ? Array.from(navigator.plugins).map(plugin => plugin.name) : [],
    availableFonts: hasConsent ? detectAvailableFonts() : []
  }

  // BUG-HIGH-009 FIX: Advanced fingerprinting only with explicit consent
  // Canvas, WebGL, and Audio fingerprinting require user consent
  if (hasConsent) {
    try {
      const [canvasFingerprint, webGLFingerprint, audioFingerprint] = await Promise.all([
        Promise.resolve(generateCanvasFingerprint()),
        Promise.resolve(generateWebGLFingerprint()),
        generateAudioFingerprint()
      ])

      fingerprint.canvasFingerprint = canvasFingerprint
      fingerprint.webGLFingerprint = webGLFingerprint
      fingerprint.audioFingerprint = audioFingerprint
    } catch (error) {
      logger.warn('Advanced fingerprinting partially failed', { util: 'deviceFingerprinting', error })
    }
  } else {
    logger.info('Advanced fingerprinting skipped - user has not consented', { util: 'deviceFingerprinting' })
  }

  return fingerprint
}

/**
 * Generate a hash from device fingerprint for consistent identification
 */
export async function generateDeviceHash(fingerprint?: DeviceFingerprint): Promise<string> {
  if (!fingerprint) {
    fingerprint = await collectDeviceFingerprint()
  }
  
  // Create a deterministic string from fingerprint data
  const fingerprintString = [
    fingerprint.userAgent,
    fingerprint.timezone,
    fingerprint.screenResolution,
    fingerprint.platform,
    fingerprint.colorDepth,
    fingerprint.hardwareConcurrency,
    fingerprint.touchSupport ? '1' : '0',
    fingerprint.cookieEnabled ? '1' : '0',
    fingerprint.canvasFingerprint || '',
    fingerprint.webGLFingerprint || '',
    fingerprint.audioFingerprint || '',
    fingerprint.availableFonts.join(',')
  ].join('|')
  
  // Generate hash using SubtleCrypto if available
  if (typeof crypto !== 'undefined' && crypto.subtle) {
    try {
      const encoder = new TextEncoder()
      const data = encoder.encode(fingerprintString)
      const hashBuffer = await crypto.subtle.digest('SHA-256', data)
      const hashArray = Array.from(new Uint8Array(hashBuffer))
      return hashArray.map(b => b.toString(16).padStart(2, '0')).join('')
    } catch (error) {
      logger.warn('Crypto API hashing failed, using fallback', { util: 'deviceFingerprinting', error })
    }
  }
  
  // Fallback: Simple hash function
  let hash = 0
  for (let i = 0; i < fingerprintString.length; i++) {
    const char = fingerprintString.charCodeAt(i)
    hash = ((hash << 5) - hash) + char
    hash = hash & hash // Convert to 32-bit integer
  }
  
  return Math.abs(hash).toString(16)
}