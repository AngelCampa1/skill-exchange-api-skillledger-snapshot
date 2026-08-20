'use client'

import { logger } from '@/utils/logger';
import React, { useState, useEffect, useRef, useCallback } from 'react'
import { Send, User } from 'lucide-react'

interface Message {
  id: string
  workspaceId: string
  senderId: string
  senderName: string
  content: string
  createdAt: string
}

interface SimpleMessagingProps {
  workspaceId: string
  currentUserId: string
  currentUserName?: string
}

export default function SimpleMessaging({
  workspaceId,
  currentUserId,
  currentUserName = 'You'
}: SimpleMessagingProps) {
  const [messages, setMessages] = useState<Message[]>([])
  const [newMessage, setNewMessage] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [isSending, setIsSending] = useState(false)
  const messagesEndRef = useRef<HTMLDivElement>(null)

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }

  const loadMessages = useCallback(async () => {
    try {
      setIsLoading(true)
      const response = await fetch(`/api/workspace/${workspaceId}/messages`, {
        credentials: 'include',
      })

      if (response.ok) {
        const data = await response.json()
        setMessages(data.messages || data || [])
      }
    } catch (error) {
      logger.error('Error loading messages', error, { component: 'SimpleMessaging' })
    } finally {
      setIsLoading(false)
    }
  }, [workspaceId])

  useEffect(() => {
    loadMessages()
    // Poll for new messages every 5 seconds
    const interval = setInterval(loadMessages, 5000)
    return () => clearInterval(interval)
  }, [workspaceId, loadMessages])

  useEffect(() => {
    scrollToBottom()
  }, [messages])

  const getCsrfToken = async (): Promise<string | null> => {
    try {
      const response = await fetch('/api/auth/csrf-token', {
        credentials: 'include',
      })
      
      if (response.ok) {
        const data = await response.json()
        return data.token
      }
    } catch (error) {
      logger.error('Failed to get CSRF token', error, { component: 'SimpleMessaging' })
    }
    
    return null
  }

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!newMessage.trim()) return

    setIsSending(true)

    try {
      const csrfToken = await getCsrfToken()
      if (!csrfToken) {
        throw new Error('Failed to get CSRF token')
      }

      const response = await fetch(`/api/workspace/${workspaceId}/messages`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-CSRF-TOKEN': csrfToken,
        },
        credentials: 'include',
        body: JSON.stringify({
          content: newMessage.trim()
        }),
      })

      if (response.ok) {
        setNewMessage('')
        // Reload messages immediately
        await loadMessages()
      } else {
        logger.error('Failed to send message', undefined, { component: 'SimpleMessaging' })
        alert('Failed to send message. Please try again.')
      }
    } catch (error) {
      logger.error('Error sending message', error, { component: 'SimpleMessaging' })
      alert('Network error. Please check your connection.')
    } finally {
      setIsSending(false)
    }
  }

  return (
    <div className="flex flex-col h-full">
      {/* Messages List */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4" data-testid="messages">
        {isLoading && messages.length === 0 ? (
          <div className="text-center py-8">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto"></div>
            <p className="mt-2 text-muted-foreground">Loading messages...</p>
          </div>
        ) : messages.length === 0 ? (
          <div className="text-center py-8">
            <p className="text-muted-foreground">No messages yet. Start the conversation!</p>
          </div>
        ) : (
          messages.map((message) => {
            const isOwnMessage = message.senderId === currentUserId
            return (
              <div
                key={message.id}
                className={`flex ${isOwnMessage ? 'justify-end' : 'justify-start'}`}
                data-testid="message-item"
              >
                <div className={`max-w-[70%] ${isOwnMessage ? 'order-2' : 'order-1'}`}>
                  <div className="flex items-center space-x-2 mb-1">
                    {!isOwnMessage && (
                      <div className="w-6 h-6 rounded-full bg-primary/10 flex items-center justify-center">
                        <User className="w-4 h-4 text-primary" />
                      </div>
                    )}
                    <span className="text-sm font-medium text-foreground">
                      {isOwnMessage ? 'You' : message.senderName}
                    </span>
                    <span className="text-xs text-muted-foreground">
                      {new Date(message.createdAt).toLocaleTimeString()}
                    </span>
                  </div>
                  <div
                    className={`rounded-lg p-3 ${
                      isOwnMessage
                        ? 'bg-primary text-primary-foreground'
                        : 'bg-muted text-foreground'
                    }`}
                  >
                    <p className="whitespace-pre-wrap break-words">{message.content}</p>
                  </div>
                </div>
              </div>
            )
          })
        )}
        <div ref={messagesEndRef} />
      </div>

      {/* Message Input */}
      <div className="border-t border-border p-4 bg-card">
        <form onSubmit={handleSendMessage} className="flex space-x-2">
          <input
            type="text"
            value={newMessage}
            onChange={(e) => setNewMessage(e.target.value)}
            placeholder="Type your message..."
            className="flex-1 px-4 py-2 border border-input rounded-lg focus:ring-2 focus:ring-ring focus:border-input"
            disabled={isSending}
            data-testid="message-input"
          />
          <button
            type="submit"
            disabled={isSending || !newMessage.trim()}
            className={`px-6 py-2 rounded-full font-medium flex items-center space-x-2 ${
              isSending || !newMessage.trim()
                ? 'bg-muted text-muted-foreground cursor-not-allowed'
                : 'bg-primary hover:bg-primary/90 text-primary-foreground'
            }`}
            data-testid="send-message-button"
          >
            {isSending ? (
              <>
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-primary-foreground"></div>
                <span>Sending...</span>
              </>
            ) : (
              <>
                <Send className="w-4 h-4" />
                <span>Send</span>
              </>
            )}
          </button>
        </form>
      </div>
    </div>
  )
}



