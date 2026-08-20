import { logger } from './logger';
/**
 * Geolocation and geographic restrictions utility
 * Handles IP-based location detection and country restrictions
 */

export interface GeolocationInfo {
  ip: string
  country: string
  countryCode: string
  region: string
  city: string
  timezone: string
  isVPN: boolean
  isProxy: boolean
  isTor: boolean
  riskScore: number
  isRestricted: boolean
  restrictionReason?: string
}

export interface GeolocationResponse {
  success: boolean
  data?: GeolocationInfo
  error?: string
  fallbackUsed?: boolean
}

/**
 * High-risk countries and regions for compliance
 */
const RESTRICTED_COUNTRIES = new Set([
  // OFAC Sanctions
  'CU', 'IR', 'KP', 'SY', 'RU', 'BY',
  // High-risk financial jurisdictions
  'AF', 'MM', 'SO', 'YE', 'LY', 'SD',
  // Additional restrictions (configurable)
])

/**
 * Countries with enhanced verification requirements
 */
const ENHANCED_VERIFICATION_COUNTRIES = new Set([
  'CN', 'PK', 'BD', 'NG', 'ID', 'IN', 'BR', 'TR', 'EG', 'MA'
])

/**
 * Fetch geolocation from primary API
 */
async function fetchGeolocationPrimary(): Promise<GeolocationInfo | null> {
  try {
    const response = await fetch('/api/user/geolocation', {
      method: 'GET',
      headers: {
        'Accept': 'application/json',
      }
    })
    
    if (!response.ok) {
      throw new Error(`Geolocation API failed: ${response.status}`)
    }
    
    const data = await response.json()
    return data.geolocation
  } catch (error) {
    logger.warn('Primary geolocation API failed', { util: 'geolocation', error })
    return null
  }
}

/**
 * Fetch geolocation from fallback service
 */
async function fetchGeolocationFallback(): Promise<GeolocationInfo | null> {
  try {
    // Use a simple IP API as fallback
    const response = await fetch('https://ip-api.com/json/', {
      method: 'GET',
      headers: {
        'Accept': 'application/json',
      }
    })
    
    if (!response.ok) {
      throw new Error(`Fallback API failed: ${response.status}`)
    }
    
    const data = await response.json()
    
    // Transform to our standard format
    const geolocation: GeolocationInfo = {
      ip: data.query || 'unknown',
      country: data.country || 'Unknown',
      countryCode: data.countryCode || 'XX',
      region: data.regionName || 'Unknown',
      city: data.city || 'Unknown',
      timezone: data.timezone || 'UTC',
      isVPN: false, // This API doesn't detect VPN
      isProxy: false,
      isTor: false,
      riskScore: RESTRICTED_COUNTRIES.has(data.countryCode) ? 100 : 
                ENHANCED_VERIFICATION_COUNTRIES.has(data.countryCode) ? 50 : 10,
      isRestricted: RESTRICTED_COUNTRIES.has(data.countryCode),
      restrictionReason: RESTRICTED_COUNTRIES.has(data.countryCode) 
        ? 'Country is subject to regulatory restrictions' : undefined
    }
    
    return geolocation
  } catch (error) {
    logger.warn('Fallback geolocation API failed', { util: 'geolocation', error })
    return null
  }
}

/**
 * Get user's geolocation with fallback strategy
 */
export async function getUserGeolocation(): Promise<GeolocationResponse> {
  try {
    // Try primary API first
    let geolocation = await fetchGeolocationPrimary()
    let fallbackUsed = false
    
    if (!geolocation) {
      // Fallback to secondary service
      geolocation = await fetchGeolocationFallback()
      fallbackUsed = true
    }
    
    if (!geolocation) {
      return {
        success: false,
        error: 'Unable to determine location'
      }
    }
    
    return {
      success: true,
      data: geolocation,
      fallbackUsed
    }
  } catch (error) {
    logger.error('Geolocation detection failed', error, { util: 'geolocation' })
    return {
      success: false,
      error: 'Geolocation service temporarily unavailable'
    }
  }
}

/**
 * Check if user's location is restricted
 */
export function isLocationRestricted(countryCode: string): {
  isRestricted: boolean
  reason?: string
  requiresEnhancedVerification: boolean
} {
  const isRestricted = RESTRICTED_COUNTRIES.has(countryCode.toUpperCase())
  const requiresEnhancedVerification = ENHANCED_VERIFICATION_COUNTRIES.has(countryCode.toUpperCase())
  
  let reason: string | undefined
  
  if (isRestricted) {
    reason = 'Registration is not currently available in your country due to regulatory restrictions.'
  }
  
  return {
    isRestricted,
    reason,
    requiresEnhancedVerification
  }
}

/**
 * Generate user-friendly location restriction message
 */
export function getLocationRestrictionMessage(geolocation: GeolocationInfo): string | null {
  if (!geolocation.isRestricted) {
    return null
  }
  
  const messages = [
    `We're unable to provide services to users in ${geolocation.country} at this time.`,
    'This restriction is due to regulatory and compliance requirements.',
    'We apologize for any inconvenience and are working to expand our service availability.'
  ]
  
  if (geolocation.isVPN || geolocation.isProxy || geolocation.isTor) {
    messages.unshift(
      'We detected that you may be using a VPN, proxy, or Tor network.',
      'Please disable any privacy tools and try again with your actual location.'
    )
  }
  
  return messages.join(' ')
}

/**
 * Check for VPN/Proxy usage warning
 */
export function getVPNWarningMessage(geolocation: GeolocationInfo): string | null {
  if (geolocation.isVPN || geolocation.isProxy || geolocation.isTor) {
    return 'We detected you may be using a VPN or proxy. For security reasons, please connect from your actual location to continue registration.'
  }
  
  return null
}

/**
 * Get enhanced verification requirements message
 */
export function getEnhancedVerificationMessage(countryCode: string): string | null {
  if (ENHANCED_VERIFICATION_COUNTRIES.has(countryCode.toUpperCase())) {
    return 'Due to your location, additional verification steps may be required to complete your registration. This helps us maintain platform security and compliance.'
  }
  
  return null
}