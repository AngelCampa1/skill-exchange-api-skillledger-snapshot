/**
 * URL Validation Utilities
 * VULN-009 & VULN-010 FIX: Validate URLs to prevent XSS attacks via javascript: or data: protocols
 */

/**
 * List of safe URL protocols
 * Only http and https are allowed to prevent XSS attacks
 */
const SAFE_PROTOCOLS = ['http:', 'https:']

/**
 * Validates that a URL uses a safe protocol (http or https only)
 * Prevents XSS attacks via javascript:, data:, vbscript:, file: protocols
 *
 * @param url - The URL to validate
 * @returns True if the URL is safe, false otherwise
 *
 * @example
 * isSafeUrl('https://example.com') // true
 * isSafeUrl('http://example.com') // true
 * isSafeUrl('javascript:alert(1)') // false
 * isSafeUrl('data:text/html,<script>alert(1)</script>') // false
 */
export function isSafeUrl(url: string | null | undefined): boolean {
  if (!url || typeof url !== 'string') {
    return false
  }

  // Trim whitespace
  const trimmedUrl = url.trim()

  if (trimmedUrl.length === 0) {
    return false
  }

  try {
    // Parse URL to extract protocol
    const urlObject = new URL(trimmedUrl)

    // Check if protocol is in safe list
    return SAFE_PROTOCOLS.includes(urlObject.protocol.toLowerCase())
  } catch (error) {
    // Invalid URL format
    return false
  }
}

/**
 * Sanitizes a URL by validating it and returning a safe version
 * If the URL is unsafe, returns undefined
 *
 * @param url - The URL to sanitize
 * @returns The original URL if safe, undefined if unsafe
 *
 * @example
 * sanitizeUrl('https://example.com') // 'https://example.com'
 * sanitizeUrl('javascript:alert(1)') // undefined
 */
export function sanitizeUrl(url: string | null | undefined): string | undefined {
  if (!isSafeUrl(url)) {
    return undefined
  }
  // BUG-HIGH-007 FIX: url is guaranteed to be a non-empty trimmed string if isSafeUrl passed
  // TypeScript needs explicit narrowing since isSafeUrl doesn't act as type guard
  return url ? url.trim() : undefined
}

/**
 * Gets a safe URL for use in href attributes
 * If the URL is unsafe, returns '#' to prevent navigation
 *
 * @param url - The URL to make safe
 * @returns The original URL if safe, '#' if unsafe
 *
 * @example
 * getSafeHref('https://example.com') // 'https://example.com'
 * getSafeHref('javascript:alert(1)') // '#'
 */
export function getSafeHref(url: string | null | undefined): string {
  const safe = sanitizeUrl(url)
  return safe ?? '#'
}
