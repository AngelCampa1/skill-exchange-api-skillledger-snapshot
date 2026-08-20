import { useState, useEffect } from 'react';

/**
 * Custom hook to handle media query matching
 * @param query - CSS media query string (e.g., '(max-width: 768px)')
 * @returns boolean indicating if the media query matches
 */
export const useMediaQuery = (query: string): boolean => {
  const [matches, setMatches] = useState<boolean>(false);

  useEffect(() => {
    const mediaQuery = window.matchMedia(query);

    // Set initial value after mount (prevents SSR hydration mismatch)
    setMatches(mediaQuery.matches);

    // Create event listener
    const handleChange = (event: MediaQueryListEvent) => {
      setMatches(event.matches);
    };

    // Add listener
    if (mediaQuery.addEventListener) {
      mediaQuery.addEventListener('change', handleChange);
    } else {
      // Fallback for older browsers
      mediaQuery.addListener(handleChange);
    }

    // Cleanup
    return () => {
      if (mediaQuery.removeEventListener) {
        mediaQuery.removeEventListener('change', handleChange);
      } else {
        // Fallback for older browsers
        mediaQuery.removeListener(handleChange);
      }
    };
  }, [query]);

  return matches;
};

/**
 * Common breakpoints for convenience
 */
export const breakpoints = {
  sm: '(min-width: 640px)',
  md: '(min-width: 768px)',
  lg: '(min-width: 1024px)',
  xl: '(min-width: 1280px)',
  '2xl': '(min-width: 1536px)',
  
  // Max width breakpoints
  maxSm: '(max-width: 639px)',
  maxMd: '(max-width: 767px)',
  maxLg: '(max-width: 1023px)',
  maxXl: '(max-width: 1279px)',
  max2xl: '(max-width: 1535px)',
  
  // Common device breakpoints
  mobile: '(max-width: 768px)',
  tablet: '(min-width: 769px) and (max-width: 1023px)',
  desktop: '(min-width: 1024px)',
  
  // Orientation
  landscape: '(orientation: landscape)',
  portrait: '(orientation: portrait)',
  
  // High DPI displays
  retina: '(-webkit-min-device-pixel-ratio: 2), (min-resolution: 2dppx)',
};

/**
 * Hook variants for common breakpoints
 */
export const useIsMobile = () => useMediaQuery(breakpoints.mobile);
export const useIsTablet = () => useMediaQuery(breakpoints.tablet);
export const useIsDesktop = () => useMediaQuery(breakpoints.desktop);
export const useIsLandscape = () => useMediaQuery(breakpoints.landscape);
export const useIsPortrait = () => useMediaQuery(breakpoints.portrait);
export const useIsRetina = () => useMediaQuery(breakpoints.retina);