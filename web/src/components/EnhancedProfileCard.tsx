'use client'

import React, { useState } from 'react'
import Link from 'next/link'
import Image from 'next/image'
import {
  User,
  MapPin,
  Mail,
  Star,
  Briefcase,
  Award,
  Calendar,
  MessageSquare,
  ExternalLink,
  CheckCircle,
  TrendingUp,
  Clock,
  DollarSign,
  Users,
  Heart,
  ThumbsUp,
  Eye,
  Shield,
  Zap
} from 'lucide-react'
import { cn } from '@/lib/utils'

interface EnhancedProfileCardProps {
  user: {
    id: string
    name: string
    title: string
    avatar?: string
    email: string
    location: string
    bio: string
    skills: string[]
    rating: number
    reviews: number
    completedProjects: number
    totalEarnings: number
    hourlyRate: number
    memberSince: string
    responseTime: string
    lastActive: string
    verified: boolean
    featured?: boolean
    topRated?: boolean
    availability: 'available' | 'busy' | 'away'
    languages: string[]
    education?: string[]
    certifications?: string[]
  }
  variant?: 'card' | 'compact' | 'detailed'
  animated?: boolean
  showActions?: boolean
}

export function EnhancedProfileCard({
  user,
  variant = 'card',
  animated = true,
  showActions = true
}: EnhancedProfileCardProps) {
  const [isHovered, setIsHovered] = useState(false)
  const [isFollowing, setIsFollowing] = useState(false)

  const getAvailabilityColor = (availability: string) => {
    switch (availability) {
      case 'available':
        return 'from-success to-success'
      case 'busy':
        return 'from-warning to-warning'
      case 'away':
        return 'from-muted-foreground to-muted-foreground'
      default:
        return 'from-muted-foreground to-muted-foreground'
    }
  }

  const getAvailabilityText = (availability: string) => {
    switch (availability) {
      case 'available':
        return 'Available for work'
      case 'busy':
        return 'Currently busy'
      case 'away':
        return 'Away'
      default:
        return 'Status unknown'
    }
  }

  if (variant === 'compact') {
    return (
      <Link
        href={`/profile/${user.id}`}
        className={cn(
          "group block p-4 card-premium transition-all duration-300",
          animated && "hover:scale-102 hover:z-10 hover:shadow-lg"
        )}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      >
        <div className="flex items-center space-x-4">
          {/* Avatar */}
          <div className="relative">
            <div className={cn(
              "w-16 h-16 rounded-2xl bg-gradient-to-br from-primary to-secondary flex items-center justify-center transition-all duration-300",
              animated && "group-hover:scale-110 group-hover:rotate-6"
            )}>
              {user.avatar ? (
                <Image
                  src={user.avatar}
                  alt={user.name}
                  width={64}
                  height={64}
                  className="w-full h-full rounded-2xl object-cover"
                />
              ) : (
                <User className="w-8 h-8 text-primary-foreground" />
              )}
            </div>
            {/* Verification Badge */}
            {user.verified && (
              <div className="absolute -bottom-1 -right-1 w-6 h-6 bg-primary rounded-full flex items-center justify-center">
                <CheckCircle className="w-4 h-4 text-primary-foreground" />
              </div>
            )}
            {/* Availability Indicator */}
            <div className={cn(
              "absolute -top-1 -right-1 w-4 h-4 rounded-full border-2 border-card",
              user.availability === 'available' ? "bg-success animate-pulse" :
              user.availability === 'busy' ? "bg-warning" :
              "bg-muted"
            )}></div>
          </div>

          {/* User Info */}
          <div className="flex-1 min-w-0">
            <div className="flex items-center space-x-2">
              <h3 className="text-lg font-bold text-foreground group-hover:text-primary transition-colors duration-300 truncate">
                {user.name}
              </h3>
              {user.topRated && (
                <div className="flex items-center space-x-1 px-2 py-1 bg-gradient-to-r from-warning to-warning text-warning-foreground text-xs font-bold rounded-full">
                  <Award className="w-3 h-3" />
                  <span>TOP RATED</span>
                </div>
              )}
            </div>

            <p className="text-sm text-muted-foreground mb-2">{user.title}</p>

            {/* Rating and Stats */}
            <div className="flex items-center space-x-4 text-sm text-muted-foreground">
              <div className="flex items-center space-x-1">
                <Star className="w-4 h-4 text-warning fill-warning" />
                <span className="font-medium">{user.rating}</span>
                <span>({user.reviews})</span>
              </div>
              <div className="flex items-center space-x-1">
                <Briefcase className="w-4 h-4" />
                <span>{user.completedProjects} projects</span>
              </div>
              <div className="flex items-center space-x-1">
                <DollarSign className="w-4 h-4 text-success" />
                <span>${user.hourlyRate}/hr</span>
              </div>
            </div>

            {/* Skills */}
            <div className="flex flex-wrap gap-1 mt-2">
              {user.skills.slice(0, 3).map((skill) => (
                <span
                  key={skill}
                  className="px-2 py-1 text-xs bg-primary/10 text-primary rounded-full"
                >
                  {skill}
                </span>
              ))}
              {user.skills.length > 3 && (
                <span className="px-2 py-1 text-xs bg-muted text-muted-foreground rounded-full">
                  +{user.skills.length - 3}
                </span>
              )}
            </div>
          </div>

          {/* Actions */}
          <div className="flex flex-col space-y-2">
            <button
              onClick={(e) => {
                e.preventDefault()
                setIsFollowing(!isFollowing)
              }}
              className={cn(
                "px-4 py-2 rounded-full text-sm font-medium transition-all duration-300",
                isFollowing
                  ? "bg-primary text-primary-foreground"
                  : "bg-muted text-muted-foreground hover:bg-primary hover:text-primary-foreground",
                animated && "hover:scale-105"
              )}
            >
              {isFollowing ? 'Following' : 'Follow'}
            </button>
            <button className="p-2 rounded-full bg-card border border-border hover:bg-muted transition-all duration-300">
              <MessageSquare className="w-4 h-4" />
            </button>
          </div>
        </div>
      </Link>
    )
  }

  return (
    <Link
      href={`/profile/${user.id}`}
      className={cn(
        "group block card-elevated relative overflow-hidden transition-all duration-500",
        animated && [
          "hover:scale-105 hover:z-10",
          isHovered && "animate-float-3d"
        ]
      )}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      style={{ perspective: '1000px' }}
    >
      {/* Background Gradient */}
      <div className="absolute inset-0 bg-gradient-to-br from-primary/5 via-transparent to-secondary/5 opacity-0 group-hover:opacity-100 transition-opacity duration-500"></div>

      {/* Featured Banner */}
      {user.featured && (
        <div className="absolute top-0 left-0 right-0 bg-gradient-to-r from-warning to-warning text-warning-foreground text-xs font-bold py-2 px-4 z-10 flex items-center justify-center animate-gradient-shift">
          <Award className="w-4 h-4 mr-2 animate-pulse" />
          FEATURED FREELANCER
        </div>
      )}

      <div className="p-6 space-y-6 relative z-10">
        {/* Header Section */}
        <div className="flex items-start justify-between">
          <div className="flex items-center space-x-4">
            {/* Avatar */}
            <div className="relative">
              <div className={cn(
                "w-20 h-20 rounded-3xl bg-gradient-to-br from-primary to-secondary flex items-center justify-center transition-all duration-300",
                animated && "group-hover:scale-110 group-hover:rotate-6"
              )}>
                {user.avatar ? (
                  <Image
                    src={user.avatar}
                    alt={user.name}
                    width={80}
                    height={80}
                    className="w-full h-full rounded-3xl object-cover"
                  />
                ) : (
                  <User className="w-10 h-10 text-primary-foreground" />
                )}
              </div>

              {/* Verification Badge */}
              {user.verified && (
                <div className="absolute -bottom-2 -right-2 w-8 h-8 bg-primary rounded-full flex items-center justify-center shadow-lg">
                  <Shield className="w-5 h-5 text-primary-foreground" />
                </div>
              )}

              {/* Availability Indicator */}
              <div className={cn(
                "absolute -top-1 -right-1 w-5 h-5 rounded-full border-2 border-card shadow-md",
                user.availability === 'available' ? "bg-success animate-pulse" :
                user.availability === 'busy' ? "bg-warning" :
                "bg-muted"
              )}></div>
            </div>

            {/* Basic Info */}
            <div>
              <div className="flex items-center space-x-2 mb-1">
                <h2 className="text-2xl font-bold text-foreground group-hover:text-primary transition-colors duration-300">
                  {user.name}
                </h2>
                {user.topRated && (
                  <div className="flex items-center space-x-1 px-2 py-1 bg-gradient-to-r from-warning to-warning text-warning-foreground text-xs font-bold rounded-full">
                    <Award className="w-3 h-3" />
                    <span>TOP RATED</span>
                  </div>
                )}
              </div>

              <p className="text-lg text-muted-foreground mb-2">{user.title}</p>

              <div className="flex items-center space-x-4 text-sm text-muted-foreground">
                <div className="flex items-center space-x-1">
                  <MapPin className="w-4 h-4" />
                  <span>{user.location}</span>
                </div>
                <div className="flex items-center space-x-1">
                  <Calendar className="w-4 h-4" />
                  <span>Since {user.memberSince}</span>
                </div>
              </div>
            </div>
          </div>

          {/* Actions */}
          {showActions && (
            <div className="flex flex-col space-y-2">
              <button
                onClick={(e) => {
                  e.preventDefault()
                  setIsFollowing(!isFollowing)
                }}
                className={cn(
                  "px-6 py-2 rounded-full text-sm font-medium transition-all duration-300",
                  isFollowing
                    ? "bg-primary text-primary-foreground"
                    : "bg-muted text-muted-foreground hover:bg-primary hover:text-primary-foreground",
                  animated && "hover:scale-105"
                )}
              >
                {isFollowing ? 'Following' : 'Follow'}
              </button>
              <button className="px-6 py-2 rounded-full bg-card border border-border hover:bg-muted transition-all duration-300 flex items-center justify-center space-x-2">
                <MessageSquare className="w-4 h-4" />
                <span>Message</span>
              </button>
            </div>
          )}
        </div>

        {/* Bio */}
        <p className="text-sm text-muted-foreground leading-relaxed line-clamp-3">
          {user.bio}
        </p>

        {/* Rating Stats */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 py-4 border-t border-border/30">
          <div className="text-center">
            <div className="flex items-center justify-center space-x-1 text-2xl font-bold text-warning">
              <Star className="w-6 h-6 fill-warning" />
              <span>{user.rating}</span>
            </div>
            <div className="text-xs text-muted-foreground">Rating</div>
          </div>

          <div className="text-center">
            <div className="text-2xl font-bold text-primary">
              {user.completedProjects}
            </div>
            <div className="text-xs text-muted-foreground">Completed</div>
          </div>

          <div className="text-center">
            <div className="text-2xl font-bold text-success">
              ${user.hourlyRate}
            </div>
            <div className="text-xs text-muted-foreground">Per Hour</div>
          </div>

          <div className="text-center">
            <div className="text-2xl font-bold text-accent">
              {user.responseTime}
            </div>
            <div className="text-xs text-muted-foreground">Response</div>
          </div>
        </div>

        {/* Skills */}
        <div className="space-y-3">
          <h3 className="text-sm font-semibold text-foreground">Skills & Expertise</h3>
          <div className="flex flex-wrap gap-2">
            {user.skills.map((skill, index) => (
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
          </div>
        </div>

        {/* Availability Status */}
        <div className={cn(
          "flex items-center justify-between p-4 rounded-2xl border",
          user.availability === 'available' ? "bg-success/10 border-success/20" :
          user.availability === 'busy' ? "bg-warning/10 border-warning/20" :
          "bg-muted border-border"
        )}>
          <div className="flex items-center space-x-3">
            <div className={cn(
              "w-3 h-3 rounded-full",
              user.availability === 'available' ? "bg-success animate-pulse" :
              user.availability === 'busy' ? "bg-warning" :
              "bg-muted-foreground"
            )}></div>
            <span className="text-sm font-medium text-foreground">
              {getAvailabilityText(user.availability)}
            </span>
          </div>
          {user.availability === 'available' && (
            <div className="flex items-center space-x-1 text-success">
              <Zap className="w-4 h-4" />
              <span className="text-sm font-medium">Ready to start</span>
            </div>
          )}
        </div>

        {/* Languages */}
        {user.languages && user.languages.length > 0 && (
          <div className="space-y-2">
            <h3 className="text-sm font-semibold text-foreground">Languages</h3>
            <div className="flex flex-wrap gap-2">
              {user.languages.map((language) => (
                <span
                  key={language}
                  className="px-3 py-1 text-xs rounded-full bg-card border border-border"
                >
                  {language}
                </span>
              ))}
            </div>
          </div>
        )}

        {/* Footer Stats */}
        <div className="flex items-center justify-between pt-4 border-t border-border/30 text-xs text-muted-foreground">
          <div className="flex items-center space-x-4">
            <div className="flex items-center space-x-1">
              <Eye className="w-3 h-3" />
              <span>Last active {user.lastActive}</span>
            </div>
            <div className="flex items-center space-x-1">
              <TrendingUp className="w-3 h-3" />
              <span>${user.totalEarnings.toLocaleString()} earned</span>
            </div>
          </div>

          <div className="flex items-center space-x-2">
            <ThumbsUp className="w-3 h-3" />
            <span>{user.reviews} reviews</span>
          </div>
        </div>
      </div>

      {/* Hover Glow Effect */}
      {isHovered && (
        <div className="absolute inset-0 pointer-events-none">
          <div className="absolute inset-0 bg-gradient-to-br from-primary/10 to-secondary/10 rounded-2xl blur-xl animate-pulse-glow"></div>
        </div>
      )}
    </Link>
  )
}