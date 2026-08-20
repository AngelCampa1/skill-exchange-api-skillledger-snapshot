'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect, useCallback } from 'react';
import { Card, CardHeader, CardTitle, CardContent } from './ui/card';

interface ReputationData {
  userId: string;
  overallScore: number;
  reliabilityScore: number;
  qualityScore: number;
  responseScore: number;
  riskLevel: 'Low' | 'Medium' | 'High' | 'Critical';
  trustLevel: 'New' | 'Emerging' | 'Established' | 'Trusted' | 'Elite';
  lastUpdated: string;
  trends: {
    period: string;
    scoreChange: number;
    direction: 'up' | 'down' | 'stable';
  }[];
}

interface ReputationHistory {
  date: string;
  overallScore: number;
  reliabilityScore: number;
  qualityScore: number;
  responseScore: number;
  event?: string;
}

interface AccountStatus {
  hasActivePenalties: boolean;
  penaltyCount: number;
  lastPenaltyDate?: string;
  appealsCount: number;
  reviewRestricted: boolean;
  suspensionEndDate?: string;
}

export default function ReputationDashboard() {
  const [reputation, setReputation] = useState<ReputationData | null>(null);
  const [history, setHistory] = useState<ReputationHistory[]>([]);
  const [accountStatus, setAccountStatus] = useState<AccountStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedTimeframe, setSelectedTimeframe] = useState<'7d' | '30d' | '90d' | '1y'>('30d');

  const loadReputationData = useCallback(async () => {
    setLoading(true);
    try {
      const [reputationResponse, historyResponse, statusResponse] = await Promise.all([
        fetch('/api/user/reputation'),
        fetch(`/api/user/reputation/history?timeframe=${selectedTimeframe}`),
        fetch('/api/user/reputation/status')
      ]);

      if (reputationResponse.ok) {
        setReputation(await reputationResponse.json());
      }
      if (historyResponse.ok) {
        setHistory(await historyResponse.json());
      }
      if (statusResponse.ok) {
        setAccountStatus(await statusResponse.json());
      }
    } catch (error) {
      logger.error('Failed to load reputation data:', error);
    } finally {
      setLoading(false);
    }
  }, [selectedTimeframe]);

  useEffect(() => {
    loadReputationData();
  }, [selectedTimeframe, loadReputationData]);

  const getScoreColor = (score: number) => {
    if (score >= 0.8) return 'text-success';
    if (score >= 0.6) return 'text-warning';
    if (score >= 0.4) return 'text-warning';
    return 'text-destructive';
  };

  const getScoreBackground = (score: number) => {
    if (score >= 0.8) return 'bg-success/10 border-success/20';
    if (score >= 0.6) return 'bg-warning/10 border-warning/20';
    if (score >= 0.4) return 'bg-warning/10 border-warning/20';
    return 'bg-destructive/10 border-destructive/20';
  };

  const getRiskLevelColor = (level: string) => {
    switch (level) {
      case 'Low': return 'bg-success/10 text-success border-success/20';
      case 'Medium': return 'bg-warning/10 text-warning border-warning/20';
      case 'High': return 'bg-warning/10 text-warning border-warning/20';
      case 'Critical': return 'bg-destructive/10 text-destructive border-destructive/20';
      default: return 'bg-muted text-muted-foreground border-border';
    }
  };

  const getTrustLevelColor = (level: string) => {
    switch (level) {
      case 'Elite': return 'bg-primary/10 text-primary border-primary/20';
      case 'Trusted': return 'bg-primary/10 text-primary border-primary/20';
      case 'Established': return 'bg-success/10 text-success border-success/20';
      case 'Emerging': return 'bg-warning/10 text-warning border-warning/20';
      default: return 'bg-muted text-muted-foreground border-border';
    }
  };

  const formatScoreChange = (change: number) => {
    const sign = change > 0 ? '+' : '';
    return `${sign}${(change * 100).toFixed(1)}%`;
  };

  const getChangeIcon = (direction: string) => {
    switch (direction) {
      case 'up': return '📈';
      case 'down': return '📉';
      default: return '➡️';
    }
  };

  if (loading || !reputation) {
    return (
      <div className="max-w-6xl mx-auto p-6">
        <div className="animate-pulse">
          <div className="h-8 bg-muted rounded w-1/3 mb-6"></div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
            {[1, 2, 3, 4].map(i => (
              <div key={i} className="h-32 bg-muted rounded"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto p-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-foreground">Reputation Dashboard</h1>
        <p className="text-muted-foreground mt-2">
          Track your platform reputation and trust level
        </p>
      </div>

      {/* Account Status Alerts */}
      {accountStatus?.hasActivePenalties && (
        <Card className="mb-6 bg-destructive/10 border-destructive/20">
          <CardContent className="pt-6">
            <div className="flex items-start gap-3">
              <span className="text-destructive text-xl">⚠️</span>
              <div>
                <h3 className="font-medium text-destructive">Account Penalties Active</h3>
                <p className="text-sm text-destructive mt-1">
                  You have {accountStatus.penaltyCount} active penalties affecting your reputation.
                  {accountStatus.reviewRestricted && ' You are currently restricted from leaving reviews.'}
                  {accountStatus.suspensionEndDate && (
                    <> Your suspension ends on {new Date(accountStatus.suspensionEndDate).toLocaleDateString()}.</>
                  )}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Reputation Overview Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        <Card className={`border-2 ${getScoreBackground(reputation.overallScore)}`}>
          <CardHeader className="pb-3">
            <CardTitle className="text-sm font-medium text-muted-foreground">Overall Score</CardTitle>
          </CardHeader>
          <CardContent>
            <div className={`text-3xl font-bold ${getScoreColor(reputation.overallScore)}`}>
              {(reputation.overallScore * 100).toFixed(0)}
            </div>
            <div className="text-sm text-muted-foreground">out of 100</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-sm font-medium text-muted-foreground">Trust Level</CardTitle>
          </CardHeader>
          <CardContent>
            <div className={`px-3 py-1 rounded-full text-sm border ${getTrustLevelColor(reputation.trustLevel)} inline-block`}>
              {reputation.trustLevel}
            </div>
            <div className="text-xs text-muted-foreground mt-2">Platform standing</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-sm font-medium text-muted-foreground">Risk Level</CardTitle>
          </CardHeader>
          <CardContent>
            <div className={`px-3 py-1 rounded-full text-sm border ${getRiskLevelColor(reputation.riskLevel)} inline-block`}>
              {reputation.riskLevel}
            </div>
            <div className="text-xs text-muted-foreground mt-2">Fraud detection</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-sm font-medium text-muted-foreground">Last Updated</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-lg font-semibold">
              {new Date(reputation.lastUpdated).toLocaleDateString()}
            </div>
            <div className="text-xs text-muted-foreground">Scores refresh daily</div>
          </CardContent>
        </Card>
      </div>

      {/* Detailed Scores */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-8">
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Reliability Score</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-center justify-between mb-4">
              <span className={`text-2xl font-bold ${getScoreColor(reputation.reliabilityScore)}`}>
                {(reputation.reliabilityScore * 100).toFixed(0)}
              </span>
              <div className="text-right text-sm text-muted-foreground">
                <div>Commitment to projects</div>
                <div>Meeting deadlines</div>
              </div>
            </div>
            <div className="w-full bg-muted rounded-full h-2">
              <div
                className="bg-primary h-2 rounded-full"
                style={{ width: `${reputation.reliabilityScore * 100}%` }}
              ></div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Quality Score</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-center justify-between mb-4">
              <span className={`text-2xl font-bold ${getScoreColor(reputation.qualityScore)}`}>
                {(reputation.qualityScore * 100).toFixed(0)}
              </span>
              <div className="text-right text-sm text-muted-foreground">
                <div>Work quality rating</div>
                <div>Client satisfaction</div>
              </div>
            </div>
            <div className="w-full bg-muted rounded-full h-2">
              <div
                className="bg-success h-2 rounded-full"
                style={{ width: `${reputation.qualityScore * 100}%` }}
              ></div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Response Score</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex items-center justify-between mb-4">
              <span className={`text-2xl font-bold ${getScoreColor(reputation.responseScore)}`}>
                {(reputation.responseScore * 100).toFixed(0)}
              </span>
              <div className="text-right text-sm text-muted-foreground">
                <div>Communication speed</div>
                <div>Response quality</div>
              </div>
            </div>
            <div className="w-full bg-muted rounded-full h-2">
              <div
                className="bg-primary h-2 rounded-full"
                style={{ width: `${reputation.responseScore * 100}%` }}
              ></div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Trends and History */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle>Recent Trends</CardTitle>
              <select
                value={selectedTimeframe}
                onChange={(e) => setSelectedTimeframe(e.target.value as any)}
                className="text-sm border border-border rounded px-2 py-1"
              >
                <option value="7d">7 days</option>
                <option value="30d">30 days</option>
                <option value="90d">90 days</option>
                <option value="1y">1 year</option>
              </select>
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {reputation.trends.map((trend, index) => (
                <div key={index} className="flex items-center justify-between p-3 bg-muted rounded">
                  <div>
                    <div className="font-medium">{trend.period}</div>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-lg">{getChangeIcon(trend.direction)}</span>
                    <span className={`font-medium ${
                      trend.direction === 'up' ? 'text-success' :
                      trend.direction === 'down' ? 'text-destructive' :
                      'text-muted-foreground'
                    }`}>
                      {formatScoreChange(trend.scoreChange)}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Improvement Tips</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {reputation.reliabilityScore < 0.7 && (
                <div className="p-3 bg-primary/10 rounded border-l-4 border-primary">
                  <div className="font-medium text-primary">Improve Reliability</div>
                  <div className="text-sm text-primary">Complete projects on time and maintain consistent communication</div>
                </div>
              )}

              {reputation.qualityScore < 0.7 && (
                <div className="p-3 bg-success/10 rounded border-l-4 border-success">
                  <div className="font-medium text-success">Enhance Quality</div>
                  <div className="text-sm text-success">Focus on delivering high-quality work that exceeds expectations</div>
                </div>
              )}

              {reputation.responseScore < 0.7 && (
                <div className="p-3 bg-primary/10 rounded border-l-4 border-primary">
                  <div className="font-medium text-primary">Better Communication</div>
                  <div className="text-sm text-primary">Respond promptly to messages and maintain clear communication</div>
                </div>
              )}

              {reputation.overallScore >= 0.8 && (
                <div className="p-3 bg-warning/10 rounded border-l-4 border-warning">
                  <div className="font-medium text-warning">Great Job!</div>
                  <div className="text-sm text-warning">Your reputation is excellent. Keep maintaining these high standards!</div>
                </div>
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}