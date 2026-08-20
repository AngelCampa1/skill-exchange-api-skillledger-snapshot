import * as React from "react"

export interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  error?: boolean
  helperText?: string
  characterCount?: boolean
  maxLength?: number
}

const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, error, helperText, characterCount, maxLength, ...props }, ref) => {
    const [charCount, setCharCount] = React.useState(0)

    const handleChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
      setCharCount(e.target.value.length)
      props.onChange?.(e)
    }

    const baseClasses = "flex min-h-[80px] w-full rounded-xl border bg-background px-4 py-3 text-sm text-foreground ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 disabled:cursor-not-allowed disabled:opacity-50 transition-all duration-200 shadow-sm resize-y"

    const stateClasses = error
      ? "border-destructive focus-visible:ring-destructive/20 focus-visible:border-destructive"
      : "border-border focus-visible:ring-primary/20 focus-visible:border-primary"

    return (
      <div className="w-full">
        <textarea
          className={`${baseClasses} ${stateClasses} ${className || ""}`}
          ref={ref}
          aria-invalid={error ? "true" : "false"}
          aria-describedby={helperText ? `${props.id}-helper` : undefined}
          maxLength={maxLength}
          onChange={handleChange}
          {...props}
        />
        <div className="flex justify-between items-center mt-2">
          {helperText && (
            <p
              id={`${props.id}-helper`}
              data-testid={error ? `${props.id}-error` : `${props.id}-helper`}
              className={`text-xs ${error ? 'text-destructive' : 'text-muted-foreground'}`}
              role={error ? "alert" : undefined}
              aria-live={error ? "assertive" : undefined}
            >
              {helperText}
            </p>
          )}
          {characterCount && maxLength && (
            <span className={`text-xs ${charCount > maxLength * 0.9 ? 'text-destructive' : 'text-muted-foreground'} ml-auto`}>
              {charCount} / {maxLength}
            </span>
          )}
        </div>
      </div>
    )
  }
)
Textarea.displayName = "Textarea"

export { Textarea }