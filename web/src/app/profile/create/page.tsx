'use client'

import { logger } from '@/utils/logger';

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import ProfileOnboardingWizard from '@/components/ProfileOnboardingWizard'
import { useAuth } from '@/contexts/AuthContext'
import { ProfileOnboardingData } from '@/types/profile'

export default function CreateProfilePage() {
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)
  const router = useRouter()
  const { isAuthenticated } = useAuth()

  const handleCreateProfile = async (data: ProfileOnboardingData) => {
    try {
      setIsLoading(true)
      setError(null)

      if (!isAuthenticated) {
        setError('You must be logged in to create a profile')
        return
      }

      // Get CSRF token
      const csrfResponse = await fetch('/api/auth/csrf-token')
      const csrfData = await csrfResponse.json()

      // BUG-PROFILE-002 FIX: Check if profile already exists
      const existingProfileResponse = await fetch('/api/profile/me', {
        credentials: 'include',
      })
      const hasExistingProfile = existingProfileResponse.ok

      // Step 1: Create or update basic profile
      const profileMethod = hasExistingProfile ? 'PUT' : 'POST'
      const profileResponse = await fetch('/api/profile', {
        method: profileMethod,
        headers: {
          'Content-Type': 'application/json',
          'X-CSRF-TOKEN': csrfData.token,
        },
        credentials: 'include',
        body: JSON.stringify({
          ...data.basicInfo,
          isPublic: data.isPublic,
        }),
      })

      const profileResult = await profileResponse.json()

      if (!profileResponse.ok || !profileResult.success) {
        setError(profileResult.message || `Failed to ${hasExistingProfile ? 'update' : 'create'} profile`)
        return
      }

      // Step 2: Add skills (if any)
      if (data.skills && data.skills.length > 0) {
        // Map proficiency level strings to enum values (matches backend SkillProficiency enum)
        const proficiencyToEnum: Record<string, number> = {
          'Beginner': 1,
          'Intermediate': 2,
          'Advanced': 3,
          'Expert': 4,
        }

        for (const skill of data.skills) {
          try {
            // Search for the skill by name to get its ID
            const searchResponse = await fetch(`/api/skill?searchTerm=${encodeURIComponent(skill.name)}&take=10`, {
              credentials: 'include',
            })

            if (!searchResponse.ok) {
              logger.error('Failed to search for skill', { skillName: skill.name })
              continue
            }

            const searchResult = await searchResponse.json()

            // Find exact match (case-insensitive) or use first result
            let skillId: string | null = null
            if (searchResult.skills && searchResult.skills.length > 0) {
              const exactMatch = searchResult.skills.find(
                (s: { name: string }) => s.name.toLowerCase() === skill.name.toLowerCase()
              )
              skillId = exactMatch?.id || searchResult.skills[0]?.id
            }

            if (!skillId) {
              // If skill not found, try to create it
              const createSkillResponse = await fetch('/api/skill', {
                method: 'POST',
                headers: {
                  'Content-Type': 'application/json',
                  'X-CSRF-TOKEN': csrfData.token,
                },
                credentials: 'include',
                body: JSON.stringify({
                  name: skill.name,
                  category: 'General', // Default category
                  description: `User-added skill: ${skill.name}`,
                }),
              })

              if (createSkillResponse.ok) {
                const createResult = await createSkillResponse.json()
                skillId = createResult.data?.id || createResult.id
              }
            }

            if (!skillId) {
              logger.error('Could not find or create skill', { skillName: skill.name })
              continue
            }

            // Now add the user skill with the correct DTO format
            const proficiencyValue = proficiencyToEnum[skill.proficiencyLevel] || 2 // Default to Intermediate

            await fetch('/api/skill/my-skills', {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': csrfData.token,
              },
              credentials: 'include',
              body: JSON.stringify({
                skillId: skillId,
                proficiency: proficiencyValue,
                yearsOfExperience: skill.yearsOfExperience || 0,
                notes: '',
                isFeatured: false,
                isVisible: true,
              }),
            })
          } catch (skillError) {
            logger.error('Error adding skill', skillError, { skillName: skill.name })
            // Continue with other skills even if one fails
          }
        }
      }

      // Step 3: Add experiences (if any)
      if (data.experiences && data.experiences.length > 0) {
        for (const exp of data.experiences) {
          await fetch('/api/experience', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              'X-CSRF-TOKEN': csrfData.token,
            },
            credentials: 'include',
            body: JSON.stringify({
              type: exp.type,
              title: exp.title,
              organization: exp.organization,
              location: exp.location,
              startDate: exp.startDate,
              endDate: exp.endDate,
              isCurrent: exp.isCurrent,
              description: exp.description,
            }),
          })
        }
      }

      // Step 4: Upload photo (if provided)
      if (data.photo && data.photo.file) {
        const formData = new FormData()
        formData.append('avatar', data.photo.file)

        await fetch('/api/profile/avatar', {
          method: 'PUT',
          headers: {
            'X-CSRF-TOKEN': csrfData.token,
          },
          credentials: 'include',
          body: formData,
        })
      }

      setSuccess(true)
      setTimeout(() => {
        router.push('/profile/me')
      }, 2000)
    } catch (err) {
      setError('An unexpected error occurred')
      logger.error('Profile creation error:', err)
    } finally {
      setIsLoading(false)
    }
  }

  if (success) {
    return (
      <div className="min-h-screen bg-muted flex items-center justify-center">
        <div className="max-w-md w-full bg-card rounded-lg shadow-md p-6">
          <div className="text-center">
            <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-success/10 mb-4">
              <span className="text-success text-xl">✓</span>
            </div>
            <h2 className="text-xl font-bold text-foreground mb-2">Profile Created!</h2>
            <p className="text-muted-foreground">
              Your profile has been created successfully. Redirecting to your profile page...
            </p>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div>
      {error && (
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 pt-8">
          <div className="mb-6 bg-destructive/10 border border-destructive/20 rounded-md p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <span className="text-destructive">✗</span>
              </div>
              <div className="ml-3">
                <p className="text-sm text-destructive">{error}</p>
              </div>
            </div>
          </div>
        </div>
      )}

      <ProfileOnboardingWizard
        onComplete={handleCreateProfile}
        isLoading={isLoading}
      />
    </div>
  )
}