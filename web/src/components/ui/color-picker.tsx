import * as React from "react"
import { Check } from "lucide-react"

export interface ColorPickerProps {
  value?: string
  onValueChange?: (color: string) => void
  presetColors?: string[]
  showInput?: boolean
  label?: string
  helperText?: string
  error?: boolean
  disabled?: boolean
  className?: string
}

const DEFAULT_PRESET_COLORS = [
  "#EF4444", // red
  "#F97316", // orange
  "#F59E0B", // amber
  "#EAB308", // yellow
  "#84CC16", // lime
  "#22C55E", // green
  "#10B981", // emerald
  "#14B8A6", // teal
  "#06B6D4", // cyan
  "#0EA5E9", // sky
  "#3B82F6", // blue
  "#6366F1", // indigo
  "#8B5CF6", // violet
  "#A855F7", // purple
  "#D946EF", // fuchsia
  "#EC4899", // pink
  "#F43F5E", // rose
  "#64748B", // slate
  "#6B7280", // gray
  "#000000", // black
]

export const ColorPicker = React.forwardRef<HTMLDivElement, ColorPickerProps>(
  (
    {
      value = "#3B82F6",
      onValueChange,
      presetColors = DEFAULT_PRESET_COLORS,
      showInput = true,
      label,
      helperText,
      error,
      disabled,
      className,
    },
    ref
  ) => {
    const [isOpen, setIsOpen] = React.useState(false)
    const [customColor, setCustomColor] = React.useState(value)
    const containerRef = React.useRef<HTMLDivElement>(null)

    React.useEffect(() => {
      setCustomColor(value)
    }, [value])

    React.useEffect(() => {
      const handleClickOutside = (event: MouseEvent) => {
        if (
          containerRef.current &&
          !containerRef.current.contains(event.target as Node)
        ) {
          setIsOpen(false)
        }
      }

      const handleEscape = (event: KeyboardEvent) => {
        if (event.key === "Escape") {
          setIsOpen(false)
        }
      }

      if (isOpen) {
        document.addEventListener("mousedown", handleClickOutside)
        document.addEventListener("keydown", handleEscape)
      }

      return () => {
        document.removeEventListener("mousedown", handleClickOutside)
        document.removeEventListener("keydown", handleEscape)
      }
    }, [isOpen])

    const handleColorSelect = (color: string) => {
      setCustomColor(color)
      onValueChange?.(color)
    }

    const handleCustomColorChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      const newColor = e.target.value
      setCustomColor(newColor)
      onValueChange?.(newColor)
    }

    const isValidHexColor = (color: string) => {
      return /^#[0-9A-F]{6}$/i.test(color)
    }

    return (
      <div ref={ref} className={`w-full space-y-2 ${className || ""}`}>
        {label && (
          <label
            className={`text-sm font-medium ${
              error ? "text-destructive" : "text-foreground"
            }`}
          >
            {label}
          </label>
        )}

        <div ref={containerRef} className="relative">
          {/* Trigger Button */}
          <button
            type="button"
            onClick={() => !disabled && setIsOpen(!isOpen)}
            disabled={disabled}
            className={`
              w-full flex items-center space-x-3 h-12 px-4 py-3 rounded-xl
              border text-sm font-medium transition-all duration-200 shadow-sm
              ${
                error
                  ? "border-destructive focus:ring-destructive/20"
                  : "border-border focus:ring-primary/20 focus:border-primary"
              }
              ${
                disabled
                  ? "bg-muted cursor-not-allowed opacity-50"
                  : "bg-background hover:border-primary/50 focus:outline-none focus:ring-2"
              }
            `}
            aria-haspopup="dialog"
            aria-expanded={isOpen}
          >
            <div
              className="w-8 h-8 rounded-lg border-2 border-border flex-shrink-0 shadow-sm"
              style={{ backgroundColor: value }}
              aria-hidden="true"
            />
            <span className="flex-1 text-left text-foreground font-mono uppercase">
              {value}
            </span>
          </button>

          {/* Color Picker Dropdown */}
          {isOpen && (
            <div
              role="dialog"
              aria-label="Color picker"
              className="absolute z-50 w-full mt-2 p-4 bg-background border border-border rounded-xl shadow-lg animate-in fade-in-0 zoom-in-95"
            >
              {/* Preset Colors Grid */}
              <div className="grid grid-cols-5 gap-2 mb-4">
                {presetColors.map((color) => (
                  <button
                    key={color}
                    type="button"
                    onClick={() => handleColorSelect(color)}
                    className={`
                      relative w-full aspect-square rounded-lg transition-all hover:scale-110
                      ${
                        value === color
                          ? "ring-2 ring-primary ring-offset-2 ring-offset-background"
                          : "hover:ring-2 hover:ring-muted"
                      }
                    `}
                    style={{ backgroundColor: color }}
                    title={color}
                    aria-label={`Select color ${color}`}
                  >
                    {value === color && (
                      <Check
                        className="absolute inset-0 m-auto h-4 w-4 text-white drop-shadow-md"
                        aria-hidden="true"
                      />
                    )}
                  </button>
                ))}
              </div>

              {/* Custom Color Input */}
              {showInput && (
                <div className="space-y-2 pt-3 border-t border-border">
                  <label className="text-xs font-medium text-muted-foreground">
                    Custom Color
                  </label>
                  <div className="flex items-center space-x-2">
                    <input
                      type="color"
                      value={customColor}
                      onChange={handleCustomColorChange}
                      className="w-12 h-10 rounded-lg border border-border cursor-pointer bg-transparent"
                      disabled={disabled}
                    />
                    <input
                      type="text"
                      value={customColor}
                      onChange={(e) => {
                        const newValue = e.target.value
                        setCustomColor(newValue)
                        if (isValidHexColor(newValue)) {
                          onValueChange?.(newValue)
                        }
                      }}
                      placeholder="#000000"
                      maxLength={7}
                      className={`
                        flex-1 h-10 px-3 rounded-lg border font-mono uppercase text-sm
                        ${
                          !isValidHexColor(customColor)
                            ? "border-destructive"
                            : "border-border"
                        }
                        bg-background focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary
                      `}
                      disabled={disabled}
                    />
                  </div>
                  {!isValidHexColor(customColor) && (
                    <p className="text-xs text-destructive">
                      Invalid hex color format (e.g., #FF0000)
                    </p>
                  )}
                </div>
              )}
            </div>
          )}
        </div>

        {helperText && (
          <p
            className={`text-xs ${
              error ? "text-destructive" : "text-muted-foreground"
            }`}
          >
            {helperText}
          </p>
        )}
      </div>
    )
  }
)

ColorPicker.displayName = "ColorPicker"

// Simple color swatch component for read-only display
export interface ColorSwatchProps {
  color: string
  size?: "sm" | "md" | "lg"
  showLabel?: boolean
  className?: string
}

export const ColorSwatch: React.FC<ColorSwatchProps> = ({
  color,
  size = "md",
  showLabel = false,
  className,
}) => {
  const sizeClasses = {
    sm: "w-6 h-6",
    md: "w-8 h-8",
    lg: "w-12 h-12",
  }

  return (
    <div className={`inline-flex items-center space-x-2 ${className || ""}`}>
      <div
        className={`${sizeClasses[size]} rounded-lg border-2 border-border shadow-sm flex-shrink-0`}
        style={{ backgroundColor: color }}
        aria-label={`Color ${color}`}
      />
      {showLabel && (
        <span className="text-sm font-mono uppercase text-foreground">{color}</span>
      )}
    </div>
  )
}

ColorSwatch.displayName = "ColorSwatch"

// Color palette component for displaying multiple colors
export interface ColorPaletteProps {
  colors: string[]
  selectedColor?: string
  onColorSelect?: (color: string) => void
  size?: "sm" | "md" | "lg"
  columns?: number
  className?: string
}

export const ColorPalette: React.FC<ColorPaletteProps> = ({
  colors,
  selectedColor,
  onColorSelect,
  size = "md",
  columns = 5,
  className,
}) => {
  const sizeClasses = {
    sm: "w-8 h-8",
    md: "w-10 h-10",
    lg: "w-12 h-12",
  }

  return (
    <div
      className={`grid gap-2 ${className || ""}`}
      style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }}
    >
      {colors.map((color) => {
        const isSelected = selectedColor === color
        const isClickable = !!onColorSelect

        return (
          <button
            key={color}
            type="button"
            onClick={() => onColorSelect?.(color)}
            disabled={!isClickable}
            className={`
              ${sizeClasses[size]} rounded-lg transition-all
              ${isClickable ? "cursor-pointer hover:scale-110" : "cursor-default"}
              ${
                isSelected
                  ? "ring-2 ring-primary ring-offset-2 ring-offset-background"
                  : isClickable
                  ? "hover:ring-2 hover:ring-muted"
                  : ""
              }
            `}
            style={{ backgroundColor: color }}
            title={color}
            aria-label={`${isSelected ? "Selected color" : "Select color"} ${color}`}
            aria-pressed={isSelected}
          >
            {isSelected && (
              <Check
                className="w-full h-full p-1.5 text-white drop-shadow-md"
                aria-hidden="true"
              />
            )}
          </button>
        )
      })}
    </div>
  )
}

ColorPalette.displayName = "ColorPalette"
