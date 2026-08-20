/**
 * Tests for urlValidation.ts utilities
 *
 * This file validates URL security functions to prevent XSS attacks
 */

import { isSafeUrl, sanitizeUrl, getSafeHref } from '@/lib/urlValidation'

describe('urlValidation utilities', () => {
  describe('isSafeUrl', () => {
    describe('Safe URLs', () => {
      it('should allow https URLs', () => {
        expect(isSafeUrl('https://example.com')).toBe(true)
        expect(isSafeUrl('https://example.com/path')).toBe(true)
        expect(isSafeUrl('https://example.com/path?query=value')).toBe(true)
        expect(isSafeUrl('https://example.com/path#hash')).toBe(true)
      })

      it('should allow http URLs', () => {
        expect(isSafeUrl('http://example.com')).toBe(true)
        expect(isSafeUrl('http://example.com/path')).toBe(true)
        expect(isSafeUrl('http://localhost:3000')).toBe(true)
        expect(isSafeUrl('http://192.168.1.1')).toBe(true)
      })

      it('should handle URLs with different casing', () => {
        expect(isSafeUrl('HTTPS://EXAMPLE.COM')).toBe(true)
        expect(isSafeUrl('HTTP://EXAMPLE.COM')).toBe(true)
        expect(isSafeUrl('HtTpS://example.com')).toBe(true)
      })

      it('should trim whitespace from URLs', () => {
        expect(isSafeUrl('  https://example.com  ')).toBe(true)
        expect(isSafeUrl('\thttps://example.com\t')).toBe(true)
        expect(isSafeUrl('\nhttps://example.com\n')).toBe(true)
      })

      it('should handle complex valid URLs', () => {
        expect(isSafeUrl('https://user:pass@example.com:8080/path?query=value#hash')).toBe(true)
        expect(isSafeUrl('https://subdomain.example.com')).toBe(true)
        expect(isSafeUrl('https://example.com/path/to/resource.html')).toBe(true)
      })
    })

    describe('Unsafe URLs - XSS Prevention', () => {
      it('should block javascript: protocol', () => {
        expect(isSafeUrl('javascript:alert(1)')).toBe(false)
        expect(isSafeUrl('javascript:void(0)')).toBe(false)
        expect(isSafeUrl('JavaScript:alert(document.cookie)')).toBe(false)
        expect(isSafeUrl('JAVASCRIPT:alert(1)')).toBe(false)
      })

      it('should block data: protocol', () => {
        expect(isSafeUrl('data:text/html,<script>alert(1)</script>')).toBe(false)
        expect(isSafeUrl('data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==')).toBe(false)
        expect(isSafeUrl('DATA:text/html,<script>alert(1)</script>')).toBe(false)
      })

      it('should block vbscript: protocol', () => {
        expect(isSafeUrl('vbscript:msgbox(1)')).toBe(false)
        expect(isSafeUrl('VBScript:msgbox(1)')).toBe(false)
      })

      it('should block file: protocol', () => {
        expect(isSafeUrl('file:///etc/passwd')).toBe(false)
        expect(isSafeUrl('file://C:/Windows/System32')).toBe(false)
        expect(isSafeUrl('FILE:///etc/passwd')).toBe(false)
      })

      it('should block other dangerous protocols', () => {
        expect(isSafeUrl('ftp://example.com')).toBe(false)
        expect(isSafeUrl('ws://example.com')).toBe(false)
        expect(isSafeUrl('wss://example.com')).toBe(false)
        expect(isSafeUrl('tel:+1234567890')).toBe(false)
        expect(isSafeUrl('mailto:test@example.com')).toBe(false)
      })
    })

    describe('Invalid inputs', () => {
      it('should reject null', () => {
        expect(isSafeUrl(null)).toBe(false)
      })

      it('should reject undefined', () => {
        expect(isSafeUrl(undefined)).toBe(false)
      })

      it('should reject empty string', () => {
        expect(isSafeUrl('')).toBe(false)
      })

      it('should reject whitespace-only string', () => {
        expect(isSafeUrl('   ')).toBe(false)
        expect(isSafeUrl('\t\t\t')).toBe(false)
        expect(isSafeUrl('\n\n\n')).toBe(false)
      })

      it('should reject non-string values', () => {
        expect(isSafeUrl(123 as any)).toBe(false)
        expect(isSafeUrl({} as any)).toBe(false)
        expect(isSafeUrl([] as any)).toBe(false)
        expect(isSafeUrl(true as any)).toBe(false)
      })

      it('should reject malformed URLs', () => {
        expect(isSafeUrl('not-a-url')).toBe(false)
        expect(isSafeUrl('://invalid')).toBe(false)
        expect(isSafeUrl('http://')).toBe(false)
        expect(isSafeUrl('http:///')).toBe(false)
      })

      it('should handle URLs without protocol', () => {
        expect(isSafeUrl('example.com')).toBe(false)
        expect(isSafeUrl('//example.com')).toBe(false)
        expect(isSafeUrl('www.example.com')).toBe(false)
      })
    })

    describe('Edge cases', () => {
      it('should handle URLs with encoded characters', () => {
        expect(isSafeUrl('https://example.com/%20path')).toBe(true)
        expect(isSafeUrl('https://example.com/path%3Fquery')).toBe(true)
      })

      it('should handle IPv6 URLs', () => {
        expect(isSafeUrl('http://[2001:db8::1]')).toBe(true)
        expect(isSafeUrl('https://[::1]')).toBe(true)
      })

      it('should handle URLs with ports', () => {
        expect(isSafeUrl('https://example.com:443')).toBe(true)
        expect(isSafeUrl('http://example.com:80')).toBe(true)
        expect(isSafeUrl('https://example.com:8443')).toBe(true)
      })

      it('should handle URLs with authentication', () => {
        expect(isSafeUrl('https://user@example.com')).toBe(true)
        expect(isSafeUrl('https://user:password@example.com')).toBe(true)
      })
    })
  })

  describe('sanitizeUrl', () => {
    describe('Safe URLs', () => {
      it('should return safe https URLs unchanged', () => {
        expect(sanitizeUrl('https://example.com')).toBe('https://example.com')
        expect(sanitizeUrl('https://example.com/path')).toBe('https://example.com/path')
      })

      it('should return safe http URLs unchanged', () => {
        expect(sanitizeUrl('http://example.com')).toBe('http://example.com')
        expect(sanitizeUrl('http://localhost:3000')).toBe('http://localhost:3000')
      })

      it('should trim whitespace from safe URLs', () => {
        expect(sanitizeUrl('  https://example.com  ')).toBe('https://example.com')
        expect(sanitizeUrl('\thttps://example.com\t')).toBe('https://example.com')
      })

      it('should preserve URL structure', () => {
        const complexUrl = 'https://user:pass@example.com:8080/path?query=value#hash'
        expect(sanitizeUrl(complexUrl)).toBe(complexUrl)
      })
    })

    describe('Unsafe URLs', () => {
      it('should return undefined for javascript: protocol', () => {
        expect(sanitizeUrl('javascript:alert(1)')).toBeUndefined()
        expect(sanitizeUrl('JavaScript:void(0)')).toBeUndefined()
      })

      it('should return undefined for data: protocol', () => {
        expect(sanitizeUrl('data:text/html,<script>alert(1)</script>')).toBeUndefined()
      })

      it('should return undefined for vbscript: protocol', () => {
        expect(sanitizeUrl('vbscript:msgbox(1)')).toBeUndefined()
      })

      it('should return undefined for file: protocol', () => {
        expect(sanitizeUrl('file:///etc/passwd')).toBeUndefined()
      })

      it('should return undefined for other protocols', () => {
        expect(sanitizeUrl('ftp://example.com')).toBeUndefined()
        expect(sanitizeUrl('mailto:test@example.com')).toBeUndefined()
      })
    })

    describe('Invalid inputs', () => {
      it('should return undefined for null', () => {
        expect(sanitizeUrl(null)).toBeUndefined()
      })

      it('should return undefined for undefined', () => {
        expect(sanitizeUrl(undefined)).toBeUndefined()
      })

      it('should return undefined for empty string', () => {
        expect(sanitizeUrl('')).toBeUndefined()
      })

      it('should return undefined for whitespace-only string', () => {
        expect(sanitizeUrl('   ')).toBeUndefined()
      })

      it('should return undefined for malformed URLs', () => {
        expect(sanitizeUrl('not-a-url')).toBeUndefined()
        expect(sanitizeUrl('://invalid')).toBeUndefined()
      })
    })
  })

  describe('getSafeHref', () => {
    describe('Safe URLs', () => {
      it('should return safe https URLs unchanged', () => {
        expect(getSafeHref('https://example.com')).toBe('https://example.com')
        expect(getSafeHref('https://example.com/path')).toBe('https://example.com/path')
      })

      it('should return safe http URLs unchanged', () => {
        expect(getSafeHref('http://example.com')).toBe('http://example.com')
        expect(getSafeHref('http://localhost:3000')).toBe('http://localhost:3000')
      })

      it('should trim whitespace from safe URLs', () => {
        expect(getSafeHref('  https://example.com  ')).toBe('https://example.com')
      })
    })

    describe('Unsafe URLs', () => {
      it('should return # for javascript: protocol', () => {
        expect(getSafeHref('javascript:alert(1)')).toBe('#')
        expect(getSafeHref('JavaScript:void(0)')).toBe('#')
      })

      it('should return # for data: protocol', () => {
        expect(getSafeHref('data:text/html,<script>alert(1)</script>')).toBe('#')
      })

      it('should return # for vbscript: protocol', () => {
        expect(getSafeHref('vbscript:msgbox(1)')).toBe('#')
      })

      it('should return # for file: protocol', () => {
        expect(getSafeHref('file:///etc/passwd')).toBe('#')
      })

      it('should return # for other protocols', () => {
        expect(getSafeHref('ftp://example.com')).toBe('#')
        expect(getSafeHref('mailto:test@example.com')).toBe('#')
      })
    })

    describe('Invalid inputs', () => {
      it('should return # for null', () => {
        expect(getSafeHref(null)).toBe('#')
      })

      it('should return # for undefined', () => {
        expect(getSafeHref(undefined)).toBe('#')
      })

      it('should return # for empty string', () => {
        expect(getSafeHref('')).toBe('#')
      })

      it('should return # for whitespace-only string', () => {
        expect(getSafeHref('   ')).toBe('#')
      })

      it('should return # for malformed URLs', () => {
        expect(getSafeHref('not-a-url')).toBe('#')
        expect(getSafeHref('://invalid')).toBe('#')
      })
    })

    describe('Use case scenarios', () => {
      it('should be safe for use in <a href="...">', () => {
        // Valid URL - should pass through
        const validUrl = 'https://example.com'
        expect(getSafeHref(validUrl)).toBe(validUrl)

        // XSS attempt - should be blocked
        const xssUrl = 'javascript:alert(document.cookie)'
        expect(getSafeHref(xssUrl)).toBe('#')
      })

      it('should prevent navigation for unsafe URLs', () => {
        // User-provided URL from untrusted source
        const userInput = 'javascript:void(document.body.innerHTML="")'
        const safeHref = getSafeHref(userInput)

        // Should return # to prevent navigation
        expect(safeHref).toBe('#')
        expect(safeHref).not.toContain('javascript')
      })

      it('should handle external links safely', () => {
        const externalLink = 'https://external-site.com/page'
        expect(getSafeHref(externalLink)).toBe(externalLink)
      })

      it('should handle internal links safely', () => {
        const internalLink = 'https://skillledger.app/dashboard'
        expect(getSafeHref(internalLink)).toBe(internalLink)
      })
    })
  })

  describe('Security test vectors', () => {
    const xssVectors = [
      'javascript:alert(1)',
      'javascript:alert(document.cookie)',
      'javascript:void(0)',
      'data:text/html,<script>alert(1)</script>',
      'data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==',
      'vbscript:msgbox(1)',
      'file:///etc/passwd',
      'javascript:eval("alert(1)")',
      'javascript://example.com/%0Aalert(1)',
      'data:text/html,<img src=x onerror=alert(1)>',
    ]

    xssVectors.forEach((vector) => {
      it(`should block XSS vector: ${vector}`, () => {
        expect(isSafeUrl(vector)).toBe(false)
        expect(sanitizeUrl(vector)).toBeUndefined()
        expect(getSafeHref(vector)).toBe('#')
      })
    })
  })
})
