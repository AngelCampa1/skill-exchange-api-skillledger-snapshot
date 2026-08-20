'use client';

import { logger } from '@/utils/logger';
import React, { useState, useEffect, useCallback } from 'react';
import {
  MessageCircle,
  FileText,
  Calendar,
  DollarSign,
  Users,
  Clock,
  Settings,
  Archive,
  MoreHorizontal,
  Paperclip,
  Download
} from 'lucide-react';
import FileManager from './FileManager';
import { FileAttachButton, useFileShareIntegration } from './FileShareIntegration';
import SimpleMessaging from './SimpleMessaging';
import { AUTH_CONFIG } from '../../constants/auth';

interface EnhancedWorkspaceDashboardProps {
  workspaceId: string;
  currentUserId: string;
  isClient: boolean;
}

interface WorkspaceDashboardData {
  workspaceId: string;
  projectTitle: string;
  projectDescription: string;
  clientName: string;
  providerName: string;
  status: string;
  createdAt: string;
  archivedAt?: string;
  timelineData?: string;
  milestoneData?: string;
  integrationStatus?: string;
  lastSyncedAt?: string;
}

type ActiveTab = 'overview' | 'messages' | 'files' | 'timeline' | 'settings';

export default function EnhancedWorkspaceDashboard({
  workspaceId,
  currentUserId,
  isClient
}: EnhancedWorkspaceDashboardProps) {
  const [workspaceData, setWorkspaceData] = useState<WorkspaceDashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<ActiveTab>('overview');
  const [lastSyncedAt, setLastSyncedAt] = useState<Date>(new Date());

  const { downloadFile, previewFile, shareFileInMessage } = useFileShareIntegration(workspaceId);

  const fetchWorkspaceData = useCallback(async () => {
    try {
      // BUG-FE-002 FIX: Use httpOnly cookies for authentication
      const response = await fetch(`/api/workspace/${workspaceId}`, {
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          'Content-Type': 'application/json'
        }
      });

      if (response.ok) {
        const data = await response.json();
        setWorkspaceData(data);
        setLastSyncedAt(new Date());
      } else if (response.status === 404) {
        setError('Workspace not found');
      } else if (response.status === 403) {
        setError('Access denied to this workspace');
      } else {
        setError('Failed to load workspace data');
      }
    } catch (error) {
      setError('An error occurred while loading workspace data');
    } finally {
      setLoading(false);
    }
  }, [workspaceId]);

  useEffect(() => {
    fetchWorkspaceData();
    const interval = setInterval(fetchWorkspaceData, 30000); // Refresh every 30 seconds
    return () => clearInterval(interval);
  }, [fetchWorkspaceData]);

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'active':
        return 'bg-success/10 text-success';
      case 'archived':
        return 'bg-muted text-muted-foreground';
      case 'completed':
        return 'bg-primary/10 text-primary';
      default:
        return 'bg-warning/10 text-warning';
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  /**
   * SECURITY FIX: Safe JSON.parse wrapper to prevent application crashes
   * from malformed JSON data returned by API
   */
  const safeJsonParse = (jsonString: string, fieldName: string): unknown => {
    try {
      return JSON.parse(jsonString);
    } catch (error) {
      logger.error(`Failed to parse ${fieldName} JSON`, { error, jsonString });
      return null;
    }
  };

  const handleTabChange = (tab: ActiveTab) => {
    setActiveTab(tab);
  };

  const handleArchiveWorkspace = async () => {
    if (!confirm('Are you sure you want to archive this workspace?')) return;

    try {
      // BUG-FE-002 FIX: Removed localStorage token
      const response = await fetch(`/api/workspace/${workspaceId}/archive`, {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          'Content-Type': 'application/json'
        }
      });

      if (response.ok) {
        await fetchWorkspaceData();
      } else {
        setError('Failed to archive workspace');
      }
    } catch (error) {
      setError('An error occurred while archiving workspace');
    }
  };

  const TabButton = ({ tab, label, icon: Icon, count }: {
    tab: ActiveTab;
    label: string;
    icon: React.ComponentType<{ className?: string }>;
    count?: number;
  }) => (
    <button
      onClick={() => handleTabChange(tab)}
      className={`flex items-center px-4 py-2 text-sm font-medium rounded-full transition-colors ${
        activeTab === tab
          ? 'bg-primary/10 text-primary border border-primary/20'
          : 'text-muted-foreground hover:text-foreground hover:bg-muted'
      }`}
    >
      <Icon className="h-4 w-4 mr-2" />
      {label}
      {count !== undefined && (
        <span className={`ml-2 px-2 py-0.5 rounded-full text-xs ${
          activeTab === tab ? 'bg-primary/20 text-primary' : 'bg-muted text-muted-foreground'
        }`}>
          {count}
        </span>
      )}
    </button>
  );

  if (loading) {
    return (
      <div className="flex justify-center items-center min-h-96">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-destructive/10 border border-destructive/20 rounded-lg p-6 max-w-2xl mx-auto">
        <h3 className="text-lg font-semibold text-destructive mb-2">Error Loading Workspace</h3>
        <p className="text-destructive mb-4">{error}</p>
        <button
          onClick={fetchWorkspaceData}
          className="bg-destructive/10 text-destructive px-4 py-2 rounded-full hover:bg-destructive/20 transition-colors"
        >
          Retry
        </button>
      </div>
    );
  }

  if (!workspaceData) {
    return (
      <div className="bg-muted border border-border rounded-lg p-6 max-w-2xl mx-auto">
        <h3 className="text-lg font-semibold text-foreground mb-2">Workspace Not Found</h3>
        <p className="text-muted-foreground">The requested workspace could not be found or you don't have access to it.</p>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto p-6 space-y-6">
      {/* Workspace Header */}
      <div className="bg-card rounded-lg shadow-sm border border-border p-6">
        <div className="flex flex-col md:flex-row md:items-center justify-between mb-6">
          <div>
            <h1 className="text-2xl font-bold text-foreground mb-2">{workspaceData.projectTitle}</h1>
            <p className="text-muted-foreground mb-3">{workspaceData.projectDescription}</p>
            <div className="flex items-center space-x-4">
              <span className={`px-3 py-1 rounded-full text-sm font-medium ${getStatusColor(workspaceData.status)}`}>
                {workspaceData.status}
              </span>
              <span className="text-sm text-muted-foreground">
                Created {formatDate(workspaceData.createdAt)}
              </span>
            </div>
          </div>
          <div className="flex items-center space-x-3 mt-4 md:mt-0">
            <FileAttachButton
              workspaceId={workspaceId}
              onFileAttach={() => {
                if (activeTab !== 'files') {
                  handleTabChange('files');
                }
              }}
            />
            {isClient && workspaceData.status === 'Active' && (
              <button
                onClick={handleArchiveWorkspace}
                className="flex items-center px-3 py-2 text-sm bg-muted text-muted-foreground rounded-full hover:bg-muted/80"
              >
                <Archive className="h-4 w-4 mr-2" />
                Archive
              </button>
            )}
            <button className="p-2 text-muted-foreground hover:text-foreground hover:bg-muted rounded-full">
              <MoreHorizontal className="h-5 w-5" />
            </button>
          </div>
        </div>

        {/* Participants */}
        <div className="bg-muted rounded-lg p-4">
          <h3 className="text-sm font-semibold text-foreground mb-2">Project Team</h3>
          <div className="flex items-center space-x-6">
            <div className="flex items-center">
              <div className="h-8 w-8 bg-primary rounded-full flex items-center justify-center text-primary-foreground text-sm font-medium">
                {workspaceData.clientName.charAt(0).toUpperCase()}
              </div>
              <div className="ml-3">
                <p className="text-sm font-medium text-foreground">{workspaceData.clientName}</p>
                <p className="text-xs text-muted-foreground">Project Client</p>
              </div>
            </div>
            <div className="flex items-center">
              <div className="h-8 w-8 bg-success rounded-full flex items-center justify-center text-success-foreground text-sm font-medium">
                {workspaceData.providerName.charAt(0).toUpperCase()}
              </div>
              <div className="ml-3">
                <p className="text-sm font-medium text-foreground">{workspaceData.providerName}</p>
                <p className="text-xs text-muted-foreground">Service Provider</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Navigation Tabs */}
      <div className="bg-card rounded-lg shadow-sm border border-border p-4">
        <div className="flex flex-wrap gap-2">
          <TabButton tab="overview" label="Overview" icon={Calendar} />
          <TabButton tab="messages" label="Messages" icon={MessageCircle} />
          <TabButton tab="files" label="Files" icon={FileText} />
          <TabButton tab="timeline" label="Timeline" icon={Clock} />
          {isClient && (
            <TabButton tab="settings" label="Settings" icon={Settings} />
          )}
        </div>
      </div>

      {/* Tab Content */}
      <div className="bg-card rounded-lg shadow-sm border border-border min-h-96">
        {activeTab === 'overview' && (
          <div className="p-6">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {/* Project Status Card */}
              <div className="bg-gradient-to-r from-primary/5 to-primary/10 rounded-lg p-4">
                <div className="flex items-center">
                  <div className="p-2 bg-primary rounded-lg">
                    <Calendar className="h-6 w-6 text-primary-foreground" />
                  </div>
                  <div className="ml-4">
                    <h3 className="text-lg font-semibold text-primary">Project Status</h3>
                    <p className="text-primary/80">{workspaceData.status}</p>
                  </div>
                </div>
              </div>

              {/* Integration Status Card */}
              {workspaceData.integrationStatus && (
                <div className="bg-gradient-to-r from-success/5 to-success/10 rounded-lg p-4">
                  <div className="flex items-center">
                    <div className="p-2 bg-success rounded-lg">
                      <Settings className="h-6 w-6 text-success-foreground" />
                    </div>
                    <div className="ml-4">
                      <h3 className="text-lg font-semibold text-success">Integration</h3>
                      <p className="text-success/80">{workspaceData.integrationStatus}</p>
                    </div>
                  </div>
                </div>
              )}

              {/* Last Sync Card */}
              <div className="bg-gradient-to-r from-accent/5 to-accent/10 rounded-lg p-4">
                <div className="flex items-center">
                  <div className="p-2 bg-accent rounded-lg">
                    <Clock className="h-6 w-6 text-accent-foreground" />
                  </div>
                  <div className="ml-4">
                    <h3 className="text-lg font-semibold text-accent-foreground">Last Updated</h3>
                    <p className="text-accent-foreground/80 text-sm">
                      {lastSyncedAt.toLocaleTimeString()}
                    </p>
                  </div>
                </div>
              </div>
            </div>

            {/* Timeline Data */}
            {workspaceData.timelineData && (
              <div className="mt-6">
                <h3 className="text-lg font-semibold text-foreground mb-4">Timeline Information</h3>
                <div className="bg-muted rounded-lg p-4">
                  <pre className="text-sm text-foreground whitespace-pre-wrap">
                    {JSON.stringify(safeJsonParse(workspaceData.timelineData, 'timelineData') || {}, null, 2)}
                  </pre>
                </div>
              </div>
            )}

            {/* Milestone Data */}
            {workspaceData.milestoneData && (
              <div className="mt-6">
                <h3 className="text-lg font-semibold text-foreground mb-4">Milestone Information</h3>
                <div className="bg-muted rounded-lg p-4">
                  <pre className="text-sm text-foreground whitespace-pre-wrap">
                    {JSON.stringify(safeJsonParse(workspaceData.milestoneData, 'milestoneData') || {}, null, 2)}
                  </pre>
                </div>
              </div>
            )}
          </div>
        )}

        {activeTab === 'messages' && (
          <div className="h-[600px]">
            <SimpleMessaging 
              workspaceId={workspaceId} 
              currentUserId={currentUserId}
            />
          </div>
        )}

        {activeTab === 'files' && (
          <FileManager workspaceId={workspaceId} isClient={isClient} />
        )}

        {activeTab === 'timeline' && (
          <div className="p-6">
            <div className="bg-primary/10 border border-primary/20 rounded-lg p-4">
              <h3 className="text-lg font-semibold text-primary mb-2">Project Timeline</h3>
              <p className="text-primary/80">
                Timeline and milestone tracking features will be displayed here.
              </p>
            </div>
          </div>
        )}

        {activeTab === 'settings' && isClient && (
          <div className="p-6">
            <div className="space-y-6">
              <div>
                <h3 className="text-lg font-semibold text-foreground mb-4">Workspace Settings</h3>
                <div className="space-y-4">
                  <div className="flex items-center justify-between p-4 bg-muted rounded-lg">
                    <div>
                      <h4 className="font-medium text-foreground">Archive Workspace</h4>
                      <p className="text-sm text-muted-foreground">
                        Archive this workspace when the project is complete
                      </p>
                    </div>
                    <button
                      onClick={handleArchiveWorkspace}
                      className="px-4 py-2 bg-destructive text-destructive-foreground text-sm font-medium rounded-full hover:bg-destructive/90"
                    >
                      Archive
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Status Footer */}
      <div className="bg-muted rounded-lg p-4">
        <div className="flex items-center justify-between text-sm text-muted-foreground">
          <span>
            Last synced: {lastSyncedAt.toLocaleString()}
          </span>
          <span>
            Workspace ID: {workspaceId}
          </span>
        </div>
      </div>
    </div>
  );
}