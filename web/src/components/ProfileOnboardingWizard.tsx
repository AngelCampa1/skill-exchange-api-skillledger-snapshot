'use client'

import { logger } from '@/utils/logger';
import { trackEvent } from '@/utils/analytics';

import React, { useState, useEffect, useCallback, useRef } from 'react'
import {
  ProfileOnboardingData,
  ProfileDraft,
  WIZARD_STEPS,
  STORAGE_KEY,
  BasicInfo,
  Skill,
  Experience,
  PhotoUpload
} from '@/types/profile'
import Step1BasicInfo from './wizard/Step1BasicInfo'
import Step2SkillSelection from './wizard/Step2SkillSelection'
import Step3ExperienceTimeline from './wizard/Step3ExperienceTimeline'
import Step4PhotoUpload from './wizard/Step4PhotoUpload'
import Step5ReviewPublish from './wizard/Step5ReviewPublish'

interface ProfileOnboardingWizardProps {
  onComplete: (data: ProfileOnboardingData) => Promise<void>
  isLoading?: boolean
}

const AUTOSAVE_INTERVAL = 30000 // 30 seconds
// BUG-LOW-020 FIX: Add idle detection to prevent unnecessary auto-saves
const IDLE_TIMEOUT = 5 * 60 * 1000 // 5 minutes of inactivity

// BUG-TEST-031 FIX: minimum skills required (single source of truth, also used in handleComplete)
const MIN_SKILLS_REQUIRED = 3

/**
 * Returns the highest step number a user may legitimately be on given the draft data.
 * Clamps requestedStep so that no required-prerequisite step is bypassed.
 *
 * Rules:
 *   - Step 1 prerequisite: basicInfo.firstName, lastName, and title are all non-empty (trimmed).
 *   - Step 2 prerequisite: skills.length >= MIN_SKILLS_REQUIRED.
 *   - Steps 3, 4, 5 have no prerequisites beyond step 2.
 *
 * Never returns less than 1 or more than WIZARD_STEPS.length.
 */
function getSafeStep(data: Partial<ProfileOnboardingData>, requestedStep: number): number {
  const { basicInfo, skills } = data

  // Defensive: a corrupt/partial draft may have basicInfo present but with
  // missing or non-string fields. Guard with typeof so we clamp to step 1
  // (and still restore whatever data exists) rather than throwing.
  const step1Complete =
    basicInfo !== undefined &&
    typeof basicInfo.firstName === 'string' &&
    typeof basicInfo.lastName === 'string' &&
    typeof basicInfo.title === 'string' &&
    basicInfo.firstName.trim().length > 0 &&
    basicInfo.lastName.trim().length > 0 &&
    basicInfo.title.trim().length > 0

  if (!step1Complete) {
    return 1
  }

  const step2Complete = Array.isArray(skills) && skills.length >= MIN_SKILLS_REQUIRED

  if (!step2Complete) {
    return Math.min(2, Math.max(1, requestedStep))
  }

  return Math.min(Math.max(1, requestedStep), WIZARD_STEPS.length)
}

export default function ProfileOnboardingWizard({
  onComplete,
  isLoading = false
}: ProfileOnboardingWizardProps) {
  const [currentStep, setCurrentStep] = useState(1)
  const [profileData, setProfileData] = useState<Partial<ProfileOnboardingData>>({
    basicInfo: {
      firstName: '',
      lastName: '',
      title: '',
    },
    skills: [],
    experiences: [],
    photo: {},
    isPublic: false,
  })
  const [completedSteps, setCompletedSteps] = useState<Set<number>>(new Set())
  const [lastSaved, setLastSaved] = useState<string | null>(null)
  // BUG-TEST-030 FIX: surface when another browser tab saves a newer draft so
  // we never silently clobber it (last-write-wins data loss).
  const [externalDraftConflict, setExternalDraftConflict] = useState(false)

  // BUG-LOW-020 FIX: Track user activity for idle detection
  const lastActivityRef = useRef<number>(Date.now())
  // BUG-TEST-030 FIX: ISO timestamp of the draft this tab most recently
  // persisted or loaded. Used to detect foreign writes from other tabs.
  const lastPersistedAtRef = useRef<string | null>(null)

  // Apply a persisted draft to component state, clamping the restored step so
  // required prerequisites are never bypassed (see getSafeStep). Records the
  // draft's timestamp so later saves can detect foreign writes.
  const applyDraft = useCallback((draft: ProfileDraft) => {
    const safeStep = getSafeStep(draft.data, draft.currentStep)

    // Rebuild completedSteps: only steps strictly before safeStep whose rule is met.
    // Step 1 complete when safeStep > 1 (getSafeStep already verified it).
    // Step 2 complete when safeStep > 2 (getSafeStep already verified it).
    // Steps 3 and 4 are optional — mark complete for any step > them when safeStep allows.
    const rebuilt = new Set<number>()
    for (let s = 1; s < safeStep; s++) {
      rebuilt.add(s)
    }

    setProfileData(draft.data)
    setCurrentStep(safeStep)
    setCompletedSteps(rebuilt)
    setLastSaved(draft.lastSaved)
    lastPersistedAtRef.current = draft.lastSaved
  }, [])

  // Load saved draft on mount
  useEffect(() => {
    if (typeof window !== 'undefined') {
      const savedDraft = localStorage.getItem(STORAGE_KEY)
      if (savedDraft) {
        try {
          const draft: ProfileDraft = JSON.parse(savedDraft)
          applyDraft(draft)
        } catch (error) {
          logger.error('Failed to load saved draft:', error)
        }
      }
    }
  }, [applyDraft])

  // Auto-save functionality
  const saveDraft = useCallback(() => {
    if (typeof window === 'undefined') {
      return
    }

    // BUG-TEST-030 FIX: don't silently overwrite a draft that another tab saved
    // after we last synced. Detect the conflict, adopt its timestamp so we stop
    // fighting it, and surface it to the user instead of destroying their work.
    const existingRaw = localStorage.getItem(STORAGE_KEY)
    if (existingRaw) {
      try {
        const existing = JSON.parse(existingRaw) as ProfileDraft
        const lastSeen = lastPersistedAtRef.current
        if (existing.lastSaved && lastSeen && existing.lastSaved > lastSeen) {
          lastPersistedAtRef.current = existing.lastSaved
          setExternalDraftConflict(true)
          return
        }
      } catch {
        // Corrupt existing draft: fall through and replace it with valid data.
      }
    }

    const draft: ProfileDraft = {
      data: profileData,
      currentStep,
      lastSaved: new Date().toISOString(),
    }
    localStorage.setItem(STORAGE_KEY, JSON.stringify(draft))
    lastPersistedAtRef.current = draft.lastSaved
    setLastSaved(draft.lastSaved)
  }, [profileData, currentStep])

  // BUG-LOW-020 FIX: Track user activity
  useEffect(() => {
    const updateActivity = () => {
      lastActivityRef.current = Date.now()
    }

    // Listen for user interactions
    const events = ['mousedown', 'keydown', 'scroll', 'touchstart']
    events.forEach(event => {
      window.addEventListener(event, updateActivity, { passive: true })
    })

    return () => {
      events.forEach(event => {
        window.removeEventListener(event, updateActivity)
      })
    }
  }, [])

  // Auto-save every 30 seconds (only if user is active)
  useEffect(() => {
    const interval = setInterval(() => {
      // BUG-LOW-020 FIX: Only auto-save if user has been active recently
      const timeSinceActivity = Date.now() - lastActivityRef.current
      if (timeSinceActivity < IDLE_TIMEOUT) {
        saveDraft()
      } else {
        logger.debug('Skipping auto-save due to user inactivity', {
          component: 'ProfileOnboardingWizard',
          idleTime: Math.floor(timeSinceActivity / 1000)
        })
      }
    }, AUTOSAVE_INTERVAL)

    return () => clearInterval(interval)
  }, [saveDraft])

  // BUG-TEST-030 FIX: react to drafts written by other tabs in real time.
  // The `storage` event only fires in tabs other than the one that wrote it,
  // so this is how a passive tab learns its draft is now stale.
  useEffect(() => {
    if (typeof window === 'undefined') {
      return
    }
    const onStorage = (event: StorageEvent) => {
      if (event.key !== STORAGE_KEY || event.newValue === null) {
        return
      }
      try {
        const incoming = JSON.parse(event.newValue) as ProfileDraft
        const lastSeen = lastPersistedAtRef.current
        if (incoming.lastSaved && (!lastSeen || incoming.lastSaved > lastSeen)) {
          lastPersistedAtRef.current = incoming.lastSaved
          setExternalDraftConflict(true)
        }
      } catch {
        // Ignore corrupt foreign writes.
      }
    }
    window.addEventListener('storage', onStorage)
    return () => window.removeEventListener('storage', onStorage)
  }, [])

  // BUG-TEST-030 FIX: load the newer draft another tab saved.
  const loadExternalDraft = useCallback(() => {
    if (typeof window !== 'undefined') {
      const savedDraft = localStorage.getItem(STORAGE_KEY)
      if (savedDraft) {
        try {
          applyDraft(JSON.parse(savedDraft) as ProfileDraft)
        } catch (error) {
          logger.error('Failed to load external draft:', error)
        }
      }
    }
    setExternalDraftConflict(false)
  }, [applyDraft])

  // BUG-TEST-030 FIX: keep this tab's version. lastPersistedAtRef is already
  // synced to the other tab's timestamp, so the next save overwrites it.
  const dismissDraftConflict = useCallback(() => {
    setExternalDraftConflict(false)
  }, [])

  const handleNext = () => {
    if (currentStep < WIZARD_STEPS.length) {
      // Track wizard step completion
      trackEvent({
        name: 'profile_wizard_step',
        category: 'profile',
        priority: 'high',
        properties: {
          step_number: currentStep,
          step_name: WIZARD_STEPS[currentStep - 1]?.title || `Step ${currentStep}`,
          total_steps: WIZARD_STEPS.length,
        },
      })

      setCompletedSteps(prev => {
        const newSet = new Set(prev)
        newSet.add(currentStep)
        return newSet
      })
      setCurrentStep(prev => prev + 1)
      saveDraft()
    }
  }

  const handleBack = () => {
    if (currentStep > 1) {
      setCurrentStep(prev => prev - 1)
    }
  }

  const handleStepClick = (stepNumber: number) => {
    // Allow navigation to completed steps or the next step after the last completed
    if (stepNumber <= currentStep || completedSteps.has(stepNumber - 1)) {
      setCurrentStep(stepNumber)
    }
  }

  const updateBasicInfo = (data: BasicInfo) => {
    setProfileData(prev => ({
      ...prev,
      basicInfo: data,
    }))
  }

  const updateSkills = (skills: Skill[]) => {
    setProfileData(prev => ({
      ...prev,
      skills,
    }))
  }

  const updateExperiences = (experiences: Experience[]) => {
    setProfileData(prev => ({
      ...prev,
      experiences,
    }))
  }

  const updatePhoto = (photo: PhotoUpload) => {
    setProfileData(prev => ({
      ...prev,
      photo,
    }))
  }

  const updateIsPublic = (isPublic: boolean) => {
    setProfileData(prev => ({
      ...prev,
      isPublic,
    }))
  }

  const handleComplete = async () => {
    // BUG-005 FIX: Validate minimum skills requirement before submission
    if (!profileData.skills || profileData.skills.length < MIN_SKILLS_REQUIRED) {
      alert(`Please add at least ${MIN_SKILLS_REQUIRED} skills to complete your profile.`)
      setCurrentStep(2) // Navigate back to skills step
      return
    }

    // Track profile completion
    trackEvent({
      name: 'profile_published',
      category: 'profile',
      priority: 'critical',
      properties: {
        is_public: profileData.isPublic || false,
        skills_count: profileData.skills?.length || 0,
        experiences_count: profileData.experiences?.length || 0,
        has_photo: !!(profileData.photo?.avatarUrl || profileData.photo?.file),
      },
    })

    // Clear the saved draft
    if (typeof window !== 'undefined') {
      localStorage.removeItem(STORAGE_KEY)
    }

    // Submit the complete profile
    await onComplete(profileData as ProfileOnboardingData)
  }

  const clearDraft = () => {
    if (typeof window !== 'undefined' && confirm('Are you sure you want to clear your saved draft?')) {
      localStorage.removeItem(STORAGE_KEY)
      setProfileData({
        basicInfo: {
          firstName: '',
          lastName: '',
          title: '',
        },
        skills: [],
        experiences: [],
        photo: {},
        isPublic: false,
      })
      setCurrentStep(1)
      setCompletedSteps(new Set())
      setLastSaved(null)
    }
  }

  const renderStep = () => {
    switch (currentStep) {
      case 1:
        return (
          <Step1BasicInfo
            data={profileData.basicInfo!}
            onUpdate={updateBasicInfo}
            onNext={handleNext}
          />
        )
      case 2:
        // BUG-005 FIX: Pass minSkills={3} to enforce minimum skill requirement
        return (
          <Step2SkillSelection
            skills={profileData.skills || []}
            onUpdate={updateSkills}
            onNext={handleNext}
            onBack={handleBack}
            minSkills={3}
          />
        )
      case 3:
        return (
          <Step3ExperienceTimeline
            experiences={profileData.experiences || []}
            onUpdate={updateExperiences}
            onNext={handleNext}
            onBack={handleBack}
          />
        )
      case 4:
        return (
          <Step4PhotoUpload
            photo={profileData.photo || {}}
            onUpdate={updatePhoto}
            onNext={handleNext}
            onBack={handleBack}
          />
        )
      case 5:
        return (
          <Step5ReviewPublish
            profileData={profileData as ProfileOnboardingData}
            isPublic={profileData.isPublic || false}
            onUpdateIsPublic={updateIsPublic}
            onBack={handleBack}
            onComplete={handleComplete}
            isLoading={isLoading}
          />
        )
      default:
        return null
    }
  }

  return (
    <div className="min-h-screen bg-muted py-8">
      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-foreground">Create Your Profile</h1>
          <p className="mt-2 text-muted-foreground">
            Complete your profile in 5 easy steps to get started on SkillLedger
          </p>
        </div>

        {/* Progress Indicator */}
        <div className="mb-8">
          <div className="flex items-center justify-between">
            {WIZARD_STEPS.map((step, index) => (
              <React.Fragment key={step.number}>
                <div className="flex flex-col items-center flex-1">
                  <button
                    onClick={() => handleStepClick(step.number)}
                    disabled={step.number > currentStep && !completedSteps.has(step.number - 1)}
                    className={`
                      w-10 h-10 rounded-full flex items-center justify-center font-semibold text-sm
                      transition-colors duration-200
                      ${
                        currentStep === step.number
                          ? 'bg-primary text-primary-foreground'
                          : completedSteps.has(step.number)
                          ? 'bg-success text-success-foreground'
                          : 'bg-muted text-muted-foreground'
                      }
                      ${
                        step.number <= currentStep || completedSteps.has(step.number - 1)
                          ? 'cursor-pointer hover:opacity-80'
                          : 'cursor-not-allowed opacity-50'
                      }
                    `}
                  >
                    {completedSteps.has(step.number) ? '✓' : step.number}
                  </button>
                  <div className="mt-2 text-center">
                    <div className="text-xs font-medium text-foreground">{step.title}</div>
                    <div className="text-xs text-muted-foreground hidden sm:block">{step.description}</div>
                  </div>
                </div>
                {index < WIZARD_STEPS.length - 1 && (
                  <div
                    className={`
                      h-1 flex-1 mx-2 -mt-8 transition-colors duration-200
                      ${
                        completedSteps.has(step.number) || currentStep > step.number
                          ? 'bg-success'
                          : 'bg-muted'
                      }
                    `}
                  />
                )}
              </React.Fragment>
            ))}
          </div>
        </div>

        {/* BUG-TEST-030 FIX: multi-tab edit conflict notice */}
        {externalDraftConflict && (
          <div
            role="alert"
            className="mb-4 rounded-lg border border-warning bg-warning/10 p-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"
          >
            <p className="text-sm text-foreground">
              You changed this profile in another tab.
            </p>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={loadExternalDraft}
                className="rounded-full bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-opacity hover:opacity-80"
              >
                Load the new changes
              </button>
              <button
                type="button"
                onClick={dismissDraftConflict}
                className="rounded-full border border-input px-4 py-2 text-sm font-medium text-foreground transition-colors hover:bg-muted"
              >
                Keep this version
              </button>
            </div>
          </div>
        )}

        {/* Auto-save indicator */}
        {lastSaved && (
          <div className="mb-4 flex items-center justify-between">
            <p className="text-sm text-muted-foreground">
              Last saved: {new Date(lastSaved).toLocaleTimeString()}
            </p>
            <button
              onClick={clearDraft}
              className="text-sm text-destructive hover:text-destructive/80"
            >
              Clear draft
            </button>
          </div>
        )}

        {/* Step Content */}
        <div className="bg-card rounded-lg shadow-md p-6">
          {renderStep()}
        </div>

        {/* Save and Continue Later */}
        <div className="mt-4 text-center">
          <button
            onClick={() => {
              saveDraft()
              window.history.back()
            }}
            className="text-sm text-muted-foreground hover:text-foreground"
          >
            Save and continue later
          </button>
        </div>
      </div>
    </div>
  )
}
