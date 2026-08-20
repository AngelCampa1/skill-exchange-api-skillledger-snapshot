/**
 * Step4PhotoUpload.tsx Tests
 *
 * Tests for profile photo upload wizard step with security validations.
 * Coverage Target: 80%+
 */

import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import '@testing-library/jest-dom';
import Step4PhotoUpload from '../Step4PhotoUpload';
import { PhotoUpload } from '@/types/profile';

// Mock Next.js Image component
jest.mock('next/image', () => ({
  __esModule: true,
  default: ({ priority, ...props }: any) => {
    // eslint-disable-next-line @next/next/no-img-element, jsx-a11y/alt-text
    return <img {...props} />;
  },
}));

// Mock FileReader
class MockFileReader {
  onloadend: ((this: FileReader, ev: ProgressEvent<FileReader>) => any) | null = null;
  onload: ((this: FileReader, ev: ProgressEvent<FileReader>) => any) | null = null;
  onerror: ((this: FileReader, ev: ProgressEvent<FileReader>) => any) | null = null;
  result: string | ArrayBuffer | null = null;

  readAsDataURL(file: Blob) {
    setTimeout(() => {
      this.result = `data:image/png;base64,mock-${file.size}`;
      if (this.onloadend) {
        this.onloadend.call(this as unknown as FileReader, {} as ProgressEvent<FileReader>);
      }
    }, 0);
  }

  readAsArrayBuffer(blob: Blob) {
    setTimeout(() => {
      // Create mock array buffer with PNG magic bytes
      const arr = new Uint8Array([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00]);
      this.result = arr.buffer;
      if (this.onload) {
        const event = { target: { result: arr.buffer } } as ProgressEvent<FileReader>;
        this.onload.call(this as unknown as FileReader, event);
      }
    }, 0);
  }
}

global.FileReader = MockFileReader as any;

// Mock URL.createObjectURL and revokeObjectURL
const mockObjectURLs = new Set<string>();
global.URL.createObjectURL = jest.fn((blob: Blob) => {
  const url = `blob:mock-${Date.now()}-${blob.size}`;
  mockObjectURLs.add(url);
  return url;
});

global.URL.revokeObjectURL = jest.fn((url: string) => {
  mockObjectURLs.delete(url);
});

// Track created images for testing
let createdImages: HTMLImageElement[] = [];
const originalCreateElement = document.createElement.bind(document);
document.createElement = jest.fn((tagName: string, options?: any) => {
  const element = originalCreateElement(tagName, options);
  if (tagName === 'img') {
    createdImages.push(element as HTMLImageElement);
    // Auto-trigger onload for image elements
    setTimeout(() => {
      if (element instanceof HTMLImageElement && element.onload) {
        Object.defineProperty(element, 'width', { value: 800, writable: true });
        Object.defineProperty(element, 'height', { value: 800, writable: true });
        element.onload({} as Event);
      }
    }, 0);
  }
  return element;
}) as any;

describe('Step4PhotoUpload', () => {
  const mockOnUpdate = jest.fn();
  const mockOnNext = jest.fn();
  const mockOnBack = jest.fn();

  const defaultProps = {
    photo: {} as PhotoUpload,
    onUpdate: mockOnUpdate,
    onNext: mockOnNext,
    onBack: mockOnBack,
  };

  const createMockFile = (
    name: string = 'test.png',
    size: number = 1024,
    type: string = 'image/png'
  ): File => {
    const file = new File(['mock file content'], name, { type });
    Object.defineProperty(file, 'size', { value: size });
    return file;
  };

  beforeEach(() => {
    jest.clearAllMocks();
    createdImages = [];
    mockObjectURLs.clear();
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  describe('Rendering', () => {
    it('renders heading and description', () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      expect(screen.getByText('Profile Photo')).toBeInTheDocument();
      expect(screen.getByText(/Upload a profile picture/i)).toBeInTheDocument();
    });

    it('renders placeholder avatar when no photo is selected', () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      // SVG placeholder should be visible
      const placeholder = document.querySelector('svg.w-24');
      expect(placeholder).toBeInTheDocument();
    });

    it('renders existing photo preview from props', () => {
      const photoWithUrl = { avatarUrl: 'https://example.com/photo.jpg' };
      render(<Step4PhotoUpload {...defaultProps} photo={photoWithUrl} />);

      const image = screen.getByAltText('Profile preview');
      expect(image).toBeInTheDocument();
      expect(image).toHaveAttribute('src', expect.stringContaining('photo.jpg'));
    });

    it('renders "Choose Photo" button when no photo is selected', () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      expect(screen.getByText('Choose Photo')).toBeInTheDocument();
      expect(screen.queryByText('Change Photo')).not.toBeInTheDocument();
    });

    it('renders "Change Photo" and "Remove Photo" buttons when photo is selected', () => {
      const photoWithUrl = { avatarUrl: 'https://example.com/photo.jpg' };
      render(<Step4PhotoUpload {...defaultProps} photo={photoWithUrl} />);

      expect(screen.getByText('Change Photo')).toBeInTheDocument();
      expect(screen.getByText('Remove Photo')).toBeInTheDocument();
      expect(screen.queryByText('Choose Photo')).not.toBeInTheDocument();
    });

    it('renders info box with helpful message', () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      expect(screen.getByText(/A clear profile photo helps build trust/i)).toBeInTheDocument();
    });

    it('renders navigation buttons', () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      expect(screen.getByRole('button', { name: /back/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /next step/i })).toBeInTheDocument();
    });

    it('renders "Skip for now" button when no photo is selected', () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      expect(screen.getByRole('button', { name: /skip for now/i })).toBeInTheDocument();
    });

    it('does not render "Skip for now" button when photo is selected', () => {
      const photoWithUrl = { avatarUrl: 'https://example.com/photo.jpg' };
      render(<Step4PhotoUpload {...defaultProps} photo={photoWithUrl} />);

      expect(screen.queryByRole('button', { name: /skip for now/i })).not.toBeInTheDocument();
    });
  });

  describe('File Selection - Valid Files', () => {
    it('accepts valid PNG file', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('test.png', 1024, 'image/png');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByAltText('Profile preview')).toBeInTheDocument();
      });
    });

    it('accepts valid JPEG file with .jpg extension', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('test.jpg', 1024, 'image/jpeg');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByAltText('Profile preview')).toBeInTheDocument();
      });
    });

    it('accepts valid JPEG file with .jpeg extension', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('test.jpeg', 1024, 'image/jpeg');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByAltText('Profile preview')).toBeInTheDocument();
      });
    });

    it('accepts valid GIF file', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('test.gif', 1024, 'image/gif');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      // Mock GIF magic bytes
      const originalReadAsArrayBuffer = MockFileReader.prototype.readAsArrayBuffer;
      MockFileReader.prototype.readAsArrayBuffer = function(blob: Blob) {
        setTimeout(() => {
          const arr = new Uint8Array([0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
          this.result = arr.buffer;
          if (this.onload) {
            const event = { target: { result: arr.buffer } } as ProgressEvent<FileReader>;
            this.onload.call(this as unknown as FileReader, event);
          }
        }, 0);
      };

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByAltText('Profile preview')).toBeInTheDocument();
      });

      MockFileReader.prototype.readAsArrayBuffer = originalReadAsArrayBuffer;
    });

    it('creates preview URL for selected file', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('test.png', 1024, 'image/png');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        const image = screen.getByAltText('Profile preview');
        expect(image).toHaveAttribute('src', expect.stringContaining('data:image/png;base64,mock-1024'));
      });
    });
  });

  describe('File Validation - Extension', () => {
    it('rejects file with invalid extension', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('test.txt', 1024, 'text/plain');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByText(/Invalid file type/i)).toBeInTheDocument();
      });
    });

    it('rejects file with .exe extension', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('virus.exe', 1024, 'application/x-msdownload');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByText(/Invalid file type/i)).toBeInTheDocument();
      });
    });

    it('is case-insensitive for extensions', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('TEST.PNG', 1024, 'image/png');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByAltText('Profile preview')).toBeInTheDocument();
      });
    });
  });

  describe('File Validation - MIME Type', () => {
    it('rejects file with invalid MIME type despite valid extension', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('test.png', 1024, 'application/x-msdownload');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByText(/Invalid image format/i)).toBeInTheDocument();
      });
    });
  });

  describe('File Validation - Size', () => {
    it('rejects file larger than 5MB', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('large.png', 6 * 1024 * 1024, 'image/png');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByText(/Image size must be less than 5MB/i)).toBeInTheDocument();
      });
    });

    it('accepts file exactly 5MB', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('large.png', 5 * 1024 * 1024, 'image/png');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByAltText('Profile preview')).toBeInTheDocument();
      });
    });
  });

  describe('File Validation - Magic Bytes', () => {
    it('validates JPEG magic bytes (FF D8 FF)', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('test.jpg', 1024, 'image/jpeg');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      // Mock JPEG magic bytes
      const originalReadAsArrayBuffer = MockFileReader.prototype.readAsArrayBuffer;
      MockFileReader.prototype.readAsArrayBuffer = function(blob: Blob) {
        setTimeout(() => {
          const arr = new Uint8Array([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01]);
          this.result = arr.buffer;
          if (this.onload) {
            const event = { target: { result: arr.buffer } } as ProgressEvent<FileReader>;
            this.onload.call(this as unknown as FileReader, event);
          }
        }, 0);
      };

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByAltText('Profile preview')).toBeInTheDocument();
      });

      MockFileReader.prototype.readAsArrayBuffer = originalReadAsArrayBuffer;
    });

    it('rejects file with invalid magic bytes', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('fake.png', 1024, 'image/png');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      // Mock invalid magic bytes
      const originalReadAsArrayBuffer = MockFileReader.prototype.readAsArrayBuffer;
      MockFileReader.prototype.readAsArrayBuffer = function(blob: Blob) {
        setTimeout(() => {
          const arr = new Uint8Array([0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
          this.result = arr.buffer;
          if (this.onload) {
            const event = { target: { result: arr.buffer } } as ProgressEvent<FileReader>;
            this.onload.call(this as unknown as FileReader, event);
          }
        }, 0);
      };

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByText(/magic bytes check failed/i)).toBeInTheDocument();
      });

      MockFileReader.prototype.readAsArrayBuffer = originalReadAsArrayBuffer;
    });
  });

  describe('File Validation - Image Loading', () => {
    it('rejects file that cannot be loaded as image', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('corrupt.png', 1024, 'image/png');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      // Temporarily replace document.createElement
      const originalCreateElement = document.createElement;
      const mockImages: HTMLImageElement[] = [];

      document.createElement = jest.fn((tagName: string) => {
        const element = originalCreateElement.call(document, tagName);
        if (tagName === 'img') {
          mockImages.push(element as HTMLImageElement);
          // Trigger error immediately instead of using setTimeout
          queueMicrotask(() => {
            if (element instanceof HTMLImageElement && element.onerror) {
              element.onerror({} as ErrorEvent);
            }
          });
        }
        return element;
      }) as any;

      fireEvent.change(input, { target: { files: [file] } });

      await waitFor(() => {
        expect(screen.getByText(/cannot be loaded as an image/i)).toBeInTheDocument();
      }, { timeout: 2000 });

      document.createElement = originalCreateElement as any;
    });

    it('rejects image with dimensions exceeding 10000x10000', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('huge.png', 1024, 'image/png');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      // Override image dimensions
      const originalCreateElement = document.createElement;
      const mockImages: HTMLImageElement[] = [];

      document.createElement = jest.fn((tagName: string) => {
        const element = originalCreateElement.call(document, tagName);
        if (tagName === 'img') {
          mockImages.push(element as HTMLImageElement);
          // Set dimensions and trigger onload immediately
          queueMicrotask(() => {
            if (element instanceof HTMLImageElement && element.onload) {
              Object.defineProperty(element, 'width', { value: 15000, writable: true });
              Object.defineProperty(element, 'height', { value: 15000, writable: true });
              element.onload({} as Event);
            }
          });
        }
        return element;
      }) as any;

      fireEvent.change(input, { target: { files: [file] } });

      await waitFor(() => {
        expect(screen.getByText(/Image dimensions too large/i)).toBeInTheDocument();
      }, { timeout: 2000 });

      document.createElement = originalCreateElement as any;
    });

    it('revokes object URL after image loads successfully', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('test.png', 1024, 'image/png');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(global.URL.revokeObjectURL).toHaveBeenCalled();
      });
    });

    it('revokes object URL after image fails to load', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('corrupt.png', 1024, 'image/png');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      // Override to trigger error
      const originalCreateElement = document.createElement;
      document.createElement = jest.fn((tagName: string) => {
        const element = originalCreateElement.call(document, tagName);
        if (tagName === 'img') {
          setTimeout(() => {
            if (element instanceof HTMLImageElement && element.onerror) {
              element.onerror({} as ErrorEvent);
            }
          }, 0);
        }
        return element;
      }) as any;

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(global.URL.revokeObjectURL).toHaveBeenCalled();
      });

      document.createElement = originalCreateElement as any;
    });
  });

  describe('Photo Removal', () => {
    it('removes photo and clears preview when remove button is clicked', async () => {
      const photoWithUrl = { avatarUrl: 'https://example.com/photo.jpg' };
      render(<Step4PhotoUpload {...defaultProps} photo={photoWithUrl} />);

      const removeButton = screen.getByRole('button', { name: /remove photo/i });
      fireEvent.click(removeButton);

      await waitFor(() => {
        expect(screen.queryByAltText('Profile preview')).not.toBeInTheDocument();
        expect(screen.getByText('Choose Photo')).toBeInTheDocument();
      });
    });

    it('clears file input value on remove', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('test.png', 1024, 'image/png');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByAltText('Profile preview')).toBeInTheDocument();
      });

      // Now remove the photo
      const removeButton = screen.getByRole('button', { name: /remove photo/i });
      fireEvent.click(removeButton);

      await waitFor(() => {
        expect(input.value).toBe('');
      });
    });

    it('clears error message on remove', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      // Trigger an error first
      const file = createMockFile('test.txt', 1024, 'text/plain');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByText(/Invalid file type/i)).toBeInTheDocument();
      });

      // Now upload a valid file and remove it
      const validFile = createMockFile('test.png', 1024, 'image/png');
      await act(async () => {
        fireEvent.change(input, { target: { files: [validFile] } });
      });

      await waitFor(() => {
        expect(screen.getByAltText('Profile preview')).toBeInTheDocument();
      });

      const removeButton = screen.getByRole('button', { name: /remove photo/i });
      fireEvent.click(removeButton);

      expect(screen.queryByText(/Invalid file type/i)).not.toBeInTheDocument();
    });
  });

  describe('Navigation', () => {
    it('calls onBack when back button is clicked', () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      fireEvent.click(screen.getByRole('button', { name: /back/i }));

      expect(mockOnBack).toHaveBeenCalledTimes(1);
    });

    it('calls onUpdate and onNext when next button is clicked with no photo', () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      fireEvent.click(screen.getByRole('button', { name: /next step/i }));

      expect(mockOnUpdate).toHaveBeenCalledWith({
        avatarUrl: undefined,
        file: undefined,
      });
      expect(mockOnNext).toHaveBeenCalledTimes(1);
    });

    it('calls onUpdate and onNext when next button is clicked with photo', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('test.png', 1024, 'image/png');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByAltText('Profile preview')).toBeInTheDocument();
      });

      fireEvent.click(screen.getByRole('button', { name: /next step/i }));

      expect(mockOnUpdate).toHaveBeenCalledWith(
        expect.objectContaining({
          avatarUrl: expect.stringContaining('data:image/png'),
        })
      );
      expect(mockOnNext).toHaveBeenCalledTimes(1);
    });

    it('calls onUpdate and onNext when skip button is clicked', () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      fireEvent.click(screen.getByRole('button', { name: /skip for now/i }));

      expect(mockOnUpdate).toHaveBeenCalledWith({
        avatarUrl: undefined,
        file: undefined,
      });
      expect(mockOnNext).toHaveBeenCalledTimes(1);
    });
  });

  describe('Error Handling', () => {
    it('clears error after successful file upload', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      // Upload invalid file first
      const invalidFile = createMockFile('test.txt', 1024, 'text/plain');
      await act(async () => {
        fireEvent.change(input, { target: { files: [invalidFile] } });
      });

      await waitFor(() => {
        expect(screen.getByText(/Invalid file type/i)).toBeInTheDocument();
      });

      // Upload valid file
      const validFile = createMockFile('test.png', 1024, 'image/png');
      await act(async () => {
        fireEvent.change(input, { target: { files: [validFile] } });
      });

      await waitFor(() => {
        expect(screen.queryByText(/Invalid file type/i)).not.toBeInTheDocument();
      });
    });

    it('handles FileReader read error gracefully', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('test.png', 1024, 'image/png');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      // Mock FileReader error during readAsArrayBuffer
      const originalReadAsArrayBuffer = MockFileReader.prototype.readAsArrayBuffer;
      MockFileReader.prototype.readAsArrayBuffer = function(blob: Blob) {
        setTimeout(() => {
          if (this.onerror) {
            this.onerror.call(this as unknown as FileReader, {} as ProgressEvent<FileReader>);
          }
        }, 0);
      };

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByText(/Failed to read file/i)).toBeInTheDocument();
      });

      MockFileReader.prototype.readAsArrayBuffer = originalReadAsArrayBuffer;
    });

    it('handles non-Error objects in validation', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const file = createMockFile('test.png', 1024, 'image/png');
      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      // Mock validation to throw a non-Error object
      const originalReadAsArrayBuffer = MockFileReader.prototype.readAsArrayBuffer;
      MockFileReader.prototype.readAsArrayBuffer = function(blob: Blob) {
        setTimeout(() => {
          if (this.onerror) {
            this.onerror.call(this as unknown as FileReader, {} as ProgressEvent<FileReader>);
          }
        }, 0);
      };

      await act(async () => {
        fireEvent.change(input, { target: { files: [file] } });
      });

      await waitFor(() => {
        expect(screen.getByText(/Invalid image file|Failed to read file/i)).toBeInTheDocument();
      });

      MockFileReader.prototype.readAsArrayBuffer = originalReadAsArrayBuffer;
    });
  });

  describe('Edge Cases', () => {
    it('handles empty file input (no file selected)', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      await act(async () => {
        fireEvent.change(input, { target: { files: [] } });
      });

      // Should not show error or preview
      expect(screen.queryByAltText('Profile preview')).not.toBeInTheDocument();
      expect(screen.queryByText(/Invalid/i)).not.toBeInTheDocument();
    });

    it('handles null files array', async () => {
      render(<Step4PhotoUpload {...defaultProps} />);

      const input = document.querySelector('input[type="file"]') as HTMLInputElement;

      await act(async () => {
        fireEvent.change(input, { target: { files: null } });
      });

      // Should not crash
      expect(screen.queryByAltText('Profile preview')).not.toBeInTheDocument();
    });
  });
});
