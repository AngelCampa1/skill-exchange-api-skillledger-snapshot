/**
 * Integration Tests for urlValidation.ts
 *
 * Tests URL validation utilities to prevent XSS attacks via unsafe protocols.
 * VULN-009 & VULN-010 coverage
 */

import { isSafeUrl, sanitizeUrl, getSafeHref } from '../urlValidation';

describe('urlValidation', () => {
  describe('isSafeUrl()', () => {
    describe('valid URLs', () => {
      test('returns true for https URLs', () => {
        expect(isSafeUrl('https://example.com')).toBe(true);
        expect(isSafeUrl('https://example.com/path')).toBe(true);
        expect(isSafeUrl('https://example.com/path?query=value')).toBe(true);
        expect(isSafeUrl('https://example.com:8080/path')).toBe(true);
      });

      test('returns true for http URLs', () => {
        expect(isSafeUrl('http://example.com')).toBe(true);
        expect(isSafeUrl('http://localhost:3000')).toBe(true);
        expect(isSafeUrl('http://127.0.0.1')).toBe(true);
      });

      test('handles URLs with special characters', () => {
        expect(isSafeUrl('https://example.com/path%20with%20spaces')).toBe(true);
        expect(isSafeUrl('https://example.com/?q=test&foo=bar')).toBe(true);
        expect(isSafeUrl('https://example.com/#anchor')).toBe(true);
      });

      test('is case-insensitive for protocol', () => {
        expect(isSafeUrl('HTTPS://example.com')).toBe(true);
        expect(isSafeUrl('HTTP://example.com')).toBe(true);
        expect(isSafeUrl('HttpS://example.com')).toBe(true);
      });

      test('handles URLs with auth info', () => {
        expect(isSafeUrl('https://user:pass@example.com')).toBe(true);
        expect(isSafeUrl('http://user@example.com')).toBe(true);
      });
    });

    describe('unsafe URLs (XSS prevention)', () => {
      test('returns false for javascript: protocol', () => {
        expect(isSafeUrl('javascript:alert(1)')).toBe(false);
        expect(isSafeUrl('JAVASCRIPT:alert(1)')).toBe(false);
        expect(isSafeUrl('JavaScript:alert("xss")')).toBe(false);
        expect(isSafeUrl('javascript:void(0)')).toBe(false);
      });

      test('returns false for data: protocol', () => {
        expect(isSafeUrl('data:text/html,<script>alert(1)</script>')).toBe(false);
        expect(isSafeUrl('data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==')).toBe(false);
        expect(isSafeUrl('DATA:text/html,test')).toBe(false);
      });

      test('returns false for vbscript: protocol', () => {
        expect(isSafeUrl('vbscript:msgbox("xss")')).toBe(false);
        expect(isSafeUrl('VBSCRIPT:alert')).toBe(false);
      });

      test('returns false for file: protocol', () => {
        expect(isSafeUrl('file:///etc/passwd')).toBe(false);
        expect(isSafeUrl('file://C:/Windows/system.ini')).toBe(false);
      });

      test('returns false for ftp: protocol', () => {
        expect(isSafeUrl('ftp://example.com/file.txt')).toBe(false);
      });

      test('returns false for blob: protocol', () => {
        expect(isSafeUrl('blob:http://example.com/uuid')).toBe(false);
      });
    });

    describe('invalid/edge cases', () => {
      test('returns false for null', () => {
        expect(isSafeUrl(null)).toBe(false);
      });

      test('returns false for undefined', () => {
        expect(isSafeUrl(undefined)).toBe(false);
      });

      test('returns false for empty string', () => {
        expect(isSafeUrl('')).toBe(false);
      });

      test('returns false for whitespace-only string', () => {
        expect(isSafeUrl('   ')).toBe(false);
        expect(isSafeUrl('\t\n')).toBe(false);
      });

      test('returns false for non-string values', () => {
        // TypeScript would prevent this, but test runtime behavior
        expect(isSafeUrl(123 as any)).toBe(false);
        expect(isSafeUrl({} as any)).toBe(false);
        expect(isSafeUrl([] as any)).toBe(false);
      });

      test('returns false for invalid URL format', () => {
        expect(isSafeUrl('not-a-url')).toBe(false);
        expect(isSafeUrl('://missing-protocol')).toBe(false);
        expect(isSafeUrl('htp://typo.com')).toBe(false);
      });

      test('handles URLs with leading/trailing whitespace', () => {
        expect(isSafeUrl('  https://example.com  ')).toBe(true);
        expect(isSafeUrl('\nhttps://example.com\n')).toBe(true);
      });

      test('returns false for relative URLs (no protocol)', () => {
        expect(isSafeUrl('/path/to/page')).toBe(false);
        expect(isSafeUrl('./relative/path')).toBe(false);
        expect(isSafeUrl('../parent/path')).toBe(false);
      });
    });
  });

  describe('sanitizeUrl()', () => {
    test('returns original URL for safe URLs', () => {
      expect(sanitizeUrl('https://example.com')).toBe('https://example.com');
      expect(sanitizeUrl('http://localhost:3000')).toBe('http://localhost:3000');
    });

    test('returns trimmed URL for safe URLs with whitespace', () => {
      expect(sanitizeUrl('  https://example.com  ')).toBe('https://example.com');
      expect(sanitizeUrl('\nhttps://example.com\t')).toBe('https://example.com');
    });

    test('returns undefined for unsafe URLs', () => {
      expect(sanitizeUrl('javascript:alert(1)')).toBeUndefined();
      expect(sanitizeUrl('data:text/html,<script>alert(1)</script>')).toBeUndefined();
    });

    test('returns undefined for null/undefined', () => {
      expect(sanitizeUrl(null)).toBeUndefined();
      expect(sanitizeUrl(undefined)).toBeUndefined();
    });

    test('returns undefined for empty string', () => {
      expect(sanitizeUrl('')).toBeUndefined();
      expect(sanitizeUrl('   ')).toBeUndefined();
    });

    test('returns undefined for invalid URLs', () => {
      expect(sanitizeUrl('not-a-url')).toBeUndefined();
      expect(sanitizeUrl('://bad-protocol')).toBeUndefined();
    });
  });

  describe('getSafeHref()', () => {
    test('returns original URL for safe URLs', () => {
      expect(getSafeHref('https://example.com')).toBe('https://example.com');
      expect(getSafeHref('http://localhost:3000/path')).toBe('http://localhost:3000/path');
    });

    test('returns "#" for unsafe URLs', () => {
      expect(getSafeHref('javascript:alert(1)')).toBe('#');
      expect(getSafeHref('data:text/html,xss')).toBe('#');
      expect(getSafeHref('vbscript:xss')).toBe('#');
    });

    test('returns "#" for null/undefined', () => {
      expect(getSafeHref(null)).toBe('#');
      expect(getSafeHref(undefined)).toBe('#');
    });

    test('returns "#" for empty string', () => {
      expect(getSafeHref('')).toBe('#');
      expect(getSafeHref('   ')).toBe('#');
    });

    test('returns "#" for invalid URLs', () => {
      expect(getSafeHref('not-a-url')).toBe('#');
      expect(getSafeHref('/relative/path')).toBe('#');
    });

    test('returns trimmed safe URL', () => {
      expect(getSafeHref('  https://example.com  ')).toBe('https://example.com');
    });

    test('is safe for use in href attributes', () => {
      // This simulates real-world usage
      const userInput = 'javascript:alert(document.cookie)';
      const safeHref = getSafeHref(userInput);

      // Safe href should never execute JavaScript
      expect(safeHref).toBe('#');
      expect(safeHref.includes('javascript:')).toBe(false);
    });
  });

  describe('security scenarios', () => {
    test('prevents common XSS attack vectors', () => {
      const attacks = [
        'javascript:alert(1)',
        'javascript:alert(document.cookie)',
        'jAvAsCrIpT:alert(1)',
        'data:text/html,<script>alert(1)</script>',
        'data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==',
        'vbscript:msgbox(1)',
        '&#106;avascript:alert(1)', // HTML entity encoding
        'java\tscript:alert(1)', // Tab injection (would need trimming)
        'java\nscript:alert(1)', // Newline injection
      ];

      attacks.forEach(attack => {
        expect(isSafeUrl(attack)).toBe(false);
        expect(sanitizeUrl(attack)).toBeUndefined();
        expect(getSafeHref(attack)).toBe('#');
      });
    });

    test('allows legitimate URLs from various domains', () => {
      const safeUrls = [
        'https://google.com',
        'https://github.com/user/repo',
        'https://skillledger.example.com/dashboard',
        'http://localhost:3000',
        'https://api.stripe.com/v1/charges',
        'https://cdn.example.com/image.png',
        'https://example.com/?redirect=https://other.com',
      ];

      safeUrls.forEach(url => {
        expect(isSafeUrl(url)).toBe(true);
        expect(sanitizeUrl(url)).toBe(url);
        expect(getSafeHref(url)).toBe(url);
      });
    });
  });
});
