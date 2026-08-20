'use client'

import { useState, useEffect, useRef, useCallback } from 'react'
import Link from 'next/link'
import { Menu, X } from 'lucide-react'

interface MobileNavProps {
  items: Array<{ href: string; label: string; isPrimary?: boolean }>
}

export function MobileNav({ items }: MobileNavProps) {
  const [isOpen, setIsOpen] = useState(false)
  const menuRef = useRef<HTMLElement>(null)
  const closeButtonRef = useRef<HTMLButtonElement>(null)
  const firstLinkRef = useRef<HTMLAnchorElement>(null)
  const lastLinkRef = useRef<HTMLAnchorElement>(null)

  const closeMenu = () => setIsOpen(false)

  // BUG-009 FIX: Implement focus trap for keyboard accessibility
  const handleKeyDown = useCallback((e: KeyboardEvent) => {
    if (!isOpen) return

    if (e.key === 'Escape') {
      closeMenu()
      return
    }

    // Handle Tab key for focus trap
    if (e.key === 'Tab') {
      const focusableElements = menuRef.current?.querySelectorAll(
        'button, a, input, select, textarea, [tabindex]:not([tabindex="-1"])'
      )

      if (!focusableElements || focusableElements.length === 0) return

      const firstElement = focusableElements[0] as HTMLElement
      const lastElement = focusableElements[focusableElements.length - 1] as HTMLElement

      if (e.shiftKey) {
        // Shift + Tab: if on first element, move to last
        if (document.activeElement === firstElement) {
          e.preventDefault()
          lastElement.focus()
        }
      } else {
        // Tab: if on last element, move to first
        if (document.activeElement === lastElement) {
          e.preventDefault()
          firstElement.focus()
        }
      }
    }
  }, [isOpen])

  useEffect(() => {
    if (isOpen) {
      document.addEventListener('keydown', handleKeyDown)
      // Focus the close button when menu opens
      setTimeout(() => closeButtonRef.current?.focus(), 50)
      // Prevent body scroll
      document.body.style.overflow = 'hidden'
    } else {
      document.body.style.overflow = 'unset'
    }

    return () => {
      document.removeEventListener('keydown', handleKeyDown)
      document.body.style.overflow = 'unset'
    }
  }, [isOpen, handleKeyDown])

  return (
    <div className="md:hidden">
      {/* Hamburger Button */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="btn-ghost p-2"
        aria-label={isOpen ? 'Close navigation menu' : 'Open navigation menu'}
        aria-expanded={isOpen}
        aria-controls="mobile-navigation"
      >
        {isOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
      </button>

      {/* Mobile Menu Overlay */}
      {isOpen && (
        <>
          {/* Backdrop - BUG-053 FIX: Use standard Tailwind animation */}
          {/* BUG-012 FIX: Increased backdrop opacity for better contrast on mobile */}
          <div
            className="fixed inset-0 bg-overlay/80 backdrop-blur-md z-40 animate-in fade-in duration-200"
            onClick={closeMenu}
            aria-hidden="true"
          />

          {/* Menu Panel - BUG-053 FIX: Use standard Tailwind animation */}
          <nav
            ref={menuRef}
            id="mobile-navigation"
            className="fixed top-0 right-0 h-full w-64 bg-card border-l border-border shadow-2xl z-50 animate-in slide-in-from-right duration-300"
            role="navigation"
            aria-label="Mobile navigation"
          >
            {/* Close Button */}
            <div className="flex justify-end p-4 border-b border-border">
              <button
                ref={closeButtonRef}
                onClick={closeMenu}
                className="btn-ghost p-2 focus:outline-none focus:ring-2 focus:ring-ring rounded-full"
                aria-label="Close menu"
              >
                <X className="w-6 h-6" />
              </button>
            </div>

            {/* Navigation Links */}
            <div className="flex flex-col p-4 space-y-2">
              {items.map((item, index) => (
                <Link
                  key={index}
                  ref={index === 0 ? firstLinkRef : index === items.length - 1 ? lastLinkRef : undefined}
                  href={item.href}
                  onClick={closeMenu}
                  className={item.isPrimary ? 'btn-primary w-full justify-center' : 'btn-ghost w-full justify-start'}
                >
                  {item.label}
                </Link>
              ))}
            </div>
          </nav>
        </>
      )}
    </div>
  )
}
