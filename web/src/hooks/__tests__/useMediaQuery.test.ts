/**
 * useMediaQuery.ts Tests
 *
 * Tests responsive media query hook with SSR handling and breakpoint utilities.
 * Focus: matchMedia integration, SSR safety, breakpoint exports, convenience hooks.
 *
 * Coverage Target: 85%+ (87 lines)
 * Test Count: 10 tests
 */

import { renderHook, act } from '@testing-library/react';
import {
  useMediaQuery,
  breakpoints,
  useIsMobile,
  useIsTablet,
  useIsDesktop,
  useIsLandscape,
  useIsPortrait,
  useIsRetina,
} from '../useMediaQuery';

describe('useMediaQuery - Media Query Matching', () => {
  let mockMatchMedia: jest.Mock;

  beforeEach(() => {
    // Mock window.matchMedia
    mockMatchMedia = jest.fn();
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: mockMatchMedia,
    });
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should return true when media query matches', () => {
    const mockMediaQueryList = {
      matches: true,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn(),
    };

    mockMatchMedia.mockReturnValue(mockMediaQueryList);

    const { result } = renderHook(() => useMediaQuery('(min-width: 768px)'));

    expect(result.current).toBe(true);
    expect(mockMatchMedia).toHaveBeenCalledWith('(min-width: 768px)');
  });

  it('should return false when media query does not match', () => {
    const mockMediaQueryList = {
      matches: false,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn(),
    };

    mockMatchMedia.mockReturnValue(mockMediaQueryList);

    const { result } = renderHook(() => useMediaQuery('(max-width: 640px)'));

    expect(result.current).toBe(false);
  });

  it('should update when media query changes', () => {
    let changeHandler: ((event: MediaQueryListEvent) => void) | null = null;

    const mockMediaQueryList = {
      matches: false,
      addEventListener: jest.fn((event, handler) => {
        if (event === 'change') {
          changeHandler = handler as (event: MediaQueryListEvent) => void;
        }
      }),
      removeEventListener: jest.fn(),
    };

    mockMatchMedia.mockReturnValue(mockMediaQueryList);

    const { result } = renderHook(() => useMediaQuery('(min-width: 768px)'));

    // Initial value should be false
    expect(result.current).toBe(false);

    // Simulate media query change
    act(() => {
      if (changeHandler) {
        changeHandler({ matches: true } as MediaQueryListEvent);
      }
    });

    // Value should now be true
    expect(result.current).toBe(true);
  });

  it('should cleanup event listener on unmount', () => {
    const mockRemoveEventListener = jest.fn();
    const mockMediaQueryList = {
      matches: false,
      addEventListener: jest.fn(),
      removeEventListener: mockRemoveEventListener,
    };

    mockMatchMedia.mockReturnValue(mockMediaQueryList);

    const { unmount } = renderHook(() => useMediaQuery('(min-width: 768px)'));

    unmount();

    expect(mockRemoveEventListener).toHaveBeenCalledWith('change', expect.any(Function));
  });

  it('should handle older browsers with addListener/removeListener fallback', () => {
    const mockRemoveListener = jest.fn();
    const mockMediaQueryList = {
      matches: true,
      addListener: jest.fn(),
      removeListener: mockRemoveListener,
      // No addEventListener/removeEventListener
    };

    mockMatchMedia.mockReturnValue(mockMediaQueryList);

    const { unmount } = renderHook(() => useMediaQuery('(min-width: 1024px)'));

    expect(mockMediaQueryList.addListener).toHaveBeenCalledWith(expect.any(Function));

    unmount();

    expect(mockRemoveListener).toHaveBeenCalledWith(expect.any(Function));
  });
});

describe('useMediaQuery - Convenience Hooks', () => {
  let mockMatchMedia: jest.Mock;

  beforeEach(() => {
    mockMatchMedia = jest.fn();
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: mockMatchMedia,
    });
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should useIsMobile hook with correct breakpoint', () => {
    const mockMediaQueryList = {
      matches: true,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn(),
    };

    mockMatchMedia.mockReturnValue(mockMediaQueryList);

    const { result } = renderHook(() => useIsMobile());

    expect(result.current).toBe(true);
    expect(mockMatchMedia).toHaveBeenCalledWith(breakpoints.mobile);
  });

  it('should useIsTablet hook with correct breakpoint', () => {
    const mockMediaQueryList = {
      matches: false,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn(),
    };

    mockMatchMedia.mockReturnValue(mockMediaQueryList);

    const { result } = renderHook(() => useIsTablet());

    expect(result.current).toBe(false);
    expect(mockMatchMedia).toHaveBeenCalledWith(breakpoints.tablet);
  });

  it('should useIsDesktop hook with correct breakpoint', () => {
    const mockMediaQueryList = {
      matches: true,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn(),
    };

    mockMatchMedia.mockReturnValue(mockMediaQueryList);

    const { result } = renderHook(() => useIsDesktop());

    expect(result.current).toBe(true);
    expect(mockMatchMedia).toHaveBeenCalledWith(breakpoints.desktop);
  });

  it('should useIsLandscape hook with correct breakpoint', () => {
    const mockMediaQueryList = {
      matches: false,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn(),
    };

    mockMatchMedia.mockReturnValue(mockMediaQueryList);

    const { result } = renderHook(() => useIsLandscape());

    expect(result.current).toBe(false);
    expect(mockMatchMedia).toHaveBeenCalledWith(breakpoints.landscape);
  });

  it('should useIsPortrait hook with correct breakpoint', () => {
    const mockMediaQueryList = {
      matches: true,
      addEventListener: jest.fn(),
      removeEventListener: jest.fn(),
    };

    mockMatchMedia.mockReturnValue(mockMediaQueryList);

    const { result } = renderHook(() => useIsPortrait());

    expect(result.current).toBe(true);
    expect(mockMatchMedia).toHaveBeenCalledWith(breakpoints.portrait);
  });
});
