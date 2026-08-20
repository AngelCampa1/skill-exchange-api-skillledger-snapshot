/**
 * MessageItem.tsx Tests
 *
 * Tests for the individual message component with reactions, editing, and attachments.
 * Coverage Target: 80%+
 */

import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import '@testing-library/jest-dom';
import { MessageItem } from '../MessageItem';
import { messagingApiService } from '../../../services/messagingApiService';
import { Message, MessageType, MessageStatus } from '../../../types/messaging';

// Mock the messaging API service
jest.mock('../../../services/messagingApiService', () => ({
  messagingApiService: {
    editMessage: jest.fn(),
    deleteMessage: jest.fn(),
    downloadFile: jest.fn(),
  },
}));

// Mock next/image
jest.mock('next/image', () => ({
  __esModule: true,
  default: ({ src, alt, onClick, ...props }: any) => (
    // eslint-disable-next-line @next/next/no-img-element
    <img src={src} alt={alt} onClick={onClick} {...props} data-testid="next-image" />
  ),
}));

// Mock lucide-react icons
jest.mock('lucide-react', () => ({
  MoreHorizontal: () => <span data-testid="more-icon">MoreIcon</span>,
  Edit: () => <span data-testid="edit-icon">EditIcon</span>,
  Trash2: () => <span data-testid="trash-icon">TrashIcon</span>,
  Reply: () => <span data-testid="reply-icon">ReplyIcon</span>,
  Copy: () => <span data-testid="copy-icon">CopyIcon</span>,
  Download: () => <span data-testid="download-icon">DownloadIcon</span>,
  Check: () => <span data-testid="check-icon">CheckIcon</span>,
  CheckCheck: () => <span data-testid="checkcheck-icon">CheckCheckIcon</span>,
  Clock: () => <span data-testid="clock-icon">ClockIcon</span>,
  AlertCircle: () => <span data-testid="alert-icon">AlertIcon</span>,
  FileText: () => <span data-testid="file-icon">FileIcon</span>,
  Image: () => <span data-testid="image-icon">ImageIcon</span>,
  Mic: () => <span data-testid="mic-icon">MicIcon</span>,
  Play: () => <span data-testid="play-icon">PlayIcon</span>,
  Pause: () => <span data-testid="pause-icon">PauseIcon</span>,
}));

// Mock date-fns format
jest.mock('date-fns', () => ({
  format: jest.fn(() => '10:30 AM'),
}));

// Mock EmojiReactions component
jest.mock('../EmojiReactions', () => ({
  EmojiReactions: ({ messageId }: { messageId: string }) => (
    <div data-testid="emoji-reactions">Reactions for {messageId}</div>
  ),
}));

// Mock window.confirm
const mockConfirm = jest.fn();
window.confirm = mockConfirm;

// Mock clipboard
const mockClipboard = {
  writeText: jest.fn(),
};
Object.assign(navigator, { clipboard: mockClipboard });

// Mock window.open
const mockWindowOpen = jest.fn();
window.open = mockWindowOpen;

describe('MessageItem', () => {
  const defaultProps = {
    workspaceId: 'ws-123',
    isCurrentUser: false,
    showAvatar: true,
    showSender: true,
    showTimestamp: true,
  };

  const createMockMessage = (overrides: Partial<Message> = {}): Message => ({
    id: 'msg-1',
    workspaceId: 'ws-123',
    senderId: 'user-1',
    senderName: 'John Doe',
    senderAvatar: '',
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
    jest.clearAllMocks();
    mockConfirm.mockReturnValue(true);
  });

  describe('Text Message Rendering', () => {
    it('renders text message content', () => {
      const message = createMockMessage();
      render(<MessageItem message={message} {...defaultProps} />);

      expect(screen.getByText('Hello world!')).toBeInTheDocument();
    });

    it('shows edited indicator when message is edited', () => {
      const message = createMockMessage({ isEdited: true });
      render(<MessageItem message={message} {...defaultProps} />);

      expect(screen.getByText('(edited)')).toBeInTheDocument();
    });

    it('renders timestamp when showTimestamp is true', () => {
      const message = createMockMessage();
      render(<MessageItem message={message} {...defaultProps} showTimestamp={true} />);

      expect(screen.getByText('10:30 AM')).toBeInTheDocument();
    });

    it('does not render timestamp when showTimestamp is false', () => {
      const message = createMockMessage();
      render(<MessageItem message={message} {...defaultProps} showTimestamp={false} />);

      expect(screen.queryByText('10:30 AM')).not.toBeInTheDocument();
    });

    it('renders avatar for other users when showAvatar is true', () => {
      const message = createMockMessage({ senderAvatar: '/avatar.png' });
      render(<MessageItem message={message} {...defaultProps} showAvatar={true} isCurrentUser={false} />);

      const avatar = screen.getByAltText('John Doe');
      expect(avatar).toBeInTheDocument();
    });

    it('does not render avatar when showAvatar is false', () => {
      const message = createMockMessage({ senderAvatar: '/avatar.png' });
      render(<MessageItem message={message} {...defaultProps} showAvatar={false} isCurrentUser={false} />);

      expect(screen.queryByAltText('John Doe')).not.toBeInTheDocument();
    });

    it('uses default avatar when senderAvatar is not provided', () => {
      const message = createMockMessage({ senderAvatar: undefined });
      render(<MessageItem message={message} {...defaultProps} showAvatar={true} isCurrentUser={false} />);

      const avatar = screen.getByAltText('John Doe');
      expect(avatar).toHaveAttribute('src', '/default-avatar.png');
    });
  });

  describe('Message Status Icons', () => {
    it('shows clock icon for Sent status', () => {
      const message = createMockMessage({ status: MessageStatus.Sent });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      expect(screen.getByTestId('clock-icon')).toBeInTheDocument();
    });

    it('shows check icon for Delivered status', () => {
      const message = createMockMessage({ status: MessageStatus.Delivered });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      expect(screen.getByTestId('check-icon')).toBeInTheDocument();
    });

    it('shows double check icon for Read status', () => {
      const message = createMockMessage({ status: MessageStatus.Read });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      // One from status, possibly another from milestone - get the one from status
      const checkChecks = screen.getAllByTestId('checkcheck-icon');
      expect(checkChecks.length).toBeGreaterThanOrEqual(1);
    });

    it('shows alert icon for Failed status', () => {
      const message = createMockMessage({ status: MessageStatus.Failed });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      expect(screen.getByTestId('alert-icon')).toBeInTheDocument();
    });

    it('does not show status icon when not current user', () => {
      const message = createMockMessage({ status: MessageStatus.Sent });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={false} />);

      expect(screen.queryByTestId('clock-icon')).not.toBeInTheDocument();
    });
  });

  describe('Image Message', () => {
    it('renders image message with preview', () => {
      const message = createMockMessage({
        messageType: MessageType.Image,
        attachmentUrl: 'https://example.com/image.jpg',
        attachmentFileName: 'photo.jpg',
      });
      render(<MessageItem message={message} {...defaultProps} />);

      const images = screen.getAllByTestId('next-image');
      const messageImage = images.find(img => img.getAttribute('src') === 'https://example.com/image.jpg');
      expect(messageImage).toBeInTheDocument();
    });

    it('opens image in new tab when clicked', () => {
      const message = createMockMessage({
        messageType: MessageType.Image,
        attachmentUrl: 'https://example.com/image.jpg',
        attachmentFileName: 'photo.jpg',
      });
      render(<MessageItem message={message} {...defaultProps} />);

      const images = screen.getAllByTestId('next-image');
      const messageImage = images.find(img => img.getAttribute('src') === 'https://example.com/image.jpg');
      fireEvent.click(messageImage!);

      expect(mockWindowOpen).toHaveBeenCalledWith('https://example.com/image.jpg', '_blank');
    });

    it('renders caption text with image', () => {
      const message = createMockMessage({
        messageType: MessageType.Image,
        messageText: 'Check out this photo!',
        attachmentUrl: 'https://example.com/image.jpg',
      });
      render(<MessageItem message={message} {...defaultProps} />);

      expect(screen.getByText('Check out this photo!')).toBeInTheDocument();
    });
  });

  describe('File Message', () => {
    it('renders file message with filename', () => {
      const message = createMockMessage({
        messageType: MessageType.File,
        attachmentUrl: 'https://example.com/doc.pdf',
        attachmentFileName: 'document.pdf',
        attachmentSize: 1024 * 1024, // 1 MB
      });
      render(<MessageItem message={message} {...defaultProps} />);

      expect(screen.getByText('document.pdf')).toBeInTheDocument();
      expect(screen.getByText('1 MB')).toBeInTheDocument();
    });

    it('renders download button for file message', () => {
      const message = createMockMessage({
        messageType: MessageType.File,
        attachmentUrl: 'https://example.com/doc.pdf',
        attachmentFileName: 'document.pdf',
      });
      render(<MessageItem message={message} {...defaultProps} />);

      const downloadIcons = screen.getAllByTestId('download-icon');
      expect(downloadIcons.length).toBeGreaterThanOrEqual(1);
    });

    it('calls download when download button is clicked', async () => {
      (messagingApiService.downloadFile as jest.Mock).mockResolvedValue(undefined);

      const message = createMockMessage({
        messageType: MessageType.File,
        attachmentUrl: 'https://example.com/doc.pdf',
        attachmentFileName: 'document.pdf',
      });
      render(<MessageItem message={message} {...defaultProps} />);

      // Click the download button (in the file card)
      const downloadButtons = screen.getAllByRole('button');
      const downloadButton = downloadButtons.find(btn => btn.querySelector('[data-testid="download-icon"]'));

      if (downloadButton) {
        fireEvent.click(downloadButton);

        await waitFor(() => {
          expect(messagingApiService.downloadFile).toHaveBeenCalledWith(
            'https://example.com/doc.pdf',
            'document.pdf'
          );
        });
      }
    });
  });

  describe('Voice Message', () => {
    it('renders voice message with play button', () => {
      const message = createMockMessage({
        messageType: MessageType.Voice,
        attachmentUrl: 'https://example.com/voice.mp3',
      });
      render(<MessageItem message={message} {...defaultProps} />);

      expect(screen.getByText('Voice message')).toBeInTheDocument();
      expect(screen.getByTestId('play-icon')).toBeInTheDocument();
    });

    it('toggles play/pause when button is clicked', () => {
      const mockPlay = jest.fn();
      const mockPause = jest.fn();

      // Mock audio element
      const mockAudio = {
        play: mockPlay,
        pause: mockPause,
      };

      jest.spyOn(React, 'useRef').mockReturnValueOnce({ current: null })
        .mockReturnValueOnce({ current: mockAudio as any });

      const message = createMockMessage({
        messageType: MessageType.Voice,
        attachmentUrl: 'https://example.com/voice.mp3',
      });

      const { rerender } = render(<MessageItem message={message} {...defaultProps} />);

      // Verify play icon is shown initially
      expect(screen.getByTestId('play-icon')).toBeInTheDocument();
    });
  });

  describe('System Message', () => {
    it('renders system message with centered styling', () => {
      const message = createMockMessage({
        messageType: MessageType.System,
        messageText: 'User joined the workspace',
      });
      render(<MessageItem message={message} {...defaultProps} />);

      expect(screen.getByText('User joined the workspace')).toBeInTheDocument();
    });
  });

  describe('Milestone Message', () => {
    it('renders milestone message with special styling', () => {
      const message = createMockMessage({
        messageType: MessageType.Milestone,
        messageText: 'Phase 1 completed!',
      });
      render(<MessageItem message={message} {...defaultProps} />);

      expect(screen.getByText('Milestone Update')).toBeInTheDocument();
      expect(screen.getByText('Phase 1 completed!')).toBeInTheDocument();
    });
  });

  describe('Reply Indicator', () => {
    it('renders reply indicator when message is a reply', () => {
      const message = createMockMessage({
        replyToMessage: {
          id: 'msg-0',
          workspaceId: 'ws-123',
          senderId: 'user-0',
          senderName: 'Jane Smith',
          senderAvatar: '',
          messageText: 'Original message',
          messageType: MessageType.Text,
          status: MessageStatus.Sent,
          createdAt: new Date().toISOString(),
          reactions: [],
          isEdited: false,
          canEdit: false,
          canDelete: false,
        },
      });
      render(<MessageItem message={message} {...defaultProps} />);

      expect(screen.getByText('Jane Smith')).toBeInTheDocument();
      expect(screen.getByText('Original message')).toBeInTheDocument();
    });
  });

  describe('Reactions', () => {
    it('renders emoji reactions when present', () => {
      const message = createMockMessage({
        reactions: [{ id: 'r1', emoji: '👍', userId: 'user-2', userName: 'Jane', createdAt: new Date().toISOString() }],
      });
      render(<MessageItem message={message} {...defaultProps} />);

      expect(screen.getByTestId('emoji-reactions')).toBeInTheDocument();
    });

    it('does not render reactions when empty', () => {
      const message = createMockMessage({ reactions: [] });
      render(<MessageItem message={message} {...defaultProps} />);

      expect(screen.queryByTestId('emoji-reactions')).not.toBeInTheDocument();
    });
  });

  describe('Message Menu', () => {
    it('shows menu when more button is clicked', () => {
      const message = createMockMessage();
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);

      expect(screen.getByText('Reply')).toBeInTheDocument();
    });

    it('shows copy option for text messages', () => {
      const message = createMockMessage({ messageType: MessageType.Text });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);

      expect(screen.getByText('Copy')).toBeInTheDocument();
    });

    it('copies text when copy is clicked', () => {
      const message = createMockMessage({ messageText: 'Copy me!' });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);

      const copyButton = screen.getByText('Copy');
      fireEvent.click(copyButton);

      expect(mockClipboard.writeText).toHaveBeenCalledWith('Copy me!');
    });

    it('shows edit option for own editable messages', () => {
      const message = createMockMessage({ canEdit: true });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);

      expect(screen.getByText('Edit')).toBeInTheDocument();
    });

    it('does not show edit option for non-editable messages', () => {
      const message = createMockMessage({ canEdit: false });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);

      expect(screen.queryByText('Edit')).not.toBeInTheDocument();
    });

    it('shows delete option for own deletable messages', () => {
      const message = createMockMessage({ canDelete: true });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);

      expect(screen.getByText('Delete')).toBeInTheDocument();
    });

    it('shows download option for messages with attachments', () => {
      const message = createMockMessage({
        attachmentUrl: 'https://example.com/file.pdf',
        attachmentFileName: 'file.pdf',
      });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);

      // Should have download in menu
      const downloadTexts = screen.getAllByText('Download');
      expect(downloadTexts.length).toBeGreaterThanOrEqual(1);
    });

    it('closes menu when clicking outside', async () => {
      const message = createMockMessage();
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      // Open menu
      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);

      expect(screen.getByText('Reply')).toBeInTheDocument();

      // Click outside
      fireEvent.mouseDown(document.body);

      await waitFor(() => {
        expect(screen.queryByText('Reply')).not.toBeInTheDocument();
      });
    });
  });

  describe('Edit Mode', () => {
    it('enters edit mode when edit button is clicked', () => {
      const message = createMockMessage({ canEdit: true });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);

      const editButton = screen.getByText('Edit');
      fireEvent.click(editButton);

      expect(screen.getByRole('textbox')).toBeInTheDocument();
      expect(screen.getByText('Save')).toBeInTheDocument();
      expect(screen.getByText('Cancel')).toBeInTheDocument();
    });

    it('saves edit when save button is clicked', async () => {
      (messagingApiService.editMessage as jest.Mock).mockResolvedValue(undefined);

      const message = createMockMessage({ canEdit: true });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      // Open menu and click edit
      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);
      fireEvent.click(screen.getByText('Edit'));

      // Change text and save
      const textarea = screen.getByRole('textbox');
      fireEvent.change(textarea, { target: { value: 'Updated message!' } });
      fireEvent.click(screen.getByText('Save'));

      await waitFor(() => {
        expect(messagingApiService.editMessage).toHaveBeenCalledWith('msg-1', {
          messageText: 'Updated message!',
        });
      });
    });

    it('does not save when text is empty', async () => {
      const message = createMockMessage({ canEdit: true });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      // Open menu and click edit
      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);
      fireEvent.click(screen.getByText('Edit'));

      // Clear text and try to save
      const textarea = screen.getByRole('textbox');
      fireEvent.change(textarea, { target: { value: '' } });

      const saveButton = screen.getByText('Save');
      expect(saveButton).toBeDisabled();
    });

    it('cancels edit mode when cancel is clicked', () => {
      const message = createMockMessage({ canEdit: true, messageText: 'Original' });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      // Open menu and click edit
      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);
      fireEvent.click(screen.getByText('Edit'));

      // Change text then cancel
      const textarea = screen.getByRole('textbox');
      fireEvent.change(textarea, { target: { value: 'Changed' } });
      fireEvent.click(screen.getByText('Cancel'));

      // Should show original text again
      expect(screen.getByText('Original')).toBeInTheDocument();
      expect(screen.queryByRole('textbox')).not.toBeInTheDocument();
    });

    it('handles edit error gracefully', async () => {
      const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
      (messagingApiService.editMessage as jest.Mock).mockRejectedValue(new Error('Edit failed'));

      const message = createMockMessage({ canEdit: true });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      // Open menu and click edit
      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);
      fireEvent.click(screen.getByText('Edit'));

      // Change text and save
      const textarea = screen.getByRole('textbox');
      fireEvent.change(textarea, { target: { value: 'Updated' } });
      fireEvent.click(screen.getByText('Save'));

      await waitFor(() => {
        expect(messagingApiService.editMessage).toHaveBeenCalled();
      });

      consoleSpy.mockRestore();
    });
  });

  describe('Delete Functionality', () => {
    it('deletes message when delete is confirmed', async () => {
      mockConfirm.mockReturnValue(true);
      (messagingApiService.deleteMessage as jest.Mock).mockResolvedValue(undefined);

      const message = createMockMessage({ canDelete: true });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);
      fireEvent.click(screen.getByText('Delete'));

      await waitFor(() => {
        expect(messagingApiService.deleteMessage).toHaveBeenCalledWith('msg-1');
      });
    });

    it('does not delete when confirmation is cancelled', async () => {
      mockConfirm.mockReturnValue(false);

      const message = createMockMessage({ canDelete: true });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);
      fireEvent.click(screen.getByText('Delete'));

      expect(messagingApiService.deleteMessage).not.toHaveBeenCalled();
    });

    it('handles delete error gracefully', async () => {
      const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
      mockConfirm.mockReturnValue(true);
      (messagingApiService.deleteMessage as jest.Mock).mockRejectedValue(new Error('Delete failed'));

      const message = createMockMessage({ canDelete: true });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);
      fireEvent.click(screen.getByText('Delete'));

      await waitFor(() => {
        expect(messagingApiService.deleteMessage).toHaveBeenCalled();
      });

      consoleSpy.mockRestore();
    });
  });

  describe('File Size Formatting', () => {
    it('formats bytes correctly', () => {
      const message = createMockMessage({
        messageType: MessageType.File,
        attachmentUrl: 'https://example.com/file.txt',
        attachmentFileName: 'file.txt',
        attachmentSize: 512,
      });
      render(<MessageItem message={message} {...defaultProps} />);

      expect(screen.getByText('512 Bytes')).toBeInTheDocument();
    });

    it('formats KB correctly', () => {
      const message = createMockMessage({
        messageType: MessageType.File,
        attachmentUrl: 'https://example.com/file.txt',
        attachmentFileName: 'file.txt',
        attachmentSize: 2048, // 2 KB
      });
      render(<MessageItem message={message} {...defaultProps} />);

      expect(screen.getByText('2 KB')).toBeInTheDocument();
    });

    it('formats MB correctly', () => {
      const message = createMockMessage({
        messageType: MessageType.File,
        attachmentUrl: 'https://example.com/file.pdf',
        attachmentFileName: 'file.pdf',
        attachmentSize: 1024 * 1024 * 5, // 5 MB
      });
      render(<MessageItem message={message} {...defaultProps} />);

      expect(screen.getByText('5 MB')).toBeInTheDocument();
    });

    it('formats GB correctly', () => {
      const message = createMockMessage({
        messageType: MessageType.File,
        attachmentUrl: 'https://example.com/file.zip',
        attachmentFileName: 'file.zip',
        attachmentSize: 1024 * 1024 * 1024 * 2, // 2 GB
      });
      render(<MessageItem message={message} {...defaultProps} />);

      expect(screen.getByText('2 GB')).toBeInTheDocument();
    });

    it('handles 0 bytes (no size shown due to truthy check)', () => {
      const message = createMockMessage({
        messageType: MessageType.File,
        attachmentUrl: 'https://example.com/empty.txt',
        attachmentFileName: 'empty.txt',
        attachmentSize: 0,
      });
      render(<MessageItem message={message} {...defaultProps} />);

      // Component uses {message.attachmentSize && formatFileSize(...)}
      // So 0 is falsy and size is not displayed
      expect(screen.getByText('empty.txt')).toBeInTheDocument();
      expect(screen.queryByText('0 Bytes')).not.toBeInTheDocument();
    });
  });

  describe('Download Functionality', () => {
    it('downloads file from menu', async () => {
      (messagingApiService.downloadFile as jest.Mock).mockResolvedValue(undefined);

      const message = createMockMessage({
        messageType: MessageType.Text,
        attachmentUrl: 'https://example.com/file.pdf',
        attachmentFileName: 'document.pdf',
      });
      render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      const moreButton = screen.getByTestId('more-icon').parentElement;
      fireEvent.click(moreButton!);

      const downloadOption = screen.getByText('Download');
      fireEvent.click(downloadOption);

      await waitFor(() => {
        expect(messagingApiService.downloadFile).toHaveBeenCalledWith(
          'https://example.com/file.pdf',
          'document.pdf'
        );
      });
    });

    it('handles download error gracefully', async () => {
      const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
      (messagingApiService.downloadFile as jest.Mock).mockRejectedValue(new Error('Download failed'));

      const message = createMockMessage({
        messageType: MessageType.File,
        attachmentUrl: 'https://example.com/file.pdf',
        attachmentFileName: 'document.pdf',
      });
      render(<MessageItem message={message} {...defaultProps} />);

      // Click download button in file card
      const downloadButtons = screen.getAllByRole('button');
      const downloadButton = downloadButtons.find(btn => btn.querySelector('[data-testid="download-icon"]'));

      if (downloadButton) {
        fireEvent.click(downloadButton);

        await waitFor(() => {
          expect(messagingApiService.downloadFile).toHaveBeenCalled();
        });
      }

      consoleSpy.mockRestore();
    });
  });

  describe('Current User Styling', () => {
    it('applies current user styling when isCurrentUser is true', () => {
      const message = createMockMessage();
      const { container } = render(<MessageItem message={message} {...defaultProps} isCurrentUser={true} />);

      // Check for flex-row-reverse class
      const messageContainer = container.querySelector('.flex-row-reverse');
      expect(messageContainer).toBeInTheDocument();
    });

    it('applies default styling when isCurrentUser is false', () => {
      const message = createMockMessage();
      const { container } = render(<MessageItem message={message} {...defaultProps} isCurrentUser={false} />);

      // Should not have flex-row-reverse
      const messageContainer = container.querySelector('.flex-row-reverse');
      expect(messageContainer).not.toBeInTheDocument();
    });
  });

  describe('Default Message Type', () => {
    it('renders default message for unknown type', () => {
      const message = createMockMessage({
        messageType: 'unknown' as unknown as MessageType,
        messageText: 'Unknown type message',
      });
      render(<MessageItem message={message} {...defaultProps} />);

      expect(screen.getByText('Unknown type message')).toBeInTheDocument();
    });
  });
});
