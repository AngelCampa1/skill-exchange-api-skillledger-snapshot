import * as React from "react"

export interface SpinnerProps extends React.HTMLAttributes<HTMLDivElement> {
  size?: "sm" | "md" | "lg" | "xl"
  variant?: "default" | "primary" | "white"
  label?: string
}

export const Spinner = React.forwardRef<HTMLDivElement, SpinnerProps>(
  ({ className, size = "md", variant = "default", label = "Loading...", ...props }, ref) => {
    const sizeClasses = {
      sm: "w-4 h-4 border-2",
      md: "w-8 h-8 border-2",
      lg: "w-12 h-12 border-3",
      xl: "w-16 h-16 border-4"
    }

    const variantClasses = {
      default: "border-muted-foreground/20 border-t-muted-foreground",
      primary: "border-primary/20 border-t-primary",
      white: "border-white/20 border-t-white"
    }

    return (
      <div
        ref={ref}
        role="status"
        aria-label={label}
        className={`flex items-center justify-center ${className || ""}`}
        {...props}
      >
        <div
          className={`
            ${sizeClasses[size]}
            ${variantClasses[variant]}
            rounded-full animate-spin
          `}
          aria-hidden="true"
        />
        <span className="sr-only">{label}</span>
      </div>
    )
  }
)

Spinner.displayName = "Spinner"

// Fullscreen loading overlay
export const FullPageSpinner: React.FC<{ message?: string }> = ({
  message = "Loading..."
}) => {
  return (
    <div
      className="fixed inset-0 z-50 flex flex-col items-center justify-center bg-background/80 backdrop-blur-sm"
      role="alert"
      aria-live="assertive"
      aria-busy="true"
    >
      <Spinner size="xl" variant="primary" label={message} />
      {message && (
        <p className="mt-4 text-sm text-muted-foreground">{message}</p>
      )}
    </div>
  )
}
