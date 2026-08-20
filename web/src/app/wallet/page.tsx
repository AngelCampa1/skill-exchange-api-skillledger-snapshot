'use client'

import { logger } from '@/utils/logger';
import { trackEvent } from '@/utils/analytics';
import { fetchWithAuth } from '@/utils/apiClient';

import { useEffect, useState } from 'react'
import { useAuth } from '@/contexts/AuthContext'
import { useRouter } from 'next/navigation'
import { Wallet, TrendingUp, TrendingDown, Clock, DollarSign, X, AlertCircle, CheckCircle } from 'lucide-react'
import LogoutButton from '@/components/LogoutButton'
import { ThemeToggle } from '@/components/ThemeToggle'
import Link from 'next/link'

interface Transaction {
  id: string
  type: 'credit' | 'debit'
  amount: number
  description: string
  date: string
  status: string
}

// BUG-011/012 FIX: Credit packages for internal credit transactions
const creditPackages = [
  { id: 'starter', name: 'Starter', credits: 100, description: 'Perfect for small projects' },
  { id: 'professional', name: 'Professional', credits: 500, description: 'Great for regular freelancers' },
  { id: 'business', name: 'Business', credits: 1000, description: 'Best value for active users' },
  { id: 'enterprise', name: 'Enterprise', credits: 5000, description: 'For power users and agencies' },
]

export default function WalletPage() {
  const { user, isAuthenticated, isLoading } = useAuth()
  const router = useRouter()
  const [balance, setBalance] = useState(0)
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [loading, setLoading] = useState(true)

  // BUG-010 FIX: Additional wallet stats from API
  const [totalEarned, setTotalEarned] = useState(0)
  const [totalSpent, setTotalSpent] = useState(0)
  const [pendingBalance, setPendingBalance] = useState(0)

  // BUG-011 FIX: Modal states for Add Credits
  const [showAddCreditsModal, setShowAddCreditsModal] = useState(false)
  const [selectedPackage, setSelectedPackage] = useState<string | null>(null)
  const [actionLoading, setActionLoading] = useState(false)
  const [actionMessage, setActionMessage] = useState<{ type: 'success' | 'error', text: string } | null>(null)

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      // E2E-017 FIX: Call logout API to clear any stale cookies before redirecting
      fetch('/api/auth/logout', { method: 'POST', credentials: 'include' })
        .finally(() => {
          window.location.href = '/login'
        })
      return
    }

    if (isAuthenticated && user) {
      // Fetch wallet data
      fetchWalletData()
    }
  }, [isAuthenticated, isLoading, user, router])

  const fetchWalletData = async () => {
    try {
      // BUG-010 FIX: Integrate with real wallet API endpoint
      const response = await fetch('/api/credit-wallet', {
        method: 'GET',
        credentials: 'include',
      })

      if (response.ok) {
        const data = await response.json()

        // Map API response to component state
        setBalance(data.wallet?.currentBalance ?? 0)
        setTotalEarned(data.wallet?.totalEarned ?? 0)
        setTotalSpent(data.wallet?.totalSpent ?? 0)
        setPendingBalance(data.wallet?.pendingBalance ?? 0)

        // Map recent transactions from API to component format
        if (data.recentTransactions && Array.isArray(data.recentTransactions)) {
          const mappedTransactions: Transaction[] = data.recentTransactions.map((t: {
            transactionId: string
            type: string
            amount: number
            description: string
            createdAt: string
            status: string
            wasIncoming: boolean
          }) => ({
            id: t.transactionId,
            type: t.wasIncoming ? 'credit' : 'debit',
            amount: t.amount,
            description: t.description || 'Transaction',
            date: t.createdAt,
            status: t.status?.toLowerCase() || 'completed'
          }))
          setTransactions(mappedTransactions)
        }
      } else if (response.status === 404) {
        // New user without wallet - use default values
        setBalance(0)
        setTransactions([])
      } else {
        logger.error('Failed to fetch wallet data:', response.status)
        // Fallback to empty state
        setBalance(0)
        setTransactions([])
      }
    } catch (error) {
      logger.error('Failed to fetch wallet data:', error)
      // Fallback to empty state on network error
      setBalance(0)
      setTransactions([])
    } finally {
      setLoading(false)
    }
  }

  // BUG-011 FIX: Handle adding credits
  const handleAddCredits = async () => {
    if (!selectedPackage) {
      setActionMessage({ type: 'error', text: 'Please select a credit package' })
      return
    }

    const pkg = creditPackages.find(p => p.id === selectedPackage)
    if (!pkg) return

    setActionLoading(true)
    setActionMessage(null)

    try {
      // BUG-003 FIX: Use the correct add-credits endpoint instead of transfer
      await fetchWithAuth('/api/credit-wallet/add-credits', {
        method: 'POST',
        body: JSON.stringify({
          amount: pkg.credits,
          description: `Credit package: ${pkg.name}`,
          packageId: pkg.id,
        }),
      })

      setBalance(prev => prev + pkg.credits)
      setTransactions(prev => [
        {
          id: Date.now().toString(),
          type: 'credit',
          amount: pkg.credits,
          description: `Credit package: ${pkg.name}`,
          date: new Date().toISOString(),
          status: 'completed'
        },
        ...prev
      ])

      // Track credit purchase
      trackEvent({
        name: 'credit_transfer',
        category: 'credits',
        priority: 'critical',
        properties: {
          transaction_type: 'credit',
          amount: pkg.credits,
          package_id: pkg.id,
          package_name: pkg.name,
          success: true,
        },
      })

      // Refresh wallet data from API to ensure accurate balance
      await fetchWalletData()

      setActionMessage({ type: 'success', text: `Successfully added ${pkg.credits} credits!` })
      // Increased timeout to 5 seconds for better UX
      setTimeout(() => {
        setShowAddCreditsModal(false)
        setSelectedPackage(null)
        setActionMessage(null)
      }, 5000)
    } catch (error) {
      logger.error('Failed to add credits:', error)
      setActionMessage({ type: 'error', text: error instanceof Error ? error.message : 'Failed to add credits. Please try again.' })
    } finally {
      setActionLoading(false)
    }
  }

  if (isLoading || loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center space-md animate-fade-in">
          <div className="loading-spinner mx-auto animate-glow"></div>
          <p className="text-body text-muted-foreground">Loading your wallet...</p>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return null
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-background via-primary/5 to-secondary/10">
      {/* Navigation */}
      <nav className="nav-blur border-b border-border/50 sticky top-0 z-50 backdrop-blur-xl bg-background/80">
        <div className="container-responsive py-4">
          <div className="flex items-center justify-between">
            <Link href="/" className="text-title font-bold text-foreground hover:text-primary transition-colors">
              SkillLedger
            </Link>
            <Link href="/" className="text-body text-foreground/70 hover:text-foreground transition-colors px-4">
              Dashboard
            </Link>
            <div className="flex items-center gap-4">
              <ThemeToggle />
              {user && (
                <div className="flex items-center gap-4">
                  <span className="text-sm text-muted-foreground hidden md:inline">
                    {/* E2E-015 FIX: Display firstName if available, fallback to email */}
                    Welcome back, <span className="text-foreground font-medium">{user.firstName || user.email}</span>
                  </span>
                  <LogoutButton />
                </div>
              )}
            </div>
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="container-responsive py-12">
        <div className="max-w-6xl mx-auto space-golden-lg">
          {/* Header */}
          <div className="space-golden-sm">
            <h1 className="text-display gradient-text">Premium Wallet</h1>
            <p className="text-heading text-muted-foreground leading-relaxed">
              Manage your credits and track your financial activity
            </p>
          </div>

          {/* Balance Card */}
          <div className="card-elevated p-8 space-golden-md" data-testid="wallet-balance">
            <div className="flex items-center justify-between mb-6">
              <div className="flex items-center gap-4">
                <div className="p-4 bg-gradient-to-br from-primary/20 to-primary/10 rounded-2xl">
                  <Wallet className="w-8 h-8 text-primary" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground mb-1">Available Balance</p>
                  <p className="text-display gradient-text" data-testid="balance-amount">
                    {balance} <span className="text-heading">credits</span>
                  </p>
                </div>
              </div>
              {/* BUG-011 FIX: Added onClick handler for Add Credits button */}
              <button
                className="btn-primary"
                onClick={() => setShowAddCreditsModal(true)}
              >
                <DollarSign className="w-4 h-4" />
                Add Credits
              </button>
            </div>

            {/* Quick Stats - BUG-010 FIX: Display real data from API */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 pt-6 border-t border-border/50">
              <div className="space-md">
                <div className="flex items-center gap-2 text-muted-foreground mb-1">
                  <TrendingUp className="w-4 h-4" />
                  <span className="text-sm">Total Earned</span>
                </div>
                <p className="text-subheading text-foreground font-semibold">{totalEarned.toLocaleString()} credits</p>
              </div>
              <div className="space-md">
                <div className="flex items-center gap-2 text-muted-foreground mb-1">
                  <TrendingDown className="w-4 h-4" />
                  <span className="text-sm">Total Spent</span>
                </div>
                <p className="text-subheading text-foreground font-semibold">{totalSpent.toLocaleString()} credits</p>
              </div>
              <div className="space-md">
                <div className="flex items-center gap-2 text-muted-foreground mb-1">
                  <Clock className="w-4 h-4" />
                  <span className="text-sm">Pending</span>
                </div>
                <p className="text-subheading text-foreground font-semibold">{pendingBalance.toLocaleString()} credits</p>
              </div>
            </div>
          </div>

          {/* Recent Transactions */}
          <div className="card p-8 space-golden-md">
            <h2 className="text-title text-foreground mb-6">Recent Transactions</h2>
            
            {transactions.length === 0 ? (
              <div className="text-center py-12 space-golden-sm">
                <div className="w-16 h-16 bg-muted/50 rounded-full flex items-center justify-center mx-auto mb-4">
                  <Wallet className="w-8 h-8 text-muted-foreground" />
                </div>
                <p className="text-body text-muted-foreground">No transactions yet</p>
                <p className="text-sm text-muted-foreground/70">Your transaction history will appear here</p>
              </div>
            ) : (
              <div className="space-y-4">
                {transactions.map((tx) => (
                  <div 
                    key={tx.id} 
                    className="flex items-center justify-between p-4 rounded-lg border border-border/50 hover:border-primary/30 transition-colors"
                  >
                    <div className="flex items-center gap-4">
                      <div className={`p-3 rounded-full ${tx.type === 'credit' ? 'bg-success/10' : 'bg-destructive/10'}`}>
                        {tx.type === 'credit' ? (
                          <TrendingUp className="w-5 h-5 text-success" />
                        ) : (
                          <TrendingDown className="w-5 h-5 text-destructive" />
                        )}
                      </div>
                      <div>
                        <p className="text-body text-foreground font-medium">{tx.description}</p>
                        <p className="text-sm text-muted-foreground">
                          {new Date(tx.date).toLocaleDateString('en-US', {
                            month: 'short',
                            day: 'numeric',
                            year: 'numeric'
                          })}
                        </p>
                      </div>
                    </div>
                    <div className="text-right">
                      <p className={`text-subheading font-semibold ${tx.type === 'credit' ? 'text-success' : 'text-destructive'}`}>
                        {tx.type === 'credit' ? '+' : '-'}{tx.amount} credits
                      </p>
                      <p className="text-xs text-muted-foreground capitalize">{tx.status}</p>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </main>

      {/* BUG-011 FIX: Add Credits Modal */}
      {showAddCreditsModal && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center z-50 animate-fade-in">
          <div className="card-elevated max-w-lg w-full mx-4 p-6 space-golden-md">
            <div className="flex items-center justify-between mb-6">
              <h3 className="text-title text-foreground">Add Credits</h3>
              <button
                onClick={() => {
                  setShowAddCreditsModal(false)
                  setSelectedPackage(null)
                  setActionMessage(null)
                }}
                className="p-2 hover:bg-muted rounded-lg transition-colors"
              >
                <X className="w-5 h-5 text-muted-foreground" />
              </button>
            </div>

            {actionMessage && (
              <div className={`flex items-center gap-2 p-3 rounded-lg mb-4 ${
                actionMessage.type === 'success' ? 'bg-success/10 text-success' : 'bg-destructive/10 text-destructive'
              }`}>
                {actionMessage.type === 'success' ? (
                  <CheckCircle className="w-5 h-5" />
                ) : (
                  <AlertCircle className="w-5 h-5" />
                )}
                <span className="text-sm">{actionMessage.text}</span>
              </div>
            )}

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-6">
              {creditPackages.map((pkg) => (
                <button
                  key={pkg.id}
                  onClick={() => setSelectedPackage(pkg.id)}
                  className={`p-4 rounded-lg border-2 text-left transition-all ${
                    selectedPackage === pkg.id
                      ? 'border-primary bg-primary/10'
                      : 'border-border hover:border-primary/50'
                  }`}
                >
                  <p className="text-subheading font-semibold text-foreground">{pkg.name}</p>
                  <p className="text-heading text-primary font-bold">{pkg.credits} credits</p>
                  <p className="text-sm text-muted-foreground mt-1">{pkg.description}</p>
                </button>
              ))}
            </div>

            <div className="flex gap-3 justify-end">
              <button
                onClick={() => {
                  setShowAddCreditsModal(false)
                  setSelectedPackage(null)
                  setActionMessage(null)
                }}
                className="btn-secondary"
                disabled={actionLoading}
              >
                Cancel
              </button>
              <button
                onClick={handleAddCredits}
                className="btn-primary"
                disabled={!selectedPackage || actionLoading}
              >
                {actionLoading ? (
                  <span className="flex items-center gap-2">
                    <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                    Processing...
                  </span>
                ) : (
                  'Add Credits'
                )}
              </button>
            </div>
          </div>
        </div>
      )}

    </div>
  )
}


