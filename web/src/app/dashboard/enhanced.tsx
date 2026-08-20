'use client'

import Link from 'next/link'
import { useAuth } from '@/contexts/AuthContext'
import { ThemeToggle } from '@/components/ThemeToggle'
import { EnhancedNavigation } from '@/components/EnhancedNavigation'
import { EnhancedDashboardContent } from '@/components/EnhancedDashboardContent'

export default function EnhancedDashboardPage() {
  const { user, isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center space-md animate-fade-in">
          <div className="loading-spinner mx-auto animate-pulse-glow"></div>
          <p className="text-body text-muted-foreground">Loading your workspace...</p>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return null // Middleware will redirect
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-background via-primary/5 to-secondary/10">
      {/* Enhanced Navigation with Rich Animations */}
      <EnhancedNavigation />

      {/* Enhanced Dashboard Content with Rich Visual Effects */}
      <main className="container-premium py-16 lg:py-24 relative" role="main" aria-label="Dashboard content">
        {/* Enhanced Background Elements */}
        <div className="absolute top-20 right-10 w-72 h-72 bg-gradient-to-br from-primary/20 to-secondary/20 rounded-full blur-3xl opacity-60 animate-float-3d" aria-hidden="true"></div>
        <div className="absolute bottom-40 left-10 w-96 h-96 bg-gradient-to-tr from-secondary/15 to-primary/15 rounded-full blur-3xl opacity-50 animate-pendulum" aria-hidden="true"></div>

        <EnhancedDashboardContent />
      </main>
    </div>
  )
}