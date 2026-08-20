'use client'

import React, { useEffect } from 'react'
import { useRouter } from 'next/navigation'
import { useAuth } from '@/contexts/AuthContext'

interface ProtectedRouteProps {
  children: React.ReactNode
  requireEmailVerification?: boolean
  requirePhoneVerification?: boolean
  requireTaxCompliance?: boolean
  redirectTo?: string
}

export default function ProtectedRoute({
  children,
  requireEmailVerification = true,
  requirePhoneVerification = false,
  requireTaxCompliance = false,
  redirectTo = '/login'
}: ProtectedRouteProps) {
  // BUG-HIGH-003 FIX: Use isInitialized to prevent race condition
  const { user, isAuthenticated, isLoading, isInitialized } = useAuth()
  const router = useRouter()

  useEffect(() => {
    // BUG-HIGH-003 FIX: Only make routing decisions after initialization is complete
    if (isInitialized && !isLoading) {
      if (!isAuthenticated) {
        router.push(redirectTo)
        return
      }

      if (user) {
        // Check email verification requirement
        if (requireEmailVerification && !user.emailVerified) {
          router.push('/verify-email')
          return
        }

        // Check phone verification requirement
        if (requirePhoneVerification && !user.phoneVerified) {
          router.push('/profile/me')
          return
        }

        // Check tax compliance requirement
        if (requireTaxCompliance && !user.taxCompliant) {
          router.push('/dashboard')
          return
        }
      }
    }
  }, [isAuthenticated, isLoading, isInitialized, user, router, requireEmailVerification, requirePhoneVerification, requireTaxCompliance, redirectTo])

  // BUG-HIGH-003 FIX: Show loading spinner until initialization is complete
  if (isLoading || !isInitialized) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto"></div>
          <p className="mt-4 text-muted-foreground">Loading...</p>
        </div>
      </div>
    )
  }

  // Don't render children if not authenticated or requirements not met
  if (!isAuthenticated) {
    return null
  }

  if (user) {
    if (requireEmailVerification && !user.emailVerified) {
      return null
    }

    if (requirePhoneVerification && !user.phoneVerified) {
      return null
    }

    if (requireTaxCompliance && !user.taxCompliant) {
      return null
    }
  }

  return <>{children}</>
}