import * as React from "react"
import { X, CheckCircle, AlertCircle, Info, AlertTriangle } from "lucide-react"

export type ToastVariant = "default" | "success" | "error" | "warning" | "info"

export interface Toast {
  id: string
  title: string
  description?: string
  variant?: ToastVariant
  duration?: number
}

interface ToastProps extends Toast {
  onClose: (id: string) => void
}

const toastVariants = {
  default: {
    container: "bg-background border-border",
    icon: null,
    iconColor: ""
  },
  success: {
    container: "bg-success/10 border-success/20",
    icon: CheckCircle,
    iconColor: "text-success"
  },
  error: {
    container: "bg-destructive/10 border-destructive/20",
    icon: AlertCircle,
    iconColor: "text-destructive"
  },
  warning: {
    container: "bg-warning/10 border-warning/20",
    icon: AlertTriangle,
    iconColor: "text-warning"
  },
  info: {
    container: "bg-info/10 border-info/20",
    icon: Info,
    iconColor: "text-info"
  }
}

export const ToastItem: React.FC<ToastProps> = ({
  id,
  title,
  description,
  variant = "default",
  duration = 5000,
  onClose
}) => {
  const [isExiting, setIsExiting] = React.useState(false)
  const variantConfig = toastVariants[variant]
  const Icon = variantConfig.icon

  const handleClose = React.useCallback(() => {
    setIsExiting(true)
    setTimeout(() => {
      onClose(id)
    }, 300)
  }, [id, onClose])

  React.useEffect(() => {
    if (duration > 0) {
      const timer = setTimeout(() => {
        handleClose()
      }, duration)
      return () => clearTimeout(timer)
    }
  }, [duration, id, handleClose])

  return (
    <div
      role="alert"
      aria-live="polite"
      aria-atomic="true"
      className={`
        pointer-events-auto flex w-full max-w-md items-start gap-3 rounded-lg border p-4 shadow-lg
        transition-all duration-300 ease-out
        ${variantConfig.container}
        ${isExiting ? 'opacity-0 translate-x-full' : 'opacity-100 translate-x-0'}
        animate-in slide-in-from-right-full
      `}
    >
      {Icon && (
        <Icon className={`h-5 w-5 flex-shrink-0 mt-0.5 ${variantConfig.iconColor}`} aria-hidden="true" />
      )}

      <div className="flex-1 space-y-1">
        <p className="text-sm font-semibold text-foreground">
          {title}
        </p>
        {description && (
          <p className="text-sm text-muted-foreground">
            {description}
          </p>
        )}
      </div>

      <button
        onClick={handleClose}
        className="flex-shrink-0 rounded-full p-1 hover:bg-foreground/5 transition-colors"
        aria-label="Close notification"
      >
        <X className="h-4 w-4 text-muted-foreground" aria-hidden="true" />
      </button>
    </div>
  )
}

interface ToastContainerProps {
  toasts: Toast[]
  onClose: (id: string) => void
}

// BUG-048 FIX: Max toast limit
const MAX_TOASTS = 5

export const ToastContainer: React.FC<ToastContainerProps> = ({ toasts, onClose }) => {
  // BUG-048 FIX: Only show the most recent MAX_TOASTS toasts
  const visibleToasts = toasts.slice(-MAX_TOASTS)

  return (
    // BUG-022 FIX: Responsive positioning for mobile
    <div
      className="fixed z-50 flex flex-col gap-2 p-4 pointer-events-none top-0 right-0 sm:top-0 sm:right-0 left-0 sm:left-auto"
      aria-label="Notifications"
      role="region"
    >
      {visibleToasts.map((toast) => (
        <ToastItem key={toast.id} {...toast} onClose={onClose} />
      ))}
    </div>
  )
}

// Toast Hook
export const useToast = () => {
  const [toasts, setToasts] = React.useState<Toast[]>([])

  const toast = React.useCallback((options: Omit<Toast, 'id'>) => {
    const id = Math.random().toString(36).substring(2, 9)
    const newToast: Toast = {
      id,
      duration: 5000,
      ...options
    }
    // BUG-048 FIX: Remove oldest toasts if exceeding limit
    setToasts((prev) => {
      const updated = [...prev, newToast]
      // Remove oldest toasts if exceeding MAX_TOASTS
      if (updated.length > MAX_TOASTS) {
        return updated.slice(-MAX_TOASTS)
      }
      return updated
    })
    return id
  }, [])

  const dismiss = React.useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id))
  }, [])

  const dismissAll = React.useCallback(() => {
    setToasts([])
  }, [])

  return {
    toasts,
    toast,
    dismiss,
    dismissAll,
    success: (title: string, description?: string) =>
      toast({ title, description, variant: "success" }),
    error: (title: string, description?: string) =>
      toast({ title, description, variant: "error" }),
    warning: (title: string, description?: string) =>
      toast({ title, description, variant: "warning" }),
    info: (title: string, description?: string) =>
      toast({ title, description, variant: "info" })
  }
}
