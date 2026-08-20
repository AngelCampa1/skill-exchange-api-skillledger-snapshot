'use client'

import { logger } from '@/utils/logger';

import { useState, useEffect } from 'react'
import {
  CreditCard,
  Plus,
  Trash2,
  Check,
  AlertCircle,
  Loader2,
  Shield,
  Calendar,
  Star,
  Edit2,
  RefreshCw
} from 'lucide-react'
import { PaymentMethod } from '@/types/subscription'
import { useSubscription } from '@/lib/subscription-api'
import { AUTH_CONFIG } from '../constants/auth';

interface PaymentMethodManagerProps {
  className?: string
}

export function PaymentMethodManager({ className = '' }: PaymentMethodManagerProps) {
  const [paymentMethods, setPaymentMethods] = useState<PaymentMethod[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isDeleting, setIsDeleting] = useState<string | null>(null)
  const [isSettingDefault, setIsSettingDefault] = useState<string | null>(null)

  const { setupPaymentMethod } = useSubscription()

  useEffect(() => {
    fetchPaymentMethods()
  }, [])

  const fetchPaymentMethods = async () => {
    try {
      setLoading(true)
      setError(null)

      const response = await fetch('/api/Subscription/payment-methods', {
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          // BUG-FE-002 FIX: Removed - Auth via httpOnly cookies
        }
      })

      if (!response.ok) {
        throw new Error('Failed to fetch payment methods')
      }

      const data = await response.json()
      // API returns array directly, not wrapped in data property
      setPaymentMethods(Array.isArray(data) ? data : data.data || [])
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load payment methods')
    } finally {
      setLoading(false)
    }
  }

  const handleSyncPaymentMethods = async () => {
    try {
      setLoading(true)
      setError(null)

      const response = await fetch('/api/Subscription/payment-methods/sync', {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          // BUG-FE-002 FIX: Removed - Auth via httpOnly cookies
        }
      })

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}))
        throw new Error(errorData.message || 'Failed to sync payment methods')
      }

      const data = await response.json()
      // API returns array directly
      setPaymentMethods(Array.isArray(data) ? data : data.data || [])
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to sync payment methods')
    } finally {
      setLoading(false)
    }
  }

  const handleAddPaymentMethod = async () => {
    try {
      const result = await setupPaymentMethod()

      if (result.success && result.sessionUrl) {
        window.location.href = result.sessionUrl
      }
    } catch (err) {
      logger.error('Failed to setup payment method:', err)
    }
  }

  const handleSetDefault = async (paymentMethodId: string) => {
    try {
      setIsSettingDefault(paymentMethodId)

      const response = await fetch(`/api/Subscription/payment-methods/${paymentMethodId}/set-default`, {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          // BUG-FE-002 FIX: Removed - Auth via httpOnly cookies
        }
      })

      if (!response.ok) {
        throw new Error('Failed to set default payment method')
      }

      // Refresh the payment methods list
      await fetchPaymentMethods()
    } catch (err) {
      logger.error('Failed to set default payment method:', err)
    } finally {
      setIsSettingDefault(null)
    }
  }

  const handleDeletePaymentMethod = async (paymentMethodId: string) => {
    if (!confirm('Are you sure you want to remove this payment method?')) {
      return
    }

    try {
      setIsDeleting(paymentMethodId)

      const response = await fetch(`/api/Subscription/payment-methods/${paymentMethodId}`, {
        method: 'DELETE',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          // BUG-FE-002 FIX: Removed - Auth via httpOnly cookies
        }
      })

      if (!response.ok) {
        throw new Error('Failed to delete payment method')
      }

      // Remove from local state
      setPaymentMethods(prev => prev.filter(pm => pm.id !== paymentMethodId))
    } catch (err) {
      logger.error('Failed to delete payment method:', err)
    } finally {
      setIsDeleting(null)
    }
  }

  const getCardIcon = (brand: string) => {
    const brandLower = brand.toLowerCase()

    if (brandLower.includes('visa')) return '💳'
    if (brandLower.includes('mastercard')) return '💳'
    if (brandLower.includes('amex')) return '💳'
    if (brandLower.includes('discover')) return '💳'

    return <CreditCard className="w-6 h-6" />
  }

  const getCardBrandDisplay = (brand: string) => {
    const brandLower = brand.toLowerCase()

    if (brandLower.includes('visa')) return 'Visa'
    if (brandLower.includes('mastercard')) return 'Mastercard'
    if (brandLower.includes('amex')) return 'American Express'
    if (brandLower.includes('discover')) return 'Discover'

    return brand
  }

  const formatDate = (month?: number, year?: number) => {
    if (!month || !year) return ''

    const monthStr = month.toString().padStart(2, '0')
    const yearStr = year.toString().slice(-2)

    return `${monthStr}/${yearStr}`
  }

  if (loading) {
    return (
      <div className={`flex flex-col items-center justify-center py-12 ${className}`}>
        <Loader2 className="w-8 h-8 animate-spin text-primary mb-4" />
        <p className="text-body text-muted-foreground">Loading payment methods...</p>
      </div>
    )
  }

  return (
    <div className={`space-y-6 ${className}`}>
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-subheading text-foreground">Payment Methods</h3>
          <p className="text-caption text-muted-foreground">
            Manage your payment methods for subscription billing
          </p>
        </div>

        <button
          onClick={handleAddPaymentMethod}
          className="btn-primary flex items-center space-golden-sm"
        >
          <Plus className="w-4 h-4" />
          Add Payment Method
        </button>
      </div>

      {error && (
        <div className="card-error p-4">
          <div className="flex items-center space-golden-sm">
            <AlertCircle className="w-5 h-5 text-error" />
            <span className="text-body text-error">{error}</span>
          </div>
        </div>
      )}

      {paymentMethods.length === 0 ? (
        <div className="card-interactive p-12 text-center">
          <CreditCard className="w-12 h-12 text-muted mx-auto mb-4" />
          <h4 className="text-subheading text-foreground mb-2">No payment methods</h4>
          <p className="text-body text-muted-foreground mb-6">
            Add a payment method to manage your subscription billing
          </p>
          <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
            <button
              onClick={handleAddPaymentMethod}
              className="btn-primary"
            >
              Add Your First Payment Method
            </button>
            <button
              onClick={handleSyncPaymentMethods}
              disabled={loading}
              className="btn-ghost flex items-center space-golden-sm"
            >
              {loading ? (
                <Loader2 className="w-4 h-4 animate-spin" />
              ) : (
                <RefreshCw className="w-4 h-4" />
              )}
              <span>Sync from Stripe</span>
            </button>
          </div>
        </div>
      ) : (
        <div className="space-y-4">
          {paymentMethods.map((paymentMethod) => (
            <div
              key={paymentMethod.id}
              className={`card-interactive p-6 transition-all duration-200 ${
                paymentMethod.isDefault
                  ? 'ring-2 ring-success ring-offset-2 ring-offset-background'
                  : ''
              }`}
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center space-golden-md">
                  <div className="text-2xl mr-4">
                    {getCardIcon(paymentMethod.brand)}
                  </div>

                  <div>
                    <div className="flex items-center space-golden-sm">
                      <span className="text-body font-semibold text-foreground">
                        {getCardBrandDisplay(paymentMethod.brand)}
                      </span>

                      {paymentMethod.isDefault && (
                        <span className="status-success text-xs">Default</span>
                      )}
                    </div>

                    <div className="flex items-center space-golden-sm text-sm text-muted-foreground mt-1">
                      <span>•••• {paymentMethod.last4}</span>
                      <span>•</span>
                      <span>{formatDate(paymentMethod.expiryMonth, paymentMethod.expiryYear)}</span>
                    </div>

                    <div className="text-xs text-muted-foreground mt-1">
                      Added {new Date(paymentMethod.createdAt).toLocaleDateString()}
                    </div>
                  </div>
                </div>

                <div className="flex items-center space-golden-sm">
                  {!paymentMethod.isDefault && (
                    <button
                      onClick={() => handleSetDefault(paymentMethod.id)}
                      disabled={isSettingDefault === paymentMethod.id}
                      className="btn-ghost text-sm flex items-center space-golden-sm"
                    >
                      {isSettingDefault === paymentMethod.id ? (
                        <Loader2 className="w-4 h-4 animate-spin" />
                      ) : (
                        <Star className="w-4 h-4" />
                      )}
                      Set Default
                    </button>
                  )}

                  <button
                    onClick={() => handleDeletePaymentMethod(paymentMethod.id)}
                    disabled={isDeleting === paymentMethod.id || paymentMethod.isDefault}
                    className="btn-ghost text-sm text-error flex items-center space-golden-sm disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {isDeleting === paymentMethod.id ? (
                      <Loader2 className="w-4 h-4 animate-spin" />
                    ) : (
                      <Trash2 className="w-4 h-4" />
                    )}
                    Remove
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Security Notice */}
      <div className="bg-muted/50 rounded-xl p-6 border border-border/50">
        <div className="flex items-start space-golden-sm">
          <Shield className="w-5 h-5 text-success mt-0.5 flex-shrink-0" />
          <div className="space-y-1">
            <h4 className="text-subheading text-foreground">Secure Payment Processing</h4>
            <p className="text-body text-muted-foreground text-sm">
              Your payment information is encrypted and securely processed by Stripe. We never store your full card details on our servers.
            </p>
          </div>
        </div>
      </div>

      {/* Billing Information */}
      <div className="bg-muted/30 rounded-xl p-6">
        <h4 className="text-subheading text-foreground mb-4">Billing Information</h4>
        <div className="space-y-3 text-sm">
          <div className="flex items-center justify-between">
            <span className="text-muted-foreground">Next billing date</span>
            <span className="text-foreground font-medium">
              {/* This would come from subscription data */}
              Not applicable
            </span>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-muted-foreground">Currency</span>
            <span className="text-foreground font-medium">USD</span>
          </div>
          <div className="flex items-center justify-between">
            <span className="text-muted-foreground">Tax</span>
            <span className="text-foreground font-medium">Calculated at checkout</span>
          </div>
        </div>
      </div>
    </div>
  )
}