'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect } from 'react';
import { Card, CardHeader, CardTitle, CardContent } from '../ui/card';
import { Button } from '../ui/button';

interface AntiGamingAlert {
  id: string;
  userId: string;
  userName: string;
  userEmail: string;
  alertType: 'BehaviorAnomaly' | 'ContentSimilarity' | 'NetworkSuspicion' | 'ReviewGaming';
  riskScore: number;
  isResolved: boolean;
  createdAt: string;
  details: string;
  evidenceItems: string[];
}

interface UserSanction {
  id: string;
  userId: string;
  userName: string;
  sanctionType: 'Warning' | 'TempSuspension' | 'PermBan' | 'ReviewRestriction';
  reason: string;
  isActive: boolean;
  expiresAt?: string;
  createdAt: string;
}

interface PendingReview {
  alertId: string;
  userName: string;
  riskScore: number;
  alertType: string;
  createdAt: string;
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
}

export default function AntiGamingDashboard() {
  const [alerts, setAlerts] = useState<AntiGamingAlert[]>([]);
  const [sanctions, setSanctions] = useState<UserSanction[]>([]);
  const [pendingReviews, setPendingReviews] = useState<PendingReview[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedAlert, setSelectedAlert] = useState<AntiGamingAlert | null>(null);
  const [actionLoading, setActionLoading] = useState(false);

  useEffect(() => {
    loadDashboardData();
  }, []);

  const loadDashboardData = async () => {
    setLoading(true);
    try {
      const [alertsResponse, sanctionsResponse, reviewsResponse] = await Promise.all([
        fetch('/api/admin/anti-gaming/alerts'),
        fetch('/api/admin/anti-gaming/sanctions'),
        fetch('/api/admin/anti-gaming/pending-reviews')
      ]);

      if (alertsResponse.ok) {
        setAlerts(await alertsResponse.json());
      }
      if (sanctionsResponse.ok) {
        setSanctions(await sanctionsResponse.json());
      }
      if (reviewsResponse.ok) {
        setPendingReviews(await reviewsResponse.json());
      }
    } catch (error) {
      logger.error('Failed to load dashboard data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleResolveAlert = async (alertId: string, action: 'dismiss' | 'warn' | 'suspend') => {
    setActionLoading(true);
    try {
      const response = await fetch(`/api/admin/anti-gaming/alerts/${alertId}/resolve`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ action })
      });

      if (response.ok) {
        await loadDashboardData();
        setSelectedAlert(null);
      } else {
        logger.error('Failed to resolve alert');
      }
    } catch (error) {
      logger.error('Error resolving alert:', error);
    } finally {
      setActionLoading(false);
    }
  };

  const getRiskScoreColor = (score: number) => {
    if (score >= 0.8) return 'text-destructive bg-destructive/10';
    if (score >= 0.6) return 'text-warning bg-warning/10';
    if (score >= 0.4) return 'text-warning bg-warning/10';
    return 'text-success bg-success/10';
  };

  // BUG-015 FIX: Use distinct colors for each priority level
  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'Critical': return 'bg-destructive/10 text-destructive border-destructive/20';
      case 'High': return 'bg-destructive/10 text-destructive border-destructive/20';
      case 'Medium': return 'bg-warning/10 text-warning border-warning/20';
      default: return 'bg-primary/10 text-primary border-primary/20';
    }
  };

  if (loading) {
    return (
      <div className="p-6">
        <div className="animate-pulse">
          <div className="h-8 bg-muted rounded w-1/4 mb-6"></div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {[1, 2, 3].map(i => (
              <div key={i} className="h-32 bg-muted rounded"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 max-w-7xl mx-auto">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-foreground">Anti-Gaming Dashboard</h1>
        <p className="text-muted-foreground mt-2">Monitor and review gaming detection alerts</p>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-sm font-medium text-muted-foreground">Pending Reviews</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{pendingReviews.length}</div>
            <div className="text-sm text-muted-foreground">Require attention</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-sm font-medium text-muted-foreground">High Risk Alerts</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-destructive">
              {alerts.filter(a => a.riskScore >= 0.8 && !a.isResolved).length}
            </div>
            <div className="text-sm text-muted-foreground">Score ≥ 0.8</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-sm font-medium text-muted-foreground">Active Sanctions</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {sanctions.filter(s => s.isActive).length}
            </div>
            <div className="text-sm text-muted-foreground">Currently enforced</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-sm font-medium text-muted-foreground">Today's Alerts</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {alerts.filter(a =>
                new Date(a.createdAt).toDateString() === new Date().toDateString()
              ).length}
            </div>
            <div className="text-sm text-muted-foreground">Last 24 hours</div>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* Pending Reviews */}
        <Card>
          <CardHeader>
            <CardTitle>Pending Reviews</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {pendingReviews.length === 0 ? (
                <p className="text-muted-foreground text-center py-8">No pending reviews</p>
              ) : (
                pendingReviews.map(review => (
                  <div key={review.alertId} className="border border-border rounded-lg p-4 hover:bg-muted">
                    <div className="flex items-center justify-between mb-2">
                      <div className="font-medium">{review.userName}</div>
                      <div className={`px-2 py-1 rounded text-xs border ${getPriorityColor(review.priority)}`}>
                        {review.priority}
                      </div>
                    </div>
                    <div className="text-sm text-muted-foreground mb-2">
                      {review.alertType} • Risk: {(review.riskScore * 100).toFixed(0)}%
                    </div>
                    <div className="flex items-center justify-between">
                      <div className="text-xs text-muted-foreground">
                        {new Date(review.createdAt).toLocaleDateString()}
                      </div>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => {
                          const alert = alerts.find(a => a.id === review.alertId);
                          if (alert) setSelectedAlert(alert);
                        }}
                      >
                        Review
                      </Button>
                    </div>
                  </div>
                ))
              )}
            </div>
          </CardContent>
        </Card>

        {/* Recent Alerts */}
        <Card>
          <CardHeader>
            <CardTitle>Recent Alerts</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {/* BUG-013 FIX: Add empty state for alerts */}
              {alerts.length === 0 ? (
                <p className="text-muted-foreground text-center py-8">No alerts to display</p>
              ) : alerts.slice(0, 5).map(alert => (
                <div key={alert.id} className="border border-border rounded-lg p-4">
                  <div className="flex items-center justify-between mb-2">
                    <div className="font-medium">{alert.userName}</div>
                    <div className={`px-2 py-1 rounded text-xs ${getRiskScoreColor(alert.riskScore)}`}>
                      {(alert.riskScore * 100).toFixed(0)}%
                    </div>
                  </div>
                  <div className="text-sm text-muted-foreground mb-2">{alert.alertType}</div>
                  <div className="flex items-center justify-between">
                    <div className="text-xs text-muted-foreground">
                      {new Date(alert.createdAt).toLocaleDateString()}
                    </div>
                    <div className={`text-xs px-2 py-1 rounded ${
                      alert.isResolved ? 'bg-success/10 text-success' : 'bg-muted text-muted-foreground'
                    }`}>
                      {alert.isResolved ? 'Resolved' : 'Pending'}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Alert Review Modal */}
      {selectedAlert && (
        <div className="fixed inset-0 bg-overlay/80 flex items-center justify-center p-4 z-50">
          <div className="bg-card rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
            <div className="p-6">
              <div className="flex justify-between items-start mb-4">
                <h2 className="text-xl font-semibold">Alert Review</h2>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setSelectedAlert(null)}
                >
                  ×
                </Button>
              </div>

              <div className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-muted-foreground">User</label>
                    <div className="mt-1 text-sm text-foreground">{selectedAlert.userName}</div>
                    <div className="text-xs text-muted-foreground">{selectedAlert.userEmail}</div>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-muted-foreground">Risk Score</label>
                    <div className={`mt-1 text-sm px-2 py-1 rounded inline-block ${getRiskScoreColor(selectedAlert.riskScore)}`}>
                      {(selectedAlert.riskScore * 100).toFixed(1)}%
                    </div>
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-muted-foreground">Alert Type</label>
                  <div className="mt-1 text-sm text-foreground">{selectedAlert.alertType}</div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-muted-foreground">Details</label>
                  <div className="mt-1 text-sm text-foreground bg-muted p-3 rounded">
                    {selectedAlert.details}
                  </div>
                </div>

                {selectedAlert.evidenceItems.length > 0 && (
                  <div>
                    <label className="block text-sm font-medium text-muted-foreground">Evidence</label>
                    <ul className="mt-1 text-sm text-foreground space-y-1">
                      {selectedAlert.evidenceItems.map((item, index) => (
                        <li key={index} className="bg-muted p-2 rounded">• {item}</li>
                      ))}
                    </ul>
                  </div>
                )}

                <div className="flex gap-3 pt-4 border-t">
                  <Button
                    variant="outline"
                    onClick={() => handleResolveAlert(selectedAlert.id, 'dismiss')}
                    disabled={actionLoading}
                  >
                    Dismiss
                  </Button>
                  <Button
                    variant="secondary"
                    onClick={() => handleResolveAlert(selectedAlert.id, 'warn')}
                    disabled={actionLoading}
                  >
                    Issue Warning
                  </Button>
                  <Button
                    variant="destructive"
                    onClick={() => handleResolveAlert(selectedAlert.id, 'suspend')}
                    disabled={actionLoading}
                  >
                    Suspend User
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}