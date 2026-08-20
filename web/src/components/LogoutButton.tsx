'use client'

import { logger } from '@/utils/logger';

import React, { useState } from 'react'
import { useAuth } from '@/contexts/AuthContext'

interface LogoutButtonProps {
  className?: string
  showAllDevicesOption?: boolean
  variant?: 'button' | 'link'
  children?: React.ReactNode
}

export default function LogoutButton({ 
  className = '',
  showAllDevicesOption = false,
  variant = 'button',
  children
}: LogoutButtonProps) {
  const { logout, user } = useAuth()
  const [isLoggingOut, setIsLoggingOut] = useState(false)
  const [showDropdown, setShowDropdown] = useState(false)

  const handleLogout = async (logoutFromAllDevices = false) => {
    setIsLoggingOut(true)
    setShowDropdown(false)
    
    try {
      await logout(logoutFromAllDevices)
    } catch (error) {
      logger.error('Logout failed:', error)
    } finally {
      setIsLoggingOut(false)
    }
  }

  if (!user) {
    return null
  }

  const baseClasses = variant === 'button' 
    ? 'px-4 py-2 rounded-full font-medium focus:outline-none focus:ring-2 focus:ring-offset-2'
    : 'focus:outline-none focus:underline'

  const variantClasses = variant === 'button'
    ? 'bg-destructive text-destructive-foreground hover:bg-destructive/90 focus:ring-ring disabled:opacity-50 disabled:cursor-not-allowed'
    : 'text-destructive hover:text-destructive/90'

  if (showAllDevicesOption && !isLoggingOut) {
    return (
      <div className="relative">
        <button
          onClick={() => setShowDropdown(!showDropdown)}
          className={`${baseClasses} ${variantClasses} ${className}`}
          disabled={isLoggingOut}
        >
          {children || 'Sign Out'}
          <svg 
            className="ml-1 h-4 w-4 inline" 
            fill="none" 
            viewBox="0 0 24 24" 
            stroke="currentColor"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
          </svg>
        </button>

        {showDropdown && (
          <div className="absolute right-0 mt-2 w-64 bg-card rounded-md shadow-lg ring-1 ring-border z-50">
            <div className="py-1">
              <button
                onClick={() => handleLogout(false)}
                className="block w-full text-left px-4 py-2 text-sm text-foreground hover:bg-muted"
              >
                Sign out from this device
              </button>
              <button
                onClick={() => handleLogout(true)}
                className="block w-full text-left px-4 py-2 text-sm text-destructive hover:bg-destructive/10"
              >
                Sign out from all devices
              </button>
            </div>
          </div>
        )}

        {/* Backdrop to close dropdown */}
        {showDropdown && (
          <div 
            className="fixed inset-0 z-40" 
            onClick={() => setShowDropdown(false)}
          />
        )}
      </div>
    )
  }

  return (
    <button
      onClick={() => handleLogout(false)}
      className={`${baseClasses} ${variantClasses} ${className}`}
      disabled={isLoggingOut}
    >
      {isLoggingOut ? 'Signing Out...' : (children || 'Sign Out')}
    </button>
  )
}