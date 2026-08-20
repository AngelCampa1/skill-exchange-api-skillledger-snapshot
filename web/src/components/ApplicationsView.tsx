'use client'

import { logger } from '@/utils/logger';

import React, { useState } from 'react';
import { 
  Calendar, 
  Clock, 
  DollarSign, 
  Eye, 
  Star, 
  User, 
  FileText, 
  CheckCircle, 
  XCircle, 
  AlertCircle,
  Download,
  MessageSquare
} from 'lucide-react';

interface ProjectApplication {
  id: string;
  project: {
    id: string;
    title: string;
    shortDescription: string;
    creditBudget: number;
  };
  provider: {
    id: string;
    displayName: string;
    email?: string;
    title?: string;
    company?: string;
    avatarUrl?: string;
  };
  coverLetter: string;
  proposedTimeline?: number;
  skillMatchScore?: number;
  status: string;
  createdAt: string;
  updatedAt: string;
  reviewedAt?: string;
  clientFeedback?: string;
  isAvailableImmediately: boolean;
  proposedBudget?: number;
  availabilityDetails?: string;
  attachments: Array<{
    id: string;
    fileName: string;
    contentType: string;
    fileSize: number;
    url: string;
    description?: string;
    isSafe: boolean;
  }>;
  daysSinceSubmitted: number;
  canBeWithdrawn: boolean;
}

interface ApplicationsViewProps {
  applications: ProjectApplication[];
  viewMode: 'provider' | 'client';
  isLoading?: boolean;
  onStatusUpdate?: (applicationId: string, status: string, feedback?: string) => Promise<void>;
  onWithdraw?: (applicationId: string, reason?: string) => Promise<void>;
  onRefresh?: () => void;
  totalCount: number;
  hasNextPage: boolean;
  onLoadMore?: () => void;
}

// BUG-016 FIX: Add fallback styling for unknown statuses
const STATUS_COLORS: Record<string, string> = {
  Pending: 'bg-warning/10 text-warning',
  UnderReview: 'bg-info/10 text-info',
  Accepted: 'bg-success/10 text-success',
  Rejected: 'bg-destructive/10 text-destructive',
  Withdrawn: 'bg-muted text-muted-foreground',
  Expired: 'bg-warning/20 text-warning',
};

// BUG-016 FIX: Helper function with fallback
const getStatusColor = (status: string): string => {
  return STATUS_COLORS[status] || 'bg-muted text-muted-foreground';
};

// BUG-016 FIX: Add type annotation and helper function
const STATUS_ICONS: Record<string, React.ComponentType<{ className?: string }>> = {
  Pending: AlertCircle,
  UnderReview: Eye,
  Accepted: CheckCircle,
  Rejected: XCircle,
  Withdrawn: XCircle,
  Expired: Clock,
};

// BUG-016 FIX: Helper function with fallback
const getStatusIcon = (status: string) => {
  return STATUS_ICONS[status] || AlertCircle;
};

export function ApplicationsView({
  applications,
  viewMode,
  isLoading = false,
  onStatusUpdate,
  onWithdraw,
  onRefresh,
  totalCount,
  hasNextPage,
  onLoadMore
}: ApplicationsViewProps) {
  // BUG-006 FIX: Separate state for each modal to avoid state collision
  const [viewApplication, setViewApplication] = useState<ProjectApplication | null>(null);
  const [statusUpdateApplication, setStatusUpdateApplication] = useState<ProjectApplication | null>(null);
  const [withdrawApplication, setWithdrawApplication] = useState<ProjectApplication | null>(null);
  const [feedback, setFeedback] = useState('');
  const [withdrawReason, setWithdrawReason] = useState('');
  const [newStatus, setNewStatus] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [sortBy, setSortBy] = useState('submittedAt');

  // BUG-033 FIX: Normalize case for filtering
  const filteredApplications = applications.filter(app => {
    const normalizedQuery = searchQuery.toLowerCase().trim();
    const matchesSearch = normalizedQuery === '' ||
      app.project.title.toLowerCase().includes(normalizedQuery) ||
      app.provider.displayName.toLowerCase().includes(normalizedQuery);

    // BUG-033 FIX: Normalize status comparison
    const normalizedFilter = statusFilter.toLowerCase();
    const normalizedStatus = app.status.toLowerCase();
    const matchesStatus = normalizedFilter === 'all' || normalizedStatus === normalizedFilter;

    return matchesSearch && matchesStatus;
  });

  const sortedApplications = [...filteredApplications].sort((a, b) => {
    switch (sortBy) {
      case 'skillMatch':
        return (b.skillMatchScore || 0) - (a.skillMatchScore || 0);
      case 'status':
        return a.status.localeCompare(b.status);
      default:
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
    }
  });

  // BUG-006 FIX: Use separate state for status update modal
  const handleStatusUpdate = async () => {
    if (!statusUpdateApplication || !onStatusUpdate) return;

    try {
      await onStatusUpdate(statusUpdateApplication.id, newStatus, feedback || undefined);
      setStatusUpdateApplication(null);
      setFeedback('');
      setNewStatus('');
      onRefresh?.();
    } catch (error) {
      logger.error('Error updating status:', error);
    }
  };

  // BUG-006 FIX: Use separate state for withdraw modal
  const handleWithdraw = async () => {
    if (!withdrawApplication || !onWithdraw) return;

    try {
      await onWithdraw(withdrawApplication.id, withdrawReason || undefined);
      setWithdrawApplication(null);
      setWithdrawReason('');
      onRefresh?.();
    } catch (error) {
      logger.error('Error withdrawing application:', error);
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  // BUG-024 FIX: Separate formatters for different date display contexts
  const formatDateTime = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const formatDateShort = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric'
    });
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-2xl font-bold">
            {viewMode === 'provider' ? 'My Applications' : 'Project Applications'}
          </h2>
          <p className="text-muted-foreground">
            {totalCount} {totalCount === 1 ? 'application' : 'applications'} total
          </p>
        </div>
        {onRefresh && (
          <button 
            onClick={onRefresh} 
            disabled={isLoading}
            className="px-4 py-2 border border-border rounded-full text-sm font-medium hover:bg-muted disabled:opacity-50"
          >
            {isLoading ? 'Loading...' : 'Refresh'}
          </button>
        )}
      </div>

      {/* Filters - BUG-020 FIX: Responsive grid layout */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="sm:col-span-2 lg:col-span-2">
          <input
            type="text"
            placeholder={`Search ${viewMode === 'provider' ? 'projects' : 'applicants'}...`}
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
        >
          <option value="all">All Status</option>
          <option value="pending">Pending</option>
          <option value="underreview">Under Review</option>
          <option value="accepted">Accepted</option>
          <option value="rejected">Rejected</option>
          {viewMode === 'provider' && <option value="withdrawn">Withdrawn</option>}
        </select>
        <select
          value={sortBy}
          onChange={(e) => setSortBy(e.target.value)}
          className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
        >
          <option value="submittedAt">Date Submitted</option>
          <option value="skillMatch">Skill Match</option>
          <option value="status">Status</option>
        </select>
      </div>

      {/* Applications List */}
      <div className="space-y-4">
        {/* BUG-012 FIX: Add skeleton loading state */}
        {isLoading && applications.length === 0 ? (
          <div className="space-y-4">
            {[1, 2, 3].map((i) => (
              <div key={i} className="bg-card border border-border rounded-lg p-6 animate-pulse">
                <div className="flex justify-between items-start mb-4">
                  <div className="flex-1">
                    <div className="h-6 bg-muted rounded w-1/3 mb-2"></div>
                    <div className="h-4 bg-muted rounded w-1/2"></div>
                  </div>
                  <div className="h-8 bg-muted rounded w-20"></div>
                </div>
                <div className="flex gap-4">
                  <div className="h-4 bg-muted rounded w-24"></div>
                  <div className="h-4 bg-muted rounded w-32"></div>
                </div>
              </div>
            ))}
          </div>
        ) : sortedApplications.length === 0 ? (
          <div className="bg-warning/10 border border-warning/20 rounded-lg p-4">
            <p className="text-warning">
              {searchQuery || statusFilter !== 'all'
                ? 'No applications match your current filters.'
                : viewMode === 'provider'
                  ? "You haven't submitted any applications yet."
                  : "No applications have been submitted for your projects yet."
              }
            </p>
          </div>
        ) : (
          sortedApplications.map((application) => {
            // BUG-016 FIX: Use helper function with fallback
            const StatusIcon = getStatusIcon(application.status);
            
            return (
              <div key={application.id} className="bg-card border border-border rounded-lg p-6 hover:shadow-md transition-shadow">
                <div className="flex justify-between items-start mb-4">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <h3 className="text-lg font-semibold">
                        {viewMode === 'provider' ? application.project.title : application.provider.displayName}
                      </h3>
                      {/* BUG-016 FIX: Use helper function with fallback */}
                      <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(application.status)}`}>
                        <StatusIcon className="w-3 h-3 mr-1" />
                        {application.status}
                      </span>
                    </div>
                    
                    <div className="flex items-center gap-4 text-sm text-muted-foreground">
                      {/* BUG-024 FIX: Use short date format for list view */}
                      <div className="flex items-center gap-1">
                        <Calendar className="w-4 h-4" />
                        Submitted {formatDateShort(application.createdAt)}
                      </div>
                      {application.daysSinceSubmitted > 0 && (
                        <div className="flex items-center gap-1">
                          <Clock className="w-4 h-4" />
                          {application.daysSinceSubmitted} days ago
                        </div>
                      )}
                      <div className="flex items-center gap-1">
                        <DollarSign className="w-4 h-4" />
                        {application.proposedBudget || application.project.creditBudget} credits
                      </div>
                    </div>

                    {viewMode === 'client' && application.skillMatchScore !== undefined && (
                      <div className="flex items-center gap-2 mt-2">
                        <Star className="w-4 h-4 text-warning" />
                        <span className="text-sm font-medium">
                          Skill Match: {Math.round(application.skillMatchScore * 100)}%
                        </span>
                        <div className="w-20 bg-muted rounded-full h-2">
                          <div
                            className="bg-primary h-2 rounded-full"
                            style={{ width: `${application.skillMatchScore * 100}%` }}
                          ></div>
                        </div>
                      </div>
                    )}

                    {viewMode === 'provider' && (
                      <p className="text-sm text-muted-foreground mt-2">
                        {application.project.shortDescription}
                      </p>
                    )}
                  </div>

                  {/* BUG-006 FIX: Use separate state for each modal */}
                  <div className="flex gap-2 ml-4">
                    <button
                      onClick={() => setViewApplication(application)}
                      className="px-3 py-1 border border-border rounded-full text-sm font-medium hover:bg-muted flex items-center gap-1"
                    >
                      <Eye className="w-4 h-4" />
                      View
                    </button>

                    {viewMode === 'client' && onStatusUpdate &&
                      ['Pending', 'UnderReview'].includes(application.status) && (
                      <button
                        onClick={() => setStatusUpdateApplication(application)}
                        className="px-3 py-1 bg-primary text-primary-foreground rounded-full text-sm font-medium hover:bg-primary/90"
                      >
                        Review
                      </button>
                    )}

                    {viewMode === 'provider' && onWithdraw && application.canBeWithdrawn && (
                      <button
                        onClick={() => setWithdrawApplication(application)}
                        className="px-3 py-1 bg-destructive text-destructive-foreground rounded-full text-sm font-medium hover:bg-destructive/90"
                      >
                        Withdraw
                      </button>
                    )}
                  </div>
                </div>

                {/* Additional Info */}
                <div className="flex gap-6 text-sm">
                  {application.proposedTimeline && (
                    <div className="flex items-center gap-1">
                      <Clock className="w-4 h-4 text-muted-foreground" />
                      {application.proposedTimeline} days
                    </div>
                  )}
                  {application.isAvailableImmediately && (
                    <span className="inline-flex items-center px-2 py-1 rounded-full text-xs bg-success/10 text-success">
                      Available immediately
                    </span>
                  )}
                  {application.attachments.length > 0 && (
                    <div className="flex items-center gap-1">
                      <FileText className="w-4 h-4 text-muted-foreground" />
                      {application.attachments.length} attachment{application.attachments.length !== 1 ? 's' : ''}
                    </div>
                  )}
                </div>

                {application.clientFeedback && (
                  <div className="mt-3 p-3 bg-muted rounded-lg">
                    <div className="flex items-center gap-1 mb-1">
                      <MessageSquare className="w-4 h-4 text-muted-foreground" />
                      <span className="text-sm font-medium">Client Feedback:</span>
                    </div>
                    <p className="text-sm text-foreground">{application.clientFeedback}</p>
                  </div>
                )}
              </div>
            );
          })
        )}
      </div>

      {/* Load More */}
      {hasNextPage && onLoadMore && (
        <div className="flex justify-center">
          <button
            onClick={onLoadMore}
            className="px-4 py-2 border border-border rounded-full text-sm font-medium hover:bg-muted"
          >
            Load More Applications
          </button>
        </div>
      )}

      {/* Application Detail Modal - BUG-006 FIX: Use separate state */}
      {viewApplication && (
        <div className="fixed inset-0 bg-overlay/80 flex items-center justify-center z-50 p-4">
          <div className="bg-card rounded-lg max-w-4xl w-full max-h-[90vh] overflow-y-auto">
            <div className="sticky top-0 bg-card border-b border-border px-6 py-4">
              <div className="flex justify-between items-center">
                <div>
                  <h3 className="text-lg font-semibold flex items-center gap-3">
                    Application Details
                    {/* BUG-016 FIX: Use helper function with fallback */}
                    <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(viewApplication.status)}`}>
                      {viewApplication.status}
                    </span>
                  </h3>
                  <p className="text-muted-foreground">
                    {viewMode === 'provider' ? viewApplication.project.title : viewApplication.provider.displayName}
                  </p>
                </div>
                <button
                  onClick={() => setViewApplication(null)}
                  className="text-muted-foreground hover:text-foreground"
                >
                  <XCircle className="w-6 h-6" />
                </button>
              </div>
            </div>

            <div className="p-6 space-y-6">
              {/* Cover Letter */}
              <div>
                <h4 className="font-medium mb-2">Cover Letter</h4>
                <div className="bg-muted rounded-lg p-4">
                  <p className="whitespace-pre-wrap text-sm">
                    {viewApplication.coverLetter}
                  </p>
                </div>
              </div>

              {/* Application Info */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <h4 className="font-medium mb-2">Timeline & Budget</h4>
                  <div className="space-y-2 text-sm">
                    {viewApplication.proposedTimeline && (
                      <div>Proposed Timeline: {viewApplication.proposedTimeline} days</div>
                    )}
                    {viewApplication.proposedBudget && (
                      <div>Proposed Budget: {viewApplication.proposedBudget} credits</div>
                    )}
                    {viewApplication.isAvailableImmediately && (
                      <div className="text-success">Available immediately</div>
                    )}
                  </div>
                </div>
                <div>
                  <h4 className="font-medium mb-2">Submission Details</h4>
                  <div className="space-y-2 text-sm">
                    {/* BUG-024 FIX: Use full datetime format for detail view */}
                    <div>Submitted: {formatDateTime(viewApplication.createdAt)}</div>
                    {viewApplication.reviewedAt && (
                      <div>Reviewed: {formatDateTime(viewApplication.reviewedAt)}</div>
                    )}
                    {viewApplication.skillMatchScore !== undefined && (
                      <div>Skill Match: {Math.round(viewApplication.skillMatchScore * 100)}%</div>
                    )}
                  </div>
                </div>
              </div>

              {/* Availability Details */}
              {viewApplication.availabilityDetails && (
                <div>
                  <h4 className="font-medium mb-2">Availability Details</h4>
                  <div className="bg-muted rounded-lg p-4">
                    <p className="whitespace-pre-wrap text-sm">
                      {viewApplication.availabilityDetails}
                    </p>
                  </div>
                </div>
              )}

              {/* Attachments */}
              {viewApplication.attachments.length > 0 && (
                <div>
                  <h4 className="font-medium mb-2">Portfolio Attachments</h4>
                  <div className="space-y-2">
                    {viewApplication.attachments.map((attachment) => (
                      <div key={attachment.id} className="flex items-center justify-between p-3 bg-muted rounded-lg">
                        <div className="flex items-center gap-3">
                          <FileText className="w-5 h-5 text-muted-foreground" />
                          <div>
                            <p className="font-medium text-sm">{attachment.fileName}</p>
                            <p className="text-xs text-muted-foreground">
                              {formatFileSize(attachment.fileSize)}
                              {attachment.description && ` • ${attachment.description}`}
                            </p>
                          </div>
                        </div>
                        <a
                          href={attachment.url}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="flex items-center gap-1 px-3 py-1 text-sm text-primary hover:text-primary/80"
                        >
                          <Download className="w-4 h-4" />
                          Download
                        </a>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Client Feedback */}
              {viewApplication.clientFeedback && (
                <div>
                  <h4 className="font-medium mb-2">Client Feedback</h4>
                  <div className="bg-muted rounded-lg p-4">
                    <p className="whitespace-pre-wrap text-sm">
                      {viewApplication.clientFeedback}
                    </p>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Status Update Modal - BUG-006 FIX: Use separate state */}
      {statusUpdateApplication && (
        <div className="fixed inset-0 bg-overlay/80 flex items-center justify-center z-50 p-4">
          <div className="bg-card rounded-lg max-w-md w-full">
            <div className="p-6">
              <h3 className="text-lg font-semibold mb-4">Update Application Status</h3>
              <p className="text-muted-foreground mb-4">
                Change the status of this application and provide feedback to the applicant.
              </p>
              <div className="space-y-4">
                <div>
                  <label htmlFor="status" className="block text-sm font-medium text-foreground mb-2">New Status</label>
                  <select
                    id="status"
                    value={newStatus}
                    onChange={(e) => setNewStatus(e.target.value)}
                    className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                  >
                    <option value="">Select status</option>
                    <option value="UnderReview">Under Review</option>
                    <option value="Accepted">Accept</option>
                    <option value="Rejected">Reject</option>
                  </select>
                </div>
                <div>
                  <label htmlFor="feedback" className="block text-sm font-medium text-foreground mb-2">Feedback (optional)</label>
                  <textarea
                    id="feedback"
                    placeholder="Provide feedback to the applicant..."
                    value={feedback}
                    onChange={(e) => setFeedback(e.target.value)}
                    rows={4}
                    className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                  />
                </div>
                <div className="flex justify-end gap-2">
                  <button
                    onClick={() => setStatusUpdateApplication(null)}
                    className="px-4 py-2 border border-border rounded-full text-sm font-medium hover:bg-muted"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handleStatusUpdate}
                    disabled={!newStatus}
                    className="px-4 py-2 bg-primary text-primary-foreground rounded-full text-sm font-medium hover:bg-primary/90 disabled:opacity-50"
                  >
                    Update Status
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Withdraw Modal - BUG-006 FIX: Use separate state */}
      {withdrawApplication && (
        <div className="fixed inset-0 bg-overlay/80 flex items-center justify-center z-50 p-4">
          <div className="bg-card rounded-lg max-w-md w-full">
            <div className="p-6">
              <h3 className="text-lg font-semibold mb-4">Withdraw Application</h3>
              <p className="text-muted-foreground mb-4">
                Are you sure you want to withdraw this application? This action cannot be undone.
              </p>
              <div className="space-y-4">
                <div>
                  <label htmlFor="reason" className="block text-sm font-medium text-foreground mb-2">Reason (optional)</label>
                  <textarea
                    id="reason"
                    placeholder="Provide a reason for withdrawing..."
                    value={withdrawReason}
                    onChange={(e) => setWithdrawReason(e.target.value)}
                    rows={3}
                    className="w-full px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                  />
                </div>
                <div className="flex justify-end gap-2">
                  <button
                    onClick={() => setWithdrawApplication(null)}
                    className="px-4 py-2 border border-border rounded-full text-sm font-medium hover:bg-muted"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handleWithdraw}
                    className="px-4 py-2 bg-destructive text-destructive-foreground rounded-full text-sm font-medium hover:bg-destructive/90"
                  >
                    Withdraw Application
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}