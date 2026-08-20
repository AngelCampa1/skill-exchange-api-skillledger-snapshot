'use client'

import React, { useState, useEffect } from 'react'
import { Skill } from '@/types/profile'

interface Step2SkillSelectionProps {
  skills: Skill[]
  onUpdate: (skills: Skill[]) => void
  onNext: () => void
  onBack: () => void
  minSkills?: number
}

// BUG-005 FIX: Added minSkills prop for consistent validation
const MIN_SKILLS_DEFAULT = 1

export default function Step2SkillSelection({
  skills,
  onUpdate,
  onNext,
  onBack,
  minSkills = MIN_SKILLS_DEFAULT,
}: Step2SkillSelectionProps) {
  const [currentSkills, setCurrentSkills] = useState<Skill[]>(skills)
  const [newSkill, setNewSkill] = useState<Partial<Skill>>({
    name: '',
    proficiencyLevel: 'Intermediate',
    yearsOfExperience: 0,
  })
  const [errors, setErrors] = useState<string | null>(null)

  // Sync local state with props when navigating back to this step
  useEffect(() => {
    setCurrentSkills(skills)
  }, [skills])

  const handleAddSkill = () => {
    if (!newSkill.name || newSkill.name.trim() === '') {
      setErrors('Skill name is required')
      return
    }

    const skill: Skill = {
      id: `skill-${Date.now()}`,
      name: newSkill.name,
      proficiencyLevel: newSkill.proficiencyLevel as Skill['proficiencyLevel'],
      yearsOfExperience: newSkill.yearsOfExperience || 0,
    }

    const updatedSkills = [...currentSkills, skill]
    setCurrentSkills(updatedSkills)
    setNewSkill({
      name: '',
      proficiencyLevel: 'Intermediate',
      yearsOfExperience: 0,
    })
    setErrors(null)
  }

  const handleRemoveSkill = (skillId: string) => {
    const updatedSkills = currentSkills.filter(s => s.id !== skillId)
    setCurrentSkills(updatedSkills)
  }

  // BUG-005 FIX: Validate minimum skills requirement
  const handleNext = () => {
    if (currentSkills.length < minSkills) {
      setErrors(`Please add at least ${minSkills} skill${minSkills > 1 ? 's' : ''}`)
      return
    }
    onUpdate(currentSkills)
    onNext()
  }

  return (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-foreground">Your Skills</h2>
        <p className="text-muted-foreground mt-2">
          Add the skills you can offer to other users on SkillLedger
        </p>
      </div>

      {/* Add Skill Form */}
      <div className="mb-6 p-4 bg-muted rounded-lg">
        <h3 className="text-lg font-medium text-foreground mb-4">Add a Skill</h3>

        <div className="space-y-4">
          <div>
            <label htmlFor="skillName" className="block text-sm font-medium text-foreground mb-1">
              Skill Name <span className="text-destructive">*</span>
            </label>
            <input
              type="text"
              id="skillName"
              value={newSkill.name}
              onChange={(e) => setNewSkill({ ...newSkill, name: e.target.value })}
              className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              placeholder="e.g., JavaScript, Project Management, Graphic Design"
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label
                htmlFor="proficiencyLevel"
                className="block text-sm font-medium text-foreground mb-1"
              >
                Proficiency Level
              </label>
              <select
                id="proficiencyLevel"
                value={newSkill.proficiencyLevel}
                onChange={(e) =>
                  setNewSkill({
                    ...newSkill,
                    proficiencyLevel: e.target.value as Skill['proficiencyLevel'],
                  })
                }
                className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              >
                <option value="Beginner">Beginner</option>
                <option value="Intermediate">Intermediate</option>
                <option value="Advanced">Advanced</option>
                <option value="Expert">Expert</option>
              </select>
            </div>

            <div>
              <label
                htmlFor="yearsOfExperience"
                className="block text-sm font-medium text-foreground mb-1"
              >
                Years of Experience
              </label>
              <input
                type="number"
                id="yearsOfExperience"
                min="0"
                max="50"
                value={newSkill.yearsOfExperience}
                onChange={(e) =>
                  setNewSkill({ ...newSkill, yearsOfExperience: parseInt(e.target.value) || 0 })
                }
                className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              />
            </div>
          </div>

          <button
            type="button"
            onClick={handleAddSkill}
            className="w-full px-4 py-2 bg-primary text-primary-foreground rounded-full hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
          >
            Add Skill
          </button>
        </div>
      </div>

      {/* Skills List */}
      {currentSkills.length > 0 && (
        <div className="mb-6">
          {/* BUG-005 FIX: Show progress toward minimum skills */}
          <h3 className="text-lg font-medium text-foreground mb-4">
            Your Skills ({currentSkills.length})
            {minSkills > 0 && (
              <span className={`ml-2 text-sm ${currentSkills.length >= minSkills ? 'text-success' : 'text-muted-foreground'}`}>
                {currentSkills.length >= minSkills ? '✓ Minimum met' : `(${minSkills} required)`}
              </span>
            )}
          </h3>
          <div className="space-y-3">
            {currentSkills.map((skill) => (
              <div
                key={skill.id}
                className="flex items-center justify-between p-4 bg-card border border-border rounded-lg"
              >
                <div className="flex-1">
                  <h4 className="font-medium text-foreground">{skill.name}</h4>
                  <div className="flex items-center space-x-4 mt-1">
                    <span className="text-sm text-muted-foreground">
                      Level: <span className="font-medium">{skill.proficiencyLevel}</span>
                    </span>
                    {skill.yearsOfExperience && skill.yearsOfExperience > 0 && (
                      <span className="text-sm text-muted-foreground">
                        {skill.yearsOfExperience} {skill.yearsOfExperience === 1 ? 'year' : 'years'}
                      </span>
                    )}
                  </div>
                </div>
                <button
                  type="button"
                  onClick={() => handleRemoveSkill(skill.id!)}
                  className="ml-4 text-destructive hover:text-destructive/80"
                >
                  Remove
                </button>
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
