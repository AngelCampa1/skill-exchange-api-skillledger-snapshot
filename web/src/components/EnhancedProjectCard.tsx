'use client'

import React, { useState } from 'react'
import Link from 'next/link'
import {
  Briefcase,
  MapPin,
  DollarSign,
  Clock,
  Users,
  Star,
  TrendingUp,
  ExternalLink,
  Heart,
  MessageSquare,
  Calendar,
  Award,
  Zap
} from 'lucide-react'
import { cn } from '@/lib/utils'

interface EnhancedProjectCardProps {
  project: {
    id: string
    title: string
    description: string
    category: string
    budget: number
    budgetType: 'fixed' | 'hourly'
    duration: string
    location: string
    remote: boolean
    status: 'open' | 'in_progress' | 'completed'
    clientRating?: number
    applicationsCount: number
    skills: string[]
    featured?: boolean
    urgent?: boolean
    postedAt: string
    matchScore?: number
  }
  variant?: 'search' | 'recommendation' | 'marketplace'
  animated?: boolean
}

export function EnhancedProjectCard({
  project,
  variant = 'search',
  animated = true
}: EnhancedProjectCardProps) {
  const [isHovered, setIsHovered] = useState(false)
  const [isFavorited, setIsFavorited] = useState(false)

  const getCategoryColor = (category: string) => {
    const colors = {
      'Development': 'from-primary to-primary',
      'Design': 'from-primary to-secondary',
      'Marketing': 'from-secondary to-secondary',
      'Writing': 'from-success to-success',
      'Consulting': 'from-warning to-warning',
      'Sales': 'from-destructive to-destructive'
    }
    return colors[category as keyof typeof colors] || 'from-muted-foreground to-muted-foreground'
  }

  return (
    <Link
      href={`/projects/${project.id}`}
      className={cn(
        "group block relative transform-gpu transition-all duration-500 ease-out",
        animated && [
          "hover:scale-105 hover:z-10",
          isHovered && "animate-float-3d"
        ]
      )}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      style={{ perspective: '1000px' }}
    >
      <div className={cn(
        "card-interactive relative overflow-hidden",
        "before:absolute before:inset-0 before:bg-gradient-to-br before:opacity-0 before:transition-opacity before:duration-500",
        isHovered && "before:opacity-100",
        project.featured && "ring-2 ring-primary/20 ring-offset-2"
      )}>
        {/* 3D Gradient Background */}
        <div
          className={cn(
            "absolute inset-0 bg-gradient-to-br opacity-5 transition-opacity duration-500",
            getCategoryColor(project.category)
          )}
        />

        {/* Featured Banner */}
        {project.featured && (
          <div className="absolute top-0 left-0 right-0 bg-gradient-to-r from-warning to-warning text-warning-foreground text-xs font-bold py-2 px-4 z-10 flex items-center justify-center animate-gradient-shift">
            <Award className="w-4 h-4 mr-2 animate-pulse" />
            FEATURED PROJECT
          </div>
        )}

        {/* Urgent Badge */}
        {project.urgent && (
          <div className="absolute top-0 right-0 bg-gradient-to-r from-destructive to-destructive text-destructive-foreground text-xs font-bold py-2 px-4 z-10 flex items-center justify-center animate-pulse-glow">
            <Zap className="w-4 h-4 mr-1" />
            URGENT
          </div>
        )}

        <div className="p-6 space-y-4 relative z-10">
          {/* Project Header */}
          <div className="space-y-3">
            <div className="flex items-start justify-between">
              <div className="flex items-center space-x-3">
                <div className={cn(
                  "w-12 h-12 rounded-2xl flex items-center justify-center transition-transform duration-300",
                  animated && "group-hover:scale-110 group-hover:rotate-6"
                )}>
                  <Briefcase className={cn(
                    "w-6 h-6 transition-colors duration-300",
                    isHovered ? "text-primary" : "text-muted-foreground"
                  )} />
                </div>
                <div>
                  <h3 className="text-lg font-bold text-foreground group-hover:text-primary transition-colors duration-300 line-clamp-2">
                    {project.title}
                  </h3>
                  <div className="flex items-center space-x-2 text-xs text-muted-foreground">
                    <span
                      className="px-2 py-1 rounded-full bg-gradient-to-r text-primary-foreground text-xs font-medium shadow-md"
                      style={{
                        backgroundImage: `linear-gradient(to right, ${getCategoryColor(project.category)
                          .split(' ')
                          .map(c => c.replace('from-', 'hsl(var(--').replace('-500', '-500)').replace('-600', '-500)'))
                          .join(', ')})`
                      }}
                    >
                      {project.category}
                    </span>
                    {project.remote && (
                      <span className="px-2 py-1 rounded-full bg-success/10 text-success text-xs font-medium">
                        Remote
                      </span>
                    )}
                  </div>
                </div>
              </div>

              {/* Favorite Button */}
              <button
                onClick={(e) => {
                  e.preventDefault()
                  setIsFavorited(!isFavorited)
                }}
                className={cn(
                  "w-10 h-10 rounded-full flex items-center justify-center transition-all duration-300",
                  isFavorited
                    ? "bg-destructive text-destructive-foreground scale-110"
                    : "bg-muted text-muted-foreground hover:bg-destructive/10 hover:text-destructive",
                  animated && "hover:scale-110 hover:rotate-12"
                )}
              >
                <Heart className={cn(
                  "w-5 h-5 transition-transform duration-300",
                  isFavorited && "animate-scale-rotate-in"
                )} />
              </button>
            </div>

            {/* Description */}
            <p className="text-sm text-muted-foreground line-clamp-3 leading-relaxed">
              {project.description}
            </p>
          </div>

          {/* Skills Tags */}
          <div className="flex flex-wrap gap-2">
            {project.skills.slice(0, 4).map((skill, index) => (
              <span
                key={skill}
                className={cn(
                  "px-3 py-1 text-xs font-medium rounded-full bg-primary/10 text-primary border border-primary/20 transition-all duration-300",
                  animated && "hover:scale-110 hover:bg-primary/20",
                  isHovered && "animate-slide-up"
                )}
                style={{ animationDelay: `${index * 100}ms` }}
              >
                {skill}
              </span>
            ))}
            {project.skills.length > 4 && (
              <span className="px-3 py-1 text-xs font-medium rounded-full bg-muted text-muted-foreground">
                +{project.skills.length - 4} more
              </span>
            )}
          </div>

          {/* Project Metrics */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 py-4 border-t border-border/30">
            {/* Budget */}
            <div className="flex items-center space-x-2">
              <div className="relative">
                <DollarSign className="w-4 h-4 text-success" />
                <div className={cn(
                  "absolute -top-1 -right-1 w-2 h-2 bg-success rounded-full",
                  animated && "animate-ping"
                )}></div>
              </div>
              <div>
                <div className="text-lg font-bold text-success">
                  {project.budget.toLocaleString()} credits
                </div>
                <div className="text-xs text-muted-foreground capitalize">
                  {project.budgetType}
                </div>
              </div>
            </div>

            {/* Duration */}
            <div className="flex items-center space-x-2">
              <Clock className="w-4 h-4 text-primary" />
              <div>
                <div className="text-sm font-semibold text-foreground">
                  {project.duration}
                </div>
                <div className="text-xs text-muted-foreground">Duration</div>
              </div>
            </div>

            {/* Location */}
            <div className="flex items-center space-x-2">
              <MapPin className="w-4 h-4 text-warning" />
              <div>
                <div className="text-sm font-semibold text-foreground">
                  {project.location}
                </div>
                <div className="text-xs text-muted-foreground">Location</div>
              </div>
            </div>

            {/* Applications */}
            <div className="flex items-center space-x-2">
              <Users className="w-4 h-4 text-primary" />
              <div>
                <div className="text-sm font-semibold text-foreground">
                  {project.applicationsCount}
                </div>
                <div className="text-xs text-muted-foreground">Applied</div>
              </div>
            </div>
          </div>

          {/* Match Score (for recommendations) */}
          {project.matchScore && variant === 'recommendation' && (
            <div className="flex items-center justify-between pt-4 border-t border-border/30">
              <div className="flex items-center space-x-3">
                <div className="relative w-12 h-12">
                  <svg className="transform -rotate-90 w-12 h-12">
                    <circle
                      cx="24"
                      cy="24"
                      r="20"
                      stroke="currentColor"
                      strokeWidth="4"
                      fill="none"
                      className="text-muted"
                    />
                    <circle
                      cx="24"
                      cy="24"
                      r="20"
                      stroke="currentColor"
                      strokeWidth="4"
                      fill="none"
                      strokeDasharray={`${2 * Math.PI * 20}`}
                      strokeDashoffset={`${2 * Math.PI * 20 * (1 - project.matchScore / 100)}`}
                      className="text-primary transition-all duration-1000 ease-out"
                    />
                  </svg>
                  <div className="absolute inset-0 flex items-center justify-center">
                    <span className="text-sm font-bold text-primary">
                      {Math.round(project.matchScore)}%
                    </span>
                  </div>
                </div>
                <div>
                  <div className="text-sm font-semibold text-foreground">
                    Match Score
                  </div>
                  <div className="text-xs text-muted-foreground">
                    Based on your skills
                  </div>
                </div>
              </div>

              <TrendingUp className="w-5 h-5 text-success animate-bounce-in" />
            </div>
          )}

          {/* Client Rating */}
          {project.clientRating && (
            <div className="flex items-center space-x-2 pt-4 border-t border-border/30">
              <div className="flex items-center">
                {[...Array(5)].map((_, i) => (
                  <Star
                    key={i}
                    className={cn(
                      "w-4 h-4 transition-all duration-300",
                      i < (project.clientRating || 0)
                        ? "text-warning fill-warning"
                        : "text-muted",
                      animated && "hover:scale-110"
                    )}
                  />
                ))}
              </div>
              <span className="text-sm text-muted-foreground">
                Client Rating ({project.clientRating}.0)
              </span>
            </div>
          )}

          {/* Footer Actions */}
          <div className="flex items-center justify-between pt-4 border-t border-border/30">
            <div className="flex items-center space-x-4 text-xs text-muted-foreground">
              <div className="flex items-center space-x-1">
                <Calendar className="w-3 h-3" />
                <span>{project.postedAt}</span>
              </div>
              <div className="flex items-center space-x-1">
                <MessageSquare className="w-3 h-3" />
                <span>Active</span>
              </div>
            </div>

            <div className="flex items-center space-x-2">
              <span className={cn(
                "px-3 py-1 rounded-full text-xs font-medium",
                project.status === 'open' ? "bg-success/10 text-success" :
                project.status === 'in_progress' ? "bg-primary/10 text-primary" :
                "bg-muted text-muted-foreground"
              )}>
                {project.status.replace('_', ' ').toUpperCase()}
              </span>

              <ExternalLink className="w-4 h-4 text-muted-foreground group-hover:text-primary transition-colors duration-300 group-hover:translate-x-1" />
            </div>
          </div>
        </div>

        {/* Hover Glow Effect */}
        {isHovered && (
          <div className="absolute inset-0 pointer-events-none">
            <div className="absolute inset-0 bg-gradient-to-br from-primary/10 to-secondary/10 rounded-2xl blur-xl animate-pulse-glow"></div>
          </div>
        )}
      </div>
    </Link>
  )
}