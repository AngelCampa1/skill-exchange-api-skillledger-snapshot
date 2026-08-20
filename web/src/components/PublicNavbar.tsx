'use client'

import React, { useState, useEffect } from 'react'
import Link from 'next/link'
import { usePathname } from 'next/navigation'
import {
  Menu,
  X,
  ChevronDown,
  BookOpen,
  Layers,
  Factory,
  Scale,
  BookOpenText,
  Calculator,
  FileText,
  MapPin,
  Lightbulb,
  Sparkles,
  DollarSign,
} from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import { Logo } from './Logo'
import { cn } from '@/lib/utils'

interface NavLink {
  href: string
  label: string
  icon?: React.ComponentType<{ className?: string }>
}

const resourcesDropdown: NavLink[] = [
  { href: '/resources', label: 'Articles', icon: BookOpen },
  { href: '/how-to', label: 'How-To Guides', icon: Lightbulb },
  { href: '/resources/templates', label: 'Templates', icon: FileText },
  { href: '/tools/barter-valuation-calculator', label: 'Credit Calculator', icon: Calculator },
  { href: '/glossary', label: 'Glossary', icon: BookOpenText },
]

const mainNavLinks: NavLink[] = [
  { href: '/skill-match', label: 'Skill Match', icon: Sparkles },
  { href: '/categories', label: 'Categories', icon: Layers },
  { href: '/industries', label: 'Industries', icon: Factory },
  { href: '/compare', label: 'Compare', icon: Scale },
  { href: '/pricing', label: 'Pricing', icon: DollarSign },
  { href: '/skill-exchange', label: 'Cities', icon: MapPin },
]

export function PublicNavbar() {
  const { isAuthenticated, isLoading } = useAuth()
  const pathname = usePathname()
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)
  const [isScrolled, setIsScrolled] = useState(false)
  const [isResourcesOpen, setIsResourcesOpen] = useState(false)
  const dropdownRef = React.useRef<HTMLDivElement>(null)

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

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsResourcesOpen(false)
      }
    }
    if (isResourcesOpen) {
      document.addEventListener('mousedown', handleClickOutside)
      return () => document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isResourcesOpen])

  const handleKeyDown = React.useCallback((e: KeyboardEvent) => {
    if (e.key === 'Escape') {
      setIsMobileMenuOpen(false)
      setIsResourcesOpen(false)
    }
  }, [])

  useEffect(() => {
    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [handleKeyDown])

  // Close mobile menu on route change
  useEffect(() => {
    setIsMobileMenuOpen(false)
    setIsResourcesOpen(false)
  }, [pathname])

  // E2E-009 FIX: Return null while auth is loading to prevent flash of unauthenticated navbar
  if (isLoading || isAuthenticated) {
    return null
  }

  return (
    <>
      <header
        className={cn(
          'fixed top-0 left-0 right-0 z-50 transition-all duration-500',
          isScrolled
            ? 'bg-card/95 backdrop-blur-xl border-b border-border/30 shadow-xl shadow-primary/5'
            : 'bg-card/50 backdrop-blur-md border-b border-border/10'
        )}
      >
        <div className="container-premium">
          <div className="flex items-center justify-between h-16">
            {/* Logo */}
            <Link href="/" className="group flex items-center space-x-3 transition-all duration-300 hover:scale-105">
              <Logo size="small" showText={true} className="group" />
            </Link>

            {/* Desktop Navigation */}
            <nav className="hidden lg:flex items-center gap-1" aria-label="Main navigation">
              {/* Resources Dropdown */}
              <div className="relative" ref={dropdownRef}>
                <button
                  onClick={() => setIsResourcesOpen(!isResourcesOpen)}
                  aria-haspopup="true"
                  aria-expanded={isResourcesOpen}
                  className={cn(
                    'flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-medium transition-all duration-200',
                    isResourcesOpen
                      ? 'text-foreground bg-muted/50'
                      : 'text-muted-foreground hover:text-foreground hover:bg-muted/30'
                  )}
                >
                  Resources
                  <ChevronDown className={cn('w-3.5 h-3.5 transition-transform duration-200', isResourcesOpen && 'rotate-180')} />
                </button>

                {isResourcesOpen && (
                  <div className="absolute top-full left-0 mt-2 w-56 bg-card border border-border/50 rounded-xl shadow-xl overflow-hidden animate-in fade-in zoom-in-95 z-50" role="menu" aria-label="Resources">
                    <div className="p-2">
                      {resourcesDropdown.map((item) => {
                        const Icon = item.icon
                        return (
                          <Link
                            key={item.href}
                            href={item.href}
                            role="menuitem"
                            className="flex items-center gap-3 px-3 py-2.5 text-sm rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted/50 transition-colors"
                          >
                            {Icon && <Icon className="w-4 h-4" />}
                            {item.label}
                          </Link>
                        )
                      })}
                    </div>
                  </div>
                )}
              </div>

              {/* Main Nav Links */}
              {mainNavLinks.map((item) => (
                <Link
                  key={item.href}
                  href={item.href}
                  className={cn(
                    'px-3 py-2 rounded-lg text-sm font-medium transition-all duration-200',
                    pathname.startsWith(item.href)
                      ? 'text-primary bg-primary/10'
                      : 'text-muted-foreground hover:text-foreground hover:bg-muted/30'
                  )}
                >
                  {item.label}
                </Link>
              ))}
            </nav>

            {/* Right: CTAs + Mobile Toggle */}
            <div className="flex items-center gap-3">
              <Link
                href="/login"
                className="hidden sm:inline-flex px-4 py-2 text-sm font-medium text-muted-foreground hover:text-foreground transition-colors"
              >
                Sign In
              </Link>
              <Link
                href="/register"
                className="hidden sm:inline-flex btn-primary text-sm px-4 py-2 shadow-sm font-semibold"
              >
                Get Started
              </Link>

              {/* Mobile Toggle */}
              <button
                onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
                className="lg:hidden relative z-50 flex items-center justify-center w-10 h-10 rounded-xl bg-card border border-border transition-all duration-300 hover:bg-muted/50"
                aria-label="Toggle mobile menu"
                aria-expanded={isMobileMenuOpen}
              >
                {isMobileMenuOpen ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
              </button>
            </div>
          </div>
        </div>

        {/* Mobile Menu */}
        {isMobileMenuOpen && (
          <>
            <div
              className="fixed inset-0 bg-overlay/70 backdrop-blur-md z-40 lg:hidden"
              onClick={() => setIsMobileMenuOpen(false)}
              role="button"
              aria-label="Close menu"
            />
            <div className="fixed top-16 left-0 right-0 bottom-0 bg-card z-50 lg:hidden overflow-y-auto border-t border-border">
              <div className="container-premium py-6 space-y-6">
                {/* Auth CTAs */}
                <div className="space-y-2 sm:hidden">
                  <Link href="/register" className="block btn-primary text-center text-sm py-2.5">Get Started</Link>
                  <Link href="/login" className="block text-center text-sm text-muted-foreground hover:text-foreground transition-colors py-1">Sign In</Link>
                </div>

                {/* Resources Section */}
                <div>
                  <h3 className="text-xs font-bold uppercase tracking-wider text-muted-foreground mb-3 px-3">Resources</h3>
                  <nav className="space-y-1">
                    {resourcesDropdown.map((item) => {
                      const Icon = item.icon
                      return (
                        <Link
                          key={item.href}
                          href={item.href}
                          className={cn(
                            'flex items-center gap-3 px-3 py-3 rounded-xl text-sm font-medium transition-colors',
                            pathname === item.href
                              ? 'text-primary bg-primary/10'
                              : 'text-muted-foreground hover:text-foreground hover:bg-muted/30'
                          )}
                        >
                          {Icon && <Icon className="w-4 h-4" />}
                          {item.label}
                        </Link>
                      )
                    })}
                  </nav>
                </div>

                {/* Explore Section */}
                <div>
                  <h3 className="text-xs font-bold uppercase tracking-wider text-muted-foreground mb-3 px-3">Explore</h3>
                  <nav className="space-y-1">
                    {mainNavLinks.map((item) => {
                      const Icon = item.icon
                      return (
                        <Link
                          key={item.href}
                          href={item.href}
                          className={cn(
                            'flex items-center gap-3 px-3 py-3 rounded-xl text-sm font-medium transition-colors',
                            pathname.startsWith(item.href)
                              ? 'text-primary bg-primary/10'
                              : 'text-muted-foreground hover:text-foreground hover:bg-muted/30'
                          )}
                        >
                          {Icon && <Icon className="w-4 h-4" />}
                          {item.label}
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
      <div className="h-16" />
    </>
  )
}
