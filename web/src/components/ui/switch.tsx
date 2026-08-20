import * as React from "react"

export interface SwitchProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type' | 'onChange' | 'size'> {
  label?: string
  description?: string
  onCheckedChange?: (checked: boolean) => void
  size?: "sm" | "md" | "lg"
}

export const Switch = React.forwardRef<HTMLInputElement, SwitchProps>(
  ({ className, label, description, checked, onCheckedChange, size = "md", disabled, id, ...props }, ref) => {
    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      onCheckedChange?.(e.target.checked)
    }

    const sizeClasses = {
      sm: {
        track: "h-5 w-9",
        thumb: "h-4 w-4",
        translate: "translate-x-4"
      },
      md: {
        track: "h-6 w-11",
        thumb: "h-5 w-5",
        translate: "translate-x-5"
      },
      lg: {
        track: "h-7 w-14",
        thumb: "h-6 w-6",
        translate: "translate-x-7"
      }
    }

    const { track, thumb, translate } = sizeClasses[size]

    const switchElement = (
      <div className="relative inline-flex items-center">
        <input
          ref={ref}
          type="checkbox"
          role="switch"
          checked={checked}
          onChange={handleChange}
          disabled={disabled}
          className="peer sr-only"
          id={id}
          aria-checked={checked}
          {...props}
        />
        <div
          className={`
            ${track}
            rounded-full transition-all duration-200 cursor-pointer
            peer-focus-visible:ring-2 peer-focus-visible:ring-ring peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background
            peer-disabled:cursor-not-allowed peer-disabled:opacity-50
            ${checked
              ? "bg-primary"
              : "bg-muted border-2 border-border"
            }
            ${className || ""}
          `}
        >
          <div
            className={`
              ${thumb}
              rounded-full bg-background shadow-md transition-transform duration-200
              ${checked ? translate : "translate-x-0.5"}
            `}
          />
        </div>
      </div>
    )

    if (!label && !description) {
      return switchElement
    }

    return (
      <label
        htmlFor={id}
        className={`flex items-start space-x-3 ${disabled ? "cursor-not-allowed opacity-50" : "cursor-pointer"}`}
      >
        {switchElement}
        <div className="flex-1 space-y-1">
          {label && (
            <span className="text-sm font-medium text-foreground leading-none">
              {label}
            </span>
          )}
          {description && (
            <p className="text-xs text-muted-foreground leading-relaxed">
              {description}
            </p>
          )}
        </div>
      </label>
    )
  }
)

Switch.displayName = "Switch"
