'use client'

import React, { useState } from 'react'
import { Experience } from '@/types/profile'

interface Step3ExperienceTimelineProps {
  experiences: Experience[]
  onUpdate: (experiences: Experience[]) => void
  onNext: () => void
  onBack: () => void
}

export default function Step3ExperienceTimeline({
  experiences,
  onUpdate,
  onNext,
  onBack,
}: Step3ExperienceTimelineProps) {
  const [currentExperiences, setCurrentExperiences] = useState<Experience[]>(experiences)
  const [newExperience, setNewExperience] = useState<Partial<Experience>>({
    type: 'work',
    title: '',
    organization: '',
    location: '',
    startDate: '',
    endDate: '',
    isCurrent: false,
    description: '',
  })
  const [errors, setErrors] = useState<string | null>(null)

  const handleAddExperience = () => {
    if (!newExperience.title || newExperience.title.trim() === '') {
      setErrors('Title is required')
      return
    }
    if (!newExperience.organization || newExperience.organization.trim() === '') {
      setErrors('Organization is required')
      return
    }
    if (!newExperience.startDate) {
      setErrors('Start date is required')
      return
    }

    const experience: Experience = {
      id: `exp-${Date.now()}`,
      type: newExperience.type as Experience['type'],
      title: newExperience.title,
      organization: newExperience.organization,
      location: newExperience.location || '',
      startDate: newExperience.startDate,
      endDate: newExperience.isCurrent ? undefined : newExperience.endDate,
      isCurrent: newExperience.isCurrent || false,
      description: newExperience.description || '',
    }

    const updatedExperiences = [...currentExperiences, experience]
    setCurrentExperiences(updatedExperiences)
    setNewExperience({
      type: 'work',
      title: '',
      organization: '',
      location: '',
      startDate: '',
      endDate: '',
      isCurrent: false,
      description: '',
    })
    setErrors(null)
  }

  const handleRemoveExperience = (expId: string) => {
    const updatedExperiences = currentExperiences.filter(e => e.id !== expId)
    setCurrentExperiences(updatedExperiences)
  }

  const handleNext = () => {
    // Experience is optional, so allow moving forward even with 0 entries
    onUpdate(currentExperiences)
    onNext()
  }

  const formatDate = (dateString: string) => {
    if (!dateString) return ''
    const date = new Date(dateString)
    return date.toLocaleDateString('en-US', { year: 'numeric', month: 'short' })
  }

  return (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-foreground">Experience Timeline</h2>
        <p className="text-muted-foreground mt-2">
          Add your work experience and education history (optional)
        </p>
      </div>

      {/* Add Experience Form */}
      <div className="mb-6 p-4 bg-muted rounded-lg">
        <h3 className="text-lg font-medium text-foreground mb-4">Add Experience</h3>

        <div className="space-y-4">
          {/* Type Selection */}
          <div>
            <label className="block text-sm font-medium text-foreground mb-2">Type</label>
            <div className="flex space-x-4">
              <label className="flex items-center">
                <input
                  type="radio"
                  value="work"
                  checked={newExperience.type === 'work'}
                  onChange={(e) => setNewExperience({ ...newExperience, type: e.target.value as 'work' })}
                  className="h-4 w-4 text-primary focus:ring-ring border-input"
                />
                <span className="ml-2 text-sm text-foreground">Work Experience</span>
              </label>
              <label className="flex items-center">
                <input
                  type="radio"
                  value="education"
                  checked={newExperience.type === 'education'}
                  onChange={(e) => setNewExperience({ ...newExperience, type: e.target.value as 'education' })}
                  className="h-4 w-4 text-primary focus:ring-ring border-input"
                />
                <span className="ml-2 text-sm text-foreground">Education</span>
              </label>
            </div>
          </div>

          {/* Title */}
          <div>
            <label htmlFor="title" className="block text-sm font-medium text-foreground mb-1">
              {newExperience.type === 'work' ? 'Job Title' : 'Degree'}{' '}
              <span className="text-destructive">*</span>
            </label>
            <input
              type="text"
              id="title"
              value={newExperience.title}
              onChange={(e) => setNewExperience({ ...newExperience, title: e.target.value })}
              className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              placeholder={
                newExperience.type === 'work' ? 'e.g., Senior Developer' : 'e.g., Bachelor of Science'
              }
            />
          </div>

          {/* Organization */}
          <div>
            <label htmlFor="organization" className="block text-sm font-medium text-foreground mb-1">
              {newExperience.type === 'work' ? 'Company' : 'School'}{' '}
              <span className="text-destructive">*</span>
            </label>
            <input
              type="text"
              id="organization"
              value={newExperience.organization}
              onChange={(e) => setNewExperience({ ...newExperience, organization: e.target.value })}
              className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              placeholder={
                newExperience.type === 'work' ? 'e.g., Tech Corp' : 'e.g., University of Example'
              }
            />
          </div>

          {/* Location */}
          <div>
            <label htmlFor="location" className="block text-sm font-medium text-foreground mb-1">
              Location
            </label>
            <input
              type="text"
              id="location"
              value={newExperience.location}
              onChange={(e) => setNewExperience({ ...newExperience, location: e.target.value })}
              className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              placeholder="e.g., New York, NY"
            />
          </div>

          {/* Dates */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="startDate" className="block text-sm font-medium text-foreground mb-1">
                Start Date <span className="text-destructive">*</span>
              </label>
              <input
                type="month"
                id="startDate"
                value={newExperience.startDate}
                onChange={(e) => setNewExperience({ ...newExperience, startDate: e.target.value })}
                className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              />
            </div>

            <div>
              <label htmlFor="endDate" className="block text-sm font-medium text-foreground mb-1">
                End Date
              </label>
              <input
                type="month"
                id="endDate"
                value={newExperience.endDate}
                onChange={(e) => setNewExperience({ ...newExperience, endDate: e.target.value })}
                disabled={newExperience.isCurrent}
                className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring disabled:bg-muted disabled:cursor-not-allowed"
              />
            </div>
          </div>

          {/* Current Position */}
          <div className="flex items-center">
            <input
              type="checkbox"
              id="isCurrent"
              checked={newExperience.isCurrent}
              onChange={(e) =>
                setNewExperience({
                  ...newExperience,
                  isCurrent: e.target.checked,
                  endDate: e.target.checked ? '' : newExperience.endDate,
                })
              }
              className="h-4 w-4 text-primary focus:ring-ring border-input rounded"
            />
            <label htmlFor="isCurrent" className="ml-2 block text-sm text-foreground">
              I currently {newExperience.type === 'work' ? 'work' : 'study'} here
            </label>
          </div>

          {/* Description */}
          <div>
            <label htmlFor="description" className="block text-sm font-medium text-foreground mb-1">
              Description
            </label>
            <textarea
              id="description"
              rows={3}
              value={newExperience.description}
              onChange={(e) => setNewExperience({ ...newExperience, description: e.target.value })}
              className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              placeholder="Brief description of your responsibilities or achievements"
            />
          </div>

          <button
            type="button"
            onClick={handleAddExperience}
            className="w-full px-4 py-2 bg-primary text-primary-foreground rounded-full hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
          >
            Add Experience
          </button>
        </div>
      </div>

      {/* Experiences List */}
      {currentExperiences.length > 0 && (
        <div className="mb-6">
          <h3 className="text-lg font-medium text-foreground mb-4">
            Your Experience ({currentExperiences.length})
          </h3>
          <div className="space-y-4">
            {currentExperiences
              .sort((a, b) => {
                // Sort by start date, most recent first
                return new Date(b.startDate).getTime() - new Date(a.startDate).getTime()
              })
              .map((exp) => (
                <div
                  key={exp.id}
                  className="p-4 bg-card border border-border rounded-lg"
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
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
                    <button
                      type="button"
                      onClick={() => handleRemoveExperience(exp.id!)}
                      className="ml-4 text-destructive hover:text-destructive/80"
                    >
                      Remove
                    </button>
                  </div>
                </div>
              ))}
          </div>
        </div>
      )}

      {/* Error Message */}
      {errors && (
        <div className="mb-4 bg-destructive/10 border border-destructive/20 rounded-md p-4">
          <p className="text-sm text-destructive">{errors}</p>
        </div>
      )}

      {/* Navigation Buttons */}
      <div className="flex justify-between pt-6 border-t border-border">
        <button
          type="button"
          onClick={onBack}
          className="px-6 py-2 border border-input text-foreground rounded-full hover:bg-muted focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
        >
          Back
        </button>
        <button
          type="button"
          onClick={handleNext}
          className="px-6 py-2 bg-primary text-primary-foreground rounded-full hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
        >
          Next Step
        </button>
      </div>
    </div>
  )
}
