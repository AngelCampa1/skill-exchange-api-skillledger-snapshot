'use client'

import { useRouter } from 'next/navigation'
import { useEffect } from 'react'

/**
 * Projects List Page - Redirects to search
 * This page redirects to /projects/search for the full project browsing experience
 */
export default function ProjectsPage() {
  const router = useRouter()

  useEffect(() => {
    // Redirect to the search page
    router.push('/projects/search')
  }, [router])

  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="text-center space-md animate-fade-in">
        <div className="loading-spinner mx-auto animate-glow"></div>
        <p className="text-body text-muted-foreground">Loading projects...</p>
      </div>
    </div>
  )
}

