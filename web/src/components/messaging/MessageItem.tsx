import { logger } from '@/utils/logger';
/**
 * MessageItem - Individual message component with reactions, editing, and file attachments
 */

import React, { useState, useRef, useEffect } from 'react';
import Image from 'next/image';
import { format } from 'date-fns';
import { 
  MoreHorizontal, 
  Edit, 
  Trash2, 
  Reply, 
  Copy, 
  Download,
  Check,
  CheckCheck,
  Clock,
  AlertCircle,
  FileText,
  Image as ImageIcon,
  Mic,
  Play,
  Pause
} from 'lucide-react';
import { Button } from '../ui/button';
import { Message, MessageType, MessageStatus } from '../../types/messaging';
import { messagingApiService } from '../../services/messagingApiService';
import { EmojiReactions } from './EmojiReactions';

interface MessageItemProps {
  message: Message;
  isCurrentUser: boolean;
  showAvatar: boolean;
  showSender: boolean;
  showTimestamp: boolean;
  workspaceId: string;
}

export const MessageItem: React.FC<MessageItemProps> = ({
  message,
  isCurrentUser,
  showAvatar,
  showSender,
  showTimestamp,
  workspaceId
}) => {
  const [showMenu, setShowMenu] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [editText, setEditText] = useState(message.messageText || '');
  const [isPlaying, setIsPlaying] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const audioRef = useRef<HTMLAudioElement>(null);

  // Close menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setShowMenu(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleEdit = async () => {
    if (!editText.trim()) return;
    
    try {
      await messagingApiService.editMessage(message.id, {
        messageText: editText.trim()
      });
      setIsEditing(false);
      setShowMenu(false);
    } catch (error) {
      logger.error('Failed to edit message:', error);
    }
  };

  const handleDelete = async () => {
    if (!confirm('Are you sure you want to delete this message?')) return;
    
    try {
      await messagingApiService.deleteMessage(message.id);
      setShowMenu(false);
    } catch (error) {
      logger.error('Failed to delete message:', error);
    }
  };

  const handleCopy = () => {
    navigator.clipboard.writeText(message.messageText || '');
    setShowMenu(false);
  };

  const handleDownload = async () => {
    if (message.attachmentUrl && message.attachmentFileName) {
      try {
        await messagingApiService.downloadFile(message.attachmentUrl, message.attachmentFileName);
      } catch (error) {
        logger.error('Failed to download file:', error);
      }
    }
    setShowMenu(false);
  };

  const handleVoicePlayPause = () => {
    if (audioRef.current) {
      if (isPlaying) {
        audioRef.current.pause();
      } else {
        audioRef.current.play();
      }
      setIsPlaying(!isPlaying);
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const getStatusIcon = () => {
    switch (message.status) {
      case MessageStatus.Sent:
        return <Clock className="h-3 w-3 text-muted-foreground" />;
      case MessageStatus.Delivered:
        return <Check className="h-3 w-3 text-muted-foreground" />;
      case MessageStatus.Read:
        return <CheckCheck className="h-3 w-3 text-info" />;
      case MessageStatus.Failed:
        return <AlertCircle className="h-3 w-3 text-destructive" />;
      default:
        return null;
    }
  };

  const renderMessageContent = () => {
    switch (message.messageType) {
      case MessageType.Text:
        if (isEditing && isCurrentUser) {
          return (
            <div className="space-y-2">
              <textarea
                value={editText}
                onChange={(e) => setEditText(e.target.value)}
                className="w-full p-2 border border-input rounded-md resize-none focus:outline-none focus:ring-2 focus:ring-ring"
                rows={3}
                autoFocus
              />
              <div className="flex space-x-2">
                <Button size="sm" onClick={handleEdit} disabled={!editText.trim()}>
                  Save
                </Button>
                <Button 
                  size="sm" 
                  variant="ghost" 
                  onClick={() => {
                    setIsEditing(false);
                    setEditText(message.messageText || '');
                  }}
                >
                  Cancel
                </Button>
              </div>
            </div>
          );
        }
        return (
          <div className="whitespace-pre-wrap break-words">
            {message.messageText}
            {message.isEdited && (
              <span className="text-xs text-muted-foreground ml-2">(edited)</span>
            )}
          </div>
        );

      case MessageType.Image:
        return (
          <div className="space-y-2">
            {message.attachmentUrl && (
              <Image
                src={message.attachmentUrl}
                alt={message.attachmentFileName || 'Image'}
                width={300}
                height={256}
                className="max-w-xs max-h-64 rounded-lg cursor-pointer hover:opacity-90 object-cover"
                onClick={() => window.open(message.attachmentUrl, '_blank')}
              />
            )}
            {message.messageText && (
              <div className="whitespace-pre-wrap break-words text-sm">
                {message.messageText}
              </div>
            )}
          </div>
        );

      case MessageType.File:
        return (
          <div className="space-y-2">
            <div className="flex items-center space-x-3 p-3 bg-muted rounded-lg max-w-xs">
              <FileText className="h-8 w-8 text-info" />
              <div className="flex-1 min-w-0">
                <div className="text-sm font-medium text-foreground truncate">
                  {message.attachmentFileName}
                </div>
                <div className="text-xs text-muted-foreground">
                  {message.attachmentSize && formatFileSize(message.attachmentSize)}
                </div>
              </div>
              <Button
                size="icon"
                variant="ghost"
                onClick={handleDownload}
                className="h-8 w-8"
              >
                <Download className="h-4 w-4" />
              </Button>
            </div>
            {message.messageText && (
              <div className="whitespace-pre-wrap break-words text-sm">
                {message.messageText}
              </div>
            )}
          </div>
        );

      case MessageType.Voice:
        return (
          <div className="space-y-2">
            <div className="flex items-center space-x-3 p-3 bg-info/10 rounded-lg max-w-xs">
              <Button
                size="icon"
                variant="ghost"
                onClick={handleVoicePlayPause}
                className="h-8 w-8"
              >
                {isPlaying ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
              </Button>
              <div className="flex-1">
                <Mic className="h-4 w-4 text-info" />
                <span className="text-sm text-foreground ml-2">Voice message</span>
              </div>
              {message.attachmentUrl && (
                <audio
                  ref={audioRef}
                  src={message.attachmentUrl}
                  onEnded={() => setIsPlaying(false)}
                  preload="metadata"
                />
              )}
            </div>
            {message.messageText && (
              <div className="whitespace-pre-wrap break-words text-sm">
                {message.messageText}
              </div>
            )}
          </div>
        );

      case MessageType.System:
        return (
          <div className="text-sm text-muted-foreground italic text-center py-2">
            {message.messageText}
          </div>
        );

      case MessageType.Milestone:
        return (
          <div className="bg-success/10 border border-success/20 rounded-lg p-3">
            <div className="flex items-center space-x-2 text-success">
              <CheckCheck className="h-4 w-4" />
              <span className="font-medium">Milestone Update</span>
            </div>
            <div className="mt-2 text-sm text-success">
              {message.messageText}
            </div>
          </div>
        );

      default:
        return <div>{message.messageText}</div>;
    }
  };

  // System messages have different styling
  if (message.messageType === MessageType.System) {
    return (
      <div className="flex justify-center my-2">
        {renderMessageContent()}
      </div>
    );
  }

  return (
    <div
      className={`group flex items-start space-x-3 hover:bg-muted/50 px-2 py-1 rounded ${
        isCurrentUser ? 'flex-row-reverse space-x-reverse' : ''
      }`}
    >
      {/* Avatar */}
      <div className="flex-shrink-0">
        {showAvatar && !isCurrentUser && (
          <Image
            src={message.senderAvatar || '/default-avatar.png'}
            alt={message.senderName}
            width={32}
            height={32}
            className="h-8 w-8 rounded-full"
          />
        )}
        {!showAvatar && !isCurrentUser && <div className="h-8 w-8" />}
      </div>

      {/* Message Content */}
      <div className={`flex-1 min-w-0 ${isCurrentUser ? 'text-right' : ''}`}>
        {/* Reply indicator */}
        {message.replyToMessage && (
          <div className="text-xs text-muted-foreground mb-1 p-2 bg-muted rounded border-l-2 border-border">
            <div className="font-medium">{message.replyToMessage.senderName}</div>
            <div className="truncate">{message.replyToMessage.messageText}</div>
          </div>
        )}

        {/* Message bubble */}
        <div
          className={`relative max-w-lg ${
            isCurrentUser
              ? 'bg-primary text-primary-foreground ml-auto'
              : 'bg-card border border-border'
          } rounded-lg px-3 py-2`}
        >
          {renderMessageContent()}

          {/* Message menu */}
          <div className={`absolute top-0 ${isCurrentUser ? 'left-0' : 'right-0'} -translate-y-full opacity-0 group-hover:opacity-100 transition-opacity`}>
            <div className="relative" ref={menuRef}>
              <Button
                size="icon"
                variant="ghost"
                onClick={() => setShowMenu(!showMenu)}
                className="h-6 w-6 bg-card shadow-sm border"
              >
                <MoreHorizontal className="h-3 w-3" />
              </Button>

              {showMenu && (
                <div className="absolute top-full right-0 mt-1 bg-popover border border-border rounded-lg shadow-lg py-1 z-10 min-w-[120px]">
                  {message.messageType === MessageType.Text && (
                    <button
                      onClick={handleCopy}
                      className="w-full text-left px-3 py-2 text-sm hover:bg-muted flex items-center"
                    >
                      <Copy className="h-3 w-3 mr-2" />
                      Copy
                    </button>
                  )}

                  <button
                    onClick={() => {/* Reply logic */}}
                    className="w-full text-left px-3 py-2 text-sm hover:bg-muted flex items-center"
                  >
                    <Reply className="h-3 w-3 mr-2" />
                    Reply
                  </button>

                  {(message.attachmentUrl && message.attachmentFileName) && (
                    <button
                      onClick={handleDownload}
                      className="w-full text-left px-3 py-2 text-sm hover:bg-muted flex items-center"
                    >
                      <Download className="h-3 w-3 mr-2" />
                      Download
                    </button>
                  )}

                  {isCurrentUser && message.canEdit && message.messageType === MessageType.Text && (
                    <button
                      onClick={() => {
                        setIsEditing(true);
                        setShowMenu(false);
                      }}
                      className="w-full text-left px-3 py-2 text-sm hover:bg-muted flex items-center"
                    >
                      <Edit className="h-3 w-3 mr-2" />
                      Edit
                    </button>
                  )}

                  {isCurrentUser && message.canDelete && (
                    <button
                      onClick={handleDelete}
                      className="w-full text-left px-3 py-2 text-sm hover:bg-muted flex items-center text-destructive"
                    >
                      <Trash2 className="h-3 w-3 mr-2" />
                      Delete
                    </button>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Reactions */}
        {message.reactions.length > 0 && (
          <div className="mt-1">
            <EmojiReactions
              messageId={message.id}
              reactions={message.reactions}
              workspaceId={workspaceId}
            />
          </div>
        )}

        {/* Timestamp and status */}
        {showTimestamp && (
          <div className={`flex items-center space-x-1 mt-1 text-xs text-muted-foreground ${
            isCurrentUser ? 'justify-end' : ''
          }`}>
            <span>{format(new Date(message.createdAt), 'h:mm a')}</span>
            {isCurrentUser && getStatusIcon()}
          </div>
        )}
      </div>
    </div>
  );
};