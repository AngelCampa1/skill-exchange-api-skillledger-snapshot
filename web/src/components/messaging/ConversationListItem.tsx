/**
 * ConversationListItem - Individual conversation preview item for the conversation list
 */

import React from 'react'
import { formatDistanceToNow } from 'date-fns'
import { Archive } from 'lucide-react'
import { Avatar } from '../ui/avatar'
import { Badge } from '../ui/badge'
import { ConversationPreview, WorkspaceStatus } from '@/types/conversations'

interface ConversationListItemProps {
  conversation: ConversationPreview
  isSelected: boolean
  onClick: (id: string) => void
}

export const ConversationListItem: React.FC<ConversationListItemProps> = ({
  conversation,
  isSelected,
  onClick,
}) => {
  const {
    id,
    projectTitle,
    otherParticipantName,
    status,
    lastActivity,
    createdAt,
    unreadCount,
    lastMessagePreview,
    otherParticipantAvatar,
  } = conversation

  const isArchived = status === WorkspaceStatus.Archived

  // Format the timestamp
  const timestamp = lastActivity || createdAt
  const formattedTime = timestamp
    ? formatDistanceToNow(new Date(timestamp), { addSuffix: true })
    : ''

  // Handle click
  const handleClick = () => {
    onClick(id)
  }

  // Handle keyboard navigation
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault()
      onClick(id)
    }
  }

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={handleClick}
      onKeyDown={handleKeyDown}
      className={`
        w-full p-3 flex items-start gap-3 cursor-pointer transition-colors
        border-b border-border last:border-b-0
        focus:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-inset
        ${isSelected
          ? 'bg-primary/10 border-l-2 border-l-primary'
          : 'hover:bg-muted/50'
        }
        ${isArchived ? 'opacity-60' : ''}
      `}
    >
      {/* Avatar */}
      <Avatar
        src={otherParticipantAvatar}
        fallback={otherParticipantName || 'U'}
        size="md"
        className="flex-shrink-0"
      />

      {/* Content */}
      <div className="flex-1 min-w-0">
        {/* Top row: Title and timestamp */}
        <div className="flex items-center justify-between gap-2">
          <h4 className="text-sm font-semibold text-foreground truncate">
            {projectTitle}
          </h4>
          <div className="flex items-center gap-2 flex-shrink-0">
            {isArchived && (
              <Archive className="h-3 w-3 text-muted-foreground" />
            )}
            {formattedTime && (
              <span className="text-xs text-muted-foreground whitespace-nowrap">
                {formattedTime}
              </span>
            )}
          </div>
        </div>

        {/* Participant name */}
        <p className="text-xs text-muted-foreground truncate mt-0.5">
          {otherParticipantName || 'Unknown'}
        </p>

        {/* Bottom row: Message preview and unread badge */}
        <div className="flex items-center justify-between gap-2 mt-1">
          <p className="text-xs text-muted-foreground truncate line-clamp-1">
            {lastMessagePreview || 'No messages yet'}
          </p>
          {typeof unreadCount === 'number' && unreadCount > 0 && (
            <Badge
              variant="default"
              size="sm"
              className="flex-shrink-0 min-w-[20px] justify-center"
            >
              {unreadCount > 99 ? '99+' : unreadCount}
            </Badge>
          )}
        </div>
      </div>
    </div>
  )
}

ConversationListItem.displayName = 'ConversationListItem'
