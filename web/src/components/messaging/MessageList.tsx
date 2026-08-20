/**
 * MessageList - Displays a list of messages with proper grouping and formatting
 */

import React from 'react';
import Image from 'next/image';
import { format, isToday, isYesterday, isSameDay } from 'date-fns';
import { Message } from '../../types/messaging';
import { MessageItem } from './MessageItem';

interface MessageListProps {
  messages: Message[];
  currentUserId: string;
  workspaceId: string;
}

export const MessageList: React.FC<MessageListProps> = ({
  messages,
  currentUserId,
  workspaceId
}) => {
  // Group messages by date and consecutive messages from same sender
  const groupedMessages = React.useMemo(() => {
    const groups: Array<{
      date: string;
      dateLabel: string;
      messageGroups: Array<{
        senderId: string;
        senderName: string;
        senderAvatar: string;
        messages: Message[];
      }>;
    }> = [];

    // BUG-TEST-025 FIX: Guard against undefined messages prop
    if (!messages || messages.length === 0) {
      return groups;
    }

    let currentDateGroup: typeof groups[0] | null = null;
    let currentMessageGroup: typeof groups[0]['messageGroups'][0] | null = null;

    messages.forEach((message) => {
      const messageDate = new Date(message.createdAt);
      const dateKey = format(messageDate, 'yyyy-MM-dd');
      
      // Create date label
      let dateLabel: string;
      if (isToday(messageDate)) {
        dateLabel = 'Today';
      } else if (isYesterday(messageDate)) {
        dateLabel = 'Yesterday';
      } else {
        dateLabel = format(messageDate, 'MMMM d, yyyy');
      }

      // Start new date group if needed
      if (!currentDateGroup || currentDateGroup.date !== dateKey) {
        currentDateGroup = {
          date: dateKey,
          dateLabel,
          messageGroups: []
        };
        groups.push(currentDateGroup);
        currentMessageGroup = null;
      }

      // Check if we need to start a new message group
      const shouldStartNewGroup = 
        !currentMessageGroup ||
        currentMessageGroup.senderId !== message.senderId ||
        // Start new group if more than 5 minutes between messages
        (new Date(message.createdAt).getTime() - 
         new Date(currentMessageGroup.messages[currentMessageGroup.messages.length - 1].createdAt).getTime()) > 5 * 60 * 1000;

      if (shouldStartNewGroup) {
        currentMessageGroup = {
          senderId: message.senderId,
          senderName: message.senderName,
          senderAvatar: message.senderAvatar,
          messages: []
        };
        currentDateGroup.messageGroups.push(currentMessageGroup);
      }

      if (currentMessageGroup) {
        currentMessageGroup.messages.push(message);
      }
    });

    return groups;
  }, [messages]);

  // BUG-TEST-025 FIX: Guard against undefined messages prop here too
  if (!messages || messages.length === 0) {
    return (
      <div className="flex-1 flex items-center justify-center">
        <div className="text-center text-muted-foreground">
          <div className="text-6xl mb-4">💬</div>
          <h3 className="text-lg font-medium mb-2">No messages yet</h3>
          <p className="text-sm">Start the conversation by sending a message below.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {groupedMessages.map((dateGroup) => (
        <div key={dateGroup.date} className="space-y-4">
          {/* Date Divider */}
          <div className="flex items-center justify-center">
            <div className="bg-muted text-muted-foreground text-xs font-medium px-3 py-1 rounded-full">
              {dateGroup.dateLabel}
            </div>
          </div>

          {/* Message Groups for this date */}
          {dateGroup.messageGroups.map((messageGroup, groupIndex) => (
            <div key={`${dateGroup.date}-${groupIndex}`} className="space-y-1">
              <MessageGroupHeader
                senderId={messageGroup.senderId}
                senderName={messageGroup.senderName}
                senderAvatar={messageGroup.senderAvatar}
                timestamp={messageGroup.messages[0].createdAt}
                isCurrentUser={messageGroup.senderId === currentUserId}
              />
              
              {/* Messages in this group */}
              <div className="space-y-1">
                {messageGroup.messages.map((message, messageIndex) => (
                  <MessageItem
                    key={message.id}
                    message={message}
                    isCurrentUser={message.senderId === currentUserId}
                    showAvatar={messageIndex === 0} // Only show avatar for first message in group
                    showSender={messageIndex === 0} // Only show sender name for first message in group
                    showTimestamp={messageIndex === messageGroup.messages.length - 1} // Only show timestamp for last message in group
                    workspaceId={workspaceId}
                  />
                ))}
              </div>
            </div>
          ))}
        </div>
      ))}
    </div>
  );
};

interface MessageGroupHeaderProps {
  senderId: string;
  senderName: string;
  senderAvatar: string;
  timestamp: string;
  isCurrentUser: boolean;
}

const MessageGroupHeader: React.FC<MessageGroupHeaderProps> = ({
  senderId,
  senderName,
  senderAvatar,
  timestamp,
  isCurrentUser
}) => {
  const messageDate = new Date(timestamp);
  const timeString = format(messageDate, 'h:mm a');

  if (isCurrentUser) {
    return null; // Don't show header for current user messages
  }

  return (
    <div className="flex items-center space-x-2 mb-1">
      <Image
        src={senderAvatar || '/default-avatar.png'}
        alt={senderName}
        width={24}
        height={24}
        className="h-6 w-6 rounded-full"
      />
      <span className="text-sm font-medium text-foreground">{senderName}</span>
      <span className="text-xs text-muted-foreground">{timeString}</span>
    </div>
  );
};