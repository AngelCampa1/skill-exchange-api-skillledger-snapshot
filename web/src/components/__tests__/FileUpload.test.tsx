import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import '@testing-library/jest-dom';
import FileUpload from '../workspace/FileUpload';
import type { DropzoneOptions, FileRejection, DropEvent } from 'react-dropzone';

// Mock react-dropzone
jest.mock('react-dropzone', () => ({
  useDropzone: jest.fn()
}));

// Mock fetch
global.fetch = jest.fn();
const mockFetch = fetch as jest.MockedFunction<typeof fetch>;

// Mock localStorage
const mockLocalStorage = {
  getItem: jest.fn(),
  setItem: jest.fn(),
  removeItem: jest.fn(),
  clear: jest.fn(),
};
Object.defineProperty(window, 'localStorage', {
  value: mockLocalStorage,
});

// Mock XMLHttpRequest for upload progress
const mockXHR = {
  open: jest.fn(),
  send: jest.fn(),
  setRequestHeader: jest.fn(),
  addEventListener: jest.fn(),
  abort: jest.fn(),  // BUG-HIGH-002 FIX: Add abort method for cleanup tests
  upload: {
    addEventListener: jest.fn()
  },
  status: 200,
  responseText: '{"id": "test-file-id", "name": "test.pdf", "url": "/files/test.pdf"}'
};

Object.defineProperty(window, 'XMLHttpRequest', {
  writable: true,
  value: jest.fn().mockImplementation(() => mockXHR)
});

describe('FileUpload', () => {
  const mockProps = {
    workspaceId: 'test-workspace-id',
    onUploadComplete: jest.fn(),
    onError: jest.fn()
  };

  beforeEach(() => {
    jest.clearAllMocks();
    mockLocalStorage.getItem.mockReturnValue('mock-token');
    
    // Mock useDropzone implementation
    const { useDropzone } = require('react-dropzone');
    useDropzone.mockReturnValue({
      getRootProps: () => ({
        onClick: jest.fn()
      }),
      getInputProps: () => ({}),
      isDragActive: false,
      fileRejections: []
    });
  });

  it('renders upload zone correctly', () => {
    render(<FileUpload {...mockProps} />);
    
    expect(screen.getByText(/drag & drop files here/i)).toBeInTheDocument();
    expect(screen.getByText(/browse/i)).toBeInTheDocument();
    expect(screen.getByText(/Max 10 files, 50MB each/i)).toBeInTheDocument();
  });

  it('shows drag active state', () => {
    const { useDropzone } = require('react-dropzone');
    useDropzone.mockReturnValue({
      getRootProps: () => ({}),
      getInputProps: () => ({}),
      isDragActive: true,
      fileRejections: []
    });

    render(<FileUpload {...mockProps} />);
    
    expect(screen.getByText(/drop files here/i)).toBeInTheDocument();
  });

  it('displays file rejections', () => {
    const { useDropzone } = require('react-dropzone');
    useDropzone.mockReturnValue({
      getRootProps: () => ({}),
      getInputProps: () => ({}),
      isDragActive: false,
      fileRejections: [
        {
          file: { name: 'test.exe' },
          errors: [{ message: 'File type not accepted' }]
        }
      ]
    });

    render(<FileUpload {...mockProps} />);
    
    expect(screen.getByText(/some files were rejected/i)).toBeInTheDocument();
    expect(screen.getByText(/test\.exe/i)).toBeInTheDocument();
    expect(screen.getByText(/file type not accepted/i)).toBeInTheDocument();
  });

  it('handles custom props correctly', () => {
    render(
      <FileUpload 
        {...mockProps}
        maxFiles={5}
        maxSizeMB={25}
        acceptedFileTypes={['.pdf', '.doc']}
      />
    );
    
    expect(screen.getByText(/Max 5 files, 25MB each/i)).toBeInTheDocument();
  });

  it('formats file size correctly', () => {
    const { useDropzone } = require('react-dropzone');
    // BUG-LOW-002 FIX: Match actual onDrop signature with 3 parameters
    let onDropCallback: ((files: File[], rejections: FileRejection[], event: DropEvent) => void) | undefined;
    
    // BUG-LOW-002 FIX: Use proper DropzoneOptions type instead of 'any'
    useDropzone.mockImplementation((options: DropzoneOptions) => {
      onDropCallback = options.onDrop;
      return {
        getRootProps: () => ({}),
        getInputProps: () => ({}),
        isDragActive: false,
        fileRejections: []
      };
    });

    render(<FileUpload {...mockProps} />);

    // Simulate file drop
    const mockFile = new File(['content'], 'test.pdf', { type: 'application/pdf' });
    Object.defineProperty(mockFile, 'size', { value: 1024 * 1024 }); // 1MB

    // Trigger onDrop
    if (onDropCallback) {
      act(() => {
        onDropCallback!([mockFile], [], {} as DropEvent);
      });
    }

    // The component should show file size as "1 MB"
    waitFor(() => {
      expect(screen.getByText(/1 MB/)).toBeInTheDocument();
    });
  });

  it('calls onUploadComplete when upload succeeds', async () => {
    const { useDropzone } = require('react-dropzone');
    // BUG-LOW-002 FIX: Match actual onDrop signature with 3 parameters
    let onDropCallback: ((files: File[], rejections: FileRejection[], event: DropEvent) => void) | undefined;
    
    // BUG-LOW-002 FIX: Use proper DropzoneOptions type instead of 'any'
    useDropzone.mockImplementation((options: DropzoneOptions) => {
      onDropCallback = options.onDrop;
      return {
        getRootProps: () => ({}),
        getInputProps: () => ({}),
        isDragActive: false,
        fileRejections: []
      };
    });

    render(<FileUpload {...mockProps} />);

    // Mock successful XHR response
    mockXHR.addEventListener.mockImplementation((event, callback) => {
      if (event === 'load') {
        // Use immediate callback instead of setTimeout to avoid act() warnings
        callback();
      }
    });

    const mockFile = new File(['content'], 'test.pdf', { type: 'application/pdf' });
    
    if (onDropCallback) {
      act(() => {
        onDropCallback!([mockFile], [], {} as DropEvent);
      });
    }

    await waitFor(() => {
      expect(mockProps.onUploadComplete).toHaveBeenCalledWith([
        expect.objectContaining({
          id: 'test-file-id',
          name: 'test.pdf'
        })
      ]);
    });
  });

  it('calls onError when upload fails', async () => {
    const { useDropzone } = require('react-dropzone');
    // BUG-LOW-002 FIX: Match actual onDrop signature with 3 parameters
    let onDropCallback: ((files: File[], rejections: FileRejection[], event: DropEvent) => void) | undefined;
    
    // BUG-LOW-002 FIX: Use proper DropzoneOptions type instead of 'any'
    useDropzone.mockImplementation((options: DropzoneOptions) => {
      onDropCallback = options.onDrop;
      return {
        getRootProps: () => ({}),
        getInputProps: () => ({}),
        isDragActive: false,
        fileRejections: []
      };
    });

    render(<FileUpload {...mockProps} />);

    // Mock failed XHR response
    mockXHR.status = 400;
    mockXHR.responseText = '{"message": "Upload failed"}';
    mockXHR.addEventListener.mockImplementation((event, callback) => {
      if (event === 'load') {
        // Use immediate callback instead of setTimeout to avoid act() warnings
        callback();
      }
    });

    const mockFile = new File(['content'], 'test.pdf', { type: 'application/pdf' });
    
    if (onDropCallback) {
      act(() => {
        onDropCallback!([mockFile], [], {} as DropEvent);
      });
    }

    await waitFor(() => {
      // BUG-MED-006: Error messages now include file name for better debugging
      expect(mockProps.onError).toHaveBeenCalledWith('Failed to upload "test.pdf": Upload failed');
    });
  });

  it('prevents upload when files exceed max count', () => {
    const { useDropzone } = require('react-dropzone');
    // BUG-LOW-002 FIX: Match actual onDrop signature with 3 parameters
    let onDropCallback: ((files: File[], rejections: FileRejection[], event: DropEvent) => void) | undefined;
    
    // BUG-LOW-002 FIX: Use proper DropzoneOptions type instead of 'any'
    useDropzone.mockImplementation((options: DropzoneOptions) => {
      onDropCallback = options.onDrop;
      return {
        getRootProps: () => ({}),
        getInputProps: () => ({}),
        isDragActive: false,
        fileRejections: []
      };
    });

    render(<FileUpload {...mockProps} maxFiles={2} />);

    const files = [
      new File(['content1'], 'test1.pdf'),
      new File(['content2'], 'test2.pdf'),
      new File(['content3'], 'test3.pdf')
    ];
    
    if (onDropCallback) {
      onDropCallback(files, [], {} as DropEvent);
    }

    expect(mockProps.onError).toHaveBeenCalledWith('Maximum 2 files allowed');
  });

  it('prevents upload when files exceed size limit', () => {
    const { useDropzone } = require('react-dropzone');
    // BUG-LOW-002 FIX: Match actual onDrop signature with 3 parameters
    let onDropCallback: ((files: File[], rejections: FileRejection[], event: DropEvent) => void) | undefined;
    
    // BUG-LOW-002 FIX: Use proper DropzoneOptions type instead of 'any'
    useDropzone.mockImplementation((options: DropzoneOptions) => {
      onDropCallback = options.onDrop;
      return {
        getRootProps: () => ({}),
        getInputProps: () => ({}),
        isDragActive: false,
        fileRejections: []
      };
    });

    render(<FileUpload {...mockProps} maxSizeMB={1} />);

    const mockFile = new File(['content'], 'large.pdf', { type: 'application/pdf' });
    // Mock file size to be larger than 1MB
    Object.defineProperty(mockFile, 'size', { value: 2 * 1024 * 1024 });
    
    if (onDropCallback) {
      act(() => {
        onDropCallback!([mockFile], [], {} as DropEvent);
      });
    }

    expect(mockProps.onError).toHaveBeenCalledWith('Files too large. Maximum size: 1MB');
  });

  // Note: Auth token tests removed - application now uses cookie-based authentication instead of Bearer tokens
});