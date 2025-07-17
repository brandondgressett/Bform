import React, { useState } from 'react';
import classNames from 'classnames';
import { FileEntity } from '../../types';
import { formatFileSize, formatRelativeTime } from '../../utils/formatting';

interface FileRendererProps {
  file: FileEntity;
  variant?: 'card' | 'list' | 'grid';
  showPreview?: boolean;
  showActions?: boolean;
  onDownload?: (file: FileEntity) => void;
  onDelete?: (file: FileEntity) => Promise<void>;
  onPreview?: (file: FileEntity) => void;
  className?: string;
}

/**
 * Renders a single file with metadata and actions
 */
export const FileRenderer: React.FC<FileRendererProps> = ({
  file,
  variant = 'list',
  showPreview = true,
  showActions = true,
  onDownload,
  onDelete,
  onPreview,
  className
}) => {
  const [isDeleting, setIsDeleting] = useState(false);

  const getFileIcon = () => {
    const ext = file.fileExtension?.toLowerCase() || '';
    
    // Map extensions to Bootstrap Icons
    const iconMap: Record<string, string> = {
      pdf: 'bi-file-pdf',
      doc: 'bi-file-word',
      docx: 'bi-file-word',
      xls: 'bi-file-excel',
      xlsx: 'bi-file-excel',
      ppt: 'bi-file-ppt',
      pptx: 'bi-file-ppt',
      jpg: 'bi-file-image',
      jpeg: 'bi-file-image',
      png: 'bi-file-image',
      gif: 'bi-file-image',
      svg: 'bi-file-image',
      mp4: 'bi-file-play',
      avi: 'bi-file-play',
      mov: 'bi-file-play',
      mp3: 'bi-file-music',
      wav: 'bi-file-music',
      zip: 'bi-file-zip',
      rar: 'bi-file-zip',
      txt: 'bi-file-text',
      csv: 'bi-file-text',
      json: 'bi-file-code',
      xml: 'bi-file-code',
      js: 'bi-file-code',
      ts: 'bi-file-code',
      html: 'bi-file-code',
      css: 'bi-file-code'
    };

    return iconMap[ext] || 'bi-file-earmark';
  };

  const isImageFile = () => {
    const imageExts = ['jpg', 'jpeg', 'png', 'gif', 'svg', 'webp'];
    return imageExts.includes(file.fileExtension?.toLowerCase() || '');
  };

  const handleDelete = async () => {
    if (onDelete && window.confirm(`Delete "${file.fileName}"?`)) {
      setIsDeleting(true);
      try {
        await onDelete(file);
      } finally {
        setIsDeleting(false);
      }
    }
  };

  const containerClasses = classNames(
    'bf-file-renderer',
    `bf-file-${variant}`,
    {
      'bf-file-deleting': isDeleting
    },
    className
  );

  // Card variant
  if (variant === 'card') {
    return (
      <div className={containerClasses}>
        <div className="card h-100">
          {/* Preview */}
          {showPreview && isImageFile() && file.thumbnailUrl && (
            <div 
              className="card-img-top bf-file-preview"
              style={{ 
                backgroundImage: `url(${file.thumbnailUrl})`,
                height: '150px',
                backgroundSize: 'cover',
                backgroundPosition: 'center'
              }}
              onClick={() => onPreview?.(file)}
            />
          )}
          
          <div className="card-body">
            <div className="text-center mb-3">
              <i className={`${getFileIcon()} fs-1`}></i>
            </div>
            
            <h6 className="card-title text-truncate" title={file.fileName}>
              {file.fileName}
            </h6>
            
            <p className="card-text">
              <small className="text-muted">
                {formatFileSize(file.fileSize)}<br/>
                {formatRelativeTime(file.uploadDate)}
              </small>
            </p>
            
            {showActions && (
              <div className="d-flex gap-2">
                {onDownload && (
                  <button 
                    className="btn btn-sm btn-primary flex-fill"
                    onClick={() => onDownload(file)}
                  >
                    <i className="bi bi-download"></i>
                  </button>
                )}
                {onPreview && (
                  <button 
                    className="btn btn-sm btn-secondary flex-fill"
                    onClick={() => onPreview(file)}
                  >
                    <i className="bi bi-eye"></i>
                  </button>
                )}
                {onDelete && (
                  <button 
                    className="btn btn-sm btn-danger"
                    onClick={handleDelete}
                    disabled={isDeleting}
                  >
                    <i className="bi bi-trash"></i>
                  </button>
                )}
              </div>
            )}
          </div>
        </div>
      </div>
    );
  }

  // Grid variant
  if (variant === 'grid') {
    return (
      <div className={containerClasses}>
        <div 
          className="bf-file-grid-item p-3 text-center"
          onClick={() => onPreview?.(file)}
          style={{ cursor: onPreview ? 'pointer' : 'default' }}
        >
          {showPreview && isImageFile() && file.thumbnailUrl ? (
            <div 
              className="bf-file-thumbnail mb-2"
              style={{ 
                backgroundImage: `url(${file.thumbnailUrl})`,
                width: '100px',
                height: '100px',
                backgroundSize: 'cover',
                backgroundPosition: 'center',
                margin: '0 auto',
                borderRadius: '0.25rem'
              }}
            />
          ) : (
            <i className={`${getFileIcon()} fs-1 mb-2 d-block`}></i>
          )}
          
          <small className="d-block text-truncate" title={file.fileName}>
            {file.fileName}
          </small>
          <small className="text-muted">
            {formatFileSize(file.fileSize)}
          </small>
        </div>
      </div>
    );
  }

  // List variant (default)
  return (
    <div className={containerClasses}>
      <div className="d-flex align-items-center">
        {/* Icon */}
        <div className="me-3">
          {showPreview && isImageFile() && file.thumbnailUrl ? (
            <div 
              className="bf-file-thumbnail"
              style={{ 
                backgroundImage: `url(${file.thumbnailUrl})`,
                width: '40px',
                height: '40px',
                backgroundSize: 'cover',
                backgroundPosition: 'center',
                borderRadius: '0.25rem'
              }}
            />
          ) : (
            <i className={`${getFileIcon()} fs-3`}></i>
          )}
        </div>

        {/* Info */}
        <div className="flex-grow-1 min-width-0">
          <div className="fw-medium text-truncate" title={file.fileName}>
            {file.fileName}
          </div>
          <small className="text-muted">
            {formatFileSize(file.fileSize)} • {formatRelativeTime(file.uploadDate)}
            {file.uploadedBy && ` • ${file.uploadedBy}`}
          </small>
          {file.description && (
            <div className="text-muted small text-truncate" title={file.description}>
              {file.description}
            </div>
          )}
        </div>

        {/* Actions */}
        {showActions && (
          <div className="ms-3 d-flex gap-1">
            {onDownload && (
              <button 
                className="btn btn-sm btn-outline-primary"
                onClick={() => onDownload(file)}
                title="Download"
              >
                <i className="bi bi-download"></i>
              </button>
            )}
            {onPreview && (
              <button 
                className="btn btn-sm btn-outline-secondary"
                onClick={() => onPreview(file)}
                title="Preview"
              >
                <i className="bi bi-eye"></i>
              </button>
            )}
            {onDelete && (
              <button 
                className="btn btn-sm btn-outline-danger"
                onClick={handleDelete}
                disabled={isDeleting}
                title="Delete"
              >
                <i className="bi bi-trash"></i>
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

FileRenderer.displayName = 'FileRenderer';

// File List Component
interface FileListProps {
  files: FileEntity[];
  variant?: 'card' | 'list' | 'grid';
  showUpload?: boolean;
  acceptedFileTypes?: string;
  maxFileSize?: number;
  onUpload?: (files: File[]) => Promise<void>;
  onDownload?: (file: FileEntity) => void;
  onDelete?: (file: FileEntity) => Promise<void>;
  onPreview?: (file: FileEntity) => void;
  className?: string;
}

/**
 * Renders a list of files with upload capability
 */
export const FileList: React.FC<FileListProps> = ({
  files,
  variant = 'list',
  showUpload = true,
  acceptedFileTypes,
  maxFileSize,
  onUpload,
  onDownload,
  onDelete,
  onPreview,
  className
}) => {
  const [isDragging, setIsDragging] = useState(false);
  const [isUploading, setIsUploading] = useState(false);

  const handleFileSelect = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = Array.from(event.target.files || []);
    if (selectedFiles.length > 0 && onUpload) {
      setIsUploading(true);
      try {
        await onUpload(selectedFiles);
      } finally {
        setIsUploading(false);
      }
    }
  };

  const handleDrop = async (event: React.DragEvent) => {
    event.preventDefault();
    setIsDragging(false);

    const droppedFiles = Array.from(event.dataTransfer.files);
    if (droppedFiles.length > 0 && onUpload) {
      setIsUploading(true);
      try {
        await onUpload(droppedFiles);
      } finally {
        setIsUploading(false);
      }
    }
  };

  const handleDragOver = (event: React.DragEvent) => {
    event.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = () => {
    setIsDragging(false);
  };

  const getListClasses = () => {
    if (variant === 'card') return 'row g-3';
    if (variant === 'grid') return 'd-flex flex-wrap gap-3';
    return 'list-group';
  };

  const getItemClasses = () => {
    if (variant === 'card') return 'col-12 col-sm-6 col-md-4 col-lg-3';
    if (variant === 'grid') return '';
    return 'list-group-item';
  };

  return (
    <div className={classNames('bf-file-list', className)}>
      {/* Upload Area */}
      {showUpload && onUpload && (
        <div 
          className={classNames('bf-file-upload mb-4', {
            'bf-file-upload-dragging': isDragging
          })}
          onDrop={handleDrop}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
        >
          <div className="text-center p-4 border-2 border-dashed rounded">
            <i className="bi bi-cloud-upload fs-1 mb-3 d-block"></i>
            <p className="mb-2">
              Drag and drop files here or{' '}
              <label className="btn btn-link p-0 text-decoration-none">
                browse
                <input
                  type="file"
                  multiple
                  accept={acceptedFileTypes}
                  onChange={handleFileSelect}
                  style={{ display: 'none' }}
                  disabled={isUploading}
                />
              </label>
            </p>
            {acceptedFileTypes && (
              <small className="text-muted d-block">
                Accepted file types: {acceptedFileTypes}
              </small>
            )}
            {maxFileSize && (
              <small className="text-muted d-block">
                Max file size: {formatFileSize(maxFileSize)}
              </small>
            )}
            {isUploading && (
              <div className="spinner-border spinner-border-sm mt-2" role="status">
                <span className="visually-hidden">Uploading...</span>
              </div>
            )}
          </div>
        </div>
      )}

      {/* File List */}
      {files.length === 0 ? (
        <p className="text-muted text-center">No files uploaded yet</p>
      ) : (
        <div className={getListClasses()}>
          {files.map(file => (
            <div key={file.id} className={getItemClasses()}>
              <FileRenderer
                file={file}
                variant={variant}
                onDownload={onDownload}
                onDelete={onDelete}
                onPreview={onPreview}
              />
            </div>
          ))}
        </div>
      )}

      {/* Styles */}
      <style>{`
        .bf-file-upload-dragging .border-dashed {
          border-color: #0d6efd !important;
          background-color: rgba(13, 110, 253, 0.05);
        }
        .bf-file-deleting {
          opacity: 0.5;
          pointer-events: none;
        }
        .bf-file-grid-item:hover {
          background-color: #f8f9fa;
          border-radius: 0.25rem;
        }
        .border-dashed {
          border-style: dashed !important;
        }
      `}</style>
    </div>
  );
};

FileList.displayName = 'FileList';