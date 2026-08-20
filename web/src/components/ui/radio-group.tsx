import * as React from "react"

export interface RadioOption {
  value: string
  label: string
  description?: string
  disabled?: boolean
}

export interface RadioGroupProps {
  name: string
  value?: string
  defaultValue?: string
  onValueChange?: (value: string) => void
  options: RadioOption[]
  orientation?: "horizontal" | "vertical"
  error?: boolean
  helperText?: string
  disabled?: boolean
  className?: string
}

const RadioGroupContext = React.createContext<{
  name: string
  value?: string
  onValueChange?: (value: string) => void
  disabled?: boolean
  error?: boolean
}>({
  name: "",
})

export const RadioGroup: React.FC<RadioGroupProps> = ({
  name,
  value,
  defaultValue,
  onValueChange,
  options,
  orientation = "vertical",
  error,
  helperText,
  disabled,
  className,
}) => {
  const [internalValue, setInternalValue] = React.useState(defaultValue || "")

  const currentValue = value !== undefined ? value : internalValue

  const handleValueChange = (newValue: string) => {
    if (value === undefined) {
      setInternalValue(newValue)
    }
    onValueChange?.(newValue)
  }

  return (
    <RadioGroupContext.Provider
      value={{ name, value: currentValue, onValueChange: handleValueChange, disabled, error }}
    >
      <div
        role="radiogroup"
        className={`${orientation === "horizontal" ? "flex flex-wrap gap-4" : "space-y-3"} ${className || ""}`}
      >
        {options.map((option) => (
          <RadioItem key={option.value} {...option} />
        ))}
      </div>
      {helperText && (
        <p
          className={`text-xs mt-2 ${error ? "text-destructive" : "text-muted-foreground"}`}
          role={error ? "alert" : undefined}
        >
          {helperText}
        </p>
      )}
    </RadioGroupContext.Provider>
  )
}

interface RadioItemProps extends RadioOption {}

const RadioItem: React.FC<RadioItemProps> = ({ value, label, description, disabled: itemDisabled }) => {
  const { name, value: selectedValue, onValueChange, disabled: groupDisabled, error } = React.useContext(RadioGroupContext)
  const isChecked = selectedValue === value
  const isDisabled = groupDisabled || itemDisabled

  const handleChange = () => {
    if (!isDisabled) {
      onValueChange?.(value)
    }
  }

  return (
    <label
      className={`flex items-start space-x-3 ${isDisabled ? "cursor-not-allowed opacity-50" : "cursor-pointer"}`}
    >
      <div className="relative flex items-center">
        <input
          type="radio"
          name={name}
          value={value}
          checked={isChecked}
          onChange={handleChange}
          disabled={isDisabled}
          className="peer sr-only"
        />
        <div
          className={`
            h-5 w-5 rounded-full border-2 flex items-center justify-center
            transition-all duration-200
            peer-focus-visible:ring-2 peer-focus-visible:ring-ring peer-focus-visible:ring-offset-2
            ${
              error
                ? "border-destructive"
                : isChecked
                ? "border-primary"
                : "border-border hover:border-primary/50"
            }
          `}
        >
          {isChecked && (
            <div className={`h-2.5 w-2.5 rounded-full ${error ? "bg-destructive" : "bg-primary"}`} />
          )}
        </div>
      </div>

      <div className="flex-1 space-y-0.5">
        <span className={`text-sm font-medium leading-none ${error ? "text-destructive" : "text-foreground"}`}>
          {label}
        </span>
        {description && (
          <p className="text-xs text-muted-foreground leading-relaxed">{description}</p>
        )}
      </div>
    </label>
  )
}

RadioItem.displayName = "RadioItem"
