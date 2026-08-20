'use client'

import { logger } from '@/utils/logger'
import Link from 'next/link'
import { useEffect, useState, useCallback } from 'react'
import { useRouter } from 'next/navigation'
import {
  TrendingUp,
  TrendingDown,
  Minus,
  Star,
  Award,
  AlertTriangle,
  CheckCircle,
  Clock,
  MessageSquare,
  Briefcase,
  Calendar
} from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import LogoutButton from '@/components/LogoutButton'
import { ThemeToggle } from '@/components/ThemeToggle'
import { MobileNav } from '@/components/MobileNav'

interface ReputationScore {
  userId: string
  overallScore: number
  projectCompletionRate: number
  averageResponseTime: string
  totalProjectsCompleted: number
  performanceStreakBonus: number
  totalPenalties: number
  lastUpdated: string
  activeDisputes: number
  averageQualityRating: number
  averageCommunicationRating: number
  averageTimelinessRating: number
  averageProfessionalismRating: number
}

interface ReputationTrend {
  trendDirection: 'Declining' | 'Stable' | 'Improving'
  averageChangePerDay: number
  totalChange: number
  startingScore: number
  currentScore: number
  projectsInPeriod: number
  peakScore: number
  lowestScore: number
  summary: string
  daysActive: number
  totalReviews: number
  recentReviews: number
}

interface ReputationHistoryItem {
  date: string
  score: number
  projectsCompleted: number
  eventType: string
  description: string
  scoreChange: number
  changeReason: string
  projectId: string
  reviewId: string
}

type TimePeriod = 7 | 30 | 90

export default function ReputationPage() {
  const { user, isAuthenticated, isLoading } = useAuth()
  const router = useRouter()

  const [score, setScore] = useState<ReputationScore | null>(null)
  const [trend, setTrend] = useState<ReputationTrend | null>(null)
  const [history, setHistory] = useState<ReputationHistoryItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [noData, setNoData] = useState(false)
  const [selectedPeriod, setSelectedPeriod] = useState<TimePeriod>(30)

  const fetchReputationData = useCallback(async (days: TimePeriod = 30) => {
    if (!user?.id) return

    setLoading(true)
    setError(null)

    try {
      // Fetch all data in parallel
      const [scoreRes, trendRes, historyRes] = await Promise.all([
        fetch(`/api/reputation/user/${user.id}/score`, {
          method: 'GET',
          credentials: 'include',
        }),
        fetch(`/api/reputation/user/${user.id}/trend?days=${days}`, {
          method: 'GET',
          credentials: 'include',
        }),
        fetch(`/api/reputation/user/${user.id}/history?days=${days}`, {
          method: 'GET',
          credentials: 'include',
        }),
      ])

      if (scoreRes.status === 404) {
        setNoData(true)
        setLoading(false)
        return
      }

      if (!scoreRes.ok) {
        throw new Error('Failed to fetch reputation data')
      }

      const scoreData = await scoreRes.json()
      setScore(scoreData)

      if (trendRes.ok) {
        const trendData = await trendRes.json()
        setTrend(trendData)
      }

      if (historyRes.ok) {
        const historyData = await historyRes.json()
        setHistory(Array.isArray(historyData) ? historyData : [])
      }

      setNoData(false)
    } catch (err) {
      logger.error('Failed to fetch reputation data', err)
      setError('Unable to load reputation data')
    } finally {
      setLoading(false)
    }
  }, [user?.id])

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
      fetchReputationData(selectedPeriod)
    }
  }, [isAuthenticated, user?.id, fetchReputationData, selectedPeriod])

  const handlePeriodChange = (period: TimePeriod) => {
    setSelectedPeriod(period)
  }

  const getTrendIcon = () => {
    if (!trend) return <Minus className="w-6 h-6 text-muted-foreground" />
    switch (trend.trendDirection) {
      case 'Improving':
        return <TrendingUp className="w-6 h-6 text-success" />
      case 'Declining':
        return <TrendingDown className="w-6 h-6 text-destructive" />
      default:
        return <Minus className="w-6 h-6 text-warning" />
    }
  }

  const getTrendColor = () => {
    if (!trend) return 'text-muted-foreground'
    switch (trend.trendDirection) {
      case 'Improving':
        return 'text-success'
      case 'Declining':
        return 'text-destructive'
      default:
        return 'text-warning'
    }
  }

  const formatScore = (value: number) => value.toFixed(1)
  const formatPercentage = (value: number) => `${Math.round(value * 100)}%`
  const formatChange = (value: number) => value >= 0 ? `+${value.toFixed(1)}` : value.toFixed(1)

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center space-md animate-fade-in">
          <div className="loading-spinner mx-auto animate-glow"></div>
          <p className="text-body text-muted-foreground">Loading your reputation...</p>
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
              <Link href="/reviews" className="btn-ghost">Reviews</Link>
              <Link href="/projects/search" className="btn-ghost">Browse Projects</Link>
            </div>

            <div className="flex items-center space-golden-sm">
              <MobileNav
                items={[
                  { href: '/dashboard', label: 'Dashboard' },
                  { href: '/reviews', label: 'Reviews' },
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
      <main className="container-premium py-16 lg:py-24 relative" role="main" aria-label="Reputation content">
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
                    Reputation
                  </span>
                </h1>
                <p className="text-lg text-muted-foreground max-w-2xl leading-relaxed">
                  Your professional reputation score and performance metrics
                </p>
              </div>
            </div>
          </header>

          {/* Error State */}
          {error && (
            <div className="card-elevated p-10 text-center animate-fade-in">
              <AlertTriangle className="w-16 h-16 text-destructive mx-auto mb-4" />
              <h2 className="text-2xl font-bold text-foreground mb-2">Unable to load reputation data</h2>
              <p className="text-muted-foreground">Please try again later or contact support if the issue persists.</p>
            </div>
          )}

          {/* No Data State */}
          {noData && !error && (
            <div className="card-elevated p-10 text-center animate-fade-in">
              <Star className="w-16 h-16 text-primary mx-auto mb-4" />
              <h2 className="text-2xl font-bold text-foreground mb-2">No reputation data yet</h2>
              <p className="text-muted-foreground mb-6">Complete your first project to build your reputation score.</p>
              <Link href="/projects/search" className="btn-primary">
                Find Projects
              </Link>
            </div>
          )}

          {/* Data Loaded State */}
          {!error && !noData && !loading && score && (
            <>
              {/* Score Overview Card */}
              <section className="card-elevated p-10 lg:p-14 animate-slide-in relative overflow-hidden">
                <div className="absolute inset-0 bg-gradient-to-br from-primary/5 via-transparent to-secondary/5 pointer-events-none"></div>
                <div className="relative z-10">
                  <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-8">
                    {/* Overall Score */}
                    <div className="text-center lg:text-left">
                      <p className="text-caption mb-2">Overall Score</p>
                      <div className="flex items-center gap-4 justify-center lg:justify-start">
                        <span data-testid="overall-score" className="text-7xl lg:text-8xl font-black bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                          {formatScore(score.overallScore)}
                        </span>
                        <span className="text-3xl text-muted-foreground">/5</span>
                      </div>
                      {trend && (
                        <div className="flex items-center gap-2 mt-4 justify-center lg:justify-start">
                          <span data-testid="trend-indicator" className={`flex items-center gap-1 ${getTrendColor()}`}>
                            {getTrendIcon()}
                            <span className="font-semibold">{trend.trendDirection}</span>
                          </span>
                          <span className={`text-lg font-bold ${getTrendColor()}`}>
                            {formatChange(trend.totalChange)}
                          </span>
                        </div>
                      )}
                    </div>

                    {/* Trend Summary */}
                    {trend && (
                      <div className="bg-card/50 rounded-2xl p-6 max-w-md">
                        <p className="text-muted-foreground">{trend.summary}</p>
                        <div className="flex items-center gap-4 mt-4 text-sm">
                          <span className="text-muted-foreground">
                            <strong className="text-foreground">{trend.totalReviews}</strong> total reviews
                          </span>
                          <span className="text-muted-foreground">
                            <strong className="text-foreground">{trend.recentReviews}</strong> recent
                          </span>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              </section>

              {/* Category Breakdown */}
              <section className="animate-slide-in">
                <h2 className="text-3xl font-black mb-8">
                  <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                    Category Scores
                  </span>
                </h2>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                  {/* Quality */}
                  <div className="card-interactive p-6 text-center">
                    <div className="p-4 bg-gradient-to-br from-success/20 to-success/10 rounded-2xl w-fit mx-auto mb-4">
                      <CheckCircle className="w-8 h-8 text-success" />
                    </div>
                    <p className="text-caption mb-2">Quality</p>
                    <p data-testid="quality-score" className="text-4xl font-black text-foreground">
                      {formatScore(score.averageQualityRating)}
                    </p>
                  </div>

                  {/* Communication */}
                  <div className="card-interactive p-6 text-center">
                    <div className="p-4 bg-gradient-to-br from-info/20 to-info/10 rounded-2xl w-fit mx-auto mb-4">
                      <MessageSquare className="w-8 h-8 text-info" />
                    </div>
                    <p className="text-caption mb-2">Communication</p>
                    <p data-testid="communication-score" className="text-4xl font-black text-foreground">
                      {formatScore(score.averageCommunicationRating)}
                    </p>
                  </div>

                  {/* Timeliness */}
                  <div className="card-interactive p-6 text-center">
                    <div className="p-4 bg-gradient-to-br from-warning/20 to-warning/10 rounded-2xl w-fit mx-auto mb-4">
                      <Clock className="w-8 h-8 text-warning" />
                    </div>
                    <p className="text-caption mb-2">Timeliness</p>
                    <p data-testid="timeliness-score" className="text-4xl font-black text-foreground">
                      {formatScore(score.averageTimelinessRating)}
                    </p>
                  </div>

                  {/* Professionalism */}
                  <div className="card-interactive p-6 text-center">
                    <div className="p-4 bg-gradient-to-br from-primary/20 to-primary/10 rounded-2xl w-fit mx-auto mb-4">
                      <Briefcase className="w-8 h-8 text-primary" />
                    </div>
                    <p className="text-caption mb-2">Professionalism</p>
                    <p data-testid="professionalism-score" className="text-4xl font-black text-foreground">
                      {formatScore(score.averageProfessionalismRating)}
                    </p>
                  </div>
                </div>
              </section>

              {/* Performance Metrics */}
              <section className="animate-slide-in">
                <h2 className="text-3xl font-black mb-8">
                  <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                    Performance Metrics
                  </span>
                </h2>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                  {/* Projects Completed */}
                  <div className="card-elevated p-6">
                    <p className="text-caption mb-2">Projects Completed</p>
                    <p className="text-4xl font-black text-foreground">{score.totalProjectsCompleted}</p>
                  </div>

                  {/* Completion Rate */}
                  <div className="card-elevated p-6">
                    <p className="text-caption mb-2">Completion Rate</p>
                    <p className="text-4xl font-black text-success">{formatPercentage(score.projectCompletionRate)}</p>
                  </div>

                  {/* Streak Bonus */}
                  <div className="card-elevated p-6">
                    <p className="text-caption mb-2">Streak Bonus</p>
                    <p className="text-4xl font-black text-primary">{formatChange(score.performanceStreakBonus)}</p>
                  </div>

                  {/* Penalties */}
                  <div className="card-elevated p-6">
                    <p className="text-caption mb-2">Penalties</p>
                    <p className={`text-4xl font-black ${score.totalPenalties > 0 ? 'text-destructive' : 'text-success'}`}>
                      {score.totalPenalties}
                    </p>
                  </div>
                </div>
              </section>

              {/* History Section */}
              <section className="animate-slide-in">
                <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 mb-8">
                  <h2 className="text-3xl font-black">
                    <span className="bg-gradient-to-r from-primary to-secondary bg-clip-text text-transparent">
                      History
                    </span>
                  </h2>
                  {/* Time Period Filter */}
                  <div className="flex gap-2">
                    {([7, 30, 90] as TimePeriod[]).map((period) => (
                      <button
                        key={period}
                        onClick={() => handlePeriodChange(period)}
                        className={`px-4 py-2 rounded-lg text-sm font-semibold transition-all ${
                          selectedPeriod === period
                            ? 'bg-primary text-primary-foreground'
                            : 'bg-card border border-border text-muted-foreground hover:bg-muted'
                        }`}
                      >
                        {period} Days
                      </button>
                    ))}
                  </div>
                </div>

                {history.length === 0 ? (
                  <div className="card-elevated p-10 text-center">
                    <Calendar className="w-12 h-12 text-muted-foreground mx-auto mb-4" />
                    <p className="text-muted-foreground">No history events in this period</p>
                  </div>
                ) : (
                  <div className="space-y-4">
                    {history.map((item, index) => (
                      <div key={index} className="card-elevated p-6 flex items-center gap-6">
                        <div className={`p-3 rounded-full ${item.scoreChange >= 0 ? 'bg-success/10' : 'bg-destructive/10'}`}>
                          {item.scoreChange >= 0 ? (
                            <TrendingUp className={`w-6 h-6 text-success`} />
                          ) : (
                            <TrendingDown className={`w-6 h-6 text-destructive`} />
                          )}
                        </div>
                        <div className="flex-1">
                          <p className="font-semibold text-foreground">{item.description}</p>
                          <p className="text-sm text-muted-foreground">{item.changeReason}</p>
                        </div>
                        <div className="text-right">
                          <p className={`text-2xl font-bold ${item.scoreChange >= 0 ? 'text-success' : 'text-destructive'}`}>
                            {formatChange(item.scoreChange)}
                          </p>
                          <p className="text-sm text-muted-foreground">
                            {new Date(item.date).toLocaleDateString('en-US', {
                              month: 'short',
                              day: 'numeric',
                              year: 'numeric'
                            })}
                          </p>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </section>
            </>
          )}

          {/* Loading State for Data */}
          {loading && !isLoading && (
            <div className="card-elevated p-10 text-center animate-fade-in">
              <div className="loading-spinner mx-auto mb-4"></div>
              <p className="text-muted-foreground">Loading reputation data...</p>
            </div>
          )}
        </div>
      </main>
    </div>
  )
}
