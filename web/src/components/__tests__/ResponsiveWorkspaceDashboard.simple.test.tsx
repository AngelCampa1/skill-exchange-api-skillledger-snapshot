import React from 'react';
import { render, screen, act } from '@testing-library/react';
import { ResponsiveWorkspaceDashboard } from '../workspace/ResponsiveWorkspaceDashboard';

// Mock fetch globally
const mockFetch = jest.fn();
global.fetch = mockFetch;

// Mock window.matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: jest.fn().mockImplementation((query) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: jest.fn(),
    removeListener: jest.fn(),
    addEventListener: jest.fn(),
    removeEventListener: jest.fn(),
    dispatchEvent: jest.fn(),
  })),
});

describe('ResponsiveWorkspaceDashboard', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => ({
        id: 'workspace-1',
        name: 'Test Workspace',
        description: 'A test workspace',
        participants: [],
        messages: [],
        files: []
      }),
    });
  });

  it('renders without crashing', async () => {
    await act(async () => {
      render(<ResponsiveWorkspaceDashboard workspaceId="workspace-1" currentUserId="user-1" isClient={true} />);
    });

    // Wait for any async operations to complete
    await act(async () => {
      await new Promise(resolve => setTimeout(resolve, 100));
    });

    // Check if the workspace data is rendered (workspace name might be in title)
    expect(document.querySelector('h1')).toBeInTheDocument();
  });

  it('shows loading state initially', async () => {
    // BUG-LOW-002 FIX: Use unknown for mock fetch responses (tests don't need full Response object)
    let resolvePromise: (value: unknown) => void;
    const loadingPromise = new Promise<unknown>(resolve => {
      resolvePromise = resolve;
    });

    mockFetch.mockReturnValueOnce(loadingPromise as Promise<Response>);

    await act(async () => {
      render(<ResponsiveWorkspaceDashboard workspaceId="workspace-1" currentUserId="user-1" isClient={true} />);
    });

    expect(screen.getByText(/loading/i)).toBeInTheDocument();

    // Clean up
    await act(async () => {
      resolvePromise!({
        ok: true,
        json: async () => ({ id: 'workspace-1', name: 'Test', participants: [], messages: [], files: [] })
      });
    });
  });
});