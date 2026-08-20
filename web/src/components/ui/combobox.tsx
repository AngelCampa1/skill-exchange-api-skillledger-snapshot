import * as React from "react"
import { Check, ChevronDown, Search, X } from "lucide-react"

export interface ComboboxOption {
  value: string
  label: string
  description?: string
  disabled?: boolean
}

export interface ComboboxProps {
  options: ComboboxOption[]
  value?: string
  onValueChange?: (value: string) => void
  placeholder?: string
  searchPlaceholder?: string
  emptyMessage?: string
  disabled?: boolean
  label?: string
  error?: boolean
  helperText?: string
  className?: string
  allowClear?: boolean
}

export const Combobox = React.forwardRef<HTMLDivElement, ComboboxProps>(
  ({
    options,
    value,
    onValueChange,
    placeholder = "Select option...",
    searchPlaceholder = "Search...",
    emptyMessage = "No results found.",
    disabled,
    label,
    error,
    helperText,
    className,
    allowClear = false,
  }, ref) => {
    const [isOpen, setIsOpen] = React.useState(false)
    const [search, setSearch] = React.useState("")
    const containerRef = React.useRef<HTMLDivElement>(null)
    const searchInputRef = React.useRef<HTMLInputElement>(null)

    const selectedOption = options.find((opt) => opt.value === value)

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

    const handleSelect = (optionValue: string) => {
      onValueChange?.(optionValue)
      setIsOpen(false)
      setSearch("")
    }

    const handleClear = (e: React.MouseEvent) => {
      e.stopPropagation()
      onValueChange?.("")
    }

    return (
      <div ref={ref} className={`w-full space-y-2 ${className || ""}`}>
        {label && (
          <label className={`text-sm font-medium ${error ? "text-destructive" : "text-foreground"}`}>
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
              w-full flex items-center justify-between h-12 px-4 py-3 rounded-xl
              border text-sm font-medium transition-all duration-200 shadow-sm
              ${error
                ? "border-destructive focus:ring-destructive/20"
                : "border-border focus:ring-primary/20 focus:border-primary"
              }
              ${disabled
                ? "bg-muted cursor-not-allowed opacity-50"
                : "bg-background hover:border-primary/50 focus:outline-none focus:ring-2"
              }
            `}
            aria-haspopup="listbox"
            aria-expanded={isOpen}
          >
            <span className={selectedOption ? "text-foreground" : "text-muted-foreground"}>
              {selectedOption?.label || placeholder}
            </span>
            <div className="flex items-center space-x-1">
              {allowClear && value && !disabled && (
                <X
                  className="h-4 w-4 text-muted-foreground hover:text-foreground"
                  onClick={handleClear}
                />
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
              className="absolute z-50 w-full mt-2 bg-background border border-border rounded-xl shadow-lg overflow-hidden animate-in fade-in-0 zoom-in-95"
            >
              {/* Search Input */}
              <div className="p-2 border-b border-border">
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <input
                    ref={searchInputRef}
                    type="text"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    placeholder={searchPlaceholder}
                    className="w-full h-10 pl-10 pr-4 bg-background border border-border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                  />
                </div>
              </div>

              {/* Options List */}
              <div className="max-h-64 overflow-y-auto p-1">
                {filteredOptions.length === 0 ? (
                  <div className="px-3 py-8 text-center text-sm text-muted-foreground">
                    {emptyMessage}
                  </div>
                ) : (
                  filteredOptions.map((option) => {
                    const isSelected = option.value === value

                    return (
                      <button
                        key={option.value}
                        type="button"
                        role="option"
                        aria-selected={isSelected}
                        onClick={() => !option.disabled && handleSelect(option.value)}
                        disabled={option.disabled}
                        className={`
                          w-full flex items-center justify-between px-3 py-2.5 rounded-lg
                          text-sm transition-colors
                          ${option.disabled
                            ? "opacity-50 cursor-not-allowed"
                            : isSelected
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
                        {isSelected && (
                          <Check className="h-4 w-4 flex-shrink-0 ml-2" aria-hidden="true" />
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
          <p className={`text-xs ${error ? "text-destructive" : "text-muted-foreground"}`}>
            {helperText}
          </p>
        )}
      </div>
    )
  }
)

Combobox.displayName = "Combobox"
