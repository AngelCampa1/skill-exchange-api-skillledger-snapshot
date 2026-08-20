/**
 * Authentication configuration constants
 *
 * BUG-FE-002 FIX: Authentication via httpOnly cookies only
 * =========================================================
 * This application uses httpOnly cookies for authentication, NOT localStorage.
 * Tokens are automatically included in requests via credentials: 'include'.
 *
 * DO NOT store tokens in localStorage as it's vulnerable to XSS attacks.
 */

export const AUTH_CONFIG = {
  /**
   * Cookie name (read-only, set by backend)
   * The backend sets this httpOnly cookie on successful authentication
   */
  COOKIE_NAME: '.SkillLedger.Auth',

  /**
   * API request configuration
   * Always include credentials to send httpOnly cookies
   */
  CREDENTIALS: 'include' as RequestCredentials,

  /**
   * Session timeout (15 minutes)
   */
  SESSION_TIMEOUT_MS: 15 * 60 * 1000,

  /**
   * Token refresh interval (13 minutes - before expiry)
   */
  REFRESH_INTERVAL_MS: 13 * 60 * 1000,
} as const;

/**
 * @deprecated DO NOT USE localStorage for tokens
 * @security Tokens must be stored in httpOnly cookies only
 *
 * BAD (vulnerable to XSS):
 * ❌ localStorage.getItem('token')
 * ❌ localStorage.setItem('token', token)
 *
 * GOOD (secure, httpOnly cookies):
 * ✅ fetch(url, { credentials: 'include' })
 */
export const DEPRECATED_DO_NOT_USE_LOCALSTORAGE_FOR_TOKENS = undefined;
