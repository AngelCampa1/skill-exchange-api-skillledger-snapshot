// Profile Onboarding Wizard Types

export interface BasicInfo {
  firstName: string
  lastName: string
  title: string
  company?: string
  location?: string
  timeZone?: string
  summary?: string
  websiteUrl?: string
  linkedInUrl?: string
  gitHubUrl?: string
}

export interface Skill {
  id?: string
  name: string
  proficiencyLevel: 'Beginner' | 'Intermediate' | 'Advanced' | 'Expert'
  yearsOfExperience?: number
  category?: string
}

export interface Experience {
  id?: string
  type: 'work' | 'education'
  title: string
  organization: string
  location?: string
  startDate: string
  endDate?: string
  isCurrent: boolean
  description?: string
}

export interface PhotoUpload {
  avatarUrl?: string
  file?: File
}

export interface ProfileOnboardingData {
  basicInfo: BasicInfo
  skills: Skill[]
  experiences: Experience[]
  photo: PhotoUpload
  isPublic: boolean
}

export interface WizardStep {
  number: number
  title: string
  description: string
  isComplete: boolean
}

export const WIZARD_STEPS: WizardStep[] = [
  {
    number: 1,
    title: 'Basic Information',
    description: 'Tell us about yourself',
    isComplete: false,
  },
  {
    number: 2,
    title: 'Skills',
    description: 'What can you offer?',
    isComplete: false,
  },
  {
    number: 3,
    title: 'Experience',
    description: 'Your professional journey',
    isComplete: false,
  },
  {
    number: 4,
    title: 'Photo',
    description: 'Add a profile picture',
    isComplete: false,
  },
  {
    number: 5,
    title: 'Review',
    description: 'Review and publish',
    isComplete: false,
  },
]

export const STORAGE_KEY = 'skillledger_profile_draft'

export interface ProfileDraft {
  data: Partial<ProfileOnboardingData>
  currentStep: number
  lastSaved: string
}
