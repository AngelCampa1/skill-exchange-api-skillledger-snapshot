import { logger } from '@/utils/logger';
/**
 * EmojiReactions - Display and manage emoji reactions on messages
 */

import React, { useState, useRef, useEffect } from 'react';
import { Plus } from 'lucide-react';
import { Button } from '../ui/button';
import { MessageReaction, EmojiReactionGroup } from '../../types/messaging';
import { messagingApiService } from '../../services/messagingApiService';
import EmojiPicker from 'emoji-picker-react';

interface EmojiReactionsProps {
  messageId: string;
  reactions: MessageReaction[];
  workspaceId: string;
  currentUserId?: string;
}

export const EmojiReactions: React.FC<EmojiReactionsProps> = ({
  messageId,
  reactions,
  workspaceId,
  currentUserId
}) => {
  const [showEmojiPicker, setShowEmojiPicker] = useState(false);
  const [hoveredReaction, setHoveredReaction] = useState<string | null>(null);
  const emojiPickerRef = useRef<HTMLDivElement>(null);

  // Group reactions by emoji
  const reactionGroups = React.useMemo(() => {
    const groups: Record<string, EmojiReactionGroup> = {};
    
    reactions.forEach(reaction => {
      if (!groups[reaction.emoji]) {
        groups[reaction.emoji] = {
          emoji: reaction.emoji,
          count: 0,
          users: [],
          hasUserReacted: false
        };
      }
      
      groups[reaction.emoji].count++;
      groups[reaction.emoji].users.push({
        id: reaction.userId,
        name: reaction.userName
      });
      
      if (currentUserId && reaction.userId === currentUserId) {
        groups[reaction.emoji].hasUserReacted = true;
      }
    });
    
    return Object.values(groups);
  }, [reactions, currentUserId]);

  // Close emoji picker when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (emojiPickerRef.current && !emojiPickerRef.current.contains(event.target as Node)) {
        setShowEmojiPicker(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleReactionClick = async (emoji: string, hasUserReacted: boolean) => {
    try {
      if (hasUserReacted) {
        // Remove reaction
        await messagingApiService.removeReaction(messageId, emoji);
      } else {
        // Add reaction
        await messagingApiService.addReaction(messageId, { emoji });
      }
    } catch (error) {
      logger.error('Failed to toggle reaction:', error);
    }
  };

  const handleEmojiSelect = async (emojiObject: any) => {
    try {
      const emoji = emojiObject.emoji;
      await messagingApiService.addReaction(messageId, { emoji });
      setShowEmojiPicker(false);
    } catch (error) {
      logger.error('Failed to add reaction:', error);
    }
  };

  const getUsersString = (users: Array<{ id: string; name: string }>) => {
    if (users.length === 1) {
      return users[0].name;
    } else if (users.length === 2) {
      return `${users[0].name} and ${users[1].name}`;
    } else if (users.length <= 5) {
      const lastUser = users[users.length - 1];
      const otherUsers = users.slice(0, -1);
      return `${otherUsers.map(u => u.name).join(', ')} and ${lastUser.name}`;
    } else {
      return `${users.slice(0, 3).map(u => u.name).join(', ')} and ${users.length - 3} others`;
    }
  };

  if (reactionGroups.length === 0 && !showEmojiPicker) {
    return null;
  }

  return (
    <div className="flex items-center space-x-1 mt-1">
      {/* Existing reactions */}
      {reactionGroups.map(group => (
        <div
          key={group.emoji}
          className="relative"
          onMouseEnter={() => setHoveredReaction(group.emoji)}
          onMouseLeave={() => setHoveredReaction(null)}
        >
          <Button
            variant={group.hasUserReacted ? "default" : "outline"}
            size="sm"
            onClick={() => handleReactionClick(group.emoji, group.hasUserReacted)}
            className={`h-6 px-2 text-xs space-x-1 ${
              group.hasUserReacted
                ? 'bg-primary/10 border-primary/30 text-primary hover:bg-primary/20'
                : 'bg-muted border-border text-foreground hover:bg-muted/80'
            }`}
          >
            <span>{group.emoji}</span>
            <span className="font-medium">{group.count}</span>
          </Button>

          {/* Tooltip showing users who reacted */}
          {hoveredReaction === group.emoji && (
            <div className="absolute bottom-full left-1/2 transform -translate-x-1/2 mb-2 z-10">
              <div className="bg-popover text-popover-foreground text-xs rounded px-2 py-1 whitespace-nowrap border border-border shadow-md">
                <div className="font-medium">{group.emoji}</div>
                <div>{getUsersString(group.users)}</div>
                {/* Arrow */}
                <div className="absolute top-full left-1/2 transform -translate-x-1/2 border-l-4 border-r-4 border-t-4 border-transparent border-t-popover" />
              </div>
            </div>
          )}
        </div>
      ))}

      {/* Add reaction button */}
      <div className="relative" ref={emojiPickerRef}>
        <Button
          variant="ghost"
          size="icon"
          onClick={() => setShowEmojiPicker(!showEmojiPicker)}
          className="h-6 w-6 text-muted-foreground hover:text-foreground"
        >
          <Plus className="h-3 w-3" />
        </Button>

        {/* Emoji picker */}
        {showEmojiPicker && (
          <div className="absolute bottom-full left-0 mb-2 z-50">
            <EmojiPicker
              onEmojiClick={handleEmojiSelect}
              width={300}
              height={350}
              previewConfig={{ showPreview: false }}
              skinTonesDisabled
              searchDisabled
            />
          </div>
        )}
      </div>
    </div>
  );
};