import * as React from "react"
import { Button, ButtonProps } from "./button"

export interface IconButtonProps extends Omit<ButtonProps, 'children'> {
  icon: React.ReactNode
  label: string // Required for accessibility
  tooltipText?: string
}

/**
 * IconButton - Accessible icon-only button component
 *
 * This component ensures all icon buttons have proper aria-labels
 * for screen readers, meeting WCAG 2.1 Level A compliance.
 *
 * @param icon - The icon element to display
 * @param label - Required accessible label for screen readers
 * @param tooltipText - Optional tooltip text (defaults to label)
 */
export const IconButton = React.forwardRef<HTMLButtonElement, IconButtonProps>(
  ({ icon, label, tooltipText, ...props }, ref) => {
    return (
      <Button
        ref={ref}
        aria-label={label}
        title={tooltipText || label}
        {...props}
      >
        {icon}
      </Button>
    )
  }
)

IconButton.displayName = "IconButton"
