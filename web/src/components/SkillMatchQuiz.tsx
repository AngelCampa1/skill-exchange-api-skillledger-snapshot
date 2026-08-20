'use client'

import { useState } from'react'
import Link from'next/link'
import { ArrowRight, ArrowLeft, CheckCircle2 } from'lucide-react'
import { skillMatchQuestions, getSkillMatchResults, SkillMatchResults } from'@/lib/data/skill-match-data'
import { trackEvent } from'@/utils/analytics'

export default function SkillMatchQuiz() {
  const [currentStep, setCurrentStep] = useState(0)
  const [answers, setAnswers] = useState<Record<string, string>>({})
  const [results, setResults] = useState<SkillMatchResults | null>(null)

  const totalSteps = skillMatchQuestions.length
  const currentQuestion = skillMatchQuestions[currentStep]
  const isComplete = results !== null

  const handleSelect = (value: string) => {
    const questionId = currentQuestion.id
    const newAnswers = { ...answers, [questionId]: value }
    setAnswers(newAnswers)

    trackEvent({
      name:'skill_match_step_completed',
      category:'forms',
      priority:'medium',
      properties: {
        step: currentStep + 1,
        step_name: questionId,
        answer: value,
      },
    })

    if (currentStep < totalSteps - 1) {
      setCurrentStep(currentStep + 1)
    } else {
      // Final step — compute results
      const matchResults = getSkillMatchResults(
        newAnswers.profession ||'other',
        newAnswers.need ||'other'
      )
      setResults(matchResults)
      trackEvent({
        name:'skill_match_completed',
        category:'forms',
        priority:'high',
        properties: {
          profession: newAnswers.profession,
          need: newAnswers.need,
          experience: newAnswers.experience,
          matched_categories: matchResults.categories.length,
          matched_scenarios: matchResults.scenarios.length,
        },
      })
    }
  }

  const handleBack = () => {
    if (currentStep > 0) {
      setCurrentStep(currentStep - 1)
    }
  }

  // Track quiz start on first render
  useState(() => {
    trackEvent({
      name:'skill_match_started',
      category:'forms',
      priority:'medium',
    })
  })

  if (isComplete && results) {
    return (
      <div className="max-w-2xl mx-auto space-y-8">
        {/* Results Header */}
        <div className="text-center space-y-3">
          <div className="flex justify-center">
            <div className="p-3 bg-green-100  rounded-full">
              <CheckCircle2 className="w-8 h-8 text-green-600" />
            </div>
          </div>
          <h2 className="text-2xl font-bold text-foreground">Your Skill Matches</h2>
          <p className="text-muted-foreground">
            Based on your answers, here are the best categories and exchange scenarios for you.
          </p>
        </div>

        {/* Matched Categories */}
        <div className="space-y-4">
          <h3 className="text-lg font-semibold text-foreground">Recommended Categories</h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {results.categories.map((cat) => (
              <Link
                key={cat.slug}
                href={`/categories/${cat.slug}`}
                className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
              >
                <div className="flex items-start justify-between mb-2">
                  <h4 className="font-bold group-hover:text-primary transition-colors">{cat.name}</h4>
                  {cat.demandLevel ==='high' && (
                    <span className="text-xs px-2 py-0.5 rounded-full bg-green-100  text-green-700">
                      high demand
                    </span>
                  )}
                </div>
                <p className="text-sm text-muted-foreground line-clamp-2">{cat.description}</p>
              </Link>
            ))}
          </div>
        </div>

        {/* Matched Scenarios */}
        <div className="space-y-4">
          <h3 className="text-lg font-semibold text-foreground">How-To Exchange Guides</h3>
          <div className="grid grid-cols-1 gap-4">
            {results.scenarios.map((scenario) => (
              <Link
                key={scenario.slug}
                href={`/how-to/${scenario.slug}`}
                className="card-feature p-5 hover:shadow-lg transition-all duration-200 group"
              >
                <h4 className="font-bold group-hover:text-primary transition-colors mb-2">{scenario.title}</h4>
                <p className="text-sm text-muted-foreground line-clamp-2 mb-3">{scenario.description}</p>
                <div className="flex flex-wrap gap-1.5">
                  <span className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded">{scenario.skillOffered}</span>
                  <span className="text-xs text-muted-foreground">for</span>
                  <span className="text-xs bg-secondary/10 text-secondary px-2 py-0.5 rounded">{scenario.skillNeeded}</span>
                </div>
              </Link>
            ))}
          </div>
        </div>

        {/* CTAs */}
        <div className="space-y-4 pt-4">
          <Link
            href="/register"
            className="btn-primary w-full text-center block hover:scale-105 transition-all duration-300 shadow-lg hover:shadow-xl"
          >
            Start Your Free Trial
          </Link>
          <Link
            href="/categories"
            className="btn-secondary w-full text-center block hover:scale-105 transition-all duration-300"
          >
            Browse All Categories
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="max-w-xl mx-auto space-y-8">
      {/* Progress */}
      <div className="space-y-2">
        <div className="flex items-center justify-between text-sm text-muted-foreground">
          <span>Step {currentStep + 1} of {totalSteps}</span>
          <span>{Math.round(((currentStep + 1) / totalSteps) * 100)}%</span>
        </div>
        <div className="w-full bg-muted rounded-full h-2">
          <div
            className="bg-primary h-2 rounded-full transition-all duration-300"
            style={{ width: `${((currentStep + 1) / totalSteps) * 100}%` }}
          />
        </div>
      </div>

      {/* Question */}
      <div className="space-y-6">
        <h2 className="text-xl font-bold text-foreground">{currentQuestion.question}</h2>

        <div className="space-y-3">
          {currentQuestion.options.map((option) => (
            <button
              key={option.value}
              onClick={() => handleSelect(option.value)}
              className={`w-full text-left p-4 rounded-xl border transition-all duration-200 ${
                answers[currentQuestion.id] === option.value
                  ?'border-primary bg-primary/10 text-foreground'
                  :'border-border bg-card hover:border-primary/50 hover:bg-muted/30 text-foreground'
              }`}
            >
              <div className="flex items-center justify-between">
                <span className="font-medium">{option.label}</span>
                <ArrowRight className="w-4 h-4 text-muted-foreground" />
              </div>
            </button>
          ))}
        </div>
      </div>

      {/* Back Button */}
      {currentStep > 0 && (
        <button
          onClick={handleBack}
          className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft className="w-4 h-4" />
          Back
        </button>
      )}
    </div>
  )
}
