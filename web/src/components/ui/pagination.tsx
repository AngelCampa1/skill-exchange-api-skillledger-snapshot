import * as React from "react"
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight, MoreHorizontal } from "lucide-react"
import { Button } from "./button"

export interface PaginationProps {
  currentPage: number
  totalPages: number
  onPageChange: (page: number) => void
  showFirstLast?: boolean
  showPreviousNext?: boolean
  siblingCount?: number
  className?: string
}

const DOTS = "..."

const range = (start: number, end: number) => {
  const length = end - start + 1
  return Array.from({ length }, (_, idx) => idx + start)
}

const usePagination = ({
  currentPage,
  totalPages,
  siblingCount = 1,
}: {
  currentPage: number
  totalPages: number
  siblingCount: number
}) => {
  return React.useMemo(() => {
    const totalPageNumbers = siblingCount + 5 // siblingCount + firstPage + lastPage + currentPage + 2*DOTS

    // Case 1: If the number of pages is less than the page numbers we want to show
    if (totalPageNumbers >= totalPages) {
      return range(1, totalPages)
    }

    const leftSiblingIndex = Math.max(currentPage - siblingCount, 1)
    const rightSiblingIndex = Math.min(currentPage + siblingCount, totalPages)

    const shouldShowLeftDots = leftSiblingIndex > 2
    const shouldShowRightDots = rightSiblingIndex < totalPages - 2

    const firstPageIndex = 1
    const lastPageIndex = totalPages

    // Case 2: No left dots, but right dots
    if (!shouldShowLeftDots && shouldShowRightDots) {
      const leftItemCount = 3 + 2 * siblingCount
      const leftRange = range(1, leftItemCount)
      return [...leftRange, DOTS, totalPages]
    }

    // Case 3: Left dots, but no right dots
    if (shouldShowLeftDots && !shouldShowRightDots) {
      const rightItemCount = 3 + 2 * siblingCount
      const rightRange = range(totalPages - rightItemCount + 1, totalPages)
      return [firstPageIndex, DOTS, ...rightRange]
    }

    // Case 4: Both left and right dots
    if (shouldShowLeftDots && shouldShowRightDots) {
      const middleRange = range(leftSiblingIndex, rightSiblingIndex)
      return [firstPageIndex, DOTS, ...middleRange, DOTS, lastPageIndex]
    }

    return []
  }, [currentPage, totalPages, siblingCount])
}

export const Pagination: React.FC<PaginationProps> = ({
  currentPage,
  totalPages,
  onPageChange,
  showFirstLast = true,
  showPreviousNext = true,
  siblingCount = 1,
  className,
}) => {
  const paginationRange = usePagination({ currentPage, totalPages, siblingCount })

  if (currentPage === 0 || totalPages === 0 || paginationRange.length < 2) {
    return null
  }

  const onNext = () => {
    if (currentPage < totalPages) {
      onPageChange(currentPage + 1)
    }
  }

  const onPrevious = () => {
    if (currentPage > 1) {
      onPageChange(currentPage - 1)
    }
  }

  const onFirst = () => {
    onPageChange(1)
  }

  const onLast = () => {
    onPageChange(totalPages)
  }

  return (
    <nav
      role="navigation"
      aria-label="Pagination"
      className={`flex items-center justify-center space-x-2 ${className || ""}`}
    >
      {showFirstLast && (
        <Button
          variant="outline"
          size="icon"
          onClick={onFirst}
          disabled={currentPage === 1}
          aria-label="Go to first page"
        >
          <ChevronsLeft className="h-4 w-4" aria-hidden="true" />
        </Button>
      )}

      {showPreviousNext && (
        <Button
          variant="outline"
          size="icon"
          onClick={onPrevious}
          disabled={currentPage === 1}
          aria-label="Go to previous page"
        >
          <ChevronLeft className="h-4 w-4" aria-hidden="true" />
        </Button>
      )}

      {paginationRange.map((pageNumber, index) => {
        if (pageNumber === DOTS) {
          return (
            <span
              key={`dots-${index}`}
              className="flex h-12 w-12 items-center justify-center text-muted-foreground"
              aria-hidden="true"
            >
              <MoreHorizontal className="h-4 w-4" />
            </span>
          )
        }

        const isCurrentPage = pageNumber === currentPage

        return (
          <Button
            key={pageNumber}
            variant={isCurrentPage ? "default" : "outline"}
            size="icon"
            onClick={() => onPageChange(pageNumber as number)}
            aria-label={`Go to page ${pageNumber}`}
            aria-current={isCurrentPage ? "page" : undefined}
          >
            {pageNumber}
          </Button>
        )
      })}

      {showPreviousNext && (
        <Button
          variant="outline"
          size="icon"
          onClick={onNext}
          disabled={currentPage === totalPages}
          aria-label="Go to next page"
        >
          <ChevronRight className="h-4 w-4" aria-hidden="true" />
        </Button>
      )}

      {showFirstLast && (
        <Button
          variant="outline"
          size="icon"
          onClick={onLast}
          disabled={currentPage === totalPages}
          aria-label="Go to last page"
        >
          <ChevronsRight className="h-4 w-4" aria-hidden="true" />
        </Button>
      )}
    </nav>
  )
}

// Simple pagination info component
export interface PaginationInfoProps {
  currentPage: number
  totalPages: number
  totalItems: number
  itemsPerPage: number
  className?: string
}

export const PaginationInfo: React.FC<PaginationInfoProps> = ({
  currentPage,
  totalPages,
  totalItems,
  itemsPerPage,
  className,
}) => {
  // E2E-006 FIX: Handle empty state to avoid showing "1 - 0 of 0"
  if (totalItems === 0) {
    return (
      <p className={`text-sm text-muted-foreground ${className || ""}`}>
        No results found
      </p>
    )
  }

  const start = (currentPage - 1) * itemsPerPage + 1
  const end = Math.min(currentPage * itemsPerPage, totalItems)

  return (
    <p className={`text-sm text-muted-foreground ${className || ""}`}>
      Showing <span className="font-medium text-foreground">{start}</span> to{" "}
      <span className="font-medium text-foreground">{end}</span> of{" "}
      <span className="font-medium text-foreground">{totalItems}</span> results
    </p>
  )
}
