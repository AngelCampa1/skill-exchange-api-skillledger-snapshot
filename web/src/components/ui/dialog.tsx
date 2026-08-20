import * as React from "react"
import { X } from "lucide-react"
import { FocusTrap } from "focus-trap-react"

interface DialogProps extends React.HTMLAttributes<HTMLDivElement> {
  open?: boolean
  onOpenChange?: (open: boolean) => void
}

interface DialogTriggerProps extends React.HTMLAttributes<HTMLDivElement> {}

interface DialogContentProps extends React.HTMLAttributes<HTMLDivElement> {}

interface DialogHeaderProps extends React.HTMLAttributes<HTMLDivElement> {}

interface DialogFooterProps extends React.HTMLAttributes<HTMLDivElement> {}

interface DialogTitleProps extends React.HTMLAttributes<HTMLHeadingElement> {}

interface DialogDescriptionProps extends React.HTMLAttributes<HTMLParagraphElement> {}

const DialogContext = React.createContext<{
  open: boolean
  onOpenChange: (open: boolean) => void
}>({
  open: false,
  onOpenChange: () => {}
})

const Dialog: React.FC<DialogProps> = ({ 
  children, 
  open = false, 
  onOpenChange = () => {},
  ...props 
}) => {
  return (
    <DialogContext.Provider value={{ open, onOpenChange }}>
      <div {...props}>
        {children}
      </div>
    </DialogContext.Provider>
  )
}

const DialogTrigger: React.FC<DialogTriggerProps> = ({ 
  children, 
  onClick,
  ...props 
}) => {
  const { onOpenChange } = React.useContext(DialogContext)
  
  return (
    <div
      onClick={(e) => {
        onClick?.(e)
        onOpenChange(true)
      }}
      {...props}
    >
      {children}
    </div>
  )
}

const DialogContent = React.forwardRef<HTMLDivElement, DialogContentProps>(
  ({ className, children, ...props }, ref) => {
    const { open, onOpenChange } = React.useContext(DialogContext)
    // BUG-004 FIX: Create ref for close button to set initial focus (moved before conditional)
    const closeButtonRef = React.useRef<HTMLButtonElement>(null)

    React.useEffect(() => {
      const handleEscape = (e: KeyboardEvent) => {
        if (e.key === "Escape" && open) {
          onOpenChange(false)
        }
      }

      if (open) {
        document.addEventListener("keydown", handleEscape)
        document.body.style.overflow = "hidden"
      }

      return () => {
        document.removeEventListener("keydown", handleEscape)
        document.body.style.overflow = "unset"
      }
    }, [open, onOpenChange])

    if (!open) return null

    return (
      <FocusTrap
        active={open}
        focusTrapOptions={{
          // BUG-004 FIX: Set initial focus to close button for better accessibility
          initialFocus: () => closeButtonRef.current || false,
          allowOutsideClick: true,
          escapeDeactivates: true,
          returnFocusOnDeactivate: true,
        }}
      >
        <div className="fixed inset-0 z-50 flex items-center justify-center" role="dialog" aria-modal="true">
          <div
            className="fixed inset-0 bg-overlay/80 backdrop-blur-sm"
            onClick={() => onOpenChange(false)}
            aria-hidden="true"
          />
          <div
            ref={ref}
            className={`relative z-50 grid w-full max-w-lg gap-4 border border-border bg-background p-6 shadow-lg duration-200 animate-in fade-in-0 zoom-in-95 sm:rounded-lg ${className || ""}`}
            {...props}
          >
            <button
              ref={closeButtonRef}
              className="absolute right-4 top-4 rounded-full opacity-70 ring-offset-background transition-opacity hover:opacity-100 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:pointer-events-none"
              onClick={() => onOpenChange(false)}
              aria-label="Close dialog"
            >
              <X className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
              <span className="sr-only">Close</span>
            </button>
            {children}
          </div>
        </div>
      </FocusTrap>
    )
  }
)
DialogContent.displayName = "DialogContent"

const DialogHeader = React.forwardRef<HTMLDivElement, DialogHeaderProps>(
  ({ className, ...props }, ref) => (
    <div
      ref={ref}
      className={`flex flex-col space-y-1.5 text-center sm:text-left ${className || ""}`}
      {...props}
    />
  )
)
DialogHeader.displayName = "DialogHeader"

const DialogFooter = React.forwardRef<HTMLDivElement, DialogFooterProps>(
  ({ className, ...props }, ref) => (
    <div
      ref={ref}
      className={`flex flex-col-reverse sm:flex-row sm:justify-end sm:space-x-2 ${className || ""}`}
      {...props}
    />
  )
)
DialogFooter.displayName = "DialogFooter"

const DialogTitle = React.forwardRef<HTMLHeadingElement, DialogTitleProps>(
  ({ className, ...props }, ref) => (
    <h3
      ref={ref}
      className={`text-lg font-semibold leading-none tracking-tight ${className || ""}`}
      {...props}
    />
  )
)
DialogTitle.displayName = "DialogTitle"

const DialogDescription = React.forwardRef<HTMLParagraphElement, DialogDescriptionProps>(
  ({ className, ...props }, ref) => (
    <p
      ref={ref}
      className={`text-sm text-muted-foreground ${className || ""}`}
      {...props}
    />
  )
)
DialogDescription.displayName = "DialogDescription"

export {
  Dialog,
  DialogTrigger,
  DialogContent,
  DialogHeader,
  DialogFooter,
  DialogTitle,
  DialogDescription,
}