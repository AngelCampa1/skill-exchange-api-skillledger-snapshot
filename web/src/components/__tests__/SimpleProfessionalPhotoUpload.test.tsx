/**
 * Tests for SimpleProfessionalPhotoUpload
 *
 * Comprehensive test suite for the photo upload component with XHR progress tracking
 * Coverage target: 80%+ (411 lines)
 */

import React from 'react'
import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import SimpleProfessionalPhotoUpload from '../SimpleProfessionalPhotoUpload'

// Mock logger
jest.mock('@/utils/logger', () => ({
  logger: {
    error: jest.fn(),
  },
}))

// Mock Next.js Image component
jest.mock('next/image', () => ({
  __esModule: true,
  default: (props: any) => {
    // eslint-disable-next-line @next/next/no-img-element, jsx-a11y/alt-text
    return <img {...props} />
  },
}))

describe('SimpleProfessionalPhotoUpload', () => {
  let mockOnUploadComplete: jest.Mock
  let mockFetch: jest.MockedFunction<typeof fetch>
  let mockXHR: any
  let xhrInstance: any

  beforeEach(() => {
    mockOnUploadComplete = jest.fn()

    // Mock global fetch for CSRF token
    mockFetch = jest.fn()
    global.fetch = mockFetch

    // Mock XMLHttpRequest for file upload
    xhrInstance = {
      open: jest.fn(),
      send: jest.fn(),
      setRequestHeader: jest.fn(),
      upload: {
        addEventListener: jest.fn(),
      },
      addEventListener: jest.fn(),
      status: 200,
      responseText: JSON.stringify({ success: true, fileId: 'file-123', fileUrl: 'https://example.com/photo.jpg' }),
    }

    mockXHR = jest.fn(() => xhrInstance)
    global.XMLHttpRequest = mockXHR as any

    // Mock URL.createObjectURL
    global.URL.createObjectURL = jest.fn(() => 'blob:mock-url')

    // Mock CSRF token response
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => ({ token: 'mock-csrf-token' }),
    } as Response)
  })

  afterEach(() => {
    jest.clearAllMocks()
  })

  describe('Initial Rendering', () => {
    it('should render upload area', () => {
      render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      expect(screen.getByText('Upload a professional photo')).toBeInTheDocument()
      expect(screen.getByText('Drag and drop or click to select')).toBeInTheDocument()
    })

    it('should render professional photo guidelines', () => {
      render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      expect(screen.getByText('Professional Photo Guidelines')).toBeInTheDocument()
      expect(screen.getByText(/Use a clear, high-quality headshot/)).toBeInTheDocument()
    })

    it('should render file input with correct accept types', () => {
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const fileInput = container.querySelector('input[type="file"]')
      expect(fileInput).toHaveAttribute('accept', 'image/jpeg,image/jpg,image/png,image/webp')
    })

    it('should render with current photo URL when provided', () => {
      render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} currentPhotoUrl="https://example.com/current.jpg" />)

      const img = screen.getByAltText('Profile preview')
      expect(img).toHaveAttribute('src', 'https://example.com/current.jpg')
    })
  })

  describe('File Selection', () => {
    it('should open file picker when clicking upload area', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const uploadArea = screen.getByText('Upload a professional photo').closest('div')
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement
      const clickSpy = jest.spyOn(fileInput, 'click')

      await user.click(uploadArea!)

      expect(clickSpy).toHaveBeenCalled()
    })

    it('should not open file picker when uploading', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      // Start upload
      await user.upload(fileInput, validFile)

      // During upload, input should be disabled
      await waitFor(() => {
        expect(fileInput).toBeDisabled()
      })
    })

    it('should not open file picker when loading', () => {
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} isLoading />)

      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement
      expect(fileInput).toBeDisabled()
    })
  })

  describe('File Validation', () => {
    it('should reject files larger than 5MB', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const largeFile = new File(['a'.repeat(6 * 1024 * 1024)], 'large.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, largeFile)

      await waitFor(() => {
        expect(screen.getByText('File size must be less than 5MB')).toBeInTheDocument()
      })

      expect(mockOnUploadComplete).not.toHaveBeenCalled()
    })

    it('should reject invalid file types', async () => {
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const invalidFile = new File(['content'], 'file.gif', { type: 'image/gif' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      // Manually set files and trigger change (bypassing accept attribute)
      Object.defineProperty(fileInput, 'files', {
        value: [invalidFile],
        writable: false,
      })
      fireEvent.change(fileInput)

      await waitFor(() => {
        expect(screen.getByText('Only JPEG, PNG, and WebP images are allowed')).toBeInTheDocument()
      })

      expect(mockOnUploadComplete).not.toHaveBeenCalled()
    })

    it('should reject files with names longer than 255 characters', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const longName = 'a'.repeat(256) + '.jpg'
      const invalidFile = new File(['content'], longName, { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, invalidFile)

      await waitFor(() => {
        expect(screen.getByText('File name is too long')).toBeInTheDocument()
      })

      expect(mockOnUploadComplete).not.toHaveBeenCalled()
    })

    it('should accept valid JPEG files', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      // Trigger XHR load event
      const loadHandler = xhrInstance.addEventListener.mock.calls.find((call: any) => call[0] === 'load')[1]
      loadHandler()

      await waitFor(() => {
        expect(mockOnUploadComplete).toHaveBeenCalled()
      })
    })

    it('should accept valid PNG files', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const validFile = new File(['content'], 'photo.png', { type: 'image/png' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      // Trigger XHR load event
      const loadHandler = xhrInstance.addEventListener.mock.calls.find((call: any) => call[0] === 'load')[1]
      loadHandler()

      await waitFor(() => {
        expect(mockOnUploadComplete).toHaveBeenCalled()
      })
    })

    it('should accept valid WebP files', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const validFile = new File(['content'], 'photo.webp', { type: 'image/webp' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      // Trigger XHR load event
      const loadHandler = xhrInstance.addEventListener.mock.calls.find((call: any) => call[0] === 'load')[1]
      loadHandler()

      await waitFor(() => {
        expect(mockOnUploadComplete).toHaveBeenCalled()
      })
    })
  })

  describe('File Upload', () => {
    it('should fetch CSRF token before upload', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/csrf-token')
      })
    })

    it('should display uploading state with progress', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      await waitFor(() => {
        expect(screen.getByText(/Uploading.../)).toBeInTheDocument()
      })
    })

    it('should track upload progress', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      // Trigger progress event
      const progressHandler = xhrInstance.upload.addEventListener.mock.calls.find((call: any) => call[0] === 'progress')[1]
      progressHandler({ lengthComputable: true, loaded: 50, total: 100 })

      await waitFor(() => {
        expect(screen.getByText(/50%/)).toBeInTheDocument()
      })
    })

    it('should handle successful upload with approved status', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      xhrInstance.responseText = JSON.stringify({
        success: true,
        fileId: 'file-123',
        fileUrl: 'https://example.com/photo.jpg',
        moderationStatus: 'approved',
      })

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      // Trigger XHR load event
      const loadHandler = xhrInstance.addEventListener.mock.calls.find((call: any) => call[0] === 'load')[1]
      loadHandler()

      await waitFor(() => {
        expect(screen.getByText('Photo uploaded and approved successfully!')).toBeInTheDocument()
        expect(mockOnUploadComplete).toHaveBeenCalledWith({
          success: true,
          fileId: 'file-123',
          fileUrl: 'https://example.com/photo.jpg',
          moderationStatus: 'approved',
        })
      })
    })

    it('should handle successful upload with pending moderation', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      xhrInstance.responseText = JSON.stringify({
        success: true,
        fileId: 'file-123',
        fileUrl: 'https://example.com/photo.jpg',
        moderationStatus: 'pending',
      })

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      // Trigger XHR load event
      const loadHandler = xhrInstance.addEventListener.mock.calls.find((call: any) => call[0] === 'load')[1]
      loadHandler()

      await waitFor(() => {
        expect(screen.getByText('Photo uploaded successfully! Content review in progress.')).toBeInTheDocument()
        expect(screen.getByText('Content Review in Progress')).toBeInTheDocument()
      })
    })

    it('should handle upload failure with error message', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      xhrInstance.status = 400
      xhrInstance.responseText = JSON.stringify({ error: 'Invalid file format' })

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      // Trigger XHR load event
      const loadHandler = xhrInstance.addEventListener.mock.calls.find((call: any) => call[0] === 'load')[1]
      loadHandler()

      await waitFor(() => {
        expect(screen.getByText('Invalid file format')).toBeInTheDocument()
      })

      expect(mockOnUploadComplete).not.toHaveBeenCalled()
    })

    it('should handle network error during upload', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      // Trigger XHR error event
      const errorHandler = xhrInstance.addEventListener.mock.calls.find((call: any) => call[0] === 'error')[1]
      errorHandler()

      await waitFor(() => {
        expect(screen.getByText('Upload failed. Please try again.')).toBeInTheDocument()
      })

      expect(mockOnUploadComplete).not.toHaveBeenCalled()
    })

    it('should handle CSRF token fetch failure', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
      } as Response)

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      await waitFor(() => {
        expect(screen.getByText('Failed to get security token')).toBeInTheDocument()
      }, { timeout: 2000 })

      expect(xhrInstance.send).not.toHaveBeenCalled()
    })
  })

  describe('Drag and Drop', () => {
    it('should highlight upload area on drag over', () => {
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const uploadArea = container.querySelector('.border-dashed')
      fireEvent.dragOver(uploadArea!)

      expect(uploadArea).toHaveClass('border-primary')
    })

    it('should remove highlight on drag leave', () => {
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const uploadArea = container.querySelector('.border-dashed')
      fireEvent.dragOver(uploadArea!)
      fireEvent.dragLeave(uploadArea!)

      expect(uploadArea).not.toHaveClass('border-primary')
    })

    it('should handle file drop', async () => {
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const uploadArea = container.querySelector('.border-dashed')

      // Create FileList-like object
      const fileList = {
        0: validFile,
        length: 1,
        item: (index: number) => (index === 0 ? validFile : null),
      }

      fireEvent.drop(uploadArea!, {
        dataTransfer: {
          files: fileList,
        },
      })

      // Wait for XHR setup, then trigger load event
      await waitFor(() => {
        expect(xhrInstance.addEventListener).toHaveBeenCalled()
      })

      const loadHandler = xhrInstance.addEventListener.mock.calls.find((call: any) => call[0] === 'load')[1]
      loadHandler()

      await waitFor(() => {
        expect(mockOnUploadComplete).toHaveBeenCalled()
      })
    })

    it('should show drop message when dragging over', () => {
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const uploadArea = container.querySelector('.border-dashed')
      fireEvent.dragOver(uploadArea!)

      expect(screen.getByText('Drop your photo here')).toBeInTheDocument()
    })
  })

  describe('Photo Preview', () => {
    it('should display preview after selecting file', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      await waitFor(() => {
        const preview = screen.getByAltText('Profile preview')
        expect(preview).toBeInTheDocument()
        expect(preview).toHaveAttribute('src', 'blob:mock-url')
      })
    })

    it('should show remove button when preview is displayed', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} currentPhotoUrl="https://example.com/photo.jpg" />)

      const removeButton = container.querySelector('button[title="Remove photo"]')
      expect(removeButton).toBeInTheDocument()
    })

    it('should restore previous photo on upload error', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} currentPhotoUrl="https://example.com/current.jpg" />)

      xhrInstance.status = 400
      xhrInstance.responseText = JSON.stringify({ error: 'Upload failed' })

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      // Trigger XHR load event
      const loadHandler = xhrInstance.addEventListener.mock.calls.find((call: any) => call[0] === 'load')[1]
      loadHandler()

      await waitFor(() => {
        const preview = screen.getByAltText('Profile preview')
        expect(preview).toHaveAttribute('src', 'https://example.com/current.jpg')
      })
    })
  })

  describe('Remove Photo', () => {
    it('should remove photo when clicking remove button', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} currentPhotoUrl="https://example.com/photo.jpg" />)

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'mock-csrf-token' }),
      } as Response)

      mockFetch.mockResolvedValueOnce({
        ok: true,
      } as Response)

      const removeButton = container.querySelector('button[title="Remove photo"]')
      await user.click(removeButton!)

      await waitFor(() => {
        expect(mockFetch).toHaveBeenCalledWith('/api/profile/avatar', {
          method: 'DELETE',
          headers: {
            'X-CSRF-Token': 'mock-csrf-token',
          },
        })
        expect(screen.getByText('Photo removed successfully')).toBeInTheDocument()
        expect(mockOnUploadComplete).toHaveBeenCalledWith({
          success: true,
          fileId: undefined,
          fileUrl: undefined,
        })
      })
    })

    it('should handle remove photo failure', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} currentPhotoUrl="https://example.com/photo.jpg" />)

      mockFetch.mockResolvedValueOnce({
        ok: true,
        json: async () => ({ token: 'mock-csrf-token' }),
      } as Response)

      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
      } as Response)

      const removeButton = container.querySelector('button[title="Remove photo"]')
      await user.click(removeButton!)

      await waitFor(() => {
        expect(screen.getByText('Failed to remove photo')).toBeInTheDocument()
      })
    })
  })

  describe('Moderation Status', () => {
    it('should show moderation pending icon', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      xhrInstance.responseText = JSON.stringify({
        success: true,
        fileId: 'file-123',
        fileUrl: 'https://example.com/photo.jpg',
        moderationStatus: 'pending',
      })

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      // Trigger XHR load event
      const loadHandler = xhrInstance.addEventListener.mock.calls.find((call: any) => call[0] === 'load')[1]
      loadHandler()

      await waitFor(() => {
        const moderationIcon = container.querySelector('.bg-warning\\/10')
        expect(moderationIcon).toBeTruthy()
      })
    })

    it('should display moderation message', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      xhrInstance.responseText = JSON.stringify({
        success: true,
        fileId: 'file-123',
        fileUrl: 'https://example.com/photo.jpg',
        moderationStatus: 'pending',
      })

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      // Trigger XHR load event
      const loadHandler = xhrInstance.addEventListener.mock.calls.find((call: any) => call[0] === 'load')[1]
      loadHandler()

      await waitFor(() => {
        expect(screen.getByText(/Your photo is being reviewed for content policy compliance/)).toBeInTheDocument()
      })
    })
  })

  describe('Edge Cases', () => {
    it('should handle upload abort', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      // Trigger XHR abort event
      const abortHandler = xhrInstance.addEventListener.mock.calls.find((call: any) => call[0] === 'abort')[1]
      abortHandler()

      await waitFor(() => {
        expect(screen.getByText('Upload failed. Please try again.')).toBeInTheDocument()
      })
    })

    it('should handle empty file list', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      // Trigger change with no files
      fireEvent.change(fileInput, { target: { files: [] } })

      // Should not show any error
      expect(screen.queryByText(/error/i)).not.toBeInTheDocument()
    })

    it('should handle invalid JSON response', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      xhrInstance.responseText = 'invalid json'

      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      // Trigger XHR load event
      const loadHandler = xhrInstance.addEventListener.mock.calls.find((call: any) => call[0] === 'load')[1]
      loadHandler()

      await waitFor(() => {
        expect(screen.getByText('Upload failed. Please try again.')).toBeInTheDocument()
      })
    })

    it('should clear previous error messages when selecting new file', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      // First, select an invalid file
      const largeFile = new File(['a'.repeat(6 * 1024 * 1024)], 'large.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, largeFile)

      await waitFor(() => {
        expect(screen.getByText('File size must be less than 5MB')).toBeInTheDocument()
      })

      // Then select a valid file
      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      await user.upload(fileInput, validFile)

      // Error should be cleared
      expect(screen.queryByText('File size must be less than 5MB')).not.toBeInTheDocument()
    })
  })

  describe('Integration', () => {
    it('should render complete component without errors', () => {
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      expect(container.firstChild).toBeTruthy()
      expect(screen.getByText('Upload a professional photo')).toBeInTheDocument()
      expect(screen.getByText('Professional Photo Guidelines')).toBeInTheDocument()
    })

    it('should handle full upload workflow', async () => {
      const user = userEvent.setup()
      const { container } = render(<SimpleProfessionalPhotoUpload onUploadComplete={mockOnUploadComplete} />)

      // Select file
      const validFile = new File(['content'], 'photo.jpg', { type: 'image/jpeg' })
      const fileInput = container.querySelector('input[type="file"]') as HTMLInputElement

      await user.upload(fileInput, validFile)

      // Should show loading state
      await waitFor(() => {
        expect(screen.getByText(/Uploading.../)).toBeInTheDocument()
      })

      // Trigger progress
      const progressHandler = xhrInstance.upload.addEventListener.mock.calls.find((call: any) => call[0] === 'progress')[1]
      progressHandler({ lengthComputable: true, loaded: 75, total: 100 })

      // Should show progress
      await waitFor(() => {
        expect(screen.getByText(/75%/)).toBeInTheDocument()
      })

      // Trigger completion
      const loadHandler = xhrInstance.addEventListener.mock.calls.find((call: any) => call[0] === 'load')[1]
      loadHandler()

      // Should show success
      await waitFor(() => {
        expect(screen.getByText('Photo uploaded successfully!')).toBeInTheDocument()
        expect(mockOnUploadComplete).toHaveBeenCalled()
      })
    })
  })
})
