/**
 * TypeScript types for messaging system
 * Mirrors backend DTOs for consistent data handling
 */

export enum MessageType {
  Text = 0,
  File = 1,
  System = 2,
  Milestone = 3,
  Image = 4,
  Voice = 5
}

export enum MessageStatus {
  Sent = 0,
  Delivered = 1,
  Read = 2,
  Failed = 3,
  Deleted = 4
}

export interface MessageReaction {
  id: string;
  userId: string;
  userName: string;
  emoji: string;
  createdAt: string;
}

export interface Message {
  id: string;
  workspaceId: string;
  senderId: string;
  senderName: string;
  senderAvatar: string;
  messageText?: string;
  messageType: MessageType;
  status: MessageStatus;
  isEdited: boolean;
  createdAt: string;
  editedAt?: string;
  readAt?: string;
  
  // Reply information
  replyToMessageId?: string;
  replyToMessage?: Message;
  
  // Attachment information
  attachmentUrl?: string;
  attachmentFileName?: string;
  attachmentSize?: number;
  attachmentMimeType?: string;
  
  // Reactions
  reactions: MessageReaction[];
  
  // Permission flags
  canEdit: boolean;
  canDelete: boolean;
}

export interface SendMessageRequest {
  workspaceId: string;
  messageText?: string;
  messageType: MessageType;
  replyToMessageId?: string;
  
  // File attachment properties
  attachmentUrl?: string;
  attachmentFileName?: string;
  attachmentSize?: number;
  attachmentMimeType?: string;
  
  ipAddress?: string;
  userAgent?: string;
}

export interface EditMessageRequest {
  messageText: string;
  ipAddress?: string;
  userAgent?: string;
}

export interface AddReactionRequest {
  emoji: string;
  ipAddress?: string;
}

export interface MessageHistoryRequest {
  workspaceId: string;
  pageNumber?: number;
  pageSize?: number;
  beforeDate?: string;
  afterDate?: string;
  searchQuery?: string;
  messageType?: MessageType;
  senderId?: string;
}

export interface MessageHistoryResponse {
  messages: Message[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface TypingIndicator {
  userId: string;
  userName: string;
  lastTypingAt: string;
  isActive: boolean;
}

export interface SearchMessagesRequest {
  workspaceId: string;
  query: string;
  pageNumber?: number;
  pageSize?: number;
  messageType?: MessageType;
  fromDate?: string;
  toDate?: string;
}

export interface SearchMessagesResponse {
  messages: Message[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  query: string;
  searchDuration: string;
}

export interface MessageStats {
  workspaceId: string;
  totalMessages: number;
  unreadMessages: number;
  lastMessageAt?: string;
  messagesByType: Record<MessageType, number>;
  topReactions: Record<string, number>;
}

// UI-specific types
export interface ConnectionState {
  status: 'connecting' | 'connected' | 'reconnecting' | 'disconnected' | 'error';
  lastConnectedAt?: string;
  reconnectAttempts: number;
  error?: string;
}

export interface MessageNotification {
  id: string;
  message: Message;
  timestamp: string;
  isRead: boolean;
}

export interface FileUploadProgress {
  id: string;
  fileName: string;
  fileSize: number;
  progress: number;
  status: 'uploading' | 'completed' | 'error';
  error?: string;
}

export interface MessageSearchResult {
  message: Message;
  highlights: string[];
  context: {
    before: Message[];
    after: Message[];
  };
}

export interface EmojiReactionGroup {
  emoji: string;
  count: number;
  users: Array<{
    id: string;
    name: string;
  }>;
  hasUserReacted: boolean;
}

// Event types for SignalR
export interface SignalREvents {
  'MessageReceived': (message: Message) => void;
  'MessageUpdated': (message: Message) => void;
  'MessageDeleted': (messageId: string) => void;
  'ReactionAdded': (messageId: string, reaction: MessageReaction) => void;
  'ReactionRemoved': (messageId: string, userId: string, emoji: string) => void;
  'UserStartedTyping': (workspaceId: string, user: TypingIndicator) => void;
  'UserStoppedTyping': (workspaceId: string, userId: string) => void;
  'MessageRead': (messageId: string, userId: string, readAt: string) => void;
  'UserJoined': (workspaceId: string, userId: string, userName: string) => void;
  'UserLeft': (workspaceId: string, userId: string, userName: string) => void;
  // BUG-FE-017 FIX: Add dedicated event for connection state changes
  'ConnectionStateChanged': (state: ConnectionState) => void;
}