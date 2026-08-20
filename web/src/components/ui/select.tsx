import * as React from "react"
import { ChevronDown } from "lucide-react"

interface SelectProps extends React.HTMLAttributes<HTMLDivElement> {
  onValueChange?: (value: string) => void
  value?: string
  defaultValue?: string
  placeholder?: string
  disabled?: boolean
}

interface SelectTriggerProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {}

interface SelectContentProps extends React.HTMLAttributes<HTMLDivElement> {}

interface SelectItemProps extends React.HTMLAttributes<HTMLDivElement> {
  value: string
}

interface SelectValueProps extends React.HTMLAttributes<HTMLSpanElement> {
  placeholder?: string
}

const SelectContext = React.createContext<{
  value?: string
  onValueChange?: (value: string) => void
  open: boolean
  setOpen: (open: boolean) => void
}>({
  open: false,
  setOpen: () => {}
})

const Select: React.FC<SelectProps> = ({ 
  children, 
  onValueChange, 
  value, 
  defaultValue,
  ...props 
}) => {
  const [open, setOpen] = React.useState(false)
  const [internalValue, setInternalValue] = React.useState(defaultValue || "")
  
  const currentValue = value !== undefined ? value : internalValue
  
  const handleValueChange = (newValue: string) => {
    if (value === undefined) {
      setInternalValue(newValue)
    }
    onValueChange?.(newValue)
    setOpen(false)
  }

  return (
    <SelectContext.Provider value={{ 
      value: currentValue, 
      onValueChange: handleValueChange, 
      open, 
      setOpen 
    }}>
      <div className="relative" {...props}>
        {children}
      </div>
    </SelectContext.Provider>
  )
}

const SelectTrigger = React.forwardRef<HTMLButtonElement, SelectTriggerProps>(
  ({ className, children, id, ...props }, ref) => {
    const { open, setOpen } = React.useContext(SelectContext)
    const contentId = `${id || 'select'}-content`

    return (
      <button
        ref={ref}
        type="button"
        role="combobox"
        aria-expanded={open}
        aria-haspopup="listbox"
        aria-controls={contentId}
        className={`flex h-12 w-full items-center justify-between rounded-full border border-border bg-background px-4 py-3 text-sm text-foreground ring-offset-background placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary disabled:cursor-not-allowed disabled:opacity-50 transition-all duration-200 shadow-sm ${className || ""}`}
        onClick={() => setOpen(!open)}
        {...props}
      >
        {children}
        <ChevronDown className={`h-4 w-4 text-muted-foreground transition-transform duration-200 ${open ? 'rotate-180' : ''}`} aria-hidden="true" />
      </button>
    )
  }
)
SelectTrigger.displayName = "SelectTrigger"

const SelectContent = React.forwardRef<HTMLDivElement, SelectContentProps>(
  ({ className, children, ...props }, ref) => {
    const { open, setOpen } = React.useContext(SelectContext)

    React.useEffect(() => {
      const handleEscape = (e: KeyboardEvent) => {
        if (e.key === "Escape" && open) {
          setOpen(false)
        }
      }

      if (open) {
        document.addEventListener("keydown", handleEscape)
      }

      return () => {
        document.removeEventListener("keydown", handleEscape)
      }
    }, [open, setOpen])

    if (!open) return null

    return (
      <div
        ref={ref}
        role="listbox"
        className={`absolute z-50 mt-1 min-w-[8rem] w-full overflow-hidden rounded-xl border border-border bg-background text-foreground shadow-lg animate-in fade-in-0 zoom-in-95 ${className || ""}`}
        {...props}
      >
        <div className="p-1">
          {children}
        </div>
      </div>
    )
  }
)
SelectContent.displayName = "SelectContent"

const SelectItem = React.forwardRef<HTMLDivElement, SelectItemProps>(
  ({ className, children, value, ...props }, ref) => {
    const { onValueChange, value: selectedValue } = React.useContext(SelectContext)
    const isSelected = selectedValue === value

    return (
      <div
        ref={ref}
        role="option"
        aria-selected={isSelected}
        className={`relative flex w-full cursor-pointer select-none items-center rounded-lg py-2.5 px-3 text-sm outline-none transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground data-[disabled]:pointer-events-none data-[disabled]:opacity-50 ${isSelected ? 'bg-accent/50 font-medium' : ''} ${className || ""}`}
        onClick={() => onValueChange?.(value)}
        {...props}
      >
        {children}
      </div>
    )
  }
)
SelectItem.displayName = "SelectItem"

const SelectValue = React.forwardRef<HTMLSpanElement, SelectValueProps>(
  ({ className, placeholder, ...props }, ref) => {
    const { value } = React.useContext(SelectContext)
    
    return (
      <span
        ref={ref}
        className={className}
        {...props}
      >
        {value || placeholder}
      </span>
    )
  }
)
SelectValue.displayName = "SelectValue"

export { Select, SelectContent, SelectItem, SelectTrigger, SelectValue }