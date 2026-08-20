import * as React from "react"
import { Check, Minus } from "lucide-react"

export interface CheckboxProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label?: string
  helperText?: string
  error?: boolean
  indeterminate?: boolean
  onCheckedChange?: (checked: boolean) => void
}

export const Checkbox = React.forwardRef<HTMLInputElement, CheckboxProps>(
  ({ className, label, helperText, error, indeterminate, checked, onCheckedChange, onChange, ...props }, ref) => {
    const internalRef = React.useRef<HTMLInputElement>(null)
    const combinedRef = ref || internalRef

    React.useEffect(() => {
      const element = (combinedRef as React.RefObject<HTMLInputElement>).current
      if (element) {
        element.indeterminate = indeterminate || false
      }
    }, [indeterminate, combinedRef])

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      onChange?.(e)
      onCheckedChange?.(e.target.checked)
    }

    const checkboxElement = (
      <div className="relative inline-flex items-center">
        <input
          ref={combinedRef}
          type="checkbox"
          className="peer sr-only"
          checked={checked}
          onChange={handleChange}
          aria-invalid={error ? "true" : "false"}
          aria-describedby={helperText ? `${props.id}-helper` : undefined}
          {...props}
        />
        <div
          className={`
            h-5 w-5 rounded border-2 flex items-center justify-center
            transition-all duration-200 cursor-pointer
            peer-focus-visible:ring-2 peer-focus-visible:ring-ring peer-focus-visible:ring-offset-2
            peer-disabled:cursor-not-allowed peer-disabled:opacity-50
            ${
              error
                ? "border-destructive text-destructive"
                : checked || indeterminate
                ? "bg-primary border-primary text-primary-foreground"
                : "border-border bg-background hover:border-primary/50"
            }
            ${className || ""}
          `}
        >
          {checked && !indeterminate && <Check className="h-3.5 w-3.5" aria-hidden="true" />}
          {indeterminate && <Minus className="h-3.5 w-3.5" aria-hidden="true" />}
        </div>
      </div>
    )

    if (!label && !helperText) {
      return checkboxElement
    }

    return (
      <div className="space-y-1">
        <label
          htmlFor={props.id}
          className="flex items-center space-x-3 cursor-pointer"
        >
          {checkboxElement}
          {label && (
            <span
              className={`text-sm font-medium leading-none ${
                error ? "text-destructive" : "text-foreground"
              } ${props.disabled ? "opacity-50 cursor-not-allowed" : ""}`}
            >
              {label}
            </span>
          )}
        </label>
        {helperText && (
          <p
            id={`${props.id}-helper`}
            className={`text-xs ${error ? "text-destructive" : "text-muted-foreground"} ml-8`}
            role={error ? "alert" : undefined}
          >
            {helperText}
          </p>
        )}
      </div>
    )
  }
)

Checkbox.displayName = "Checkbox"
