/**
 * Simplified MessageInput tests
 */

import React from 'react';
import { render, screen, fireEvent, act } from '@testing-library/react';
import '@testing-library/jest-dom';

// Mock dependencies
jest.mock('../../services/messagingApiService', () => ({
  messagingApiService: {
    sendMessage: jest.fn().mockResolvedValue({ id: 'msg-1' }),
    uploadFile: jest.fn().mockResolvedValue({ url: 'file-url' }),
  }
}));

// BUG-LOW-002 FIX: Define proper type for emoji picker props
interface MockEmojiPickerProps {
  onEmojiClick: (emojiData: { emoji: string }) => void;
}

jest.mock('emoji-picker-react', () => ({
  __esModule: true,
  default: ({ onEmojiClick }: MockEmojiPickerProps) => (
    <div data-testid="emoji-picker">
      <button
        onClick={() => onEmojiClick({ emoji: '😀' })}
        data-testid="emoji-button"
      >
        😀
      </button>
    </div>
  )
}));

// Import component after mocks
import { MessageInput } from '../messaging/MessageInput';

describe('MessageInput', () => {
  const defaultProps = {
    workspaceId: 'workspace-1',
    currentUserId: 'user-1',
    onMessageSent: jest.fn(),
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('renders input field', async () => {
    await act(async () => {
      render(<MessageInput {...defaultProps} />);
    });

    expect(screen.getByRole('textbox')).toBeInTheDocument();
  });

  test('handles text input', async () => {
    await act(async () => {
      render(<MessageInput {...defaultProps} />);
    });

    const input = screen.getByRole('textbox');
    
    await act(async () => {
      fireEvent.change(input, { target: { value: 'Hello world' } });
    });

    expect(input).toHaveValue('Hello world');
  });

  test('renders send button', async () => {
    await act(async () => {
      render(<MessageInput {...defaultProps} />);
    });

    // The send button has an SVG icon but no visible text
    const sendButtons = screen.getAllByRole('button');
    const sendButton = sendButtons.find(button => 
      button.querySelector('svg[class*="lucide-send"]')
    );
    expect(sendButton).toBeInTheDocument();
  });
});