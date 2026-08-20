/**
 * Global Type Augmentation
 *
 * Extends the Window interface with analytics tracking functions
 * for Google Analytics 4 and Microsoft Clarity.
 */

declare global {
  interface Window {
    /**
     * Google Analytics gtag function
     * @see https://developers.google.com/analytics/devguides/collection/gtagjs
     */
    gtag: (
      command: 'config' | 'event' | 'set' | 'consent' | 'js',
      targetIdOrEventName?: string | Date,
      config?: Record<string, unknown>
    ) => void

    /**
     * Google Analytics dataLayer
     */
    dataLayer: Array<unknown>

    /**
     * Microsoft Clarity function
     * @see https://docs.microsoft.com/en-us/clarity/setup-and-installation/clarity-api
     */
    clarity: (command: 'set' | 'identify' | 'upgrade' | 'consent' | 'event', ...args: unknown[]) => void

    /**
     * Analytics events array for testing purposes
     * Captures events in test environment
     */
    analyticsEvents?: Array<unknown>
  }
}

export {}
