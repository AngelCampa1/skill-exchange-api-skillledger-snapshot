/**
 * Analytics Type Definitions
 *
 * Comprehensive TypeScript types for tracking user events across SkillLedger.
 * Provides type safety for all analytics operations with GA4 and Microsoft Clarity.
 */

/**
 * Event categories matching user interaction patterns
 */
export type EventCategory =
  | 'authentication'
  | 'monetization'
  | 'projects'
  | 'credits'
  | 'profile'
  | 'messaging'
  | 'search'
  | 'navigation'
  | 'forms'
  | 'ui_interaction'
  | 'feedback'
  | 'errors'
  | 'performance'
  | 'conversion'

/**
 * Event priority levels for tracking importance
 */
export type EventPriority = 'critical' | 'high' | 'medium' | 'low'

/**
 * Base analytics event interface
 */
export interface AnalyticsEvent {
  name: string
  category: EventCategory
  priority: EventPriority
  properties?: Record<string, string | number | boolean | undefined>
  userProperties?: Record<string, string | number | boolean | undefined>
  timestamp?: number
}

/**
 * Authentication-related events
 */
export interface AuthenticationEvent extends AnalyticsEvent {
  category: 'authentication'
  name: 'sign_up' | 'sign_in' | 'logout' | 'password_reset' | 'email_verification'
  properties?: {
    method?: 'email' | 'oauth' | 'sso'
    provider?: string
    success?: boolean
    error?: string
  }
}

/**
 * Monetization and subscription events
 */
export interface MonetizationEvent extends AnalyticsEvent {
  category: 'monetization'
  name:
    | 'view_subscription'
    | 'select_tier'
    | 'begin_checkout'
    | 'purchase_success'
    | 'purchase_failure'
    | 'cancel_subscription'
    | 'update_payment_method'
    | 'view_billing_history'
  properties?: {
    tier?: 'free' | 'professional' | 'enterprise'
    billing_cycle?: 'monthly' | 'annual'
    value?: number
    currency?: string
    transaction_id?: string
    promo_code?: string
    error?: string
  }
}

/**
 * Project-related events
 */
export interface ProjectEvent extends AnalyticsEvent {
  category: 'projects'
  name:
    | 'project_created'
    | 'project_search'
    | 'project_viewed'
    | 'application_submitted'
    | 'application_accepted'
    | 'application_rejected'
    | 'project_completed'
    | 'project_archived'
  properties?: {
    project_id?: string
    project_type?: string
    budget?: number
    query?: string
    filters?: string
    result_count?: number
    application_id?: string
  }
}

/**
 * Credit and wallet transaction events
 */
export interface CreditEvent extends AnalyticsEvent {
  category: 'credits'
  name:
    | 'credit_transfer'
    | 'escrow_deposit'
    | 'escrow_release'
    | 'escrow_refund'
    | 'wallet_viewed'
    | 'transaction_history_viewed'
  properties?: {
    amount?: number
    transaction_type?: string
    recipient_id?: string
    sender_id?: string
    project_id?: string
    status?: 'completed' | 'pending' | 'failed'
  }
}

/**
 * Profile and onboarding events
 */
export interface ProfileEvent extends AnalyticsEvent {
  category: 'profile'
  name:
    | 'wizard_started'
    | 'wizard_step_completed'
    | 'wizard_completed'
    | 'profile_viewed'
    | 'profile_updated'
    | 'profile_published'
    | 'photo_uploaded'
    | 'skill_added'
    | 'experience_added'
  properties?: {
    step?: number
    step_name?: string
    is_public?: boolean
    skill_count?: number
    experience_count?: number
  }
}

/**
 * Messaging and communication events
 */
export interface MessagingEvent extends AnalyticsEvent {
  category: 'messaging'
  name:
    | 'message_sent'
    | 'message_received'
    | 'typing_started'
    | 'typing_stopped'
    | 'conversation_opened'
    | 'file_shared'
    | 'reaction_added'
  properties?: {
    conversation_id?: string
    workspace_id?: string
    message_type?: 'text' | 'file' | 'emoji'
    file_type?: string
    character_count?: number
  }
}

/**
 * Search and discovery events
 */
export interface SearchEvent extends AnalyticsEvent {
  category: 'search'
  name:
    | 'search_query_entered'
    | 'filter_applied'
    | 'filter_removed'
    | 'filter_cleared'
    | 'sort_changed'
    | 'location_search'
    | 'geolocation_used'
  properties?: {
    query?: string
    filter_type?: string
    filter_value?: string
    sort_order?: string
    result_count?: number
    location?: string
    radius?: number
  }
}

/**
 * Navigation events
 */
export interface NavigationEvent extends AnalyticsEvent {
  category: 'navigation'
  name: 'page_view' | 'link_clicked' | 'back_button' | 'tab_changed' | 'modal_opened' | 'modal_closed'
  properties?: {
    page_url?: string
    page_title?: string
    previous_page?: string
    link_url?: string
    modal_name?: string
    tab_name?: string
  }
}

/**
 * Form interaction events
 */
export interface FormEvent extends AnalyticsEvent {
  category: 'forms'
  name:
    | 'form_started'
    | 'form_field_changed'
    | 'form_validation_error'
    | 'form_submitted'
    | 'form_success'
    | 'form_error'
  properties?: {
    form_name?: string
    field_name?: string
    error_fields?: string // Comma-separated list of field names
    completion_time?: number
    attempt_count?: number
  }
}

/**
 * UI interaction events
 */
export interface UIInteractionEvent extends AnalyticsEvent {
  category: 'ui_interaction'
  name:
    | 'button_clicked'
    | 'dropdown_opened'
    | 'dropdown_closed'
    | 'tooltip_shown'
    | 'menu_toggled'
    | 'accordion_expanded'
  properties?: {
    element_name?: string
    element_type?: string
    action?: string
  }
}

/**
 * Feedback events
 */
export interface FeedbackEvent extends AnalyticsEvent {
  category: 'feedback'
  name: 'feedback_opened' | 'feedback_submitted' | 'feedback_closed' | 'rating_given'
  properties?: {
    feedback_type?: string
    rating?: number
    page_context?: string
  }
}

/**
 * Error and exception events
 */
export interface ErrorEvent extends AnalyticsEvent {
  category: 'errors'
  name: 'error_occurred' | 'api_error' | 'validation_error' | 'connection_error'
  properties?: {
    error_message?: string
    error_type?: string
    stack_trace?: string
    component?: string
    api_endpoint?: string
    status_code?: number
  }
}

/**
 * Performance tracking events
 */
export interface PerformanceEvent extends AnalyticsEvent {
  category: 'performance'
  name: 'web_vital' | 'timing_complete' | 'api_timing' | 'page_load'
  properties?: {
    metric_name?: string
    value?: number
    rating?: 'good' | 'needs-improvement' | 'poor'
  }
}

/**
 * Union type of all specific event types
 */
export type SpecificAnalyticsEvent =
  | AuthenticationEvent
  | MonetizationEvent
  | ProjectEvent
  | CreditEvent
  | ProfileEvent
  | MessagingEvent
  | SearchEvent
  | NavigationEvent
  | FormEvent
  | UIInteractionEvent
  | FeedbackEvent
  | ErrorEvent
  | PerformanceEvent

/**
 * User properties for identification
 */
export interface UserProperties {
  user_id?: string
  email_verified?: boolean
  tax_compliant?: boolean
  subscription_tier?: 'free' | 'professional' | 'enterprise'
  roles?: string
  account_age_days?: number
  project_count?: number
  total_credits?: number
  [key: string]: string | number | boolean | undefined
}

/**
 * Consent state for privacy compliance
 */
export type ConsentState = 'granted' | 'denied' | null

/**
 * Analytics configuration
 */
export interface AnalyticsConfig {
  enabled: boolean
  ga4MeasurementId?: string
  clarityProjectId?: string
  environment: 'development' | 'staging' | 'production'
  debug?: boolean
}
