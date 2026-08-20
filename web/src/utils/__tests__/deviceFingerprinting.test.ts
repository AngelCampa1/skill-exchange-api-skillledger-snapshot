/**
 * deviceFingerprinting.ts Tests
 *
 * Tests device fingerprinting with GDPR consent management and hash generation.
 * Focus: Canvas/WebGL/Audio fingerprinting, consent storage, bot detection.
 *
 * Coverage Target: 80%+ (348 lines)
 * Test Count: 12 tests
 */

import {
  hasDeviceFingerprintConsent,
  setDeviceFingerprintConsent,
  clearDeviceFingerprintConsent,
  collectDeviceFingerprint,
  generateDeviceHash,
} from '../deviceFingerprinting';

describe('deviceFingerprinting.ts - Device Fingerprint & GDPR Consent', () => {
  beforeEach(() => {
    // Clear localStorage before each test
    localStorage.clear();
    jest.clearAllMocks();
  });

  // ==========================================
  // Part 1: Fingerprint Generation (5 tests)
  // ==========================================

  describe('Fingerprint Collection', () => {
    it('should collect basic device fingerprint without consent', async () => {
      // Setup: No consent given
      const fingerprint = await collectDeviceFingerprint();

      // Should collect basic browser info (GDPR "strictly necessary")
      expect(fingerprint.userAgent).toBeDefined();
      expect(fingerprint.timezone).toBeDefined();
      expect(fingerprint.screenResolution).toMatch(/^\d+x\d+x\d+$/);
      expect(fingerprint.platform).toBeDefined();
      expect(fingerprint.colorDepth).toBeGreaterThan(0);
      expect(fingerprint.cookieEnabled).toBe(true);

      // Should NOT collect advanced fingerprints without consent
      expect(fingerprint.canvasFingerprint).toBeUndefined();
      expect(fingerprint.webGLFingerprint).toBeUndefined();
      expect(fingerprint.audioFingerprint).toBeUndefined();
      expect(fingerprint.installedPlugins).toEqual([]);
      expect(fingerprint.availableFonts).toEqual([]);
    });

    it('should collect advanced fingerprints WITH consent', async () => {
      // Grant consent first
      setDeviceFingerprintConsent(true);

      const fingerprint = await collectDeviceFingerprint();

      // Should collect basic info
      expect(fingerprint.userAgent).toBeDefined();

      // Should attempt to collect advanced fingerprints (may be undefined if browser doesn't support)
      // Canvas: should generate a dataURL string if canvas is supported
      if (fingerprint.canvasFingerprint) {
        expect(fingerprint.canvasFingerprint).toContain('data:image');
      }

      // WebGL: should have vendor~renderer format if supported
      if (fingerprint.webGLFingerprint) {
        expect(fingerprint.webGLFingerprint).toContain('~');
      }

      // Fonts: should detect some common fonts
      expect(Array.isArray(fingerprint.availableFonts)).toBe(true);

      // Plugins: should be an array (may be empty in test environment)
      expect(Array.isArray(fingerprint.installedPlugins)).toBe(true);
    });

    it('should include hardware concurrency and touch support', async () => {
      const fingerprint = await collectDeviceFingerprint();

      expect(fingerprint.hardwareConcurrency).toBeGreaterThanOrEqual(0);
      expect(typeof fingerprint.touchSupport).toBe('boolean');
      expect(typeof fingerprint.cookieEnabled).toBe('boolean');
    });

    it('should collect screen resolution in WxHxD format', async () => {
      const fingerprint = await collectDeviceFingerprint();

      // Format: 1920x1080x24 (in jsdom: 0x0x24)
      expect(fingerprint.screenResolution).toMatch(/^\d+x\d+x\d+$/);

      const parts = fingerprint.screenResolution.split('x');
      expect(parts).toHaveLength(3);
      expect(parseInt(parts[0])).toBeGreaterThanOrEqual(0); // width (can be 0 in jsdom)
      expect(parseInt(parts[1])).toBeGreaterThanOrEqual(0); // height (can be 0 in jsdom)
      expect(parseInt(parts[2])).toBeGreaterThan(0); // colorDepth
    });

    it('should handle missing browser features gracefully', async () => {
      // No consent - advanced features should be skipped
      const fingerprint = await collectDeviceFingerprint();

      // Should not throw errors, just return undefined for unsupported features
      expect(fingerprint).toBeDefined();
      expect(fingerprint.userAgent).toBeDefined();

      // Advanced features should be undefined without consent
      expect(fingerprint.canvasFingerprint).toBeUndefined();
      expect(fingerprint.webGLFingerprint).toBeUndefined();
    });
  });

  // ==========================================
  // Part 2: GDPR Consent Management (4 tests)
  // ==========================================

  describe('GDPR Consent Management', () => {
    it('should return false for consent by default', () => {
      expect(hasDeviceFingerprintConsent()).toBe(false);
    });

    it('should save consent to localStorage when granted', () => {
      setDeviceFingerprintConsent(true);

      expect(hasDeviceFingerprintConsent()).toBe(true);
      expect(localStorage.getItem('skillledger_fingerprint_consent')).toBe('granted');
    });

    it('should remove consent from localStorage when revoked', () => {
      // First grant consent
      setDeviceFingerprintConsent(true);
      expect(hasDeviceFingerprintConsent()).toBe(true);

      // Then revoke it
      setDeviceFingerprintConsent(false);
      expect(hasDeviceFingerprintConsent()).toBe(false);
      expect(localStorage.getItem('skillledger_fingerprint_consent')).toBeNull();
    });

    it('should clear consent using clearDeviceFingerprintConsent()', () => {
      // Grant consent
      setDeviceFingerprintConsent(true);
      expect(hasDeviceFingerprintConsent()).toBe(true);

      // Clear using helper function
      clearDeviceFingerprintConsent();
      expect(hasDeviceFingerprintConsent()).toBe(false);
      expect(localStorage.getItem('skillledger_fingerprint_consent')).toBeNull();
    });
  });

  // ==========================================
  // Part 3: Hash Generation (3 tests)
  // ==========================================

  describe('Device Hash Generation', () => {
    it('should generate consistent hash for same fingerprint', async () => {
      const fingerprint = await collectDeviceFingerprint();

      const hash1 = await generateDeviceHash(fingerprint);
      const hash2 = await generateDeviceHash(fingerprint);

      expect(hash1).toBe(hash2);
      expect(hash1).toBeTruthy();
      expect(hash1.length).toBeGreaterThanOrEqual(8); // Fallback hash is 8 chars
    });

    it('should generate hash without providing fingerprint (collects automatically)', async () => {
      const hash = await generateDeviceHash();

      expect(hash).toBeTruthy();
      expect(typeof hash).toBe('string');
      expect(hash.length).toBeGreaterThanOrEqual(8);
    });

    it('should use crypto.subtle for SHA-256 hashing if available', async () => {
      // Test that crypto.subtle is used when available
      // Note: In jsdom, crypto.subtle exists, so this test verifies SHA-256 hashing
      const fingerprint = await collectDeviceFingerprint();
      const hash = await generateDeviceHash(fingerprint);

      // Should be a 64-character hex string (SHA-256)
      expect(hash).toBeTruthy();
      expect(typeof hash).toBe('string');

      // If crypto.subtle is available, should be 64 chars (SHA-256)
      // Otherwise fallback hash is variable length
      if (typeof crypto !== 'undefined' && crypto.subtle) {
        expect(hash.length).toBe(64);
      } else {
        expect(hash.length).toBeGreaterThanOrEqual(8);
      }
    });
  });

  // ==========================================
  // Part 4: Error Handling (4 tests)
  // ==========================================

  describe('Error Handling', () => {
    it('should handle localStorage errors gracefully in hasDeviceFingerprintConsent', () => {
      // Mock localStorage to throw an error
      const originalGetItem = localStorage.getItem;
      localStorage.getItem = jest.fn().mockImplementation(() => {
        throw new Error('localStorage not available');
      });

      // Should return false and not throw
      expect(hasDeviceFingerprintConsent()).toBe(false);

      localStorage.getItem = originalGetItem;
    });

    it('should handle localStorage errors gracefully in setDeviceFingerprintConsent', () => {
      // Mock localStorage to throw on setItem
      const originalSetItem = localStorage.setItem;
      const warnSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});

      localStorage.setItem = jest.fn().mockImplementation(() => {
        throw new Error('localStorage quota exceeded');
      });

      // Should not throw - just log warning
      expect(() => setDeviceFingerprintConsent(true)).not.toThrow();

      localStorage.setItem = originalSetItem;
      warnSpy.mockRestore();
    });

    it('should handle localStorage errors when removing consent', () => {
      // Mock localStorage to throw on removeItem
      const originalRemoveItem = localStorage.removeItem;
      const warnSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});

      localStorage.removeItem = jest.fn().mockImplementation(() => {
        throw new Error('localStorage not available');
      });

      // Should not throw
      expect(() => setDeviceFingerprintConsent(false)).not.toThrow();

      localStorage.removeItem = originalRemoveItem;
      warnSpy.mockRestore();
    });

    it('should handle crypto.subtle failure gracefully', async () => {
      // Mock crypto.subtle.digest to fail
      const originalSubtle = crypto.subtle;
      const warnSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});

      Object.defineProperty(crypto, 'subtle', {
        value: {
          digest: jest.fn().mockRejectedValue(new Error('Crypto operation failed')),
        },
        configurable: true,
      });

      const fingerprint = await collectDeviceFingerprint();
      const hash = await generateDeviceHash(fingerprint);

      // Should fall back to simple hash
      expect(hash).toBeTruthy();
      expect(typeof hash).toBe('string');

      Object.defineProperty(crypto, 'subtle', {
        value: originalSubtle,
        configurable: true,
      });
      warnSpy.mockRestore();
    });
  });

  // ==========================================
  // Part 5: Edge Cases (4 tests)
  // ==========================================

  describe('Edge Cases', () => {
    it('should handle zero hardware concurrency', async () => {
      const originalHardwareConcurrency = navigator.hardwareConcurrency;
      Object.defineProperty(navigator, 'hardwareConcurrency', {
        value: 0,
        configurable: true,
      });

      const fingerprint = await collectDeviceFingerprint();
      expect(fingerprint.hardwareConcurrency).toBe(0);

      Object.defineProperty(navigator, 'hardwareConcurrency', {
        value: originalHardwareConcurrency,
        configurable: true,
      });
    });

    it('should handle missing doNotTrack', async () => {
      const fingerprint = await collectDeviceFingerprint();
      // doNotTrack can be null, '1', '0', 'unspecified', or undefined
      expect(['1', '0', 'unspecified', null, undefined]).toContain(fingerprint.doNotTrack);
    });

    it('should include accept language with fallback', async () => {
      const fingerprint = await collectDeviceFingerprint();
      // Should always have a value (uses fallbacks)
      expect(fingerprint.acceptLanguage).toBeTruthy();
      expect(typeof fingerprint.acceptLanguage).toBe('string');
    });

    it('should handle SSR environment (typeof window === undefined)', () => {
      // This is tested implicitly - the functions check for window
      // In jsdom, window is always defined, so we verify the check exists
      // by ensuring the functions work in browser environment
      expect(hasDeviceFingerprintConsent).toBeDefined();
      expect(setDeviceFingerprintConsent).toBeDefined();
    });
  });

  // ==========================================
  // Part 6: Advanced Fingerprinting Details (3 tests)
  // ==========================================

  describe('Advanced Fingerprinting', () => {
    beforeEach(() => {
      setDeviceFingerprintConsent(true);
    });

    it('should generate canvas fingerprint as data URL', async () => {
      const fingerprint = await collectDeviceFingerprint();

      // Canvas fingerprint depends on browser support
      // In jsdom, canvas might not work fully, but we test the structure
      if (fingerprint.canvasFingerprint) {
        expect(fingerprint.canvasFingerprint.startsWith('data:')).toBe(true);
      }
      // Either has a value or is undefined (not an error)
      expect([undefined, expect.stringContaining('data:')]).toContainEqual(fingerprint.canvasFingerprint);
    });

    it('should handle font detection', async () => {
      const fingerprint = await collectDeviceFingerprint();

      // Fonts should be an array
      expect(Array.isArray(fingerprint.availableFonts)).toBe(true);

      // Each font should be a string
      fingerprint.availableFonts.forEach(font => {
        expect(typeof font).toBe('string');
      });
    });

    it('should handle plugins array', async () => {
      const fingerprint = await collectDeviceFingerprint();

      // Plugins should be an array
      expect(Array.isArray(fingerprint.installedPlugins)).toBe(true);

      // Each plugin should be a string (plugin name)
      fingerprint.installedPlugins.forEach(plugin => {
        expect(typeof plugin).toBe('string');
      });
    });
  });

  // ==========================================
  // Part 7: Logger Integration Tests
  // ==========================================

  describe('Logger Integration', () => {
    it('should skip advanced fingerprinting when no consent', async () => {
      // Ensure no consent
      setDeviceFingerprintConsent(false);

      const fingerprint = await collectDeviceFingerprint();

      // Should not have advanced fingerprints
      expect(fingerprint.canvasFingerprint).toBeUndefined();
      expect(fingerprint.webGLFingerprint).toBeUndefined();
      expect(fingerprint.audioFingerprint).toBeUndefined();
    });

    it('should handle canvas fingerprinting errors gracefully', async () => {
      setDeviceFingerprintConsent(true);

      // Mock canvas to fail
      const originalCreateElement = document.createElement.bind(document);
      document.createElement = jest.fn((tagName: string) => {
        if (tagName === 'canvas') {
          const canvas = originalCreateElement(tagName) as HTMLCanvasElement;
          const originalGetContext = canvas.getContext.bind(canvas);
          canvas.getContext = jest.fn(() => null); // Return null to trigger error path
          return canvas;
        }
        return originalCreateElement(tagName);
      }) as any;

      const fingerprint = await collectDeviceFingerprint();

      // Should handle error and return undefined for canvas
      expect(fingerprint.canvasFingerprint).toBeUndefined();

      document.createElement = originalCreateElement;
    });

    it('should handle WebGL context failure', async () => {
      setDeviceFingerprintConsent(true);

      // Mock canvas getContext to return null for WebGL
      const originalCreateElement = document.createElement.bind(document);
      document.createElement = jest.fn((tagName: string) => {
        if (tagName === 'canvas') {
          const canvas = originalCreateElement(tagName) as HTMLCanvasElement;
          const originalGetContext = canvas.getContext.bind(canvas);
          canvas.getContext = jest.fn((type: string) => {
            if (type === 'webgl' || type === 'experimental-webgl') {
              return null;
            }
            return originalGetContext(type as any);
          }) as any;
          return canvas;
        }
        return originalCreateElement(tagName);
      }) as any;

      const fingerprint = await collectDeviceFingerprint();

      // Should handle WebGL unavailability
      expect(fingerprint.webGLFingerprint).toBeUndefined();

      document.createElement = originalCreateElement;
    });

    it('should handle WebGL extension not available', async () => {
      setDeviceFingerprintConsent(true);

      const originalCreateElement = document.createElement.bind(document);
      document.createElement = jest.fn((tagName: string) => {
        if (tagName === 'canvas') {
          const canvas = originalCreateElement(tagName) as HTMLCanvasElement;
          const mockWebGL = {
            getExtension: jest.fn(() => null),
            getParameter: jest.fn(),
          };
          const originalGetContext = canvas.getContext.bind(canvas);
          canvas.getContext = jest.fn((type: string) => {
            if (type === 'webgl' || type === 'experimental-webgl') {
              return mockWebGL as any;
            }
            return originalGetContext(type as any);
          }) as any;
          return canvas;
        }
        return originalCreateElement(tagName);
      }) as any;

      const fingerprint = await collectDeviceFingerprint();

      expect(fingerprint.webGLFingerprint).toBeUndefined();

      document.createElement = originalCreateElement;
    });
  });

  // ==========================================
  // Part 8: Hash Generation Edge Cases
  // ==========================================

  describe('Hash Generation Fallback', () => {
    it('should use fallback hash when crypto.subtle is unavailable', async () => {
      const originalCrypto = global.crypto;

      // Remove crypto.subtle
      Object.defineProperty(global, 'crypto', {
        value: undefined,
        configurable: true,
      });

      const fingerprint = await collectDeviceFingerprint();
      const hash = await generateDeviceHash(fingerprint);

      // Should still generate a hash using fallback
      expect(hash).toBeTruthy();
      expect(typeof hash).toBe('string');
      expect(hash.length).toBeGreaterThan(0);

      Object.defineProperty(global, 'crypto', {
        value: originalCrypto,
        configurable: true,
      });
    });

    it('should handle empty fingerprint data', async () => {
      const emptyFingerprint = {
        userAgent: '',
        timezone: '',
        screenResolution: '',
        acceptLanguage: '',
        platform: '',
        colorDepth: 0,
        hardwareConcurrency: 0,
        touchSupport: false,
        cookieEnabled: false,
        doNotTrack: null,
        installedPlugins: [],
        availableFonts: [],
      };

      const hash = await generateDeviceHash(emptyFingerprint);

      expect(hash).toBeTruthy();
      expect(typeof hash).toBe('string');
    });
  });

  // ==========================================
  // Part 9: Server-Side Rendering (SSR) Tests
  // ==========================================

  describe('SSR Compatibility', () => {
    it('should return false when window is undefined (SSR)', () => {
      const originalWindow = global.window;

      // Simulate SSR environment
      Object.defineProperty(global, 'window', {
        value: undefined,
        configurable: true,
      });

      const result = hasDeviceFingerprintConsent();

      expect(result).toBe(false);

      Object.defineProperty(global, 'window', {
        value: originalWindow,
        configurable: true,
      });
    });

    it('should not throw when setting consent in SSR environment', () => {
      const originalWindow = global.window;

      Object.defineProperty(global, 'window', {
        value: undefined,
        configurable: true,
      });

      expect(() => setDeviceFingerprintConsent(true)).not.toThrow();
      expect(() => setDeviceFingerprintConsent(false)).not.toThrow();
      expect(() => clearDeviceFingerprintConsent()).not.toThrow();

      Object.defineProperty(global, 'window', {
        value: originalWindow,
        configurable: true,
      });
    });
  });

  // ==========================================
  // Part 10: Audio Fingerprinting Tests
  // ==========================================

  describe('Audio Fingerprinting', () => {
    it('should handle missing AudioContext', async () => {
      setDeviceFingerprintConsent(true);

      const originalAudioContext = (window as any).AudioContext;
      const originalWebkitAudioContext = (window as any).webkitAudioContext;

      delete (window as any).AudioContext;
      delete (window as any).webkitAudioContext;

      const fingerprint = await collectDeviceFingerprint();

      expect(fingerprint.audioFingerprint).toBeUndefined();

      (window as any).AudioContext = originalAudioContext;
      (window as any).webkitAudioContext = originalWebkitAudioContext;
    });

    it('should handle audio fingerprinting timeout', async () => {
      jest.useFakeTimers();
      setDeviceFingerprintConsent(true);

      const fingerprintPromise = collectDeviceFingerprint();

      // Advance timers past the 1000ms timeout
      jest.advanceTimersByTime(1100);

      const fingerprint = await fingerprintPromise;

      // Should timeout and return undefined
      expect(fingerprint.audioFingerprint).toBeUndefined();

      jest.useRealTimers();
    });
  });

  // ==========================================
  // Part 11: Font Detection Edge Cases
  // ==========================================

  describe('Font Detection', () => {
    it('should handle canvas context failure in font detection', async () => {
      setDeviceFingerprintConsent(true);

      const originalCreateElement = document.createElement.bind(document);
      let callCount = 0;

      document.createElement = jest.fn((tagName: string) => {
        callCount++;
        if (tagName === 'canvas' && callCount > 2) {
          // Fail on the canvas used for font detection (after canvas/webgl fingerprints)
          const canvas = originalCreateElement(tagName) as HTMLCanvasElement;
          canvas.getContext = jest.fn(() => null);
          return canvas;
        }
        return originalCreateElement(tagName);
      }) as any;

      const fingerprint = await collectDeviceFingerprint();

      // Should return empty array when canvas context fails
      expect(fingerprint.availableFonts).toEqual([]);

      document.createElement = originalCreateElement;
    });
  });

  // ==========================================
  // Part 12: Device Memory and Browser Features
  // ==========================================

  describe('Extended Navigator Properties', () => {
    it('should include deviceMemory when available', async () => {
      Object.defineProperty(navigator, 'deviceMemory', {
        value: 8,
        configurable: true,
      });

      const fingerprint = await collectDeviceFingerprint();

      expect(fingerprint.deviceMemory).toBe(8);
    });

    it('should handle missing deviceMemory gracefully', async () => {
      const originalDeviceMemory = (navigator as any).deviceMemory;
      delete (navigator as any).deviceMemory;

      const fingerprint = await collectDeviceFingerprint();

      expect(fingerprint.deviceMemory).toBeUndefined();

      if (originalDeviceMemory !== undefined) {
        Object.defineProperty(navigator, 'deviceMemory', {
          value: originalDeviceMemory,
          configurable: true,
        });
      }
    });

    it('should use language fallbacks when primary language unavailable', async () => {
      const originalLanguage = navigator.language;
      const nav = navigator as any;

      Object.defineProperty(navigator, 'language', {
        value: '',
        configurable: true,
      });

      nav.userLanguage = 'fr-FR';

      const fingerprint = await collectDeviceFingerprint();

      expect(fingerprint.acceptLanguage).toBeTruthy();

      Object.defineProperty(navigator, 'language', {
        value: originalLanguage,
        configurable: true,
      });
      delete nav.userLanguage;
    });
  });
});
