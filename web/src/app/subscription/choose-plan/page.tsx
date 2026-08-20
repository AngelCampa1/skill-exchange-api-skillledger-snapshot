'use client'

import { logger } from'@/utils/logger'

import { useEffect, useCallback } from'react'
import { useRouter } from'next/navigation'
import Link from'next/link'
import { CheckCircle } from'lucide-react'
import { TierSelectionFlow } from'@/components/TierSelectionFlow'
import { ThemeToggle } from'@/components/ThemeToggle'
import { useAuth } from'@/contexts/AuthContext'
import { useSubscription } from'@/lib/subscription-api'

export default function ChoosePlanPage() {
  const router = useRouter()
  const { user, isAuthenticated, isLoading: authLoading } = useAuth()
  const { subscription, loading: subscriptionLoading } = useSubscription()

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push('/register')
    }
  }, [authLoading, isAuthenticated, router])

  useEffect(() => {
    if (!authLoading && !subscriptionLoading && isAuthenticated && subscription) {
      router.push('/dashboard')
    }
  }, [authLoading, subscriptionLoading, isAuthenticated, subscription, router])

  const handleCheckoutSuccess = (result: unknown) => {
    logger.debug('Checkout successful:', result as Record<string, unknown>)
  }

  const handleCheckoutError = (error: Error) => {
    logger.error('Checkout failed:', error)
  }

  const handleSignOut = useCallback(async () => {
    try {
      await fetch('/api/auth/logout', { method:'POST', credentials:'include' })
    } catch {
      // ignore logout errors — redirect anyway
    }
    window.location.href ='/login'
  }, [])

  if (authLoading || subscriptionLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center space-md animate-fade-in">
          <div className="loading-spinner mx-auto animate-glow"></div>
          <p className="text-body text-muted-foreground">Loading plans...</p>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return null
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-background via-primary/5 to-secondary/10">
      {/* Navigation */}
      <nav className="bg-card/90 backdrop-blur-xl border-b border-border/50 sticky top-0 z-50 shadow-lg shadow-primary/5">
        <div className="container-premium">
          <div className="flex justify-between items-center h-20">
            <Link
              href="/"
              className="flex items-center text-heading text-foreground hover:text-primary transition-colors duration-300"
            >
              Back to Home
            </Link>

            <div className="flex items-center space-golden-sm">
              {user?.userName && (
                <>
                  <span className="text-caption text-muted-foreground">Signed in as</span>
                  <span className="text-body text-foreground ml-2">{user.userName}</span>
                </>
              )}
              <ThemeToggle />
              <button
                onClick={handleSignOut}
                className="text-sm text-muted-foreground hover:text-foreground transition-colors ml-4 bg-transparent border-none cursor-pointer p-0"
              >
                Sign out
              </button>
            </div>
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="container-premium py-12 lg:py-20">
        <div className="max-w-7xl mx-auto space-y-12">
          {/* Header */}
          <header className="text-center space-y-6 animate-fade-in">
            <div className="inline-flex items-center px-4 py-2 bg-primary/10 text-primary rounded-full text-sm font-medium">
              <CheckCircle className="w-4 h-4 mr-2" />
              All plans include a 30-day free trial
            </div>

            <h1 className="text-4xl sm:text-5xl lg:text-6xl font-black tracking-tight">
              <span className="bg-gradient-to-r from-primary via-primary to-secondary bg-clip-text text-transparent">
                Choose Your Plan
              </span>
            </h1>

            <p className="text-lg text-muted-foreground max-w-3xl mx-auto leading-relaxed">
              Pick the plan that fits your work. Your card is collected now, but you won&apos;t be
              charged until your 30-day trial ends. Cancel anytime.
            </p>
          </header>

          {/* Trust Indicators */}
          <div className="flex flex-wrap justify-center items-center gap-8 py-8 border-y border-border/50">
            <div className="flex items-center space-golden-sm">
              <div className="w-12 h-12 bg-success/10 rounded-xl flex items-center justify-center">
                <CheckCircle className="w-6 h-6 text-success" />
              </div>
              <div>
                <div className="text-subheading text-foreground">30-Day Trial</div>
                <div className="text-caption text-muted-foreground">Full access, no charge</div>
              </div>
            </div>

            <div className="flex items-center space-golden-sm">
              <div className="w-12 h-12 bg-primary/10 rounded-xl flex items-center justify-center">
                <CheckCircle className="w-6 h-6 text-primary" />
              </div>
              <div>
                <div className="text-subheading text-foreground">Cancel Anytime</div>
                <div className="text-caption text-muted-foreground">No long-term contracts</div>
              </div>
            </div>

            <div className="flex items-center space-golden-sm">
              <div className="w-12 h-12 bg-secondary/10 rounded-xl flex items-center justify-center">
                <CheckCircle className="w-6 h-6 text-secondary" />
              </div>
              <div>
                <div className="text-subheading text-foreground">Secure Checkout</div>
                <div className="text-caption text-muted-foreground">Powered by Stripe</div>
              </div>
            </div>
          </div>

          {/* Tier Selection Flow */}
          <section className="animate-slide-in">
            <TierSelectionFlow
              onCheckoutSuccess={handleCheckoutSuccess}
              onCheckoutError={handleCheckoutError}
            />
          </section>
        </div>
      </main>
    </div>
  )
}
