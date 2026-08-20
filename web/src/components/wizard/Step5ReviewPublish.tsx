'use client'

import React from 'react'
import Image from 'next/image'
import { ProfileOnboardingData } from '@/types/profile'
import { getSafeHref } from '@/lib/urlValidation' // VULN-009 FIX

interface Step5ReviewPublishProps {
  profileData: ProfileOnboardingData
  isPublic: boolean
  onUpdateIsPublic: (isPublic: boolean) => void
  onBack: () => void
  onComplete: () => Promise<void>
  isLoading: boolean
}

export default function Step5ReviewPublish({
  profileData,
  isPublic,
  onUpdateIsPublic,
  onBack,
  onComplete,
  isLoading,
}: Step5ReviewPublishProps) {
  const { basicInfo, skills, experiences, photo } = profileData

  const formatDate = (dateString?: string) => {
    if (!dateString) return ''
    const date = new Date(dateString)
    return date.toLocaleDateString('en-US', { year: 'numeric', month: 'short' })
  }

  return (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-foreground">Review Your Profile</h2>
        <p className="text-muted-foreground mt-2">
          Please review all the information before publishing your profile
        </p>
      </div>

      <div className="space-y-6">
        {/* Profile Photo */}
        {photo?.avatarUrl && (
          <div className="flex justify-center pb-6 border-b border-border">
            <div className="relative w-32 h-32 rounded-full overflow-hidden border-4 border-border">
              <Image
                src={photo.avatarUrl}
                alt="Profile"
                fill
                className="object-cover"
                sizes="128px"
              />
            </div>
          </div>
        )}

        {/* Basic Information */}
        <div className="bg-muted rounded-lg p-4">
          <h3 className="text-lg font-medium text-foreground mb-4">Basic Information</h3>
          <dl className="grid grid-cols-1 gap-4">
            <div>
              <dt className="text-sm font-medium text-muted-foreground">Name</dt>
              <dd className="mt-1 text-sm text-foreground">
                {basicInfo.firstName} {basicInfo.lastName}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-muted-foreground">Title</dt>
              <dd className="mt-1 text-sm text-foreground">{basicInfo.title}</dd>
            </div>
            {basicInfo.company && (
              <div>
                <dt className="text-sm font-medium text-muted-foreground">Company</dt>
                <dd className="mt-1 text-sm text-foreground">{basicInfo.company}</dd>
              </div>
            )}
            {basicInfo.location && (
              <div>
                <dt className="text-sm font-medium text-muted-foreground">Location</dt>
                <dd className="mt-1 text-sm text-foreground">{basicInfo.location}</dd>
              </div>
            )}
            {basicInfo.timeZone && (
              <div>
                <dt className="text-sm font-medium text-muted-foreground">Time Zone</dt>
                <dd className="mt-1 text-sm text-foreground">{basicInfo.timeZone}</dd>
              </div>
            )}
            {basicInfo.summary && (
              <div>
                <dt className="text-sm font-medium text-muted-foreground">Summary</dt>
                <dd className="mt-1 text-sm text-foreground">{basicInfo.summary}</dd>
              </div>
            )}
          </dl>

          {/* Social Links */}
          {(basicInfo.websiteUrl || basicInfo.linkedInUrl || basicInfo.gitHubUrl) && (
            <div className="mt-4 pt-4 border-t border-border">
              <h4 className="text-sm font-medium text-foreground mb-2">Social Links</h4>
              <div className="space-y-2">
                {basicInfo.websiteUrl && (
                  <div>
                    <span className="text-sm text-muted-foreground">Website: </span>
                    <a
                      href={getSafeHref(basicInfo.websiteUrl)}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-sm text-primary hover:underline"
                    >
                      {basicInfo.websiteUrl}
                    </a>
                  </div>
                )}
                {basicInfo.linkedInUrl && (
                  <div>
                    <span className="text-sm text-muted-foreground">LinkedIn: </span>
                    <a
                      href={getSafeHref(basicInfo.linkedInUrl)}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-sm text-primary hover:underline"
                    >
                      {basicInfo.linkedInUrl}
                    </a>
                  </div>
                )}
                {basicInfo.gitHubUrl && (
                  <div>
                    <span className="text-sm text-muted-foreground">GitHub: </span>
                    <a
                      href={getSafeHref(basicInfo.gitHubUrl)}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-sm text-primary hover:underline"
                    >
                      {basicInfo.gitHubUrl}
                    </a>
                  </div>
                )}
              </div>
            </div>
          )}
        </div>

        {/* Skills */}
        {skills && skills.length > 0 && (
          <div className="bg-muted rounded-lg p-4">
            <h3 className="text-lg font-medium text-foreground mb-4">
              Skills ({skills.length})
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              {skills.map((skill, index) => (
                <div
                  key={skill.id || index}
                  className="p-3 bg-card border border-border rounded-lg"
                >
                  <h4 className="font-medium text-foreground">{skill.name}</h4>
                  <div className="flex items-center space-x-3 mt-1">
                    <span className="text-sm text-muted-foreground">{skill.proficiencyLevel}</span>
                    {skill.yearsOfExperience && skill.yearsOfExperience > 0 && (
                      <span className="text-sm text-muted-foreground">
                        {skill.yearsOfExperience} {skill.yearsOfExperience === 1 ? 'year' : 'years'}
                      </span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Experience */}
        {experiences && experiences.length > 0 && (
          <div className="bg-muted rounded-lg p-4">
            <h3 className="text-lg font-medium text-foreground mb-4">
              Experience ({experiences.length})
            </h3>
            <div className="space-y-3">
              {experiences
                .sort((a, b) => {
                  return new Date(b.startDate).getTime() - new Date(a.startDate).getTime()
                })
                .map((exp, index) => (
                  <div
                    key={exp.id || index}
                    className="p-3 bg-card border border-border rounded-lg"
                  >
                    <div className="flex items-center space-x-2">
                      <h4 className="font-medium text-foreground">{exp.title}</h4>
                      <span
                        className={`px-2 py-1 text-xs rounded ${
                          exp.type === 'work'
                            ? 'bg-primary/10 text-primary'
                            : 'bg-success/10 text-success'
                        }`}
                      >
                        {exp.type === 'work' ? 'Work' : 'Education'}
                      </span>
                    </div>
                    <p className="text-sm text-foreground mt-1">{exp.organization}</p>
                    {exp.location && (
                      <p className="text-sm text-muted-foreground mt-1">{exp.location}</p>
                    )}
                    <p className="text-sm text-muted-foreground mt-1">
                      {formatDate(exp.startDate)} -{' '}
                      {exp.isCurrent ? 'Present' : exp.endDate ? formatDate(exp.endDate) : 'N/A'}
                    </p>
                    {exp.description && (
                      <p className="text-sm text-foreground mt-2">{exp.description}</p>
                    )}
                  </div>
                ))}
            </div>
          </div>
        )}

        {/* Privacy Settings */}
        <div className="bg-muted rounded-lg p-4">
          <h3 className="text-lg font-medium text-foreground mb-4">Privacy Settings</h3>
          <div className="flex items-start">
            <input
              type="checkbox"
              id="isPublic"
              checked={isPublic}
              onChange={(e) => onUpdateIsPublic(e.target.checked)}
              className="h-4 w-4 text-primary focus:ring-ring border-input rounded mt-1"
            />
            <label htmlFor="isPublic" className="ml-3 block">
              <span className="text-sm font-medium text-foreground">
                Make my profile public
              </span>
              <p className="text-sm text-muted-foreground mt-1">
                When enabled, other users can find and view your profile for potential
                collaboration opportunities.
              </p>
            </label>
          </div>
        </div>

        {/* Profile Completeness */}
        <div className="bg-success/10 border border-success/20 rounded-md p-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <span className="text-success text-xl">✓</span>
            </div>
            <div className="ml-3">
              <h3 className="text-sm font-medium text-success">Profile Ready!</h3>
              <p className="text-sm text-success mt-1">
                Your profile is complete with all the essential information. You can always update
                it later from your profile settings.
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Navigation Buttons */}
      <div className="flex justify-between pt-6 border-t border-border mt-6">
        <button
          type="button"
          onClick={onBack}
          disabled={isLoading}
          className="px-6 py-2 border border-input text-foreground rounded-full hover:bg-muted focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Back
        </button>
        <button
          type="button"
          onClick={onComplete}
          disabled={isLoading}
          className="px-6 py-2 bg-success text-success-foreground rounded-full hover:bg-success/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isLoading ? (
            <>
              <svg
                className="animate-spin -ml-1 mr-3 h-5 w-5 inline"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="4"
                ></circle>
                <path
                  className="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                ></path>
              </svg>
              Publishing...
            </>
          ) : (
            'Publish Profile'
          )}
        </button>
      </div>
    </div>
  )
}
