'use client'

import React from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { BasicInfo } from '@/types/profile'

const basicInfoSchema = z.object({
  firstName: z.string().min(1, 'First name is required'),
  lastName: z.string().min(1, 'Last name is required'),
  title: z.string().min(1, 'Professional title is required'),
  company: z.string().optional(),
  location: z.string().optional(),
  timeZone: z.string().optional(),
  summary: z.string().optional(),
  websiteUrl: z.string().url('Please enter a valid URL').optional().or(z.literal('')),
  linkedInUrl: z.string().url('Please enter a valid LinkedIn URL').optional().or(z.literal('')),
  gitHubUrl: z.string().url('Please enter a valid GitHub URL').optional().or(z.literal('')),
})

interface Step1BasicInfoProps {
  data: BasicInfo
  onUpdate: (data: BasicInfo) => void
  onNext: () => void
}

export default function Step1BasicInfo({ data, onUpdate, onNext }: Step1BasicInfoProps) {
  const {
    register,
    handleSubmit,
    formState: { errors, isValid },
  } = useForm<BasicInfo>({
    resolver: zodResolver(basicInfoSchema),
    mode: 'onChange',
    defaultValues: data,
  })

  const onSubmit = (formData: BasicInfo) => {
    onUpdate(formData)
    onNext()
  }

  return (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-foreground">Basic Information</h2>
        <p className="text-muted-foreground mt-2">
          Let's start with the essentials. Tell us about yourself.
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
        {/* Name Fields */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="firstName" className="block text-sm font-medium text-foreground mb-1">
              First Name <span className="text-destructive">*</span>
            </label>
            <input
              {...register('firstName')}
              type="text"
              id="firstName"
              className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              placeholder="John"
            />
            {errors.firstName && (
              <p className="mt-1 text-sm text-destructive">{errors.firstName.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="lastName" className="block text-sm font-medium text-foreground mb-1">
              Last Name <span className="text-destructive">*</span>
            </label>
            <input
              {...register('lastName')}
              type="text"
              id="lastName"
              className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              placeholder="Doe"
            />
            {errors.lastName && (
              <p className="mt-1 text-sm text-destructive">{errors.lastName.message}</p>
            )}
          </div>
        </div>

        {/* Professional Title */}
        <div>
          <label htmlFor="title" className="block text-sm font-medium text-foreground mb-1">
            Professional Title <span className="text-destructive">*</span>
          </label>
          <input
            {...register('title')}
            type="text"
            id="title"
            className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
            placeholder="e.g., Senior Software Engineer, Marketing Manager"
          />
          {errors.title && (
            <p className="mt-1 text-sm text-destructive">{errors.title.message}</p>
          )}
        </div>

        {/* Company */}
        <div>
          <label htmlFor="company" className="block text-sm font-medium text-foreground mb-1">
            Company
          </label>
          <input
            {...register('company')}
            type="text"
            id="company"
            className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
            placeholder="Your current company"
          />
        </div>

        {/* Location and Time Zone */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="location" className="block text-sm font-medium text-foreground mb-1">
              Location
            </label>
            <input
              {...register('location')}
              type="text"
              id="location"
              className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              placeholder="e.g., San Francisco, CA"
            />
          </div>

          <div>
            <label htmlFor="timeZone" className="block text-sm font-medium text-foreground mb-1">
              Time Zone
            </label>
            <select
              {...register('timeZone')}
              id="timeZone"
              className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
            >
              <option value="">Select your time zone</option>
              <option value="America/New_York">Eastern Time (ET)</option>
              <option value="America/Chicago">Central Time (CT)</option>
              <option value="America/Denver">Mountain Time (MT)</option>
              <option value="America/Los_Angeles">Pacific Time (PT)</option>
              <option value="Europe/London">London (GMT)</option>
              <option value="Europe/Paris">Central European Time (CET)</option>
              <option value="Asia/Tokyo">Japan Time (JST)</option>
              <option value="Asia/Shanghai">China Time (CST)</option>
              <option value="Australia/Sydney">Australian Eastern Time (AET)</option>
            </select>
          </div>
        </div>

        {/* Professional Summary */}
        <div>
          <label htmlFor="summary" className="block text-sm font-medium text-foreground mb-1">
            Professional Summary
          </label>
          <textarea
            {...register('summary')}
            id="summary"
            rows={4}
            className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
            placeholder="Brief description of your professional background and expertise"
          />
        </div>

        {/* Social Links */}
        <div className="space-y-4">
          <h3 className="text-lg font-medium text-foreground">Social Links (Optional)</h3>

          <div>
            <label htmlFor="websiteUrl" className="block text-sm font-medium text-foreground mb-1">
              Website
            </label>
            <input
              {...register('websiteUrl')}
              type="url"
              id="websiteUrl"
              className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              placeholder="https://yourwebsite.com"
            />
            {errors.websiteUrl && (
              <p className="mt-1 text-sm text-destructive">{errors.websiteUrl.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="linkedInUrl" className="block text-sm font-medium text-foreground mb-1">
              LinkedIn Profile
            </label>
            <input
              {...register('linkedInUrl')}
              type="url"
              id="linkedInUrl"
              className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              placeholder="https://linkedin.com/in/yourname"
            />
            {errors.linkedInUrl && (
              <p className="mt-1 text-sm text-destructive">{errors.linkedInUrl.message}</p>
            )}
          </div>

          <div>
            <label htmlFor="gitHubUrl" className="block text-sm font-medium text-foreground mb-1">
              GitHub Profile
            </label>
            <input
              {...register('gitHubUrl')}
              type="url"
              id="gitHubUrl"
              className="w-full px-3 py-2 border border-input rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-ring"
              placeholder="https://github.com/yourusername"
            />
            {errors.gitHubUrl && (
              <p className="mt-1 text-sm text-destructive">{errors.gitHubUrl.message}</p>
            )}
          </div>
        </div>

        {/* Submit Button */}
        <div className="flex justify-end pt-6 border-t border-border">
          <button
            type="submit"
            disabled={!isValid}
            className="px-6 py-2 bg-primary text-primary-foreground rounded-full hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Next Step
          </button>
        </div>
      </form>
    </div>
  )
}
