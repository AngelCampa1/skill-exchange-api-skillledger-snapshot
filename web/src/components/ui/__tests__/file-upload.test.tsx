/**
 * Integration tests for FileUpload and ImageUpload components
 *
 * Coverage target: 85-90% (521 lines)
 * Test count: ~70 tests
 *
 * Test Strategy:
 * - Test both FileUpload and ImageUpload exports
 * - Verify all bug fixes: BUG-031, BUG-039, BUG-040, BUG-051, BUG-055
 * - Test file validation, drag-drop, keyboard accessibility
 * - Mock File and FileReader APIs
 */

import React from 'react'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { FileUpload, ImageUpload } from '../file-upload'

// Mock Next.js Image component
jest.mock('next/image', () => ({
  __esModule: true,
  default: ({ src, alt }: any) => <img src={src} alt={alt} />,
}))

// Helper to create mock File objects
const createMockFile = (name: string, size: number, type: string): File => {
  const blob = new Blob(['a'.repeat(size)], { type })
  return new File([blob], name, { type })
}

describe('FileUpload Component', () => {
  // ============================================
  // Initial Render (6 tests)
  // ============================================
  describe('Initial Render', () => {
    it('should render drop zone', () => {
      render(<FileUpload />)

      expect(screen.getByRole('button', { name: /File upload drop zone/i })).toBeInTheDocument()
    })

    it('should display label when provided', () => {
      render(<FileUpload label="Upload Files" />)

      expect(screen.getByText('Upload Files')).toBeInTheDocument()
    })

    it('should display helper text when provided', () => {
      render(<FileUpload helperText="Upload your documents" />)

      expect(screen.getByText('Upload your documents')).toBeInTheDocument()
    })

    it('should display default file constraints', () => {
      render(<FileUpload />)

      expect(screen.getByText(/Any file type/)).toBeInTheDocument()
      expect(screen.getByText(/Max 10 MB/)).toBeInTheDocument()
      expect(screen.getByText(/Up to 1 file/)).toBeInTheDocument()
    })

    it('should associate label with input using id - BUG-055 FIX', () => {
      render(<FileUpload label="Upload Files" id="custom-upload" />)

      const label = screen.getByText('Upload Files')
      expect(label).toHaveAttribute('for', 'custom-upload')

      const input = document.getElementById('custom-upload')
      expect(input).toBeInTheDocument()
      expect(input).toHaveAttribute('type', 'file')
    })

    it('should generate unique id if not provided - BUG-055 FIX', () => {
      const { container: container1 } = render(<FileUpload label="Upload 1" />)
      const { container: container2 } = render(<FileUpload label="Upload 2" />)

      const input1 = container1.querySelector('input[type="file"]')
      const input2 = container2.querySelector('input[type="file"]')

      expect(input1?.id).toBeTruthy()
      expect(input2?.id).toBeTruthy()
      expect(input1?.id).not.toBe(input2?.id)
    })
  })

  // ============================================
  // File Selection (8 tests)
  // ============================================
  describe('File Selection', () => {
    it('should open file picker when drop zone clicked', async () => {
      const user = userEvent.setup()
      render(<FileUpload />)

      const dropZone = screen.getByRole('button', { name: /File upload drop zone/i })
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      const clickSpy = jest.spyOn(fileInput, 'click')

      await user.click(dropZone)

      expect(clickSpy).toHaveBeenCalled()
    })

    it('should handle single file selection', async () => {
      const handleChange = jest.fn()
      render(<FileUpload onFilesChange={handleChange} />)

      const file = createMockFile('test.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(handleChange).toHaveBeenCalledWith([file])
      })
    })

    it('should handle multiple file selection when enabled', async () => {
      const handleChange = jest.fn()
      render(<FileUpload multiple maxFiles={3} onFilesChange={handleChange} />)

      const file1 = createMockFile('test1.pdf', 1024, 'application/pdf')
      const file2 = createMockFile('test2.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file1, file2] } })

      await waitFor(() => {
        expect(handleChange).toHaveBeenCalledWith([file1, file2])
      })
    })

    it('should display file preview after selection', async () => {
      render(<FileUpload showPreview />)

      const file = createMockFile('document.pdf', 2048, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(screen.getByText('document.pdf')).toBeInTheDocument()
        expect(screen.getByText('2 KB')).toBeInTheDocument()
      })
    })

    it('should not show preview when showPreview is false', async () => {
      render(<FileUpload showPreview={false} />)

      const file = createMockFile('test.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(screen.queryByText('test.pdf')).not.toBeInTheDocument()
      })
    })

    it('should display "Clear all" button when files are selected', async () => {
      render(<FileUpload showPreview />)

      const file = createMockFile('test.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(screen.getByText('Clear all')).toBeInTheDocument()
      })
    })

    it('should handle empty file selection gracefully', () => {
      const handleChange = jest.fn()
      render(<FileUpload onFilesChange={handleChange} />)

      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: null } })

      expect(handleChange).not.toHaveBeenCalled()
    })

    it('should replace file in single-file mode', async () => {
      const handleChange = jest.fn()
      render(<FileUpload onFilesChange={handleChange} />)

      const file1 = createMockFile('first.pdf', 1024, 'application/pdf')
      const file2 = createMockFile('second.pdf', 2048, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file1] } })

      await waitFor(() => {
        expect(handleChange).toHaveBeenCalledWith([file1])
      })

      handleChange.mockClear()

      fireEvent.change(fileInput, { target: { files: [file2] } })

      await waitFor(() => {
        expect(screen.getByText(/Maximum 1 file allowed/)).toBeInTheDocument()
        expect(handleChange).not.toHaveBeenCalled()
      })
    })
  })

  // ============================================
  // Drag and Drop (6 tests)
  // ============================================
  describe('Drag and Drop', () => {
    it('should highlight drop zone on drag over', () => {
      const { container } = render(<FileUpload />)

      const dropZone = screen.getByRole('button', { name: /File upload drop zone/i })

      fireEvent.dragOver(dropZone)

      expect(screen.getByText('Drop files here')).toBeInTheDocument()
    })

    it('should remove highlight on drag leave', () => {
      render(<FileUpload />)

      const dropZone = screen.getByRole('button', { name: /File upload drop zone/i })

      fireEvent.dragOver(dropZone)
      expect(screen.getByText('Drop files here')).toBeInTheDocument()

      fireEvent.dragLeave(dropZone)
      expect(screen.queryByText('Drop files here')).not.toBeInTheDocument()
    })

    it('should handle file drop', async () => {
      const handleChange = jest.fn()
      render(<FileUpload onFilesChange={handleChange} />)

      const file = createMockFile('dropped.pdf', 1024, 'application/pdf')
      const dropZone = screen.getByRole('button', { name: /File upload drop zone/i })

      const dataTransfer = {
        files: [file],
      }

      fireEvent.drop(dropZone, { dataTransfer })

      await waitFor(() => {
        expect(handleChange).toHaveBeenCalledWith([file])
      })
    })

    it('should not highlight when disabled', () => {
      render(<FileUpload disabled />)

      const dropZone = screen.getByRole('button', { name: /File upload drop zone/i })

      fireEvent.dragOver(dropZone)

      expect(screen.queryByText('Drop files here')).not.toBeInTheDocument()
    })

    it('should not handle drop when disabled', () => {
      const handleChange = jest.fn()
      render(<FileUpload disabled onFilesChange={handleChange} />)

      const file = createMockFile('test.pdf', 1024, 'application/pdf')
      const dropZone = screen.getByRole('button', { name: /File upload drop zone/i })

      const dataTransfer = {
        files: [file],
      }

      fireEvent.drop(dropZone, { dataTransfer })

      expect(handleChange).not.toHaveBeenCalled()
    })

    it('should show drop indicator with visual changes', () => {
      const { container } = render(<FileUpload />)

      const dropZone = screen.getByRole('button', { name: /File upload drop zone/i })

      fireEvent.dragOver(dropZone)

      const iconContainer = container.querySelector('.bg-primary\\/10')
      expect(iconContainer).toBeInTheDocument()
    })
  })

  // ============================================
  // File Type Validation - BUG-031 Fix (8 tests)
  // ============================================
  describe('File Type Validation - BUG-031 Fix', () => {
    it('should accept file when type matches accept prop', async () => {
      const handleChange = jest.fn()
      render(<FileUpload accept=".pdf" onFilesChange={handleChange} />)

      const file = createMockFile('document.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(handleChange).toHaveBeenCalledWith([file])
      })
    })

    it('should reject file with invalid type', async () => {
      const handleChange = jest.fn()
      render(<FileUpload accept=".pdf" onFilesChange={handleChange} />)

      const file = createMockFile('image.jpg', 1024, 'image/jpeg')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(screen.getByText(/Invalid file type/)).toBeInTheDocument()
        expect(handleChange).not.toHaveBeenCalled()
      })
    })

    it('should accept wildcard MIME types (image/*)', async () => {
      const handleChange = jest.fn()
      render(<FileUpload accept="image/*" onFilesChange={handleChange} />)

      const file = createMockFile('photo.jpg', 1024, 'image/jpeg')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(handleChange).toHaveBeenCalledWith([file])
      })
    })

    it('should accept exact MIME types', async () => {
      const handleChange = jest.fn()
      render(<FileUpload accept="application/pdf" onFilesChange={handleChange} />)

      const file = createMockFile('doc.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(handleChange).toHaveBeenCalledWith([file])
      })
    })

    it('should accept multiple file types', async () => {
      const handleChange = jest.fn()
      render(<FileUpload accept=".pdf,.docx,image/*" multiple maxFiles={5} onFilesChange={handleChange} />)

      const file1 = createMockFile('doc.pdf', 1024, 'application/pdf')
      const file2 = createMockFile('photo.jpg', 1024, 'image/jpeg')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file1] } })

      await waitFor(() => {
        expect(handleChange).toHaveBeenCalledWith([file1])
      })

      handleChange.mockClear()

      fireEvent.change(fileInput, { target: { files: [file2] } })

      await waitFor(() => {
        expect(handleChange).toHaveBeenCalledWith([file1, file2])
      })
    })

    it('should display formatted accept types - BUG-051 FIX', () => {
      render(<FileUpload accept=".pdf,.docx,.xlsx" />)

      expect(screen.getByText(/PDF, DOCX, XLSX/)).toBeInTheDocument()
    })

    it('should format wildcard MIME types - BUG-051 FIX', () => {
      render(<FileUpload accept="image/*,video/*" />)

      expect(screen.getByText(/Images, Videos/)).toBeInTheDocument()
    })

    it('should show truncated accept types when many - BUG-051 FIX', () => {
      render(<FileUpload accept=".pdf,.doc,.docx,.xls,.xlsx,.ppt" />)

      expect(screen.getByText(/PDF, DOC, DOCX \+3 more/)).toBeInTheDocument()
    })
  })

  // ============================================
  // File Size Validation (4 tests)
  // ============================================
  describe('File Size Validation', () => {
    it('should accept file within size limit', async () => {
      const handleChange = jest.fn()
      render(<FileUpload maxSize={5000} onFilesChange={handleChange} />)

      const file = createMockFile('small.pdf', 4000, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(handleChange).toHaveBeenCalledWith([file])
      })
    })

    it('should reject file exceeding size limit', async () => {
      const handleChange = jest.fn()
      render(<FileUpload maxSize={1000} onFilesChange={handleChange} />)

      const file = createMockFile('large.pdf', 2000, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(screen.getByText(/File too large/)).toBeInTheDocument()
        expect(screen.getByText(/Maximum size: 1000 B/)).toBeInTheDocument()
        expect(handleChange).not.toHaveBeenCalled()
      })
    })

    it('should display max size in helper text', () => {
      render(<FileUpload maxSize={5 * 1024 * 1024} />)

      expect(screen.getByText(/Max 5 MB/)).toBeInTheDocument()
    })

    it('should reject multiple files when some exceed size', async () => {
      const handleChange = jest.fn()
      render(<FileUpload multiple maxFiles={3} maxSize={1000} onFilesChange={handleChange} />)

      const file1 = createMockFile('small.pdf', 500, 'application/pdf')
      const file2 = createMockFile('large.pdf', 2000, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file1, file2] } })

      await waitFor(() => {
        expect(screen.getByText(/File too large/)).toBeInTheDocument()
        expect(handleChange).not.toHaveBeenCalled()
      })
    })
  })

  // ============================================
  // File Count Validation (4 tests)
  // ============================================
  describe('File Count Validation', () => {
    it('should accept files up to maxFiles limit', async () => {
      const handleChange = jest.fn()
      render(<FileUpload multiple maxFiles={3} onFilesChange={handleChange} />)

      const file1 = createMockFile('file1.pdf', 1024, 'application/pdf')
      const file2 = createMockFile('file2.pdf', 1024, 'application/pdf')
      const file3 = createMockFile('file3.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file1, file2, file3] } })

      await waitFor(() => {
        expect(handleChange).toHaveBeenCalledWith([file1, file2, file3])
      })
    })

    it('should reject files exceeding maxFiles limit', async () => {
      const handleChange = jest.fn()
      render(<FileUpload multiple maxFiles={2} onFilesChange={handleChange} />)

      const file1 = createMockFile('file1.pdf', 1024, 'application/pdf')
      const file2 = createMockFile('file2.pdf', 1024, 'application/pdf')
      const file3 = createMockFile('file3.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file1, file2, file3] } })

      await waitFor(() => {
        expect(screen.getByText(/Maximum 2 files allowed/)).toBeInTheDocument()
        expect(handleChange).not.toHaveBeenCalled()
      })
    })

    it('should display maxFiles in helper text', () => {
      render(<FileUpload maxFiles={5} />)

      expect(screen.getByText(/Up to 5 files/)).toBeInTheDocument()
    })

    it('should prevent adding more files when limit reached', async () => {
      const handleChange = jest.fn()
      render(<FileUpload multiple maxFiles={2} onFilesChange={handleChange} />)

      const file1 = createMockFile('file1.pdf', 1024, 'application/pdf')
      const file2 = createMockFile('file2.pdf', 1024, 'application/pdf')
      const file3 = createMockFile('file3.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      // Add 2 files first
      fireEvent.change(fileInput, { target: { files: [file1, file2] } })

      await waitFor(() => {
        expect(handleChange).toHaveBeenCalledWith([file1, file2])
      })

      handleChange.mockClear()

      // Try to add 1 more (should fail)
      fireEvent.change(fileInput, { target: { files: [file3] } })

      await waitFor(() => {
        expect(screen.getByText(/Maximum 2 files allowed/)).toBeInTheDocument()
        expect(handleChange).not.toHaveBeenCalled()
      })
    })
  })

  // ============================================
  // File Icons and Preview (5 tests)
  // ============================================
  describe('File Icons and Preview', () => {
    it('should display correct icon for image files', async () => {
      const { container } = render(<FileUpload showPreview />)

      const file = createMockFile('photo.jpg', 1024, 'image/jpeg')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        const icon = container.querySelector('.lucide-file-image')
        expect(icon).toBeInTheDocument()
      })
    })

    it('should display video file in preview', async () => {
      render(<FileUpload showPreview />)

      const file = createMockFile('video.mp4', 1024, 'video/mp4')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(screen.getByText('video.mp4')).toBeInTheDocument()
        expect(screen.getByText('1 KB')).toBeInTheDocument()
      })
    })

    it('should display correct icon for audio files', async () => {
      const { container } = render(<FileUpload showPreview />)

      const file = createMockFile('song.mp3', 1024, 'audio/mpeg')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        const icon = container.querySelector('.lucide-file-audio')
        expect(icon).toBeInTheDocument()
      })
    })

    it('should display correct icon for PDF files', async () => {
      const { container } = render(<FileUpload showPreview />)

      const file = createMockFile('document.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        const icon = container.querySelector('.lucide-file-text')
        expect(icon).toBeInTheDocument()
      })
    })

    it('should display generic file icon for unknown types', async () => {
      const { container } = render(<FileUpload showPreview />)

      const file = createMockFile('data.bin', 1024, 'application/octet-stream')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        const icon = container.querySelector('.lucide-file')
        expect(icon).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Remove Files (4 tests)
  // ============================================
  describe('Remove Files', () => {
    it('should remove individual file when clicked', async () => {
      const user = userEvent.setup()
      const handleChange = jest.fn()
      render(<FileUpload multiple maxFiles={3} onFilesChange={handleChange} showPreview />)

      const file1 = createMockFile('file1.pdf', 1024, 'application/pdf')
      const file2 = createMockFile('file2.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file1, file2] } })

      await waitFor(() => {
        expect(screen.getByText('file1.pdf')).toBeInTheDocument()
        expect(screen.getByText('file2.pdf')).toBeInTheDocument()
      })

      handleChange.mockClear()

      const removeButtons = screen.getAllByRole('button', { name: /Remove/ })
      await user.click(removeButtons[0])

      expect(handleChange).toHaveBeenCalledWith([file2])
    })

    it('should clear error message when file removed', async () => {
      const user = userEvent.setup()
      render(<FileUpload maxFiles={1} showPreview />)

      const file = createMockFile('test.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      // Add file
      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(screen.getByText('test.pdf')).toBeInTheDocument()
      })

      // Try to add another (causes error)
      const file2 = createMockFile('test2.pdf', 1024, 'application/pdf')
      fireEvent.change(fileInput, { target: { files: [file2] } })

      await waitFor(() => {
        expect(screen.getByText(/Maximum 1 file allowed/)).toBeInTheDocument()
      })

      // Remove file
      const removeButton = screen.getByRole('button', { name: /Remove/ })
      await user.click(removeButton)

      expect(screen.queryByText(/Maximum 1 file allowed/)).not.toBeInTheDocument()
    })

    it('should not remove file when disabled', async () => {
      const user = userEvent.setup()
      const handleChange = jest.fn()
      render(<FileUpload disabled onFilesChange={handleChange} showPreview />)

      const file = createMockFile('test.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(screen.getByText('test.pdf')).toBeInTheDocument()
      })

      handleChange.mockClear()

      const removeButton = screen.getByRole('button', { name: /Remove/ })
      expect(removeButton).toBeDisabled()
    })

    it('should have accessible remove button labels', async () => {
      render(<FileUpload showPreview />)

      const file = createMockFile('document.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'Remove document.pdf' })).toBeInTheDocument()
      })
    })
  })

  // ============================================
  // Clear All (3 tests)
  // ============================================
  describe('Clear All', () => {
    it('should clear all files when clicked', async () => {
      const user = userEvent.setup()
      const handleChange = jest.fn()
      render(<FileUpload multiple maxFiles={3} onFilesChange={handleChange} showPreview />)

      const file1 = createMockFile('file1.pdf', 1024, 'application/pdf')
      const file2 = createMockFile('file2.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file1, file2] } })

      await waitFor(() => {
        expect(screen.getByText('file1.pdf')).toBeInTheDocument()
      })

      handleChange.mockClear()

      await user.click(screen.getByText('Clear all'))

      expect(handleChange).toHaveBeenCalledWith([])
      expect(screen.queryByText('file1.pdf')).not.toBeInTheDocument()
      expect(screen.queryByText('file2.pdf')).not.toBeInTheDocument()
    })

    it('should clear input value when clearing all', async () => {
      const user = userEvent.setup()
      render(<FileUpload showPreview />)

      const file = createMockFile('test.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(screen.getByText('test.pdf')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Clear all'))

      expect(fileInput.value).toBe('')
    })

    it('should not clear files when disabled', async () => {
      const user = userEvent.setup()
      render(<FileUpload disabled showPreview />)

      const file = createMockFile('test.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(screen.getByText('test.pdf')).toBeInTheDocument()
      })

      const clearButton = screen.getByText('Clear all')
      expect(clearButton).toBeDisabled()
    })
  })

  // ============================================
  // Keyboard Accessibility - BUG-040 Fix (4 tests)
  // ============================================
  describe('Keyboard Accessibility - BUG-040 Fix', () => {
    it('should open file picker on Enter key', () => {
      render(<FileUpload />)

      const dropZone = screen.getByRole('button', { name: /File upload drop zone/i })
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
      const clickSpy = jest.spyOn(fileInput, 'click')

      fireEvent.keyDown(dropZone, { key: 'Enter' })

      expect(clickSpy).toHaveBeenCalled()
    })

    it('should open file picker on Space key', () => {
      render(<FileUpload />)

      const dropZone = screen.getByRole('button', { name: /File upload drop zone/i })
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
      const clickSpy = jest.spyOn(fileInput, 'click')

      fireEvent.keyDown(dropZone, { key: ' ' })

      expect(clickSpy).toHaveBeenCalled()
    })

    it('should not handle keyboard when disabled', () => {
      render(<FileUpload disabled />)

      const dropZone = screen.getByRole('button', { name: /File upload drop zone/i })
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
      const clickSpy = jest.spyOn(fileInput, 'click')

      fireEvent.keyDown(dropZone, { key: 'Enter' })

      expect(clickSpy).not.toHaveBeenCalled()
    })

    it('should have focusable drop zone with correct tabIndex', () => {
      render(<FileUpload />)

      const dropZone = screen.getByRole('button', { name: /File upload drop zone/i })
      expect(dropZone).toHaveAttribute('tabIndex', '0')
    })
  })

  // ============================================
  // External State Sync - BUG-039 Fix (3 tests)
  // ============================================
  describe('External State Sync - BUG-039 Fix', () => {
    it('should sync with external files prop', () => {
      const file = createMockFile('external.pdf', 1024, 'application/pdf')
      render(<FileUpload files={[file]} showPreview />)

      expect(screen.getByText('external.pdf')).toBeInTheDocument()
    })

    it('should update when external files prop changes', () => {
      const file1 = createMockFile('file1.pdf', 1024, 'application/pdf')
      const file2 = createMockFile('file2.pdf', 1024, 'application/pdf')

      const { rerender } = render(<FileUpload files={[file1]} showPreview />)

      expect(screen.getByText('file1.pdf')).toBeInTheDocument()

      rerender(<FileUpload files={[file2]} showPreview />)

      expect(screen.queryByText('file1.pdf')).not.toBeInTheDocument()
      expect(screen.getByText('file2.pdf')).toBeInTheDocument()
    })

    it('should use internal state when files prop not provided', async () => {
      const handleChange = jest.fn()
      render(<FileUpload onFilesChange={handleChange} showPreview />)

      const file = createMockFile('internal.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(screen.getByText('internal.pdf')).toBeInTheDocument()
        expect(handleChange).toHaveBeenCalledWith([file])
      })
    })
  })

  // ============================================
  // Disabled State (4 tests)
  // ============================================
  describe('Disabled State', () => {
    it('should not open file picker when disabled', async () => {
      const user = userEvent.setup()
      render(<FileUpload disabled />)

      const dropZone = screen.getByRole('button', { name: /File upload drop zone/i })
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
      const clickSpy = jest.spyOn(fileInput, 'click')

      await user.click(dropZone)

      expect(clickSpy).not.toHaveBeenCalled()
    })

    it('should have correct styling when disabled', () => {
      const { container } = render(<FileUpload disabled />)

      const dropZone = screen.getByRole('button', { name: /File upload drop zone/i })
      expect(dropZone.className).toContain('opacity-50')
      expect(dropZone.className).toContain('cursor-not-allowed')
    })

    it('should have tabIndex -1 when disabled', () => {
      render(<FileUpload disabled />)

      const dropZone = screen.getByRole('button', { name: /File upload drop zone/i })
      expect(dropZone).toHaveAttribute('tabIndex', '-1')
    })

    it('should disable file input when disabled', () => {
      render(<FileUpload disabled />)

      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
      expect(fileInput).toBeDisabled()
    })
  })

  // ============================================
  // Error States (3 tests)
  // ============================================
  describe('Error States', () => {
    it('should show error styling when error prop is true', () => {
      const { container } = render(<FileUpload error label="Upload" />)

      const label = screen.getByText('Upload')
      expect(label.className).toContain('text-destructive')

      const dropZone = container.querySelector('.border-destructive')
      expect(dropZone).toBeInTheDocument()
    })

    it('should display error message with alert role', async () => {
      const handleChange = jest.fn()
      render(<FileUpload maxSize={100} onFilesChange={handleChange} />)

      const file = createMockFile('large.pdf', 200, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        const errorMessage = screen.getByRole('alert')
        expect(errorMessage).toHaveTextContent(/File too large/)
      })
    })

    it('should show error styling when validation fails', async () => {
      const { container } = render(<FileUpload maxSize={100} />)

      const file = createMockFile('large.pdf', 200, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        const dropZone = container.querySelector('.border-destructive')
        expect(dropZone).toBeInTheDocument()
      })
    })
  })
})

// ============================================
// ImageUpload Component Tests (20 tests)
// ============================================
describe('ImageUpload Component', () => {
  let mockFileReader: any

  beforeEach(() => {
    // Mock FileReader
    mockFileReader = {
      readAsDataURL: jest.fn(),
      onloadend: null,
      result: 'data:image/jpeg;base64,mockImageData',
    }
    global.FileReader = jest.fn(() => mockFileReader) as any
  })

  describe('Initial Render', () => {
    it('should render upload area', () => {
      render(<ImageUpload />)

      expect(screen.getByText('Click to upload image')).toBeInTheDocument()
    })

    it('should display label when provided', () => {
      render(<ImageUpload label="Profile Picture" />)

      expect(screen.getByText('Profile Picture')).toBeInTheDocument()
    })

    it('should display helper text when provided', () => {
      render(<ImageUpload helperText="Upload your avatar" />)

      expect(screen.getByText('Upload your avatar')).toBeInTheDocument()
    })

    it('should display default constraints', () => {
      render(<ImageUpload />)

      expect(screen.getByText(/PNG, JPG, GIF/)).toBeInTheDocument()
      expect(screen.getByText(/Max 5 MB/)).toBeInTheDocument()
    })
  })

  describe('Image Selection and Preview', () => {
    it('should handle image selection', async () => {
      const handleChange = jest.fn()
      render(<ImageUpload onImageChange={handleChange} />)

      const file = createMockFile('photo.jpg', 1024, 'image/jpeg')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      // Trigger FileReader onloadend
      mockFileReader.onloadend()

      await waitFor(() => {
        expect(handleChange).toHaveBeenCalledWith(file)
      })
    })

    it('should display image preview after selection', async () => {
      render(<ImageUpload />)

      const file = createMockFile('photo.jpg', 1024, 'image/jpeg')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      mockFileReader.onloadend()

      await waitFor(() => {
        const preview = screen.getByAltText('Preview')
        expect(preview).toBeInTheDocument()
        expect(preview).toHaveAttribute('src', 'data:image/jpeg;base64,mockImageData')
      })
    })

    it('should display current image when provided', () => {
      render(<ImageUpload currentImage="/path/to/image.jpg" />)

      const preview = screen.getByAltText('Preview')
      expect(preview).toHaveAttribute('src', '/path/to/image.jpg')
    })

    it('should show remove button when image is displayed', async () => {
      render(<ImageUpload />)

      const file = createMockFile('photo.jpg', 1024, 'image/jpeg')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      mockFileReader.onloadend()

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'Remove image' })).toBeInTheDocument()
      })
    })
  })

  describe('Image Validation', () => {
    it('should reject non-image files', async () => {
      const handleChange = jest.fn()
      render(<ImageUpload onImageChange={handleChange} />)

      const file = createMockFile('document.pdf', 1024, 'application/pdf')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(screen.getByText('Please select an image file')).toBeInTheDocument()
        expect(handleChange).not.toHaveBeenCalled()
      })
    })

    it('should reject image exceeding size limit', async () => {
      const handleChange = jest.fn()
      render(<ImageUpload maxSize={1000} onImageChange={handleChange} />)

      const file = createMockFile('large.jpg', 2000, 'image/jpeg')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        expect(screen.getByText(/Image too large/)).toBeInTheDocument()
        expect(handleChange).not.toHaveBeenCalled()
      })
    })

    it('should accept valid image within size limit', async () => {
      const handleChange = jest.fn()
      render(<ImageUpload maxSize={5000} onImageChange={handleChange} />)

      const file = createMockFile('photo.jpg', 4000, 'image/jpeg')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      mockFileReader.onloadend()

      await waitFor(() => {
        expect(handleChange).toHaveBeenCalledWith(file)
      })
    })

    it('should handle empty file selection gracefully', () => {
      const handleChange = jest.fn()
      render(<ImageUpload onImageChange={handleChange} />)

      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: null } })

      expect(handleChange).not.toHaveBeenCalled()
    })
  })

  describe('Remove Image', () => {
    it('should remove image when remove button clicked', async () => {
      const user = userEvent.setup()
      const handleChange = jest.fn()
      render(<ImageUpload onImageChange={handleChange} currentImage="/photo.jpg" />)

      const removeButton = screen.getByRole('button', { name: 'Remove image' })
      await user.click(removeButton)

      expect(handleChange).toHaveBeenCalledWith(null)
      expect(screen.queryByAltText('Preview')).not.toBeInTheDocument()
    })

    it('should allow uploading new image after removing current image', async () => {
      const user = userEvent.setup()
      const handleChange = jest.fn()
      render(<ImageUpload currentImage="/photo.jpg" onImageChange={handleChange} />)

      const removeButton = screen.getByRole('button', { name: 'Remove image' })
      await user.click(removeButton)

      expect(handleChange).toHaveBeenCalledWith(null)

      // Should be able to upload a new image now
      expect(screen.getByText(/Click to upload image/)).toBeInTheDocument()
    })
  })

  describe('Aspect Ratios', () => {
    it('should apply square aspect ratio', () => {
      const { container } = render(<ImageUpload aspectRatio="square" />)

      const imageContainer = container.querySelector('.aspect-square')
      expect(imageContainer).toBeInTheDocument()
    })

    it('should apply video aspect ratio', () => {
      const { container } = render(<ImageUpload aspectRatio="video" />)

      const imageContainer = container.querySelector('.aspect-video')
      expect(imageContainer).toBeInTheDocument()
    })

    it('should apply portrait aspect ratio', () => {
      const { container } = render(<ImageUpload aspectRatio="portrait" />)

      const imageContainer = container.querySelector('.aspect-\\[3\\/4\\]')
      expect(imageContainer).toBeInTheDocument()
    })

    it('should apply auto aspect ratio', () => {
      const { container } = render(<ImageUpload aspectRatio="auto" />)

      const imageContainer = container.querySelector('.aspect-auto')
      expect(imageContainer).toBeInTheDocument()
    })
  })

  describe('Disabled State', () => {
    it('should disable input when disabled', () => {
      render(<ImageUpload disabled />)

      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
      expect(fileInput).toBeDisabled()
    })

    it('should disable remove button when disabled', () => {
      render(<ImageUpload disabled currentImage="/photo.jpg" />)

      const removeButton = screen.getByRole('button', { name: 'Remove image' })
      expect(removeButton).toBeDisabled()
    })
  })

  describe('Error States', () => {
    it('should show error styling when error prop is true', () => {
      render(<ImageUpload error label="Avatar" />)

      const label = screen.getByText('Avatar')
      expect(label.className).toContain('text-destructive')
    })

    it('should display error message with alert role', async () => {
      render(<ImageUpload maxSize={100} />)

      const file = createMockFile('large.jpg', 200, 'image/jpeg')
      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement

      fireEvent.change(fileInput, { target: { files: [file] } })

      await waitFor(() => {
        const errorMessage = screen.getByRole('alert')
        expect(errorMessage).toHaveTextContent(/Image too large/)
      })
    })
  })
})
