import * as React from "react"
import { Star, Heart, ThumbsUp } from "lucide-react"

export interface RatingProps {
  value?: number
  onChange?: (value: number) => void
  max?: number
  size?: "sm" | "md" | "lg"
  icon?: "star" | "heart" | "thumbs"
  allowHalf?: boolean
  readonly?: boolean
  disabled?: boolean
  showValue?: boolean
  label?: string
  helperText?: string
  error?: boolean
  className?: string
}

export const Rating = React.forwardRef<HTMLDivElement, RatingProps>(
  (
    {
      value = 0,
      onChange,
      max = 5,
      size = "md",
      icon = "star",
      allowHalf = false,
      readonly = false,
      disabled = false,
      showValue = false,
      label,
      helperText,
      error,
      className,
    },
    ref
  ) => {
    const [hoverValue, setHoverValue] = React.useState<number | null>(null)

    const sizeClasses = {
      sm: "h-4 w-4",
      md: "h-6 w-6",
      lg: "h-8 w-8",
    }

    const IconComponent = {
      star: Star,
      heart: Heart,
      thumbs: ThumbsUp,
    }[icon]

    const handleClick = (index: number, isHalf: boolean) => {
      if (readonly || disabled) return
      const newValue = isHalf ? index + 0.5 : index + 1
      onChange?.(newValue)
    }

    const handleMouseMove = (index: number, e: React.MouseEvent<HTMLButtonElement>) => {
      if (readonly || disabled || !allowHalf) return
      const rect = e.currentTarget.getBoundingClientRect()
      const x = e.clientX - rect.left
      const isHalf = x < rect.width / 2
      setHoverValue(isHalf ? index + 0.5 : index + 1)
    }

    const handleMouseLeave = () => {
      setHoverValue(null)
    }

    const isIconFilled = (index: number) => {
      const currentValue = hoverValue !== null ? hoverValue : value
      return currentValue > index
    }

    const isIconHalfFilled = (index: number) => {
      const currentValue = hoverValue !== null ? hoverValue : value
      return currentValue === index + 0.5
    }

    return (
      <div ref={ref} className={`space-y-2 ${className || ""}`}>
        {label && (
          <label
            className={`text-sm font-medium ${
              error ? "text-destructive" : "text-foreground"
            }`}
          >
            {label}
          </label>
        )}

        <div className="flex items-center space-x-1">
          {Array.from({ length: max }, (_, index) => {
            const filled = isIconFilled(index)
            const halfFilled = allowHalf && isIconHalfFilled(index)

            return (
              <button
                key={index}
                type="button"
                onClick={(e) => {
                  if (allowHalf) {
                    const rect = e.currentTarget.getBoundingClientRect()
                    const x = e.clientX - rect.left
                    const isHalf = x < rect.width / 2
                    handleClick(index, isHalf)
                  } else {
                    handleClick(index, false)
                  }
                }}
                onMouseMove={(e) => handleMouseMove(index, e)}
                onMouseLeave={handleMouseLeave}
                disabled={readonly || disabled}
                className={`
                  relative transition-all duration-150
                  ${readonly || disabled ? "cursor-default" : "cursor-pointer hover:scale-110"}
                  ${disabled ? "opacity-50" : ""}
                `}
                aria-label={`Rate ${index + 1} out of ${max}`}
              >
                {halfFilled ? (
                  <div className="relative">
                    <IconComponent
                      className={`${sizeClasses[size]} text-muted stroke-muted-foreground`}
                      aria-hidden="true"
                    />
                    <div className="absolute inset-0 overflow-hidden" style={{ width: "50%" }}>
                      <IconComponent
                        className={`${sizeClasses[size]} ${
                          error ? "text-destructive fill-destructive" : "text-warning fill-warning"
                        }`}
                        aria-hidden="true"
                      />
                    </div>
                  </div>
                ) : (
                  <IconComponent
                    className={`${sizeClasses[size]} ${
                      filled
                        ? error
                          ? "text-destructive fill-destructive"
                          : "text-warning fill-warning"
                        : "text-muted stroke-muted-foreground"
                    }`}
                    aria-hidden="true"
                  />
                )}
              </button>
            )
          })}

          {showValue && (
            <span
              className={`ml-2 text-sm font-medium ${
                error ? "text-destructive" : "text-foreground"
              }`}
            >
              {value.toFixed(allowHalf ? 1 : 0)} / {max}
            </span>
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

Rating.displayName = "Rating"

// Read-only star rating display
export interface StarRatingDisplayProps {
  value: number
  max?: number
  size?: "sm" | "md" | "lg"
  showValue?: boolean
  reviewCount?: number
  className?: string
}

export const StarRatingDisplay: React.FC<StarRatingDisplayProps> = ({
  value,
  max = 5,
  size = "md",
  showValue = true,
  reviewCount,
  className,
}) => {
  const sizeClasses = {
    sm: "h-3 w-3",
    md: "h-4 w-4",
    lg: "h-5 w-5",
  }

  const textSizeClasses = {
    sm: "text-xs",
    md: "text-sm",
    lg: "text-base",
  }

  return (
    <div className={`inline-flex items-center space-x-2 ${className || ""}`}>
      <div className="flex items-center space-x-0.5">
        {Array.from({ length: max }, (_, index) => {
          const filled = value > index
          const partialFill = value > index && value < index + 1
          const fillPercentage = partialFill ? ((value - index) * 100).toFixed(0) : "100"

          return (
            <div key={index} className="relative">
              <Star
                className={`${sizeClasses[size]} text-muted stroke-muted-foreground`}
                aria-hidden="true"
              />
              {(filled || partialFill) && (
                <div
                  className="absolute inset-0 overflow-hidden"
                  style={{ width: `${fillPercentage}%` }}
                >
                  <Star
                    className={`${sizeClasses[size]} text-warning fill-warning`}
                    aria-hidden="true"
                  />
                </div>
              )}
            </div>
          )
        })}
      </div>

      {showValue && (
        <div className={`flex items-center space-x-1 ${textSizeClasses[size]}`}>
          <span className="font-semibold text-foreground">{value.toFixed(1)}</span>
          {reviewCount !== undefined && (
            <span className="text-muted-foreground">({reviewCount.toLocaleString()})</span>
          )}
        </div>
      )}
    </div>
  )
}

StarRatingDisplay.displayName = "StarRatingDisplay"

// Rating summary component
export interface RatingSummaryProps {
  averageRating: number
  totalReviews: number
  distribution: { rating: number; count: number }[]
  className?: string
}

export const RatingSummary: React.FC<RatingSummaryProps> = ({
  averageRating,
  totalReviews,
  distribution,
  className,
}) => {
  const maxCount = Math.max(...distribution.map((d) => d.count))

  return (
    <div className={`space-y-4 ${className || ""}`}>
      {/* Overall Rating */}
      <div className="flex items-center space-x-4">
        <div className="text-center">
          <div className="text-4xl font-bold text-foreground">{averageRating.toFixed(1)}</div>
          <StarRatingDisplay value={averageRating} showValue={false} />
          <div className="text-sm text-muted-foreground mt-1">
            {totalReviews.toLocaleString()} review{totalReviews !== 1 ? "s" : ""}
          </div>
        </div>
      </div>

      {/* Rating Distribution */}
      <div className="space-y-2">
        {distribution
          .sort((a, b) => b.rating - a.rating)
          .map((item) => {
            const percentage = maxCount > 0 ? (item.count / maxCount) * 100 : 0

            return (
              <div key={item.rating} className="flex items-center space-x-3">
                <div className="flex items-center space-x-1 w-16">
                  <span className="text-sm font-medium text-foreground">{item.rating}</span>
                  <Star className="h-3 w-3 text-warning fill-warning" aria-hidden="true" />
                </div>
                <div className="flex-1 h-2 bg-muted rounded-full overflow-hidden">
                  <div
                    className="h-full bg-warning transition-all duration-300"
                    style={{ width: `${percentage}%` }}
                  />
                </div>
                <span className="text-sm text-muted-foreground w-12 text-right">
                  {item.count.toLocaleString()}
                </span>
              </div>
            )
          })}
      </div>
    </div>
  )
}

RatingSummary.displayName = "RatingSummary"
