'use client'

import React, { useState, useEffect } from 'react'
import Link from 'next/link'
import { usePathname } from 'next/navigation'
import {
  Menu,
  X,
  Home,
  Briefcase,
  Search,
  Wallet,
  Settings,
  User,
  LogOut,
  ChevronDown,
  Sparkles
} from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import { ThemeToggle } from './ThemeToggle'
import { Logo } from './Logo'
import { cn } from '@/lib/utils'

interface NavigationItem {
  href: string
  label: string
  icon: React.ComponentType<{ className?: string }>
  badge?: string | number
  onClick?: () => void
}

export function EnhancedNavigation() {
  const { user, isAuthenticated, logout } = useAuth()
  const pathname = usePathname()
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)
  const [isScrolled, setIsScrolled] = useState(false)
  const [activeDropdown, setActiveDropdown] = useState<string | null>(null)
  const dropdownRef = React.useRef<HTMLDivElement>(null)

  // Enhanced scroll detection with performance optimization
  useEffect(() => {
    let ticking = false
    const handleScroll = () => {
      if (!ticking) {
        window.requestAnimationFrame(() => {
          setIsScrolled(window.scrollY > 20)
          ticking = false
        })
        ticking = true
      }
    }

    window.addEventListener('scroll', handleScroll, { passive: true })
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  const navigationItems: NavigationItem[] = [
    {
      href: '/',
      label: 'Dashboard',
      icon: Home,
      badge: pathname === '/' ? '●' : undefined
    },
    {
      href: '/projects',
      label: 'Projects',
      icon: Briefcase,
      onClick: () => setActiveDropdown(null)
    },
    {
      href: '/projects/search',
      label: 'Browse',
      icon: Search,
      onClick: () => setActiveDropdown(null)
    },
    {
      href: '/wallet',
      label: 'Wallet',
      icon: Wallet,
      // Badge removed - wallet balance should be shown on the wallet page itself
      onClick: () => setActiveDropdown(null)
    },
  ]

  const handleLogout = () => {
    setActiveDropdown(null)
    logout()
  }

  // FE-MED-005 FIX: Wrap in useCallback to create stable reference for useEffect dependency
  const handleKeyDown = React.useCallback((e: KeyboardEvent) => {
    if (e.key === 'Escape') {
      setIsMobileMenuOpen(false)
      setActiveDropdown(null)
    }
  }, [])

  // BUG-UI-011 FIX: Close dropdown on outside click
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setActiveDropdown(null)
      }
    }

    if (activeDropdown) {
      document.addEventListener('mousedown', handleClickOutside)
      return () => document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [activeDropdown])

  // FE-MED-005 FIX: handleKeyDown is defined inside component, add to deps to prevent stale closures
  // and ensure cleanup properly removes the exact handler that was added
  useEffect(() => {
    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [handleKeyDown])

  if (!isAuthenticated) {
    return null
  }

  return (
    <>
      {/* Enhanced Animated Header */}
      <header
        className={cn(
          "fixed top-0 left-0 right-0 z-50 transition-all duration-500",
          isScrolled
            ? "bg-card/95 backdrop-blur-xl border-b border-border/30 shadow-xl shadow-primary/10"
            : "bg-card/50 backdrop-blur-md border-b border-border/10"
        )}
      >
        <div className="container-premium">
          <div className="flex items-center justify-between h-20">

            {/* Animated Logo */}
            <Link
              href="/"
              className="group flex items-center space-x-3 transition-all duration-300 hover:scale-105"
            >
              <Logo size="small" showText={true} className="group" />
            </Link>

            {/* Desktop Navigation */}
            <nav className="hidden lg:flex items-center space-golden-md">
              {navigationItems.map((item, index) => {
                const IconComponent = item.icon
                return (
                  <div key={item.href} className="relative group">
                    <Link
                      href={item.href}
                      onClick={item.onClick}
                      className={cn(
                        "flex items-center space-x-2 px-5 py-3 rounded-xl font-medium transition-all duration-300 hover:lift-subtle",
                        pathname === item.href
                          ? "bg-primary text-primary-foreground shadow-lg shadow-primary/20 scale-[1.02]"
                          : "text-muted-foreground hover:text-foreground hover:bg-muted/50"
                      )}
                    >
                      <IconComponent className="w-4 h-4 transition-transform duration-300 group-hover:scale-110" />
                      <span className="tracking-tight">{item.label}</span>

                      {/* Badge */}
                      {item.badge && (
                        <span className="absolute -top-1 -right-1 px-2 py-1 text-xs font-bold bg-gradient-to-r from-primary to-secondary text-primary-foreground rounded-full animate-bounce-in shadow-md">
                          {item.badge}
                        </span>
                      )}

                      {/* Active Indicator */}
                      {pathname === item.href && (
                        <div className="absolute bottom-0 left-1/2 w-8 h-0.5 bg-gradient-to-r from-primary to-secondary rounded-full animate-slide-left"></div>
                      )}
                    </Link>
                  </div>
                )
              })}
            </nav>

            {/* Right Section */}
            <div className="flex items-center space-golden-sm">
              {/* Enhanced Theme Toggle */}
              <div className="animate-slide-right" style={{ animationDelay: '100ms' }}>
                <ThemeToggle />
              </div>

              {/* User Menu - Desktop Only */}
              <div className="hidden lg:block relative" ref={dropdownRef}>
                <button
                  onClick={() => setActiveDropdown(activeDropdown === 'user' ? null : 'user')}
                  className="group flex items-center space-x-3 px-4 py-3 rounded-2xl font-medium transition-all duration-300 hover:lift hover:bg-muted/50"
                  aria-label="User menu"
                  aria-haspopup="menu"
                  aria-expanded={activeDropdown === 'user'}
                >
                  <div className="relative">
                    <div className="w-8 h-8 bg-gradient-to-br from-primary to-secondary rounded-2xl shadow-md group-hover:shadow-primary/30 transition-all duration-300 group-hover:scale-110 flex items-center justify-center">
                      <User className="w-4 h-4 text-primary-foreground" />
                    </div>
                    <div className="absolute -bottom-1 -right-1 w-3 h-3 bg-primary rounded-full animate-ping"></div>
                  </div>
                  <div className="flex flex-col items-start">
                    <span className="text-sm font-semibold tracking-tight">{user?.userName}</span>
                    <span className="text-xs text-muted-foreground animate-fade-in">{user?.email}</span>
                  </div>
                  <ChevronDown
                    className={cn(
                      "w-4 h-4 transition-transform duration-300",
                      activeDropdown === 'user' ? "rotate-180" : ""
                    )}
                  />
                </button>

                {/* Enhanced Dropdown Menu - BUG-011 FIX: Added proper role */}
                {activeDropdown === 'user' && (
                  <div
                    className="absolute top-full right-0 mt-2 w-72 animate-in fade-in zoom-in-95 z-50"
                    role="menu"
                    aria-label="User menu"
                  >
                    <div className="card-elevated border border-border/50 shadow-2xl overflow-hidden">
                      {/* User Profile Section */}
                      <div className="p-4 border-b border-border/30 bg-gradient-to-br from-primary/5 to-secondary/5">
                        <div className="flex items-center space-x-3">
                          <div className="w-10 h-10 bg-gradient-to-br from-primary to-secondary rounded-2xl shadow-md flex items-center justify-center">
                            <User className="w-5 h-5 text-primary-foreground" />
                          </div>
                          <div className="flex-1">
                            <p className="text-sm font-semibold tracking-tight">{user?.userName}</p>
                            <p className="text-xs text-muted-foreground">{user?.email}</p>
                          </div>
                        </div>
                      </div>

                      {/* Navigation Links - BUG-011 FIX: Added menu item roles */}
                      <nav className="p-2 space-y-1">
                        <Link
                          href="/profile/me"
                          className="flex items-center space-x-3 px-3 py-3 text-sm rounded-xl transition-all duration-300 hover:lift hover:bg-muted/50"
                          onClick={() => setActiveDropdown(null)}
                          role="menuitem"
                        >
                          <User className="w-4 h-4" />
                          <span>My Profile</span>
                        </Link>

                        <Link
                          href="/subscription"
                          className="flex items-center space-x-3 px-3 py-3 text-sm rounded-xl transition-all duration-300 hover:lift hover:bg-muted/50"
                          onClick={() => setActiveDropdown(null)}
                          role="menuitem"
                        >
                          <Settings className="w-4 h-4" />
                          <span>Settings</span>
                        </Link>
                      </nav>

                      {/* Logout Button - BUG-011 FIX: Added menu item role */}
                      <div className="p-2 border-t border-border/30">
                        <button
                          onClick={handleLogout}
                          className="w-full flex items-center justify-center space-x-3 px-4 py-3 text-sm font-medium text-destructive bg-destructive/10 rounded-full transition-all duration-300 hover:bg-destructive/20 hover:lift hover:shadow-destructive/20"
                          role="menuitem"
                        >
                          <LogOut className="w-4 h-4" />
                          <span>Logout</span>
                        </button>
                      </div>

                      {/* BUG-045 FIX: Keyboard shortcut help */}
                      <div className="px-4 py-2 border-t border-border/30 bg-muted/30">
                        <p className="text-xs text-muted-foreground text-center">
                          Press <kbd className="px-1.5 py-0.5 bg-muted rounded border border-border text-xs">Esc</kbd> to close
                        </p>
                      </div>
                    </div>
                  </div>
                )}
              </div>

              {/* Mobile Menu Toggle */}
              <button
                onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
                className="lg:hidden flex items-center justify-center w-10 h-10 rounded-full bg-card border border-border transition-all duration-300 hover:lift hover:bg-muted/50"
                aria-label="Toggle mobile menu"
                aria-expanded={isMobileMenuOpen}
              >
                <div className="relative space-y-1">
                  <div className={cn("w-6 h-0.5 bg-foreground rounded-full transition-all duration-300", isMobileMenuOpen ? "w-8" : "")}></div>
                  <div className={cn("w-6 h-0.5 bg-foreground rounded-full transition-all duration-300", isMobileMenuOpen ? "w-4 -rotate-45" : "rotate-0")}></div>
                  <div className={cn("w-6 h-0.5 bg-foreground rounded-full transition-all duration-300", isMobileMenuOpen ? "rotate-45" : "")}></div>
                </div>
              </button>
            </div>
          </div>
        </div>

        {/* Mobile Menu */}
        {isMobileMenuOpen && (
          <>
            {/* Backdrop */}
            <div
              className="fixed inset-0 bg-overlay/70 backdrop-blur-md z-40 animate-fade-in lg:hidden"
              onClick={() => setIsMobileMenuOpen(false)}
            />

            {/* Mobile Navigation Panel */}
            <div className="fixed top-20 left-0 right-0 bottom-0 bg-card/95 backdrop-blur-xl z-50 lg:hidden animate-slide-up-enhanced">
              <div className="container-premium h-full overflow-y-auto py-8">
                <div className="space-golden-lg">
                  {/* User Info */}
                  <div className="flex items-center space-x-4 p-4 bg-gradient-to-br from-primary/10 to-secondary/10 rounded-2xl mb-6 animate-fade-in">
                    <div className="w-12 h-12 bg-gradient-to-br from-primary to-secondary rounded-2xl shadow-md flex items-center justify-center">
                      <User className="w-6 h-6 text-primary-foreground" />
                    </div>
                    <div>
                      <p className="text-base font-semibold tracking-tight">{user?.userName}</p>
                      <p className="text-sm text-muted-foreground">{user?.email}</p>
                    </div>
                  </div>

                  {/* Mobile Navigation Items */}
                  <nav className="space-golden-md stagger-children">
                    {navigationItems.map((item, index) => {
                      const IconComponent = item.icon
                      return (
                        <Link
                          key={item.href}
                          href={item.href}
                          onClick={item.onClick}
                          className={cn(
                            "flex items-center space-x-4 p-4 rounded-2xl font-medium transition-all duration-300 hover:lift",
                            pathname === item.href
                              ? "bg-primary text-primary-foreground shadow-lg scale-[1.02]"
                              : "bg-card border border-border hover:bg-muted/50"
                          )}
                          style={{ animationDelay: `${index * 100}ms` }}
                        >
                          <IconComponent className="w-5 h-5" />
                          <span className="tracking-tight">{item.label}</span>

                          {item.badge && (
                            <span className="ml-auto px-3 py-1 text-xs font-bold bg-gradient-to-r from-primary to-secondary text-primary-foreground rounded-full animate-bounce-in">
                              {item.badge}
                            </span>
                          )}
                        </Link>
                      )
                    })}
                  </nav>
                </div>
              </div>
            </div>
          </>
        )}
      </header>

      {/* Spacer for fixed header */}
      <div className="h-20"></div>
    </>
  )
}