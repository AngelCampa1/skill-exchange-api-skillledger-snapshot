/**
 * SimpleMessaging.tsx Tests
 *
 * Tests for simple workspace messaging component with polling.
 * Coverage Target: 80%+
 */

import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import '@testing-library/jest-dom';
import SimpleMessaging from '../SimpleMessaging';

// Mock lucide-react icons
jest.mock('lucide-react', () => ({
  Send: () => <div data-testid="send-icon">Send Icon</div>,
  User: () => <div data-testid="user-icon">User Icon</div>,
}));

// Mock logger
jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
  },
}));

// Mock scrollIntoView (not implemented in jsdom)
Element.prototype.scrollIntoView = jest.fn();

describe('SimpleMessaging', () => {
  const defaultProps = {
    workspaceId: 'ws-123',
    currentUserId: 'user-1',
    currentUserName: 'Alice',
  };

  const mockMessages = [
    {
      id: 'msg-1',
      workspaceId: 'ws-123',
      senderId: 'user-2',
      senderName: 'Bob',
      content: 'Hello Alice!',
      createdAt: '2024-01-01T10:00:00Z',
    },
    {
      id: 'msg-2',
      workspaceId: 'ws-123',
      senderId: 'user-1',
      senderName: 'Alice',
      content: 'Hi Bob!',
      createdAt: '2024-01-01T10:01:00Z',
    },
  ];

  beforeEach(() => {
    jest.clearAllMocks();
    jest.useFakeTimers();
    global.fetch = jest.fn();
    global.alert = jest.fn();
  });

  afterEach(() => {
    jest.useRealTimers();
    jest.restoreAllMocks();
  });

  describe('Rendering', () => {
    it('renders messages container', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: [] }),
      });

      render(<SimpleMessaging {...defaultProps} />);

      expect(screen.getByTestId('messages')).toBeInTheDocument();
    });

    it('renders message input field', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: [] }),
      });

      render(<SimpleMessaging {...defaultProps} />);

      expect(screen.getByTestId('message-input')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('Type your message...')).toBeInTheDocument();
    });

    it('renders send button', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: [] }),
      });

      render(<SimpleMessaging {...defaultProps} />);

      expect(screen.getByTestId('send-message-button')).toBeInTheDocument();
    });

    it('disables send button when input is empty', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: [] }),
      });

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        const sendButton = screen.getByTestId('send-message-button');
        expect(sendButton).toBeDisabled();
      });
    });

    it('enables send button when input has text', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: [] }),
      });

      render(<SimpleMessaging {...defaultProps} />);

      const input = screen.getByTestId('message-input');
      fireEvent.change(input, { target: { value: 'Hello' } });

      await waitFor(() => {
        const sendButton = screen.getByTestId('send-message-button');
        expect(sendButton).not.toBeDisabled();
      });
    });

    it('disables send button when input has only whitespace', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: [] }),
      });

      render(<SimpleMessaging {...defaultProps} />);

      const input = screen.getByTestId('message-input');
      fireEvent.change(input, { target: { value: '   ' } });

      await waitFor(() => {
        const sendButton = screen.getByTestId('send-message-button');
        expect(sendButton).toBeDisabled();
      });
    });
  });

  describe('Loading Messages', () => {
    it('shows loading spinner on initial load', async () => {
      (global.fetch as jest.Mock).mockImplementation(() => new Promise(() => {})); // Never resolves

      render(<SimpleMessaging {...defaultProps} />);

      expect(screen.getByText('Loading messages...')).toBeInTheDocument();
      expect(document.querySelector('.animate-spin')).toBeInTheDocument();
    });

    it('loads messages on mount', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: mockMessages }),
      });

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/workspace/ws-123/messages',
          expect.objectContaining({ credentials: 'include' })
        );
      });
    });

    it('displays loaded messages', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: mockMessages }),
      });

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByText('Hello Alice!')).toBeInTheDocument();
        expect(screen.getByText('Hi Bob!')).toBeInTheDocument();
      });
    });

    it('handles messages array at root level', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => mockMessages, // Array at root, not in 'messages' property
      });

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByText('Hello Alice!')).toBeInTheDocument();
      });
    });

    it('shows empty state when no messages', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: [] }),
      });

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByText(/No messages yet/i)).toBeInTheDocument();
        expect(screen.getByText(/Start the conversation!/i)).toBeInTheDocument();
      });
    });

    it('handles load error gracefully', async () => {
      const { logger } = require('@/utils/logger');
      (global.fetch as jest.Mock).mockRejectedValue(new Error('Network error'));

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(logger.error).toHaveBeenCalledWith(
          'Error loading messages',
          expect.any(Error),
          { component: 'SimpleMessaging' }
        );
      });
    });

    it('handles non-ok response', async () => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: false,
        status: 500,
      });

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByText(/No messages yet/i)).toBeInTheDocument();
      });
    });
  });

  describe('Message Display', () => {
    beforeEach(() => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: mockMessages }),
      });
    });

    it('displays sender name for other users', async () => {
      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByText('Bob')).toBeInTheDocument();
      });
    });

    it('displays "You" for current user messages', async () => {
      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        const youLabels = screen.getAllByText('You');
        expect(youLabels.length).toBeGreaterThan(0);
      });
    });

    it('shows user icon for other users', async () => {
      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('user-icon')).toBeInTheDocument();
      });
    });

    it('does not show user icon for own messages', async () => {
      const singleOwnMessage = [mockMessages[1]]; // Only Alice's message
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: singleOwnMessage }),
      });

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByText('Hi Bob!')).toBeInTheDocument();
      });

      expect(screen.queryByTestId('user-icon')).not.toBeInTheDocument();
    });

    it('formats message timestamps', async () => {
      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        // Check that timestamps are rendered (exact format depends on locale)
        const timeElements = screen.queryAllByText(/\d{1,2}:\d{2}/);
        expect(timeElements.length).toBeGreaterThan(0);
      });
    });

    it('applies correct styling for own messages', async () => {
      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        const messageItems = screen.getAllByTestId('message-item');
        const ownMessage = messageItems.find(item => item.querySelector('.bg-primary'));
        expect(ownMessage).toBeInTheDocument();
      });
    });

    it('applies correct styling for other users messages', async () => {
      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        const messageItems = screen.getAllByTestId('message-item');
        const otherMessage = messageItems.find(item => item.querySelector('.bg-muted'));
        expect(otherMessage).toBeInTheDocument();
      });
    });

    it('preserves whitespace and line breaks in message content', async () => {
      const messageWithWhitespace = [
        {
          ...mockMessages[0],
          content: 'Line 1\nLine 2\n  Indented',
        },
      ];
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: messageWithWhitespace }),
      });

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        const content = screen.getByText(/Line 1/);
        expect(content).toHaveClass('whitespace-pre-wrap');
        expect(content).toHaveClass('break-words');
      });
    });
  });

  describe('Sending Messages', () => {
    beforeEach(() => {
      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: [] }),
      });
    });

    it('fetches CSRF token before sending', async () => {
      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-input')).toBeInTheDocument();
      });

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token-123' }),
      }).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ messages: [] }),
      });

      const input = screen.getByTestId('message-input');
      const form = input.closest('form')!;

      fireEvent.change(input, { target: { value: 'Test message' } });
      fireEvent.submit(form);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/auth/csrf-token',
          expect.objectContaining({ credentials: 'include' })
        );
      });
    });

    it('sends message with CSRF token', async () => {
      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-input')).toBeInTheDocument();
      });

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token-123' }),
      }).mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      }).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ messages: [] }),
      });

      const input = screen.getByTestId('message-input');
      const form = input.closest('form')!;

      fireEvent.change(input, { target: { value: 'Test message' } });
      fireEvent.submit(form);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/workspace/ws-123/messages',
          expect.objectContaining({
            method: 'POST',
            headers: expect.objectContaining({
              'Content-Type': 'application/json',
              'X-CSRF-TOKEN': 'csrf-token-123',
            }),
            credentials: 'include',
            body: JSON.stringify({ content: 'Test message' }),
          })
        );
      });
    });

    it('trims whitespace before sending', async () => {
      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-input')).toBeInTheDocument();
      });

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token-123' }),
      }).mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      }).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ messages: [] }),
      });

      const input = screen.getByTestId('message-input');
      const form = input.closest('form')!;

      fireEvent.change(input, { target: { value: '  Test message  ' } });
      fireEvent.submit(form);

      await waitFor(() => {
        const lastCall = (global.fetch as jest.Mock).mock.calls.find(
          call => call[0] === '/api/workspace/ws-123/messages' && call[1]?.method === 'POST'
        );
        expect(lastCall).toBeDefined();
        expect(JSON.parse(lastCall[1].body)).toEqual({ content: 'Test message' });
      });
    });

    it('clears input after successful send', async () => {
      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-input')).toBeInTheDocument();
      });

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token-123' }),
      }).mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      }).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ messages: [] }),
      });

      const input = screen.getByTestId('message-input') as HTMLInputElement;
      const form = input.closest('form')!;

      fireEvent.change(input, { target: { value: 'Test message' } });
      expect(input.value).toBe('Test message');

      fireEvent.submit(form);

      await waitFor(() => {
        expect(input.value).toBe('');
      });
    });

    it('reloads messages after successful send', async () => {
      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-input')).toBeInTheDocument();
      });

      const initialFetchCount = (global.fetch as jest.Mock).mock.calls.filter(
        call => call[0] === '/api/workspace/ws-123/messages' && !call[1]?.method
      ).length;

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token-123' }),
      }).mockResolvedValueOnce({
        ok: true,
        json: async () => ({}),
      }).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ messages: mockMessages }),
      });

      const input = screen.getByTestId('message-input');
      const form = input.closest('form')!;

      fireEvent.change(input, { target: { value: 'Test message' } });
      fireEvent.submit(form);

      await waitFor(() => {
        const finalFetchCount = (global.fetch as jest.Mock).mock.calls.filter(
          call => call[0] === '/api/workspace/ws-123/messages' && !call[1]?.method
        ).length;
        expect(finalFetchCount).toBeGreaterThan(initialFetchCount);
      });
    });

    it('shows sending state during send', async () => {
      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-input')).toBeInTheDocument();
      });

      // Make send operation take a while
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token-123' }),
      }).mockImplementation(() => new Promise(() => {})); // Never resolves

      const input = screen.getByTestId('message-input');
      const form = input.closest('form')!;

      fireEvent.change(input, { target: { value: 'Test message' } });
      fireEvent.submit(form);

      await waitFor(() => {
        expect(screen.getByText('Sending...')).toBeInTheDocument();
      });
    });

    it('disables input during send', async () => {
      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-input')).toBeInTheDocument();
      });

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token-123' }),
      }).mockImplementation(() => new Promise(() => {}));

      const input = screen.getByTestId('message-input') as HTMLInputElement;
      const form = input.closest('form')!;

      fireEvent.change(input, { target: { value: 'Test message' } });
      fireEvent.submit(form);

      await waitFor(() => {
        expect(input).toBeDisabled();
      });
    });

    it('does not send empty message', async () => {
      jest.useRealTimers(); // Use real timers for this test

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-input')).toBeInTheDocument();
      });

      const sendButton = screen.getByTestId('send-message-button');

      // Button should be disabled when input is empty
      expect(sendButton).toBeDisabled();

      jest.useFakeTimers(); // Restore fake timers
    });

    it('does not send whitespace-only message', async () => {
      jest.useRealTimers();

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-input')).toBeInTheDocument();
      });

      const input = screen.getByTestId('message-input');
      const sendButton = screen.getByTestId('send-message-button');

      fireEvent.change(input, { target: { value: '   ' } });

      // Button should be disabled when input is only whitespace
      await waitFor(() => {
        expect(sendButton).toBeDisabled();
      });

      jest.useFakeTimers();
    });
  });

  describe('Error Handling', () => {
    it('handles CSRF token fetch failure', async () => {
      jest.useRealTimers();
      const { logger } = require('@/utils/logger');

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-input')).toBeInTheDocument();
      });

      (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));

      const input = screen.getByTestId('message-input');
      const form = input.closest('form')!;

      fireEvent.change(input, { target: { value: 'Test message' } });
      fireEvent.submit(form);

      await waitFor(() => {
        expect(logger.error).toHaveBeenCalledWith(
          'Failed to get CSRF token',
          expect.any(Error),
          { component: 'SimpleMessaging' }
        );
      });

      jest.useFakeTimers();
    });

    it('handles CSRF token non-ok response', async () => {
      jest.useRealTimers();

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-input')).toBeInTheDocument();
      });

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        status: 500,
      });

      const input = screen.getByTestId('message-input');
      const form = input.closest('form')!;

      fireEvent.change(input, { target: { value: 'Test message' } });
      fireEvent.submit(form);

      await waitFor(() => {
        expect(global.alert).toHaveBeenCalledWith('Network error. Please check your connection.');
      });

      jest.useFakeTimers();
    });

    it('shows alert on send failure', async () => {
      jest.useRealTimers();
      const { logger } = require('@/utils/logger');

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-input')).toBeInTheDocument();
      });

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token-123' }),
      }).mockResolvedValueOnce({
        ok: false,
        status: 500,
      });

      const input = screen.getByTestId('message-input');
      const form = input.closest('form')!;

      fireEvent.change(input, { target: { value: 'Test message' } });
      fireEvent.submit(form);

      await waitFor(() => {
        expect(logger.error).toHaveBeenCalledWith(
          'Failed to send message',
          undefined,
          { component: 'SimpleMessaging' }
        );
        expect(global.alert).toHaveBeenCalledWith('Failed to send message. Please try again.');
      });

      jest.useFakeTimers();
    });

    it('shows network error alert on exception', async () => {
      jest.useRealTimers();
      const { logger } = require('@/utils/logger');

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByTestId('message-input')).toBeInTheDocument();
      });

      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'csrf-token-123' }),
      }).mockRejectedValueOnce(new Error('Network error'));

      const input = screen.getByTestId('message-input');
      const form = input.closest('form')!;

      fireEvent.change(input, { target: { value: 'Test message' } });
      fireEvent.submit(form);

      await waitFor(() => {
        expect(logger.error).toHaveBeenCalledWith(
          'Error sending message',
          expect.any(Error),
          { component: 'SimpleMessaging' }
        );
        expect(global.alert).toHaveBeenCalledWith('Network error. Please check your connection.');
      });

      jest.useFakeTimers();
    });
  });

  describe('Message Polling', () => {
    it('polls for new messages every 5 seconds', async () => {
      jest.useRealTimers();
      jest.useFakeTimers();

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: [] }),
      });

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalled();
      });

      const initialCallCount = (global.fetch as jest.Mock).mock.calls.length;

      await act(async () => {
        jest.advanceTimersByTime(5000);
        await Promise.resolve();
      });

      await waitFor(() => {
        expect((global.fetch as jest.Mock).mock.calls.length).toBeGreaterThan(initialCallCount);
      });

      jest.useRealTimers();
    });

    it('cleans up polling interval on unmount', async () => {
      jest.useRealTimers();
      jest.useFakeTimers();

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: [] }),
      });

      const { unmount } = render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalled();
      });

      const callCountBeforeUnmount = (global.fetch as jest.Mock).mock.calls.length;

      unmount();

      await act(async () => {
        jest.advanceTimersByTime(10000);
        await Promise.resolve();
      });

      // Should not have made additional calls after unmount
      expect((global.fetch as jest.Mock).mock.calls.length).toBe(callCountBeforeUnmount);

      jest.useRealTimers();
    });

    it('reloads messages when workspaceId changes', async () => {
      jest.useRealTimers();

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: [] }),
      });

      const { rerender } = render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/workspace/ws-123/messages',
          expect.any(Object)
        );
      });

      jest.clearAllMocks();

      rerender(<SimpleMessaging {...defaultProps} workspaceId="ws-456" />);

      await waitFor(() => {
        expect(global.fetch).toHaveBeenCalledWith(
          '/api/workspace/ws-456/messages',
          expect.any(Object)
        );
      });

      jest.useFakeTimers();
    });
  });

  describe('Auto Scroll', () => {
    it('scrolls to bottom when messages change', async () => {
      jest.useRealTimers();

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: mockMessages }),
      });

      const mockScrollIntoView = jest.fn();
      Element.prototype.scrollIntoView = mockScrollIntoView;

      render(<SimpleMessaging {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByText('Hello Alice!')).toBeInTheDocument();
      });

      // scrollIntoView should have been called
      expect(mockScrollIntoView).toHaveBeenCalled();

      jest.useFakeTimers();
    });
  });

  describe('Props', () => {
    it('uses default currentUserName when not provided', async () => {
      jest.useRealTimers();

      (global.fetch as jest.Mock).mockResolvedValue({
        ok: true,
        json: async () => ({ messages: [mockMessages[1]] }),
      });

      const propsWithoutName = {
        workspaceId: 'ws-123',
        currentUserId: 'user-1',
      };

      render(<SimpleMessaging {...propsWithoutName} />);

      await waitFor(() => {
        expect(screen.getByText('You')).toBeInTheDocument();
      });

      jest.useFakeTimers();
    });
  });
});
