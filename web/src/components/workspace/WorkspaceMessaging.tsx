import { logger } from '@/utils/logger';
/**
 * WorkspaceMessaging - Integration example showing how to use MessageCenter in a workspace
 */

import React, { useState, useEffect } from 'react';
import { MessageCenter } from '../messaging/MessageCenter';
import { MessageNotifications, useMessageNotifications } from '../messaging/MessageNotifications';
import { signalRService } from '../../services/signalRService';
import { AUTH_CONFIG } from '../../constants/auth';
import { Message } from '../../types/messaging';

interface WorkspaceMessagingProps {
  workspaceId: string;
  currentUserId: string;
  workspaceTitle: string;
  className?: string;
}

interface WorkspaceParticipant {
  id: string;
  name: string;
  avatar: string;
  isOnline: boolean;
}

export const WorkspaceMessaging: React.FC<WorkspaceMessagingProps> = ({
  workspaceId,
  currentUserId,
  workspaceTitle,
  className = ''
}) => {
  const [participants, setParticipants] = useState<WorkspaceParticipant[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const {
    notifications,
    addNotification,
    dismissNotification,
    clearAllNotifications,
    requestNotificationPermission
  } = useMessageNotifications();

  // Load workspace participants
  useEffect(() => {
    const loadParticipants = async () => {
      try {
        setLoading(true);
        const response = await fetch(`/api/workspace/${workspaceId}/participants`, {
          credentials: AUTH_CONFIG.CREDENTIALS,
          headers: {
            // BUG-FE-002 FIX: Removed - Auth via httpOnly cookies,
            'Content-Type': 'application/json',
          },
        });

        if (response.ok) {
          const data = await response.json();
          setParticipants(data.participants || []);
        } else {
          throw new Error('Failed to load participants');
        }
      } catch (err) {
        logger.error('Failed to load participants', err, { component: 'WorkspaceMessaging' });
        setError(err instanceof Error ? err.message : 'Failed to load participants');
        
        // Fallback to mock data for demo purposes
        setParticipants([
          {
            id: currentUserId,
            name: 'You',
            avatar: '/default-avatar.png',
            isOnline: true
          },
          {
            id: 'user-2',
            name: 'Project Manager',
            avatar: '/default-avatar.png',
            isOnline: true
          },
          {
            id: 'user-3',
            name: 'Team Lead',
            avatar: '/default-avatar.png',
            isOnline: false
          }
        ]);
      } finally {
        setLoading(false);
      }
    };

    loadParticipants();
  }, [workspaceId, currentUserId]);

  // Setup message notifications
  useEffect(() => {
    // Request notification permission when component mounts
    requestNotificationPermission();

    // Listen for new messages from other users
    // BUG-FE-014 FIX: Add proper typing and validation for message structure
    const handleNewMessage = (message: Message) => {
      // Validate message structure to prevent runtime errors
      if (!message || typeof message !== 'object') {
        logger.error('Invalid message received', undefined, { component: 'WorkspaceMessaging', message });
        return;
      }

      if (!message.id || !message.senderId) {
        logger.error('Message missing required fields', undefined, { component: 'WorkspaceMessaging', message });
        return;
      }

      // Only show notifications for messages from other users
      if (message.senderId !== currentUserId) {
        addNotification(message);
      }
    };

    // Setup SignalR event listeners for notifications
    signalRService.on('MessageReceived', handleNewMessage);

    return () => {
      signalRService.off('MessageReceived', handleNewMessage);
    };
  }, [currentUserId, addNotification, requestNotificationPermission]);

  const handleNotificationClick = (notification: any) => {
    // Navigate to the message or scroll to it
    logger.debug('Navigate to message', { messageId: notification.message.id });
    // In a real app, you might scroll to the message or highlight it
  };

  if (loading) {
    return (
      <div className={`flex items-center justify-center h-full ${className}`}>
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-4"></div>
          <p className="text-muted-foreground">Loading workspace...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`flex items-center justify-center h-full ${className}`}>
        <div className="text-center">
          <div className="text-destructive mb-4">{error}</div>
          <p className="text-muted-foreground">Using demo data for messaging interface</p>
        </div>
      </div>
    );
  }

  return (
    <div className={`h-full relative ${className}`}>
      {/* Main messaging interface */}
      <MessageCenter
        workspaceId={workspaceId}
        currentUserId={currentUserId}
        workspaceTitle={workspaceTitle}
        participants={participants}
        className="h-full"
      />

      {/* Toast notifications for new messages */}
      <MessageNotifications
        notifications={notifications}
        onNotificationClick={handleNotificationClick}
        onNotificationDismiss={dismissNotification}
        onNotificationClearAll={clearAllNotifications}
      />
    </div>
  );
};

// Example usage in a workspace page
export const ExampleWorkspacePage: React.FC = () => {
  const workspaceId = 'workspace-123'; // Would come from route params
  const currentUserId = 'user-123'; // Would come from auth context
  const workspaceTitle = 'Project Alpha Development';

  return (
    <div className="h-screen flex flex-col">
      {/* Workspace header */}
      <div className="bg-card border-b px-6 py-4">
        <h1 className="text-xl font-semibold text-foreground">
          {workspaceTitle}
        </h1>
        <p className="text-sm text-muted-foreground">
          Collaborative workspace for project management and communication
        </p>
      </div>

      {/* Main content area */}
      <div className="flex-1 flex overflow-hidden">
        {/* Sidebar (optional) */}
        <div className="w-64 bg-muted border-r hidden lg:block">
          <div className="p-4">
            <h2 className="font-medium text-foreground mb-3">Workspace Tools</h2>
            <div className="space-y-2">
              <button className="w-full text-left px-3 py-2 text-sm text-muted-foreground hover:bg-background rounded">
                📁 Files
              </button>
              <button className="w-full text-left px-3 py-2 text-sm text-muted-foreground hover:bg-background rounded">
                📊 Tasks
              </button>
              <button className="w-full text-left px-3 py-2 text-sm text-primary bg-primary/10 rounded font-medium">
                💬 Messages
              </button>
              <button className="w-full text-left px-3 py-2 text-sm text-muted-foreground hover:bg-background rounded">
                📈 Analytics
              </button>
            </div>
          </div>
        </div>

        {/* Messaging interface */}
        <div className="flex-1">
          <WorkspaceMessaging
            workspaceId={workspaceId}
            currentUserId={currentUserId}
            workspaceTitle={workspaceTitle}
          />
        </div>
      </div>
    </div>
  );
};