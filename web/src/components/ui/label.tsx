import * as React from "react"

export interface LabelProps extends React.LabelHTMLAttributes<HTMLLabelElement> {
  required?: boolean
  error?: boolean
  helperText?: string
}

const Label = React.forwardRef<HTMLLabelElement, LabelProps>(
  ({ className, required, error, helperText, children, ...props }, ref) => {
    return (
      <div className="space-y-1">
        <label
          ref={ref}
          className={`text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 ${
            error ? "text-destructive" : "text-foreground"
          } ${className || ""}`}
          {...props}
        >
          {children}
          {required && (
            <span className="ml-1 text-destructive" aria-label="required">
              *
            </span>
          )}
        </label>
        {helperText && (
          <p className={`text-xs ${error ? "text-destructive" : "text-muted-foreground"}`}>
            {helperText}
          </p>
        )}
      </div>
    )
  }
)
Label.displayName = "Label"

export { Label }