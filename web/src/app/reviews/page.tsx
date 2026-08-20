'use client'

import { logger } from '@/utils/logger'
import { fetchWithAuth } from '@/utils/apiClient'
import Link from 'next/link'
import { useEffect, useState, useCallback } from 'react'
import { useRouter } from 'next/navigation'
import {
  Star,
  MessageSquare,
  Filter,
  ChevronLeft,
  ChevronRight,
  User,
  Calendar,
  CheckCircle,
  Clock,
  Briefcase,
  AlertTriangle,
  X,
  Send
} from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import LogoutButton from '@/components/LogoutButton'
import { ThemeToggle } from '@/components/ThemeToggle'
import { MobileNav } from '@/components/MobileNav'

interface ReviewStatistics {
  userId: string
  userName: string
  totalReviewsReceived: number
  averageOverallRating: number
  averageQualityRating: number
  averageCommunicationRating: number
  averageTimelinessRating: number
  averageProfessionalismRating: number
  clientReviewsCount: number
  providerReviewsCount: number
  mostRecentReviewDate?: string
}

interface Review {
  id: string
  projectId: string
  projectTitle: string
  reviewerId: string
  reviewerName: string
  revieweeId: string
  revieweeName: string
  type: 'ClientToProvider' | 'ProviderToClient'
  overallRating: number
  qualityRating?: number
  communicationRating?: number
  timelinessRating?: number
  professionalismRating?: number
  calculatedAverageRating: number
  reviewText: string
  responseText?: string
  status: string
  createdAt: string
  publishedAt?: string
  hasPhotoAttachments: boolean
  photoAttachmentCount: number
}

interface ReviewsResponse {
  success: boolean
  data: Review[]
  pagination: {
    currentPage: number
    pageSize: number
    totalCount: number
    totalPages: number
  }
  statistics: ReviewStatistics
}

type ReviewFilter = 'all' | 'ClientToProvider' | 'ProviderToClient'
type SortOption = 'newest' | 'oldest' | 'highest' | 'lowest'

export default function ReviewsPage() {
  const { user, isAuthenticated, isLoading } = useAuth()
  const router = useRouter()

  const [statistics, setStatistics] = useState<ReviewStatistics | null>(null)
  const [reviews, setReviews] = useState<Review[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [noData, setNoData] = useState(false)

  // Pagination
  const [currentPage, setCurrentPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const pageSize = 10

  // Filters
  const [filter, setFilter] = useState<ReviewFilter>('all')
  const [sortBy, setSortBy] = useState<SortOption>('newest')

  // Response Modal
  const [showResponseModal, setShowResponseModal] = useState(false)
  const [selectedReview, setSelectedReview] = useState<Review | null>(null)
  const [responseText, setResponseText] = useState('')
  const [submittingResponse, setSubmittingResponse] = useState(false)
  const [responseError, setResponseError] = useState<string | null>(null)

  const fetchReviewsData = useCallback(async () => {
    if (!user?.id) return

    setLoading(true)
    setError(null)

    try {
      // Build query params
      const params = new URLSearchParams({
        page: currentPage.toString(),
        pageSize: pageSize.toString(),
        sortBy: sortBy === 'highest' || sortBy === 'lowest' ? 'overallRating' : 'CreatedAt',
        sortDescending: (sortBy === 'newest' || sortBy === 'highest').toString(),
      })

      if (filter !== 'all') {
        params.set('type', filter)
      }

      // Fetch statistics and reviews
      const [statsRes, reviewsRes] = await Promise.all([
        fetch(`/api/review/statistics/${user.id}`, {
          method: 'GET',
          credentials: 'include',
        }),
        fetch(`/api/review/user/${user.id}?${params.toString()}`, {
          method: 'GET',
          credentials: 'include',
        }),
      ])

      if (statsRes.status === 404 || reviewsRes.status === 404) {
        setNoData(true)
        setLoading(false)
        return
      }

      if (!statsRes.ok || !reviewsRes.ok) {
        throw new Error('Failed to fetch reviews data')
      }

      const statsData = await statsRes.json()
      const reviewsData: ReviewsResponse = await reviewsRes.json()

      setStatistics(statsData)
      setReviews(reviewsData.data || [])
      setTotalPages(reviewsData.pagination?.totalPages || 1)
      setTotalCount(reviewsData.pagination?.totalCount || 0)
      setNoData(reviewsData.data?.length === 0 && statsData.totalReviewsReceived === 0)
    } catch (err) {
      logger.error('Failed to fetch reviews data', err)
      setError('Unable to load reviews')
    } finally {
      setLoading(false)
    }
  }, [user?.id, currentPage, filter, sortBy])

  // Handle redirect for unauthenticated users
  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      fetch('/api/auth/logout', { method: 'POST', credentials: 'include' })
        .finally(() => {
          window.location.href = '/login'
        })
    }
  }, [isLoading, isAuthenticated])

  // Fetch data when authenticated
  useEffect(() => {
    if (isAuthenticated && user?.id) {
      fetchReviewsData()
    }
  }, [isAuthenticated, user?.id, fetchReviewsData])

  const handleFilterChange = (newFilter: ReviewFilter) => {
    setFilter(newFilter)
    setCurrentPage(1)
  }

  const handleSortChange = (newSort: SortOption) => {
    setSortBy(newSort)
    setCurrentPage(1)
  }

  const handlePageChange = (page: number) => {
    setCurrentPage(page)
  }

  const openResponseModal = (review: Review) => {
    setSelectedReview(review)
    setResponseText('')
    setResponseError(null)
    setShowResponseModal(true)
  }

  const closeResponseModal = () => {
    setShowResponseModal(false)
    setSelectedReview(null)
    setResponseText('')
    setResponseError(null)
  }

  const submitResponse = async () => {
    if (!selectedReview || !responseText.trim()) return

    setSubmittingResponse(true)
    setResponseError(null)

    try {
      await fetchWithAuth(`/api/review/${selectedReview.id}/respond`, {
        method: 'POST',
        body: JSON.stringify({ response: responseText }),
      })

      // Refresh data
      closeResponseModal()
      fetchReviewsData()
    } catch (err) {
      logger.error('Failed to submit response', err)
      setResponseError(err instanceof Error ? err.message : 'Failed to submit response')
    } finally {
      setSubmittingResponse(false)
    }
  }

  const formatRating = (rating: number) => `${rating}/10`
  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    })
  }

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center space-md animate-fade-in">
          <div className="loading-spinner mx-auto animate-glow"></div>
          <p className="text-body text-muted-foreground">Loading your reviews...</p>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center space-md animate-fade-in">
          <div className="loading-spinner mx-auto animate-glow"></div>
          <p className="text-body text-muted-foreground">Redirecting to login...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-background via-primary/5 to-secondary/10">
      {/* Navigation */}
      <nav className="bg-card/90 backdrop-blur-xl border-b border-border/50 sticky top-0 z-50 shadow-lg shadow-primary/5">
        <div className="container-premium">
          <div className="flex justify-between items-center h-24">
            <Link
              href="/dashboard"
              className="text-heading text-foreground hover:text-primary transition-colors duration-300 font-bold tracking-tight"
            >
              SkillLedger
            </Link>

            <div className="hidden md:flex items-center space-golden-md">
              <Link href="/dashboard" className="btn-ghost">Dashboard</Link>
              <Link href="/reputation" className="btn-ghost">Reputation</Link>
              <Link href="/projects/search" className="btn-ghost">Browse Projects</Link>
            </div>

            <div className="flex items-center space-golden-sm">
              <MobileNav
                items={[
                  { href: '/dashboard', label: 'Dashboard' },
                  { href: '/reputation', label: 'Reputation' },
                  { href: '/projects/search', label: 'Browse Projects' },
                ]}
              />
              <ThemeToggle />
              <LogoutButton showAllDevicesOption={true} />
            </div>
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="container-premium py-16 lg:py-24 relative" role="main" aria-label="Reviews content">
        {/* Decorative gradient orbs */}
        <div className="absolute top-20 right-10 w-72 h-72 bg-gradient-to-br from-primary/20 to-secondary/20 rounded-full blur-3xl opacity-60 animate-float" aria-hidden="true"></div>
        <div className="absolute bottom-40 left-10 w-96 h-96 bg-gradient-to-tr from-secondary/15 to-primary/15 rounded-full blur-3xl opacity-50" aria-hidden="true"></div>

        <div className="flex flex-col gap-12 lg:gap-16 relative z-10">
          {/* Header */}
          <header className="animate-fade-in">
            <div className="flex flex-col lg:flex-row lg:items-end lg:justify-between gap-8 bg-gradient-to-r from-primary/10 via-transparent to-secondary/10 p-10 lg:p-12 rounded-3xl border border-primary/20 shadow-xl shadow-primary/5">
              <div className="space-y-4">
                <h1 className="text-5xl sm:text-6xl lg:text-7xl font-black tracking-tight">
                  <span className="bg-gradient-to-r from-primary via-primary to-secondary bg-clip-text text-transparent">
                    Reviews
                  </span>
                </h1>
                <p className="text-lg text-muted-foreground max-w-2xl leading-relaxed">
                  Reviews you have received from completed projects
                </p>
              </div>
            </div>
          </header>

          {/* Error State */}
          {error && (
            <div className="card-elevated p-10 text-center animate-fade-in">
              <AlertTriangle className="w-16 h-16 text-destructive mx-auto mb-4" />
              <h2 className="text-2xl font-bold text-foreground mb-2">Unable to load reviews</h2>
              <p className="text-muted-foreground">Please try again later or contact support if the issue persists.</p>
            </div>
          )}

          {/* No Data State */}
          {noData && !error && (
            <div className="card-elevated p-10 text-center animate-fade-in">
              <Star className="w-16 h-16 text-primary mx-auto mb-4" />
              <h2 className="text-2xl font-bold text-foreground mb-2">No reviews yet</h2>
              <p className="text-muted-foreground mb-6">Complete your first project to receive reviews</p>
              <Link href="/projects/search" className="btn-primary">
                Find Projects
              </Link>
            </div>
          )}

          {/* Data Loaded State */}
          {!error && !noData && !loading && statistics && (
            <>
              {/* Statistics Section */}
              <section className="card-elevated p-10 lg:p-14 animate-slide-in relative overflow-hidden">
                <div className="absolute inset-0 bg-gradient-to-br from-primary/5 via-transparent to-secondary/5 pointer-events-none"></div>
                <div className="relative z-10">
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
                    {/* Total Reviews */}
                    <div className="text-center">
                      <p className="text-caption mb-2">Total Reviews</p>
                      <p data-testid="total-reviews" className="text-5xl font-black text-foreground">
                        {statistics.totalReviewsReceived}
                      </p>
                      <div className="flex justify-center gap-4 mt-2 text-sm text-muted-foreground">
                        <span>{statistics.providerReviewsCount} as Provider</span>
                        <span>{statistics.clientReviewsCount} as Client</span>
                      </div>
                    </div>

                    {/* Average Rating */}
                    <div className="text-center">
                      <p className="text-caption mb-2">Average Rating</p>
                      <div className="flex items-center justify-center gap-2">
                        <Star className="w-8 h-8 text-warning fill-warning" />
                        <span data-testid="average-rating" className="text-5xl font-black text-foreground">
                          {(statistics.averageOverallRating ?? 0).toFixed(1)}
                        </span>
                        <span className="text-2xl text-muted-foreground">/10</span>
                      </div>
                    </div>

                    {/* Last Review */}
                    <div className="text-center">
                      <p className="text-caption mb-2">Most Recent</p>
                      <p className="text-xl font-semibold text-foreground">
                        {statistics.mostRecentReviewDate
                          ? formatDate(statistics.mostRecentReviewDate)
                          : 'N/A'}
                      </p>
                    </div>
                  </div>

                  {/* Category Ratings */}
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-10 pt-10 border-t border-border">
                    <div className="text-center">
                      <div className="flex items-center justify-center gap-2 mb-1">
                        <CheckCircle className="w-4 h-4 text-success" />
                        <span className="text-sm text-muted-foreground">Quality</span>
                      </div>
                      <p className="text-2xl font-bold text-foreground">{(statistics.averageQualityRating ?? 0).toFixed(1)}</p>
                    </div>
                    <div className="text-center">
                      <div className="flex items-center justify-center gap-2 mb-1">
                        <MessageSquare className="w-4 h-4 text-info" />
                        <span className="text-sm text-muted-foreground">Communication</span>
                      </div>
                      <p className="text-2xl font-bold text-foreground">{(statistics.averageCommunicationRating ?? 0).toFixed(1)}</p>
                    </div>
                    <div className="text-center">
                      <div className="flex items-center justify-center gap-2 mb-1">
                        <Clock className="w-4 h-4 text-warning" />
                        <span className="text-sm text-muted-foreground">Timeliness</span>
                      </div>
                      <p className="text-2xl font-bold text-foreground">{(statistics.averageTimelinessRating ?? 0).toFixed(1)}</p>
                    </div>
                    <div className="text-center">
                      <div className="flex items-center justify-center gap-2 mb-1">
                        <Briefcase className="w-4 h-4 text-primary" />
                        <span className="text-sm text-muted-foreground">Professionalism</span>
                      </div>
                      <p className="text-2xl font-bold text-foreground">{(statistics.averageProfessionalismRating ?? 0).toFixed(1)}</p>
                    </div>
                  </div>
                </div>
              </section>

              {/* Filters */}
              <section className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 animate-slide-in">
                {/* Type Filter */}
                <div className="flex gap-2">
                  <button
                    onClick={() => handleFilterChange('all')}
                    className={`px-4 py-2 rounded-full text-sm font-semibold transition-all ${
                      filter === 'all'
                        ? 'bg-primary text-primary-foreground'
                        : 'bg-card border border-border text-muted-foreground hover:bg-muted'
                    }`}
                  >
                    All Reviews
                  </button>
                  <button
                    onClick={() => handleFilterChange('ClientToProvider')}
                    className={`px-4 py-2 rounded-full text-sm font-semibold transition-all ${
                      filter === 'ClientToProvider'
                        ? 'bg-primary text-primary-foreground'
                        : 'bg-card border border-border text-muted-foreground hover:bg-muted'
                    }`}
                  >
                    As Provider
                  </button>
                  <button
                    onClick={() => handleFilterChange('ProviderToClient')}
                    className={`px-4 py-2 rounded-full text-sm font-semibold transition-all ${
                      filter === 'ProviderToClient'
                        ? 'bg-primary text-primary-foreground'
                        : 'bg-card border border-border text-muted-foreground hover:bg-muted'
                    }`}
                  >
                    As Client
                  </button>
                </div>

                {/* Sort Options */}
                <div className="flex items-center gap-2">
                  <span className="text-sm text-muted-foreground">Sort by:</span>
                  <div className="flex gap-2">
                    <button
                      onClick={() => handleSortChange('newest')}
                      className={`px-3 py-1 rounded text-sm transition-all ${
                        sortBy === 'newest'
                          ? 'bg-secondary text-secondary-foreground'
                          : 'text-muted-foreground hover:text-foreground'
                      }`}
                    >
                      Newest First
                    </button>
                    <button
                      onClick={() => handleSortChange('oldest')}
                      className={`px-3 py-1 rounded text-sm transition-all ${
                        sortBy === 'oldest'
                          ? 'bg-secondary text-secondary-foreground'
                          : 'text-muted-foreground hover:text-foreground'
                      }`}
                    >
                      Oldest First
                    </button>
                    <button
                      onClick={() => handleSortChange('highest')}
                      className={`px-3 py-1 rounded text-sm transition-all ${
                        sortBy === 'highest'
                          ? 'bg-secondary text-secondary-foreground'
                          : 'text-muted-foreground hover:text-foreground'
                      }`}
                    >
                      Highest Rated
                    </button>
                  </div>
                </div>
              </section>

              {/* Reviews List */}
              <section className="space-y-6 animate-slide-in">
                {reviews.length === 0 ? (
                  <div className="card-elevated p-10 text-center">
                    <MessageSquare className="w-12 h-12 text-muted-foreground mx-auto mb-4" />
                    <p className="text-muted-foreground">No reviews match your filters</p>
                  </div>
                ) : (
                  reviews.map((review) => (
                    <div key={review.id} className="card-elevated p-6 lg:p-8">
                      {/* Review Header */}
                      <div className="flex flex-col md:flex-row md:items-start md:justify-between gap-4 mb-4">
                        <div>
                          <h3 className="text-lg font-bold text-foreground">{review.projectTitle}</h3>
                          <div className="flex items-center gap-2 mt-1 text-sm text-muted-foreground">
                            <User className="w-4 h-4" />
                            <span>{review.reviewerName}</span>
                            <span>•</span>
                            <Calendar className="w-4 h-4" />
                            <span>{formatDate(review.createdAt)}</span>
                          </div>
                        </div>
                        <div className="flex items-center gap-2">
                          <Star className="w-5 h-5 text-warning fill-warning" />
                          <span className="text-2xl font-bold text-foreground">{formatRating(review.overallRating)}</span>
                        </div>
                      </div>

                      {/* Review Text */}
                      <p className="text-foreground mb-4">{review.reviewText}</p>

                      {/* Response Section */}
                      {review.responseText ? (
                        <div className="bg-muted/50 rounded-lg p-4 mt-4">
                          <p className="text-sm font-semibold text-muted-foreground mb-2">Your Response:</p>
                          <p className="text-foreground">{review.responseText}</p>
                        </div>
                      ) : (
                        <button
                          onClick={() => openResponseModal(review)}
                          className="btn-secondary mt-4"
                        >
                          Respond
                        </button>
                      )}
                    </div>
                  ))
                )}
              </section>

              {/* Pagination */}
              {totalPages > 1 && (
                <section className="flex items-center justify-center gap-4 animate-fade-in">
                  <button
                    onClick={() => handlePageChange(currentPage - 1)}
                    disabled={currentPage === 1}
                    className="btn-ghost disabled:opacity-50 disabled:cursor-not-allowed"
                    aria-label="Previous page"
                  >
                    <ChevronLeft className="w-5 h-5" />
                  </button>
                  <span className="text-sm text-muted-foreground">
                    Page {currentPage} of {totalPages}
                  </span>
                  <button
                    onClick={() => handlePageChange(currentPage + 1)}
                    disabled={currentPage === totalPages}
                    className="btn-ghost disabled:opacity-50 disabled:cursor-not-allowed"
                    aria-label="Next page"
                  >
                    <ChevronRight className="w-5 h-5" />
                  </button>
                </section>
              )}
            </>
          )}

          {/* Loading State for Data */}
          {loading && !isLoading && (
            <div className="card-elevated p-10 text-center animate-fade-in">
              <div className="loading-spinner mx-auto mb-4"></div>
              <p className="text-muted-foreground">Loading reviews...</p>
            </div>
          )}
        </div>
      </main>

      {/* Response Modal */}
      {showResponseModal && selectedReview && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-overlay/80 backdrop-blur-sm animate-fade-in">
          <div className="card-elevated p-8 w-full max-w-lg animate-scale-in">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-2xl font-bold text-foreground">Respond to Review</h2>
              <button
                onClick={closeResponseModal}
                className="p-2 rounded-full hover:bg-muted transition-colors"
                aria-label="Close modal"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="mb-6">
              <p className="text-sm text-muted-foreground mb-2">Review from {selectedReview.reviewerName}:</p>
              <p className="text-foreground italic">&quot;{selectedReview.reviewText}&quot;</p>
            </div>

            <div className="mb-6">
              <label htmlFor="response" className="block text-sm font-semibold text-foreground mb-2">
                Your Response
              </label>
              <textarea
                id="response"
                value={responseText}
                onChange={(e) => setResponseText(e.target.value)}
                placeholder="Write your response to this review..."
                className="input-primary w-full h-32 resize-none"
                minLength={10}
                maxLength={1000}
              />
              <p className="text-xs text-muted-foreground mt-1">
                {responseText.length}/1000 characters (minimum 10)
              </p>
            </div>

            {responseError && (
              <div className="bg-destructive/10 border border-destructive/20 text-destructive rounded-lg p-3 mb-4 text-sm">
                {responseError}
              </div>
            )}

            <div className="flex gap-4">
              <button
                onClick={closeResponseModal}
                className="btn-secondary flex-1"
                disabled={submittingResponse}
              >
                Cancel
              </button>
              <button
                onClick={submitResponse}
                disabled={submittingResponse || responseText.length < 10}
                className="btn-primary flex-1 flex items-center justify-center gap-2"
              >
                {submittingResponse ? (
                  <>
                    <div className="loading-spinner w-4 h-4"></div>
                    Submitting...
                  </>
                ) : (
                  <>
                    <Send className="w-4 h-4" />
                    Submit Response
                  </>
                )}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
