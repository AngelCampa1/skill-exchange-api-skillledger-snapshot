import * as React from "react"
import { ChevronLeft, ChevronRight, Calendar as CalendarIcon } from "lucide-react"

export interface DatePickerProps {
  value?: Date
  onChange?: (date: Date | undefined) => void
  minDate?: Date
  maxDate?: Date
  disabled?: boolean
  label?: string
  placeholder?: string
  error?: boolean
  helperText?: string
  className?: string
}

export const DatePicker = React.forwardRef<HTMLDivElement, DatePickerProps>(
  (
    {
      value,
      onChange,
      minDate,
      maxDate,
      disabled = false,
      label,
      placeholder = "Select date...",
      error,
      helperText,
      className,
    },
    ref
  ) => {
    const [isOpen, setIsOpen] = React.useState(false)
    const [currentMonth, setCurrentMonth] = React.useState(value || new Date())
    const containerRef = React.useRef<HTMLDivElement>(null)

    React.useEffect(() => {
      const handleClickOutside = (event: MouseEvent) => {
        if (
          containerRef.current &&
          !containerRef.current.contains(event.target as Node)
        ) {
          setIsOpen(false)
        }
      }

      if (isOpen) {
        document.addEventListener("mousedown", handleClickOutside)
      }

      return () => {
        document.removeEventListener("mousedown", handleClickOutside)
      }
    }, [isOpen])

    const daysInMonth = (date: Date) => {
      return new Date(date.getFullYear(), date.getMonth() + 1, 0).getDate()
    }

    const firstDayOfMonth = (date: Date) => {
      return new Date(date.getFullYear(), date.getMonth(), 1).getDay()
    }

    const isDateDisabled = (date: Date) => {
      if (minDate && date < minDate) return true
      if (maxDate && date > maxDate) return true
      return false
    }

    const isSameDay = (date1: Date | undefined, date2: Date) => {
      if (!date1) return false
      return (
        date1.getDate() === date2.getDate() &&
        date1.getMonth() === date2.getMonth() &&
        date1.getFullYear() === date2.getFullYear()
      )
    }

    const handleDateSelect = (day: number) => {
      const selectedDate = new Date(
        currentMonth.getFullYear(),
        currentMonth.getMonth(),
        day
      )
      if (!isDateDisabled(selectedDate)) {
        onChange?.(selectedDate)
        setIsOpen(false)
      }
    }

    const handlePreviousMonth = () => {
      setCurrentMonth(
        new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1)
      )
    }

    const handleNextMonth = () => {
      setCurrentMonth(
        new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1)
      )
    }

    const formatDate = (date: Date | undefined) => {
      if (!date) return placeholder
      return date.toLocaleDateString(undefined, {
        year: "numeric",
        month: "long",
        day: "numeric",
      })
    }

    const monthName = currentMonth.toLocaleString("default", {
      month: "long",
      year: "numeric",
    })

    const days = []
    const totalDays = daysInMonth(currentMonth)
    const startDay = firstDayOfMonth(currentMonth)

    // Add empty cells for days before the first day of the month
    for (let i = 0; i < startDay; i++) {
      days.push(<div key={`empty-${i}`} />)
    }

    // Add cells for each day of the month
    for (let day = 1; day <= totalDays; day++) {
      const date = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), day)
      const isDisabled = isDateDisabled(date)
      const isSelected = isSameDay(value, date)
      const isToday = isSameDay(new Date(), date)

      days.push(
        <button
          key={day}
          type="button"
          onClick={() => handleDateSelect(day)}
          disabled={isDisabled}
          className={`
            aspect-square rounded-lg text-sm font-medium transition-colors
            ${
              isSelected
                ? "bg-primary text-primary-foreground"
                : isToday
                ? "bg-accent text-accent-foreground ring-1 ring-primary"
                : "text-foreground hover:bg-accent"
            }
            ${isDisabled ? "opacity-30 cursor-not-allowed" : ""}
          `}
        >
          {day}
        </button>
      )
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
          <button
            type="button"
            onClick={() => !disabled && setIsOpen(!isOpen)}
            disabled={disabled}
            className={`
              w-full flex items-center justify-between h-12 px-4 py-3 rounded-full
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
          >
            <span className={value ? "text-foreground" : "text-muted-foreground"}>
              {formatDate(value)}
            </span>
            <CalendarIcon className="h-5 w-5 text-muted-foreground" aria-hidden="true" />
          </button>

          {isOpen && (
            <div className="absolute z-50 mt-2 p-4 bg-background border border-border rounded-xl shadow-lg animate-in fade-in-0 zoom-in-95">
              {/* Month Navigation */}
              <div className="flex items-center justify-between mb-4">
                <button
                  type="button"
                  onClick={handlePreviousMonth}
                  className="p-2 hover:bg-accent rounded-full transition-colors"
                  aria-label="Previous month"
                >
                  <ChevronLeft className="h-5 w-5" aria-hidden="true" />
                </button>
                <span className="text-sm font-semibold text-foreground">{monthName}</span>
                <button
                  type="button"
                  onClick={handleNextMonth}
                  className="p-2 hover:bg-accent rounded-full transition-colors"
                  aria-label="Next month"
                >
                  <ChevronRight className="h-5 w-5" aria-hidden="true" />
                </button>
              </div>

              {/* Weekday Headers */}
              <div className="grid grid-cols-7 gap-1 mb-2">
                {["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"].map((day) => (
                  <div
                    key={day}
                    className="aspect-square flex items-center justify-center text-xs font-medium text-muted-foreground"
                  >
                    {day}
                  </div>
                ))}
              </div>

              {/* Calendar Days */}
              <div className="grid grid-cols-7 gap-1">{days}</div>

              {/* Today Button */}
              <div className="mt-4 pt-4 border-t border-border">
                <button
                  type="button"
                  onClick={() => {
                    const today = new Date()
                    onChange?.(today)
                    setCurrentMonth(today)
                    setIsOpen(false)
                  }}
                  className="w-full px-3 py-2 text-sm font-medium text-primary hover:bg-accent rounded-full transition-colors"
                >
                  Today
                </button>
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

DatePicker.displayName = "DatePicker"

// Date Range Picker
export interface DateRangePickerProps {
  startDate?: Date
  endDate?: Date
  onChange?: (startDate: Date | undefined, endDate: Date | undefined) => void
  minDate?: Date
  maxDate?: Date
  disabled?: boolean
  label?: string
  className?: string
}

export const DateRangePicker = React.forwardRef<HTMLDivElement, DateRangePickerProps>(
  (
    {
      startDate,
      endDate,
      onChange,
      minDate,
      maxDate,
      disabled = false,
      label,
      className,
    },
    ref
  ) => {
    const [tempStartDate, setTempStartDate] = React.useState(startDate)

    const handleStartDateChange = (date: Date | undefined) => {
      setTempStartDate(date)
      if (!date) {
        onChange?.(undefined, undefined)
      }
    }

    const handleEndDateChange = (date: Date | undefined) => {
      if (tempStartDate && date) {
        onChange?.(tempStartDate, date)
      }
      setTempStartDate(undefined)
    }

    return (
      <div ref={ref} className={`space-y-4 ${className || ""}`}>
        {label && <label className="text-sm font-medium text-foreground">{label}</label>}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <DatePicker
            label="Start Date"
            value={startDate}
            onChange={handleStartDateChange}
            minDate={minDate}
            maxDate={endDate || maxDate}
            disabled={disabled}
          />
          <DatePicker
            label="End Date"
            value={endDate}
            onChange={handleEndDateChange}
            minDate={tempStartDate || startDate || minDate}
            maxDate={maxDate}
            disabled={disabled || !tempStartDate}
          />
        </div>
      </div>
    )
  }
)

DateRangePicker.displayName = "DateRangePicker"
