'use client'

import { useEffect } from 'react'
import { useRouter } from 'next/navigation'

// BUG-003 FIX: Redirect /profile to /profile/me
export default function ProfileRedirect() {
  const router = useRouter()

  useEffect(() => {
    router.replace('/profile/me')
  }, [router])

  return (
    <div className="min-h-screen bg-background flex items-center justify-center">
      <div className="text-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto"></div>
        <p className="mt-4 text-muted-foreground">Redirecting to your profile...</p>
      </div>
    </div>
  )
}
