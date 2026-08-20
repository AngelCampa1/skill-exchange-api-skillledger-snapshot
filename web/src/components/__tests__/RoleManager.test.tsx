/**
 * RoleManager Component Tests
 *
 * Week 18 - Gap Filling: Testing highest-impact untested files
 * Target: 85%+ coverage
 *
 * Tests cover:
 * - Loading state
 * - Role list display
 * - Pagination
 * - Create role modal and validation
 * - View permissions modal
 * - Delete role with confirmation
 */

import React from 'react';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RoleManager } from '../RoleManager';

// Mock AuthContext
jest.mock('../../contexts/AuthContext', () => ({
  useAuth: jest.fn(() => ({
    user: { id: 'admin-1', roles: ['Admin'] },
    isAuthenticated: true,
    isInitialized: true,
  })),
}));

// Mock data
const mockRoles = [
  {
    id: 'role-1',
    name: 'Admin',
    description: 'System administrator with full access',
    isSystemRole: true,
    isActive: true,
    priority: 100,
    permissions: [
      { id: 'perm-1', name: 'users:read', description: 'Read users', category: 'Users', isActive: true },
      { id: 'perm-2', name: 'users:write', description: 'Write users', category: 'Users', isActive: true },
    ],
    userCount: 5,
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
  },
  {
    id: 'role-2',
    name: 'Moderator',
    description: 'Content moderator',
    isSystemRole: false,
    isActive: true,
    priority: 50,
    permissions: [
      { id: 'perm-3', name: 'content:read', description: 'Read content', category: 'Content', isActive: true },
    ],
    userCount: 10,
    createdAt: '2024-01-02T00:00:00Z',
    updatedAt: '2024-01-02T00:00:00Z',
  },
  {
    id: 'role-3',
    name: 'User',
    description: 'Regular user',
    isSystemRole: false,
    isActive: true,
    priority: 10,
    permissions: [],
    userCount: 100,
    createdAt: '2024-01-03T00:00:00Z',
    updatedAt: '2024-01-03T00:00:00Z',
  },
];

const mockPermissions = {
  Users: [
    { id: 'perm-1', name: 'users:read', description: 'Read users', category: 'Users', isActive: true },
    { id: 'perm-2', name: 'users:write', description: 'Write users', category: 'Users', isActive: true },
  ],
  Content: [
    { id: 'perm-3', name: 'content:read', description: 'Read content', category: 'Content', isActive: true },
    { id: 'perm-4', name: 'content:write', description: 'Write content', category: 'Content', isActive: true },
  ],
};

describe('RoleManager', () => {
  let mockFetch: jest.Mock;

  beforeEach(() => {
    mockFetch = jest.fn();
    global.fetch = mockFetch;

    // Default mock responses
    mockFetch.mockImplementation((url: string) => {
      if (url.includes('/api/role/permissions')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockPermissions),
        });
      }
      if (url.includes('/api/role')) {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve(mockRoles),
        });
      }
      return Promise.resolve({ ok: false });
    });
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  // =========================================================================
  // Suite 1: Loading & Initial State (4 tests)
  // =========================================================================
  describe('Loading & Initial State', () => {
    test('shows loading spinner while fetching data', () => {
      // Never resolve to keep loading state
      mockFetch.mockImplementation(() => new Promise(() => {}));

      const { container } = render(<RoleManager />);

      const spinner = container.querySelector('.animate-spin');
      expect(spinner).toBeInTheDocument();
    });

    test('displays page title after loading', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText('Role Management')).toBeInTheDocument();
      });
    });

    test('displays Create Role button', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /create role/i })).toBeInTheDocument();
      });
    });

    test('displays error message when fetch fails', async () => {
      mockFetch.mockImplementation(() => Promise.resolve({ ok: false }));

      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText(/failed to fetch/i)).toBeInTheDocument();
      });
    });
  });

  // =========================================================================
  // Suite 2: Role List Display (5 tests)
  // =========================================================================
  describe('Role List Display', () => {
    test('displays role names', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText('Admin')).toBeInTheDocument();
        expect(screen.getByText('Moderator')).toBeInTheDocument();
        expect(screen.getByText('User')).toBeInTheDocument();
      });
    });

    test('displays role descriptions', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText('System administrator with full access')).toBeInTheDocument();
        expect(screen.getByText('Content moderator')).toBeInTheDocument();
      });
    });

    test('shows System Role badge for system roles', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText('System Role')).toBeInTheDocument();
      });
    });

    test('displays role statistics', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        // Each role card has these stats, so use getAllByText
        expect(screen.getAllByText('Priority:').length).toBeGreaterThan(0);
        expect(screen.getAllByText('Users:').length).toBeGreaterThan(0);
        expect(screen.getAllByText('Permissions:').length).toBeGreaterThan(0);
      });
    });

    test('shows View Permissions button for each role', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        const viewButtons = screen.getAllByRole('button', { name: /view permissions/i });
        expect(viewButtons.length).toBe(3); // 3 roles
      });
    });
  });

  // =========================================================================
  // Suite 3: Create Role Modal (6 tests)
  // =========================================================================
  describe('Create Role Modal', () => {
    test('opens modal when Create Role button clicked', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /create role/i })).toBeInTheDocument();
      });

      await userEvent.click(screen.getByRole('button', { name: /create role/i }));

      expect(screen.getByText('Create New Role')).toBeInTheDocument();
    });

    test('closes modal when Cancel clicked', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /create role/i })).toBeInTheDocument();
      });

      await userEvent.click(screen.getByRole('button', { name: /create role/i }));
      expect(screen.getByText('Create New Role')).toBeInTheDocument();

      await userEvent.click(screen.getByRole('button', { name: 'Cancel' }));

      await waitFor(() => {
        expect(screen.queryByText('Create New Role')).not.toBeInTheDocument();
      });
    });

    test('displays role name input with character counter', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /create role/i })).toBeInTheDocument();
      });

      await userEvent.click(screen.getByRole('button', { name: /create role/i }));

      expect(screen.getByPlaceholderText('Enter role name')).toBeInTheDocument();
      expect(screen.getByText('0/50 characters')).toBeInTheDocument();
    });

    test('shows validation error for empty role name', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /create role/i })).toBeInTheDocument();
      });

      await userEvent.click(screen.getByRole('button', { name: /create role/i }));

      // Try to submit empty form
      const submitButtons = screen.getAllByRole('button', { name: /create role/i });
      const submitButton = submitButtons[submitButtons.length - 1];

      // Button should be disabled when name is empty
      expect(submitButton).toBeDisabled();
    });

    test('shows validation error for invalid characters', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /create role/i })).toBeInTheDocument();
      });

      await userEvent.click(screen.getByRole('button', { name: /create role/i }));

      const nameInput = screen.getByPlaceholderText('Enter role name');
      await userEvent.type(nameInput, 'Invalid@Role#Name');

      // Click submit
      const submitButtons = screen.getAllByRole('button', { name: /create role/i });
      const submitButton = submitButtons[submitButtons.length - 1];
      await userEvent.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/can only contain letters, numbers/i)).toBeInTheDocument();
      });
    });

    test('displays permission categories and checkboxes', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /create role/i })).toBeInTheDocument();
      });

      await userEvent.click(screen.getByRole('button', { name: /create role/i }));

      await waitFor(() => {
        expect(screen.getByText('Users')).toBeInTheDocument();
        expect(screen.getByText('Content')).toBeInTheDocument();
      });
    });
  });

  // =========================================================================
  // Suite 4: View Permissions Modal (4 tests)
  // =========================================================================
  describe('View Permissions Modal', () => {
    test('opens permissions modal when View Permissions clicked', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText('Admin')).toBeInTheDocument();
      });

      const viewButtons = screen.getAllByRole('button', { name: /view permissions/i });
      await userEvent.click(viewButtons[0]);

      await waitFor(() => {
        expect(screen.getByText('Admin Permissions')).toBeInTheDocument();
      });
    });

    test('displays permission list grouped by category', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText('Admin')).toBeInTheDocument();
      });

      const viewButtons = screen.getAllByRole('button', { name: /view permissions/i });
      await userEvent.click(viewButtons[0]);

      await waitFor(() => {
        expect(screen.getByText('users:read')).toBeInTheDocument();
        expect(screen.getByText('users:write')).toBeInTheDocument();
      });
    });

    test('shows empty message for role with no permissions', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText('User')).toBeInTheDocument();
      });

      const viewButtons = screen.getAllByRole('button', { name: /view permissions/i });
      // User role is 3rd, has no permissions
      await userEvent.click(viewButtons[2]);

      await waitFor(() => {
        expect(screen.getByText(/no permissions assigned/i)).toBeInTheDocument();
      });
    });

    test('closes modal when clicking close button', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText('Admin')).toBeInTheDocument();
      });

      const viewButtons = screen.getAllByRole('button', { name: /view permissions/i });
      await userEvent.click(viewButtons[0]);

      await waitFor(() => {
        expect(screen.getByText('Admin Permissions')).toBeInTheDocument();
      });

      // Click the X button
      await userEvent.click(screen.getByRole('button', { name: '✕' }));

      await waitFor(() => {
        expect(screen.queryByText('Admin Permissions')).not.toBeInTheDocument();
      });
    });
  });

  // =========================================================================
  // Suite 5: Delete Role (4 tests)
  // =========================================================================
  describe('Delete Role', () => {
    test('shows delete button only for non-system roles', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText('Admin')).toBeInTheDocument();
      });

      // Delete buttons should exist only for non-system roles
      const deleteButtons = screen.getAllByRole('button', { name: /delete/i });
      // Admin is system role, so only Moderator and User should have delete buttons
      expect(deleteButtons.length).toBe(2);
    });

    test('opens confirmation dialog when delete clicked', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText('Moderator')).toBeInTheDocument();
      });

      const deleteButtons = screen.getAllByRole('button', { name: /delete/i });
      await userEvent.click(deleteButtons[0]);

      await waitFor(() => {
        expect(screen.getByText('Delete Role')).toBeInTheDocument();
        expect(screen.getByText(/are you sure you want to delete/i)).toBeInTheDocument();
      });
    });

    test('cancels deletion when Cancel clicked in dialog', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText('Moderator')).toBeInTheDocument();
      });

      const deleteButtons = screen.getAllByRole('button', { name: /delete/i });
      await userEvent.click(deleteButtons[0]);

      await waitFor(() => {
        expect(screen.getByText('Delete Role')).toBeInTheDocument();
      });

      // Click Cancel in dialog
      const cancelButtons = screen.getAllByRole('button', { name: 'Cancel' });
      await userEvent.click(cancelButtons[cancelButtons.length - 1]);

      await waitFor(() => {
        // Moderator should still be there
        expect(screen.getByText('Moderator')).toBeInTheDocument();
      });
    });

    test('deletes role when confirmed', async () => {
      // Setup mock for delete
      mockFetch.mockImplementation((url: string, options?: RequestInit) => {
        if (options?.method === 'DELETE') {
          return Promise.resolve({ ok: true });
        }
        if (url.includes('/api/role/permissions')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockPermissions),
          });
        }
        if (url.includes('/api/role')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockRoles),
          });
        }
        return Promise.resolve({ ok: false });
      });

      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText('Moderator')).toBeInTheDocument();
      });

      const deleteButtons = screen.getAllByRole('button', { name: /delete/i });
      await userEvent.click(deleteButtons[0]);

      await waitFor(() => {
        expect(screen.getByText('Delete Role')).toBeInTheDocument();
      });

      // Click confirm delete - look for all delete buttons and get the one in the dialog
      const allDeleteButtons = screen.getAllByRole('button', { name: /delete/i });
      // The confirm button is the last delete button (in the dialog)
      const confirmButton = allDeleteButtons[allDeleteButtons.length - 1];
      await userEvent.click(confirmButton);

      // Verify delete API was called
      await waitFor(() => {
        const deleteCalls = mockFetch.mock.calls.filter(
          (call: any[]) => call[1]?.method === 'DELETE'
        );
        expect(deleteCalls.length).toBeGreaterThan(0);
      });
    });
  });

  // =========================================================================
  // Suite 6: Pagination (4 tests)
  // =========================================================================
  describe('Pagination', () => {
    const manyRoles = Array.from({ length: 15 }, (_, i) => ({
      id: `role-${i}`,
      name: `Role ${i}`,
      description: `Description for role ${i}`,
      isSystemRole: false,
      isActive: true,
      priority: i * 10,
      permissions: [],
      userCount: i,
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    }));

    beforeEach(() => {
      mockFetch.mockImplementation((url: string) => {
        if (url.includes('/api/role/permissions')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockPermissions),
          });
        }
        if (url.includes('/api/role')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(manyRoles),
          });
        }
        return Promise.resolve({ ok: false });
      });
    });

    test('shows pagination controls when more than 9 roles', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText('Role 0')).toBeInTheDocument();
      });

      expect(screen.getByRole('button', { name: /previous/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /next/i })).toBeInTheDocument();
    });

    test('displays correct page info', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText('Role 0')).toBeInTheDocument();
      });

      expect(screen.getByText(/page 1 of 2/i)).toBeInTheDocument();
    });

    test('navigates to next page', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText('Role 0')).toBeInTheDocument();
      });

      await userEvent.click(screen.getByRole('button', { name: /next/i }));

      await waitFor(() => {
        // Page 2 should show roles 9-14
        expect(screen.getByText('Role 9')).toBeInTheDocument();
      });
    });

    test('disables Previous button on first page', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByText('Role 0')).toBeInTheDocument();
      });

      expect(screen.getByRole('button', { name: /previous/i })).toBeDisabled();
    });
  });

  // =========================================================================
  // Suite 7: Form Interaction (3 tests)
  // =========================================================================
  describe('Form Interaction', () => {
    test('updates character count as user types', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /create role/i })).toBeInTheDocument();
      });

      await userEvent.click(screen.getByRole('button', { name: /create role/i }));

      const nameInput = screen.getByPlaceholderText('Enter role name');
      await userEvent.type(nameInput, 'Test');

      expect(screen.getByText('4/50 characters')).toBeInTheDocument();
    });

    test('can select permissions in create modal', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /create role/i })).toBeInTheDocument();
      });

      await userEvent.click(screen.getByRole('button', { name: /create role/i }));

      await waitFor(() => {
        expect(screen.getByText('users:read')).toBeInTheDocument();
      });

      const checkbox = screen.getByRole('checkbox', { name: /users:read/i });
      await userEvent.click(checkbox);

      expect(checkbox).toBeChecked();
    });

    test('closes modal when clicking backdrop', async () => {
      render(<RoleManager />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /create role/i })).toBeInTheDocument();
      });

      await userEvent.click(screen.getByRole('button', { name: /create role/i }));

      expect(screen.getByText('Create New Role')).toBeInTheDocument();

      // Click on backdrop (the overlay div)
      const overlay = document.querySelector('.bg-overlay\\/80');
      if (overlay) {
        await userEvent.click(overlay);
      }

      await waitFor(() => {
        expect(screen.queryByText('Create New Role')).not.toBeInTheDocument();
      });
    });
  });
});
