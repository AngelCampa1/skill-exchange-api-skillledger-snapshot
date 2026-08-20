import { logger } from '@/utils/logger';
import { trackEvent } from '@/utils/analytics';
/**
 * MessageInput - Input component with file upload, emoji picker, and typing indicators
 */

import React, { useState, useRef, useCallback, useEffect } from 'react';
import { useDropzone } from 'react-dropzone';
import {
  Send,
  Paperclip,
  Smile,
  X,
  FileText,
  Image as ImageIcon,
  Mic,
  MicOff
} from 'lucide-react';
import { Button } from '../ui/button';
import { MessageType, SendMessageRequest, FileUploadProgress } from '../../types/messaging';
import { messagingApiService } from '../../services/messagingApiService';
import { signalRService } from '../../services/signalRService';
import EmojiPicker from 'emoji-picker-react';

interface MessageInputProps {
  workspaceId: string;
  replyToMessage?: {
    id: string;
    senderName: string;
    messageText?: string;
  } | null;
  onMessageSent?: () => void;
  onCancelReply?: () => void;
}

export const MessageInput: React.FC<MessageInputProps> = ({
  workspaceId,
  replyToMessage,
  onMessageSent,
  onCancelReply
}) => {
  const [message, setMessage] = useState('');
  const [sending, setSending] = useState(false);
  const [uploadingFiles, setUploadingFiles] = useState<FileUploadProgress[]>([]);
  const [showEmojiPicker, setShowEmojiPicker] = useState(false);
  const [isRecording, setIsRecording] = useState(false);
  const [recordingTime, setRecordingTime] = useState(0);
  const [isDragging, setIsDragging] = useState(false);
  
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const emojiPickerRef = useRef<HTMLDivElement>(null);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const typingTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const recordingIntervalRef = useRef<NodeJS.Timeout | null>(null);

  // Auto-resize textarea
  const adjustTextareaHeight = useCallback(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${textareaRef.current.scrollHeight}px`;
    }
  }, []);

  useEffect(() => {
    adjustTextareaHeight();
  }, [message, adjustTextareaHeight]);

  // Handle typing indicators
  useEffect(() => {
    if (message.trim()) {
      // Send typing indicator
      signalRService.sendTypingIndicator();
      
      // Clear existing timeout
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }
      
      // Stop typing indicator after 3 seconds of inactivity
      typingTimeoutRef.current = setTimeout(() => {
        signalRService.stopTypingIndicator();
      }, 3000);
    } else {
      // Stop typing immediately when message is empty
      signalRService.stopTypingIndicator();
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }
    }

    return () => {
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }
    };
  }, [message]);

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

  // File upload handler
  const handleFileUpload = useCallback(async (file: File) => {
    const uploadId = Math.random().toString(36).substr(2, 9);
    
    // Add to uploading files
    const fileProgress: FileUploadProgress = {
      id: uploadId,
      fileName: file.name,
      fileSize: file.size,
      progress: 0,
      status: 'uploading'
    };
    
    setUploadingFiles(prev => [...prev, fileProgress]);

    try {
      // Upload file
      const uploadResult = await messagingApiService.uploadFile(file, workspaceId);
      
      // Update progress to completed
      setUploadingFiles(prev => 
        prev.map(f => f.id === uploadId 
          ? { ...f, progress: 100, status: 'completed' as const }
          : f
        )
      );

      // Send message with file attachment
      const messageType = file.type.startsWith('image/') ? MessageType.Image : MessageType.File;
      
      const sendRequest: SendMessageRequest = {
        workspaceId,
        messageText: message.trim() || undefined,
        messageType,
        attachmentUrl: uploadResult.url,
        attachmentFileName: uploadResult.fileName,
        attachmentSize: uploadResult.size,
        attachmentMimeType: uploadResult.mimeType,
        replyToMessageId: replyToMessage?.id
      };

      await messagingApiService.sendMessage(sendRequest);
      
      // Clear message and files
      setMessage('');
      setUploadingFiles(prev => prev.filter(f => f.id !== uploadId));
      
      if (onMessageSent) onMessageSent();
      if (onCancelReply) onCancelReply();
      
    } catch (error) {
      logger.error('File upload failed:', error);
      setUploadingFiles(prev => 
        prev.map(f => f.id === uploadId 
          ? { ...f, status: 'error' as const, error: 'Upload failed' }
          : f
        )
      );
    }
  }, [workspaceId, message, replyToMessage?.id, onMessageSent, onCancelReply]);

  // File drop zone
  const onDrop = useCallback(async (acceptedFiles: File[]) => {
    for (const file of acceptedFiles) {
      await handleFileUpload(file);
    }
  }, [handleFileUpload]);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    noClick: true,
    noKeyboard: true,
    onDragEnter: () => setIsDragging(true),
    onDragLeave: () => setIsDragging(false),
    onDropAccepted: () => setIsDragging(false),
    onDropRejected: () => setIsDragging(false),
  });

  const handleSendMessage = async () => {
    const messageText = message.trim();
    if (!messageText && uploadingFiles.length === 0) return;
    
    setSending(true);
    signalRService.stopTypingIndicator();

    try {
      const sendRequest: SendMessageRequest = {
        workspaceId,
        messageText,
        messageType: MessageType.Text,
        replyToMessageId: replyToMessage?.id
      };

      await messagingApiService.sendMessage(sendRequest);

      // Track message sent
      trackEvent({
        name: 'message_sent',
        category: 'messaging',
        priority: 'high',
        properties: {
          workspace_id: workspaceId,
          message_type: sendRequest.messageType,
          is_reply: !!replyToMessage,
          message_length: messageText.length,
        },
      })

      setMessage('');
      if (onMessageSent) onMessageSent();
      if (onCancelReply) onCancelReply();

    } catch (error) {
      logger.error('Failed to send message:', error);
    } finally {
      setSending(false);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  const handleEmojiSelect = (emojiObject: any) => {
    const emoji = emojiObject.emoji;
    setMessage(prev => prev + emoji);
    setShowEmojiPicker(false);
    textareaRef.current?.focus();
  };

  const startVoiceRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      mediaRecorderRef.current = new MediaRecorder(stream);
      
      const audioChunks: BlobPart[] = [];
      
      mediaRecorderRef.current.ondataavailable = (event) => {
        audioChunks.push(event.data);
      };
      
      mediaRecorderRef.current.onstop = async () => {
        const audioBlob = new Blob(audioChunks, { type: 'audio/wav' });
        const audioFile = new File([audioBlob], `voice-${Date.now()}.wav`, {
          type: 'audio/wav'
        });
        
        await handleFileUpload(audioFile);
        
        // Stop all tracks
        stream.getTracks().forEach(track => track.stop());
      };
      
      mediaRecorderRef.current.start();
      setIsRecording(true);
      setRecordingTime(0);
      
      recordingIntervalRef.current = setInterval(() => {
        setRecordingTime(prev => prev + 1);
      }, 1000);
      
    } catch (error) {
      logger.error('Failed to start recording:', error);
    }
  };

  const stopVoiceRecording = () => {
    if (mediaRecorderRef.current && isRecording) {
      mediaRecorderRef.current.stop();
      setIsRecording(false);
      
      if (recordingIntervalRef.current) {
        clearInterval(recordingIntervalRef.current);
      }
    }
  };

  const formatRecordingTime = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const removeUploadingFile = (id: string) => {
    setUploadingFiles(prev => prev.filter(f => f.id !== id));
  };

  return (
    <div className="p-4 border-t border-border bg-card">
      {/* Drag overlay */}
      {isDragging && (
        <div className="absolute inset-0 bg-primary/10 border-2 border-dashed border-primary flex items-center justify-center z-50">
          <div className="text-center">
            <Paperclip className="h-12 w-12 text-primary mx-auto mb-2" />
            <p className="text-primary font-medium">Drop files here to upload</p>
          </div>
        </div>
      )}

      <div {...getRootProps()} className="space-y-3">
        <input {...getInputProps()} />
        
        {/* Reply indicator */}
        {replyToMessage && (
          <div className="flex items-center justify-between p-3 bg-muted rounded-lg">
            <div className="flex-1">
              <div className="text-sm font-medium text-foreground">
                Replying to {replyToMessage.senderName}
              </div>
              <div className="text-sm text-muted-foreground truncate">
                {replyToMessage.messageText}
              </div>
            </div>
            {onCancelReply && (
              <Button
                size="icon"
                variant="ghost"
                onClick={onCancelReply}
                className="h-8 w-8"
              >
                <X className="h-4 w-4" />
              </Button>
            )}
          </div>
        )}

        {/* Uploading files */}
        {uploadingFiles.length > 0 && (
          <div className="space-y-2">
            {uploadingFiles.map(file => (
              <div key={file.id} className="flex items-center space-x-3 p-3 bg-muted rounded-lg">
                <FileText className="h-6 w-6 text-info" />
                <div className="flex-1">
                  <div className="text-sm font-medium">{file.fileName}</div>
                  <div className="text-xs text-muted-foreground">
                    {file.status === 'uploading' ? `${file.progress}%` : file.status}
                    {file.error && ` - ${file.error}`}
                  </div>
                  {file.status === 'uploading' && (
                    <div className="w-full bg-muted-foreground/20 rounded-full h-1 mt-1">
                      <div
                        className="bg-primary h-1 rounded-full transition-all duration-300"
                        style={{ width: `${file.progress}%` }}
                      />
                    </div>
                  )}
                </div>
                <Button
                  size="icon"
                  variant="ghost"
                  onClick={() => removeUploadingFile(file.id)}
                  className="h-8 w-8"
                >
                  <X className="h-4 w-4" />
                </Button>
              </div>
            ))}
          </div>
        )}

        {/* Message input area */}
        <div className="flex items-end space-x-3">
          <div className="flex-1">
            <div className="relative">
              <textarea
                ref={textareaRef}
                value={message}
                onChange={(e) => setMessage(e.target.value)}
                onKeyPress={handleKeyPress}
                placeholder={isRecording ? 'Recording voice message...' : 'Type a message...'}
                disabled={isRecording}
                className="w-full min-h-[40px] max-h-32 px-4 py-2 pr-12 border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent resize-none bg-background"
                rows={1}
              />
              
              {/* Emoji picker button */}
              <div className="absolute right-3 bottom-2">
                <div className="relative" ref={emojiPickerRef}>
                  <Button
                    size="icon"
                    variant="ghost"
                    onClick={() => setShowEmojiPicker(!showEmojiPicker)}
                    className="h-8 w-8"
                  >
                    <Smile className="h-4 w-4" />
                  </Button>
                  
                  {showEmojiPicker && (
                    <div className="absolute bottom-full right-0 mb-2 z-50">
                      <EmojiPicker
                        onEmojiClick={handleEmojiSelect}
                        width={300}
                        height={400}
                      />
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>

          {/* Action buttons */}
          <div className="flex items-center space-x-2">
            {/* File upload button */}
            <Button
              size="icon"
              variant="ghost"
              onClick={() => fileInputRef.current?.click()}
              disabled={isRecording}
              className="h-10 w-10"
            >
              <Paperclip className="h-4 w-4" />
            </Button>
            
            <input
              ref={fileInputRef}
              type="file"
              multiple
              className="hidden"
              onChange={(e) => {
                if (e.target.files) {
                  Array.from(e.target.files).forEach(handleFileUpload);
                }
              }}
            />

            {/* Voice recording button */}
            <Button
              size="icon"
              variant={isRecording ? "destructive" : "ghost"}
              onMouseDown={startVoiceRecording}
              onMouseUp={stopVoiceRecording}
              onTouchStart={startVoiceRecording}
              onTouchEnd={stopVoiceRecording}
              className="h-10 w-10"
            >
              {isRecording ? <MicOff className="h-4 w-4" /> : <Mic className="h-4 w-4" />}
            </Button>
            
            {isRecording && (
              <span className="text-sm text-destructive font-mono">
                {formatRecordingTime(recordingTime)}
              </span>
            )}

            {/* Send button - FE-LOW-002 FIX: Added aria-label for accessibility */}
            <Button
              onClick={handleSendMessage}
              disabled={(!message.trim() && uploadingFiles.length === 0) || sending || isRecording}
              className="h-10 px-4"
              aria-label="Send message"
            >
              {sending ? (
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-primary-foreground" />
              ) : (
                <Send className="h-4 w-4" />
              )}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
};