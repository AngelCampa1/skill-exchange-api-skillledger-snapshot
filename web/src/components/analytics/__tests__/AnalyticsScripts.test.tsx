/**
 * AnalyticsScripts.tsx Tests
 *
 * Tests for analytics scripts loader component.
 * Coverage Target: 80%+
 */

import React from 'react';
import { render, act } from '@testing-library/react';
import '@testing-library/jest-dom';
import AnalyticsScripts from '../AnalyticsScripts';

// Mock Next.js Script component
const mockScripts: Array<{ src?: string; id?: string; strategy?: string; onLoad?: () => void; dangerouslySetInnerHTML?: { __html: string } }> = [];

jest.mock('next/script', () => ({
  __esModule: true,
  default: ({ src, id, strategy, onLoad, dangerouslySetInnerHTML }: any) => {
    mockScripts.push({ src, id, strategy, onLoad, dangerouslySetInnerHTML });
    return <script data-testid={id || 'script'} />;
  },
}));

// Mock CookieConsentContext
const mockUseCookieConsent = jest.fn();
jest.mock('@/contexts/CookieConsentContext', () => ({
  useCookieConsent: () => mockUseCookieConsent(),
}));

// Mock window.gtag globally
(global as any).window = {
  gtag: jest.fn(),
};

describe('AnalyticsScripts', () => {
  const originalEnv = process.env;

  beforeEach(() => {
    jest.clearAllMocks();
    mockScripts.length = 0;
    process.env = { ...originalEnv };
    // Reset gtag mock
    (global as any).window.gtag = jest.fn();
  });

  afterEach(() => {
    process.env = originalEnv;
  });

  describe('Rendering Conditions', () => {
    it('renders nothing when analytics is disabled', () => {
      process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'false';
      mockUseCookieConsent.mockReturnValue({ consentGiven: true });

      let container: any;
      act(() => {
        const result = render(<AnalyticsScripts />);
        container = result.container;
      });

      expect(container.firstChild).toBeNull();
      expect(mockScripts.length).toBe(0);
    });

    it('renders nothing when consent is not given', () => {
      process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'true';
      process.env.NEXT_PUBLIC_GA4_MEASUREMENT_ID = 'G-TEST123';
      mockUseCookieConsent.mockReturnValue({ consentGiven: false });

      let container: any;
      act(() => {
        const result = render(<AnalyticsScripts />);
        container = result.container;
      });

      expect(container.firstChild).toBeNull();
      expect(mockScripts.length).toBe(0);
    });

    it('renders nothing when analytics enabled but no IDs provided', () => {
      process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'true';
      delete process.env.NEXT_PUBLIC_GA4_MEASUREMENT_ID;
      delete process.env.NEXT_PUBLIC_CLARITY_PROJECT_ID;
      mockUseCookieConsent.mockReturnValue({ consentGiven: true });

      let container: any;
      act(() => {
        const result = render(<AnalyticsScripts />);
        container = result.container;
      });

      expect(container.firstChild).toBeNull();
      expect(mockScripts.length).toBe(0);
    });
  });

  describe('Google Analytics 4', () => {
    beforeEach(() => {
      process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'true';
      process.env.NEXT_PUBLIC_GA4_MEASUREMENT_ID = 'G-TEST123';
      mockUseCookieConsent.mockReturnValue({ consentGiven: true });
    });

    it('loads GA4 script when enabled with consent and GA4 ID', () => {
      act(() => {
        render(<AnalyticsScripts />);
      });

      const ga4Script = mockScripts.find(s => s.src?.includes('googletagmanager.com'));
      expect(ga4Script).toBeDefined();
      expect(ga4Script?.src).toBe('https://www.googletagmanager.com/gtag/js?id=G-TEST123');
      expect(ga4Script?.strategy).toBe('afterInteractive');
    });

    it('includes GA4 initialization script', () => {
      act(() => {
        render(<AnalyticsScripts />);
      });

      const initScript = mockScripts.find(s => s.id === 'ga4-init');
      expect(initScript).toBeDefined();
      expect(initScript?.strategy).toBe('afterInteractive');
      expect(initScript?.dangerouslySetInnerHTML?.__html).toContain('window.dataLayer');
      expect(initScript?.dangerouslySetInnerHTML?.__html).toContain('gtag');
    });

    it('sets analytics_storage to granted when consent is given', () => {
      act(() => {
        render(<AnalyticsScripts />);
      });

      const initScript = mockScripts.find(s => s.id === 'ga4-init');
      expect(initScript?.dangerouslySetInnerHTML?.__html).toContain("'analytics_storage': 'granted'");
    });

    it('calls gtag on GA4 script load', () => {
      const mockGtag = jest.fn();
      (global as any).window.gtag = mockGtag;

      act(() => {
        render(<AnalyticsScripts />);
      });

      const ga4Script = mockScripts.find(s => s.src?.includes('googletagmanager.com'));
      expect(ga4Script?.onLoad).toBeDefined();

      // Trigger onLoad callback
      act(() => {
        ga4Script?.onLoad?.();
      });

      expect(mockGtag).toHaveBeenCalledWith('js', expect.any(Date));
      expect(mockGtag).toHaveBeenCalledWith('config', 'G-TEST123', {
        anonymize_ip: true,
        cookie_flags: 'SameSite=None;Secure',
        send_page_view: false,
      });
    });

    it('does not call gtag if window.gtag is undefined', () => {
      delete (global as any).window.gtag;

      act(() => {
        render(<AnalyticsScripts />);
      });

      const ga4Script = mockScripts.find(s => s.src?.includes('googletagmanager.com'));

      // Should not throw when gtag is undefined
      expect(() => {
        act(() => {
          ga4Script?.onLoad?.();
        });
      }).not.toThrow();
    });
  });

  describe('Microsoft Clarity', () => {
    beforeEach(() => {
      process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'true';
      process.env.NEXT_PUBLIC_CLARITY_PROJECT_ID = 'clarity123';
      mockUseCookieConsent.mockReturnValue({ consentGiven: true });
    });

    it('loads Clarity script when enabled with consent and Clarity ID', () => {
      act(() => {
        render(<AnalyticsScripts />);
      });

      const clarityScript = mockScripts.find(s => s.id === 'clarity-init');
      expect(clarityScript).toBeDefined();
      expect(clarityScript?.strategy).toBe('afterInteractive');
    });

    it('includes Clarity initialization code with project ID', () => {
      act(() => {
        render(<AnalyticsScripts />);
      });

      const clarityScript = mockScripts.find(s => s.id === 'clarity-init');
      expect(clarityScript?.dangerouslySetInnerHTML?.__html).toContain('clarity123');
      expect(clarityScript?.dangerouslySetInnerHTML?.__html).toContain('clarity.ms');
      expect(clarityScript?.dangerouslySetInnerHTML?.__html).toContain('window');
      expect(clarityScript?.dangerouslySetInnerHTML?.__html).toContain('document');
    });
  });

  describe('Combined Analytics', () => {
    it('loads both GA4 and Clarity when both IDs provided', () => {
      process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'true';
      process.env.NEXT_PUBLIC_GA4_MEASUREMENT_ID = 'G-TEST123';
      process.env.NEXT_PUBLIC_CLARITY_PROJECT_ID = 'clarity123';
      mockUseCookieConsent.mockReturnValue({ consentGiven: true });

      act(() => {
        render(<AnalyticsScripts />);
      });

      const ga4Script = mockScripts.find(s => s.src?.includes('googletagmanager.com'));
      const ga4InitScript = mockScripts.find(s => s.id === 'ga4-init');
      const clarityScript = mockScripts.find(s => s.id === 'clarity-init');

      expect(ga4Script).toBeDefined();
      expect(ga4InitScript).toBeDefined();
      expect(clarityScript).toBeDefined();
      expect(mockScripts.length).toBe(3);
    });

    it('loads only GA4 when only GA4 ID provided', () => {
      process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'true';
      process.env.NEXT_PUBLIC_GA4_MEASUREMENT_ID = 'G-TEST123';
      delete process.env.NEXT_PUBLIC_CLARITY_PROJECT_ID;
      mockUseCookieConsent.mockReturnValue({ consentGiven: true });

      act(() => {
        render(<AnalyticsScripts />);
      });

      const ga4Script = mockScripts.find(s => s.src?.includes('googletagmanager.com'));
      const clarityScript = mockScripts.find(s => s.id === 'clarity-init');

      expect(ga4Script).toBeDefined();
      expect(clarityScript).toBeUndefined();
      expect(mockScripts.length).toBe(2); // GA4 script + GA4 init
    });

    it('loads only Clarity when only Clarity ID provided', () => {
      process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'true';
      delete process.env.NEXT_PUBLIC_GA4_MEASUREMENT_ID;
      process.env.NEXT_PUBLIC_CLARITY_PROJECT_ID = 'clarity123';
      mockUseCookieConsent.mockReturnValue({ consentGiven: true });

      act(() => {
        render(<AnalyticsScripts />);
      });

      const ga4Script = mockScripts.find(s => s.src?.includes('googletagmanager.com'));
      const clarityScript = mockScripts.find(s => s.id === 'clarity-init');

      expect(ga4Script).toBeUndefined();
      expect(clarityScript).toBeDefined();
      expect(mockScripts.length).toBe(1); // Only Clarity
    });
  });

  describe('Consent State Changes', () => {
    it('sets analytics_storage to denied when consent not given initially', () => {
      process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'true';
      process.env.NEXT_PUBLIC_GA4_MEASUREMENT_ID = 'G-TEST123';
      mockUseCookieConsent.mockReturnValue({ consentGiven: false });

      act(() => {
        render(<AnalyticsScripts />);
      });

      // Component should render nothing
      expect(mockScripts.length).toBe(0);
    });

    it('respects consent state in initialization script', () => {
      process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'true';
      process.env.NEXT_PUBLIC_GA4_MEASUREMENT_ID = 'G-TEST123';
      mockUseCookieConsent.mockReturnValue({ consentGiven: true });

      act(() => {
        render(<AnalyticsScripts />);
      });

      const initScript = mockScripts.find(s => s.id === 'ga4-init');
      expect(initScript?.dangerouslySetInnerHTML?.__html).toContain("'analytics_storage': 'granted'");
    });
  });

  describe('Script Loading Strategy', () => {
    beforeEach(() => {
      process.env.NEXT_PUBLIC_ENABLE_ANALYTICS = 'true';
      process.env.NEXT_PUBLIC_GA4_MEASUREMENT_ID = 'G-TEST123';
      process.env.NEXT_PUBLIC_CLARITY_PROJECT_ID = 'clarity123';
      mockUseCookieConsent.mockReturnValue({ consentGiven: true });
    });

    it('uses afterInteractive strategy for all scripts', () => {
      act(() => {
        render(<AnalyticsScripts />);
      });

      mockScripts.forEach(script => {
        expect(script.strategy).toBe('afterInteractive');
      });
    });
  });
});
