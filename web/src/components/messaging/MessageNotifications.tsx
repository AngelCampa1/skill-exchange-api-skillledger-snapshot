/**
 * MessageNotifications - Toast notifications for new messages and system events
 */

import React, { useState, useEffect, useCallback } from 'react';
import Image from 'next/image';
import { X, MessageCircle, FileText, Image as ImageIcon, Mic, Users, Info } from 'lucide-react';
import { format } from 'date-fns';
import { Button } from '../ui/button';
import { Message, MessageType, MessageNotification } from '../../types/messaging';

interface MessageNotificationsProps {
  notifications: MessageNotification[];
  onNotificationClick: (notification: MessageNotification) => void;
  onNotificationDismiss: (notificationId: string) => void;
  onNotificationClearAll: () => void;
}

export const MessageNotifications: React.FC<MessageNotificationsProps> = ({
  notifications,
  onNotificationClick,
  onNotificationDismiss,
  onNotificationClearAll
}) => {
  const [visibleNotifications, setVisibleNotifications] = useState<MessageNotification[]>([]);

  // Manage visible notifications (show max 3 at a time)
  useEffect(() => {
    const unreadNotifications = notifications.filter(n => !n.isRead);
    setVisibleNotifications(unreadNotifications.slice(-3)); // Show latest 3
  }, [notifications]);

  // Auto-dismiss notifications after 5 seconds
  // BUG-FE-021 FIX: Properly track all setTimeout timers to prevent memory leaks
  // The previous implementation returned cleanup from forEach which doesn't work
  useEffect(() => {
    const timers: NodeJS.Timeout[] = [];

    visibleNotifications.forEach(notification => {
      const timer = setTimeout(() => {
        onNotificationDismiss(notification.id);
      }, 5000);
      timers.push(timer);
    });

    // Cleanup all timers when effect re-runs or component unmounts
    return () => {
      timers.forEach(timer => clearTimeout(timer));
    };
  }, [visibleNotifications, onNotificationDismiss]);

  const getNotificationIcon = (messageType: MessageType) => {
    switch (messageType) {
      case MessageType.Text:
        return <MessageCircle className="h-5 w-5 text-info" />;
      case MessageType.Image:
        return <ImageIcon className="h-5 w-5 text-success" />;
      case MessageType.File:
        return <FileText className="h-5 w-5 text-warning" />;
      case MessageType.Voice:
        return <Mic className="h-5 w-5 text-primary" />;
      case MessageType.System:
        return <Info className="h-5 w-5 text-muted-foreground" />;
      case MessageType.Milestone:
        return <Users className="h-5 w-5 text-primary" />;
      default:
        return <MessageCircle className="h-5 w-5 text-info" />;
    }
  };

  const getNotificationTitle = (message: Message) => {
    switch (message.messageType) {
      case MessageType.Image:
        return `${message.senderName} sent an image`;
      case MessageType.File:
        return `${message.senderName} sent a file`;
      case MessageType.Voice:
        return `${message.senderName} sent a voice message`;
      case MessageType.System:
        return 'System notification';
      case MessageType.Milestone:
        return `${message.senderName} updated a milestone`;
      default:
        return `${message.senderName}`;
    }
  };

  const getNotificationContent = (message: Message) => {
    if (message.messageText) {
      return message.messageText.length > 60 
        ? `${message.messageText.substring(0, 60)}...`
        : message.messageText;
    }
    
    switch (message.messageType) {
      case MessageType.Image:
        return 'Shared an image';
      case MessageType.File:
        return message.attachmentFileName || 'Shared a file';
      case MessageType.Voice:
        return 'Sent a voice message';
      case MessageType.Milestone:
        return 'Updated project milestone';
      default:
        return 'New message';
    }
  };

  const handleNotificationClick = (notification: MessageNotification) => {
    onNotificationClick(notification);
    onNotificationDismiss(notification.id);
  };

  if (visibleNotifications.length === 0) {
    return null;
  }

  return (
    <div className="fixed top-4 right-4 z-50 space-y-2 max-w-sm">
      {visibleNotifications.map(notification => (
        <div
          key={notification.id}
          className="bg-card border border-border rounded-lg shadow-lg p-4 cursor-pointer transform transition-all duration-300 hover:shadow-xl hover:scale-105"
          onClick={() => handleNotificationClick(notification)}
        >
          <div className="flex items-start space-x-3">
            {/* Icon */}
            <div className="flex-shrink-0">
              {getNotificationIcon(notification.message.messageType)}
            </div>

            {/* Content */}
            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between">
                <p className="text-sm font-medium text-foreground truncate">
                  {getNotificationTitle(notification.message)}
                </p>
                <Button
                  size="icon"
                  variant="ghost"
                  onClick={(e) => {
                    e.stopPropagation();
                    onNotificationDismiss(notification.id);
                  }}
                  className="h-6 w-6 text-muted-foreground hover:text-foreground"
                >
                  <X className="h-4 w-4" />
                </Button>
              </div>

              <p className="text-sm text-muted-foreground mt-1">
                {getNotificationContent(notification.message)}
              </p>

              <p className="text-xs text-muted-foreground/70 mt-2">
                {format(new Date(notification.message.createdAt), 'h:mm a')}
              </p>
            </div>

            {/* Avatar */}
            <div className="flex-shrink-0">
              <Image
                src={notification.message.senderAvatar || '/default-avatar.png'}
                alt={notification.message.senderName}
                width={32}
                height={32}
                className="h-8 w-8 rounded-full"
              />
            </div>
          </div>

          {/* Progress bar for auto-dismiss */}
          <div className="mt-3">
            <div className="w-full bg-muted rounded-full h-1">
              <div
                className="bg-primary h-1 rounded-full transition-all duration-5000 ease-linear"
                style={{
                  animation: 'shrink 5s linear forwards'
                }}
              />
            </div>
          </div>
        </div>
      ))}

      {/* Clear all button when multiple notifications */}
      {visibleNotifications.length > 1 && (
        <div className="text-center">
          <Button
            variant="outline"
            size="sm"
            onClick={onNotificationClearAll}
            className="bg-card"
          >
            Clear all notifications
          </Button>
        </div>
      )}

      <style jsx>{`
        @keyframes shrink {
          from { width: 100%; }
          to { width: 0%; }
        }
      `}</style>
    </div>
  );
};

// Hook for managing message notifications
export const useMessageNotifications = () => {
  const [notifications, setNotifications] = useState<MessageNotification[]>([]);

  const addNotification = useCallback((message: Message) => {
    const notification: MessageNotification = {
      id: `notification-${message.id}-${Date.now()}`,
      message,
      timestamp: new Date().toISOString(),
      isRead: false
    };

    setNotifications(prev => [...prev, notification]);

    // Request browser notification permission and show if granted
    if ('Notification' in window && Notification.permission === 'granted') {
      const browserNotification = new Notification(
        getNotificationTitle(message),
        {
          body: getNotificationContent(message),
          icon: message.senderAvatar || '/default-avatar.png',
          tag: message.id,
        }
      );

      // Close after 5 seconds
      setTimeout(() => {
        browserNotification.close();
      }, 5000);
    }
  }, []);

  const dismissNotification = useCallback((notificationId: string) => {
    setNotifications(prev => 
      prev.map(n => 
        n.id === notificationId 
          ? { ...n, isRead: true }
          : n
      )
    );
  }, []);

  const clearAllNotifications = useCallback(() => {
    setNotifications(prev => 
      prev.map(n => ({ ...n, isRead: true }))
    );
  }, []);

  const requestNotificationPermission = useCallback(async () => {
    if ('Notification' in window && Notification.permission === 'default') {
      const permission = await Notification.requestPermission();
      return permission === 'granted';
    }
    return Notification.permission === 'granted';
  }, []);

  return {
    notifications,
    addNotification,
    dismissNotification,
    clearAllNotifications,
    requestNotificationPermission
  };
};

// Helper functions used in the hook
function getNotificationTitle(message: Message): string {
  switch (message.messageType) {
    case MessageType.Image:
      return `${message.senderName} sent an image`;
    case MessageType.File:
      return `${message.senderName} sent a file`;
    case MessageType.Voice:
      return `${message.senderName} sent a voice message`;
    case MessageType.System:
      return 'System notification';
    case MessageType.Milestone:
      return `${message.senderName} updated a milestone`;
    default:
      return message.senderName;
  }
}

function getNotificationContent(message: Message): string {
  if (message.messageText) {
    return message.messageText.length > 100 
      ? `${message.messageText.substring(0, 100)}...`
      : message.messageText;
  }
  
  switch (message.messageType) {
    case MessageType.Image:
      return 'Shared an image';
    case MessageType.File:
      return message.attachmentFileName || 'Shared a file';
    case MessageType.Voice:
      return 'Sent a voice message';
    case MessageType.Milestone:
      return 'Updated project milestone';
    default:
      return 'New message';
  }
}