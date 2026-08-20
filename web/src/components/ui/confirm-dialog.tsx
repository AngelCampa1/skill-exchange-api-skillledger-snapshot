import * as React from "react"
import { AlertTriangle, CheckCircle, Info, XCircle } from "lucide-react"
import { Button } from "./button"

export interface ConfirmDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description: string
  confirmText?: string
  cancelText?: string
  // BUG-052 FIX: Add loadingText prop
  loadingText?: string
  onConfirm: () => void | Promise<void>
  onCancel?: () => void
  variant?: "default" | "destructive" | "warning" | "success" | "info"
  loading?: boolean
}

export const ConfirmDialog: React.FC<ConfirmDialogProps> = ({
  open,
  onOpenChange,
  title,
  description,
  confirmText = "Confirm",
  cancelText = "Cancel",
  // BUG-052 FIX: Add loadingText prop with default
  loadingText = "Processing...",
  onConfirm,
  onCancel,
  variant = "default",
  loading = false,
}) => {
  // BUG-028 FIX: Ref for focus management
  const confirmButtonRef = React.useRef<HTMLButtonElement>(null)
  const [isLoading, setIsLoading] = React.useState(false)

  const handleConfirm = async () => {
    setIsLoading(true)
    try {
      await onConfirm()
      onOpenChange(false)
    } finally {
      setIsLoading(false)
    }
  }

  const handleCancel = React.useCallback(() => {
    onCancel?.()
    onOpenChange(false)
  }, [onCancel, onOpenChange])

  React.useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === "Escape" && open && !isLoading) {
        handleCancel()
      }
    }

    if (open) {
      document.addEventListener("keydown", handleEscape)
      document.body.style.overflow = "hidden"
      // BUG-028 FIX: Focus the confirm button when dialog opens
      setTimeout(() => confirmButtonRef.current?.focus(), 0)
    }

    return () => {
      document.removeEventListener("keydown", handleEscape)
      document.body.style.overflow = "unset"
    }
  }, [open, isLoading, handleCancel])

  if (!open) return null

  const variantConfig = {
    default: {
      icon: Info,
      iconColor: "text-primary",
      buttonVariant: "default" as const,
    },
    destructive: {
      icon: XCircle,
      iconColor: "text-destructive",
      buttonVariant: "destructive" as const,
    },
    warning: {
      icon: AlertTriangle,
      iconColor: "text-warning",
      buttonVariant: "default" as const,
    },
    success: {
      icon: CheckCircle,
      iconColor: "text-success",
      buttonVariant: "default" as const,
    },
    info: {
      icon: Info,
      iconColor: "text-info",
      buttonVariant: "default" as const,
    },
  }

  const config = variantConfig[variant]
  const Icon = config.icon

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="confirm-dialog-title"
      aria-describedby="confirm-dialog-description"
    >
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-overlay/80 backdrop-blur-sm animate-in fade-in-0"
        onClick={!isLoading ? handleCancel : undefined}
        aria-hidden="true"
      />

      {/* Dialog */}
      <div className="relative z-50 w-full max-w-md bg-background rounded-xl shadow-lg border border-border animate-in fade-in-0 zoom-in-95 duration-200">
        <div className="p-6 space-y-4">
          {/* Icon and Title */}
          <div className="flex items-start space-x-4">
            <div className={`flex-shrink-0 ${config.iconColor}`}>
              <Icon className="h-6 w-6" aria-hidden="true" />
            </div>
            <div className="flex-1 space-y-2">
              <h2
                id="confirm-dialog-title"
                className="text-lg font-semibold text-foreground"
              >
                {title}
              </h2>
              <p
                id="confirm-dialog-description"
                className="text-sm text-muted-foreground leading-relaxed"
              >
                {description}
              </p>
            </div>
          </div>

          {/* Actions */}
          <div className="flex flex-col-reverse sm:flex-row sm:justify-end gap-3 pt-2">
            <Button
              variant="outline"
              onClick={handleCancel}
              disabled={isLoading || loading}
            >
              {cancelText}
            </Button>
            {/* BUG-028 FIX: Add ref for focus management, BUG-052 FIX: Show loadingText when loading */}
            <Button
              ref={confirmButtonRef}
              variant={config.buttonVariant}
              onClick={handleConfirm}
              loading={isLoading || loading}
              disabled={isLoading || loading}
            >
              {isLoading || loading ? loadingText : confirmText}
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}

ConfirmDialog.displayName = "ConfirmDialog"

// Hook for easier usage
export const useConfirmDialog = () => {
  const [isOpen, setIsOpen] = React.useState(false)
  const [config, setConfig] = React.useState<Omit<ConfirmDialogProps, "open" | "onOpenChange">>({
    title: "",
    description: "",
    onConfirm: () => {},
  })

  const confirm = React.useCallback((options: Omit<ConfirmDialogProps, "open" | "onOpenChange">) => {
    return new Promise<boolean>((resolve) => {
      setConfig({
        ...options,
        onConfirm: async () => {
          await options.onConfirm()
          resolve(true)
        },
        onCancel: () => {
          options.onCancel?.()
          resolve(false)
        },
      })
      setIsOpen(true)
    })
  }, [])

  const dialog = (
    <ConfirmDialog
      {...config}
      open={isOpen}
      onOpenChange={setIsOpen}
    />
  )

  return { confirm, dialog }
}
