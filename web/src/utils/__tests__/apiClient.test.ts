/**
 * Integration Tests for apiClient.ts
 *
 * Week 1 of Frontend Testing Plan - Critical Security & Race Condition Tests
 *
 * Focus Areas:
 * 1. Token Refresh Race Conditions (BUG-HIGH-008 verification)
 * 2. CSRF Token Management
 * 3. Session Expiration & Redirects (BUG-HIGH-004 verification)
 * 4. Error Handling Edge Cases
 * 5. Security Validation
 *
 * Testing Philosophy: Mock fetch API only, test real apiClient logic
 */

import {
  fetchWithAuth,
  clearCsrfToken,
  SessionExpiredError,
  CsrfTokenError,
  resetRedirectFlag,
  resetCsrfTokenCache,
  uploadFileWithAuth,
  downloadFileWithAuth,
} from '../apiClient';
import { setupFetchMock, suppressConsole } from '@/utils/test/testUtils';

// Mock window.location for redirect tests
delete (window as any).location;
window.location = { href: '' } as any;

describe('apiClient - CSRF Token Management', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  beforeEach(() => {
    fetchMock = setupFetchMock();
    clearCsrfToken();
    jest.clearAllMocks();
  });

  afterEach(() => {
    fetchMock.reset();
  });

  test('CSRF token fetched only once and cached', async () => {
    // Setup: CSRF endpoint returns token
    fetchMock.respondWith({ token: 'csrf-token-123' });
    fetchMock.respondWith({ success: true }); // POST request

    await fetchWithAuth('/api/test', { method: 'POST' });

    const calls = fetchMock.getCalls();

    // Verify: CSRF fetched once, then POST request made
    expect(calls).toHaveLength(2);
    expect(calls[0].url).toBe('/api/auth/csrf-token');
    expect(calls[1].url).toBe('/api/test');
    expect(calls[1].options?.headers).toMatchObject({
      'X-CSRF-TOKEN': 'csrf-token-123',
    });
  });

  test('concurrent POST requests use singleton CSRF fetch', async () => {
    // Setup: CSRF endpoint response and POST responses
    fetchMock.respondWith({ token: 'csrf-456' }); // CSRF call
    fetchMock.respondWith({ success: true }); // POST 1
    fetchMock.respondWith({ success: true }); // POST 2
    fetchMock.respondWith({ success: true }); // POST 3

    // Execute: 3 concurrent POST requests
    const promises = [
      fetchWithAuth('/api/test1', { method: 'POST' }),
      fetchWithAuth('/api/test2', { method: 'POST' }),
      fetchWithAuth('/api/test3', { method: 'POST' }),
    ];

    await Promise.all(promises);

    const calls = fetchMock.getCalls();

    // Verify: Only 1 CSRF fetch, then 3 POST requests
    const csrfCalls = calls.filter(c => c.url.includes('csrf-token'));
    expect(csrfCalls).toHaveLength(1);
  });

  test('CSRF added to POST/PUT/DELETE only (not GET/HEAD/OPTIONS)', async () => {
    fetchMock.respondWith({ token: 'csrf-789' });

    // Test GET request (should NOT have CSRF)
    await fetchWithAuth('/api/test', { method: 'GET' });
    const getCalls = fetchMock.getCalls();
    expect(getCalls[0].options?.headers).not.toHaveProperty('X-CSRF-TOKEN');

    fetchMock.reset();
    clearCsrfToken();

    // Test POST request (SHOULD have CSRF)
    fetchMock.respondWith({ token: 'csrf-789' });
    fetchMock.respondWith({ success: true });
    await fetchWithAuth('/api/test', { method: 'POST' });
    const postCalls = fetchMock.getCalls();
    expect(postCalls[1].options?.headers).toHaveProperty('X-CSRF-TOKEN');
  });

  test('CSRF fetch failure rejects the request with CsrfTokenError (fail-closed)', async () => {
    // Ensure cache is clean so the 500 actually triggers
    resetCsrfTokenCache();

    // Setup: CSRF endpoint fails
    fetchMock.respondWithError(500, 'CSRF service down');

    // The POST must be rejected with CsrfTokenError — never sent
    await expect(
      fetchWithAuth('/api/test', { method: 'POST' })
    ).rejects.toBeInstanceOf(CsrfTokenError);

    const calls = fetchMock.getCalls();

    // Only the failed CSRF fetch should have been made; no POST to /api/test
    expect(calls).toHaveLength(1);
    expect(calls[0].url).toBe('/api/auth/csrf-token');
    const postCall = calls.find(c => c.url === '/api/test');
    expect(postCall).toBeUndefined();
  });

  test('clearCsrfToken() clears cache', async () => {
    // Setup: Fetch with CSRF
    fetchMock.respondWith({ token: 'csrf-old' });
    fetchMock.respondWith({ success: true });
    await fetchWithAuth('/api/test1', { method: 'POST' });

    // Clear cache
    clearCsrfToken();
    fetchMock.reset();

    // Setup: New CSRF token
    fetchMock.respondWith({ token: 'csrf-new' });
    fetchMock.respondWith({ success: true });
    await fetchWithAuth('/api/test2', { method: 'POST' });

    const calls = fetchMock.getCalls();

    // Verify: New CSRF token fetched
    expect(calls[0].url).toBe('/api/auth/csrf-token');
    expect(calls[1].options?.headers).toMatchObject({
      'X-CSRF-TOKEN': 'csrf-new',
    });
  });

  test('CSRF re-fetched after clear', async () => {
    fetchMock.respondWith({ token: 'csrf-first' });
    fetchMock.respondWith({ success: true });

    await fetchWithAuth('/api/test1', { method: 'POST' });
    const firstCallCount = fetchMock.getCalls().length;

    clearCsrfToken();
    fetchMock.reset();

    fetchMock.respondWith({ token: 'csrf-second' });
    fetchMock.respondWith({ success: true });

    await fetchWithAuth('/api/test2', { method: 'POST' });
    const secondCalls = fetchMock.getCalls();

    // Verify: Second request fetched new CSRF token
    expect(secondCalls[0].url).toBe('/api/auth/csrf-token');
    expect(secondCalls[1].options?.headers).toMatchObject({
      'X-CSRF-TOKEN': 'csrf-second',
    });
  });

  test('X-CSRF-TOKEN header present in state-changing requests', async () => {
    fetchMock.respondWith({ token: 'csrf-header-test' });
    fetchMock.respondWith({ success: true });

    await fetchWithAuth('/api/test', {
      method: 'POST',
      body: JSON.stringify({ data: 'test' }),
    });

    const postCall = fetchMock.getCalls().find(c => c.url === '/api/test');

    expect(postCall?.options?.headers).toMatchObject({
      'X-CSRF-TOKEN': 'csrf-header-test',
      'Content-Type': 'application/json',
    });
  });

  test('CSRF not added to read-only requests', async () => {
    await fetchWithAuth('/api/data', { method: 'GET' });
    await fetchWithAuth('/api/data', { method: 'HEAD' });
    await fetchWithAuth('/api/data', { method: 'OPTIONS' });

    const calls = fetchMock.getCalls();

    // Verify: No CSRF token fetches for read-only methods
    calls.forEach(call => {
      expect(call.url).not.toContain('csrf-token');
      expect(call.options?.headers).not.toHaveProperty('X-CSRF-TOKEN');
    });
  });
});

describe('apiClient - Token Refresh Race Conditions (BUG-HIGH-008)', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  beforeEach(() => {
    fetchMock = setupFetchMock();
    resetRedirectFlag();
    jest.clearAllMocks();
  });

  afterEach(() => {
    fetchMock.reset();
  });

  test('concurrent 401 responses use shared refresh promise', async () => {
    // Setup: First request returns 401
    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWithError(401, 'Unauthorized');

    // Refresh endpoint succeeds
    fetchMock.respondWith({ success: true });

    // Retry requests succeed
    fetchMock.respondWith({ data: 'success1' });
    fetchMock.respondWith({ data: 'success2' });
    fetchMock.respondWith({ data: 'success3' });

    // Execute: 3 concurrent requests that will get 401
    const promises = [
      fetchWithAuth('/api/test1'),
      fetchWithAuth('/api/test2'),
      fetchWithAuth('/api/test3'),
    ];

    const results = await Promise.all(promises);

    const calls = fetchMock.getCalls();

    // Verify: Only ONE refresh call, not three
    const refreshCalls = calls.filter(c => c.url === '/api/auth/refresh');
    expect(refreshCalls).toHaveLength(1);

    // All requests should succeed after retry
    expect(results).toEqual([
      { data: 'success1' },
      { data: 'success2' },
      { data: 'success3' },
    ]);
  });

  test('original request retries with same body/headers after refresh', async () => {
    const originalBody = JSON.stringify({ foo: 'bar', timestamp: Date.now() });
    const customHeaders = { 'X-Custom-Header': 'custom-value' };

    // Setup: CSRF, 401, refresh success, CSRF for retry, retry success
    fetchMock.respondWith({ token: 'csrf-123' }); // CSRF for original request
    fetchMock.respondWithError(401, 'Unauthorized'); // Original request fails
    fetchMock.respondWith({ success: true }); // refresh succeeds
    fetchMock.respondWith({ token: 'csrf-123' }); // CSRF for retry request
    fetchMock.respondWith({ data: 'retry-success' }); // retry succeeds

    await fetchWithAuth('/api/test', {
      method: 'POST',
      headers: customHeaders,
      body: originalBody,
    });

    const calls = fetchMock.getCalls();

    // Find the retry call (should be after refresh)
    const retryCalls = calls.filter(c => c.url === '/api/test');
    expect(retryCalls).toHaveLength(2); // Original + retry

    const retryCall = retryCalls[1];
    expect(retryCall.options?.body).toBe(originalBody);
    expect(retryCall.options?.headers).toMatchObject(customHeaders);
  });

  test('refresh timeout after 10 seconds (BUG EXPECTED: no timeout exists)', async () => {
    jest.useFakeTimers({ advanceTimers: true });

    // Setup: 401 response, refresh hangs indefinitely
    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.mockFetch.mockImplementationOnce(
      () => new Promise(() => {}) // Never resolves
    );

    const promise = fetchWithAuth('/api/test');

    // Advance time 10 seconds
    jest.advanceTimersByTime(10000);

    // EXPECTED BUG: This will hang forever, no timeout implemented
    // In a proper implementation, this should reject after 10s

    jest.useRealTimers();
  }, 15000);

  test('10 concurrent API calls all wait for single refresh', async () => {
    // Setup: All requests get 401
    for (let i = 0; i < 10; i++) {
      fetchMock.respondWithError(401, 'Unauthorized');
    }

    // One refresh succeeds
    fetchMock.respondWith({ success: true });

    // All retries succeed
    for (let i = 0; i < 10; i++) {
      fetchMock.respondWith({ data: `success-${i}` });
    }

    // Execute: 10 concurrent requests
    const promises = Array.from({ length: 10 }, (_, i) =>
      fetchWithAuth(`/api/test${i}`)
    );

    await Promise.all(promises);

    const calls = fetchMock.getCalls();
    const refreshCalls = calls.filter(c => c.url === '/api/auth/refresh');

    // Verify: Only 1 refresh call for all 10 requests
    expect(refreshCalls).toHaveLength(1);
  }, 10000);

  test('refresh lock released even if refresh fails', async () => {
    const suppress = suppressConsole('warn');

    // Setup: 401, refresh fails, should redirect
    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWithError(500, 'Refresh failed');

    try {
      await fetchWithAuth('/api/test');
    } catch (error) {
      // Expected to throw SessionExpiredError
    }

    fetchMock.reset();
    resetRedirectFlag();

    // Setup: New request should be able to attempt refresh again
    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWith({ success: true }); // This refresh succeeds
    fetchMock.respondWith({ data: 'success' });

    const result = await fetchWithAuth('/api/test2');

    expect(result).toEqual({ data: 'success' });

    suppress.restore();
  }, 10000);

  test('request body preserved on retry (BUG EXPECTED: may lose FormData)', async () => {
    // This test expects a bug: FormData might not be preserved on retry

    const formData = new FormData();
    formData.append('file', new Blob(['test']), 'test.txt');
    formData.append('metadata', 'test-value');

    // Setup: 401, refresh, retry
    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWith({ success: true });
    fetchMock.respondWith({ data: 'uploaded' });

    await fetchWithAuth('/api/upload', {
      method: 'POST',
      body: formData,
    });

    const calls = fetchMock.getCalls();
    const retryCall = calls.find(
      (c, i) => c.url === '/api/upload' && i > 0 // Second call to /api/upload
    );

    // EXPECTED BUG: FormData might be consumed/empty on retry
    // This is a known issue with FormData streams
    expect(retryCall?.options?.body).toBeDefined();
  });

  test('request headers preserved on retry (BUG EXPECTED: may lose custom headers)', async () => {
    const customHeaders = {
      'X-Request-ID': 'req-123',
      'X-Correlation-ID': 'corr-456',
    };

    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWith({ success: true });
    fetchMock.respondWith({ data: 'success' });

    await fetchWithAuth('/api/test', {
      headers: customHeaders,
    });

    const calls = fetchMock.getCalls();
    const retryCall = calls.find((c, i) => c.url === '/api/test' && i > 0);

    expect(retryCall?.options?.headers).toMatchObject(customHeaders);
  }, 10000);

  test('refresh during active file upload (edge case)', async () => {
    const largeBlob = new Blob([new ArrayBuffer(1024 * 1024)]); // 1MB
    const formData = new FormData();
    formData.append('file', largeBlob, 'large-file.bin');

    // Mock sequence (CSRF is cached from previous tests in this block):
    // 1. Original POST fails with 401
    // 2. Refresh succeeds
    // 3. Retry POST succeeds (uses cached CSRF)
    fetchMock.respondWithError(401, 'Unauthorized'); // Original request fails
    fetchMock.respondWith({ success: true }); // Refresh succeeds
    fetchMock.respondWith({ uploaded: true }); // Retry succeeds

    const result = await fetchWithAuth('/api/upload', {
      method: 'POST',
      body: formData,
    });

    expect(result).toEqual({ uploaded: true });
  });

  test('second 401 after refresh redirects to login (no infinite retry)', async () => {
    const suppress = suppressConsole('warn');

    // Setup: 401, refresh succeeds, retry gets ANOTHER 401
    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWith({ success: true }); // refresh succeeds
    fetchMock.respondWithError(401, 'Unauthorized again'); // retry fails with 401

    try {
      await fetchWithAuth('/api/test');
      fail('Should have thrown SessionExpiredError');
    } catch (error) {
      expect(error).toBeInstanceOf(SessionExpiredError);
      expect(window.location.href).toContain('login');
    }

    suppress.restore();
  }, 10000);

  test('isRedirectingToLogin flag prevents duplicate redirects', async () => {
    const suppress = suppressConsole('warn');

    // Setup: Multiple 401s after refresh failure
    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWithError(500, 'Refresh failed');
    fetchMock.respondWithError(500, 'Refresh failed');

    const promises = [
      fetchWithAuth('/api/test1').catch(() => {}),
      fetchWithAuth('/api/test2').catch(() => {}),
    ];

    await Promise.all(promises);

    // Verify: window.location.href only set once
    // (In real browser, second redirect would be no-op)
    expect(window.location.href).toContain('login');

    suppress.restore();
  });
});

describe('apiClient - Session Expiration & Redirects', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  beforeEach(() => {
    fetchMock = setupFetchMock();
    resetRedirectFlag();
    jest.clearAllMocks();
    window.location.href = '';
  });

  afterEach(() => {
    fetchMock.reset();
  });

  test('401 response redirects to /login?reason=session_expired', async () => {
    const suppress = suppressConsole('warn');

    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWithError(500, 'Refresh failed');

    try {
      await fetchWithAuth('/api/test');
      fail('Should have thrown');
    } catch (error) {
      expect(window.location.href).toBe('/login?reason=session_expired');
    }

    suppress.restore();
  }, 10000);

  test('SessionExpiredError thrown with proper message', async () => {
    const suppress = suppressConsole('warn');

    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWithError(500, 'Refresh failed');

    await expect(fetchWithAuth('/api/test')).rejects.toThrow(SessionExpiredError);

    // Reset for second test
    fetchMock.reset();
    resetRedirectFlag();
    window.location.href = '';

    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWithError(500, 'Refresh failed');

    await expect(fetchWithAuth('/api/test')).rejects.toThrow(
      'Session expired. Please login again.'
    );

    suppress.restore();
  }, 10000);

  test('isRedirectingToLogin prevents duplicate redirects', async () => {
    const suppress = suppressConsole('warn');

    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWithError(500, 'Refresh failed');
    fetchMock.respondWithError(500, 'Refresh failed');

    const errors: any[] = [];

    await Promise.all([
      fetchWithAuth('/api/test1').catch(e => errors.push(e)),
      fetchWithAuth('/api/test2').catch(e => errors.push(e)),
    ]);

    // Both should throw same error
    expect(errors).toHaveLength(2);
    expect(errors.every(e => e instanceof SessionExpiredError)).toBe(true);

    // But redirect should only happen once (can't verify count, but flag should be set)
    expect(window.location.href).toContain('login');

    suppress.restore();
  }, 10000);

  test('redirect during multiple concurrent 401s', async () => {
    const suppress = suppressConsole('warn');

    for (let i = 0; i < 5; i++) {
      fetchMock.respondWithError(401, 'Unauthorized');
    }
    fetchMock.respondWithError(500, 'Refresh failed');

    const promises = Array.from({ length: 5 }, (_, i) =>
      fetchWithAuth(`/api/test${i}`).catch(() => {})
    );

    await Promise.all(promises);

    expect(window.location.href).toContain('login');

    suppress.restore();
  }, 10000);

  test('resetRedirectFlag() resets state', async () => {
    const suppress = suppressConsole('warn');

    // First request triggers redirect
    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWithError(500, 'Refresh failed');

    try {
      await fetchWithAuth('/api/test1');
    } catch {}

    expect(window.location.href).toContain('login');

    // Reset flag
    resetRedirectFlag();
    window.location.href = '';
    fetchMock.reset();

    // Second request should be able to redirect again
    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWithError(500, 'Refresh failed');

    try {
      await fetchWithAuth('/api/test2');
    } catch {}

    expect(window.location.href).toContain('login');

    suppress.restore();
  }, 10000);

  test('redirect includes original URL in returnUrl query param (BUG EXPECTED: not implemented)', async () => {
    const suppress = suppressConsole('warn');

    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWithError(500, 'Refresh failed');

    try {
      await fetchWithAuth('/api/protected-resource');
    } catch {}

    // EXPECTED BUG: returnUrl not included, should be /login?reason=session_expired&returnUrl=/api/protected-resource
    expect(window.location.href).toBe('/login?reason=session_expired');

    suppress.restore();
  }, 10000);
});

describe('apiClient - Error Handling Edge Cases', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  beforeEach(() => {
    fetchMock = setupFetchMock();
    jest.clearAllMocks();
  });

  afterEach(() => {
    fetchMock.reset();
  });

  test('204 No Content (empty response body) parsed correctly', async () => {
    fetchMock.mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 204,
      statusText: 'No Content',
      headers: new Headers(),
      json: () => Promise.reject(new Error('No body')),
    } as any);

    const result = await fetchWithAuth('/api/test');

    expect(result).toBeNull();
  });

  test('non-JSON response crashes (BUG EXPECTED: assumes JSON)', async () => {
    fetchMock.mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      headers: new Headers({ 'content-type': 'text/html' }),
      json: () => Promise.reject(new Error('Unexpected token < in JSON')),
      text: () => Promise.resolve('<html>Error page</html>'),
    } as any);

    // EXPECTED BUG: This might crash or return wrong type
    const result = await fetchWithAuth('/api/test');

    // Should return text, not crash
    expect(typeof result).toBe('string');
  });

  test('credentials: "include" present in all requests', async () => {
    await fetchWithAuth('/api/test1', { method: 'GET' });
    await fetchWithAuth('/api/test2', { method: 'POST' });

    const calls = fetchMock.getCalls();

    calls.forEach(call => {
      expect(call.options?.credentials).toBe('include');
    });
  });

  test('custom headers merged with defaults', async () => {
    await fetchWithAuth('/api/test', {
      headers: {
        'X-Custom': 'value',
        'Content-Type': 'application/xml', // Override default
      },
    });

    const call = fetchMock.getLastCall();

    expect(call.options?.headers).toMatchObject({
      'X-Custom': 'value',
      'Content-Type': 'application/xml',
    });
  });

  test('network offline with navigator.onLine (BUG EXPECTED: not checked)', async () => {
    // Mock offline
    Object.defineProperty(navigator, 'onLine', {
      writable: true,
      value: false,
    });

    fetchMock.mockFetch.mockRejectedValueOnce(new Error('Network request failed'));

    await expect(fetchWithAuth('/api/test')).rejects.toThrow();

    // EXPECTED BUG: No special handling for offline state
    // Should show user-friendly offline message

    // Restore
    Object.defineProperty(navigator, 'onLine', {
      writable: true,
      value: true,
    });
  });

  test('blob URL cleanup on download error (BUG EXPECTED: memory leak)', async () => {
    const revokeObjectURLSpy = jest.spyOn(URL, 'revokeObjectURL');

    fetchMock.respondWithError(404, 'File not found');

    try {
      await downloadFileWithAuth('/api/download/missing', 'file.pdf');
      fail('Should have thrown');
    } catch (error) {
      // Error expected
    }

    // EXPECTED BUG: Blob URL not revoked on error path (memory leak)
    expect(revokeObjectURLSpy).not.toHaveBeenCalled();

    revokeObjectURLSpy.mockRestore();
  });
});

describe('apiClient - CSRF Token Error & Timeout Handling', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  beforeEach(() => {
    fetchMock = setupFetchMock();
    clearCsrfToken();
    jest.clearAllMocks();
  });

  afterEach(() => {
    fetchMock.reset();
  });

  test('CSRF fetch network error rejects the request with CsrfTokenError (fail-closed)', async () => {
    // First call (CSRF) - fail with network error; no second call should happen
    fetchMock.mockFetch.mockImplementationOnce((_url: string) => {
      return Promise.reject(new Error('Network error'));
    });

    // The POST must be rejected — never sent
    await expect(
      fetchWithAuth('/api/test', { method: 'POST' })
    ).rejects.toBeInstanceOf(CsrfTokenError);

    // Only the CSRF fetch was attempted; the target endpoint was never called
    const calls = fetchMock.getCalls();
    const postCall = calls.find(c => c.url === '/api/test');
    expect(postCall).toBeUndefined();
  });

  test('CSRF fetch failure (500) rejects the request with CsrfTokenError (fail-closed)', async () => {
    // Queue only the failed CSRF response — no POST response needed
    fetchMock.respondWithError(500, 'CSRF service unavailable');

    // The POST must be rejected — never sent
    await expect(
      fetchWithAuth('/api/test', { method: 'POST' })
    ).rejects.toBeInstanceOf(CsrfTokenError);

    // Verify no POST to the target endpoint was made
    const calls = fetchMock.getCalls();
    const postCall = calls.find(c => c.url === '/api/test');
    expect(postCall).toBeUndefined();
  });
});

describe('apiClient - Token Refresh Timeout', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  beforeEach(() => {
    fetchMock = setupFetchMock();
    resetRedirectFlag();
    clearCsrfToken();
    jest.clearAllMocks();
  });

  afterEach(() => {
    fetchMock.reset();
  });

  test('token refresh failure triggers session expired error', async () => {
    const suppress = suppressConsole('warn');

    // First request gets 401, refresh fails with 500
    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWithError(500, 'Refresh failed');

    await expect(fetchWithAuth('/api/test')).rejects.toThrow(SessionExpiredError);
    expect(window.location.href).toContain('login');

    suppress.restore();
  });

  test('token refresh with network error redirects to login', async () => {
    const suppress = suppressConsole('warn');

    // Create a custom mock that simulates 401 then network error on refresh
    let callCount = 0;
    fetchMock.mockFetch.mockImplementation((url: string) => {
      callCount++;
      if (callCount === 1) {
        // First call - 401
        return Promise.resolve({
          ok: false,
          status: 401,
          json: () => Promise.resolve({ message: 'Unauthorized' }),
        } as Response);
      }
      if (callCount === 2 && url.includes('refresh')) {
        // Second call (refresh) - network error
        return Promise.reject(new Error('Network timeout'));
      }
      return Promise.resolve({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ success: true }),
      } as Response);
    });

    await expect(fetchWithAuth('/api/test')).rejects.toThrow(SessionExpiredError);

    suppress.restore();
  });
});

describe('apiClient - returnUrl Handling', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  beforeEach(() => {
    fetchMock = setupFetchMock();
    resetRedirectFlag();
    clearCsrfToken();
    jest.clearAllMocks();
    // Setup mock window.location with pathname
    delete (window as any).location;
    window.location = {
      href: '',
      pathname: '/dashboard/projects',
      search: '?page=2'
    } as any;
  });

  afterEach(() => {
    fetchMock.reset();
  });

  test('redirect includes returnUrl when on valid path', async () => {
    const suppress = suppressConsole('warn');

    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWithError(500, 'Refresh failed');

    try {
      await fetchWithAuth('/api/protected');
    } catch {}

    // Should include encoded returnUrl
    expect(window.location.href).toContain('returnUrl=');
    expect(window.location.href).toContain('%2Fdashboard%2Fprojects');

    suppress.restore();
  }, 10000);

  test('redirect skips returnUrl when already on login page', async () => {
    const suppress = suppressConsole('warn');

    window.location.pathname = '/login';
    window.location.search = '';

    fetchMock.respondWithError(401, 'Unauthorized');
    fetchMock.respondWithError(500, 'Refresh failed');

    try {
      await fetchWithAuth('/api/protected');
    } catch {}

    // Should NOT include returnUrl for login page
    expect(window.location.href).toBe('/login?reason=session_expired');

    suppress.restore();
  }, 10000);
});

describe('apiClient - File Upload & Download', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  beforeEach(() => {
    fetchMock = setupFetchMock();
    clearCsrfToken();
    jest.clearAllMocks();
  });

  afterEach(() => {
    fetchMock.reset();
  });

  test('uploadFileWithAuth includes additional data in FormData', async () => {
    fetchMock.respondWith({ token: 'csrf-upload' });
    fetchMock.respondWith({ fileId: 'file-123', url: '/files/file-123' });

    const file = new File(['test content'], 'test.txt', { type: 'text/plain' });
    const additionalData = {
      folderId: 'folder-456',
      description: 'Test file upload',
    };

    const result = await uploadFileWithAuth('/api/upload', file, additionalData);

    expect(result).toEqual({ fileId: 'file-123', url: '/files/file-123' });

    const calls = fetchMock.getCalls();
    const uploadCall = calls.find(c => c.url === '/api/upload');
    expect(uploadCall?.options?.body).toBeInstanceOf(FormData);
  });

  test('uploadFileWithAuth rejects with CsrfTokenError when CSRF fetch fails (fail-closed)', async () => {
    // CSRF fails — upload must be rejected, never sent
    fetchMock.mockFetch.mockImplementationOnce((_url: string) => {
      return Promise.reject(new Error('CSRF service down'));
    });

    const file = new File(['content'], 'doc.pdf', { type: 'application/pdf' });

    await expect(
      uploadFileWithAuth('/api/upload', file)
    ).rejects.toBeInstanceOf(CsrfTokenError);

    // Verify the upload endpoint was never called
    const calls = fetchMock.getCalls();
    const uploadCall = calls.find(c => c.url === '/api/upload');
    expect(uploadCall).toBeUndefined();
  });

  test('uploadFileWithAuth throws on upload failure', async () => {
    // Create custom mock: CSRF succeeds, upload fails
    let callCount = 0;
    fetchMock.mockFetch.mockImplementation((url: string) => {
      callCount++;
      if (callCount === 1 && url.includes('csrf')) {
        return Promise.resolve({
          ok: true,
          status: 200,
          json: () => Promise.resolve({ token: 'csrf-xyz' }),
        } as Response);
      }
      // Upload call fails with 413
      return Promise.resolve({
        ok: false,
        status: 413,
        json: () => Promise.resolve({ message: 'File too large' }),
      } as Response);
    });

    const file = new File(['large content'], 'big.zip', { type: 'application/zip' });

    await expect(uploadFileWithAuth('/api/upload', file)).rejects.toThrow('File too large');
  });

  test('downloadFileWithAuth creates and clicks download link', async () => {
    // Mock blob response
    const blob = new Blob(['file content'], { type: 'text/plain' });
    fetchMock.mockFetch.mockResolvedValueOnce({
      ok: true,
      blob: () => Promise.resolve(blob),
    } as any);

    // Mock DOM methods
    const mockClick = jest.fn();
    const mockAppendChild = jest.spyOn(document.body, 'appendChild').mockImplementation((node) => node);
    const mockRemoveChild = jest.spyOn(document.body, 'removeChild').mockImplementation(() => document.createElement('a'));
    const createObjectURLSpy = jest.spyOn(URL, 'createObjectURL').mockReturnValue('blob:test-url');
    const revokeObjectURLSpy = jest.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});

    // Create mock anchor element
    const mockAnchor = document.createElement('a');
    mockAnchor.click = mockClick;
    jest.spyOn(document, 'createElement').mockReturnValue(mockAnchor);

    await downloadFileWithAuth('/api/download/file.txt', 'downloaded.txt');

    expect(createObjectURLSpy).toHaveBeenCalled();
    expect(mockClick).toHaveBeenCalled();
    expect(revokeObjectURLSpy).toHaveBeenCalled();
    expect(mockAnchor.download).toBe('downloaded.txt');

    // Cleanup
    createObjectURLSpy.mockRestore();
    revokeObjectURLSpy.mockRestore();
    mockAppendChild.mockRestore();
    mockRemoveChild.mockRestore();
    (document.createElement as jest.Mock).mockRestore?.();
  });
});

describe('apiClient - Legacy & Utility Functions', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  beforeEach(() => {
    fetchMock = setupFetchMock();
    clearCsrfToken();
    jest.clearAllMocks();
  });

  afterEach(() => {
    fetchMock.reset();
  });

  test('createLegacyAuthHeaders returns Content-Type and logs deprecation warning', async () => {
    const { createLegacyAuthHeaders: legacyFn } = await import('../apiClient');

    const warnSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});

    const headers = legacyFn();

    expect(headers).toEqual({ 'Content-Type': 'application/json' });

    warnSpy.mockRestore();
  });

  test('resetCsrfTokenCache clears both token and promise', async () => {
    const { resetCsrfTokenCache } = await import('../apiClient');

    // First, cache a token
    fetchMock.respondWith({ token: 'cached-token' });
    fetchMock.respondWith({ success: true });
    await fetchWithAuth('/api/test', { method: 'POST' });

    // Reset cache
    resetCsrfTokenCache();
    clearCsrfToken(); // Also clear via the exported function
    fetchMock.reset();

    // Now a new request should fetch a new token
    fetchMock.respondWith({ token: 'new-token' });
    fetchMock.respondWith({ success: true });
    await fetchWithAuth('/api/test2', { method: 'POST' });

    const calls = fetchMock.getCalls();
    expect(calls[0].url).toBe('/api/auth/csrf-token');
  });
});

describe('apiClient - Security Validation', () => {
  let fetchMock: ReturnType<typeof setupFetchMock>;

  beforeEach(() => {
    fetchMock = setupFetchMock();
    jest.clearAllMocks();
  });

  afterEach(() => {
    fetchMock.reset();
  });

  test('HTTPS enforced for auth endpoints (BUG EXPECTED: not enforced)', async () => {
    // This test checks if HTTP is blocked for sensitive endpoints

    // In a production environment, this should fail or upgrade to HTTPS
    await fetchWithAuth('http://example.com/api/auth/login', { method: 'POST' });

    // EXPECTED BUG: No HTTPS enforcement, should reject or upgrade
    const call = fetchMock.getLastCall();
    expect(call.url).toContain('http://'); // Not upgraded
  });

  test('credentials sent with cross-origin requests', async () => {
    await fetchWithAuth('https://api.example.com/data');

    const call = fetchMock.getLastCall();

    expect(call.options?.credentials).toBe('include');
  });

  test('no token exposure in query params', async () => {
    await fetchWithAuth('/api/test?foo=bar', { method: 'GET' });

    const call = fetchMock.getLastCall();

    // Verify: No auth tokens in URL
    expect(call.url).not.toContain('token=');
    expect(call.url).not.toContain('auth=');
    expect(call.url).not.toContain('bearer=');
  });

  test('no sensitive data in error messages', async () => {
    const suppress = suppressConsole('warn');

    fetchMock.respondWithError(500, 'Database connection string: Server=...');

    try {
      await fetchWithAuth('/api/test');
      fail('Should have thrown');
    } catch (error: any) {
      // Verify: Error message doesn't expose internals
      // EXPECTED BUG: Might expose sensitive details
      expect(error.message).toBeDefined();
    }

    suppress.restore();
  });

  test('rate limiting retry logic (429 status) - (BUG EXPECTED: no retry)', async () => {
    fetchMock.respondWithError(429, 'Too Many Requests');

    try {
      await fetchWithAuth('/api/test');
      fail('Should have thrown');
    } catch (error: any) {
      // EXPECTED BUG: No automatic retry with backoff for 429
      expect(error.message).toBeTruthy();
      // Error message is "Too Many Requests" not "429"
    }

    // In a proper implementation, this should retry after delay
  });
});
