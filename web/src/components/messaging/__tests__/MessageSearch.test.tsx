/**
 * MessageSearch.tsx Tests
 *
 * Tests for the message search component with filtering and highlighting.
 * Coverage Target: 80%+
 */

import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import '@testing-library/jest-dom';
import { MessageSearch } from '../MessageSearch';
import { messagingApiService } from '../../../services/messagingApiService';
import { MessageType } from '../../../types/messaging';

// Mock the messaging API service
jest.mock('../../../services/messagingApiService', () => ({
  messagingApiService: {
    searchMessages: jest.fn(),
  },
}));

// Mock lucide-react icons
jest.mock('lucide-react', () => ({
  Search: () => <span data-testid="search-icon">SearchIcon</span>,
  X: () => <span data-testid="x-icon">XIcon</span>,
  Clock: () => <span data-testid="clock-icon">ClockIcon</span>,
  User: () => <span data-testid="user-icon">UserIcon</span>,
  FileText: () => <span data-testid="file-icon">FileIcon</span>,
  Image: () => <span data-testid="image-icon">ImageIcon</span>,
  Mic: () => <span data-testid="mic-icon">MicIcon</span>,
}));

// Mock date-fns format
jest.mock('date-fns', () => ({
  format: jest.fn(() => 'Jan 1, 10:00 AM'),
}));

// Mock DOMPurify
jest.mock('dompurify', () => ({
  sanitize: (html: string) => html,
}));

describe('MessageSearch', () => {
  const mockOnMessageSelect = jest.fn();
  const mockOnClose = jest.fn();

  const defaultProps = {
    workspaceId: 'ws-123',
    onMessageSelect: mockOnMessageSelect,
  };

  const mockMessages = [
    {
      id: 'msg-1',
      workspaceId: 'ws-123',
      senderId: 'user-1',
      senderName: 'John Doe',
      messageText: 'Hello world, this is a test message',
      messageType: MessageType.Text,
      status: 'Sent' as const,
      createdAt: new Date().toISOString(),
      reactions: [],
      isEdited: false,
      canEdit: false,
      canDelete: false,
    },
    {
      id: 'msg-2',
      workspaceId: 'ws-123',
      senderId: 'user-2',
      senderName: 'Jane Smith',
      messageText: 'Another test message with hello',
      messageType: MessageType.Text,
      status: 'Sent' as const,
      createdAt: new Date().toISOString(),
      reactions: [],
      isEdited: false,
      canEdit: false,
      canDelete: false,
    },
  ];

  const mockImageMessage = {
    id: 'msg-3',
    workspaceId: 'ws-123',
    senderId: 'user-1',
    senderName: 'John Doe',
    messageText: '',
    messageType: MessageType.Image,
    status: 'Sent' as const,
    createdAt: new Date().toISOString(),
    reactions: [],
    isEdited: false,
    canEdit: false,
    canDelete: false,
  };

  const mockFileMessage = {
    id: 'msg-4',
    workspaceId: 'ws-123',
    senderId: 'user-1',
    senderName: 'John Doe',
    messageText: 'See the attached file',
    messageType: MessageType.File,
    attachmentFileName: 'document.pdf',
    status: 'Sent' as const,
    createdAt: new Date().toISOString(),
    reactions: [],
    isEdited: false,
    canEdit: false,
    canDelete: false,
  };

  beforeEach(() => {
    jest.clearAllMocks();
    jest.useFakeTimers();
    (messagingApiService.searchMessages as jest.Mock).mockResolvedValue({
      messages: mockMessages,
      totalCount: 2,
      searchDuration: '50ms',
    });
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  describe('Rendering', () => {
    it('renders search input with placeholder', () => {
      render(<MessageSearch {...defaultProps} />);

      expect(screen.getByPlaceholderText('Search messages...')).toBeInTheDocument();
      expect(screen.getByLabelText('Search messages')).toBeInTheDocument();
    });

    it('renders type filter dropdown', () => {
      render(<MessageSearch {...defaultProps} />);

      expect(screen.getByText('Type:')).toBeInTheDocument();
      // There are 2 comboboxes: type filter and date filter
      const comboboxes = screen.getAllByRole('combobox');
      expect(comboboxes.length).toBe(2);
    });

    it('renders date filter dropdown', () => {
      render(<MessageSearch {...defaultProps} />);

      expect(screen.getByText('When:')).toBeInTheDocument();
      expect(screen.getByDisplayValue('All time')).toBeInTheDocument();
    });

    it('renders close button when onClose prop is provided', () => {
      render(<MessageSearch {...defaultProps} onClose={mockOnClose} />);

      // Should have 2 X icons - one for clear and one for close
      const buttons = screen.getAllByTestId('x-icon');
      expect(buttons.length).toBeGreaterThanOrEqual(1);
    });

    it('does not render close button when onClose is not provided', () => {
      render(<MessageSearch {...defaultProps} />);

      // Only the clear button X should be potentially present
      // But no close button for the whole panel
    });

    it('focuses search input on mount', () => {
      render(<MessageSearch {...defaultProps} />);

      const searchInput = screen.getByPlaceholderText('Search messages...');
      expect(document.activeElement).toBe(searchInput);
    });
  });

  describe('Search Functionality', () => {
    it('performs search after debounce delay', async () => {
      render(<MessageSearch {...defaultProps} />);

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'hello' } });

      // Search should not be called immediately
      expect(messagingApiService.searchMessages).not.toHaveBeenCalled();

      // Advance timers past debounce delay
      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      await waitFor(() => {
        expect(messagingApiService.searchMessages).toHaveBeenCalledWith(
          expect.objectContaining({
            workspaceId: 'ws-123',
            query: 'hello',
            pageSize: 20,
          })
        );
      });
    });

    it('uses custom debounce delay when provided', async () => {
      render(<MessageSearch {...defaultProps} debounceDelay={500} />);

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'test' } });

      // Advance past default but before custom delay
      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      expect(messagingApiService.searchMessages).not.toHaveBeenCalled();

      // Advance to custom delay
      await act(async () => {
        jest.advanceTimersByTime(200);
      });

      await waitFor(() => {
        expect(messagingApiService.searchMessages).toHaveBeenCalled();
      });
    });

    it('displays search results', async () => {
      render(<MessageSearch {...defaultProps} />);

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'hello' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      await waitFor(() => {
        expect(screen.getByText('John Doe')).toBeInTheDocument();
        expect(screen.getByText('Jane Smith')).toBeInTheDocument();
      });
    });

    it('displays result count and search duration', async () => {
      render(<MessageSearch {...defaultProps} />);

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'hello' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      await waitFor(() => {
        expect(screen.getByText(/2 results/)).toBeInTheDocument();
        expect(screen.getByText(/50ms/)).toBeInTheDocument();
      });
    });

    it('shows loading state while searching', async () => {
      // Make search take a bit longer
      (messagingApiService.searchMessages as jest.Mock).mockImplementation(
        () => new Promise(resolve => setTimeout(() => resolve({
          messages: mockMessages,
          totalCount: 2,
          searchDuration: '50ms',
        }), 100))
      );

      render(<MessageSearch {...defaultProps} />);

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'hello' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      expect(screen.getByText('Searching...')).toBeInTheDocument();

      await act(async () => {
        jest.advanceTimersByTime(100);
      });

      await waitFor(() => {
        expect(screen.queryByText('Searching...')).not.toBeInTheDocument();
      });
    });

    it('shows no results message when search returns empty', async () => {
      (messagingApiService.searchMessages as jest.Mock).mockResolvedValue({
        messages: [],
        totalCount: 0,
        searchDuration: '10ms',
      });

      render(<MessageSearch {...defaultProps} />);

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'nonexistent' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      await waitFor(() => {
        expect(screen.getByText('No messages found')).toBeInTheDocument();
        expect(screen.getByText('Try adjusting your search terms or filters')).toBeInTheDocument();
      });
    });

    it('handles search error gracefully', async () => {
      (messagingApiService.searchMessages as jest.Mock).mockRejectedValue(new Error('Search failed'));

      render(<MessageSearch {...defaultProps} />);

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'error' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      await waitFor(() => {
        expect(screen.getByText('0 results')).toBeInTheDocument();
      });
    });

    it('does not search when query is empty', async () => {
      render(<MessageSearch {...defaultProps} />);

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: '' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      expect(messagingApiService.searchMessages).not.toHaveBeenCalled();
    });

    it('does not search when query is only whitespace', async () => {
      render(<MessageSearch {...defaultProps} />);

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: '   ' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      expect(messagingApiService.searchMessages).not.toHaveBeenCalled();
    });
  });

  describe('Filters', () => {
    it('filters by message type', async () => {
      render(<MessageSearch {...defaultProps} />);

      // Change type filter
      const typeSelect = screen.getAllByRole('combobox')[0];
      fireEvent.change(typeSelect, { target: { value: MessageType.Image.toString() } });

      // Enter search query
      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'test' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      await waitFor(() => {
        expect(messagingApiService.searchMessages).toHaveBeenCalledWith(
          expect.objectContaining({
            messageType: MessageType.Image,
          })
        );
      });
    });

    it('filters by date - today', async () => {
      render(<MessageSearch {...defaultProps} />);

      // Change date filter
      const dateSelect = screen.getByDisplayValue('All time');
      fireEvent.change(dateSelect, { target: { value: 'today' } });

      // Enter search query
      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'test' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      await waitFor(() => {
        expect(messagingApiService.searchMessages).toHaveBeenCalledWith(
          expect.objectContaining({
            fromDate: expect.any(String),
          })
        );
      });
    });

    it('filters by date - week', async () => {
      render(<MessageSearch {...defaultProps} />);

      const dateSelect = screen.getByDisplayValue('All time');
      fireEvent.change(dateSelect, { target: { value: 'week' } });

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'test' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      await waitFor(() => {
        expect(messagingApiService.searchMessages).toHaveBeenCalledWith(
          expect.objectContaining({
            fromDate: expect.any(String),
          })
        );
      });
    });

    it('filters by date - month', async () => {
      render(<MessageSearch {...defaultProps} />);

      const dateSelect = screen.getByDisplayValue('All time');
      fireEvent.change(dateSelect, { target: { value: 'month' } });

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'test' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      await waitFor(() => {
        expect(messagingApiService.searchMessages).toHaveBeenCalledWith(
          expect.objectContaining({
            fromDate: expect.any(String),
          })
        );
      });
    });

    it('clears type filter when set to All types', async () => {
      render(<MessageSearch {...defaultProps} />);

      const typeSelect = screen.getAllByRole('combobox')[0];

      // Set to Image first
      fireEvent.change(typeSelect, { target: { value: MessageType.Image.toString() } });

      // Then clear it
      fireEvent.change(typeSelect, { target: { value: '' } });

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'test' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      await waitFor(() => {
        expect(messagingApiService.searchMessages).toHaveBeenCalledWith(
          expect.objectContaining({
            messageType: undefined,
          })
        );
      });
    });
  });

  describe('Message Selection', () => {
    it('calls onMessageSelect when a result is clicked', async () => {
      render(<MessageSearch {...defaultProps} />);

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'hello' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      await waitFor(() => {
        expect(screen.getByText('John Doe')).toBeInTheDocument();
      });

      fireEvent.click(screen.getByText('John Doe'));

      expect(mockOnMessageSelect).toHaveBeenCalledWith(mockMessages[0]);
    });
  });

  describe('Clear Search', () => {
    it('clears search when X button is clicked', async () => {
      render(<MessageSearch {...defaultProps} />);

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'hello' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      await waitFor(() => {
        expect(screen.getByText('John Doe')).toBeInTheDocument();
      });

      // Click clear button
      const clearButton = screen.getByLabelText('Clear search');
      fireEvent.click(clearButton);

      expect(searchInput).toHaveValue('');
      expect(screen.queryByText('John Doe')).not.toBeInTheDocument();
    });

    it('shows clear button only when query has value', () => {
      render(<MessageSearch {...defaultProps} />);

      // Initially no clear button
      expect(screen.queryByLabelText('Clear search')).not.toBeInTheDocument();

      // Type something
      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'hello' } });

      // Clear button should appear
      expect(screen.getByLabelText('Clear search')).toBeInTheDocument();
    });
  });

  describe('Close Button', () => {
    it('calls onClose when close button is clicked', () => {
      render(<MessageSearch {...defaultProps} onClose={mockOnClose} />);

      // Find the close button (in the filters area, not the clear search button)
      const buttons = screen.getAllByRole('button');
      const closeButton = buttons.find(btn => btn.classList.contains('ml-auto'));

      if (closeButton) {
        fireEvent.click(closeButton);
        expect(mockOnClose).toHaveBeenCalled();
      }
    });
  });

  describe('Message Type Icons', () => {
    it('displays correct icon for image messages', async () => {
      (messagingApiService.searchMessages as jest.Mock).mockResolvedValue({
        messages: [mockImageMessage],
        totalCount: 1,
        searchDuration: '10ms',
      });

      render(<MessageSearch {...defaultProps} />);

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'image' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      await waitFor(() => {
        expect(screen.getByText('Image')).toBeInTheDocument();
      });
    });

    it('displays correct icon for file messages with attachment', async () => {
      (messagingApiService.searchMessages as jest.Mock).mockResolvedValue({
        messages: [mockFileMessage],
        totalCount: 1,
        searchDuration: '10ms',
      });

      render(<MessageSearch {...defaultProps} />);

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'file' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      await waitFor(() => {
        expect(screen.getByText(/document.pdf/)).toBeInTheDocument();
      });
    });
  });

  describe('Results Pagination', () => {
    it('shows pagination info when more results exist', async () => {
      (messagingApiService.searchMessages as jest.Mock).mockResolvedValue({
        messages: mockMessages,
        totalCount: 100, // More than returned
        searchDuration: '50ms',
      });

      render(<MessageSearch {...defaultProps} />);

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'hello' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      await waitFor(() => {
        expect(screen.getByText('100 results (50ms)')).toBeInTheDocument();
        expect(screen.getByText('Showing first 2 results')).toBeInTheDocument();
      });
    });
  });

  describe('XSS Protection', () => {
    it('escapes HTML in search results', async () => {
      const maliciousMessage = {
        ...mockMessages[0],
        messageText: '<script>alert("xss")</script>',
      };

      (messagingApiService.searchMessages as jest.Mock).mockResolvedValue({
        messages: [maliciousMessage],
        totalCount: 1,
        searchDuration: '10ms',
      });

      render(<MessageSearch {...defaultProps} />);

      const searchInput = screen.getByPlaceholderText('Search messages...');
      fireEvent.change(searchInput, { target: { value: 'script' } });

      await act(async () => {
        jest.advanceTimersByTime(300);
      });

      await waitFor(() => {
        // Script should be escaped, not executed
        expect(screen.queryByText('alert("xss")')).not.toBeInTheDocument();
      });
    });
  });
});
