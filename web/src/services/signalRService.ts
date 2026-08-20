/**
 * SignalR connection management service for real-time messaging
 * Handles connection lifecycle, reconnection logic, and event management
 */

import * as signalR from '@microsoft/signalr';
import { ConnectionState, SignalREvents, Message, MessageReaction, TypingIndicator } from '../types/messaging';
import { logger } from '../utils/logger';

type EventHandler<T extends keyof SignalREvents> = SignalREvents[T];

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private connectionState: ConnectionState = {
    status: 'disconnected',
    reconnectAttempts: 0
  };
  // BUG-HIGH-005 FIX: Use properly typed handlers instead of Function to avoid 'any' casts
  private eventHandlers: Map<keyof SignalREvents, Set<(...args: any[]) => void>> = new Map();
  private reconnectTimer: NodeJS.Timeout | null = null;
  private maxReconnectAttempts = 10;
  private reconnectDelay = 1000; // Start with 1 second
  private maxReconnectDelay = 30000; // Max 30 seconds
  private currentWorkspaceId: string | null = null;
  private connectionLock: Promise<void> | null = null; // BUG-FE-003 FIX: Prevent race conditions
  // BUG-SYNC-015 FIX: Track pending workspace to detect stale connections during async operations
  private pendingWorkspaceId: string | null = null;
  // BUG-LOW-022 FIX: Track correlation ID for distributed request tracing
  private correlationId: string | null = null;

  constructor() {
    this.initializeEventHandlers();
  }

  private initializeEventHandlers() {
    // Initialize empty sets for all event types
    const eventTypes: (keyof SignalREvents)[] = [
      'MessageReceived',
      'MessageUpdated',
      'MessageDeleted',
      'ReactionAdded',
      'ReactionRemoved',
      'UserStartedTyping',
      'UserStoppedTyping',
      'MessageRead',
      'UserJoined',
      'UserLeft',
      // BUG-FE-017 FIX: Add dedicated ConnectionStateChanged event
      'ConnectionStateChanged'
    ];

    eventTypes.forEach(eventType => {
      this.eventHandlers.set(eventType, new Set());
    });
  }

  /**
   * Connect to SignalR hub
   */
  async connect(workspaceId: string): Promise<void> {
    // BUG-LOW-022 FIX: Generate correlation ID for this connection session
    this.correlationId = `signalr-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

    // BUG-SYNC-015 FIX: Always update pending workspace so waiting connections know the latest target
    this.pendingWorkspaceId = workspaceId;

    // BUG-FE-003 FIX: Prevent race conditions with connection lock
    // If another connection is already in progress, wait for it to complete
    if (this.connectionLock) {
      await this.connectionLock;
      // BUG-SYNC-015 FIX: Check if a newer workspace was requested while we were waiting
      if (this.pendingWorkspaceId !== workspaceId) {
        logger.debug('Workspace changed during connection wait, skipping stale connection', {
          service: 'SignalR',
          requestedWorkspace: workspaceId,
          pendingWorkspace: this.pendingWorkspaceId,
          correlationId: this.correlationId
        });
        return;
      }
      // After waiting, check if we're already connected to this workspace
      if (this.connection?.state === signalR.HubConnectionState.Connected &&
          this.currentWorkspaceId === workspaceId) {
        return;
      }
    }

    if (this.connection?.state === signalR.HubConnectionState.Connected &&
        this.currentWorkspaceId === workspaceId) {
      return;
    }

    // BUG-CRIT-009 FIX: Create lock resolver reference BEFORE creating the promise
    // to ensure it's always available in the finally block
    let resolveLock!: () => void;
    let lockCreated = false;

    try {
      // BUG-CRIT-009 FIX: Create lock promise inside try block
      this.connectionLock = new Promise<void>(resolve => {
        resolveLock = resolve;
      });
      lockCreated = true;

      // BUG-SYNC-016 FIX: Set currentWorkspaceId AFTER disconnect to prevent it being cleared
      await this.disconnect();
      this.currentWorkspaceId = workspaceId;
      this.updateConnectionState({ status: 'connecting', reconnectAttempts: 0 });

      // BUG-FE-002 FIX: Remove localStorage token, use httpOnly cookies
      // SignalR will automatically include cookies with withCredentials
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`/api/hubs/messaging?workspaceId=${workspaceId}`, {
          // Remove accessTokenFactory - authentication via httpOnly cookies
          skipNegotiation: true,
          transport: signalR.HttpTransportType.WebSockets,
          // Ensure credentials (cookies) are sent with requests
          withCredentials: true
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            const delay = Math.min(
              this.reconnectDelay * Math.pow(2, retryContext.previousRetryCount),
              this.maxReconnectDelay
            );
            return delay;
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      this.setupConnectionEventHandlers();
      this.setupMessageEventHandlers();

      await this.connection.start();

      // BUG-SYNC-015 FIX: Check if workspace changed during connection establishment
      if (this.pendingWorkspaceId !== workspaceId) {
        logger.debug('Workspace changed during connection start, disconnecting stale connection', {
          service: 'SignalR',
          requestedWorkspace: workspaceId,
          pendingWorkspace: this.pendingWorkspaceId,
          correlationId: this.correlationId
        });
        await this.connection.stop();
        this.connection = null;
        return;
      }

      // Join the workspace group
      await this.connection.invoke('JoinWorkspace', workspaceId);

      this.updateConnectionState({
        status: 'connected',
        lastConnectedAt: new Date().toISOString(),
        reconnectAttempts: 0
      });

    } catch (error) {
      logger.error('SignalR connection failed', error, { service: 'SignalR', correlationId: this.correlationId });
      // BUG-FE-010 FIX: Ensure timer is cleared before scheduling reconnect
      // This prevents zombie timers if multiple connection attempts fail
      if (this.reconnectTimer) {
        clearTimeout(this.reconnectTimer);
        this.reconnectTimer = null;
      }
      this.updateConnectionState({
        status: 'error',
        error: error instanceof Error ? error.message : 'Connection failed'
      });
      this.scheduleReconnect();
    } finally {
      // BUG-CRIT-009 FIX: Only release lock if it was successfully created
      if (lockCreated && resolveLock) {
        resolveLock();
        this.connectionLock = null;
      }
    }
  }

  /**
   * Disconnect from SignalR hub
   */
  async disconnect(): Promise<void> {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }

    if (this.connection) {
      try {
        if (this.currentWorkspaceId && this.connection.state === signalR.HubConnectionState.Connected) {
          await this.connection.invoke('LeaveWorkspace', this.currentWorkspaceId);
        }
        await this.connection.stop();
      } catch (error) {
        logger.error('Error during disconnect', error, { service: 'SignalR', correlationId: this.correlationId });
      }
      this.connection = null;
    }

    this.currentWorkspaceId = null;
    this.correlationId = null;
    this.updateConnectionState({ status: 'disconnected', reconnectAttempts: 0 });
  }

  /**
   * Get current connection state
   */
  getConnectionState(): ConnectionState {
    return { ...this.connectionState };
  }

  /**
   * Subscribe to SignalR events
   */
  on<T extends keyof SignalREvents>(event: T, handler: EventHandler<T>): void {
    const handlers = this.eventHandlers.get(event);
    if (handlers) {
      handlers.add(handler);
    }
  }

  /**
   * Unsubscribe from SignalR events
   */
  off<T extends keyof SignalREvents>(event: T, handler: EventHandler<T>): void {
    const handlers = this.eventHandlers.get(event);
    if (handlers) {
      handlers.delete(handler);
    }
  }

  /**
   * Send typing indicator
   */
  async sendTypingIndicator(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected && this.currentWorkspaceId) {
      try {
        await this.connection.invoke('SendTypingIndicator', this.currentWorkspaceId);
      } catch (error) {
        logger.error('Failed to send typing indicator', error, { service: 'SignalR', correlationId: this.correlationId });
      }
    }
  }

  /**
   * Stop typing indicator
   */
  async stopTypingIndicator(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected && this.currentWorkspaceId) {
      try {
        await this.connection.invoke('StopTypingIndicator', this.currentWorkspaceId);
      } catch (error) {
        logger.error('Failed to stop typing indicator', error, { service: 'SignalR', correlationId: this.correlationId });
      }
    }
  }

  /**
   * Mark message as read
   */
  async markMessageAsRead(messageId: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke('MarkMessageAsRead', messageId);
      } catch (error) {
        logger.error('Failed to mark message as read', error, { service: 'SignalR', correlationId: this.correlationId });
      }
    }
  }

  private setupConnectionEventHandlers(): void {
    if (!this.connection) return;

    this.connection.onreconnecting(() => {
      this.updateConnectionState({ status: 'reconnecting' });
    });

    this.connection.onreconnected(async () => {
      if (this.currentWorkspaceId) {
        try {
          await this.connection!.invoke('JoinWorkspace', this.currentWorkspaceId);
          this.updateConnectionState({
            status: 'connected',
            lastConnectedAt: new Date().toISOString(),
            reconnectAttempts: 0
          });
        } catch (error) {
          logger.error('Failed to rejoin workspace after reconnection', error, { service: 'SignalR', correlationId: this.correlationId });
        }
      }
    });

    this.connection.onclose(async (error) => {
      if (error) {
        logger.error('SignalR connection closed with error', error, { service: 'SignalR', correlationId: this.correlationId });
        this.updateConnectionState({
          status: 'error',
          error: error.message
        });
        this.scheduleReconnect();
      } else {
        this.updateConnectionState({ status: 'disconnected' });
      }
    });
  }

  private setupMessageEventHandlers(): void {
    if (!this.connection) return;

    this.connection.on('MessageReceived', (message: Message) => {
      this.emit('MessageReceived', message);
    });

    this.connection.on('MessageUpdated', (message: Message) => {
      this.emit('MessageUpdated', message);
    });

    this.connection.on('MessageDeleted', (messageId: string) => {
      this.emit('MessageDeleted', messageId);
    });

    this.connection.on('ReactionAdded', (messageId: string, reaction: MessageReaction) => {
      this.emit('ReactionAdded', messageId, reaction);
    });

    this.connection.on('ReactionRemoved', (messageId: string, userId: string, emoji: string) => {
      this.emit('ReactionRemoved', messageId, userId, emoji);
    });

    this.connection.on('UserStartedTyping', (workspaceId: string, user: TypingIndicator) => {
      this.emit('UserStartedTyping', workspaceId, user);
    });

    this.connection.on('UserStoppedTyping', (workspaceId: string, userId: string) => {
      this.emit('UserStoppedTyping', workspaceId, userId);
    });

    this.connection.on('MessageRead', (messageId: string, userId: string, readAt: string) => {
      this.emit('MessageRead', messageId, userId, readAt);
    });

    this.connection.on('UserJoined', (workspaceId: string, userId: string, userName: string) => {
      this.emit('UserJoined', workspaceId, userId, userName);
    });

    this.connection.on('UserLeft', (workspaceId: string, userId: string, userName: string) => {
      this.emit('UserLeft', workspaceId, userId, userName);
    });
  }

  private emit<T extends keyof SignalREvents>(event: T, ...args: Parameters<SignalREvents[T]>): void {
    const handlers = this.eventHandlers.get(event);
    if (handlers) {
      handlers.forEach(handler => {
        try {
          // BUG-HIGH-005 FIX: No need for 'any' cast with properly typed handler storage
          handler(...args);
        } catch (error) {
          logger.error(`Error in ${event} handler`, error, { service: 'SignalR', event, correlationId: this.correlationId });
        }
      });
    }
  }

  private updateConnectionState(updates: Partial<ConnectionState>): void {
    this.connectionState = { ...this.connectionState, ...updates };

    // BUG-FE-017 FIX: Use dedicated ConnectionStateChanged event instead of UserJoined hack
    this.emit('ConnectionStateChanged', this.connectionState);
  }

  private scheduleReconnect(): void {
    if (this.connectionState.reconnectAttempts >= this.maxReconnectAttempts) {
      logger.error('Max reconnection attempts reached', undefined, { service: 'SignalR', correlationId: this.correlationId });
      this.updateConnectionState({ status: 'error', error: 'Max reconnection attempts reached' });
      return;
    }

    // BUG-FE-010 FIX: Always clear existing timer before creating new one
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }

    const delay = Math.min(
      this.reconnectDelay * Math.pow(2, this.connectionState.reconnectAttempts),
      this.maxReconnectDelay
    );

    this.reconnectTimer = setTimeout(async () => {
      // BUG-FE-010 FIX: Clear timer reference immediately when it fires
      this.reconnectTimer = null;

      if (this.currentWorkspaceId) {
        this.connectionState.reconnectAttempts++;
        await this.connect(this.currentWorkspaceId);
      }
    }, delay);
  }

  /**
   * Check if connected
   */
  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  /**
   * Get current workspace ID
   */
  getCurrentWorkspaceId(): string | null {
    return this.currentWorkspaceId;
  }
}

// Export singleton instance
export const signalRService = new SignalRService();
export default signalRService;