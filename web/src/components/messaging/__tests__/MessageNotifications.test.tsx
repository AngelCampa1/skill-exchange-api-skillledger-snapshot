/**
 * MessageNotifications.tsx Tests
 *
 * Tests for message notification toasts and useMessageNotifications hook.
 * Coverage Target: 80%+
 */

import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import { renderHook } from '@testing-library/react';
import '@testing-library/jest-dom';
import { MessageNotifications, useMessageNotifications } from '../MessageNotifications';
import { Message, MessageType, MessageStatus, MessageNotification } from '../../../types/messaging';

// Mock dependencies
jest.mock('next/image', () => ({
  __esModule: true,
  default: ({ src, alt, className }: any) => (
    <img src={src} alt={alt} className={className} data-testid="notification-avatar" />
  ),
}));

jest.mock('lucide-react', () => ({
  X: () => <span data-testid="x-icon">X</span>,
  MessageCircle: () => <span data-testid="message-circle-icon">MessageCircle</span>,
  FileText: () => <span data-testid="file-text-icon">FileText</span>,
  Image: () => <span data-testid="image-icon">Image</span>,
  Mic: () => <span data-testid="mic-icon">Mic</span>,
  Users: () => <span data-testid="users-icon">Users</span>,
  Info: () => <span data-testid="info-icon">Info</span>,
}));

describe('MessageNotifications', () => {
  const createMockMessage = (overrides: Partial<Message> = {}): Message => ({
    id: 'msg-1',
    workspaceId: 'ws-123',
    senderId: 'user-1',
    senderName: 'John Doe',
    senderAvatar: '/avatar.png',
    messageText: 'Hello world!',
    messageType: MessageType.Text,
    status: MessageStatus.Sent,
    createdAt: new Date().toISOString(),
    reactions: [],
    isEdited: false,
    canEdit: true,
    canDelete: true,
    ...overrides,
  });

  const createMockNotification = (overrides: Partial<MessageNotification> = {}): MessageNotification => ({
    id: 'notification-1',
    message: createMockMessage(),
    timestamp: new Date().toISOString(),
    isRead: false,
    ...overrides,
  });

  const defaultProps = {
    notifications: [] as MessageNotification[],
    onNotificationClick: jest.fn(),
    onNotificationDismiss: jest.fn(),
    onNotificationClearAll: jest.fn(),
  };

  beforeEach(() => {
    jest.clearAllMocks();
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  describe('Rendering', () => {
    it('returns null when no notifications', () => {
      const { container } = render(<MessageNotifications {...defaultProps} notifications={[]} />);
      expect(container.firstChild).toBeNull();
    });

    it('renders notifications when present', () => {
      const notifications = [createMockNotification()];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByText('John Doe')).toBeInTheDocument();
      expect(screen.getByText('Hello world!')).toBeInTheDocument();
    });

    it('renders only unread notifications', () => {
      const notifications = [
        createMockNotification({ id: 'n1', isRead: false }),
        createMockNotification({ id: 'n2', isRead: true }),
      ];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      // Only 1 should be visible (unread)
      const notificationElements = screen.getAllByTestId('notification-avatar');
      expect(notificationElements).toHaveLength(1);
    });

    it('shows max 3 notifications at a time', () => {
      const notifications = [
        createMockNotification({ id: 'n1', message: createMockMessage({ senderName: 'User1' }) }),
        createMockNotification({ id: 'n2', message: createMockMessage({ senderName: 'User2' }) }),
        createMockNotification({ id: 'n3', message: createMockMessage({ senderName: 'User3' }) }),
        createMockNotification({ id: 'n4', message: createMockMessage({ senderName: 'User4' }) }),
      ];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      // Should show latest 3 only
      const notificationElements = screen.getAllByTestId('notification-avatar');
      expect(notificationElements).toHaveLength(3);
    });

    it('uses default avatar when senderAvatar is not provided', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ senderAvatar: undefined }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      const avatar = screen.getByTestId('notification-avatar');
      expect(avatar).toHaveAttribute('src', '/default-avatar.png');
    });
  });

  describe('Notification Icons', () => {
    it('shows MessageCircle icon for text messages', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ messageType: MessageType.Text }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByTestId('message-circle-icon')).toBeInTheDocument();
    });

    it('shows Image icon for image messages', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ messageType: MessageType.Image }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByTestId('image-icon')).toBeInTheDocument();
    });

    it('shows FileText icon for file messages', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ messageType: MessageType.File }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByTestId('file-text-icon')).toBeInTheDocument();
    });

    it('shows Mic icon for voice messages', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ messageType: MessageType.Voice }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByTestId('mic-icon')).toBeInTheDocument();
    });

    it('shows Info icon for system messages', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ messageType: MessageType.System }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByTestId('info-icon')).toBeInTheDocument();
    });

    it('shows Users icon for milestone messages', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ messageType: MessageType.Milestone }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByTestId('users-icon')).toBeInTheDocument();
    });

    it('shows default MessageCircle icon for unknown message type', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ messageType: 'unknown' as unknown as MessageType }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByTestId('message-circle-icon')).toBeInTheDocument();
    });
  });

  describe('Notification Titles', () => {
    it('shows sender name for text messages', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ senderName: 'Alice', messageType: MessageType.Text }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByText('Alice')).toBeInTheDocument();
    });

    it('shows "sent an image" for image messages', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ senderName: 'Bob', messageType: MessageType.Image }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByText('Bob sent an image')).toBeInTheDocument();
    });

    it('shows "sent a file" for file messages', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ senderName: 'Charlie', messageType: MessageType.File }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByText('Charlie sent a file')).toBeInTheDocument();
    });

    it('shows "sent a voice message" for voice messages', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ senderName: 'Diana', messageType: MessageType.Voice }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByText('Diana sent a voice message')).toBeInTheDocument();
    });

    it('shows "System notification" for system messages', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ messageType: MessageType.System }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByText('System notification')).toBeInTheDocument();
    });

    it('shows "updated a milestone" for milestone messages', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ senderName: 'Eve', messageType: MessageType.Milestone }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByText('Eve updated a milestone')).toBeInTheDocument();
    });
  });

  describe('Notification Content', () => {
    it('truncates long message text to 60 characters', () => {
      const longText = 'A'.repeat(100);
      const notifications = [createMockNotification({
        message: createMockMessage({ messageText: longText }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByText('A'.repeat(60) + '...')).toBeInTheDocument();
    });

    it('shows full text when under 60 characters', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ messageText: 'Short message' }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByText('Short message')).toBeInTheDocument();
    });

    it('shows "Shared an image" for image without text', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ messageText: '', messageType: MessageType.Image }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByText('Shared an image')).toBeInTheDocument();
    });

    it('shows filename for file messages', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({
          messageText: '',
          messageType: MessageType.File,
          attachmentFileName: 'document.pdf',
        }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByText('document.pdf')).toBeInTheDocument();
    });

    it('shows "Shared a file" when no filename', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({
          messageText: '',
          messageType: MessageType.File,
          attachmentFileName: undefined,
        }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByText('Shared a file')).toBeInTheDocument();
    });

    it('shows "Sent a voice message" for voice without text', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ messageText: '', messageType: MessageType.Voice }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByText('Sent a voice message')).toBeInTheDocument();
    });

    it('shows "Updated project milestone" for milestone without text', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ messageText: '', messageType: MessageType.Milestone }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByText('Updated project milestone')).toBeInTheDocument();
    });

    it('shows "New message" for default case without text', () => {
      const notifications = [createMockNotification({
        message: createMockMessage({ messageText: '', messageType: 'unknown' as unknown as MessageType }),
      })];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByText('New message')).toBeInTheDocument();
    });
  });

  describe('User Interactions', () => {
    it('calls onNotificationClick and onNotificationDismiss when notification is clicked', () => {
      const onNotificationClick = jest.fn();
      const onNotificationDismiss = jest.fn();
      const notification = createMockNotification();

      render(
        <MessageNotifications
          {...defaultProps}
          notifications={[notification]}
          onNotificationClick={onNotificationClick}
          onNotificationDismiss={onNotificationDismiss}
        />
      );

      // Click on the notification
      const notificationElement = screen.getByText('Hello world!').closest('div.bg-card');
      fireEvent.click(notificationElement!);

      expect(onNotificationClick).toHaveBeenCalledWith(notification);
      expect(onNotificationDismiss).toHaveBeenCalledWith(notification.id);
    });

    it('calls onNotificationDismiss when X button is clicked', () => {
      const onNotificationDismiss = jest.fn();
      const notification = createMockNotification();

      render(
        <MessageNotifications
          {...defaultProps}
          notifications={[notification]}
          onNotificationDismiss={onNotificationDismiss}
        />
      );

      // Find and click dismiss button
      const dismissButton = screen.getByTestId('x-icon').closest('button');
      fireEvent.click(dismissButton!);

      expect(onNotificationDismiss).toHaveBeenCalledWith(notification.id);
    });

    it('stops propagation when dismiss button is clicked', () => {
      const onNotificationClick = jest.fn();
      const onNotificationDismiss = jest.fn();
      const notification = createMockNotification();

      render(
        <MessageNotifications
          {...defaultProps}
          notifications={[notification]}
          onNotificationClick={onNotificationClick}
          onNotificationDismiss={onNotificationDismiss}
        />
      );

      // Click dismiss button - should not trigger notification click
      const dismissButton = screen.getByTestId('x-icon').closest('button');
      fireEvent.click(dismissButton!);

      expect(onNotificationDismiss).toHaveBeenCalled();
      expect(onNotificationClick).not.toHaveBeenCalled();
    });
  });

  describe('Clear All Button', () => {
    it('does not show clear all button for single notification', () => {
      const notifications = [createMockNotification()];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.queryByText('Clear all notifications')).not.toBeInTheDocument();
    });

    it('shows clear all button for multiple notifications', () => {
      const notifications = [
        createMockNotification({ id: 'n1' }),
        createMockNotification({ id: 'n2' }),
      ];
      render(<MessageNotifications {...defaultProps} notifications={notifications} />);

      expect(screen.getByText('Clear all notifications')).toBeInTheDocument();
    });

    it('calls onNotificationClearAll when clear all button is clicked', () => {
      const onNotificationClearAll = jest.fn();
      const notifications = [
        createMockNotification({ id: 'n1' }),
        createMockNotification({ id: 'n2' }),
      ];

      render(
        <MessageNotifications
          {...defaultProps}
          notifications={notifications}
          onNotificationClearAll={onNotificationClearAll}
        />
      );

      fireEvent.click(screen.getByText('Clear all notifications'));
      expect(onNotificationClearAll).toHaveBeenCalled();
    });
  });

  describe('Auto-Dismiss', () => {
    it('auto-dismisses notifications after 5 seconds', () => {
      const onNotificationDismiss = jest.fn();
      const notification = createMockNotification();

      render(
        <MessageNotifications
          {...defaultProps}
          notifications={[notification]}
          onNotificationDismiss={onNotificationDismiss}
        />
      );

      expect(onNotificationDismiss).not.toHaveBeenCalled();

      // Advance timers by 5 seconds
      act(() => {
        jest.advanceTimersByTime(5000);
      });

      expect(onNotificationDismiss).toHaveBeenCalledWith(notification.id);
    });

    it('clears timers when component unmounts', () => {
      const onNotificationDismiss = jest.fn();
      const notification = createMockNotification();

      const { unmount } = render(
        <MessageNotifications
          {...defaultProps}
          notifications={[notification]}
          onNotificationDismiss={onNotificationDismiss}
        />
      );

      // Unmount before timer fires
      unmount();

      // Advance timers
      act(() => {
        jest.advanceTimersByTime(5000);
      });

      // Should not be called after unmount
      expect(onNotificationDismiss).not.toHaveBeenCalled();
    });
  });
});

describe('useMessageNotifications Hook', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    jest.useFakeTimers();

    // Mock Notification API
    Object.defineProperty(global, 'Notification', {
      value: jest.fn().mockImplementation(() => ({
        close: jest.fn(),
      })),
      writable: true,
    });
    (global.Notification as any).permission = 'default';
    (global.Notification as any).requestPermission = jest.fn().mockResolvedValue('granted');
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  const createMockMessage = (overrides: Partial<Message> = {}): Message => ({
    id: 'msg-1',
    workspaceId: 'ws-123',
    senderId: 'user-1',
    senderName: 'John Doe',
    senderAvatar: '/avatar.png',
    messageText: 'Hello world!',
    messageType: MessageType.Text,
    status: MessageStatus.Sent,
    createdAt: new Date().toISOString(),
    reactions: [],
    isEdited: false,
    canEdit: true,
    canDelete: true,
    ...overrides,
  });

  it('initializes with empty notifications', () => {
    const { result } = renderHook(() => useMessageNotifications());

    expect(result.current.notifications).toEqual([]);
  });

  it('adds notification with addNotification', () => {
    const { result } = renderHook(() => useMessageNotifications());
    const message = createMockMessage();

    act(() => {
      result.current.addNotification(message);
    });

    expect(result.current.notifications).toHaveLength(1);
    expect(result.current.notifications[0].message).toEqual(message);
    expect(result.current.notifications[0].isRead).toBe(false);
  });

  it('dismisses notification with dismissNotification', () => {
    const { result } = renderHook(() => useMessageNotifications());
    const message = createMockMessage();

    act(() => {
      result.current.addNotification(message);
    });

    const notificationId = result.current.notifications[0].id;

    act(() => {
      result.current.dismissNotification(notificationId);
    });

    expect(result.current.notifications[0].isRead).toBe(true);
  });

  it('clears all notifications with clearAllNotifications', () => {
    const { result } = renderHook(() => useMessageNotifications());

    act(() => {
      result.current.addNotification(createMockMessage({ id: 'msg-1' }));
      result.current.addNotification(createMockMessage({ id: 'msg-2' }));
    });

    expect(result.current.notifications.filter(n => !n.isRead)).toHaveLength(2);

    act(() => {
      result.current.clearAllNotifications();
    });

    expect(result.current.notifications.every(n => n.isRead)).toBe(true);
  });

  it('requests notification permission', async () => {
    const { result } = renderHook(() => useMessageNotifications());

    let permissionResult: boolean | undefined;
    await act(async () => {
      permissionResult = await result.current.requestNotificationPermission();
    });

    expect(Notification.requestPermission).toHaveBeenCalled();
    expect(permissionResult).toBe(true);
  });

  it('returns true if permission already granted', async () => {
    (global.Notification as any).permission = 'granted';

    const { result } = renderHook(() => useMessageNotifications());

    let permissionResult: boolean | undefined;
    await act(async () => {
      permissionResult = await result.current.requestNotificationPermission();
    });

    expect(permissionResult).toBe(true);
  });

  it('shows browser notification when permission is granted', () => {
    (global.Notification as any).permission = 'granted';
    const { result } = renderHook(() => useMessageNotifications());
    const message = createMockMessage();

    act(() => {
      result.current.addNotification(message);
    });

    expect(global.Notification).toHaveBeenCalledWith(
      'John Doe',
      expect.objectContaining({
        body: 'Hello world!',
        icon: '/avatar.png',
        tag: 'msg-1',
      })
    );
  });

  it('uses default avatar in browser notification when none provided', () => {
    (global.Notification as any).permission = 'granted';
    const { result } = renderHook(() => useMessageNotifications());
    const message = createMockMessage({ senderAvatar: undefined });

    act(() => {
      result.current.addNotification(message);
    });

    expect(global.Notification).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({
        icon: '/default-avatar.png',
      })
    );
  });

  it('closes browser notification after 5 seconds', () => {
    (global.Notification as any).permission = 'granted';
    const mockClose = jest.fn();
    (global.Notification as any).mockImplementation(() => ({
      close: mockClose,
    }));

    const { result } = renderHook(() => useMessageNotifications());

    act(() => {
      result.current.addNotification(createMockMessage());
    });

    expect(mockClose).not.toHaveBeenCalled();

    act(() => {
      jest.advanceTimersByTime(5000);
    });

    expect(mockClose).toHaveBeenCalled();
  });

  it('does not show browser notification when permission is denied', () => {
    (global.Notification as any).permission = 'denied';
    const { result } = renderHook(() => useMessageNotifications());

    act(() => {
      result.current.addNotification(createMockMessage());
    });

    expect(global.Notification).not.toHaveBeenCalled();
  });
});

describe('Helper Functions (getNotificationTitle, getNotificationContent)', () => {
  const createMockMessage = (overrides: Partial<Message> = {}): Message => ({
    id: 'msg-1',
    workspaceId: 'ws-123',
    senderId: 'user-1',
    senderName: 'John Doe',
    senderAvatar: '/avatar.png',
    messageText: 'Hello world!',
    messageType: MessageType.Text,
    status: MessageStatus.Sent,
    createdAt: new Date().toISOString(),
    reactions: [],
    isEdited: false,
    canEdit: true,
    canDelete: true,
    ...overrides,
  });

  beforeEach(() => {
    (global.Notification as any).permission = 'granted';
    jest.clearAllMocks();
  });

  // Test all message type titles through browser notifications
  it('generates correct title for image message in browser notification', () => {
    const { result } = renderHook(() => useMessageNotifications());

    act(() => {
      result.current.addNotification(createMockMessage({
        senderName: 'Alice',
        messageType: MessageType.Image,
      }));
    });

    expect(global.Notification).toHaveBeenCalledWith(
      'Alice sent an image',
      expect.any(Object)
    );
  });

  it('generates correct title for file message in browser notification', () => {
    const { result } = renderHook(() => useMessageNotifications());

    act(() => {
      result.current.addNotification(createMockMessage({
        senderName: 'Bob',
        messageType: MessageType.File,
      }));
    });

    expect(global.Notification).toHaveBeenCalledWith(
      'Bob sent a file',
      expect.any(Object)
    );
  });

  it('generates correct title for voice message in browser notification', () => {
    const { result } = renderHook(() => useMessageNotifications());

    act(() => {
      result.current.addNotification(createMockMessage({
        senderName: 'Charlie',
        messageType: MessageType.Voice,
      }));
    });

    expect(global.Notification).toHaveBeenCalledWith(
      'Charlie sent a voice message',
      expect.any(Object)
    );
  });

  it('generates correct title for system message in browser notification', () => {
    const { result } = renderHook(() => useMessageNotifications());

    act(() => {
      result.current.addNotification(createMockMessage({
        messageType: MessageType.System,
      }));
    });

    expect(global.Notification).toHaveBeenCalledWith(
      'System notification',
      expect.any(Object)
    );
  });

  it('generates correct title for milestone message in browser notification', () => {
    const { result } = renderHook(() => useMessageNotifications());

    act(() => {
      result.current.addNotification(createMockMessage({
        senderName: 'Diana',
        messageType: MessageType.Milestone,
      }));
    });

    expect(global.Notification).toHaveBeenCalledWith(
      'Diana updated a milestone',
      expect.any(Object)
    );
  });

  it('generates correct title for default message type (sender name only)', () => {
    const { result } = renderHook(() => useMessageNotifications());

    act(() => {
      result.current.addNotification(createMockMessage({
        senderName: 'Eve',
        messageType: 'unknown' as unknown as MessageType,
      }));
    });

    expect(global.Notification).toHaveBeenCalledWith(
      'Eve',
      expect.any(Object)
    );
  });

  // Test content generation
  it('generates correct content for long text (truncates at 100 chars for browser notification)', () => {
    const longText = 'B'.repeat(150);
    const { result } = renderHook(() => useMessageNotifications());

    act(() => {
      result.current.addNotification(createMockMessage({ messageText: longText }));
    });

    expect(global.Notification).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({
        body: 'B'.repeat(100) + '...',
      })
    );
  });

  it('generates "Shared an image" content for image without text', () => {
    const { result } = renderHook(() => useMessageNotifications());

    act(() => {
      result.current.addNotification(createMockMessage({
        messageText: '',
        messageType: MessageType.Image,
      }));
    });

    expect(global.Notification).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({
        body: 'Shared an image',
      })
    );
  });

  it('generates filename content for file message', () => {
    const { result } = renderHook(() => useMessageNotifications());

    act(() => {
      result.current.addNotification(createMockMessage({
        messageText: '',
        messageType: MessageType.File,
        attachmentFileName: 'report.pdf',
      }));
    });

    expect(global.Notification).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({
        body: 'report.pdf',
      })
    );
  });

  it('generates "Shared a file" content when no filename', () => {
    const { result } = renderHook(() => useMessageNotifications());

    act(() => {
      result.current.addNotification(createMockMessage({
        messageText: '',
        messageType: MessageType.File,
        attachmentFileName: undefined,
      }));
    });

    expect(global.Notification).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({
        body: 'Shared a file',
      })
    );
  });

  it('generates "Sent a voice message" content for voice', () => {
    const { result } = renderHook(() => useMessageNotifications());

    act(() => {
      result.current.addNotification(createMockMessage({
        messageText: '',
        messageType: MessageType.Voice,
      }));
    });

    expect(global.Notification).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({
        body: 'Sent a voice message',
      })
    );
  });

  it('generates "Updated project milestone" content for milestone', () => {
    const { result } = renderHook(() => useMessageNotifications());

    act(() => {
      result.current.addNotification(createMockMessage({
        messageText: '',
        messageType: MessageType.Milestone,
      }));
    });

    expect(global.Notification).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({
        body: 'Updated project milestone',
      })
    );
  });

  it('generates "New message" content for default case', () => {
    const { result } = renderHook(() => useMessageNotifications());

    act(() => {
      result.current.addNotification(createMockMessage({
        messageText: '',
        messageType: 'unknown' as unknown as MessageType,
      }));
    });

    expect(global.Notification).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({
        body: 'New message',
      })
    );
  });
});
