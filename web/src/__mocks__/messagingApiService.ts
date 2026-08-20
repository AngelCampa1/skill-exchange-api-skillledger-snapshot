/**
 * Mock implementation of messaging API service for testing
 */

import { Message, MessageType, MessageStatus } from '../types/messaging';

const mockMessage: Message = {
  id: 'msg-1',
  workspaceId: 'workspace-1',
  senderId: 'user-1',
  senderName: 'John Doe',
  senderAvatar: '/avatar1.jpg',
  messageText: 'Hello everyone!',
  messageType: MessageType.Text,
  status: MessageStatus.Sent,
  isEdited: false,
  canEdit: true,
  canDelete: true,
  createdAt: '2025-09-08T15:30:00Z',
  reactions: []
};

export const mockMessagingApiService = {
  getMessageHistory: jest.fn().mockResolvedValue({
    messages: [mockMessage],
    totalCount: 1,
    hasMore: false,
    nextCursor: null
  }),
  sendMessage: jest.fn().mockResolvedValue(mockMessage),
  editMessage: jest.fn().mockResolvedValue(mockMessage),
  deleteMessage: jest.fn().mockResolvedValue(void 0),
  addReaction: jest.fn().mockResolvedValue(void 0),
  removeReaction: jest.fn().mockResolvedValue(void 0),
  searchMessages: jest.fn().mockResolvedValue({
    messages: [mockMessage],
    totalCount: 1,
    hasMore: false,
    nextCursor: null,
    highlightedTerms: ['hello']
  }),
  uploadFile: jest.fn().mockResolvedValue({
    url: 'https://example.com/file.jpg',
    name: 'file.jpg',
    size: 12345,
    mimeType: 'image/jpeg'
  }),
  getMessageStats: jest.fn().mockResolvedValue({
    totalMessages: 100,
    messagesThisWeek: 25,
    averageResponseTime: 300,
    activeUsers: 5
  }),
  markMessagesAsRead: jest.fn().mockResolvedValue(void 0)
};

export const messagingApiService = mockMessagingApiService;