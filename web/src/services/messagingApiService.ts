import { logger } from '@/utils/logger';
/**
 * API service for messaging operations
 * Handles HTTP requests to the messaging endpoints
 */

import {
  Message,
  SendMessageRequest,
  EditMessageRequest,
  AddReactionRequest,
  MessageHistoryRequest,
  MessageHistoryResponse,
  SearchMessagesRequest,
  SearchMessagesResponse,
  MessageStats
} from '../types/messaging';
import {
  ConversationPreview,
  ConversationDetails
} from '../types/conversations';
import { fetchWithAuth, uploadFileWithAuth, downloadFileWithAuth } from '../utils/apiClient';

class MessagingApiService {
  private baseUrl = '/api';

  private async makeRequest<T>(
    url: string,
    options: RequestInit = {}
  ): Promise<T> {
    return fetchWithAuth<T>(`${this.baseUrl}${url}`, options);
  }

  /**
   * Send a new message
   */
  async sendMessage(request: SendMessageRequest): Promise<Message> {
    // Add client info (IP address is captured server-side from request headers)
    const requestWithClientInfo = {
      ...request,
      userAgent: navigator.userAgent
    };

    return this.makeRequest<Message>('/messaging/send', {
      method: 'POST',
      body: JSON.stringify(requestWithClientInfo),
    });
  }

  /**
   * Edit an existing message
   */
  async editMessage(messageId: string, request: EditMessageRequest): Promise<Message> {
    // IP address is captured server-side from request headers
    const requestWithClientInfo = {
      ...request,
      userAgent: navigator.userAgent
    };

    return this.makeRequest<Message>(`/messaging/${messageId}/edit`, {
      method: 'PUT',
      body: JSON.stringify(requestWithClientInfo),
    });
  }

  /**
   * Delete a message
   */
  async deleteMessage(messageId: string): Promise<void> {
    return this.makeRequest<void>(`/messaging/${messageId}`, {
      method: 'DELETE',
    });
  }

  /**
   * Get message history for a workspace
   */
  async getMessageHistory(request: MessageHistoryRequest): Promise<MessageHistoryResponse> {
    const params = new URLSearchParams();
    
    params.append('workspaceId', request.workspaceId);
    if (request.pageNumber) params.append('pageNumber', request.pageNumber.toString());
    if (request.pageSize) params.append('pageSize', request.pageSize.toString());
    if (request.beforeDate) params.append('beforeDate', request.beforeDate);
    if (request.afterDate) params.append('afterDate', request.afterDate);
    if (request.searchQuery) params.append('searchQuery', request.searchQuery);
    if (request.messageType !== undefined) params.append('messageType', request.messageType.toString());
    if (request.senderId) params.append('senderId', request.senderId);

    return this.makeRequest<MessageHistoryResponse>(`/messaging/history?${params.toString()}`);
  }

  /**
   * Search messages
   */
  async searchMessages(request: SearchMessagesRequest): Promise<SearchMessagesResponse> {
    const params = new URLSearchParams();
    
    params.append('workspaceId', request.workspaceId);
    params.append('query', request.query);
    if (request.pageNumber) params.append('pageNumber', request.pageNumber.toString());
    if (request.pageSize) params.append('pageSize', request.pageSize.toString());
    if (request.messageType !== undefined) params.append('messageType', request.messageType.toString());
    if (request.fromDate) params.append('fromDate', request.fromDate);
    if (request.toDate) params.append('toDate', request.toDate);

    return this.makeRequest<SearchMessagesResponse>(`/messaging/search?${params.toString()}`);
  }

  /**
   * Add reaction to a message
   */
  async addReaction(messageId: string, request: AddReactionRequest): Promise<void> {
    // IP address is captured server-side from request headers
    return this.makeRequest<void>(`/messaging/${messageId}/reactions`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  /**
   * Remove reaction from a message
   */
  async removeReaction(messageId: string, emoji: string): Promise<void> {
    return this.makeRequest<void>(`/messaging/${messageId}/reactions/${encodeURIComponent(emoji)}`, {
      method: 'DELETE',
    });
  }

  /**
   * Get message statistics for a workspace
   */
  async getMessageStats(workspaceId: string): Promise<MessageStats> {
    return this.makeRequest<MessageStats>(`/messaging/stats/${workspaceId}`);
  }

  /**
   * Mark message as read
   */
  async markMessageAsRead(messageId: string): Promise<void> {
    return this.makeRequest<void>(`/messaging/${messageId}/read`, {
      method: 'POST',
    });
  }

  /**
   * Upload file for messaging
   */
  async uploadFile(file: File, workspaceId: string): Promise<{
    url: string;
    fileName: string;
    size: number;
    mimeType: string;
  }> {
    return uploadFileWithAuth<{
      url: string;
      fileName: string;
      size: number;
      mimeType: string;
    }>(`${this.baseUrl}/messaging/upload`, file, { workspaceId });
  }

  /**
   * Download file attachment
   */
  async downloadFile(attachmentUrl: string, fileName: string): Promise<void> {
    try {
      await downloadFileWithAuth(attachmentUrl, fileName);
    } catch (error) {
      logger.error('File download failed:', error);
      throw error;
    }
  }

  // ============================================
  // Workspace/Conversation Methods
  // ============================================

  /**
   * Get all workspaces/conversations for the current user
   */
  async getMyWorkspaces(): Promise<ConversationPreview[]> {
    return this.makeRequest<ConversationPreview[]>('/workspace/my-workspaces');
  }

  /**
   * Get detailed workspace/conversation data
   */
  async getWorkspaceDetails(workspaceId: string): Promise<ConversationDetails> {
    return this.makeRequest<ConversationDetails>(`/workspace/${workspaceId}`);
  }

  /**
   * Get workspace by project ID
   */
  async getWorkspaceByProject(projectId: string): Promise<ConversationDetails> {
    return this.makeRequest<ConversationDetails>(`/workspace/project/${projectId}`);
  }

  /**
   * Check if user has access to a workspace
   */
  async checkWorkspaceAccess(workspaceId: string): Promise<{ hasAccess: boolean }> {
    return this.makeRequest<{ hasAccess: boolean }>(`/workspace/${workspaceId}/access`);
  }

  /**
   * Get unread message count for a workspace
   */
  async getUnreadCount(workspaceId: string): Promise<number> {
    const stats = await this.getMessageStats(workspaceId);
    return stats.unreadMessages;
  }

  /**
   * Mark all messages in a workspace as read
   */
  async markAllAsRead(workspaceId: string): Promise<void> {
    return this.makeRequest<void>(`/messaging/workspace/${workspaceId}/read-all`, {
      method: 'POST',
    });
  }
}

// Export singleton instance
export const messagingApiService = new MessagingApiService();
export default messagingApiService;