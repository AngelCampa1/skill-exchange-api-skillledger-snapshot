/**
 * Simplified MessageCenter tests focusing on core functionality
 */

import React from 'react';
import { render, screen, act } from '@testing-library/react';
import '@testing-library/jest-dom';

// Mock all messaging services and components
jest.mock('../../services/signalRService', () => ({
  signalRService: {
    connect: jest.fn().mockResolvedValue(void 0),
    disconnect: jest.fn().mockResolvedValue(void 0),
    getConnectionState: jest.fn().mockReturnValue({ status: 'connected' }),
    on: jest.fn(),
    off: jest.fn(),
    joinWorkspace: jest.fn().mockResolvedValue(void 0),
    isConnected: jest.fn().mockReturnValue(true),
  }
}));

jest.mock('../../services/messagingApiService', () => ({
  messagingApiService: {
    getMessageHistory: jest.fn().mockResolvedValue({
      messages: [],
      totalCount: 0,
      hasMore: false
    }),
  }
}));

// Mock all child components
jest.mock('../messaging/MessageList', () => ({
  MessageList: () => <div data-testid="message-list" />
}));

jest.mock('../messaging/MessageInput', () => ({
  MessageInput: () => <div data-testid="message-input" />
}));

jest.mock('../messaging/MessageSearch', () => ({
  MessageSearch: () => <div data-testid="message-search" />
}));

jest.mock('../messaging/MessageNotifications', () => ({
  MessageNotifications: () => <div data-testid="message-notifications" />
}));

jest.mock('../messaging/TypingIndicators', () => ({
  TypingIndicators: () => <div data-testid="typing-indicators" />
}));

jest.mock('../messaging/ConnectionStatusIndicator', () => ({
  ConnectionStatusIndicator: () => <div data-testid="connection-status" />
}));

// Import MessageCenter after mocks
import { MessageCenter } from '../messaging/MessageCenter';

describe('MessageCenter', () => {
  const defaultProps = {
    workspaceId: 'workspace-1',
    currentUserId: 'user-1',
    workspaceTitle: 'Test Workspace',
    participants: [
      {
        id: 'user-1',
        name: 'John Doe',
        avatar: '/avatar1.jpg',
        isOnline: true
      }
    ]
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('renders basic components', async () => {
    await act(async () => {
      render(<MessageCenter {...defaultProps} />);
    });

    expect(screen.getByTestId('message-list')).toBeInTheDocument();
    expect(screen.getByTestId('message-input')).toBeInTheDocument();
  });

  test('renders with workspace ID', async () => {
    await act(async () => {
      render(<MessageCenter {...defaultProps} />);
    });

    expect(screen.getByTestId('message-list')).toBeInTheDocument();
  });

  test('handles component mounting and unmounting', async () => {
    const { unmount } = await act(async () => {
      return render(<MessageCenter {...defaultProps} />);
    });

    // Component should render without errors
    expect(screen.getByTestId('message-list')).toBeInTheDocument();

    // Should unmount without errors
    await act(async () => {
      unmount();
    });
  });
});