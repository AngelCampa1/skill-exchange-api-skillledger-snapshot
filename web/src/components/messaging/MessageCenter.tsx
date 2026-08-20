import { logger } from '@/utils/logger';
/**
 * MessageCenter - Main messaging interface component
 * Provides a responsive chat interface with real-time messaging capabilities
 */

import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import Image from 'next/image';
import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';
import { Button } from '../ui/button';
import { 
  Search, 
  Settings, 
  Users, 
  Phone, 
  Video, 
  Info,
  Wifi,
  WifiOff,
  AlertCircle
} from 'lucide-react';
import { 
  Message, 
  MessageHistoryRequest, 
  ConnectionState,
  TypingIndicator
} from '../../types/messaging';
import { signalRService } from '../../services/signalRService';
import { messagingApiService } from '../../services/messagingApiService';
import { MessageList } from './MessageList';
import { MessageInput } from './MessageInput';
import { TypingIndicators } from './TypingIndicators';
import { ConnectionStatusIndicator } from './ConnectionStatusIndicator';
import { MessageSearch } from './MessageSearch';

interface MessageCenterProps {
  workspaceId: string;
  currentUserId: string;
  workspaceTitle: string;
  participants: Array<{
    id: string;
    name: string;
    avatar: string;
    isOnline: boolean;
  }>;
  className?: string;
}

export const MessageCenter: React.FC<MessageCenterProps> = ({
  workspaceId,
  currentUserId,
  workspaceTitle,
  participants,
  className = ''
}) => {
  const [messages, setMessages] = useState<Message[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [connectionState, setConnectionState] = useState<ConnectionState>({
    status: 'disconnected',
    reconnectAttempts: 0
  });
  const [typingUsers, setTypingUsers] = useState<Map<string, TypingIndicator>>(new Map());
  const [showSearch, setShowSearch] = useState(false);
  const [showParticipants, setShowParticipants] = useState(false);
  const [hasMoreMessages, setHasMoreMessages] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [pageNumber, setPageNumber] = useState(1);
  
  const messageListRef = useRef<HTMLDivElement>(null);
  const lastMessageRef = useRef<string | null>(null);
  const typingTimers = useRef<Map<string, NodeJS.Timeout>>(new Map());

  // Load initial messages
  const loadMessages = useCallback(async (page: number = 1, append: boolean = false) => {
    try {
      if (page === 1) setLoading(true);
      else setLoadingMore(true);

      // BUG-HIGH-008 FIX: Use pagination instead of virtualization
      // Pagination with pageSize 50 limits memory usage and initial render time
      // Users can scroll to load more messages (infinite scroll pattern)
      const request: MessageHistoryRequest = {
        workspaceId,
        pageNumber: page,
        pageSize: 50
      };

      const response = await messagingApiService.getMessageHistory(request);
      
      if (append) {
        setMessages(prev => [...response.messages, ...prev]);
      } else {
        setMessages(response.messages);
        // Scroll to bottom for new conversation
        setTimeout(() => {
          if (messageListRef.current) {
            messageListRef.current.scrollTop = messageListRef.current.scrollHeight;
          }
        }, 100);
      }

      setHasMoreMessages(response.hasNextPage);
      setPageNumber(page);
    } catch (err) {
      logger.error('Failed to load messages:', err);
      setError(err instanceof Error ? err.message : 'Failed to load messages');
    } finally {
      setLoading(false);
      setLoadingMore(false);
    }
  }, [workspaceId]);

  // Load more messages when scrolling to top
  const loadMoreMessages = useCallback(async () => {
    if (!hasMoreMessages || loadingMore) return;
    await loadMessages(pageNumber + 1, true);
  }, [hasMoreMessages, loadingMore, pageNumber, loadMessages]);

  // Initialize SignalR connection
  useEffect(() => {
    const initializeConnection = async () => {
      try {
        await signalRService.connect(workspaceId);
        setConnectionState(signalRService.getConnectionState());
      } catch (err) {
        logger.error('Failed to connect to SignalR:', err);
        setError('Failed to connect to real-time messaging');
      }
    };

    initializeConnection();

    // Load initial messages
    loadMessages();

    return () => {
      signalRService.disconnect();
    };
  }, [workspaceId, loadMessages]);

  /**
   * BUG-FE-019 FIX: Memoize event handlers to prevent unnecessary re-subscriptions
   * These handlers are recreated on every render, causing SignalR to unsubscribe/resubscribe
   */
  const handleMessageReceived = useCallback((message: Message) => {
    setMessages(prev => {
      // Check if message already exists to prevent duplicates
      if (prev.some(m => m.id === message.id)) {
        return prev;
      }
      return [...prev, message];
    });

    // Auto-scroll to bottom if user is near bottom
    setTimeout(() => {
      if (messageListRef.current) {
        const { scrollTop, scrollHeight, clientHeight } = messageListRef.current;
        const isNearBottom = scrollHeight - scrollTop - clientHeight < 100;

        if (isNearBottom) {
          messageListRef.current.scrollTop = scrollHeight;
        }
      }
    }, 100);

    // Mark as read if message is from another user
    if (message.senderId !== currentUserId) {
      signalRService.markMessageAsRead(message.id);
    }
  }, [currentUserId]);

  const handleMessageUpdated = useCallback((message: Message) => {
    setMessages(prev => prev.map(m => m.id === message.id ? message : m));
  }, []);

  const handleMessageDeleted = useCallback((messageId: string) => {
    setMessages(prev => prev.filter(m => m.id !== messageId));
  }, []);

  const handleUserStartedTyping = useCallback((wId: string, user: TypingIndicator) => {
    if (wId === workspaceId && user.userId !== currentUserId) {
      setTypingUsers(prev => {
        const newMap = new Map(prev);
        newMap.set(user.userId, user);
        return newMap;
      });

      // BUG-FE-001 FIX: Clear existing timer before setting new one to prevent memory leaks
      const existingTimer = typingTimers.current.get(user.userId);
      if (existingTimer) {
        clearTimeout(existingTimer);
      }

      const timer = setTimeout(() => {
        setTypingUsers(prev => {
          const newMap = new Map(prev);
          newMap.delete(user.userId);
          return newMap;
        });
        typingTimers.current.delete(user.userId);
      }, 3000);

      typingTimers.current.set(user.userId, timer);
    }
  }, [workspaceId, currentUserId]);

  const handleUserStoppedTyping = useCallback((wId: string, userId: string) => {
    if (wId === workspaceId) {
      setTypingUsers(prev => {
        const newMap = new Map(prev);
        newMap.delete(userId);
        return newMap;
      });

      const timer = typingTimers.current.get(userId);
      if (timer) {
        clearTimeout(timer);
        typingTimers.current.delete(userId);
      }
    }
  }, [workspaceId]);

  // Setup SignalR event handlers
  useEffect(() => {

    // BUG-FE-003 FIX: Use event-based connection state updates instead of polling
    const handleConnectionStateChanged = (state: typeof connectionState) => {
      setConnectionState(state);
    };

    // Subscribe to events
    signalRService.on('MessageReceived', handleMessageReceived);
    signalRService.on('MessageUpdated', handleMessageUpdated);
    signalRService.on('MessageDeleted', handleMessageDeleted);
    signalRService.on('UserStartedTyping', handleUserStartedTyping);
    signalRService.on('UserStoppedTyping', handleUserStoppedTyping);
    signalRService.on('ConnectionStateChanged', handleConnectionStateChanged);

    // Get initial connection state once (no polling needed)
    setConnectionState(signalRService.getConnectionState());

    return () => {
      // Unsubscribe from events
      signalRService.off('MessageReceived', handleMessageReceived);
      signalRService.off('MessageUpdated', handleMessageUpdated);
      signalRService.off('MessageDeleted', handleMessageDeleted);
      signalRService.off('UserStartedTyping', handleUserStartedTyping);
      signalRService.off('UserStoppedTyping', handleUserStoppedTyping);
      signalRService.off('ConnectionStateChanged', handleConnectionStateChanged);

      // BUG-FE-001 FIX: Clear all typing timers on unmount to prevent memory leaks
      // eslint-disable-next-line react-hooks/exhaustive-deps
      const timers = typingTimers.current;
      timers.forEach(timer => clearTimeout(timer));
      timers.clear();
    };
  }, [workspaceId, currentUserId, handleMessageReceived, handleMessageUpdated, handleMessageDeleted, handleUserStartedTyping, handleUserStoppedTyping]);

  const handleSendMessage = async (messageText: string, files?: File[]) => {
    // This will be handled by MessageInput component
    // The actual sending logic is in MessageInput
  };

  const handleScroll = useCallback((e: React.UIEvent<HTMLDivElement>) => {
    const { scrollTop } = e.currentTarget;

    // Load more messages when scrolling to top
    if (scrollTop < 100 && hasMoreMessages && !loadingMore) {
      loadMoreMessages();
    }
  }, [hasMoreMessages, loadingMore, loadMoreMessages]);

  /**
   * BUG-FE-019 FIX: Memoize typing users array to avoid unnecessary re-renders
   * Converting Map to Array on every render is wasteful
   */
  const typingUsersArray = useMemo(() => {
    return Array.from(typingUsers.values());
  }, [typingUsers]);

  if (loading && messages.length === 0) {
    return (
      <div className={`flex items-center justify-center h-full ${className}`}>
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-4"></div>
          <p className="text-muted-foreground">Loading messages...</p>
        </div>
      </div>
    );
  }

  return (
    <Card className={`h-full flex flex-col ${className}`}>
      {/* Header */}
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4 border-b">
        <div className="flex items-center space-x-3">
          <CardTitle className="text-lg font-semibold truncate">
            {workspaceTitle}
          </CardTitle>
          <ConnectionStatusIndicator connectionState={connectionState} />
        </div>
        
        <div className="flex items-center space-x-2">
          <Button
            variant="ghost"
            size="icon"
            onClick={() => setShowSearch(!showSearch)}
            className="h-8 w-8"
          >
            <Search className="h-4 w-4" />
          </Button>
          
          <Button
            variant="ghost"
            size="icon"
            onClick={() => setShowParticipants(!showParticipants)}
            className="h-8 w-8"
          >
            <Users className="h-4 w-4" />
          </Button>
          
          <Button variant="ghost" size="icon" className="h-8 w-8">
            <Phone className="h-4 w-4" />
          </Button>
          
          <Button variant="ghost" size="icon" className="h-8 w-8">
            <Video className="h-4 w-4" />
          </Button>
          
          <Button variant="ghost" size="icon" className="h-8 w-8">
            <Settings className="h-4 w-4" />
          </Button>
        </div>
      </CardHeader>

      {/* Search Panel */}
      {showSearch && (
        <div className="border-b bg-muted/50">
          <MessageSearch 
            workspaceId={workspaceId}
            onMessageSelect={(message) => {
              // Scroll to message logic here
              logger.debug('Navigate to message', { messageId: message.id });
            }}
          />
        </div>
      )}

      <CardContent className="flex-1 flex flex-col p-0 overflow-hidden">
        {error && (
          <div className="p-4 bg-destructive/10 border-b border-destructive/20">
            <div className="flex items-center text-destructive">
              <AlertCircle className="h-4 w-4 mr-2" />
              {error}
              <Button
                variant="link"
                size="sm"
                onClick={() => {
                  setError(null);
                  loadMessages();
                }}
                className="ml-auto text-destructive"
              >
                Retry
              </Button>
            </div>
          </div>
        )}

        {/* Messages Area */}
        <div
          ref={messageListRef}
          className="flex-1 overflow-y-auto px-4 py-2"
          onScroll={handleScroll}
        >
          {loadingMore && (
            <div className="text-center py-4">
              <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-primary mx-auto"></div>
            </div>
          )}
          
          <MessageList
            messages={messages}
            currentUserId={currentUserId}
            workspaceId={workspaceId}
          />

          <TypingIndicators
            typingUsers={typingUsersArray}
          />
        </div>

        {/* Message Input */}
        <div className="border-t bg-card">
          <MessageInput
            workspaceId={workspaceId}
            onMessageSent={() => {
              // Refresh messages or rely on SignalR
            }}
          />
        </div>
      </CardContent>

      {/* Participants Sidebar */}
      {showParticipants && (
        <div className="absolute right-0 top-0 h-full w-64 bg-card border-l shadow-lg z-10">
          <div className="p-4 border-b">
            <div className="flex items-center justify-between">
              <h3 className="font-semibold">Participants</h3>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setShowParticipants(false)}
                className="h-8 w-8"
              >
                ×
              </Button>
            </div>
          </div>
          <div className="p-4 space-y-3">
            {participants.map(participant => (
              <div key={participant.id} className="flex items-center space-x-3">
                <div className="relative">
                  <Image
                    src={participant.avatar || '/default-avatar.png'}
                    alt={participant.name}
                    width={32}
                    height={32}
                    className="h-8 w-8 rounded-full"
                  />
                  <div
                    className={`absolute -bottom-1 -right-1 h-3 w-3 rounded-full border-2 border-card ${
                      participant.isOnline ? 'bg-success' : 'bg-muted-foreground'
                    }`}
                  />
                </div>
                <span className="text-sm font-medium">{participant.name}</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </Card>
  );
};