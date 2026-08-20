'use client'

import { logger } from '@/utils/logger';

import React, { useState, useMemo } from 'react'
import { Filter, ChevronDown, ChevronUp } from 'lucide-react'
import { Badge } from '../ui/badge'
import { Button } from '../ui/button'
import BadgeDisplay from './BadgeDisplay'
import { UserBadge, BadgeCategory, BadgeListProps } from '../../types/badge'

export default function BadgeList({ 
  badges, 
  category, 
  showExpired = false, 
  groupByCategory = true 
}: BadgeListProps) {
  const [selectedCategory, setSelectedCategory] = useState<BadgeCategory | 'all'>(category || 'all')
  const [sortBy, setSortBy] = useState<'earned' | 'category' | 'name'>('earned')
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc')
  const [expandedCategories, setExpandedCategories] = useState<Set<BadgeCategory>>(
    new Set(Object.values(BadgeCategory))
  )

  // Filter and sort badges
  const processedBadges = useMemo(() => {
    let filtered = badges.filter(badge => {
      // Filter by active status
      if (!showExpired && (!badge.isActive || (badge.expiresAt && new Date(badge.expiresAt) < new Date()))) {
        return false
      }
      
      // Filter by selected category
      if (selectedCategory !== 'all' && badge.category !== selectedCategory) {
        return false
      }
      
      return true
    })

    // Sort badges
    filtered.sort((a, b) => {
      let comparison = 0
      
      switch (sortBy) {
        case 'earned':
          comparison = new Date(a.earnedAt).getTime() - new Date(b.earnedAt).getTime()
          break
        case 'category':
          comparison = a.category.localeCompare(b.category)
          break
        case 'name':
          comparison = a.badgeName.localeCompare(b.badgeName)
          break
      }
      
      return sortOrder === 'asc' ? comparison : -comparison
    })

    return filtered
  }, [badges, selectedCategory, sortBy, sortOrder, showExpired])

  // Group badges by category
  const groupedBadges = useMemo(() => {
    if (!groupByCategory) return { all: processedBadges }
    
    return processedBadges.reduce((groups, badge) => {
      if (!groups[badge.category]) {
        groups[badge.category] = []
      }
      groups[badge.category].push(badge)
      return groups
    }, {} as Record<BadgeCategory, UserBadge[]>)
  }, [processedBadges, groupByCategory])

  const toggleCategoryExpansion = (category: BadgeCategory) => {
    setExpandedCategories(prev => {
      const newSet = new Set(prev)
      if (newSet.has(category)) {
        newSet.delete(category)
      } else {
        newSet.add(category)
      }
      return newSet
    })
  }

  const badgeCount = processedBadges.length
  const activeBadges = processedBadges.filter(b => b.isActive && (!b.expiresAt || new Date(b.expiresAt) > new Date())).length
  const expiredBadges = processedBadges.filter(b => !b.isActive || (b.expiresAt && new Date(b.expiresAt) < new Date())).length

  if (badges.length === 0) {
    return (
      <div className="text-center py-12">
        <div className="text-6xl mb-4">🏆</div>
        <h3 className="text-lg font-semibold text-foreground mb-2">No badges yet</h3>
        <p className="text-muted-foreground">Complete projects and build your reputation to earn badges!</p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header with filters and stats */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div className="flex items-center gap-4">
          <h2 className="text-xl font-semibold text-foreground">
            Badges ({badgeCount})
          </h2>

          <div className="flex gap-2 text-sm">
            <Badge variant="success" className="bg-success/10 text-success">
              {activeBadges} Active
            </Badge>
            {expiredBadges > 0 && (
              <Badge variant="secondary" className="bg-muted text-muted-foreground">
                {expiredBadges} Expired
              </Badge>
            )}
          </div>
        </div>

        {/* Filters */}
        <div className="flex flex-wrap items-center gap-2">
          {/* Category Filter */}
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

          {/* Sort */}
          <select
            value={`${sortBy}-${sortOrder}`}
            onChange={(e) => {
              const [sort, order] = e.target.value.split('-') as [typeof sortBy, typeof sortOrder]
              setSortBy(sort)
              setSortOrder(order)
            }}
            className="px-3 py-1.5 text-sm border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
          >
            <option value="earned-desc">Recently Earned</option>
            <option value="earned-asc">Oldest First</option>
            <option value="name-asc">Name A-Z</option>
            <option value="name-desc">Name Z-A</option>
            <option value="category-asc">Category A-Z</option>
          </select>

          {/* Show Expired Toggle */}
          <Button
            variant={showExpired ? "default" : "outline"}
            size="sm"
            onClick={() => setSelectedCategory(selectedCategory)} // This would need to be passed up to parent
            className="text-sm"
          >
            <Filter className="h-4 w-4 mr-1" />
            {showExpired ? 'Hide' : 'Show'} Expired
          </Button>
        </div>
      </div>

      {/* Badge Grid */}
      {groupByCategory ? (
        <div className="space-y-8">
          {Object.entries(groupedBadges).map(([categoryKey, categoryBadges]) => {
            const category = categoryKey as BadgeCategory
            const isExpanded = expandedCategories.has(category)
            
            return (
              <div key={category} className="space-y-4">
                {/* Category Header */}
                <div className="flex items-center justify-between">
                  <h3 className="text-lg font-semibold text-foreground flex items-center gap-2">
                    <span className="text-2xl">
                      {category === BadgeCategory.Performance && '⭐'}
                      {category === BadgeCategory.Volume && '🎯'}
                      {category === BadgeCategory.Expertise && '🏆'}
                      {category === BadgeCategory.Trust && '🛡️'}
                      {category === BadgeCategory.Community && '👥'}
                      {category === BadgeCategory.Achievement && '🏅'}
                    </span>
                    {category} ({categoryBadges.length})
                  </h3>
                  
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => toggleCategoryExpansion(category)}
                    className="flex items-center gap-1"
                  >
                    {isExpanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                    {isExpanded ? 'Collapse' : 'Expand'}
                  </Button>
                </div>

                {/* Category Badges */}
                {isExpanded && (
                  <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-6">
                    {categoryBadges.map(badge => (
                      <BadgeDisplay
                        key={badge.id}
                        badge={badge}
                        size="medium"
                        showDetails={false}
                        onClick={() => {
                          // This could open a badge detail modal
                          logger.debug('Badge clicked:', badge)
                        }}
                      />
                    ))}
                  </div>
                )}
              </div>
            )
          })}
        </div>
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-6">
          {processedBadges.map(badge => (
            <BadgeDisplay
              key={badge.id}
              badge={badge}
              size="medium"
              showDetails={false}
              onClick={() => {
                // This could open a badge detail modal
                logger.debug('Badge clicked:', badge)
              }}
            />
          ))}
        </div>
      )}

      {/* No results message */}
      {badgeCount === 0 && badges.length > 0 && (
        <div className="text-center py-8">
          <div className="text-4xl mb-2">🔍</div>
          <h3 className="text-lg font-semibold text-foreground mb-2">No badges found</h3>
          <p className="text-muted-foreground">Try adjusting your filters to see more badges.</p>
        </div>
      )}
    </div>
  )
}