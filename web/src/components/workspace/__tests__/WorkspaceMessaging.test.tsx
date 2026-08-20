/**
 * WorkspaceMessaging.tsx Tests
 *
 * Tests for workspace messaging integration component.
 * Coverage Target: 80%+
 */

import React from 'react';
import { render, screen, waitFor, act } from '@testing-library/react';
import '@testing-library/jest-dom';
import { WorkspaceMessaging, ExampleWorkspacePage } from '../WorkspaceMessaging';

// Mock dependencies
jest.mock('../../messaging/MessageCenter', () => ({
  MessageCenter: ({ workspaceId, currentUserId, workspaceTitle, participants }: any) => (
    <div data-testid="message-center">
      <span data-testid="workspace-id">{workspaceId}</span>
      <span data-testid="current-user-id">{currentUserId}</span>
      <span data-testid="workspace-title">{workspaceTitle}</span>
      <span data-testid="participants-count">{participants?.length || 0}</span>
    </div>
  ),
}));

const mockAddNotification = jest.fn();
const mockDismissNotification = jest.fn();
const mockClearAllNotifications = jest.fn();
const mockRequestNotificationPermission = jest.fn();

let capturedOnNotificationClick: ((notification: any) => void) | null = null;

jest.mock('../../messaging/MessageNotifications', () => ({
  MessageNotifications: ({ notifications, onNotificationClick, onNotificationDismiss, onNotificationClearAll }: any) => {
    capturedOnNotificationClick = onNotificationClick;
    return (
      <div data-testid="message-notifications">
        <span data-testid="notifications-count">{notifications?.length || 0}</span>
        <button
          data-testid="notification-click-btn"
          onClick={() => onNotificationClick({ message: { id: 'msg-1' } })}
        >
          Click Notification
        </button>
      </div>
    );
  },
  useMessageNotifications: () => ({
    notifications: [],
    addNotification: mockAddNotification,
    dismissNotification: mockDismissNotification,
    clearAllNotifications: mockClearAllNotifications,
    requestNotificationPermission: mockRequestNotificationPermission,
  }),
}));

const mockSignalROn = jest.fn();
const mockSignalROff = jest.fn();

jest.mock('../../../services/signalRService', () => ({
  signalRService: {
    on: (...args: any[]) => mockSignalROn(...args),
    off: (...args: any[]) => mockSignalROff(...args),
  },
}));

jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
    debug: jest.fn(),
  },
}));

describe('WorkspaceMessaging', () => {
  const defaultProps = {
    workspaceId: 'ws-123',
    currentUserId: 'user-1',
    workspaceTitle: 'Test Workspace',
  };

  const mockParticipants = [
    { id: 'user-1', name: 'Alice', avatar: '/avatar1.png', isOnline: true },
    { id: 'user-2', name: 'Bob', avatar: '/avatar2.png', isOnline: false },
  ];

  beforeEach(() => {
    jest.clearAllMocks();
    global.fetch = jest.fn();
  });

  describe('Loading State', () => {
    it('shows loading spinner initially', () => {
      (global.fetch as jest.Mock).mockImplementation(() => new Promise(() => {})); // Never resolves

      const { container } = render(<WorkspaceMessaging {...defaultProps} />);

      expect(screen.getByText('Loading workspace...')).toBeInTheDocument();
      expect(container.querySelector('.animate-spin')).toBeInTheDocument();
    });
  });

  describe('Success State', () => {
    it('renders MessageCenter with participants after successful load', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ participants: mockParticipants }),
      });

      render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-center')).toBeInTheDocument();
      });

      expect(screen.getByTestId('workspace-id')).toHaveTextContent('ws-123');
      expect(screen.getByTestId('current-user-id')).toHaveTextContent('user-1');
      expect(screen.getByTestId('workspace-title')).toHaveTextContent('Test Workspace');
      expect(screen.getByTestId('participants-count')).toHaveTextContent('2');
    });

    it('renders MessageNotifications component', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ participants: mockParticipants }),
      });

      render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-notifications')).toBeInTheDocument();
      });
    });

    it('handles empty participants array', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ participants: [] }),
      });

      render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('participants-count')).toHaveTextContent('0');
      });
    });

    it('handles missing participants property', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({}), // No participants property
      });

      render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('participants-count')).toHaveTextContent('0');
      });
    });
  });

  describe('Error State', () => {
    it('shows error message and uses fallback data on API failure', async () => {
      (global.fetch as jest.Mock).mockRejectedValue(new Error('Network error'));

      render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByText('Network error')).toBeInTheDocument();
        expect(screen.getByText('Using demo data for messaging interface')).toBeInTheDocument();
      });
    });

    it('shows error message for non-ok response', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: false,
        status: 500,
      });

      render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByText('Failed to load participants')).toBeInTheDocument();
      });
    });

    it('handles non-Error objects in catch block', async () => {
      (global.fetch as jest.Mock).mockRejectedValue('String error');

      render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByText('Failed to load participants')).toBeInTheDocument();
      });
    });
  });

  describe('SignalR Integration', () => {
    beforeEach(() => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ participants: mockParticipants }),
      });
    });

    it('registers SignalR event listener on mount', async () => {
      render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(mockSignalROn).toHaveBeenCalledWith('MessageReceived', expect.any(Function));
      });
    });

    it('unregisters SignalR event listener on unmount', async () => {
      const { unmount } = render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-center')).toBeInTheDocument();
      });

      unmount();

      expect(mockSignalROff).toHaveBeenCalledWith('MessageReceived', expect.any(Function));
    });

    it('requests notification permission on mount', async () => {
      render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(mockRequestNotificationPermission).toHaveBeenCalled();
      });
    });

    it('adds notification for messages from other users', async () => {
      render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(mockSignalROn).toHaveBeenCalled();
      });

      // Get the registered handler
      const handler = mockSignalROn.mock.calls[0][1];

      // Simulate message from another user
      const message = {
        id: 'msg-1',
        senderId: 'user-2', // Different from currentUserId
        messageText: 'Hello!',
      };

      act(() => {
        handler(message);
      });

      expect(mockAddNotification).toHaveBeenCalledWith(message);
    });

    it('does not add notification for messages from current user', async () => {
      render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(mockSignalROn).toHaveBeenCalled();
      });

      const handler = mockSignalROn.mock.calls[0][1];

      // Simulate message from current user
      const message = {
        id: 'msg-1',
        senderId: 'user-1', // Same as currentUserId
        messageText: 'My message',
      };

      act(() => {
        handler(message);
      });

      expect(mockAddNotification).not.toHaveBeenCalled();
    });

    it('handles invalid message objects gracefully', async () => {
      const { logger } = require('@/utils/logger');

      render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(mockSignalROn).toHaveBeenCalled();
      });

      const handler = mockSignalROn.mock.calls[0][1];

      // Test with null
      act(() => {
        handler(null);
      });
      expect(logger.error).toHaveBeenCalled();
      expect(mockAddNotification).not.toHaveBeenCalled();

      jest.clearAllMocks();

      // Test with non-object
      act(() => {
        handler('invalid');
      });
      expect(logger.error).toHaveBeenCalled();
      expect(mockAddNotification).not.toHaveBeenCalled();
    });

    it('handles message with missing required fields', async () => {
      const { logger } = require('@/utils/logger');

      render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(mockSignalROn).toHaveBeenCalled();
      });

      const handler = mockSignalROn.mock.calls[0][1];

      // Test with missing id
      act(() => {
        handler({ senderId: 'user-2' });
      });
      expect(logger.error).toHaveBeenCalled();
      expect(mockAddNotification).not.toHaveBeenCalled();

      jest.clearAllMocks();

      // Test with missing senderId
      act(() => {
        handler({ id: 'msg-1' });
      });
      expect(logger.error).toHaveBeenCalled();
      expect(mockAddNotification).not.toHaveBeenCalled();
    });

    it('handles notification click', async () => {
      const { logger } = require('@/utils/logger');

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ participants: [] }),
      });

      render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-notifications')).toBeInTheDocument();
      });

      // Click the notification
      const notificationBtn = screen.getByTestId('notification-click-btn');
      act(() => {
        notificationBtn.click();
      });

      expect(logger.debug).toHaveBeenCalledWith('Navigate to message', { messageId: 'msg-1' });
    });
  });

  describe('Props', () => {
    it('applies custom className', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ participants: mockParticipants }),
      });

      const { container } = render(
        <WorkspaceMessaging {...defaultProps} className="custom-class" />
      );

      await waitFor(() => {
        expect(screen.getByTestId('message-center')).toBeInTheDocument();
      });

      expect(container.firstChild).toHaveClass('custom-class');
    });

    it('uses empty string for className when not provided', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ participants: mockParticipants }),
      });

      const { container } = render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-center')).toBeInTheDocument();
      });

      // Should have h-full and relative classes but not undefined
      expect(container.firstChild).toHaveClass('h-full');
      expect(container.firstChild).toHaveClass('relative');
    });
  });

  describe('API Integration', () => {
    it('calls correct API endpoint with credentials', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ participants: mockParticipants }),
      });

      render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/workspace/ws-123/participants',
          expect.objectContaining({
            credentials: expect.any(String),
            headers: expect.objectContaining({
              'Content-Type': 'application/json',
            }),
          })
        );
      });
    });

    it('reloads participants when workspaceId changes', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ participants: mockParticipants }),
      });

      const { rerender } = render(<WorkspaceMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/workspace/ws-123/participants',
          expect.any(Object)
        );
      });

      jest.clearAllMocks();

      rerender(<WorkspaceMessaging {...defaultProps} workspaceId="ws-456" />);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/workspace/ws-456/participants',
          expect.any(Object)
        );
      });
    });
  });
});

describe('ExampleWorkspacePage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ participants: [] }),
    });
  });

  it('renders workspace header with title', async () => {
    render(<ExampleWorkspacePage />);

    expect(screen.getByText('Project Alpha Development')).toBeInTheDocument();
    expect(screen.getByText(/Collaborative workspace/)).toBeInTheDocument();
  });

  it('renders sidebar with navigation options', async () => {
    render(<ExampleWorkspacePage />);

    expect(screen.getByText('Workspace Tools')).toBeInTheDocument();
    expect(screen.getByText(/Files/)).toBeInTheDocument();
    expect(screen.getByText(/Tasks/)).toBeInTheDocument();
    expect(screen.getByText(/Messages/)).toBeInTheDocument();
    expect(screen.getByText(/Analytics/)).toBeInTheDocument();
  });

  it('highlights the Messages button as active', async () => {
    render(<ExampleWorkspacePage />);

    const messagesButton = screen.getByText(/Messages/);
    expect(messagesButton).toHaveClass('text-primary');
  });

  it('renders WorkspaceMessaging component', async () => {
    render(<ExampleWorkspacePage />);

    await waitFor(() => {
      expect(screen.getByTestId('message-center')).toBeInTheDocument();
    });
  });
});
