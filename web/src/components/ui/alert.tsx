import * as React from "react"
import { AlertCircle, CheckCircle, Info, AlertTriangle } from "lucide-react"

export interface AlertProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: "default" | "destructive" | "success" | "warning" | "info"
  icon?: React.ReactNode
  showIcon?: boolean
}

const Alert = React.forwardRef<HTMLDivElement, AlertProps>(
  ({ className, variant = "default", icon, showIcon = true, children, ...props }, ref) => {
    const baseClasses = "relative w-full rounded-lg border p-4 flex items-start space-x-3"

    const variantClasses = {
      default: "bg-background text-foreground border-border",
      destructive: "border-destructive/20 bg-destructive/10 text-destructive",
      success: "border-success/20 bg-success/10 text-success",
      warning: "border-warning/20 bg-warning/10 text-warning",
      info: "border-info/20 bg-info/10 text-info",
    }

    const defaultIcons = {
      default: null,
      destructive: <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" aria-hidden="true" />,
      success: <CheckCircle className="h-5 w-5 flex-shrink-0 mt-0.5" aria-hidden="true" />,
      warning: <AlertTriangle className="h-5 w-5 flex-shrink-0 mt-0.5" aria-hidden="true" />,
      info: <Info className="h-5 w-5 flex-shrink-0 mt-0.5" aria-hidden="true" />,
    }

    const displayIcon = icon || (showIcon ? defaultIcons[variant] : null)

    return (
      <div
        ref={ref}
        role="alert"
        aria-live="polite"
        aria-atomic="true"
        className={`${baseClasses} ${variantClasses[variant]} ${className || ""}`}
        {...props}
      >
        {displayIcon}
        <div className="flex-1">{children}</div>
      </div>
    )
  }
)
Alert.displayName = "Alert"

const AlertTitle = React.forwardRef<
  HTMLHeadingElement,
  React.HTMLAttributes<HTMLHeadingElement>
>(({ className, ...props }, ref) => (
  <h5
    ref={ref}
    className={`mb-1 font-medium leading-none tracking-tight ${className || ""}`}
    {...props}
  />
))
AlertTitle.displayName = "AlertTitle"

const AlertDescription = React.forwardRef<
  HTMLDivElement,
  React.HTMLAttributes<HTMLDivElement>
>(({ className, ...props }, ref) => (
  <div
    ref={ref}
    className={`text-sm leading-relaxed ${className || ""}`}
    {...props}
  />
))
AlertDescription.displayName = "AlertDescription"

export { Alert, AlertTitle, AlertDescription }
