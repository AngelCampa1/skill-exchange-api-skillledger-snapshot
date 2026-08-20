import { logger } from '@/utils/logger';
/**
 * API Client Utility
 * BUG-FE-002 FIX: Centralized API client that uses httpOnly cookies
 */

import { AUTH_CONFIG } from '../constants/auth';

// BUG-HIGH-004 FIX: Custom error for session expiration
export class SessionExpiredError extends Error {
  constructor(message: string = 'Session expired. Please login again.') {
    super(message);
    this.name = 'SessionExpiredError';
  }
}

// BUG-TEST-001 FIX: Custom error for CSRF token fetch failures
export class CsrfTokenError extends Error {
  constructor(message: string = 'Failed to fetch CSRF token') {
    super(message);
    this.name = 'CsrfTokenError';
  }
}

// BUG-HIGH-004 FIX: Global flag to prevent multiple redirects
let isRedirectingToLogin = false;

// BUG-HIGH-008 FIX: Shared token refresh promise to prevent race conditions
let tokenRefreshPromise: Promise<boolean> | null = null;

// CSRF token cache for efficient reuse
let cachedCsrfToken: string | null = null;
let csrfTokenPromise: Promise<string> | null = null;

// BUG-TEST-002 FIX: Timeout configuration
const TOKEN_REFRESH_TIMEOUT_MS = 10000;
const CSRF_FETCH_TIMEOUT_MS = 5000;

/**
 * Fetch and cache CSRF token from the backend
 * BUG-TEST-001 FIX: Now throws CsrfTokenError on failure
 */
async function getCsrfToken(): Promise<string> {
  if (cachedCsrfToken) {
    return cachedCsrfToken;
  }

  if (csrfTokenPromise) {
    return csrfTokenPromise;
  }

  csrfTokenPromise = (async () => {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), CSRF_FETCH_TIMEOUT_MS);

    try {
      const response = await fetch('/api/auth/csrf-token', {
        method: 'GET',
        credentials: 'include',
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      if (response.ok) {
        const data = await response.json();
        const token = data.token ?? '';
        cachedCsrfToken = token;
        return token;
      }

      logger.error('CSRF token fetch failed', { status: response.status });
      throw new CsrfTokenError(`CSRF token fetch failed with status ${response.status}`);
    } catch (error) {
      clearTimeout(timeoutId);
      if (error instanceof CsrfTokenError) throw error;
      if ((error as Error).name === 'AbortError') {
        logger.error('CSRF token fetch timed out');
        throw new CsrfTokenError('CSRF token fetch timed out');
      }
      logger.error('Failed to fetch CSRF token', { error });
      throw new CsrfTokenError(`Failed to fetch CSRF token: ${(error as Error).message}`);
    } finally {
      csrfTokenPromise = null;
    }
  })();

  // Non-null assertion safe: csrfTokenPromise was just assigned above
  return csrfTokenPromise!;
}

export function clearCsrfToken(): void {
  cachedCsrfToken = null;
}

/**
 * BUG-HIGH-008 FIX: Attempt to refresh the authentication token
 * BUG-TEST-002 FIX: Added timeout to prevent infinite hang
 */
async function attemptTokenRefresh(): Promise<boolean> {
  if (isRedirectingToLogin) return false;
  if (tokenRefreshPromise) return tokenRefreshPromise;

  tokenRefreshPromise = (async () => {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), TOKEN_REFRESH_TIMEOUT_MS);

    try {
      logger.info('Attempting token refresh');
      const refreshResponse = await fetch('/api/auth/refresh', {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: { 'Content-Type': 'application/json' },
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      if (refreshResponse.ok) {
        logger.info('Token refresh successful');
        return true;
      } else {
        logger.warn('Token refresh failed - server returned non-OK status');
        return false;
      }
    } catch (error) {
      clearTimeout(timeoutId);
      if ((error as Error).name === 'AbortError') {
        logger.error(`Token refresh timed out after ${TOKEN_REFRESH_TIMEOUT_MS}ms`);
      } else {
        logger.error('Token refresh error:', error);
      }
      return false;
    } finally {
      tokenRefreshPromise = null;
    }
  })();

  return tokenRefreshPromise;
}

/**
 * BUG-HIGH-008 FIX: Handle session expiration by redirecting to login
 * BUG-TEST-003 FIX: Preserves returnUrl so user can return after login
 */
function handleSessionExpired(): never {
  if (!isRedirectingToLogin) {
    isRedirectingToLogin = true;
    logger.warn('Session expired - redirecting to login');
    if (typeof window !== 'undefined') {
      // BUG-TEST-003 FIX: Build returnUrl from current location, handle undefined values
      const pathname = window.location.pathname || '';
      const search = window.location.search || '';
      const currentPath = pathname + search;
      // Only include returnUrl if we have a valid path
      if (currentPath && currentPath !== '/login') {
        const returnUrl = encodeURIComponent(currentPath);
        window.location.href = `/login?reason=session_expired&returnUrl=${returnUrl}`;
      } else {
        window.location.href = '/login?reason=session_expired';
      }
    }
  }
  throw new SessionExpiredError();
}

export interface ApiClientConfig {
  baseUrl?: string;
  headers?: HeadersInit;
}

export async function fetchWithAuth<T = unknown>(
  url: string,
  options: RequestInit = {},
  retryCount = 0
): Promise<T> {
  const defaultHeaders: HeadersInit = { 'Content-Type': 'application/json' };

  const method = options.method?.toUpperCase() || 'GET';
  if (method !== 'GET' && method !== 'HEAD' && method !== 'OPTIONS') {
    // BUG-TEST-001 FIX: Fail-closed — let CsrfTokenError propagate; never send
    // an unsafe request without a token.
    const csrfToken = await getCsrfToken();
    if (!csrfToken) {
      throw new CsrfTokenError('CSRF token resolved to an empty value');
    }
    defaultHeaders['X-CSRF-TOKEN'] = csrfToken;
  }

  const response = await fetch(url, {
    ...options,
    credentials: AUTH_CONFIG.CREDENTIALS,
    headers: { ...defaultHeaders, ...options.headers },
  });

  if (response.status === 401 && retryCount === 0) {
    logger.info('Received 401 Unauthorized - attempting token refresh');
    const refreshSucceeded = await attemptTokenRefresh();
    if (refreshSucceeded) {
      logger.info('Token refresh successful - retrying original request');
      return fetchWithAuth<T>(url, options, retryCount + 1);
    } else {
      handleSessionExpired();
    }
  }

  if (response.status === 401 && retryCount > 0) {
    logger.warn('Received 401 after retry - session expired');
    handleSessionExpired();
  }

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({
      message: `HTTP ${response.status}: ${response.statusText}`,
    }));
    throw new Error(errorData.message || `Request failed: ${response.status}`);
  }

  const contentType = response.headers.get('content-type');
  if (!contentType || response.status === 204) {
    return null as T;
  }

  if (contentType.includes('application/json')) {
    return response.json();
  }

  return response.text() as unknown as T;
}

export async function uploadFileWithAuth<T = unknown>(
  url: string,
  file: File,
  additionalData?: Record<string, string>
): Promise<T> {
  const formData = new FormData();
  formData.append('file', file);

  if (additionalData) {
    Object.entries(additionalData).forEach(([key, value]) => {
      formData.append(key, value);
    });
  }

  // BUG-TEST-001 FIX: Fail-closed — let CsrfTokenError propagate; never upload
  // without a token.
  const csrfToken = await getCsrfToken();
  if (!csrfToken) {
    throw new CsrfTokenError('CSRF token resolved to an empty value');
  }

  const headers: HeadersInit = { 'X-CSRF-TOKEN': csrfToken };

  const doUpload = () =>
    fetch(url, {
      method: 'POST',
      credentials: AUTH_CONFIG.CREDENTIALS,
      headers,
      body: formData,
    });

  let response = await doUpload();

  // BUG-FE-021 FIX: Handle 401 by attempting token refresh and retrying once
  if (response.status === 401) {
    logger.info('Upload received 401 - attempting token refresh');
    const refreshSucceeded = await attemptTokenRefresh();
    if (refreshSucceeded) {
      logger.info('Token refresh successful - retrying upload');
      response = await doUpload();
      if (response.status === 401) {
        handleSessionExpired();
      }
    } else {
      handleSessionExpired();
    }
  }

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({
      message: `Upload failed: ${response.status}`,
    }));
    throw new Error(errorData.message || `Upload failed: ${response.status}`);
  }

  return response.json();
}

export async function downloadFileWithAuth(url: string, fileName: string): Promise<void> {
  const doDownload = () =>
    fetch(url, {
      credentials: AUTH_CONFIG.CREDENTIALS,
    });

  let response = await doDownload();

  // BUG-FE-021 FIX: Handle 401 by attempting token refresh and retrying once
  if (response.status === 401) {
    logger.info('Download received 401 - attempting token refresh');
    const refreshSucceeded = await attemptTokenRefresh();
    if (refreshSucceeded) {
      logger.info('Token refresh successful - retrying download');
      response = await doDownload();
      if (response.status === 401) {
        handleSessionExpired();
      }
    } else {
      handleSessionExpired();
    }
  }

  if (!response.ok) {
    throw new Error(`Download failed: ${response.status}`);
  }

  const blob = await response.blob();
  const objectUrl = window.URL.createObjectURL(blob);

  try {
    const a = document.createElement('a');
    a.href = objectUrl;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  } finally {
    window.URL.revokeObjectURL(objectUrl);
  }
}

/** @deprecated Use fetchWithAuth() instead */
export function createLegacyAuthHeaders(): HeadersInit {
  logger.warn(
    'DEPRECATED: createLegacyAuthHeaders() should not be used. ' +
    'Authentication is now handled via httpOnly cookies automatically.'
  );
  return { 'Content-Type': 'application/json' };
}

export function resetRedirectFlag(): void {
  isRedirectingToLogin = false;
}

export function resetCsrfTokenCache(): void {
  cachedCsrfToken = null;
  csrfTokenPromise = null;
}
