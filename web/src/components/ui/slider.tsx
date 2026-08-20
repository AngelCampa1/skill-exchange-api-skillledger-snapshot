import * as React from "react"

export interface SliderProps {
  label?: string
  helperText?: string
  showValue?: boolean
  formatValue?: (value: number) => string
  onValueChange?: (value: number) => void
  error?: boolean
  value?: number | string
  min?: number | string
  max?: number | string
  step?: number | string
  disabled?: boolean
  id?: string
  className?: string
  name?: string
}

export const Slider = React.forwardRef<HTMLInputElement, SliderProps>(
  ({
    className,
    label,
    helperText,
    showValue = false,
    formatValue,
    value,
    min = 0,
    max = 100,
    step = 1,
    onValueChange,
    error,
    disabled,
    id,
    name,
  }, ref) => {
    const [internalValue, setInternalValue] = React.useState(value || min)

    const currentValue = value !== undefined ? value : internalValue

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      const newValue = Number(e.target.value)
      if (value === undefined) {
        setInternalValue(newValue)
      }
      onValueChange?.(newValue)
    }

    const percentage = ((Number(currentValue) - Number(min)) / (Number(max) - Number(min))) * 100

    const displayValue = formatValue
      ? formatValue(Number(currentValue))
      : currentValue.toString()

    return (
      <div className={`w-full space-y-2 ${className || ""}`}>
        {(label || showValue) && (
          <div className="flex items-center justify-between">
            {label && (
              <label
                htmlFor={id}
                className={`text-sm font-medium ${error ? "text-destructive" : "text-foreground"}`}
              >
                {label}
              </label>
            )}
            {showValue && (
              <span className={`text-sm font-medium ${error ? "text-destructive" : "text-muted-foreground"}`}>
                {displayValue}
              </span>
            )}
          </div>
        )}

        <div className="relative">
          <input
            ref={ref}
            type="range"
            id={id}
            min={min}
            max={max}
            step={step}
            value={currentValue}
            onChange={handleChange}
            disabled={disabled}
            aria-invalid={error ? "true" : "false"}
            aria-describedby={helperText ? `${id}-helper` : undefined}
            className="sr-only peer"
            name={name}
          />

          {/* Track */}
          <div
            className={`
              h-2 w-full rounded-full
              ${error ? "bg-destructive/20" : "bg-muted"}
              ${disabled ? "opacity-50 cursor-not-allowed" : "cursor-pointer"}
            `}
            onClick={(e) => {
              if (disabled) return
              const rect = e.currentTarget.getBoundingClientRect()
              const x = e.clientX - rect.left
              const newPercentage = (x / rect.width) * 100
              const newValue = (newPercentage / 100) * (Number(max) - Number(min)) + Number(min)
              const steppedValue = Math.round(newValue / Number(step)) * Number(step)
              const clampedValue = Math.min(Math.max(steppedValue, Number(min)), Number(max))

              if (value === undefined) {
                setInternalValue(clampedValue)
              }
              onValueChange?.(clampedValue)
            }}
          >
            {/* Filled portion */}
            <div
              className={`h-full rounded-full transition-all duration-150 ${
                error ? "bg-destructive" : "bg-primary"
              }`}
              style={{ width: `${percentage}%` }}
            />
          </div>

          {/* Thumb */}
          <div
            className={`
              absolute top-1/2 -translate-y-1/2 -translate-x-1/2
              h-5 w-5 rounded-full shadow-md transition-all duration-150
              ${error ? "bg-destructive" : "bg-primary"}
              ${disabled ? "cursor-not-allowed" : "cursor-grab active:cursor-grabbing active:scale-110"}
              peer-focus-visible:ring-2 peer-focus-visible:ring-ring peer-focus-visible:ring-offset-2
            `}
            style={{ left: `${percentage}%` }}
          />
        </div>

        {helperText && (
          <p
            id={`${id}-helper`}
            className={`text-xs ${error ? "text-destructive" : "text-muted-foreground"}`}
            role={error ? "alert" : undefined}
          >
            {helperText}
          </p>
        )}
      </div>
    )
  }
)

Slider.displayName = "Slider"

// Range Slider for min-max selection
export interface RangeSliderProps {
  label?: string
  min?: number
  max?: number
  step?: number
  value?: [number, number]
  defaultValue?: [number, number]
  onValueChange?: (value: [number, number]) => void
  formatValue?: (value: number) => string
  disabled?: boolean
  className?: string
}

export const RangeSlider: React.FC<RangeSliderProps> = ({
  label,
  min = 0,
  max = 100,
  step = 1,
  value,
  defaultValue = [min, max],
  onValueChange,
  formatValue,
  disabled,
  className,
}) => {
  const [internalValue, setInternalValue] = React.useState<[number, number]>(defaultValue)

  const currentValue = value !== undefined ? value : internalValue
  const [minVal, maxVal] = currentValue

  const minPercentage = ((minVal - min) / (max - min)) * 100
  const maxPercentage = ((maxVal - min) / (max - min)) * 100

  const displayMin = formatValue ? formatValue(minVal) : minVal
  const displayMax = formatValue ? formatValue(maxVal) : maxVal

  return (
    <div className={`w-full space-y-2 ${className || ""}`}>
      {label && (
        <div className="flex items-center justify-between">
          <label className="text-sm font-medium text-foreground">{label}</label>
          <span className="text-sm font-medium text-muted-foreground">
            {displayMin} - {displayMax}
          </span>
        </div>
      )}

      <div className="relative h-2">
        {/* Track */}
        <div className={`absolute inset-0 rounded-full bg-muted ${disabled ? "opacity-50" : ""}`} />

        {/* Filled range */}
        <div
          className="absolute h-full rounded-full bg-primary"
          style={{
            left: `${minPercentage}%`,
            width: `${maxPercentage - minPercentage}%`
          }}
        />
      </div>
    </div>
  )
}

RangeSlider.displayName = "RangeSlider"
