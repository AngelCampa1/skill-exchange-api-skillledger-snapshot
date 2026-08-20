/**
 * PageViewTracker.tsx Tests
 *
 * Tests for page view tracking component.
 * Coverage Target: 80%+
 */

import React from 'react';
import { render, act } from '@testing-library/react';
import '@testing-library/jest-dom';
import PageViewTracker from '../PageViewTracker';

// Mock Next.js navigation hooks
const mockUsePathname = jest.fn();
const mockUseSearchParams = jest.fn();

jest.mock('next/navigation', () => ({
  usePathname: () => mockUsePathname(),
  useSearchParams: () => mockUseSearchParams(),
}));

// Mock useAnalytics hook
const mockTrackPageView = jest.fn();
jest.mock('@/hooks/useAnalytics', () => ({
  useAnalytics: () => ({
    trackPageView: mockTrackPageView,
  }),
}));

// Mock trackEvent utility
const mockTrackEvent = jest.fn();
jest.mock('@/utils/analytics', () => ({
  trackEvent: (args: any) => mockTrackEvent(args),
}));

// Mock Date.now for consistent timing
let currentTime = 1000;
const realDateNow = Date.now;

describe('PageViewTracker', () => {
  const mockSearchParams = {
    toString: jest.fn(() => ''),
  };

  let originalTitle: string;

  beforeAll(() => {
    // Replace Date.now with our mock
    Date.now = jest.fn(() => currentTime);
  });

  afterAll(() => {
    // Restore original Date.now
    Date.now = realDateNow;
  });

  beforeEach(() => {
    jest.clearAllMocks();
    currentTime = 1000; // Reset to initial time
    mockUsePathname.mockReturnValue('/home');
    mockUseSearchParams.mockReturnValue(mockSearchParams);
    mockSearchParams.toString.mockReturnValue('');
    // Save and mock document.title
    if (typeof document !== 'undefined') {
      originalTitle = document.title;
      Object.defineProperty(document, 'title', {
        value: 'Test Page',
        writable: true,
        configurable: true,
      });
    }
  });

  afterEach(() => {
    // Restore document.title
    if (typeof document !== 'undefined' && originalTitle !== undefined) {
      Object.defineProperty(document, 'title', {
        value: originalTitle,
        writable: true,
        configurable: true,
      });
    }
  });

  describe('Rendering', () => {
    it('renders nothing (returns null)', () => {
      const { container } = render(<PageViewTracker />);

      expect(container.firstChild).toBeNull();
    });
  });

  describe('Initial Page View', () => {
    it('tracks page view on mount', () => {
      render(<PageViewTracker />);

      expect(mockTrackPageView).toHaveBeenCalledWith('/home', 'Test Page');
    });

    it('does not track route change on initial mount', () => {
      render(<PageViewTracker />);

      expect(mockTrackEvent).not.toHaveBeenCalled();
    });

    it('tracks page view with search params', () => {
      mockSearchParams.toString.mockReturnValue('foo=bar&baz=qux');

      render(<PageViewTracker />);

      expect(mockTrackPageView).toHaveBeenCalledWith('/home?foo=bar&baz=qux', 'Test Page');
    });

    it('handles empty document.title', () => {
      document.title = '';

      render(<PageViewTracker />);

      expect(mockTrackPageView).toHaveBeenCalledWith('/home', '');
    });

    it('handles null pathname', () => {
      mockUsePathname.mockReturnValue(null);

      render(<PageViewTracker />);

      expect(mockTrackPageView).not.toHaveBeenCalled();
    });
  });

  describe('Route Changes', () => {
    it('tracks route change when pathname changes', () => {
      const { rerender } = render(<PageViewTracker />);

      expect(mockTrackPageView).toHaveBeenCalledWith('/home', 'Test Page');

      // Clear mocks after initial render
      jest.clearAllMocks();

      // Simulate navigation after 500ms
      currentTime = 1500;
      mockUsePathname.mockReturnValue('/about');

      rerender(<PageViewTracker />);

      // Should track new page view
      expect(mockTrackPageView).toHaveBeenCalledWith('/about', 'Test Page');

      // Should track route change event with navigation time
      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          name: 'route_change',
          category: 'navigation',
          priority: 'medium',
          properties: expect.objectContaining({
            from_path: '/home',
            to_path: '/about',
            has_search_params: false,
            navigation_time: 500,
          }),
        })
      );
    });

    it('tracks multiple route changes correctly', () => {
      const { rerender } = render(<PageViewTracker />);

      jest.clearAllMocks();

      // First navigation: /home -> /about after 500ms
      currentTime = 1500;
      mockUsePathname.mockReturnValue('/about');

      rerender(<PageViewTracker />);

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            from_path: '/home',
            to_path: '/about',
            navigation_time: 500,
          }),
        })
      );

      jest.clearAllMocks();

      // Second navigation: /about -> /contact after 500ms
      currentTime = 2000;
      mockUsePathname.mockReturnValue('/contact');

      rerender(<PageViewTracker />);

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            from_path: '/about',
            to_path: '/contact',
            navigation_time: 500,
          }),
        })
      );
    });

    it('includes search params flag in route change event', () => {
      const { rerender } = render(<PageViewTracker />);

      jest.clearAllMocks();

      // Navigate with search params after 500ms
      currentTime = 1500;
      mockUsePathname.mockReturnValue('/search');
      const newSearchParams = { toString: jest.fn(() => 'q=test') };
      mockUseSearchParams.mockReturnValue(newSearchParams);

      rerender(<PageViewTracker />);

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            has_search_params: true,
          }),
        })
      );
    });

    it('does not track route change when only search params change', () => {
      const { rerender } = render(<PageViewTracker />);

      jest.clearAllMocks();

      // Same pathname, different search params - need new object
      const newSearchParams = { toString: jest.fn(() => 'tab=settings') };
      mockUseSearchParams.mockReturnValue(newSearchParams);

      rerender(<PageViewTracker />);

      // Should track page view for URL change
      expect(mockTrackPageView).toHaveBeenCalledWith('/home?tab=settings', 'Test Page');

      // Should NOT track route change (pathname didn't change)
      expect(mockTrackEvent).not.toHaveBeenCalled();
    });
  });

  describe('Ref Management', () => {
    it('updates previousPathRef after each navigation', () => {
      const { rerender } = render(<PageViewTracker />);

      jest.clearAllMocks();

      // First navigation
      mockUsePathname.mockReturnValue('/page1');

      rerender(<PageViewTracker />);

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            from_path: '/home',
            to_path: '/page1',
          }),
        })
      );

      jest.clearAllMocks();

      // Second navigation - should use /page1 as from_path
      mockUsePathname.mockReturnValue('/page2');

      rerender(<PageViewTracker />);

      expect(mockTrackEvent).toHaveBeenCalledWith(
        expect.objectContaining({
          properties: expect.objectContaining({
            from_path: '/page1',
            to_path: '/page2',
          }),
        })
      );
    });

    it('updates navigationStartTimeRef after each navigation', () => {
      const { rerender } = render(<PageViewTracker />);

      jest.clearAllMocks();

      // First navigation after 300ms
      currentTime = 1300;
      mockUsePathname.mockReturnValue('/page1');

      rerender(<PageViewTracker />);

      // Verify first navigation has timing (300ms)
      const firstCall = mockTrackEvent.mock.calls[0][0];
      expect(firstCall.properties.navigation_time).toBe(300);

      jest.clearAllMocks();

      // Second navigation after another 400ms (total 700ms)
      currentTime = 1700;
      mockUsePathname.mockReturnValue('/page2');

      rerender(<PageViewTracker />);

      // Verify second navigation has timing (400ms from last navigation_start at 1300)
      const secondCall = mockTrackEvent.mock.calls[0][0];
      expect(secondCall.properties.navigation_time).toBe(400);
    });
  });

  describe('URL Construction', () => {
    it('constructs URL without search params', () => {
      mockUsePathname.mockReturnValue('/products');
      mockSearchParams.toString.mockReturnValue('');

      render(<PageViewTracker />);

      expect(mockTrackPageView).toHaveBeenCalledWith('/products', 'Test Page');
    });

    it('constructs URL with search params', () => {
      mockUsePathname.mockReturnValue('/products');
      mockSearchParams.toString.mockReturnValue('category=electronics&sort=price');

      render(<PageViewTracker />);

      expect(mockTrackPageView).toHaveBeenCalledWith(
        '/products?category=electronics&sort=price',
        'Test Page'
      );
    });

    it('handles empty string pathname', () => {
      mockUsePathname.mockReturnValue('');

      render(<PageViewTracker />);

      expect(mockTrackPageView).not.toHaveBeenCalled();
    });
  });

  describe('Document Title Handling', () => {
    it('uses document.title when available', () => {
      document.title = 'Custom Page Title';

      render(<PageViewTracker />);

      expect(mockTrackPageView).toHaveBeenCalledWith('/home', 'Custom Page Title');
    });

    it('handles different document titles correctly', () => {
      document.title = 'Another Page Title';

      render(<PageViewTracker />);

      expect(mockTrackPageView).toHaveBeenCalledWith('/home', 'Another Page Title');
    });
  });

  describe('Effect Dependencies', () => {
    it('triggers effect when pathname changes', () => {
      const { rerender } = render(<PageViewTracker />);

      expect(mockTrackPageView).toHaveBeenCalledTimes(1);

      mockUsePathname.mockReturnValue('/new-path');

      rerender(<PageViewTracker />);

      expect(mockTrackPageView).toHaveBeenCalledTimes(2);
    });

    it('triggers effect when search params change', () => {
      const { rerender } = render(<PageViewTracker />);

      expect(mockTrackPageView).toHaveBeenCalledTimes(1);

      // Return a new searchParams object to trigger effect
      const newSearchParams = { toString: jest.fn(() => 'new=params') };
      mockUseSearchParams.mockReturnValue(newSearchParams);

      rerender(<PageViewTracker />);

      expect(mockTrackPageView).toHaveBeenCalledTimes(2);
    });
  });
});
