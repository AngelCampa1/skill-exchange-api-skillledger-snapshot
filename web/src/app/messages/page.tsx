'use client'

import React from 'react'
import Link from 'next/link'
import { MessageSquare, Mail, Users, ArrowRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'

/**
 * Messages Inbox Page
 * Shown when no conversation is selected (welcome/empty state)
 */
export default function MessagesPage() {
  return (
    <div className="h-full flex items-center justify-center p-6 bg-muted/20">
      <Card className="max-w-md w-full">
        <CardContent className="p-8 text-center">
          {/* Icon */}
          <div className="flex justify-center mb-6">
            <div className="relative">
              <div className="w-20 h-20 rounded-full bg-primary/10 flex items-center justify-center">
                <MessageSquare className="w-10 h-10 text-primary" />
              </div>
              <div className="absolute -bottom-1 -right-1 w-8 h-8 rounded-full bg-success/10 flex items-center justify-center border-4 border-card">
                <Mail className="w-4 h-4 text-success" />
              </div>
            </div>
          </div>

          {/* Title */}
          <h2 className="text-xl font-semibold text-foreground mb-2">
            Select a Conversation
          </h2>

          {/* Description */}
          <p className="text-muted-foreground mb-6">
            Choose a conversation from the sidebar to start messaging, or browse
            projects to connect with collaborators.
          </p>

          {/* Features list */}
          <div className="text-left space-y-3 mb-6 bg-muted/50 rounded-lg p-4">
            <div className="flex items-start gap-3">
              <div className="w-8 h-8 rounded-full bg-primary/10 flex items-center justify-center flex-shrink-0">
                <MessageSquare className="w-4 h-4 text-primary" />
              </div>
              <div>
                <p className="text-sm font-medium text-foreground">
                  Real-time Messaging
                </p>
                <p className="text-xs text-muted-foreground">
                  Instant communication with typing indicators
                </p>
              </div>
            </div>
            <div className="flex items-start gap-3">
              <div className="w-8 h-8 rounded-full bg-success/10 flex items-center justify-center flex-shrink-0">
                <Users className="w-4 h-4 text-success" />
              </div>
              <div>
                <p className="text-sm font-medium text-foreground">
                  Project Collaboration
                </p>
                <p className="text-xs text-muted-foreground">
                  Connect with clients and service providers
                </p>
              </div>
            </div>
          </div>

          {/* Actions */}
          <div className="flex flex-col sm:flex-row gap-3 justify-center">
            <Link href="/projects/search">
              <Button variant="default" className="w-full sm:w-auto">
                Browse Projects
                <ArrowRight className="ml-2 h-4 w-4" />
              </Button>
            </Link>
            <Link href="/dashboard">
              <Button variant="outline" className="w-full sm:w-auto">
                Go to Dashboard
              </Button>
            </Link>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
