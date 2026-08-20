'use client'

import { logger } from '@/utils/logger';

import React, { useState, useEffect } from 'react';
import { Card, CardHeader, CardTitle, CardContent } from './ui/card';
import { Button } from './ui/button';

interface Appeal {
  id: string;
  sanctionId: string;
  sanctionType: string;
  reason: string;
  appealText: string;
  status: 'Pending' | 'UnderReview' | 'Approved' | 'Rejected';
  submittedAt: string;
  reviewedAt?: string;
  reviewNotes?: string;
  reviewedBy?: string;
}

interface AppealFormData {
  sanctionId: string;
  appealText: string;
  supportingEvidence?: string[];
}

interface Sanction {
  id: string;
  sanctionType: string;
  reason: string;
  severity?: number;
  issuedAt?: string;
}

export default function AppealProcess() {
  const [appeals, setAppeals] = useState<Appeal[]>([]);
  const [availableSanctions, setAvailableSanctions] = useState<Sanction[]>([]);
  const [loading, setLoading] = useState(true);
  const [showNewAppealForm, setShowNewAppealForm] = useState(false);
  const [newAppeal, setNewAppeal] = useState<AppealFormData>({
    sanctionId: '',
    appealText: '',
    supportingEvidence: []
  });
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    loadAppeals();
    loadAvailableSanctions();
  }, []);

  const loadAppeals = async () => {
    try {
      const response = await fetch('/api/user/appeals');
      if (response.ok) {
        setAppeals(await response.json());
      }
    } catch (error) {
      logger.error('Failed to load appeals:', error);
    }
  };

  const loadAvailableSanctions = async () => {
    try {
      const response = await fetch('/api/user/penalties/sanctions?appealable=true');
      if (response.ok) {
        setAvailableSanctions(await response.json());
      }
    } catch (error) {
      logger.error('Failed to load available sanctions:', error);
    } finally {
      setLoading(false);
    }
  };

  const submitAppeal = async () => {
    if (!newAppeal.sanctionId || !newAppeal.appealText.trim()) return;

    setSubmitting(true);
    try {
      const response = await fetch('/api/user/appeals', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newAppeal)
      });

      if (response.ok) {
        await loadAppeals();
        await loadAvailableSanctions();
        setShowNewAppealForm(false);
        setNewAppeal({
          sanctionId: '',
          appealText: '',
          supportingEvidence: []
        });
      }
    } catch (error) {
      logger.error('Failed to submit appeal:', error);
    } finally {
      setSubmitting(false);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Approved': return 'bg-success/10 border-success/20 text-success';
      case 'Rejected': return 'bg-destructive/10 border-destructive/20 text-destructive';
      case 'UnderReview': return 'bg-primary/10 border-primary/20 text-primary';
      default: return 'bg-warning/10 border-warning/20 text-warning';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'Approved': return '✅';
      case 'Rejected': return '❌';
      case 'UnderReview': return '👀';
      default: return '⏳';
    }
  };

  if (loading) {
    return (
      <div className="max-w-4xl mx-auto p-6">
        <div className="animate-pulse">
          <div className="h-8 bg-muted rounded w-1/3 mb-6"></div>
          <div className="space-y-4">
            {[1, 2].map(i => (
              <div key={i} className="h-48 bg-muted rounded"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto p-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-foreground">Appeal Process</h1>
        <p className="text-muted-foreground mt-2">
          Submit appeals for account penalties and track their progress
        </p>
      </div>

      {/* Appeal Guidelines */}
      <Card className="mb-8">
        <CardHeader>
          <CardTitle>Appeal Guidelines</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="prose text-sm">
            <p className="font-semibold mb-2">Before submitting an appeal:</p>
            <ul className="list-disc list-inside space-y-1 mb-4">
              <li>Review our platform policies to understand the violation</li>
              <li>Gather any evidence that supports your case</li>
              <li>Be honest and provide specific details</li>
              <li>Appeals are reviewed within 5-7 business days</li>
            </ul>
            
            <p className="font-semibold mb-2">What to include:</p>
            <ul className="list-disc list-inside space-y-1">
              <li>Explanation of why you believe the penalty was incorrect</li>
              <li>Any relevant context or circumstances</li>
              <li>Screenshots or documentation if applicable</li>
              <li>Acknowledgment if you made an honest mistake</li>
            </ul>
          </div>
        </CardContent>
      </Card>

      {/* New Appeal Form */}
      {availableSanctions.length > 0 && (
        <Card className="mb-8">
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle>Submit New Appeal</CardTitle>
              <Button
                variant="outline"
                onClick={() => setShowNewAppealForm(!showNewAppealForm)}
              >
                {showNewAppealForm ? 'Cancel' : 'New Appeal'}
              </Button>
            </div>
          </CardHeader>
          {showNewAppealForm && (
            <CardContent>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-muted-foreground mb-2">
                    Select Penalty to Appeal
                  </label>
                  <select
                    value={newAppeal.sanctionId}
                    onChange={(e) => setNewAppeal({...newAppeal, sanctionId: e.target.value})}
                    className="w-full p-2 border border-input rounded-md"
                  >
                    <option value="">Choose a penalty...</option>
                    {availableSanctions.map(sanction => (
                      <option key={sanction.id} value={sanction.id}>
                        {sanction.sanctionType.replace(/([A-Z])/g, ' $1').trim()} - {sanction.reason}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-muted-foreground mb-2">
                    Appeal Statement *
                  </label>
                  <textarea
                    value={newAppeal.appealText}
                    onChange={(e) => setNewAppeal({...newAppeal, appealText: e.target.value})}
                    placeholder="Please explain why you believe this penalty should be reversed. Be specific and honest about the circumstances."
                    className="w-full p-3 border border-input rounded-md resize-none"
                    rows={6}
                    maxLength={2000}
                  />
                  <div className="text-xs text-muted-foreground mt-1">
                    {newAppeal.appealText.length}/2000 characters
                  </div>
                </div>

                <div className="flex gap-3">
                  <Button
                    onClick={submitAppeal}
                    disabled={!newAppeal.sanctionId || !newAppeal.appealText.trim() || submitting}
                  >
                    {submitting ? 'Submitting...' : 'Submit Appeal'}
                  </Button>
                  <Button
                    variant="outline"
                    onClick={() => setShowNewAppealForm(false)}
                  >
                    Cancel
                  </Button>
                </div>
              </div>
            </CardContent>
          )}
        </Card>
      )}

      {/* Existing Appeals */}
      <div className="space-y-6">
        <h2 className="text-xl font-semibold">Your Appeals</h2>
        
        {appeals.length === 0 ? (
          <Card>
            <CardContent className="text-center py-12">
              <p className="text-muted-foreground">No appeals submitted yet</p>
            </CardContent>
          </Card>
        ) : (
          appeals.map(appeal => (
            <Card key={appeal.id}>
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div>
                    <CardTitle className="text-lg">
                      {appeal.sanctionType.replace(/([A-Z])/g, ' $1').trim()} Appeal
                    </CardTitle>
                    <p className="text-sm text-muted-foreground mt-1">
                      Submitted {new Date(appeal.submittedAt).toLocaleDateString()}
                    </p>
                  </div>
                  <div className={`px-3 py-1 rounded-full text-sm border ${getStatusColor(appeal.status)}`}>
                    {getStatusIcon(appeal.status)} {appeal.status}
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div>
                    <p className="font-medium text-foreground mb-2">Original Penalty Reason:</p>
                    <p className="text-sm text-muted-foreground bg-muted p-3 rounded">
                      {appeal.reason}
                    </p>
                  </div>

                  <div>
                    <p className="font-medium text-foreground mb-2">Your Appeal:</p>
                    <p className="text-sm text-muted-foreground bg-primary/10 p-3 rounded">
                      {appeal.appealText}
                    </p>
                  </div>

                  {appeal.reviewedAt && appeal.reviewNotes && (
                    <div>
                      <p className="font-medium text-foreground mb-2">Review Decision:</p>
                      <div className="bg-muted p-3 rounded">
                        <p className="text-sm text-muted-foreground mb-2">{appeal.reviewNotes}</p>
                        <p className="text-xs text-muted-foreground">
                          Reviewed on {new Date(appeal.reviewedAt).toLocaleDateString()}
                          {appeal.reviewedBy && ` by ${appeal.reviewedBy}`}
                        </p>
                      </div>
                    </div>
                  )}

                  {appeal.status === 'Pending' && (
                    <div className="bg-warning/10 p-3 rounded border border-warning/20">
                      <p className="text-sm text-warning">
                        ⏳ Your appeal is in queue for review. You'll be notified once it's been processed.
                      </p>
                    </div>
                  )}

                  {appeal.status === 'UnderReview' && (
                    <div className="bg-primary/10 p-3 rounded border border-primary/20">
                      <p className="text-sm text-primary">
                        👀 Your appeal is currently under review by our team. This typically takes 3-5 business days.
                      </p>
                    </div>
                  )}

                  {appeal.status === 'Approved' && (
                    <div className="bg-success/10 p-3 rounded border border-success/20">
                      <p className="text-sm text-success">
                        ✅ Your appeal has been approved. The penalty has been reversed.
                      </p>
                    </div>
                  )}

                  {appeal.status === 'Rejected' && (
                    <div className="bg-destructive/10 p-3 rounded border border-destructive/20">
                      <p className="text-sm text-destructive">
                        ❌ Your appeal was not approved. The original penalty remains in effect.
                      </p>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          ))
        )}
      </div>
    </div>
  );
}