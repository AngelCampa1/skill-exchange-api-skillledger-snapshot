'use client'

import Link from'next/link'
import { FolderPlus, Search, Wallet, CheckCircle, AlertCircle } from'lucide-react'
import { useAuth } from'@/contexts/AuthContext'
import LogoutButton from'@/components/LogoutButton'
import { ThemeToggle } from'@/components/ThemeToggle'
import { MobileNav } from'@/components/MobileNav'
import { Logo } from'@/components/Logo'

/**
 * Authenticated dashboard view — only renders when user is authenticated.
 * Loaded client-side only (ssr: false) so the landing page remains statically
 * renderable for SEO without any auth dependencies.
 */
export default function AuthenticatedHome() {
  const { user, isAuthenticated, isLoading } = useAuth()

  // While auth state is loading or user is not authenticated, render nothing.
  // The landing page (rendered by the server component) is already visible.
  if (isLoading || !isAuthenticated) {
    return null
  }

  return (
    <div
      className="fixed inset-0 z-50 overflow-auto bg-gradient-to-br from-background via-primary/5 to-secondary/10"
      style={{ top: 0, left: 0 }}
    >
      {/* Modern Navigation with Gradient */}
      <nav className="bg-card/90 backdrop-blur-xl border-b border-border/50 sticky top-0 z-50 shadow-lg shadow-primary/5">
        <div className="container-premium">
          <div className="flex justify-between items-center h-24">
            <Link
              href="/"
              className="text-heading text-foreground hover:text-primary transition-colors duration-300 font-bold tracking-tight"
            >
              <Logo size="medium" showText={true} />
            </Link>

            <div className="hidden md:flex items-center space-golden-md">
              <Link href="/" className="btn-ghost">Dashboard</Link>
            </div>

            <div className="flex items-center space-golden-sm">
              {/* E2E-011 FIX: Add mobile navigation for authenticated homepage */}
              <MobileNav
                items={[
                  { href:'/dashboard', label:'Dashboard' },
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
      <main className="container-premium py-16 lg:py-24 relative">
        {/* Decorative gradient orbs */}
        <div className="absolute top-20 right-10 w-72 h-72 bg-gradient-to-br from-primary/20 to-secondary/20 rounded-full blur-3xl opacity-60 animate-float" aria-hidden="true"></div>
        <div className="absolute bottom-40 left-10 w-96 h-96 bg-gradient-to-tr from-secondary/15 to-primary/15 rounded-full blur-3xl opacity-50" aria-hidden="true"></div>

        <div className="flex flex-col gap-12 lg:gap-16 relative z-10">
          {/* Modern Dashboard Header with Gradient */}
          <header className="animate-fade-in">
            <div className="flex flex-col lg:flex-row lg:items-end lg:justify-between gap-8 bg-gradient-to-r from-primary/10 via-transparent to-secondary/10 p-10 lg:p-12 rounded-3xl border border-primary/20 shadow-xl shadow-primary/5">
              <div className="space-y-4">
                {/* SEO FIX: Changed from h1 to h2 to maintain single h1 per page */}
                <h2 className="text-5xl sm:text-6xl lg:text-7xl font-black tracking-tight">
                  <span className="bg-gradient-to-r from-primary via-primary to-secondary bg-clip-text text-transparent">
                    Dashboard
                  </span>
                </h2>
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
                <div className="card-feature p-6 transition-all duration-300">
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
              <div className="card-feature p-8 text-center space-golden-sm group">
                <div className="flex justify-center mb-6">
                  <div className="p-5 bg-gradient-to-br from-primary/20 to-primary/10 rounded-2xl group-hover:from-primary/30 group-hover:to-primary/20 transition-all duration-300 shadow-lg group-hover:shadow-xl group-hover:shadow-primary/30">
                    <FolderPlus className="w-10 h-10 text-primary group-hover:scale-110 transition-transform duration-300" />
                  </div>
                </div>
                <div className="space-md">
                  <h3 className="text-subheading text-foreground">Create Project</h3>
                  <p className="text-body text-muted-foreground leading-relaxed">Set up exchange projects with clear deliverables and milestones</p>
                </div>
                <button className="btn-ghost text-xs opacity-60">
                  Available Soon
                </button>
              </div>

              <div className="card-feature p-8 text-center space-golden-sm group">
                <div className="flex justify-center mb-6">
                  <div className="p-5 bg-gradient-to-br from-secondary/20 to-secondary/10 rounded-2xl group-hover:from-secondary/30 group-hover:to-secondary/20 transition-all duration-300 shadow-lg group-hover:shadow-xl group-hover:shadow-secondary/30">
                    <Search className="w-10 h-10 text-secondary group-hover:scale-110 transition-transform duration-300" />
                  </div>
                </div>
                <div className="space-md">
                  <h3 className="text-subheading text-foreground">Browse Projects</h3>
                  <p className="text-body text-muted-foreground leading-relaxed">Browse verified professionals and find exchange partners</p>
                </div>
                <button className="btn-ghost text-xs opacity-60">
                  Available Soon
                </button>
              </div>

              <div className="card-feature p-8 text-center space-golden-sm group">
                <div className="flex justify-center mb-6">
                  <div className="p-5 bg-gradient-to-br from-primary/20 via-secondary/15 to-primary/10 rounded-2xl group-hover:from-primary/30 group-hover:via-secondary/25 group-hover:to-primary/20 transition-all duration-300 shadow-lg group-hover:shadow-xl group-hover:shadow-primary/30">
                    <Wallet className="w-10 h-10 text-primary group-hover:scale-110 transition-transform duration-300" />
                  </div>
                </div>
                <div className="space-md">
                  <h3 className="text-subheading text-foreground">Premium Wallet</h3>
                  <p className="text-body text-muted-foreground leading-relaxed">Track your credit balance, earnings, and exchange history</p>
                </div>
                <button className="btn-ghost text-xs opacity-60">
                  Available Soon
                </button>
              </div>
            </div>
          </section>
        </div>
      </main>
    </div>
  )
}
