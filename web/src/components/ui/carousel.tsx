import * as React from "react"
import Image from "next/image"
import { ChevronLeft, ChevronRight } from "lucide-react"

export interface CarouselProps {
  children: React.ReactNode[]
  autoPlay?: boolean
  interval?: number
  showControls?: boolean
  showIndicators?: boolean
  loop?: boolean
  className?: string
}

export const Carousel: React.FC<CarouselProps> = ({
  children,
  autoPlay = false,
  interval = 5000,
  showControls = true,
  showIndicators = true,
  loop = true,
  className,
}) => {
  const [currentIndex, setCurrentIndex] = React.useState(0)
  const [isHovered, setIsHovered] = React.useState(false)
  const autoPlayRef = React.useRef<NodeJS.Timeout | undefined>(undefined)

  // BUG-014 FIX: Handle empty children array
  const totalSlides = children.length

  // All hooks must be called before any conditional returns
  const goToSlide = React.useCallback((index: number) => {
    setCurrentIndex(index)
  }, [])

  const goToPrevious = React.useCallback(() => {
    setCurrentIndex((prev) => {
      if (prev === 0) {
        return loop ? totalSlides - 1 : 0
      }
      return prev - 1
    })
  }, [loop, totalSlides])

  const goToNext = React.useCallback(() => {
    setCurrentIndex((prev) => {
      if (prev === totalSlides - 1) {
        return loop ? 0 : totalSlides - 1
      }
      return prev + 1
    })
  }, [loop, totalSlides])

  React.useEffect(() => {
    if (totalSlides === 0) return // Skip effect for empty carousel
    if (autoPlay && !isHovered) {
      autoPlayRef.current = setInterval(goToNext, interval)
    }

    return () => {
      if (autoPlayRef.current) {
        clearInterval(autoPlayRef.current)
      }
    }
  }, [autoPlay, isHovered, goToNext, interval, totalSlides])

  // BUG-025 FIX: Disable keyboard at boundaries when loop=false
  React.useEffect(() => {
    if (totalSlides === 0) return // Skip effect for empty carousel
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "ArrowLeft") {
        // Only navigate if loop is enabled or not at the first slide
        if (loop || currentIndex > 0) {
          goToPrevious()
        }
      } else if (e.key === "ArrowRight") {
        // Only navigate if loop is enabled or not at the last slide
        if (loop || currentIndex < totalSlides - 1) {
          goToNext()
        }
      }
    }

    window.addEventListener("keydown", handleKeyDown)
    return () => window.removeEventListener("keydown", handleKeyDown)
  }, [goToPrevious, goToNext, loop, currentIndex, totalSlides])

  // BUG-014 FIX: Warn and render nothing for empty carousel (after all hooks)
  if (totalSlides === 0) {
    if (process.env.NODE_ENV === 'development') {
      // eslint-disable-next-line no-console
      console.warn('Carousel: No children provided. Carousel will not render.')
    }
    return null
  }

  return (
    <div
      className={`relative w-full overflow-hidden ${className || ""}`}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      role="region"
      aria-label="Carousel"
    >
      {/* Slides */}
      <div
        className="flex transition-transform duration-500 ease-in-out"
        style={{ transform: `translateX(-${currentIndex * 100}%)` }}
      >
        {children.map((child, index) => (
          <div
            key={index}
            className="min-w-full"
            role="group"
            aria-roledescription="slide"
            aria-label={`Slide ${index + 1} of ${totalSlides}`}
          >
            {child}
          </div>
        ))}
      </div>

      {/* Controls */}
      {showControls && totalSlides > 1 && (
        <>
          <button
            type="button"
            onClick={goToPrevious}
            disabled={!loop && currentIndex === 0}
            className={`
              absolute left-4 top-1/2 -translate-y-1/2 z-10
              p-2 rounded-full bg-foreground/50 hover:bg-foreground/70 text-background
              transition-all duration-200 hover:scale-110
              disabled:opacity-30 disabled:cursor-not-allowed disabled:hover:scale-100
            `}
            aria-label="Previous slide"
          >
            <ChevronLeft className="h-6 w-6" aria-hidden="true" />
          </button>
          <button
            type="button"
            onClick={goToNext}
            disabled={!loop && currentIndex === totalSlides - 1}
            className={`
              absolute right-4 top-1/2 -translate-y-1/2 z-10
              p-2 rounded-full bg-foreground/50 hover:bg-foreground/70 text-background
              transition-all duration-200 hover:scale-110
              disabled:opacity-30 disabled:cursor-not-allowed disabled:hover:scale-100
            `}
            aria-label="Next slide"
          >
            <ChevronRight className="h-6 w-6" aria-hidden="true" />
          </button>
        </>
      )}

      {/* Indicators */}
      {showIndicators && totalSlides > 1 && (
        <div className="absolute bottom-4 left-1/2 -translate-x-1/2 flex space-x-2 z-10">
          {children.map((_, index) => (
            <button
              key={index}
              type="button"
              onClick={() => goToSlide(index)}
              className={`
                h-2 rounded-full transition-all duration-200
                ${
                  index === currentIndex
                    ? "w-8 bg-background"
                    : "w-2 bg-background/50 hover:bg-background/75"
                }
              `}
              aria-label={`Go to slide ${index + 1}`}
              aria-current={index === currentIndex || undefined}
            />
          ))}
        </div>
      )}
    </div>
  )
}

Carousel.displayName = "Carousel"

// Card carousel variant for scrollable cards
export interface CardCarouselProps {
  children: React.ReactNode[]
  itemWidth?: string
  gap?: string
  showControls?: boolean
  className?: string
}

export const CardCarousel: React.FC<CardCarouselProps> = ({
  children,
  itemWidth = "300px",
  gap = "1rem",
  showControls = true,
  className,
}) => {
  const scrollRef = React.useRef<HTMLDivElement>(null)
  const [canScrollLeft, setCanScrollLeft] = React.useState(false)
  const [canScrollRight, setCanScrollRight] = React.useState(true)

  const checkScroll = React.useCallback(() => {
    if (scrollRef.current) {
      const { scrollLeft, scrollWidth, clientWidth } = scrollRef.current
      setCanScrollLeft(scrollLeft > 0)
      setCanScrollRight(scrollLeft < scrollWidth - clientWidth - 1)
    }
  }, [])

  React.useEffect(() => {
    checkScroll()
    window.addEventListener("resize", checkScroll)
    return () => window.removeEventListener("resize", checkScroll)
  }, [checkScroll])

  const scroll = (direction: "left" | "right") => {
    if (scrollRef.current) {
      const scrollAmount = scrollRef.current.clientWidth * 0.8
      scrollRef.current.scrollBy({
        left: direction === "left" ? -scrollAmount : scrollAmount,
        behavior: "smooth",
      })
    }
  }

  return (
    <div className={`relative ${className || ""}`}>
      {/* Scroll Container */}
      <div
        ref={scrollRef}
        className="flex overflow-x-auto scrollbar-hide"
        style={{ gap }}
        onScroll={checkScroll}
      >
        {children.map((child, index) => (
          <div
            key={index}
            className="flex-shrink-0"
            style={{ width: itemWidth }}
          >
            {child}
          </div>
        ))}
      </div>

      {/* Controls */}
      {showControls && (
        <>
          {canScrollLeft && (
            <button
              type="button"
              onClick={() => scroll("left")}
              className="absolute left-0 top-1/2 -translate-y-1/2 z-10 p-2 rounded-full bg-foreground/50 hover:bg-foreground/70 text-background transition-all"
              aria-label="Scroll left"
            >
              <ChevronLeft className="h-6 w-6" aria-hidden="true" />
            </button>
          )}
          {canScrollRight && (
            <button
              type="button"
              onClick={() => scroll("right")}
              className="absolute right-0 top-1/2 -translate-y-1/2 z-10 p-2 rounded-full bg-foreground/50 hover:bg-foreground/70 text-background transition-all"
              aria-label="Scroll right"
            >
              <ChevronRight className="h-6 w-6" aria-hidden="true" />
            </button>
          )}
        </>
      )}
    </div>
  )
}

CardCarousel.displayName = "CardCarousel"

// Thumbnail carousel with preview
export interface ThumbnailCarouselProps {
  items: {
    id: string
    thumbnail: string
    content: React.ReactNode
    alt?: string
  }[]
  className?: string
}

export const ThumbnailCarousel: React.FC<ThumbnailCarouselProps> = ({
  items,
  className,
}) => {
  const [selectedIndex, setSelectedIndex] = React.useState(0)

  return (
    <div className={`space-y-4 ${className || ""}`}>
      {/* Main Display */}
      <div className="relative aspect-video bg-muted rounded-xl overflow-hidden">
        {items[selectedIndex].content}
      </div>

      {/* Thumbnails */}
      <div className="flex gap-2 overflow-x-auto pb-2">
        {items.map((item, index) => (
          <button
            key={item.id}
            type="button"
            onClick={() => setSelectedIndex(index)}
            className={`
              relative flex-shrink-0 w-24 h-16 rounded-lg overflow-hidden
              border-2 transition-all duration-200
              ${
                index === selectedIndex
                  ? "border-primary ring-2 ring-primary/20"
                  : "border-border hover:border-primary/50"
              }
            `}
            aria-label={item.alt || `View item ${index + 1}`}
          >
            <Image
              src={item.thumbnail}
              alt={item.alt || `Thumbnail ${index + 1}`}
              fill
              className="object-cover"
              unoptimized
            />
            {index === selectedIndex && (
              <div className="absolute inset-0 bg-primary/20" />
            )}
          </button>
        ))}
      </div>
    </div>
  )
}

ThumbnailCarousel.displayName = "ThumbnailCarousel"
