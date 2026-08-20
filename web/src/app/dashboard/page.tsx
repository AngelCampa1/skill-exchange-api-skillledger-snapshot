'use client'

import { logger } from'@/utils/logger';

import Link from'next/link'
import dynamic from'next/dynamic'
import { useEffect } from'react'
import { useRouter } from'next/navigation'
import { FolderPlus, Search, CheckCircle, AlertCircle, TrendingUp, Users, Briefcase, MessageSquare, Star } from'lucide-react'
import { useAuth } from'@/contexts/AuthContext'
import { useSubscription } from'@/lib/subscription-api'
import LogoutButton from'@/components/LogoutButton'
import { ThemeToggle } from'@/components/ThemeToggle'
import { EnhancedNavigation } from'@/components/EnhancedNavigation'
import { EnhancedDashboardContent } from'@/components/EnhancedDashboardContent'
import { SubscriptionDashboard } from'@/components/SubscriptionDashboard'
import { MobileNav } from'@/components/MobileNav'


export default function DashboardPage() {
  const { user, isAuthenticated, isLoading } = useAuth()
  const { subscription, loading: subscriptionLoading } = useSubscription()
  const router = useRouter()

  // Handle redirect for unauthenticated users
  useEffect(() => {
    logger.debug('Dashboard useEffect', { isLoading, isAuthenticated, user })
    if (!isLoading && !isAuthenticated) {
      logger.debug('Dashboard: User not authenticated, redirecting to login...')
      // E2E-017 FIX: Call logout API to clear any stale cookies before redirecting
      // This prevents a redirect loop where middleware sees a stale cookie and redirects back to dashboard
      fetch('/api/auth/logout', { method:'POST', credentials:'include' })
        .finally(() => {
          // Use window.location.href instead of router.push to force a full page reload
          // This ensures the middleware re-evaluates without the stale cookie
          window.location.href ='/login'
        })
    } else if (!isLoading && isAuthenticated) {
      logger.debug('Dashboard: User authenticated, staying on dashboard')
    }
  }, [isLoading, isAuthenticated, router, user])

  // Redirect to plan selection if authenticated but no subscription
  useEffect(() => {
    if (!isLoading && !subscriptionLoading && isAuthenticated && !subscription) {
      logger.debug('Dashboard: No subscription found, redirecting to choose-plan...')
      router.push('/subscription/choose-plan')
    }
  }, [isLoading, subscriptionLoading, isAuthenticated, subscription, router])

  if (isLoading || subscriptionLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center space-md animate-fade-in">
          <div className="loading-spinner mx-auto animate-glow"></div>
          <p className="text-body text-muted-foreground">Loading your workspace...</p>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center space-md animate-fade-in">
          <div className="loading-spinner mx-auto animate-glow"></div>
          <p className="text-body text-muted-foreground">Redirecting to login...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-background via-primary/5 to-secondary/10">
      {/* Modern Navigation with Gradient */}
      <nav className="bg-card/90 backdrop-blur-xl border-b border-border/50 sticky top-0 z-50 shadow-lg shadow-primary/5">
        <div className="container-premium">
          <div className="flex justify-between items-center h-24">
            <Link
              href="/dashboard"
              className="text-heading text-foreground hover:text-primary transition-colors duration-300 font-bold tracking-tight"
            >
              SkillLedger
            </Link>

            <div className="hidden md:flex items-center space-golden-md">
              <Link href="/projects/search" className="btn-ghost">Browse Projects</Link>
              <Link href="/create-project" className="btn-primary">Create Project</Link>
              <Link href="/marketplace" className="btn-ghost">Marketplace</Link>
            </div>

            <div className="flex items-center space-golden-sm">
              <MobileNav
                items={[
                  { href:'/projects/search', label:'Browse Projects' },
                  { href:'/marketplace', label:'Marketplace' },
                  { href:'/create-project', label:'Create Project', isPrimary: true },
                ]}
              />
              <ThemeToggle />
              <div className="hidden sm:block">
                {/* E2E-019 FIX: Added comma and space for proper display */}
                <span className="text-caption">Welcome back, </span>
                {/* E2E-015 FIX: Display firstName if available, fallback to userName */}
                <span className="text-body text-foreground">{user?.firstName || user?.userName}</span>
              </div>
              {/* E2E-003 FIX: Direct logout without dropdown */}
              <LogoutButton showAllDevicesOption={false} />
            </div>
          </div>
        </div>
      </nav>

      {/* Vibrant Main Content Area */}
      <main className="container-premium py-16 lg:py-24 relative" role="main" aria-label="Dashboard content">
        {/* Decorative gradient orbs */}
        <div className="absolute top-20 right-10 w-72 h-72 bg-gradient-to-br from-primary/20 to-secondary/20 rounded-full blur-3xl opacity-60 animate-float" aria-hidden="true"></div>
        <div className="absolute bottom-40 left-10 w-96 h-96 bg-gradient-to-tr from-secondary/15 to-primary/15 rounded-full blur-3xl opacity-50" aria-hidden="true"></div>

        <div className="flex flex-col gap-12 lg:gap-16 relative z-10">
          {/* Modern Dashboard Header with Gradient */}
          <header className="animate-fade-in">
            <div className="flex flex-col lg:flex-row lg:items-end lg:justify-between gap-8 bg-gradient-to-r from-primary/10 via-transparent to-secondary/10 p-10 lg:p-12 rounded-3xl border border-primary/20 shadow-xl shadow-primary/5">
              <div className="space-y-4">
                <h1 className="text-5xl sm:text-6xl lg:text-7xl font-black tracking-tight">
                  <span className="bg-gradient-to-r from-primary via-primary to-secondary bg-clip-text text-transparent">
                    Dashboard
                  </span>
                </h1>
                <p className="text-lg text-muted-foreground max-w-2xl leading-relaxed">
                  Your vibrant workspace for professional collaboration and project management
                </p>
              </div>
              <div className="flex items-center gap-3">
                <div className="relative">
                  <div className="w-3 h-3 bg-success rounded-full animate-pulse shadow-lg shadow-success/50"></div>
                  <div className="absolute inset-0 w-3 h-3 bg-success rounded-full animate-ping"></div>
                </div>
                <span className="text-sm text-muted-foreground font-semibold">Live workspace</span>
              </div>
            </div>
          </header>

          {/* Vibrant Profile Card with Gradient Border */}
          <section className="card-elevated p-10 lg:p-14 animate-slide-in relative overflow-hidden">
            <div className="absolute inset-0 bg-gradient-to-br from-primary/5 via-transparent to-secondary/5 pointer-events-none"></div>
            <div className="flex flex-col gap-10 relative z-10">
              <div className="flex items-center justify-between">
                <h2 className="text-3xl lg:text-4xl font-black tracking-tight">
                  <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                    Profile Overview
                  </span>
                </h2>
                <div className="status-success shadow-lg shadow-green-500/20">Active Account</div>
              </div>

              {/* Swiss-grid layout for profile details */}
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-10">
                <div className="space-fine">
                  <span className="text-caption block">Email Address</span>
                  <span className="text-body text-foreground">{user?.email}</span>
                </div>

                <div className="space-fine">
                  <span className="text-caption block">Username</span>
                  <span className="text-body text-foreground">{user?.userName}</span>
                </div>

                <div className="space-fine">
                  <span className="text-caption block">Account Status</span>
                  <span className="text-body text-foreground capitalize">{user?.status}</span>
                </div>
              </div>

              {/* Premium verification status grid */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                <div className="card-interactive p-6 transition-all duration-300">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center space-golden-sm">
                      {user?.emailVerified ? (
                        <CheckCircle className="w-5 h-5 text-success" />
                      ) : (
                        <AlertCircle className="w-5 h-5 text-destructive" />
                      )}
                      <span className="text-body font-semibold">Email</span>
                    </div>
                    <span className={user?.emailVerified ?'status-success' :'status-error'}>
                      {user?.emailVerified ?'Verified' :'Pending'}
                    </span>
                  </div>
                </div>

              </div>

              {/* Premium roles display */}
              {user?.roles && user.roles.length > 0 && (
                <div className="space-md">
                  <span className="text-caption block">Account Roles</span>
                  <div className="flex flex-wrap gap-3">
                    {user.roles.map((role, index) => (
                      <span key={index} className="status-neutral">
                        {role}
                      </span>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </section>

          {/* Vibrant Quick Actions Section */}
          <section className="flex flex-col gap-8">
            <div className="space-y-3">
              <h2 className="text-3xl lg:text-4xl font-black tracking-tight">
                <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                  Quick Actions
                </span>
              </h2>
              <p className="text-lg text-muted-foreground">Access key features from your vibrant workspace</p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-10">
              <Link href="/create-project" className="card-interactive p-8 text-center space-golden-sm group">
                <div className="flex justify-center mb-6">
                  <div className="p-5 bg-gradient-to-br from-primary/20 to-primary/10 rounded-2xl group-hover:from-primary/30 group-hover:to-primary/20 transition-all duration-300 shadow-lg group-hover:shadow-xl group-hover:shadow-primary/30">
                    <FolderPlus className="w-10 h-10 text-primary group-hover:scale-110 transition-transform duration-300" />
                  </div>
                </div>
                <div className="space-md">
                  <h3 className="text-subheading text-foreground">Create Project</h3>
                  <p className="text-body text-muted-foreground leading-relaxed">Set up exchange projects with clear deliverables and milestones</p>
                  <div className="btn-primary text-sm mt-4 inline-block">
                    Get Started
                  </div>
                </div>
              </Link>

              <Link href="/projects/search" className="card-interactive p-8 text-center space-golden-sm group">
                <div className="flex justify-center mb-6">
                  <div className="p-5 bg-gradient-to-br from-secondary/20 to-secondary/10 rounded-2xl group-hover:from-secondary/30 group-hover:to-secondary/20 transition-all duration-300 shadow-lg group-hover:shadow-xl group-hover:shadow-secondary/30">
                    <Search className="w-10 h-10 text-secondary group-hover:scale-110 transition-transform duration-300" />
                  </div>
                </div>
                <div className="space-md">
                  <h3 className="text-subheading text-foreground">Browse Projects</h3>
                  <p className="text-body text-muted-foreground leading-relaxed">Browse verified professionals and find exchange partners</p>
                  <div className="btn-secondary text-sm mt-4 inline-block">
                    Explore Now
                  </div>
                </div>
              </Link>

              <SubscriptionDashboard />
            </div>
          </section>
        </div>
      </main>
    </div>
  )
}