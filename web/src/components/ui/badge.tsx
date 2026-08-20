import * as React from "react"

const getBadgeClasses = (variant?: string, size?: string) => {
  const baseClasses = "inline-flex items-center rounded-full border font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"

  const variantClasses = {
    default: "border-transparent bg-primary text-primary-foreground hover:bg-primary/80",
    secondary: "border-transparent bg-secondary text-secondary-foreground hover:bg-secondary/80",
    destructive: "border-transparent bg-destructive text-destructive-foreground hover:bg-destructive/80",
    outline: "text-foreground border-border hover:bg-accent",
    success: "border-transparent bg-success text-success-foreground hover:bg-success/80",
    warning: "border-transparent bg-warning text-warning-foreground hover:bg-warning/80",
    info: "border-transparent bg-info text-info-foreground hover:bg-info/80",
  }

  const sizeClasses = {
    sm: "px-2 py-0.5 text-xs",
    md: "px-2.5 py-0.5 text-sm",
    lg: "px-3 py-1 text-base"
  }

  return `${baseClasses} ${variantClasses[variant as keyof typeof variantClasses] || variantClasses.default} ${sizeClasses[size as keyof typeof sizeClasses] || sizeClasses.sm}`
}

export interface BadgeProps extends React.HTMLAttributes<HTMLSpanElement> {
  variant?: "default" | "secondary" | "destructive" | "outline" | "success" | "warning" | "info"
  size?: "sm" | "md" | "lg"
}

const Badge = React.forwardRef<HTMLSpanElement, BadgeProps>(
  ({ className, variant = "default", size = "sm", ...props }, ref) => {
    return (
      <span
        ref={ref}
        className={`${getBadgeClasses(variant, size)} ${className || ""}`}
        {...props}
      />
    )
  }
)

Badge.displayName = "Badge"

export { Badge }