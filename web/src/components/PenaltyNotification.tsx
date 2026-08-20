'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect } from 'react';
import { Card, CardHeader, CardTitle, CardContent } from './ui/card';
import { Button } from './ui/button';

interface UserSanction {
  id: string;
  sanctionType: 'Warning' | 'TempSuspension' | 'PermBan' | 'ReviewRestriction';
  reason: string;
  isActive: boolean;
  expiresAt?: string;
  createdAt: string;
  canAppeal: boolean;
  hasAppealed: boolean;
}

interface PenaltyAlert {
  id: string;
  message: string;
  severity: 'Info' | 'Warning' | 'Error';
  isRead: boolean;
  createdAt: string;
}

export default function PenaltyNotification() {
  const [sanctions, setSanctions] = useState<UserSanction[]>([]);
  const [alerts, setAlerts] = useState<PenaltyAlert[]>([]);
  const [loading, setLoading] = useState(true);
  const [showAppealForm, setShowAppealForm] = useState<string | null>(null);
  const [appealText, setAppealText] = useState('');
  const [submittingAppeal, setSubmittingAppeal] = useState(false);

  useEffect(() => {
    loadUserPenalties();
  }, []);

  const loadUserPenalties = async () => {
    setLoading(true);
    try {
      const [sanctionsResponse, alertsResponse] = await Promise.all([
        fetch('/api/user/penalties/sanctions'),
        fetch('/api/user/penalties/alerts')
      ]);

      if (sanctionsResponse.ok) {
        setSanctions(await sanctionsResponse.json());
      }
      if (alertsResponse.ok) {
        setAlerts(await alertsResponse.json());
      }
    } catch (error) {
      logger.error('Failed to load penalty data:', error);
    } finally {
      setLoading(false);
    }
  };

  const markAlertAsRead = async (alertId: string) => {
    try {
      await fetch(`/api/user/penalties/alerts/${alertId}/read`, {
        method: 'POST'
      });
      setAlerts(alerts.map(alert => 
        alert.id === alertId ? { ...alert, isRead: true } : alert
      ));
    } catch (error) {
      logger.error('Failed to mark alert as read:', error);
    }
  };

  const submitAppeal = async (sanctionId: string) => {
    setSubmittingAppeal(true);
    try {
      const response = await fetch(`/api/user/penalties/sanctions/${sanctionId}/appeal`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ appealText })
      });

      if (response.ok) {
        setSanctions(sanctions.map(s => 
          s.id === sanctionId ? { ...s, hasAppealed: true } : s
        ));
        setShowAppealForm(null);
        setAppealText('');
      }
    } catch (error) {
      logger.error('Failed to submit appeal:', error);
    } finally {
      setSubmittingAppeal(false);
    }
  };

  const getSanctionSeverityColor = (type: string) => {
    switch (type) {
      case 'PermBan': return 'bg-destructive/10 border-destructive/20 text-destructive';
      case 'TempSuspension': return 'bg-warning/10 border-warning/20 text-warning';
      case 'ReviewRestriction': return 'bg-warning/10 border-warning/20 text-warning';
      default: return 'bg-primary/10 border-primary/20 text-primary';
    }
  };

  const getSanctionDescription = (type: string) => {
    switch (type) {
      case 'Warning': return 'You have received a warning for policy violations.';
      case 'TempSuspension': return 'Your account has been temporarily suspended.';
      case 'PermBan': return 'Your account has been permanently banned.';
      case 'ReviewRestriction': return 'You are temporarily restricted from leaving reviews.';
      default: return 'A penalty has been applied to your account.';
    }
  };

  const getAlertSeverityColor = (severity: string) => {
    switch (severity) {
      case 'Error': return 'bg-destructive/10 border-destructive/20 text-destructive';
      case 'Warning': return 'bg-warning/10 border-warning/20 text-warning';
      default: return 'bg-primary/10 border-primary/20 text-primary';
    }
  };

  const formatExpirationDate = (expiresAt: string) => {
    const date = new Date(expiresAt);
    const now = new Date();
    const diffMs = date.getTime() - now.getTime();
    const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24));
    
    if (diffDays <= 0) return 'Expired';
    if (diffDays === 1) return 'Expires tomorrow';
    if (diffDays <= 7) return `Expires in ${diffDays} days`;
    return `Expires on ${date.toLocaleDateString()}`;
  };

  if (loading) {
    return (
      <div className="p-4">
        <div className="animate-pulse space-y-4">
          <div className="h-6 bg-muted rounded w-1/3"></div>
          <div className="h-32 bg-muted rounded"></div>
        </div>
      </div>
    );
  }

  const unreadAlerts = alerts.filter(alert => !alert.isRead);
  const activeSanctions = sanctions.filter(sanction => sanction.isActive);

  if (unreadAlerts.length === 0 && activeSanctions.length === 0) {
    return null; // No notifications to show
  }

  return (
    <div className="fixed top-4 right-4 max-w-md z-50 space-y-4">
      {/* Unread Alerts */}
      {unreadAlerts.map(alert => (
        <Card key={alert.id} className={`${getAlertSeverityColor(alert.severity)} shadow-lg`}>
          <CardHeader className="pb-2">
            <div className="flex items-start justify-between">
              <CardTitle className="text-sm font-medium">
                {alert.severity === 'Error' ? '⚠️' : alert.severity === 'Warning' ? '⚠️' : 'ℹ️'} 
                Account Notice
              </CardTitle>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => markAlertAsRead(alert.id)}
                className="text-xs h-6 w-6 p-0"
              >
                ×
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            <p className="text-sm mb-2">{alert.message}</p>
            <p className="text-xs opacity-75">
              {new Date(alert.createdAt).toLocaleString()}
            </p>
          </CardContent>
        </Card>
      ))}

      {/* Active Sanctions */}
      {activeSanctions.map(sanction => (
        <Card key={sanction.id} className={`${getSanctionSeverityColor(sanction.sanctionType)} shadow-lg`}>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              🚨 Account Penalty
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              <div>
                <p className="text-sm font-medium">{sanction.sanctionType.replace(/([A-Z])/g, ' $1').trim()}</p>
                <p className="text-sm">{getSanctionDescription(sanction.sanctionType)}</p>
              </div>

              <div className="text-xs">
                <p><strong>Reason:</strong> {sanction.reason}</p>
                {sanction.expiresAt && (
                  <p><strong>Status:</strong> {formatExpirationDate(sanction.expiresAt)}</p>
                )}
                <p><strong>Applied:</strong> {new Date(sanction.createdAt).toLocaleDateString()}</p>
              </div>

              {sanction.canAppeal && !sanction.hasAppealed && (
                <div className="pt-2 border-t border-current/20">
                  {showAppealForm === sanction.id ? (
                    <div className="space-y-2">
                      <textarea
                        value={appealText}
                        onChange={(e) => setAppealText(e.target.value)}
                        placeholder="Explain why you believe this penalty should be reversed..."
                        className="w-full text-xs p-2 border border-input rounded resize-none"
                        rows={3}
                      />
                      <div className="flex gap-2">
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => submitAppeal(sanction.id)}
                          disabled={!appealText.trim() || submittingAppeal}
                          className="text-xs h-7"
                        >
                          Submit Appeal
                        </Button>
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => setShowAppealForm(null)}
                          className="text-xs h-7"
                        >
                          Cancel
                        </Button>
                      </div>
                    </div>
                  ) : (
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => setShowAppealForm(sanction.id)}
                      className="text-xs h-7"
                    >
                      Appeal This Penalty
                    </Button>
                  )}
                </div>
              )}

              {sanction.hasAppealed && (
                <div className="text-xs pt-2 border-t border-current/20">
                  ✉️ Appeal submitted - you will be notified of the outcome
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}