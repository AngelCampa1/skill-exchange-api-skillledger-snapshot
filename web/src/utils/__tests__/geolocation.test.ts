/**
 * geolocation.ts Tests
 *
 * Tests IP-based location detection with fallback strategy and compliance restrictions.
 * Focus: API integration, country restrictions, VPN detection, enhanced verification.
 *
 * Coverage Target: 85%+ (218 lines)
 * Test Count: 22 tests
 */

import {
  getUserGeolocation,
  isLocationRestricted,
  getLocationRestrictionMessage,
  getVPNWarningMessage,
  getEnhancedVerificationMessage,
  GeolocationInfo,
} from '../geolocation';
import { setupFetchMock } from '@/utils/test/testUtils';

describe('geolocation.ts - IP Location & Compliance', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  beforeEach(() => {
    fetchMock = setupFetchMock();
    jest.clearAllMocks();
  });

  afterEach(() => {
    fetchMock.reset();
  });

  // ==========================================
  // Part 1: getUserGeolocation - Primary API (3 tests)
  // ==========================================

  describe('getUserGeolocation - Primary API Success', () => {
    it('should fetch geolocation from primary API successfully', async () => {
      const mockGeolocation: GeolocationInfo = {
        ip: '192.168.1.1',
        country: 'United States',
        countryCode: 'US',
        region: 'California',
        city: 'San Francisco',
        timezone: 'America/Los_Angeles',
        isVPN: false,
        isProxy: false,
        isTor: false,
        riskScore: 10,
        isRestricted: false,
      };

      fetchMock.respondWith({ geolocation: mockGeolocation });

      const result = await getUserGeolocation();

      expect(result.success).toBe(true);
      expect(result.data).toEqual(mockGeolocation);
      expect(result.fallbackUsed).toBe(false);
      expect(global.fetch).toHaveBeenCalledWith(
        '/api/user/geolocation',
        expect.objectContaining({
          method: 'GET',
          headers: { Accept: 'application/json' },
        })
      );
    });

    it('should return correct data structure from primary API', async () => {
      const mockGeolocation: GeolocationInfo = {
        ip: '10.0.0.1',
        country: 'Canada',
        countryCode: 'CA',
        region: 'Ontario',
        city: 'Toronto',
        timezone: 'America/Toronto',
        isVPN: false,
        isProxy: false,
        isTor: false,
        riskScore: 10,
        isRestricted: false,
      };

      fetchMock.respondWith({ geolocation: mockGeolocation });

      const result = await getUserGeolocation();

      expect(result.data).toHaveProperty('ip');
      expect(result.data).toHaveProperty('country');
      expect(result.data).toHaveProperty('countryCode');
      expect(result.data).toHaveProperty('timezone');
      expect(result.data).toHaveProperty('riskScore');
      expect(result.data?.riskScore).toBeGreaterThanOrEqual(0);
    });

    it('should mark fallbackUsed as false when primary API succeeds', async () => {
      const mockGeolocation: GeolocationInfo = {
        ip: '1.2.3.4',
        country: 'Australia',
        countryCode: 'AU',
        region: 'NSW',
        city: 'Sydney',
        timezone: 'Australia/Sydney',
        isVPN: false,
        isProxy: false,
        isTor: false,
        riskScore: 10,
        isRestricted: false,
      };

      fetchMock.respondWith({ geolocation: mockGeolocation });

      const result = await getUserGeolocation();

      expect(result.fallbackUsed).toBe(false);
    });
  });

  // ==========================================
  // Part 2: getUserGeolocation - Fallback Strategy (4 tests)
  // ==========================================

  describe('getUserGeolocation - Fallback Strategy', () => {
    it('should use fallback API when primary fails', async () => {
      // First call (primary) fails
      fetchMock.respondWithError(500, 'Primary API down');

      // Second call (fallback) succeeds
      const fallbackData = {
        query: '8.8.8.8',
        country: 'United States',
        countryCode: 'US',
        regionName: 'California',
        city: 'Mountain View',
        timezone: 'America/Los_Angeles',
      };
      fetchMock.respondWith(fallbackData);

      const result = await getUserGeolocation();

      expect(result.success).toBe(true);
      expect(result.fallbackUsed).toBe(true);
      expect(result.data?.ip).toBe('8.8.8.8');
      expect(result.data?.country).toBe('United States');
      expect(result.data?.countryCode).toBe('US');

      // Verify both APIs were called
      const calls = fetchMock.getCalls();
      expect(calls).toHaveLength(2);
      expect(calls[0].url).toBe('/api/user/geolocation');
      expect(calls[1].url).toBe('https://ip-api.com/json/');
    });

    it('should transform fallback API data correctly', async () => {
      fetchMock.respondWithError(500, 'Primary API down');

      const fallbackData = {
        query: '123.45.67.89',
        country: 'Germany',
        countryCode: 'DE',
        regionName: 'Bavaria',
        city: 'Munich',
        timezone: 'Europe/Berlin',
      };
      fetchMock.respondWith(fallbackData);

      const result = await getUserGeolocation();

      expect(result.data).toEqual({
        ip: '123.45.67.89',
        country: 'Germany',
        countryCode: 'DE',
        region: 'Bavaria',
        city: 'Munich',
        timezone: 'Europe/Berlin',
        isVPN: false, // Fallback doesn't detect VPN
        isProxy: false,
        isTor: false,
        riskScore: 10, // Germany is not restricted or enhanced
        isRestricted: false,
        restrictionReason: undefined,
      });
    });

    it('should calculate risk score correctly for restricted country in fallback', async () => {
      fetchMock.respondWithError(500, 'Primary API down');

      const fallbackData = {
        query: '1.1.1.1',
        country: 'Iran',
        countryCode: 'IR', // Restricted country
        regionName: 'Tehran',
        city: 'Tehran',
        timezone: 'Asia/Tehran',
      };
      fetchMock.respondWith(fallbackData);

      const result = await getUserGeolocation();

      expect(result.data?.riskScore).toBe(100); // Restricted country = 100
      expect(result.data?.isRestricted).toBe(true);
      expect(result.data?.restrictionReason).toBe('Country is subject to regulatory restrictions');
    });

    it('should return error when both primary and fallback APIs fail', async () => {
      fetchMock.respondWithError(500, 'Primary API down');
      fetchMock.respondWithError(503, 'Fallback API down');

      const result = await getUserGeolocation();

      expect(result.success).toBe(false);
      expect(result.error).toBe('Unable to determine location');
      expect(result.data).toBeUndefined();
    });
  });

  // ==========================================
  // Part 3: isLocationRestricted (5 tests)
  // ==========================================

  describe('isLocationRestricted', () => {
    it('should return isRestricted=true for sanctioned country', async () => {
      const result = isLocationRestricted('IR'); // Iran (OFAC sanctioned)

      expect(result.isRestricted).toBe(true);
      expect(result.reason).toBe('Registration is not currently available in your country due to regulatory restrictions.');
      expect(result.requiresEnhancedVerification).toBe(false);
    });

    it('should return isRestricted=false for unrestricted country', async () => {
      const result = isLocationRestricted('US'); // United States

      expect(result.isRestricted).toBe(false);
      expect(result.reason).toBeUndefined();
      expect(result.requiresEnhancedVerification).toBe(false);
    });

    it('should return requiresEnhancedVerification=true for enhanced verification country', async () => {
      const result = isLocationRestricted('CN'); // China

      expect(result.isRestricted).toBe(false);
      expect(result.requiresEnhancedVerification).toBe(true);
      expect(result.reason).toBeUndefined();
    });

    it('should handle lowercase country codes correctly', async () => {
      const resultLower = isLocationRestricted('ir'); // lowercase
      const resultUpper = isLocationRestricted('IR'); // uppercase

      expect(resultLower).toEqual(resultUpper);
      expect(resultLower.isRestricted).toBe(true);
    });

    it('should return isRestricted=false for unknown country code', async () => {
      const result = isLocationRestricted('ZZ'); // Unknown country

      expect(result.isRestricted).toBe(false);
      expect(result.requiresEnhancedVerification).toBe(false);
      expect(result.reason).toBeUndefined();
    });
  });

  // ==========================================
  // Part 4: getLocationRestrictionMessage (3 tests)
  // ==========================================

  describe('getLocationRestrictionMessage', () => {
    it('should return null when location is not restricted', () => {
      const geolocation: GeolocationInfo = {
        ip: '1.2.3.4',
        country: 'United States',
        countryCode: 'US',
        region: 'CA',
        city: 'SF',
        timezone: 'America/Los_Angeles',
        isVPN: false,
        isProxy: false,
        isTor: false,
        riskScore: 10,
        isRestricted: false,
      };

      const message = getLocationRestrictionMessage(geolocation);

      expect(message).toBeNull();
    });

    it('should return multi-sentence message when restricted', () => {
      const geolocation: GeolocationInfo = {
        ip: '1.2.3.4',
        country: 'Iran',
        countryCode: 'IR',
        region: 'Tehran',
        city: 'Tehran',
        timezone: 'Asia/Tehran',
        isVPN: false,
        isProxy: false,
        isTor: false,
        riskScore: 100,
        isRestricted: true,
        restrictionReason: 'Country is subject to regulatory restrictions',
      };

      const message = getLocationRestrictionMessage(geolocation);

      expect(message).toContain("We're unable to provide services to users in Iran");
      expect(message).toContain('regulatory and compliance requirements');
      expect(message).toContain('working to expand our service availability');
    });

    it('should include VPN warning when VPN detected', () => {
      const geolocation: GeolocationInfo = {
        ip: '1.2.3.4',
        country: 'Iran',
        countryCode: 'IR',
        region: 'Tehran',
        city: 'Tehran',
        timezone: 'Asia/Tehran',
        isVPN: true, // VPN detected
        isProxy: false,
        isTor: false,
        riskScore: 100,
        isRestricted: true,
        restrictionReason: 'Country is subject to regulatory restrictions',
      };

      const message = getLocationRestrictionMessage(geolocation);

      expect(message).toContain('We detected that you may be using a VPN');
      expect(message).toContain('Please disable any privacy tools');
      expect(message).toContain("We're unable to provide services to users in Iran");
    });
  });

  // ==========================================
  // Part 5: getVPNWarningMessage (4 tests)
  // ==========================================

  describe('getVPNWarningMessage', () => {
    it('should return null when no VPN/proxy/Tor detected', () => {
      const geolocation: GeolocationInfo = {
        ip: '1.2.3.4',
        country: 'US',
        countryCode: 'US',
        region: 'CA',
        city: 'SF',
        timezone: 'America/Los_Angeles',
        isVPN: false,
        isProxy: false,
        isTor: false,
        riskScore: 10,
        isRestricted: false,
      };

      const message = getVPNWarningMessage(geolocation);

      expect(message).toBeNull();
    });

    it('should return warning when VPN detected', () => {
      const geolocation: GeolocationInfo = {
        ip: '1.2.3.4',
        country: 'US',
        countryCode: 'US',
        region: 'CA',
        city: 'SF',
        timezone: 'America/Los_Angeles',
        isVPN: true,
        isProxy: false,
        isTor: false,
        riskScore: 10,
        isRestricted: false,
      };

      const message = getVPNWarningMessage(geolocation);

      expect(message).toContain('We detected you may be using a VPN or proxy');
      expect(message).toContain('connect from your actual location');
    });

    it('should return warning when proxy detected', () => {
      const geolocation: GeolocationInfo = {
        ip: '1.2.3.4',
        country: 'US',
        countryCode: 'US',
        region: 'CA',
        city: 'SF',
        timezone: 'America/Los_Angeles',
        isVPN: false,
        isProxy: true,
        isTor: false,
        riskScore: 10,
        isRestricted: false,
      };

      const message = getVPNWarningMessage(geolocation);

      expect(message).toContain('We detected you may be using a VPN or proxy');
    });

    it('should return warning when Tor detected', () => {
      const geolocation: GeolocationInfo = {
        ip: '1.2.3.4',
        country: 'US',
        countryCode: 'US',
        region: 'CA',
        city: 'SF',
        timezone: 'America/Los_Angeles',
        isVPN: false,
        isProxy: false,
        isTor: true,
        riskScore: 10,
        isRestricted: false,
      };

      const message = getVPNWarningMessage(geolocation);

      expect(message).toContain('We detected you may be using a VPN or proxy');
    });
  });

  // ==========================================
  // Part 6: getEnhancedVerificationMessage (3 tests)
  // ==========================================

  describe('getEnhancedVerificationMessage', () => {
    it('should return null for unrestricted country', () => {
      const message = getEnhancedVerificationMessage('US');

      expect(message).toBeNull();
    });

    it('should return message for enhanced verification country', () => {
      const message = getEnhancedVerificationMessage('CN'); // China requires enhanced verification

      expect(message).toContain('Due to your location');
      expect(message).toContain('additional verification steps may be required');
      expect(message).toContain('platform security and compliance');
    });

    it('should handle lowercase country codes correctly', () => {
      const messageLower = getEnhancedVerificationMessage('cn');
      const messageUpper = getEnhancedVerificationMessage('CN');

      expect(messageLower).toBe(messageUpper);
      expect(messageLower).not.toBeNull();
    });
  });
});
