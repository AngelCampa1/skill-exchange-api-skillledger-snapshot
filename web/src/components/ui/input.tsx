import * as React from "react"

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  error?: boolean
  helperText?: string
  startIcon?: React.ReactNode
  endIcon?: React.ReactNode
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type, error, helperText, startIcon, endIcon, onChange, ...props }, ref) => {
    const internalRef = React.useRef<HTMLInputElement>(null)
    const combinedRef = (node: HTMLInputElement | null) => {
      // Handle both internal ref and forwarded ref
      (internalRef as React.MutableRefObject<HTMLInputElement | null>).current = node
      if (typeof ref === 'function') {
        ref(node)
      } else if (ref) {
        (ref as React.MutableRefObject<HTMLInputElement | null>).current = node
      }
    }

    // E2E-007 FIX: Handle browser autofill by detecting animation and triggering change event
    // Browsers apply a pseudo-animation when autofilling, which we can detect
    const handleAnimationStart = React.useCallback((e: React.AnimationEvent<HTMLInputElement>) => {
      // Chrome/Safari autofill triggers an animation named 'onAutoFillStart'
      // We detect any animation and check if the input has a value that wasn't from user input
      if (e.animationName && internalRef.current) {
        const input = internalRef.current
        // If the input has a value after autofill, dispatch a synthetic change event
        if (input.value) {
          // Create and dispatch a native input event to trigger react-hook-form's onChange
          const nativeInputEvent = new Event('input', { bubbles: true })
          input.dispatchEvent(nativeInputEvent)

          // Also call onChange directly if provided (for react-hook-form compatibility)
          if (onChange) {
            const syntheticEvent = {
              target: input,
              currentTarget: input,
            } as React.ChangeEvent<HTMLInputElement>
            onChange(syntheticEvent)
          }
        }
      }
    }, [onChange])

    const baseClasses = "flex h-12 w-full rounded-xl border bg-background text-sm font-medium text-foreground ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 disabled:cursor-not-allowed disabled:opacity-50 transition-all duration-200 shadow-sm"

    const stateClasses = error
      ? "border-destructive focus-visible:ring-destructive/20 focus-visible:border-destructive"
      : "border-border focus-visible:ring-primary/20 focus-visible:border-primary"

    const paddingClasses = React.useMemo(() => {
      if (startIcon && endIcon) return "px-4 py-3 pl-11 pr-11"
      if (startIcon) return "px-4 py-3 pl-11"
      if (endIcon) return "px-4 py-3 pr-11"
      return "px-4 py-3"
    }, [startIcon, endIcon])

    return (
      <div className="relative">
        {startIcon && (
          <div className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground">
            {startIcon}
          </div>
        )}
        <input
          type={type}
          className={`${baseClasses} ${stateClasses} ${paddingClasses} ${className || ""}`}
          ref={combinedRef}
          aria-invalid={error ? "true" : "false"}
          aria-describedby={helperText ? `${props.id}-helper` : undefined}
          onChange={onChange}
          onAnimationStart={handleAnimationStart}
          {...props}
        />
        {endIcon && (
          <div className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground">
            {endIcon}
          </div>
        )}
        {helperText && (
          <p
            id={`${props.id}-helper`}
            data-testid={error ? `${props.id}-error` : `${props.id}-helper`}
            className={`mt-2 text-xs ${error ? 'text-destructive' : 'text-muted-foreground'}`}
            role={error ? "alert" : undefined}
            aria-live={error ? "assertive" : undefined}
          >
            {helperText}
          </p>
        )}
      </div>
    )
  }
)
Input.displayName = "Input"

export { Input }