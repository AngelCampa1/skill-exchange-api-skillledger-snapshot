import * as React from "react"

export interface ProgressProps extends React.HTMLAttributes<HTMLDivElement> {
  value: number
  max?: number
  size?: "sm" | "md" | "lg"
  variant?: "default" | "success" | "warning" | "error"
  showLabel?: boolean
  label?: string
  'data-testid'?: string
}

const Progress = React.forwardRef<HTMLDivElement, ProgressProps>(
  ({ className, value, max = 100, size = "md", variant = "default", showLabel = false, label, ...props }, ref) => {
    const percentage = Math.min(Math.max((value / max) * 100, 0), 100)
    const { 'data-testid': testId, ...restProps } = props
    
    const sizeClasses = {
      sm: "h-1",
      md: "h-2", 
      lg: "h-3"
    }
    
    const getVariantColor = (variant: string, percentage: number) => {
      if (variant !== "default") {
        const colors = {
          success: "bg-success",
          warning: "bg-warning",
          error: "bg-destructive"
        }
        return colors[variant as keyof typeof colors] || "bg-primary"
      }

      // Dynamic color based on percentage for default variant
      if (percentage < 30) return "bg-destructive"
      if (percentage < 60) return "bg-warning"
      if (percentage < 80) return "bg-info"
      return "bg-success"
    }

    const getStrengthText = (percentage: number): string => {
      if (percentage < 30) return "Weak"
      if (percentage < 60) return "Fair"
      if (percentage < 80) return "Good"
      return "Strong"
    }

    const getStrengthColorClass = (percentage: number): string => {
      if (percentage < 30) return "text-destructive"
      if (percentage < 60) return "text-warning"
      if (percentage < 80) return "text-info"
      return "text-success"
    }

    return (
      <div className={`w-full ${className || ""}`} ref={ref} {...restProps}>
        {showLabel && (
          <div className="flex justify-between text-sm mb-2">
            <span className="text-muted-foreground">{label || "Progress"}</span>
            <span
              className={`font-medium ${getStrengthColorClass(percentage)}`}
              data-testid="strength-text"
            >
              {label ? `${Math.round(percentage)}%` : getStrengthText(percentage)}
            </span>
          </div>
        )}
        <div 
          className={`w-full bg-muted rounded-full overflow-hidden ${sizeClasses[size]}`}
          role="progressbar"
          aria-valuenow={value}
          aria-valuemin={0}
          aria-valuemax={max}
          aria-label={label || `Progress: ${Math.round(percentage)}%`}
        >
          <div
            className={`${sizeClasses[size]} rounded-full transition-all duration-300 ease-out ${getVariantColor(variant, percentage)}`}
            style={{ width: `${percentage}%` }}
            data-testid={testId}
          />
        </div>
      </div>
    )
  }
)

Progress.displayName = "Progress"

export { Progress }