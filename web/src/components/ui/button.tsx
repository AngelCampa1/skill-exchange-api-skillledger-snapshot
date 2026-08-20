import * as React from "react"
import { Loader2 } from "lucide-react"

const getButtonClasses = (variant?: string, size?: string, loading?: boolean) => {
  const baseClasses = "inline-flex items-center justify-center whitespace-nowrap rounded-full text-sm font-semibold transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/20 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 disabled:cursor-not-allowed"
  
  const variantClasses = {
    default: "bg-primary text-primary-foreground shadow-sm hover:shadow-md hover:scale-[1.02]",
    destructive: "bg-destructive text-destructive-foreground shadow-sm hover:shadow-md hover:scale-[1.02]",
    outline: "border border-border bg-background text-foreground shadow-sm hover:bg-muted hover:shadow-md hover:scale-[1.02]",
    secondary: "bg-secondary text-secondary-foreground shadow-sm hover:bg-secondary/80 hover:shadow-md hover:scale-[1.02]",
    ghost: "bg-transparent text-muted-foreground hover:bg-muted hover:text-foreground",
    link: "text-primary underline-offset-4 hover:underline",
  }
  
  const sizeClasses = {
    default: "h-11 px-5 py-2.5",
    sm: "h-9 px-3 py-1.5 text-xs",
    lg: "h-12 px-6 py-3 text-base",
    xl: "h-14 px-8 py-4 text-lg",
    icon: "h-11 w-11",
  }
  
  // Disable hover effects when loading
  const hoverOverride = loading ? "hover:scale-100 hover:shadow-sm" : ""
  
  return `${baseClasses} ${variantClasses[variant as keyof typeof variantClasses] || variantClasses.default} ${sizeClasses[size as keyof typeof sizeClasses] || sizeClasses.default} ${hoverOverride}`
}

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "default" | "destructive" | "outline" | "secondary" | "ghost" | "link"
  size?: "default" | "sm" | "lg" | "xl" | "icon"
  loading?: boolean
  loadingText?: string
  startIcon?: React.ReactNode
  endIcon?: React.ReactNode
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = "default", size = "default", loading = false, loadingText, startIcon, endIcon, children, ...props }, ref) => {
    const buttonContent = () => {
      if (loading) {
        return (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />
            <span>{loadingText || "Loading..."}</span>
          </>
        )
      }
      
      return (
        <>
          {startIcon && <span className="mr-2">{startIcon}</span>}
          {children}
          {endIcon && <span className="ml-2">{endIcon}</span>}
        </>
      )
    }

    return (
      <button
        type={props.type || "button"}
        className={`${getButtonClasses(variant, size, loading)} ${className || ""}`}
        disabled={loading || props.disabled}
        ref={ref}
        aria-busy={loading}
        {...props}
      >
        {buttonContent()}
      </button>
    )
  }
)
Button.displayName = "Button"

export { Button }