import * as React from "react"
import { ChevronDown } from "lucide-react"

interface AccordionContextValue {
  openItems: Set<string>
  toggleItem: (value: string) => void
  type: "single" | "multiple"
}

const AccordionContext = React.createContext<AccordionContextValue | null>(null)

const useAccordion = () => {
  const context = React.useContext(AccordionContext)
  if (!context) {
    throw new Error("Accordion components must be used within an Accordion")
  }
  return context
}

export interface AccordionProps extends React.HTMLAttributes<HTMLDivElement> {
  type?: "single" | "multiple"
  defaultValue?: string | string[]
  value?: string | string[]
  onValueChange?: (value: string | string[]) => void
}

export const Accordion: React.FC<AccordionProps> = ({
  children,
  type = "single",
  defaultValue,
  value,
  onValueChange,
  className,
  ...props
}) => {
  const [internalOpenItems, setInternalOpenItems] = React.useState<Set<string>>(() => {
    if (defaultValue) {
      return new Set(Array.isArray(defaultValue) ? defaultValue : [defaultValue])
    }
    return new Set()
  })

  const openItems = React.useMemo(() => {
    if (value !== undefined) {
      return new Set(Array.isArray(value) ? value : [value])
    }
    return internalOpenItems
  }, [value, internalOpenItems])

  const toggleItem = React.useCallback(
    (itemValue: string) => {
      const newOpenItems = new Set(openItems)

      if (type === "single") {
        if (newOpenItems.has(itemValue)) {
          newOpenItems.delete(itemValue)
        } else {
          newOpenItems.clear()
          newOpenItems.add(itemValue)
        }
      } else {
        if (newOpenItems.has(itemValue)) {
          newOpenItems.delete(itemValue)
        } else {
          newOpenItems.add(itemValue)
        }
      }

      if (value === undefined) {
        setInternalOpenItems(newOpenItems)
      }

      onValueChange?.(
        type === "single"
          ? (newOpenItems.size > 0 ? Array.from(newOpenItems)[0] : "")
          : Array.from(newOpenItems)
      )
    },
    [openItems, type, value, onValueChange]
  )

  return (
    <AccordionContext.Provider value={{ openItems, toggleItem, type }}>
      <div className={`divide-y divide-border ${className || ""}`} {...props}>
        {children}
      </div>
    </AccordionContext.Provider>
  )
}

export interface AccordionItemProps extends React.HTMLAttributes<HTMLDivElement> {
  value: string
}

export const AccordionItem: React.FC<AccordionItemProps> = ({
  value,
  children,
  className,
  ...props
}) => {
  const { openItems } = useAccordion()
  const isOpen = openItems.has(value)

  return (
    <div
      data-state={isOpen ? "open" : "closed"}
      className={className}
      {...props}
    >
      {children}
    </div>
  )
}

export interface AccordionTriggerProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  value: string
}

export const AccordionTrigger = React.forwardRef<HTMLButtonElement, AccordionTriggerProps>(
  ({ value, children, className, ...props }, ref) => {
    const { openItems, toggleItem } = useAccordion()
    const isOpen = openItems.has(value)

    return (
      <button
        ref={ref}
        type="button"
        aria-expanded={isOpen}
        className={`flex w-full items-center justify-between py-4 px-1 text-left font-medium text-foreground transition-all hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 ${className || ""}`}
        onClick={() => toggleItem(value)}
        {...props}
      >
        {children}
        <ChevronDown
          className={`h-4 w-4 flex-shrink-0 text-muted-foreground transition-transform duration-200 ${
            isOpen ? "rotate-180" : ""
          }`}
          aria-hidden="true"
        />
      </button>
    )
  }
)
AccordionTrigger.displayName = "AccordionTrigger"

export interface AccordionContentProps extends React.HTMLAttributes<HTMLDivElement> {
  value: string
}

export const AccordionContent = React.forwardRef<HTMLDivElement, AccordionContentProps>(
  ({ value, children, className, ...props }, ref) => {
    const { openItems } = useAccordion()
    const isOpen = openItems.has(value)

    return (
      <div
        ref={ref}
        role="region"
        aria-hidden={!isOpen}
        className={`overflow-hidden transition-all duration-200 ${
          isOpen ? "animate-in slide-in-from-top-1" : "hidden"
        }`}
        {...props}
      >
        <div className={`pb-4 pt-0 px-1 text-sm text-muted-foreground ${className || ""}`}>
          {children}
        </div>
      </div>
    )
  }
)
AccordionContent.displayName = "AccordionContent"
