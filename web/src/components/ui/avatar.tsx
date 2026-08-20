import * as React from "react"
import Image from "next/image"
import { User } from "lucide-react"

export interface AvatarProps extends React.HTMLAttributes<HTMLDivElement> {
  src?: string
  alt?: string
  fallback?: string
  size?: "xs" | "sm" | "md" | "lg" | "xl" | "2xl"
  variant?: "circular" | "rounded" | "square"
  status?: "online" | "offline" | "away" | "busy"
  showStatus?: boolean
}

export const Avatar = React.forwardRef<HTMLDivElement, AvatarProps>(
  ({
    src,
    alt = "Avatar",
    fallback,
    size = "md",
    variant = "circular",
    status,
    showStatus = false,
    className,
    ...props
  }, ref) => {
    const [imageError, setImageError] = React.useState(false)

    const sizeClasses = {
      xs: "h-6 w-6 text-xs",
      sm: "h-8 w-8 text-sm",
      md: "h-10 w-10 text-base",
      lg: "h-12 w-12 text-lg",
      xl: "h-16 w-16 text-xl",
      "2xl": "h-20 w-20 text-2xl"
    }

    const variantClasses = {
      circular: "rounded-full",
      rounded: "rounded-lg",
      square: "rounded-none"
    }

    const statusSizes = {
      xs: "h-1.5 w-1.5",
      sm: "h-2 w-2",
      md: "h-2.5 w-2.5",
      lg: "h-3 w-3",
      xl: "h-3.5 w-3.5",
      "2xl": "h-4 w-4"
    }

    const statusColors = {
      online: "bg-success",
      offline: "bg-muted-foreground",
      away: "bg-warning",
      busy: "bg-destructive"
    }

    const getInitials = (name: string) => {
      const words = name.trim().split(/\s+/)
      if (words.length >= 2) {
        return `${words[0][0]}${words[1][0]}`.toUpperCase()
      }
      return name.substring(0, 2).toUpperCase()
    }

    const showImage = src && !imageError
    const showFallback = !showImage && fallback
    const showIcon = !showImage && !fallback

    return (
      <div
        ref={ref}
        className={`relative inline-flex items-center justify-center flex-shrink-0 overflow-hidden bg-muted ${sizeClasses[size]} ${variantClasses[variant]} ${className || ""}`}
        {...props}
      >
        {showImage && (
          <Image
            src={src}
            alt={alt}
            fill
            className="object-cover"
            onError={() => setImageError(true)}
            unoptimized
          />
        )}

        {showFallback && (
          <span className="font-medium text-foreground select-none">
            {getInitials(fallback)}
          </span>
        )}

        {showIcon && (
          <User className="h-1/2 w-1/2 text-muted-foreground" aria-hidden="true" />
        )}

        {showStatus && status && (
          <span
            className={`absolute bottom-0 right-0 block rounded-full ring-2 ring-background ${statusSizes[size]} ${statusColors[status]}`}
            aria-label={`Status: ${status}`}
          />
        )}
      </div>
    )
  }
)

Avatar.displayName = "Avatar"

// Avatar Group for displaying multiple avatars
export interface AvatarGroupProps {
  children: React.ReactNode
  max?: number
  size?: AvatarProps["size"]
  className?: string
}

export const AvatarGroup: React.FC<AvatarGroupProps> = ({
  children,
  max = 5,
  size = "md",
  className,
}) => {
  const childArray = React.Children.toArray(children)
  const displayChildren = max ? childArray.slice(0, max) : childArray
  const remaining = max ? Math.max(childArray.length - max, 0) : 0

  return (
    <div className={`flex items-center -space-x-2 ${className || ""}`}>
      {displayChildren.map((child, index) => (
        <div
          key={index}
          className="ring-2 ring-background"
          style={{ zIndex: displayChildren.length - index }}
        >
          {child}
        </div>
      ))}
      {remaining > 0 && (
        <Avatar
          size={size}
          fallback={`+${remaining}`}
          className="ring-2 ring-background bg-muted-foreground/10"
          style={{ zIndex: 0 }}
        />
      )}
    </div>
  )
}

AvatarGroup.displayName = "AvatarGroup"
