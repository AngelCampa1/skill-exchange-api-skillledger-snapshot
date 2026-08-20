'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect } from 'react'
import Image from 'next/image'
import { ChevronRight, Target, TrendingUp, Clock, CheckCircle, Lock } from 'lucide-react'
import { Badge } from '../ui/badge'
import { Button } from '../ui/button'
import { Progress } from '../ui/progress'
import { BadgeProgress as BadgeProgressType, BadgeCategory, BadgeProgressProps } from '../../types/badge'
import { AUTH_CONFIG } from '../../constants/auth';

const categoryColors = {
  [BadgeCategory.Performance]: 'bg-primary/10 border-primary/20 text-primary',
  [BadgeCategory.Volume]: 'bg-success/10 border-success/20 text-success',
  [BadgeCategory.Expertise]: 'bg-info/10 border-info/20 text-info',
  [BadgeCategory.Trust]: 'bg-warning/10 border-warning/20 text-warning',
  [BadgeCategory.Community]: 'bg-accent/10 border-accent/20 text-accent-foreground',
  [BadgeCategory.Achievement]: 'bg-warning/10 border-warning/20 text-warning'
}

const categoryIcons = {
  [BadgeCategory.Performance]: '⭐',
  [BadgeCategory.Volume]: '🎯',
  [BadgeCategory.Expertise]: '🏆',
  [BadgeCategory.Trust]: '🛡️',
  [BadgeCategory.Community]: '👥',
  [BadgeCategory.Achievement]: '🏅'
}

interface BadgeProgressCardProps {
  progress: BadgeProgressType
  onStartVerification?: (badgeType: string) => void
}

function BadgeProgressCard({ progress, onStartVerification }: BadgeProgressCardProps) {
  const progressPercentage = Math.min((progress.currentProgress / progress.maxProgress) * 100, 100)
  const isEligible = progress.isEligible
  const isComplete = progress.currentProgress >= progress.maxProgress

  const badgeIcon = progress.iconUrl || `/badges/${progress.badgeType.toLowerCase()}.svg`

  return (
    <div className={`
      bg-card rounded-lg border-2 transition-all duration-200 hover:shadow-md
      ${isEligible ? 'border-success/20 bg-success/5' : 'border-border'}
      ${!isEligible && progressPercentage < 10 ? 'opacity-75' : ''}
    `}>
      {/* Header */}
      <div className="p-4 border-b border-border">
        <div className="flex items-start gap-3">
          {/* Badge Icon */}
          <div className="relative w-12 h-12 flex-shrink-0">
            <Image
              src={badgeIcon}
              alt={progress.badgeName}
              width={48}
              height={48}
              className="w-full h-full object-contain rounded-full"
              onError={(e) => {
                const target = e.target as HTMLImageElement
                target.style.display = 'none'
              }}
            />
            {/* Fallback emoji */}
            <div className="absolute inset-0 flex items-center justify-center text-2xl bg-card rounded-full">
              {categoryIcons[progress.category]}
            </div>

            {/* Status Indicators */}
            {isComplete && (
              <div className="absolute -top-1 -right-1 bg-success rounded-full p-1">
                <CheckCircle className="h-3 w-3 text-success-foreground" />
              </div>
            )}
            {!isEligible && progressPercentage < 10 && (
              <div className="absolute -top-1 -right-1 bg-muted rounded-full p-1">
                <Lock className="h-3 w-3 text-muted-foreground" />
              </div>
            )}
          </div>

          {/* Badge Info */}
          <div className="flex-1 min-w-0">
            <h3 className="font-semibold text-foreground truncate">{progress.badgeName}</h3>
            <p className="text-sm text-muted-foreground mt-1 line-clamp-2">{progress.description}</p>
            
            <Badge 
              variant="outline" 
              className={`${categoryColors[progress.category]} text-xs mt-2 border`}
            >
              {progress.category}
            </Badge>
          </div>
        </div>
      </div>

      {/* Progress Section */}
      <div className="p-4 space-y-3">
        {/* Progress Bar */}
        <div className="space-y-2">
          <div className="flex items-center justify-between text-sm">
            <span className="text-muted-foreground">Progress</span>
            <span className={`font-semibold ${isComplete ? 'text-success' : 'text-foreground'}`}>
              {progress.currentProgress}/{progress.maxProgress}
            </span>
          </div>

          <Progress value={progressPercentage} className="h-2" />

          <div className="flex items-center justify-between text-xs text-muted-foreground">
            <span>{Math.round(progressPercentage)}% complete</span>
            {isComplete && <span className="text-success font-medium">Ready to claim!</span>}
          </div>
        </div>

        {/* Next Milestone */}
        {progress.nextMilestone && !isComplete && (
          <div className="bg-primary/10 rounded-lg p-3 border border-primary/20">
            <div className="flex items-center gap-2 text-sm">
              <Target className="h-4 w-4 text-primary" />
              <span className="text-primary font-medium">Next milestone:</span>
            </div>
            <p className="text-primary text-sm mt-1">{progress.nextMilestone}</p>
          </div>
        )}

        {/* Requirements */}
        {progress.requirements.length > 0 && (
          <div className="space-y-2">
            <h4 className="text-sm font-medium text-foreground flex items-center gap-2">
              <TrendingUp className="h-4 w-4" />
              Requirements
            </h4>
            <ul className="space-y-1">
              {progress.requirements.slice(0, 3).map((requirement, index) => (
                <li key={index} className="text-xs text-muted-foreground flex items-start gap-2">
                  <span className="text-muted-foreground">•</span>
                  <span>{requirement}</span>
                </li>
              ))}
              {progress.requirements.length > 3 && (
                <li className="text-xs text-muted-foreground italic">
                  +{progress.requirements.length - 3} more requirements
                </li>
              )}
            </ul>
          </div>
        )}

        {/* Action Button */}
        {isEligible && isComplete && onStartVerification && (
          <Button
            onClick={() => onStartVerification(progress.badgeType)}
            className="w-full mt-4"
            size="sm"
          >
            <CheckCircle className="h-4 w-4 mr-2" />
            Request Verification
          </Button>
        )}
        
        {!isEligible && progressPercentage < 10 && (
          <div className="text-center py-2">
            <span className="text-xs text-muted-foreground flex items-center justify-center gap-1">
              <Lock className="h-3 w-3" />
              Complete prerequisites to unlock
            </span>
          </div>
        )}
      </div>
    </div>
  )
}

export default function BadgeProgress({ progress, userId }: BadgeProgressProps) {
  const [progressData, setProgressData] = useState<BadgeProgressType[]>(progress)
  const [isLoading, setIsLoading] = useState(false)
  const [selectedCategory, setSelectedCategory] = useState<BadgeCategory | 'all'>('all')
  const [sortBy, setSortBy] = useState<'progress' | 'category' | 'name'>('progress')

  // Refresh progress data
  const refreshProgress = async () => {
    setIsLoading(true)
    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch(`/api/badge/user/${userId}/progress`, {
        credentials: AUTH_CONFIG.CREDENTIALS
      })
      
      if (response.ok) {
        const data = await response.json()
        setProgressData(data)
      }
    } catch (error) {
      logger.error('Failed to refresh badge progress:', error)
    } finally {
      setIsLoading(false)
    }
  }

  // Handle verification request
  const handleStartVerification = async (badgeType: string) => {
    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch('/api/badge/verification/request', {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          'Content-Type': 'application/json',
          },
        body: JSON.stringify({
          badgeType,
          evidence: {}
        })
      })
      
      if (response.ok) {
        // Refresh progress after submission
        await refreshProgress()
        alert('Verification request submitted successfully!')
      } else {
        const error = await response.text()
        alert(`Failed to submit verification request: ${error}`)
      }
    } catch (error) {
      logger.error('Failed to submit verification request:', error)
      alert('An error occurred while submitting your verification request.')
    }
  }

  // Filter and sort progress data
  const filteredProgress = progressData
    .filter(p => selectedCategory === 'all' || p.category === selectedCategory)
    .sort((a, b) => {
      switch (sortBy) {
        case 'progress':
          return b.progressPercentage - a.progressPercentage
        case 'category':
          return a.category.localeCompare(b.category)
        case 'name':
          return a.badgeName.localeCompare(b.badgeName)
        default:
          return 0
      }
    })

  const eligibleBadges = progressData.filter(p => p.isEligible && p.currentProgress >= p.maxProgress).length
  const inProgressBadges = progressData.filter(p => p.currentProgress > 0 && p.currentProgress < p.maxProgress).length

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h2 className="text-xl font-semibold text-foreground">Badge Progress</h2>
          <div className="flex gap-4 text-sm text-muted-foreground mt-1">
            <span>{eligibleBadges} ready to claim</span>
            <span>{inProgressBadges} in progress</span>
            <span>{progressData.length} total badges</span>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={refreshProgress}
            disabled={isLoading}
          >
            {isLoading ? (
              <>
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-muted-foreground"></div>
                <span className="ml-2">Refreshing...</span>
              </>
            ) : (
              'Refresh Progress'
            )}
          </Button>
        </div>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-2">
        <select
          value={selectedCategory}
          onChange={(e) => setSelectedCategory(e.target.value as BadgeCategory | 'all')}
          className="px-3 py-1.5 text-sm border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
        >
          <option value="all">All Categories</option>
          {Object.values(BadgeCategory).map(cat => (
            <option key={cat} value={cat}>{cat}</option>
          ))}
        </select>

        <select
          value={sortBy}
          onChange={(e) => setSortBy(e.target.value as typeof sortBy)}
          className="px-3 py-1.5 text-sm border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
        >
          <option value="progress">Sort by Progress</option>
          <option value="category">Sort by Category</option>
          <option value="name">Sort by Name</option>
        </select>
      </div>

      {/* Progress Grid */}
      {filteredProgress.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredProgress.map(progressItem => (
            <BadgeProgressCard
              key={progressItem.badgeType}
              progress={progressItem}
              onStartVerification={handleStartVerification}
            />
          ))}
        </div>
      ) : (
        <div className="text-center py-12">
          <div className="text-6xl mb-4">🎯</div>
          <h3 className="text-lg font-semibold text-foreground mb-2">No badge progress found</h3>
          <p className="text-muted-foreground">Start completing projects to make progress towards earning badges!</p>
        </div>
      )}
    </div>
  )
}