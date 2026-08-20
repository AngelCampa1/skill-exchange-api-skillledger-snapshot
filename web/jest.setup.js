import '@testing-library/jest-dom'
import 'whatwg-fetch' // Polyfill for Response, Request, Headers, fetch

// Mock Headers class if not available
if (typeof global.Headers === 'undefined') {
  global.Headers = class Headers {
    constructor(init) {}
    append(name, value) {}
    delete(name) {}
    get(name) { return null }
    has(name) { return false }
    set(name, value) {}
    entries() { return [] }
    keys() { return [] }
    values() { return [] }
    forEach(callbackfn) {}
  }
}

// Configure React Testing Library to properly handle async state updates
global.IS_REACT_ACT_ENVIRONMENT = true

// Suppress specific react-hook-form act() warnings that come from library internals
const originalError = console.error
console.error = (...args) => {
  // Debug: Log the first argument to see the actual warning message
  // console.log('Warning args[0]:', JSON.stringify(args[0]))

  // Suppress common React testing warnings that don't indicate actual bugs
  if (
    typeof args[0] === 'string' && (
      args[0].includes('Warning: An update to') && args[0].includes('inside a test was not wrapped in act') ||
      args[0].includes('A component is changing an uncontrolled input to be controlled') ||
      args[0].includes('Token validation failed') ||
      args[0].includes('Failed to load categories')
    )
  ) {
    return
  }
  originalError.call(console, ...args)
}

// Mock HTMLFormElement.requestSubmit for jsdom
window.HTMLFormElement.prototype.requestSubmit = function(submitter) {
  if (submitter) {
    submitter.click()
  } else {
    this.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
  }
}

// Mock SignalR
jest.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: jest.fn().mockImplementation(() => ({
    withUrl: jest.fn().mockReturnThis(),
    withAutomaticReconnect: jest.fn().mockReturnThis(),
    build: jest.fn().mockReturnValue({
      start: jest.fn().mockResolvedValue(void 0),
      stop: jest.fn().mockResolvedValue(void 0),
      invoke: jest.fn().mockResolvedValue(void 0),
      on: jest.fn(),
      off: jest.fn(),
      state: 'Connected'
    })
  })),
  HubConnectionState: {
    Disconnected: 'Disconnected',
    Connecting: 'Connecting', 
    Connected: 'Connected',
    Disconnecting: 'Disconnecting',
    Reconnecting: 'Reconnecting'
  }
}));

// Mock localStorage
const localStorageMock = {
  getItem: jest.fn(() => 'mock-token'),
  setItem: jest.fn(),
  removeItem: jest.fn(),
  clear: jest.fn(),
};
global.localStorage = localStorageMock;

// Mock window.Notification
global.Notification = jest.fn().mockImplementation(() => ({
  close: jest.fn()
}));
global.Notification.permission = 'granted';
global.Notification.requestPermission = jest.fn().mockResolvedValue('granted');

// Mock window.alert and window.confirm for jsdom
window.alert = jest.fn();
window.confirm = jest.fn(() => true);

// Mock window.location for jsdom (doesn't support navigation)
// This prevents "Not implemented: navigation" errors when testing logout/redirect
const mockLocation = {
  ...window.location,
  href: window.location.href,
  assign: jest.fn(),
  replace: jest.fn(),
  reload: jest.fn(),
};
Object.defineProperty(window, 'location', {
  writable: true,
  value: mockLocation,
});


// Mock ResizeObserver
global.ResizeObserver = jest.fn().mockImplementation(() => ({
  observe: jest.fn(),
  unobserve: jest.fn(),
  disconnect: jest.fn(),
}));

// Mock IntersectionObserver
global.IntersectionObserver = jest.fn().mockImplementation(() => ({
  observe: jest.fn(),
  unobserve: jest.fn(),
  disconnect: jest.fn(),
}));

// Mock Next.js router
jest.mock('next/navigation', () => ({
  useRouter() {
    return {
      push: jest.fn(),
      replace: jest.fn(),
      prefetch: jest.fn(),
      back: jest.fn(),
      forward: jest.fn(),
      refresh: jest.fn(),
    }
  },
  useSearchParams() {
    return new URLSearchParams()
  },
  usePathname() {
    return '/'
  },
}))

// Mock fetch globally for API calls with better endpoint handling
const mockFetch = jest.fn((url, options) => {
  // Default response
  const defaultResponse = {
    ok: true,
    status: 200,
    json: () => Promise.resolve({}),
    text: () => Promise.resolve(''),
    headers: {},
    redirected: false,
    statusText: 'OK',
    type: 'basic',
    url: url,
    clone: jest.fn(),
    body: null,
    bodyUsed: false,
    arrayBuffer: () => Promise.resolve(new ArrayBuffer(0)),
    blob: () => Promise.resolve(new Blob()),
    formData: () => Promise.resolve(new FormData()),
  }

  // Handle specific endpoints
  if (url.includes('/api/auth/me')) {
    return Promise.resolve({
      ...defaultResponse,
      json: () => Promise.resolve({
        success: true,
        user: {
          id: 'test-user-id',
          email: 'test@example.com',
          userName: 'testuser',
          emailVerified: true,
          phoneVerified: false,
          taxCompliant: true,
          status: 'Active',
          roles: ['User'],
          permissions: ['read:own']
        }
      })
    })
  }

  if (url.includes('/api/auth/csrf-token')) {
    return Promise.resolve({
      ...defaultResponse,
      json: () => Promise.resolve({
        token: 'mock-csrf-token'
      })
    })
  }

  if (url.includes('/api/categories') || url.includes('/api/skills/categories') || url.includes('/api/skill/categories')) {
    return Promise.resolve({
      ...defaultResponse,
      json: () => Promise.resolve([
        { id: '1', name: 'Programming', skillCount: 10 },
        { id: '2', name: 'Design', skillCount: 5 },
        { id: '3', name: 'Marketing', skillCount: 8 }
      ])
    })
  }

  if (url.includes('/api/skill')) {
    return Promise.resolve({
      ...defaultResponse,
      json: () => Promise.resolve([
        { id: '1', name: 'JavaScript', category: 'Programming', isSystemManaged: false, isActive: true, createdAt: '2024-01-01' },
        { id: '2', name: 'React', category: 'Programming', isSystemManaged: false, isActive: true, createdAt: '2024-01-01' },
        { id: '3', name: 'UI Design', category: 'Design', isSystemManaged: false, isActive: true, createdAt: '2024-01-01' },
        { id: '4', name: 'SEO', category: 'Marketing', isSystemManaged: false, isActive: true, createdAt: '2024-01-01' }
      ])
    })
  }

  // Return default response for all other requests
  return Promise.resolve(defaultResponse)
})

global.fetch = mockFetch

// Setup test environment
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: jest.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: jest.fn(),
    removeListener: jest.fn(),
    addEventListener: jest.fn(),
    removeEventListener: jest.fn(),
    dispatchEvent: jest.fn(),
  })),
})