'use client';

import React, { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import { useAuth } from '@/contexts/AuthContext';
import EnhancedWorkspaceDashboard from '@/components/workspace/EnhancedWorkspaceDashboard';

export default function WorkspacePage() {
  const params = useParams();
  const { user, isAuthenticated, isLoading: authLoading } = useAuth();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const workspaceId = params?.id as string;

  useEffect(() => {
    // Wait for authentication to be resolved
    if (!authLoading) {
      if (!isAuthenticated) {
        setError('You must be logged in to access workspaces');
      }
      setLoading(false);
    }
  }, [authLoading, isAuthenticated]);

  if (authLoading || loading) {
    return (
      <div className="min-h-screen bg-muted flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
      </div>
    );
  }

  if (error || !isAuthenticated || !user) {
    return (
      <div className="min-h-screen bg-muted flex items-center justify-center">
        <div className="bg-card rounded-lg shadow-sm border border-border p-8 max-w-md w-full mx-4">
          <div className="text-center">
            <h2 className="text-2xl font-bold text-foreground mb-4">Access Denied</h2>
            <p className="text-muted-foreground mb-6">
              {error || 'You need to be logged in to access workspace features.'}
            </p>
            <a
              href="/login"
              className="inline-flex items-center px-4 py-2 bg-primary text-primary-foreground font-medium rounded-lg hover:bg-primary/90"
            >
              Go to Login
            </a>
          </div>
        </div>
      </div>
    );
  }

  if (!workspaceId) {
    return (
      <div className="min-h-screen bg-muted flex items-center justify-center">
        <div className="bg-card rounded-lg shadow-sm border border-border p-8 max-w-md w-full mx-4">
          <div className="text-center">
            <h2 className="text-2xl font-bold text-foreground mb-4">Invalid Workspace</h2>
            <p className="text-muted-foreground mb-6">
              The workspace ID is missing or invalid.
            </p>
            <a
              href="/dashboard"
              className="inline-flex items-center px-4 py-2 bg-primary text-primary-foreground font-medium rounded-lg hover:bg-primary/90"
            >
              Go to Dashboard
            </a>
          </div>
        </div>
      </div>
    );
  }

  // Determine if the current user is the client
  // This is a simplified check - in a real app you'd verify this against the workspace data
  const isClient = user.roles?.includes('Client') || false;

  return (
    <div className="min-h-screen bg-muted">
      <EnhancedWorkspaceDashboard
        workspaceId={workspaceId}
        currentUserId={user.id}
        isClient={isClient}
      />
    </div>
  );
}