'use client'

import { logger } from '@/utils/logger';

import React, { useState } from 'react'
import Image from 'next/image'
import { format, formatDistanceToNow } from 'date-fns'
import { Shield, CheckCircle, Clock, ExternalLink, Copy, Check } from 'lucide-react'
import { Badge } from '../ui/badge'
import { Button } from '../ui/button'
import { UserBadge, BadgeCategory, VerificationLevel, BadgeDisplayProps } from '../../types/badge'
import { AUTH_CONFIG } from '../../constants/auth';

const categoryColors = {
  [BadgeCategory.Performance]: 'bg-primary/10 text-primary border-primary/20',
  [BadgeCategory.Volume]: 'bg-success/10 text-success border-success/20',
  [BadgeCategory.Expertise]: 'bg-info/10 text-info border-info/20',
  [BadgeCategory.Trust]: 'bg-warning/10 text-warning border-warning/20',
  [BadgeCategory.Community]: 'bg-accent/10 text-accent-foreground border-accent/20',
  [BadgeCategory.Achievement]: 'bg-warning/10 text-warning border-warning/20'
}

const categoryIcons = {
  [BadgeCategory.Performance]: '⭐',
  [BadgeCategory.Volume]: '🎯',
  [BadgeCategory.Expertise]: '🏆',
  [BadgeCategory.Trust]: '🛡️',
  [BadgeCategory.Community]: '👥',
  [BadgeCategory.Achievement]: '🏅'
}

const verificationIcons = {
  [VerificationLevel.Automatic]: <CheckCircle className="h-4 w-4 text-success" />,
  [VerificationLevel.Manual]: <Shield className="h-4 w-4 text-primary" />,
  [VerificationLevel.External]: <ExternalLink className="h-4 w-4 text-info" />
}

export default function BadgeDisplay({ 
  badge, 
  size = 'medium', 
  showDetails = false, 
  showVerificationCode = false,
  onClick 
}: BadgeDisplayProps) {
  const [showVerificationModal, setShowVerificationModal] = useState(false)
  const [verificationCode, setVerificationCode] = useState<string | null>(null)
  const [isGeneratingCode, setIsGeneratingCode] = useState(false)
  const [isCodeCopied, setIsCodeCopied] = useState(false)

  const sizeClasses = {
    small: 'w-12 h-12',
    medium: 'w-16 h-16',
    large: 'w-24 h-24'
  }

  const textSizeClasses = {
    small: 'text-xs',
    medium: 'text-sm',
    large: 'text-base'
  }

  const isExpired = badge.expiresAt && new Date(badge.expiresAt) < new Date()
  const isExpiringSoon = badge.expiresAt && new Date(badge.expiresAt) < new Date(Date.now() + 30 * 24 * 60 * 60 * 1000) // 30 days

  const generateVerificationCode = async () => {
    setIsGeneratingCode(true)
    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch(`/api/badge/verify/${badge.id}/generate-code`, {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS
      })
      
      if (response.ok) {
        const data = await response.json()
        setVerificationCode(data.verificationCode)
      }
    } catch (error) {
      logger.error('Failed to generate verification code:', error)
    } finally {
      setIsGeneratingCode(false)
    }
  }

  const copyVerificationCode = async () => {
    if (verificationCode) {
      await navigator.clipboard.writeText(verificationCode)
      setIsCodeCopied(true)
      setTimeout(() => setIsCodeCopied(false), 2000)
    }
  }

  const badgeIcon = badge.iconUrl || `/badges/${badge.badgeType.toLowerCase()}.svg`

  return (
    <div 
      className={`
        relative group transition-all duration-200 
        ${onClick ? 'cursor-pointer hover:scale-105' : ''}
        ${isExpired ? 'opacity-60 grayscale' : ''}
      `}
      onClick={onClick}
    >
      {/* Badge Container */}
      <div className={`
        relative ${sizeClasses[size]} mx-auto mb-2
        ${!badge.isActive ? 'filter grayscale' : ''}
      `}>
        {/* Badge Image */}
        <div className="relative w-full h-full">
          <Image
            src={badgeIcon}
            alt={badge.badgeName}
            width={96}
            height={96}
            className="w-full h-full object-contain rounded-full shadow-lg"
            onError={(e) => {
              // Fallback to category emoji if image fails to load
              const target = e.target as HTMLImageElement
              target.style.display = 'none'
            }}
          />
          
          {/* Fallback emoji display */}
          <div className="absolute inset-0 flex items-center justify-center text-4xl bg-card rounded-full shadow-lg">
            {categoryIcons[badge.category]}
          </div>
        </div>

        {/* Verification Level Indicator */}
        <div className="absolute -top-1 -right-1 bg-card rounded-full p-1 shadow-md">
          {verificationIcons[badge.verificationLevel]}
        </div>

        {/* Expiration Warning */}
        {isExpiringSoon && !isExpired && (
          <div className="absolute -bottom-1 -right-1 bg-warning rounded-full p-1">
            <Clock className="h-3 w-3 text-warning-foreground" />
          </div>
        )}
      </div>

      {/* Badge Info */}
      <div className="text-center">
        <h4 className={`font-semibold ${textSizeClasses[size]} mb-1`}>
          {badge.badgeName}
        </h4>
        
        <Badge 
          variant="outline" 
          className={`${categoryColors[badge.category]} text-xs mb-2`}
        >
          {badge.category}
        </Badge>

        {showDetails && (
          <div className="space-y-2">
            <p className="text-xs text-muted-foreground leading-relaxed">
              {badge.badgeDescription}
            </p>
            
            <div className="text-xs text-muted-foreground space-y-1">
              <div>Earned {formatDistanceToNow(new Date(badge.earnedAt), { addSuffix: true })}</div>
              
              {badge.expiresAt && (
                <div className={isExpired ? 'text-destructive font-medium' : isExpiringSoon ? 'text-warning font-medium' : ''}>
                  {isExpired ? 'Expired' : 'Expires'} {format(new Date(badge.expiresAt), 'MMM d, yyyy')}
                </div>
              )}

              {badge.verifiedAt && badge.verifiedBy && (
                <div className="flex items-center justify-center gap-1">
                  <CheckCircle className="h-3 w-3 text-success" />
                  <span>Verified</span>
                </div>
              )}
            </div>

            {showVerificationCode && badge.isActive && !isExpired && (
              <div className="mt-3">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={(e) => {
                    e.stopPropagation()
                    setShowVerificationModal(true)
                    if (!verificationCode) {
                      generateVerificationCode()
                    }
                  }}
                  className="text-xs"
                >
                  Generate Verification Code
                </Button>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Verification Code Modal */}
      {showVerificationModal && (
        <div className="fixed inset-0 bg-background/80 flex items-center justify-center z-50" onClick={() => setShowVerificationModal(false)}>
          <div className="bg-card border border-border rounded-lg p-6 max-w-md mx-4 shadow-lg" onClick={(e) => e.stopPropagation()}>
            <h3 className="text-lg font-semibold mb-4">Badge Verification</h3>
            
            <div className="mb-4">
              <p className="text-sm text-muted-foreground mb-2">
                Share this code to verify your {badge.badgeName} badge:
              </p>
              
              {isGeneratingCode ? (
                <div className="flex items-center justify-center py-4">
                  <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-primary"></div>
                </div>
              ) : verificationCode ? (
                <div className="bg-muted/50 rounded-lg p-3 border">
                  <div className="flex items-center justify-between">
                    <code className="font-mono text-sm break-all">{verificationCode}</code>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={copyVerificationCode}
                      className="ml-2 flex-shrink-0"
                    >
                      {isCodeCopied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                    </Button>
                  </div>
                </div>
              ) : (
                <div className="text-destructive text-sm">Failed to generate verification code</div>
              )}
            </div>

            <div className="flex gap-2">
              <Button
                variant="outline"
                onClick={() => setShowVerificationModal(false)}
                className="flex-1"
              >
                Close
              </Button>
              {verificationCode && (
                <Button
                  onClick={copyVerificationCode}
                  className="flex-1"
                >
                  {isCodeCopied ? 'Copied!' : 'Copy Code'}
                </Button>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}