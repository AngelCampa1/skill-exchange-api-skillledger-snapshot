'use client'

import { logger } from '@/utils/logger';
import { getSafeHref } from '@/lib/urlValidation' // VULN-009 FIX

import { useState, useEffect, useCallback } from 'react'
import { useRouter } from 'next/navigation'
import Image from 'next/image'
import Link from 'next/link'
import { Linkedin, Github, Globe, Edit, Save, X, ArrowLeft } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import ProfileCreationForm from '@/components/ProfileCreationForm'
import { ThemeToggle } from '@/components/ThemeToggle'
import LogoutButton from '@/components/LogoutButton'

interface SelectedSkill {
  skillId: string
  skillName: string
  category: string
  proficiency: 'Beginner' | 'Intermediate' | 'Advanced' | 'Expert'
  yearsOfExperience?: number
  notes?: string
}

interface Profile {
  id: string
  userId: string
  firstName?: string
  lastName?: string
  fullName?: string
  title?: string
  summary?: string
  company?: string
  websiteUrl?: string
  linkedInUrl?: string
  gitHubUrl?: string
  location?: string
  timeZone?: string
  avatarUrl?: string
  isPublic: boolean
  isComplete: boolean
  createdAt: string
  updatedAt: string
  skills?: SelectedSkill[]  // BUG-013 FIX: Include skills in profile
}

export default function MyProfilePage() {
  const [profile, setProfile] = useState<Profile | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isEditing, setIsEditing] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  // E2E-016 FIX: Use isInitialized to wait for auth context to load before checking authentication
  const { isAuthenticated, isInitialized, isLoading: authLoading } = useAuth()
  const router = useRouter()

  const fetchProfile = useCallback(async () => {
    try {
      setIsLoading(true)
      setError(null)

      const response = await fetch('/api/profile/me', {
        credentials: 'include',
      })

      if (response.status === 404) {
        // Profile doesn't exist, show creation form
        setProfile(null)
        setIsEditing(true)
      } else if (response.ok) {
        const profileData = await response.json()

        // BUG-013 FIX: Also fetch user skills to include in profile data
        try {
          const skillsResponse = await fetch('/api/skill/my-skills', {
            credentials: 'include',
          })
          if (skillsResponse.ok) {
            const userSkills = await skillsResponse.json()
            // Map backend UserSkillDto to frontend SelectedSkill format
            profileData.skills = userSkills.map((us: any) => ({
              skillId: us.skill?.id || us.skillId,
              skillName: us.skill?.name || us.skillName || 'Unknown',
              category: us.skill?.category?.name || us.category || 'General',
              proficiency: us.proficiency || us.proficiencyDisplay || 'Beginner',
              yearsOfExperience: us.yearsOfExperience || 0,
              notes: us.notes || '',
            }))
          }
        } catch (skillErr) {
          logger.error('Failed to fetch user skills:', skillErr)
          // Don't fail profile load if skills fetch fails
        }

        setProfile(profileData)
      } else {
        const errorData = await response.json().catch(() => ({}))
        setError(errorData.message || 'Failed to load profile')
      }
    } catch (err) {
      setError('An unexpected error occurred')
      logger.error('Profile fetch error:', err)
    } finally {
      setIsLoading(false)
    }
  }, [])

  // E2E-016 FIX: Wait for auth context to initialize before redirecting
  useEffect(() => {
    // Don't check authentication until auth context is initialized
    if (!isInitialized || authLoading) {
      return
    }

    if (!isAuthenticated) {
      // E2E-017 FIX: Call logout API to clear any stale cookies before redirecting
      fetch('/api/auth/logout', { method: 'POST', credentials: 'include' })
        .finally(() => {
          window.location.href = '/login'
        })
      return
    }
    fetchProfile()
  }, [isAuthenticated, isInitialized, authLoading, router, fetchProfile])

  const handleUpdateProfile = async (data: any) => {
    try {
      setError(null)
      setIsSaving(true)

      if (!isAuthenticated) {
        setError('You must be logged in to update your profile')
        return
      }

      // Get CSRF token
      const csrfResponse = await fetch('/api/auth/csrf-token', {
        credentials: 'include',
      })
      if (!csrfResponse.ok) {
        setError('Failed to fetch security token. Please try again.')
        return
      }
      const csrfData = await csrfResponse.json()

      // Extract skills from data - they need to be saved via separate API
      const { skills, ...profileData } = data

      const endpoint = profile ? '/api/profile' : '/api/profile'
      const method = profile ? 'PUT' : 'POST'

      const response = await fetch(endpoint, {
        method,
        headers: {
          'Content-Type': 'application/json',
          'X-CSRF-TOKEN': csrfData.token,
        },
        credentials: 'include',
        body: JSON.stringify(profileData),
      })

      const result = await response.json().catch(() => ({ success: false }))

      if (response.ok && result.success) {
        // BUG-013 FIX: Save skills via separate API after profile creation
        if (skills && Array.isArray(skills) && skills.length > 0) {
          const skillErrors: string[] = []

          for (const skill of skills) {
            try {
              const skillResponse = await fetch('/api/skill/my-skills', {
                method: 'POST',
                headers: {
                  'Content-Type': 'application/json',
                  'X-CSRF-TOKEN': csrfData.token,
                },
                credentials: 'include',
                body: JSON.stringify({
                  skillId: skill.skillId,
                  proficiency: skill.proficiency,
                  yearsOfExperience: skill.yearsOfExperience || 0,
                }),
              })

              if (!skillResponse.ok) {
                const skillResult = await skillResponse.json().catch(() => ({}))
                // Don't fail if skill already exists (409 Conflict)
                if (skillResponse.status !== 409) {
                  skillErrors.push(skillResult.message || `Failed to add skill: ${skill.skillName}`)
                }
              }
            } catch (skillErr) {
              logger.error('Error adding skill:', skillErr)
              skillErrors.push(`Failed to add skill: ${skill.skillName}`)
            }
          }

          if (skillErrors.length > 0) {
            logger.warn('Some skills failed to save:', skillErrors)
            // Don't block profile creation for skill errors, but log them
          }
        }

        setProfile(result.profile)
        setIsEditing(false)
        await fetchProfile() // Refresh profile data
      } else {
        setError(result.message || 'Failed to update profile')
      }
    } catch (err) {
      setError('An unexpected error occurred')
      logger.error('Profile update error:', err)
    } finally {
      setIsSaving(false)
    }
  }

  // E2E-016 FIX: Show loading while auth is initializing or profile is loading
  if (!isInitialized || authLoading || isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto"></div>
          <p className="mt-4 text-muted-foreground">Loading your profile...</p>
        </div>
      </div>
    )
  }

  if (isEditing || !profile) {
    return (
      <div className="min-h-screen bg-background">
        {/* BUG-013 FIX: Add navigation header to profile creation/editing page */}
        <nav className="bg-card/90 backdrop-blur-xl border-b border-border/50 sticky top-0 z-50 shadow-lg shadow-primary/5">
          <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="flex justify-between items-center h-16">
              <Link
                href="/dashboard"
                className="flex items-center text-foreground hover:text-primary transition-colors duration-300"
              >
                <ArrowLeft className="w-5 h-5 mr-2" />
                <span className="font-medium">Back to Dashboard</span>
              </Link>

              <div className="flex items-center space-x-4">
                <ThemeToggle />
                <LogoutButton showAllDevicesOption={false} />
              </div>
            </div>
          </div>
        </nav>

        <div className="py-12">
          <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
            {error && (
              <div className="mb-6 bg-destructive/10 border border-destructive/20 rounded-lg p-4">
                <div className="flex">
                  <div className="flex-shrink-0">
                    <span className="text-destructive">✗</span>
                  </div>
                  <div className="ml-3">
                    <p className="text-sm text-destructive">{error}</p>
                  </div>
                </div>
              </div>
            )}

            <ProfileCreationForm
              onSubmit={handleUpdateProfile}
              initialData={profile || undefined}
              submitButtonText={profile ? "Update Profile" : "Create Profile"}
              isLoading={isSaving}
            />

            {profile && (
              <div className="mt-4 text-center">
                <button
                  onClick={() => setIsEditing(false)}
                  className="text-sm text-muted-foreground hover:text-foreground"
                >
                  Cancel
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      {/* BUG-013 FIX: Add navigation header to profile view page */}
      <nav className="bg-card/90 backdrop-blur-xl border-b border-border/50 sticky top-0 z-50 shadow-lg shadow-primary/5">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <Link
              href="/dashboard"
              className="flex items-center text-foreground hover:text-primary transition-colors duration-300"
            >
              <ArrowLeft className="w-5 h-5 mr-2" />
              <span className="font-medium">Back to Dashboard</span>
            </Link>

            <div className="flex items-center space-x-4">
              <ThemeToggle />
              <LogoutButton showAllDevicesOption={false} />
            </div>
          </div>
        </div>
      </nav>

      <div className="py-12">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
          {error && (
            <div className="mb-6 bg-destructive/10 border border-destructive/20 rounded-lg p-4">
              <div className="flex">
                <div className="flex-shrink-0">
                  <span className="text-destructive">✗</span>
                </div>
                <div className="ml-3">
                  <p className="text-sm text-destructive">{error}</p>
                </div>
              </div>
            </div>
          )}

          {/* Profile Display */}
          <div className="bg-card border border-border rounded-lg shadow-sm overflow-hidden">
            {/* Header */}
            <div className="bg-gradient-to-r from-primary to-primary/90 px-6 py-8 text-primary-foreground">
              <div className="flex items-center justify-between">
                <div className="flex items-center">
                  {profile.avatarUrl ? (
                    <div className="relative h-20 w-20">
                      <Image
                        src={profile.avatarUrl}
                        alt="Profile"
                        fill
                        className="rounded-full border-4 border-background object-cover"
                      />
                    </div>
                  ) : (
                    <div className="h-20 w-20 rounded-full bg-primary-foreground/20 flex items-center justify-center text-2xl font-bold">
                      {(profile.firstName?.[0] || '') + (profile.lastName?.[0] || '')}
                    </div>
                  )}
                  <div className="ml-6">
                    <h1 className="text-3xl font-bold">
                      {profile.fullName || profile.firstName || 'Anonymous User'}
                    </h1>
                    {profile.title && (
                      <p className="text-xl opacity-90">{profile.title}</p>
                    )}
                    {profile.company && (
                      <p className="text-lg opacity-80">{profile.company}</p>
                    )}
                  </div>
                </div>
                <button
                  onClick={() => setIsEditing(true)}
                  className="bg-primary-foreground/20 hover:bg-primary-foreground/30 text-primary-foreground px-4 py-2 rounded-md transition-colors focus:ring-2 focus:ring-primary-foreground/50 focus:outline-none"
                >
                  Edit Profile
                </button>
              </div>
            </div>

            {/* Profile Content */}
            <div className="p-6">
              {/* Completion Status */}
              <div className={`mb-6 p-4 rounded-md ${profile.isComplete
                ? 'bg-success/10 border border-success/20'
                : 'bg-warning/10 border border-warning/20'
              }`}>
                <div className="flex items-center">
                  <span className={profile.isComplete ? 'text-success' : 'text-warning'}>
                    {profile.isComplete ? '✓' : '⚠'}
                  </span>
                  <p className={`ml-2 text-sm ${profile.isComplete ? 'text-success' : 'text-warning'}`}>
                    {profile.isComplete
                      ? 'Your profile is complete!'
                      : 'Complete your profile by adding your first name, last name, and professional title.'
                    }
                  </p>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* Left Column */}
                <div className="space-y-6">
                  {profile.summary && (
                    <div>
                      <h3 className="text-lg font-medium text-foreground mb-2">About</h3>
                      <p className="text-muted-foreground">{profile.summary}</p>
                    </div>
                  )}

                  <div>
                    <h3 className="text-lg font-medium text-foreground mb-3">Details</h3>
                    <dl className="space-y-2">
                      {profile.location && (
                        <div>
                          <dt className="text-sm font-medium text-muted-foreground">Location</dt>
                          <dd className="text-sm text-foreground">{profile.location}</dd>
                        </div>
                      )}
                      {profile.timeZone && (
                        <div>
                          <dt className="text-sm font-medium text-muted-foreground">Time Zone</dt>
                          <dd className="text-sm text-foreground">{profile.timeZone}</dd>
                        </div>
                      )}
                      <div>
                        <dt className="text-sm font-medium text-muted-foreground">Profile Visibility</dt>
                        <dd className="text-sm text-foreground">
                          {profile.isPublic ? 'Public (visible to other users)' : 'Private'}
                        </dd>
                      </div>
                    </dl>
                  </div>
                </div>

                {/* Right Column */}
                <div className="space-y-6">
                  {(profile.websiteUrl || profile.linkedInUrl || profile.gitHubUrl) && (
                    <div>
                      <h3 className="text-lg font-medium text-foreground mb-3">Links</h3>
                      <div className="space-y-2">
                        {profile.websiteUrl && (
                          <a
                            href={getSafeHref(profile.websiteUrl)}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="flex items-center text-primary hover:text-primary/80"
                          >
                            <span className="mr-2">🌐</span>
                            Website
                          </a>
                        )}
                        {profile.linkedInUrl && (
                          <a
                            href={getSafeHref(profile.linkedInUrl)}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="flex items-center text-primary hover:text-primary/80"
                          >
                            <Linkedin className="w-4 h-4 mr-2" />
                            LinkedIn
                          </a>
                        )}
                        {profile.gitHubUrl && (
                          <a
                            href={getSafeHref(profile.gitHubUrl)}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="flex items-center text-primary hover:text-primary/80"
                          >
                            <span className="mr-2">💻</span>
                            GitHub
                          </a>
                        )}
                      </div>
                    </div>
                  )}

                  <div>
                    <h3 className="text-lg font-medium text-foreground mb-3">Profile Stats</h3>
                    <dl className="space-y-2">
                      <div>
                        <dt className="text-sm font-medium text-muted-foreground">Member Since</dt>
                        <dd className="text-sm text-foreground">
                          {new Date(profile.createdAt).toLocaleDateString()}
                        </dd>
                      </div>
                      <div>
                        <dt className="text-sm font-medium text-muted-foreground">Last Updated</dt>
                        <dd className="text-sm text-foreground">
                          {new Date(profile.updatedAt).toLocaleDateString()}
                        </dd>
                      </div>
                    </dl>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
