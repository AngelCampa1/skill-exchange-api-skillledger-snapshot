'use client'

import React, { useEffect, useState } from 'react'
import { useRouter, usePathname } from 'next/navigation'
import { Menu, X, MessageSquare } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import { useConversations } from '@/hooks/useConversations'
import { ConversationList } from '@/components/messaging'
import { Button } from '@/components/ui/button'

/**
 * Messages Layout
 * Provides responsive layout with conversation sidebar and main content area
 */
export default function MessagesLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const { isAuthenticated, isLoading: authLoading } = useAuth()
  const router = useRouter()
  const pathname = usePathname()

  const {
    conversations,
    selectedId,
    selectConversation,
    isLoading,
    isRefreshing,
    refresh,
  } = useConversations()

  // Mobile sidebar state
  const [isSidebarOpen, setIsSidebarOpen] = useState(false)

  // Extract workspace ID from pathname if on a conversation page
  const currentWorkspaceId = pathname.match(/\/messages\/([^/]+)/)?.[1] || null

  // Sync selected conversation with URL
  useEffect(() => {
    if (currentWorkspaceId && currentWorkspaceId !== selectedId) {
      selectConversation(currentWorkspaceId)
    }
  }, [currentWorkspaceId, selectedId, selectConversation])

  // Handle authentication
  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push('/login?redirect=/messages')
    }
  }, [isAuthenticated, authLoading, router])

  // Handle conversation selection
  const handleSelectConversation = (id: string) => {
    selectConversation(id)
    router.push(`/messages/${id}`)
    setIsSidebarOpen(false) // Close sidebar on mobile after selection
  }

  // Show loading state while checking authentication
  if (authLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <div className="loading-spinner mx-auto"></div>
          <p className="mt-4 text-muted-foreground">Loading...</p>
        </div>
      </div>
    )
  }

  // Don't render if not authenticated (will redirect)
  if (!isAuthenticated) {
    return null
  }

  return (
    <div className="min-h-screen bg-background flex flex-col">
      {/* Mobile header */}
      <div className="lg:hidden flex items-center justify-between p-4 border-b border-border bg-card">
        <div className="flex items-center gap-2">
          <MessageSquare className="h-5 w-5 text-primary" />
          <h1 className="text-lg font-semibold">Messages</h1>
        </div>
        <Button
          variant="ghost"
          size="icon"
          onClick={() => setIsSidebarOpen(!isSidebarOpen)}
          aria-label={isSidebarOpen ? 'Close menu' : 'Open menu'}
        >
          {isSidebarOpen ? (
            <X className="h-5 w-5" />
          ) : (
            <Menu className="h-5 w-5" />
          )}
        </Button>
      </div>

      <div className="flex flex-1 overflow-hidden">
        {/* Sidebar - Desktop */}
        <aside className="hidden lg:flex lg:w-80 xl:w-96 flex-shrink-0 border-r border-border bg-card">
          <ConversationList
            conversations={conversations}
            selectedId={selectedId}
            onSelect={handleSelectConversation}
            isLoading={isLoading}
            isRefreshing={isRefreshing}
            onRefresh={refresh}
            className="w-full"
          />
        </aside>

        {/* Sidebar - Mobile (overlay) */}
        {isSidebarOpen && (
          <>
            {/* Backdrop */}
            <div
              className="lg:hidden fixed inset-0 bg-overlay/50 z-40"
              onClick={() => setIsSidebarOpen(false)}
            />
            {/* Sidebar drawer */}
            <aside className="lg:hidden fixed inset-y-0 left-0 w-80 max-w-[85vw] bg-card border-r border-border z-50 shadow-xl">
              <div className="flex items-center justify-between p-4 border-b border-border">
                <h2 className="font-semibold">Conversations</h2>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => setIsSidebarOpen(false)}
                  aria-label="Close sidebar"
                >
                  <X className="h-5 w-5" />
                </Button>
              </div>
              <ConversationList
                conversations={conversations}
                selectedId={selectedId}
                onSelect={handleSelectConversation}
                isLoading={isLoading}
                isRefreshing={isRefreshing}
                onRefresh={refresh}
                className="h-[calc(100vh-65px)]"
              />
            </aside>
          </>
        )}

        {/* Main content area */}
        <main className="flex-1 overflow-hidden">
          {children}
        </main>
      </div>
    </div>
  )
}
