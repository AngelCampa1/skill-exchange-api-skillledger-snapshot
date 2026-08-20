'use client'

import React, { useEffect, useState } from 'react'
import { useParams, useRouter } from 'next/navigation'
import { ArrowLeft, AlertCircle } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import { MessageCenter } from '@/components/messaging'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { messagingApiService } from '@/services/messagingApiService'
import { ConversationDetails } from '@/types/conversations'

/**
 * Conversation Detail Page
 * Displays the messaging interface for a specific workspace/conversation
 */
export default function ConversationPage() {
  const params = useParams()
  const router = useRouter()
  const { user, isAuthenticated } = useAuth()

  const workspaceId = params.workspaceId as string

  const [workspace, setWorkspace] = useState<ConversationDetails | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Fetch workspace details
  useEffect(() => {
    const fetchWorkspace = async () => {
      if (!workspaceId || !isAuthenticated) return

      try {
        setIsLoading(true)
        setError(null)
        const data = await messagingApiService.getWorkspaceDetails(workspaceId)
        setWorkspace(data)
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Failed to load conversation'
        setError(message)
      } finally {
        setIsLoading(false)
      }
    }

    fetchWorkspace()
  }, [workspaceId, isAuthenticated])

  // Handle back navigation
  const handleBack = () => {
    router.push('/messages')
  }

  // Loading state
  if (isLoading) {
    return (
      <div className="h-full flex flex-col">
        {/* Header skeleton */}
        <div className="flex items-center gap-3 p-4 border-b border-border bg-card">
          <Skeleton variant="circular" className="h-10 w-10" />
          <div className="space-y-2">
            <Skeleton variant="text" className="h-4 w-32" />
            <Skeleton variant="text" className="h-3 w-24" />
          </div>
        </div>
        {/* Content skeleton */}
        <div className="flex-1 p-4 space-y-4">
          {[1, 2, 3].map((i) => (
            <div key={i} className="flex gap-3">
              <Skeleton variant="circular" className="h-8 w-8" />
              <Skeleton variant="rectangular" className="h-16 w-64 rounded-lg" />
            </div>
          ))}
        </div>
      </div>
    )
  }

  // Error state
  if (error) {
    return (
      <div className="h-full flex items-center justify-center p-6">
        <Card className="max-w-md w-full">
          <CardContent className="p-8 text-center">
            <div className="w-16 h-16 rounded-full bg-destructive/10 flex items-center justify-center mx-auto mb-4">
              <AlertCircle className="w-8 h-8 text-destructive" />
            </div>
            <h2 className="text-xl font-semibold text-foreground mb-2">
              Unable to Load Conversation
            </h2>
            <p className="text-muted-foreground mb-6">{error}</p>
            <div className="flex flex-col sm:flex-row gap-3 justify-center">
              <Button variant="default" onClick={() => window.location.reload()}>
                Try Again
              </Button>
              <Button variant="outline" onClick={handleBack}>
                <ArrowLeft className="mr-2 h-4 w-4" />
                Back to Messages
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  // No workspace found
  if (!workspace) {
    return (
      <div className="h-full flex items-center justify-center p-6">
        <Card className="max-w-md w-full">
          <CardContent className="p-8 text-center">
            <h2 className="text-xl font-semibold text-foreground mb-2">
              Conversation Not Found
            </h2>
            <p className="text-muted-foreground mb-6">
              This conversation may have been deleted or you may not have access.
            </p>
            <Button variant="outline" onClick={handleBack}>
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Messages
            </Button>
          </CardContent>
        </Card>
      </div>
    )
  }

  // Determine if current user is client or provider
  const isClient = user?.email === workspace.clientName || user?.id === workspace.workspaceId
  const otherParticipantName = isClient ? workspace.providerName : workspace.clientName

  // Build participants array for MessageCenter
  const participants = [
    {
      id: workspace.workspaceId,
      name: workspace.clientName,
      avatar: '',
      isOnline: true,
    },
    {
      id: `provider-${workspace.workspaceId}`,
      name: workspace.providerName,
      avatar: '',
      isOnline: true,
    },
  ]

  return (
    <div className="h-full flex flex-col">
      {/* Mobile back button */}
      <div className="lg:hidden flex items-center gap-2 p-2 border-b border-border bg-card">
        <Button variant="ghost" size="icon" onClick={handleBack}>
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <div className="truncate">
          <h2 className="text-sm font-semibold truncate">{workspace.projectTitle}</h2>
          <p className="text-xs text-muted-foreground truncate">
            with {otherParticipantName}
          </p>
        </div>
      </div>

      {/* MessageCenter */}
      <div className="flex-1 overflow-hidden">
        <MessageCenter
          workspaceId={workspaceId}
          currentUserId={user?.id || ''}
          workspaceTitle={workspace.projectTitle}
          participants={participants}
          className="h-full"
        />
      </div>
    </div>
  )
}
