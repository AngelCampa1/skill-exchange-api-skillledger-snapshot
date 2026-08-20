import * as React from "react"
import { Button } from "./button"

export interface EmptyStateProps {
  icon?: React.ReactNode
  title: string
  description?: string
  action?: {
    label: string
    onClick: () => void
    variant?: "default" | "outline" | "ghost"
  }
  className?: string
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  icon,
  title,
  description,
  action,
  className
}) => {
  return (
    <div
      className={`flex flex-col items-center justify-center py-12 px-4 text-center ${className || ""}`}
      role="status"
      aria-live="polite"
    >
      {icon && (
        <div className="mb-4 text-muted-foreground opacity-50" aria-hidden="true">
          {icon}
        </div>
      )}

      <h3 className="text-lg font-semibold text-foreground mb-2">
        {title}
      </h3>

      {description && (
        <p className="text-sm text-muted-foreground max-w-sm mb-6">
          {description}
        </p>
      )}

      {action && (
        <Button
          variant={action.variant || "default"}
          onClick={action.onClick}
        >
          {action.label}
        </Button>
      )}
    </div>
  )
}

// Preset empty state variants
export const EmptySearchResults: React.FC<{
  searchTerm?: string
  onClear?: () => void
}> = ({ searchTerm, onClear }) => (
  <EmptyState
    icon={
      <svg
        className="w-16 h-16"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
        xmlns="http://www.w3.org/2000/svg"
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth={1.5}
          d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
        />
      </svg>
    }
    title={searchTerm ? `No results for "${searchTerm}"` : "No results found"}
    description="Try adjusting your search or filters to find what you're looking for."
    action={
      onClear
        ? {
            label: "Clear filters",
            onClick: onClear,
            variant: "outline"
          }
        : undefined
    }
  />
)

export const EmptyList: React.FC<{
  title: string
  description?: string
  actionLabel?: string
  onAction?: () => void
}> = ({ title, description, actionLabel, onAction }) => (
  <EmptyState
    icon={
      <svg
        className="w-16 h-16"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
        xmlns="http://www.w3.org/2000/svg"
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth={1.5}
          d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
        />
      </svg>
    }
    title={title}
    description={description}
    action={
      actionLabel && onAction
        ? {
            label: actionLabel,
            onClick: onAction
          }
        : undefined
    }
  />
)

export const EmptyError: React.FC<{
  title?: string
  description?: string
  onRetry?: () => void
}> = ({
  title = "Something went wrong",
  description = "We encountered an error while loading this content. Please try again.",
  onRetry
}) => (
  <EmptyState
    icon={
      <svg
        className="w-16 h-16"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
        xmlns="http://www.w3.org/2000/svg"
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth={1.5}
          d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
        />
      </svg>
    }
    title={title}
    description={description}
    action={
      onRetry
        ? {
            label: "Try again",
            onClick: onRetry,
            variant: "outline"
          }
        : undefined
    }
  />
)
