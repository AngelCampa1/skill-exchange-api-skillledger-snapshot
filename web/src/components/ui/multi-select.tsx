import * as React from "react"
import { Check, X, ChevronDown } from "lucide-react"

export interface MultiSelectOption {
  value: string
  label: string
  description?: string
  disabled?: boolean
}

export interface MultiSelectProps {
  options: MultiSelectOption[]
  value?: string[]
  onValueChange?: (value: string[]) => void
  placeholder?: string
  searchPlaceholder?: string
  emptyMessage?: string
  maxSelected?: number
  disabled?: boolean
  label?: string
  error?: boolean
  helperText?: string
  className?: string
  showSelectedCount?: boolean
}

export const MultiSelect = React.forwardRef<HTMLDivElement, MultiSelectProps>(
  (
    {
      options,
      value = [],
      onValueChange,
      placeholder = "Select options...",
      searchPlaceholder = "Search...",
      emptyMessage = "No results found.",
      maxSelected,
      disabled,
      label,
      error,
      helperText,
      className,
      showSelectedCount = true,
    },
    ref
  ) => {
    const [isOpen, setIsOpen] = React.useState(false)
    const [search, setSearch] = React.useState("")
    const containerRef = React.useRef<HTMLDivElement>(null)
    const searchInputRef = React.useRef<HTMLInputElement>(null)

    const selectedOptions = options.filter((opt) => value.includes(opt.value))

    const filteredOptions = React.useMemo(() => {
      if (!search) return options

      const searchLower = search.toLowerCase()
      return options.filter(
        (option) =>
          option.label.toLowerCase().includes(searchLower) ||
          option.description?.toLowerCase().includes(searchLower)
      )
    }, [options, search])

    React.useEffect(() => {
      const handleClickOutside = (event: MouseEvent) => {
        if (
          containerRef.current &&
          !containerRef.current.contains(event.target as Node)
        ) {
          setIsOpen(false)
          setSearch("")
        }
      }

      const handleEscape = (event: KeyboardEvent) => {
        if (event.key === "Escape") {
          setIsOpen(false)
          setSearch("")
        }
      }

      if (isOpen) {
        document.addEventListener("mousedown", handleClickOutside)
        document.addEventListener("keydown", handleEscape)
        searchInputRef.current?.focus()
      }

      return () => {
        document.removeEventListener("mousedown", handleClickOutside)
        document.removeEventListener("keydown", handleEscape)
      }
    }, [isOpen])

    const handleToggle = (optionValue: string) => {
      const newValue = value.includes(optionValue)
        ? value.filter((v) => v !== optionValue)
        : maxSelected && value.length >= maxSelected
        ? value
        : [...value, optionValue]

      onValueChange?.(newValue)
    }

    const handleRemove = (optionValue: string, e: React.MouseEvent) => {
      e.stopPropagation()
      onValueChange?.(value.filter((v) => v !== optionValue))
    }

    const handleClearAll = (e: React.MouseEvent) => {
      e.stopPropagation()
      onValueChange?.([])
    }

    const isSelected = (optionValue: string) => value.includes(optionValue)
    const isMaxReached = maxSelected !== undefined && value.length >= maxSelected

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
              w-full flex items-center justify-between min-h-12 px-4 py-2 rounded-xl
              border text-sm transition-all duration-200 shadow-sm
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
            aria-haspopup="listbox"
            aria-expanded={isOpen}
          >
            <div className="flex-1 flex flex-wrap gap-2 items-center text-left">
              {selectedOptions.length === 0 ? (
                <span className="text-muted-foreground">{placeholder}</span>
              ) : (
                <>
                  {selectedOptions.slice(0, 3).map((option) => (
                    <span
                      key={option.value}
                      className="inline-flex items-center gap-1 px-2 py-1 bg-accent text-accent-foreground rounded-md text-xs font-medium"
                    >
                      {option.label}
                      <button
                        type="button"
                        onClick={(e) => handleRemove(option.value, e)}
                        className="hover:bg-accent-foreground/20 rounded-full p-0.5"
                        disabled={disabled}
                      >
                        <X className="h-3 w-3" />
                      </button>
                    </span>
                  ))}
                  {selectedOptions.length > 3 && (
                    <span className="text-xs text-muted-foreground">
                      +{selectedOptions.length - 3} more
                    </span>
                  )}
                </>
              )}
            </div>
            <div className="flex items-center space-x-1 ml-2">
              {selectedOptions.length > 0 && !disabled && (
                <button
                  type="button"
                  onClick={handleClearAll}
                  className="p-1 hover:bg-muted rounded-full"
                >
                  <X className="h-4 w-4 text-muted-foreground" />
                </button>
              )}
              <ChevronDown
                className={`h-4 w-4 text-muted-foreground transition-transform duration-200 ${
                  isOpen ? "rotate-180" : ""
                }`}
                aria-hidden="true"
              />
            </div>
          </button>

          {/* Dropdown */}
          {isOpen && (
            <div
              role="listbox"
              aria-multiselectable="true"
              className="absolute z-50 w-full mt-2 bg-background border border-border rounded-xl shadow-lg overflow-hidden animate-in fade-in-0 zoom-in-95"
            >
              {/* Search Input */}
              <div className="p-2 border-b border-border">
                <input
                  ref={searchInputRef}
                  type="text"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder={searchPlaceholder}
                  className="w-full h-10 px-3 bg-background border border-border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                />
              </div>

              {/* Header with count and clear button */}
              {showSelectedCount && value.length > 0 && (
                <div className="px-3 py-2 border-b border-border flex items-center justify-between">
                  <span className="text-xs text-muted-foreground">
                    {value.length} selected
                    {maxSelected && ` (max ${maxSelected})`}
                  </span>
                  <button
                    type="button"
                    onClick={handleClearAll}
                    className="text-xs text-primary hover:underline"
                  >
                    Clear all
                  </button>
                </div>
              )}

              {/* Options List */}
              <div className="max-h-64 overflow-y-auto p-1">
                {filteredOptions.length === 0 ? (
                  <div className="px-3 py-8 text-center text-sm text-muted-foreground">
                    {emptyMessage}
                  </div>
                ) : (
                  filteredOptions.map((option) => {
                    const selected = isSelected(option.value)
                    const isDisabled =
                      option.disabled || (!selected && isMaxReached)

                    return (
                      <button
                        key={option.value}
                        type="button"
                        role="option"
                        aria-selected={selected}
                        onClick={() => !isDisabled && handleToggle(option.value)}
                        disabled={isDisabled}
                        className={`
                          w-full flex items-center justify-between px-3 py-2.5 rounded-lg
                          text-sm transition-colors
                          ${
                            isDisabled
                              ? "opacity-50 cursor-not-allowed"
                              : selected
                              ? "bg-accent text-accent-foreground"
                              : "hover:bg-accent hover:text-accent-foreground"
                          }
                        `}
                      >
                        <div className="flex-1 text-left space-y-0.5">
                          <div className="font-medium">{option.label}</div>
                          {option.description && (
                            <div className="text-xs text-muted-foreground">
                              {option.description}
                            </div>
                          )}
                        </div>
                        {selected && (
                          <Check
                            className="h-4 w-4 flex-shrink-0 ml-2"
                            aria-hidden="true"
                          />
                        )}
                      </button>
                    )
                  })
                )}
              </div>
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

MultiSelect.displayName = "MultiSelect"
