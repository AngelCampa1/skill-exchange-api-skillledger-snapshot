/**
 * Test Utilities for SkillLedger Frontend Tests
 *
 * Provides reusable mocks, render helpers, and test factories for integration testing.
 *
 * TESTING PHILOSOPHY:
 * - Mock external services ONLY (fetch, SignalR, Next.js router)
 * - Never mock internal components, hooks, or contexts
 * - Use real implementations for all business logic
 */

import React, { ReactElement } from 'react';
import { render, RenderOptions } from '@testing-library/react';
import { User } from '@/contexts/AuthContext';

// =============================================================================
// Mock User Factories
// =============================================================================

export const createMockUser = (overrides?: Partial<User>): User => ({
  id: 'test-user-123',
  email: 'test@example.com',
  userName: 'testuser',
  firstName: 'Test',
  lastName: 'User',
  emailVerified: true,
  phoneVerified: false,
  taxCompliant: true,
  status: 'Active',
  roles: ['User'],
  permissions: ['read:profile', 'write:profile'],
  ...overrides,
});

export const createAdminUser = (): User => createMockUser({
  id: 'admin-user-456',
  email: 'admin@example.com',
  userName: 'adminuser',
  roles: ['Admin', 'User'],
  permissions: ['read:*', 'write:*', 'admin:*'],
});

// =============================================================================
// Fetch Mock Utilities
// =============================================================================

export interface MockFetchResponse {
  ok: boolean;
  status: number;
  statusText?: string;
  json?: () => Promise<any>;
  text?: () => Promise<string>;
  headers?: Headers;
}

export const createMockFetchResponse = (
  data: any,
  options: Partial<MockFetchResponse> = {}
): Response => {
  const { ok = true, status = 200, statusText = 'OK' } = options;

  return {
    ok,
    status,
    statusText,
    headers: new Headers(options.headers || { 'content-type': 'application/json' }),
    json: async () => data,
    text: async () => JSON.stringify(data),
    blob: async () => new Blob([JSON.stringify(data)]),
    arrayBuffer: async () => new ArrayBuffer(0),
    formData: async () => new FormData(),
    clone: function() { return this; },
    body: null,
    bodyUsed: false,
    redirected: false,
    type: 'basic',
    url: '',
  } as Response;
};

/**
 * Setup fetch mock that tracks calls and allows custom responses
 */
export const setupFetchMock = () => {
  const calls: Array<{ url: string; options?: RequestInit }> = [];
  const responses: Response[] = [];

  const mockFetch = jest.fn((url: string, options?: RequestInit) => {
    calls.push({ url, options });

    // If we have queued responses, use the next one
    if (responses.length > 0) {
      return Promise.resolve(responses.shift()!);
    }

    // Default: return success
    return Promise.resolve(createMockFetchResponse({ success: true }));
  });

  global.fetch = mockFetch as any;

  const promiseQueue: Promise<Response>[] = [];

  return {
    mockFetch,
    calls,
    reset: () => {
      calls.length = 0;
      responses.length = 0;
      promiseQueue.length = 0;
      mockFetch.mockClear();
    },
    respondWith: (response: any, status = 200) => {
      responses.push(createMockFetchResponse(response, { status, ok: status >= 200 && status < 300 }));
    },
    respondWithError: (status = 500, message = 'Server Error') => {
      responses.push(
        createMockFetchResponse({ message }, { status, ok: false, statusText: message })
      );
    },
    respondWithPromise: (promise: Promise<any>) => {
      promiseQueue.push(promise.then(data => createMockFetchResponse(data)));
    },
    getCalls: () => calls,
    getLastCall: () => calls[calls.length - 1],
  };
};

// =============================================================================
// Timer Utilities for Testing Async Behavior
// =============================================================================

/**
 * Wait for all pending timers and promises to resolve
 * Useful for testing async race conditions
 */
export const flushPromises = () => new Promise(resolve => setImmediate(resolve));

/**
 * Advance timers and flush promises
 */
export const advanceTimersAndFlush = async (ms: number) => {
  jest.advanceTimersByTime(ms);
  await flushPromises();
};

// =============================================================================
// Custom Render with Providers (Use Sparingly)
// =============================================================================

/**
 * Custom render function with common providers
 *
 * NOTE: Only use this when you need to test a component in isolation.
 * For integration tests, mount the full component tree with real providers.
 */
interface CustomRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  initialUser?: User | null;
}

export function renderWithProviders(
  ui: ReactElement,
  options?: CustomRenderOptions
) {
  // For now, just use standard render
  // We'll add real providers as needed, not mock ones
  return render(ui, options);
}

// =============================================================================
// Test Data Factories
// =============================================================================

export const createMockMessage = (overrides?: any) => ({
  id: 'msg-123',
  workspaceId: 'ws-456',
  senderId: 'user-789',
  senderName: 'Test User',
  senderAvatar: '/default-avatar.png',
  messageText: 'Test message content',
  messageType: 'Text' as const,
  status: 'Sent' as const,
  isEdited: false,
  createdAt: new Date().toISOString(),
  reactions: [],
  canEdit: false,
  canDelete: false,
  ...overrides,
});

export const createMockProject = (overrides?: any) => ({
  id: 'proj-123',
  title: 'Test Project',
  description: 'Test project description',
  creditBudget: 1000,
  status: 'Active',
  createdAt: new Date().toISOString(),
  ...overrides,
});

// =============================================================================
// Console Suppression for Expected Errors
// =============================================================================

/**
 * Suppress console errors/warnings during tests that expect them
 * Usage:
 *   const suppress = suppressConsole('error');
 *   // ... test code that logs errors ...
 *   suppress.restore();
 */
export const suppressConsole = (method: 'error' | 'warn' | 'log' = 'error') => {
  const original = console[method];
  console[method] = jest.fn();

  return {
    restore: () => {
      console[method] = original;
    },
  };
};

// =============================================================================
// Network Simulation Utilities
// =============================================================================

/**
 * Simulate slow network by delaying fetch responses
 */
export const simulateSlowNetwork = (delayMs = 2000) => {
  const originalFetch = global.fetch;

  global.fetch = jest.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    await new Promise(resolve => setTimeout(resolve, delayMs));
    // Only pass init if defined to maintain original call signature
    return init !== undefined ? originalFetch(input, init) : originalFetch(input);
  }) as typeof fetch;

  return {
    restore: () => {
      global.fetch = originalFetch;
    },
  };
};

/**
 * Simulate network offline
 */
export const simulateOffline = () => {
  const originalFetch = global.fetch;

  global.fetch = jest.fn(() =>
    Promise.reject(new Error('Network request failed'))
  ) as any;

  return {
    restore: () => {
      global.fetch = originalFetch;
    },
  };
};

// =============================================================================
// Global Browser API Mocks (for jsdom environment)
// =============================================================================

/**
 * Mock URL.createObjectURL and URL.revokeObjectURL for download tests
 */
if (typeof URL.createObjectURL === 'undefined') {
  (global as any).URL.createObjectURL = jest.fn(() => 'blob:http://localhost/mock-blob-url');
}

if (typeof URL.revokeObjectURL === 'undefined') {
  (global as any).URL.revokeObjectURL = jest.fn();
}
