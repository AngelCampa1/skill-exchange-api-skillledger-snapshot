import React, { useState, useCallback, useRef } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import {
  Upload,
  File,
  X,
  AlertCircle,
  CheckCircle2,
  FileText,
  Image as ImageIcon,
  Link as LinkIcon,
  Code,
  Loader2
} from 'lucide-react';
import { DeliverableType, CreateSubmissionRequest, AttachedFile } from '@/types/milestone';
import { AUTH_CONFIG } from '../../constants/auth';

interface EvidenceUploadProps {
  milestoneId: string;
  milestoneTitle: string;
  onSubmissionComplete: () => void;
  onCancel: () => void;
  maxFileSize?: number; // in MB
  allowedFileTypes?: string[];
}

interface UploadProgress {
  fileId: string;
  fileName: string;
  progress: number;
  status: 'uploading' | 'completed' | 'error';
  error?: string;
}

export const EvidenceUpload: React.FC<EvidenceUploadProps> = ({
  milestoneId,
  milestoneTitle,
  onSubmissionComplete,
  onCancel,
  maxFileSize = 10, // 10MB default
  allowedFileTypes = ['pdf', 'doc', 'docx', 'txt', 'png', 'jpg', 'jpeg', 'gif', 'zip', 'rar']
}) => {
  const [formData, setFormData] = useState<CreateSubmissionRequest>({
    milestoneId,
    type: DeliverableType.FileUpload,
    title: '',
    description: '',
    submissionNotes: '',
    attachedFileIds: []
  });

  const [attachedFiles, setAttachedFiles] = useState<AttachedFile[]>([]);
  const [uploadProgress, setUploadProgress] = useState<UploadProgress[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [dragActive, setDragActive] = useState(false);
  
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Handle form field changes
  const handleFieldChange = (field: keyof CreateSubmissionRequest, value: string | number | DeliverableType) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    
    // Clear field-specific errors
    if (errors[field]) {
      setErrors(prev => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  // Validate file before upload
  const validateFile = useCallback((file: File): string | null => {
    // Check file size
    if (file.size > maxFileSize * 1024 * 1024) {
      return `File size must be less than ${maxFileSize}MB`;
    }

    // Check file type
    const fileExtension = file.name.split('.').pop()?.toLowerCase();
    if (fileExtension && !allowedFileTypes.includes(fileExtension)) {
      return `File type .${fileExtension} is not allowed`;
    }

    return null;
  }, [maxFileSize, allowedFileTypes]);

  // Upload individual file
  const uploadFile = useCallback(async (file: File): Promise<string | null> => {
    const fileId = `${Date.now()}-${Math.random()}`;
    
    // Add to progress tracking
    const progressItem: UploadProgress = {
      fileId,
      fileName: file.name,
      progress: 0,
      status: 'uploading'
    };
    
    setUploadProgress(prev => [...prev, progressItem]);

    try {
      const formData = new FormData();
      formData.append('file', file);
      formData.append('containerPath', `milestones/${milestoneId}/evidence`);

      const response = await fetch('/api/files/upload', {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          // BUG-FE-002 FIX: Removed - Auth via httpOnly cookies,
        },
        body: formData
      });

      if (response.ok) {
        const uploadedFile: AttachedFile = await response.json();
        
        // Update progress to completed
        setUploadProgress(prev => prev.map(p => 
          p.fileId === fileId 
            ? { ...p, progress: 100, status: 'completed' }
            : p
        ));

        // Add to attached files
        setAttachedFiles(prev => [...prev, uploadedFile]);
        
        return uploadedFile.id;
      } else {
        throw new Error('Upload failed');
      }
    } catch (error) {
      // Update progress to error
      setUploadProgress(prev => prev.map(p => 
        p.fileId === fileId 
          ? { ...p, status: 'error', error: 'Upload failed' }
          : p
      ));
      return null;
    }
  }, [milestoneId]);

  // Handle file selection
  const handleFileSelect = useCallback(async (files: FileList) => {
    const validFiles: File[] = [];
    const fileErrors: string[] = [];

    // Validate each file
    for (let i = 0; i < files.length; i++) {
      const file = files[i];
      const error = validateFile(file);
      
      if (error) {
        fileErrors.push(`${file.name}: ${error}`);
      } else {
        validFiles.push(file);
      }
    }

    // Show validation errors
    if (fileErrors.length > 0) {
      setErrors(prev => ({
        ...prev,
        files: fileErrors.join(', ')
      }));
    }

    // Upload valid files
    if (validFiles.length > 0) {
      const uploadPromises = validFiles.map(file => uploadFile(file));
      const uploadedFileIds = await Promise.all(uploadPromises);
      
      const successfulIds = uploadedFileIds.filter(id => id !== null) as string[];
      setFormData(prev => ({
        ...prev,
        attachedFileIds: [...(prev.attachedFileIds || []), ...successfulIds]
      }));
    }
  }, [validateFile, uploadFile]);

  // Handle drag and drop
  const handleDrag = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    
    if (e.type === "dragenter" || e.type === "dragover") {
      setDragActive(true);
    } else if (e.type === "dragleave") {
      setDragActive(false);
    }
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      handleFileSelect(e.dataTransfer.files);
    }
  }, [handleFileSelect]);

  // Remove attached file
  const removeAttachedFile = (fileId: string) => {
    setAttachedFiles(prev => prev.filter(f => f.id !== fileId));
    setFormData(prev => ({
      ...prev,
      attachedFileIds: prev.attachedFileIds?.filter(id => id !== fileId)
    }));
  };

  // Validate form
  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.title?.trim()) {
      newErrors.title = 'Title is required';
    }

    if (formData.type === DeliverableType.FileUpload && (!formData.attachedFileIds || formData.attachedFileIds.length === 0)) {
      newErrors.files = 'At least one file is required for file uploads';
    }

    if (formData.type === DeliverableType.Link && !formData.submissionUrl?.trim()) {
      newErrors.submissionUrl = 'URL is required for link submissions';
    }

    if (formData.type === DeliverableType.Text && !formData.textContent?.trim()) {
      newErrors.textContent = 'Text content is required for text submissions';
    }

    if (formData.type === DeliverableType.CodeRepository && !formData.submissionUrl?.trim()) {
      newErrors.submissionUrl = 'Repository URL is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Submit submission
  const handleSubmit = async () => {
    if (!validateForm()) return;

    setIsSubmitting(true);
    
    try {
      const response = await fetch(`/api/milestone/${milestoneId}/submissions`, {
        method: 'POST',
        credentials: AUTH_CONFIG.CREDENTIALS,
        headers: {
          // BUG-FE-002 FIX: Removed - Auth via httpOnly cookies,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(formData)
      });

      if (response.ok) {
        onSubmissionComplete();
      } else {
        const errorData = await response.json();
        setErrors({ submit: errorData.message || 'Failed to submit deliverable' });
      }
    } catch (error) {
      setErrors({ submit: 'Network error occurred' });
    } finally {
      setIsSubmitting(false);
    }
  };

  const getFileIcon = (fileName: string) => {
    const extension = fileName.split('.').pop()?.toLowerCase();
    switch (extension) {
      case 'pdf':
        return <FileText className="h-4 w-4 text-destructive" />;
      case 'png':
      case 'jpg':
      case 'jpeg':
      case 'gif':
        return <ImageIcon className="h-4 w-4 text-success" />;
      case 'zip':
      case 'rar':
        return <File className="h-4 w-4 text-info" />;
      default:
        return <File className="h-4 w-4 text-muted-foreground" />;
    }
  };

  return (
    <Card className="w-full max-w-2xl">
      <CardHeader>
        <CardTitle>Submit Deliverable Evidence</CardTitle>
        <p className="text-sm text-muted-foreground">
          Submit evidence for milestone: <span className="font-medium">{milestoneTitle}</span>
        </p>
      </CardHeader>

      <CardContent className="space-y-6">
        {/* Submission Type */}
        <div className="space-y-2">
          <Label htmlFor="type">Submission Type</Label>
          <Select 
            value={formData.type} 
            onValueChange={(value: string) => handleFieldChange('type', value as DeliverableType)}
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={DeliverableType.FileUpload}>
                <div className="flex items-center space-x-2">
                  <Upload className="h-4 w-4" />
                  <span>File Upload</span>
                </div>
              </SelectItem>
              <SelectItem value={DeliverableType.Text}>
                <div className="flex items-center space-x-2">
                  <FileText className="h-4 w-4" />
                  <span>Text Content</span>
                </div>
              </SelectItem>
              <SelectItem value={DeliverableType.Link}>
                <div className="flex items-center space-x-2">
                  <LinkIcon className="h-4 w-4" />
                  <span>Link/URL</span>
                </div>
              </SelectItem>
              <SelectItem value={DeliverableType.CodeRepository}>
                <div className="flex items-center space-x-2">
                  <Code className="h-4 w-4" />
                  <span>Code Repository</span>
                </div>
              </SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Title */}
        <div className="space-y-2">
          <Label htmlFor="title">Title *</Label>
          <Input
            id="title"
            value={formData.title}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleFieldChange('title', e.target.value)}
            placeholder="Brief title for your submission"
            className={errors.title ? 'border-destructive' : ''}
          />
          {errors.title && (
            <div className="flex items-center space-x-1 text-sm text-destructive">
              <AlertCircle className="h-4 w-4" />
              <span>{errors.title}</span>
            </div>
          )}
        </div>

        {/* Description */}
        <div className="space-y-2">
          <Label htmlFor="description">Description</Label>
          <Textarea
            id="description"
            value={formData.description || ''}
            onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => handleFieldChange('description', e.target.value)}
            placeholder="Describe what you've delivered"
            rows={3}
          />
        </div>

        {/* Conditional Fields Based on Type */}
        {(formData.type === DeliverableType.Link || formData.type === DeliverableType.CodeRepository) && (
          <div className="space-y-2">
            <Label htmlFor="submissionUrl">
              {formData.type === DeliverableType.CodeRepository ? 'Repository URL *' : 'URL *'}
            </Label>
            <Input
              id="submissionUrl"
              type="url"
              value={formData.submissionUrl || ''}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleFieldChange('submissionUrl', e.target.value)}
              placeholder={formData.type === DeliverableType.CodeRepository ? 'https://github.com/...' : 'https://...'}
              className={errors.submissionUrl ? 'border-destructive' : ''}
            />
            {errors.submissionUrl && (
              <div className="flex items-center space-x-1 text-sm text-destructive">
                <AlertCircle className="h-4 w-4" />
                <span>{errors.submissionUrl}</span>
              </div>
            )}
          </div>
        )}

        {formData.type === DeliverableType.Text && (
          <div className="space-y-2">
            <Label htmlFor="textContent">Text Content *</Label>
            <Textarea
              id="textContent"
              value={formData.textContent || ''}
              onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => handleFieldChange('textContent', e.target.value)}
              placeholder="Enter your text content here..."
              rows={6}
              className={errors.textContent ? 'border-destructive' : ''}
            />
            {errors.textContent && (
              <div className="flex items-center space-x-1 text-sm text-destructive">
                <AlertCircle className="h-4 w-4" />
                <span>{errors.textContent}</span>
              </div>
            )}
          </div>
        )}

        {/* File Upload Area */}
        {formData.type === DeliverableType.FileUpload && (
          <div className="space-y-4">
            <Label>Evidence Files *</Label>

            {/* Drag & Drop Area */}
            <div
              className={`border-2 border-dashed rounded-lg p-6 text-center transition-colors ${
                dragActive
                  ? 'border-primary bg-primary/10'
                  : errors.files
                    ? 'border-destructive/30 bg-destructive/10'
                    : 'border-border hover:border-input'
              }`}
              onDragEnter={handleDrag}
              onDragLeave={handleDrag}
              onDragOver={handleDrag}
              onDrop={handleDrop}
            >
              <Upload className="h-8 w-8 text-muted-foreground mx-auto mb-2" />
              <p className="text-sm text-muted-foreground mb-2">
                Drag & drop files here, or{' '}
                <button
                  type="button"
                  onClick={() => fileInputRef.current?.click()}
                  className="text-primary hover:text-primary/80 underline"
                >
                  browse files
                </button>
              </p>
              <p className="text-xs text-muted-foreground">
                Max {maxFileSize}MB per file. Allowed: {allowedFileTypes.join(', ')}
              </p>
            </div>

            <input
              ref={fileInputRef}
              type="file"
              multiple
              accept={allowedFileTypes.map(ext => `.${ext}`).join(',')}
              onChange={(e) => e.target.files && handleFileSelect(e.target.files)}
              className="hidden"
            />

            {errors.files && (
              <div className="flex items-center space-x-1 text-sm text-destructive">
                <AlertCircle className="h-4 w-4" />
                <span>{errors.files}</span>
              </div>
            )}
          </div>
        )}

        {/* Upload Progress */}
        {uploadProgress.length > 0 && (
          <div className="space-y-2">
            <Label>Upload Progress</Label>
            {uploadProgress.map((progress) => (
              <div key={progress.fileId} className="space-y-1">
                <div className="flex items-center justify-between text-sm">
                  <span className="truncate">{progress.fileName}</span>
                  <span className={
                    progress.status === 'completed' ? 'text-success' :
                    progress.status === 'error' ? 'text-destructive' : 'text-primary'
                  }>
                    {progress.status === 'completed' ? 'Complete' :
                     progress.status === 'error' ? 'Error' : `${progress.progress}%`}
                  </span>
                </div>
                {progress.status === 'uploading' && (
                  <Progress value={progress.progress} className="h-1" />
                )}
                {progress.error && (
                  <p className="text-xs text-destructive">{progress.error}</p>
                )}
              </div>
            ))}
          </div>
        )}

        {/* Attached Files */}
        {attachedFiles.length > 0 && (
          <div className="space-y-2">
            <Label>Attached Files</Label>
            <div className="space-y-2">
              {attachedFiles.map((file) => (
                <div key={file.id} className="flex items-center justify-between p-2 bg-muted rounded">
                  <div className="flex items-center space-x-2">
                    {getFileIcon(file.fileName)}
                    <div>
                      <p className="text-sm font-medium">{file.fileName}</p>
                      <p className="text-xs text-muted-foreground">
                        {(file.fileSize / 1024 / 1024).toFixed(1)} MB
                      </p>
                    </div>
                  </div>
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => removeAttachedFile(file.id)}
                    className="h-6 w-6 p-0 text-muted-foreground hover:text-destructive"
                  >
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Submission Notes */}
        <div className="space-y-2">
          <Label htmlFor="notes">Additional Notes</Label>
          <Textarea
            id="notes"
            value={formData.submissionNotes || ''}
            onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => handleFieldChange('submissionNotes', e.target.value)}
            placeholder="Any additional notes for the client..."
            rows={3}
          />
        </div>

        {/* Submit Error */}
        {errors.submit && (
          <div className="flex items-center space-x-1 text-sm text-destructive">
            <AlertCircle className="h-4 w-4" />
            <span>{errors.submit}</span>
          </div>
        )}

        {/* Action Buttons */}
        <div className="flex justify-end space-x-3">
          <Button variant="outline" onClick={onCancel} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={isSubmitting}>
            {isSubmitting ? (
              <div className="flex items-center space-x-2">
                <Loader2 className="h-4 w-4 animate-spin" />
                <span>Submitting...</span>
              </div>
            ) : (
              <div className="flex items-center space-x-2">
                <CheckCircle2 className="h-4 w-4" />
                <span>Submit Evidence</span>
              </div>
            )}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
};