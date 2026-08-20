import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Separator } from '@/components/ui/separator';
import {
  CheckCircle2,
  XCircle,
  FileText,
  Download,
  ExternalLink,
  Calendar,
  User,
  MessageSquare,
  AlertCircle,
  Loader2,
  Star,
  Clock,
  Eye
} from 'lucide-react';
import { 
  DeliverableSubmission, 
  ProjectMilestone, 
  DeliverableType, 
  ReviewSubmissionRequest 
} from '@/types/milestone';
import { format, formatDistanceToNow } from 'date-fns';
import { AUTH_CONFIG } from '../../constants/auth';

interface MilestoneApprovalWorkflowProps {
  milestone: ProjectMilestone;
  userRole: 'client' | 'provider';
  onApprovalComplete: () => void;
  onClose: () => void;
}

export const MilestoneApprovalWorkflow: React.FC<MilestoneApprovalWorkflowProps> = ({
  milestone,
  userRole,
  onApprovalComplete,
  onClose
}) => {
  const [submissions, setSubmissions] = useState<DeliverableSubmission[]>([]);
  const [selectedSubmission, setSelectedSubmission] = useState<DeliverableSubmission | null>(null);
  const [reviewData, setReviewData] = useState<ReviewSubmissionRequest>({
    isApproved: false,
    reviewFeedback: ''
  });
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showReviewDialog, setShowReviewDialog] = useState(false);

  useEffect(() => {
    loadSubmissions();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [milestone.id]);

  const loadSubmissions = async () => {
    try {
      setLoading(true);
      const response = await fetch(`/api/milestone/${milestone.id}/submissions`, {
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          // BUG-FE-002 FIX: Removed - Auth via httpOnly cookies,
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const data: DeliverableSubmission[] = await response.json();
        setSubmissions(data);
        
        // Auto-select the most recent unreviewed submission
        const pendingSubmission = data
          .filter(s => !s.isReviewed)
          .sort((a, b) => new Date(b.submittedAt).getTime() - new Date(a.submittedAt).getTime())[0];
        
        if (pendingSubmission) {
          setSelectedSubmission(pendingSubmission);
        }
      } else {
        setError('Failed to load submissions');
      }
    } catch (err) {
      setError('Network error loading submissions');
    } finally {
      setLoading(false);
    }
  };

  const handleReviewSubmission = async (submissionId: string, reviewRequest: ReviewSubmissionRequest) => {
    try {
      setSubmitting(true);
      const response = await fetch(`/api/milestone/submissions/${submissionId}/review`, {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          // BUG-FE-002 FIX: Removed - Auth via httpOnly cookies,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(reviewRequest)
      });

      if (response.ok) {
        await loadSubmissions();
        onApprovalComplete();
        setShowReviewDialog(false);
        
        // Reset review form
        setReviewData({
          isApproved: false,
          reviewFeedback: ''
        });
      } else {
        const errorData = await response.json();
        setError(errorData.message || 'Failed to review submission');
      }
    } catch (err) {
      setError('Network error during review');
    } finally {
      setSubmitting(false);
    }
  };

  const getSubmissionTypeIcon = (type: DeliverableType) => {
    switch (type) {
      case DeliverableType.FileUpload:
        return <FileText className="h-4 w-4" />;
      case DeliverableType.Text:
        return <MessageSquare className="h-4 w-4" />;
      case DeliverableType.Link:
        return <ExternalLink className="h-4 w-4" />;
      case DeliverableType.CodeRepository:
        return <FileText className="h-4 w-4" />;
      default:
        return <FileText className="h-4 w-4" />;
    }
  };

  const getSubmissionStatusBadge = (submission: DeliverableSubmission) => {
    if (!submission.isReviewed) {
      return <Badge className="bg-warning/10 text-warning">Pending Review</Badge>;
    }

    return submission.isApproved ? (
      <Badge className="bg-success/10 text-success">
        <CheckCircle2 className="h-3 w-3 mr-1" />
        Approved
      </Badge>
    ) : (
      <Badge className="bg-destructive/10 text-destructive">
        <XCircle className="h-3 w-3 mr-1" />
        Rejected
      </Badge>
    );
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center p-6">
        <div className="flex items-center space-x-2">
          <Loader2 className="h-4 w-4 animate-spin" />
          <span>Loading submissions...</span>
        </div>
      </div>
    );
  }

  const pendingSubmissions = submissions.filter(s => !s.isReviewed);
  const reviewedSubmissions = submissions.filter(s => s.isReviewed);

  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 max-w-7xl">
      {/* Submissions List */}
      <div className="space-y-4">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center justify-between">
              <span>Milestone Submissions</span>
              <Badge variant="outline">
                {submissions.length} submission{submissions.length !== 1 ? 's' : ''}
              </Badge>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <ScrollArea className="h-96">
              <div className="space-y-3">
                {/* Pending Reviews */}
                {pendingSubmissions.length > 0 && (
                  <>
                    <div className="flex items-center space-x-2 text-sm font-medium text-warning">
                      <Clock className="h-4 w-4" />
                      <span>Pending Review ({pendingSubmissions.length})</span>
                    </div>
                    {pendingSubmissions.map((submission) => (
                      <SubmissionCard
                        key={submission.id}
                        submission={submission}
                        isSelected={selectedSubmission?.id === submission.id}
                        onClick={() => setSelectedSubmission(submission)}
                        getSubmissionTypeIcon={getSubmissionTypeIcon}
                        getSubmissionStatusBadge={getSubmissionStatusBadge}
                      />
                    ))}
                  </>
                )}

                {/* Reviewed Submissions */}
                {reviewedSubmissions.length > 0 && (
                  <>
                    {pendingSubmissions.length > 0 && <Separator className="my-4" />}
                    <div className="flex items-center space-x-2 text-sm font-medium text-muted-foreground">
                      <CheckCircle2 className="h-4 w-4" />
                      <span>Reviewed ({reviewedSubmissions.length})</span>
                    </div>
                    {reviewedSubmissions.map((submission) => (
                      <SubmissionCard
                        key={submission.id}
                        submission={submission}
                        isSelected={selectedSubmission?.id === submission.id}
                        onClick={() => setSelectedSubmission(submission)}
                        getSubmissionTypeIcon={getSubmissionTypeIcon}
                        getSubmissionStatusBadge={getSubmissionStatusBadge}
                      />
                    ))}
                  </>
                )}

                {submissions.length === 0 && (
                  <div className="text-center py-8 text-muted-foreground">
                    <FileText className="h-12 w-12 text-muted-foreground/30 mx-auto mb-4" />
                    <p>No submissions yet</p>
                  </div>
                )}
              </div>
            </ScrollArea>
          </CardContent>
        </Card>
      </div>

      {/* Submission Details */}
      <div className="space-y-4">
        {selectedSubmission ? (
          <SubmissionDetails
            submission={selectedSubmission}
            userRole={userRole}
            onReview={(reviewRequest) => {
              setReviewData(reviewRequest);
              setShowReviewDialog(true);
            }}
            formatFileSize={formatFileSize}
          />
        ) : (
          <Card>
            <CardContent className="text-center py-8">
              <Eye className="h-12 w-12 text-muted-foreground/30 mx-auto mb-4" />
              <p className="text-muted-foreground">Select a submission to view details</p>
            </CardContent>
          </Card>
        )}
      </div>

      {/* Review Confirmation Dialog */}
      <Dialog open={showReviewDialog} onOpenChange={setShowReviewDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {reviewData.isApproved ? 'Approve Submission' : 'Reject Submission'}
            </DialogTitle>
          </DialogHeader>

          <div className="space-y-4">
            <div className="p-4 border rounded-lg bg-muted">
              <h4 className="font-medium mb-2">{selectedSubmission?.title}</h4>
              <p className="text-sm text-muted-foreground">{selectedSubmission?.description}</p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="reviewFeedback">
                {reviewData.isApproved ? 'Approval Notes (Optional)' : 'Rejection Reason (Required)'}
              </Label>
              <Textarea
                id="reviewFeedback"
                value={reviewData.reviewFeedback}
                onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setReviewData(prev => ({ ...prev, reviewFeedback: e.target.value }))}
                placeholder={reviewData.isApproved
                  ? 'Great work! The deliverable meets all requirements.'
                  : 'Please explain what needs to be changed...'
                }
                rows={4}
              />
              {!reviewData.isApproved && !reviewData.reviewFeedback.trim() && (
                <p className="text-sm text-destructive">Feedback is required for rejections</p>
              )}
            </div>

            {error && (
              <div className="flex items-center space-x-1 text-sm text-destructive">
                <AlertCircle className="h-4 w-4" />
                <span>{error}</span>
              </div>
            )}

            <div className="flex justify-end space-x-2">
              <Button variant="outline" onClick={() => setShowReviewDialog(false)}>
                Cancel
              </Button>
              <Button
                onClick={() => selectedSubmission && handleReviewSubmission(selectedSubmission.id, reviewData)}
                disabled={submitting || (!reviewData.isApproved && !reviewData.reviewFeedback.trim())}
                className={reviewData.isApproved ? '' : 'bg-destructive hover:bg-destructive/90'}
              >
                {submitting ? (
                  <div className="flex items-center space-x-2">
                    <Loader2 className="h-4 w-4 animate-spin" />
                    <span>Processing...</span>
                  </div>
                ) : (
                  <span>{reviewData.isApproved ? 'Approve' : 'Reject'}</span>
                )}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
};

// Individual Submission Card
interface SubmissionCardProps {
  submission: DeliverableSubmission;
  isSelected: boolean;
  onClick: () => void;
  getSubmissionTypeIcon: (type: DeliverableType) => React.ReactNode;
  getSubmissionStatusBadge: (submission: DeliverableSubmission) => React.ReactNode;
}

const SubmissionCard: React.FC<SubmissionCardProps> = ({
  submission,
  isSelected,
  onClick,
  getSubmissionTypeIcon,
  getSubmissionStatusBadge
}) => {
  return (
    <div
      onClick={onClick}
      className={`p-3 border rounded-lg cursor-pointer transition-all hover:shadow-sm ${
        isSelected
          ? 'border-primary bg-primary/10'
          : 'border-border hover:border-input'
      }`}
    >
      <div className="flex items-start justify-between mb-2">
        <div className="flex items-center space-x-2">
          {getSubmissionTypeIcon(submission.type)}
          <h4 className="font-medium text-sm truncate">{submission.title}</h4>
        </div>
        {getSubmissionStatusBadge(submission)}
      </div>

      <div className="space-y-1 text-xs text-muted-foreground">
        <div className="flex items-center space-x-1">
          <User className="h-3 w-3" />
          <span>{submission.submittedByUserName}</span>
        </div>
        <div className="flex items-center space-x-1">
          <Calendar className="h-3 w-3" />
          <span>{formatDistanceToNow(new Date(submission.submittedAt), { addSuffix: true })}</span>
        </div>
        {submission.attachmentCount > 0 && (
          <div className="flex items-center space-x-1">
            <FileText className="h-3 w-3" />
            <span>{submission.attachmentCount} file{submission.attachmentCount !== 1 ? 's' : ''}</span>
          </div>
        )}
      </div>
    </div>
  );
};

// Submission Details Panel
interface SubmissionDetailsProps {
  submission: DeliverableSubmission;
  userRole: 'client' | 'provider';
  onReview: (reviewRequest: ReviewSubmissionRequest) => void;
  formatFileSize: (bytes: number) => string;
}

const SubmissionDetails: React.FC<SubmissionDetailsProps> = ({
  submission,
  userRole,
  onReview,
  formatFileSize
}) => {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center justify-between">
          <span>{submission.title}</span>
          <Badge className={submission.isReviewed ? (submission.isApproved ? 'bg-success/10 text-success' : 'bg-destructive/10 text-destructive') : 'bg-warning/10 text-warning'}>
            {submission.isReviewed ? (submission.isApproved ? 'Approved' : 'Rejected') : 'Pending Review'}
          </Badge>
        </CardTitle>
      </CardHeader>

      <CardContent className="space-y-6">
        {/* Submission Info */}
        <div className="grid grid-cols-2 gap-4 text-sm">
          <div>
            <Label className="text-muted-foreground">Submitted By</Label>
            <p className="font-medium">{submission.submittedByUserName}</p>
          </div>
          <div>
            <Label className="text-muted-foreground">Submitted</Label>
            <p className="font-medium">{format(new Date(submission.submittedAt), 'MMM dd, yyyy HH:mm')}</p>
          </div>
          <div>
            <Label className="text-muted-foreground">Type</Label>
            <p className="font-medium">{submission.type}</p>
          </div>
          {submission.isReviewed && (
            <div>
              <Label className="text-muted-foreground">Reviewed</Label>
              <p className="font-medium">{format(new Date(submission.reviewedAt!), 'MMM dd, yyyy HH:mm')}</p>
            </div>
          )}
        </div>

        {/* Description */}
        {submission.description && (
          <div>
            <Label className="text-muted-foreground block mb-2">Description</Label>
            <div className="p-3 bg-muted rounded-lg">
              <p className="text-sm">{submission.description}</p>
            </div>
          </div>
        )}

        {/* URL Content */}
        {submission.submissionUrl && (
          <div>
            <Label className="text-muted-foreground block mb-2">
              {submission.type === DeliverableType.CodeRepository ? 'Repository URL' : 'URL'}
            </Label>
            <div className="p-3 bg-muted rounded-lg">
              <a
                href={submission.submissionUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center space-x-2 text-primary hover:text-primary/80"
              >
                <ExternalLink className="h-4 w-4" />
                <span className="break-all">{submission.submissionUrl}</span>
              </a>
            </div>
          </div>
        )}

        {/* Text Content */}
        {submission.textContent && (
          <div>
            <Label className="text-muted-foreground block mb-2">Text Content</Label>
            <div className="p-3 bg-muted rounded-lg">
              <pre className="text-sm whitespace-pre-wrap">{submission.textContent}</pre>
            </div>
          </div>
        )}

        {/* Attached Files */}
        {submission.attachedFiles.length > 0 && (
          <div>
            <Label className="text-muted-foreground block mb-2">
              Attached Files ({submission.attachedFiles.length})
            </Label>
            <div className="space-y-2">
              {submission.attachedFiles.map((file) => (
                <div key={file.id} className="flex items-center justify-between p-2 bg-muted rounded">
                  <div className="flex items-center space-x-2">
                    <FileText className="h-4 w-4 text-muted-foreground" />
                    <div>
                      <p className="text-sm font-medium">{file.fileName}</p>
                      <p className="text-xs text-muted-foreground">{formatFileSize(file.fileSize)}</p>
                    </div>
                  </div>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => window.open(file.fileUrl, '_blank')}
                    className="flex items-center space-x-1"
                  >
                    <Download className="h-3 w-3" />
                    <span>Download</span>
                  </Button>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Submission Notes */}
        {submission.submissionNotes && (
          <div>
            <Label className="text-muted-foreground block mb-2">Additional Notes</Label>
            <div className="p-3 bg-muted rounded-lg">
              <p className="text-sm">{submission.submissionNotes}</p>
            </div>
          </div>
        )}

        {/* Review Feedback */}
        {submission.isReviewed && submission.reviewFeedback && (
          <div>
            <Label className="text-muted-foreground block mb-2">Review Feedback</Label>
            <div className={`p-3 rounded-lg ${submission.isApproved ? 'bg-success/10 border border-success/20' : 'bg-destructive/10 border border-destructive/20'}`}>
              <div className="flex items-start space-x-2 mb-2">
                {submission.isApproved ? (
                  <CheckCircle2 className="h-4 w-4 text-success mt-0.5" />
                ) : (
                  <XCircle className="h-4 w-4 text-destructive mt-0.5" />
                )}
                <div>
                  <p className="text-sm font-medium text-foreground">
                    {submission.reviewedByUserName}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {format(new Date(submission.reviewedAt!), 'MMM dd, yyyy HH:mm')}
                  </p>
                </div>
              </div>
              <p className="text-sm">{submission.reviewFeedback}</p>
            </div>
          </div>
        )}

        {/* Action Buttons */}
        {userRole === 'client' && !submission.isReviewed && (
          <div className="flex space-x-2">
            <Button
              onClick={() => onReview({ isApproved: true, reviewFeedback: '' })}
              className="flex items-center space-x-2"
            >
              <CheckCircle2 className="h-4 w-4" />
              <span>Approve</span>
            </Button>
            <Button
              variant="outline"
              onClick={() => onReview({ isApproved: false, reviewFeedback: '' })}
              className="flex items-center space-x-2 border-destructive/30 text-destructive hover:bg-destructive/10"
            >
              <XCircle className="h-4 w-4" />
              <span>Request Revisions</span>
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  );
};