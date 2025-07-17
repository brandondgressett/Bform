import React, { useMemo } from 'react';
import classNames from 'classnames';
import { HtmlInstance } from '../../types';
import { sanitizeReportHtml, modernizeHtmlAttributes } from '../../utils/htmlSanitizer';

interface HtmlEntityRendererProps {
  entity: HtmlInstance;
  allowUnsafeContent?: boolean;
  enableBootstrapClasses?: boolean;
  className?: string;
  style?: React.CSSProperties;
  onContentClick?: (event: React.MouseEvent) => void;
}

/**
 * Renders BFormDomain HtmlEntity content
 */
export const HtmlEntityRenderer: React.FC<HtmlEntityRendererProps> = ({
  entity,
  allowUnsafeContent = false,
  enableBootstrapClasses = true,
  className,
  style,
  onContentClick
}) => {
  const processedContent = useMemo(() => {
    if (!entity.content) {
      return '';
    }

    let content = entity.content;
    
    // Sanitize unless explicitly allowed
    if (!allowUnsafeContent) {
      content = sanitizeReportHtml(content);
    }
    
    // Modernize HTML attributes
    content = modernizeHtmlAttributes(content);
    
    // Add Bootstrap classes if enabled
    if (enableBootstrapClasses) {
      // Add table classes
      content = content.replace(/<table(?![^>]*class)/gi, '<table class="table"');
      content = content.replace(/<table([^>]*class=['"])([^'"]*)/gi, '<table$1table $2');
      
      // Add button classes
      content = content.replace(/<button(?![^>]*class)/gi, '<button class="btn btn-primary"');
      content = content.replace(/<a([^>]*href[^>]*)(?![^>]*class)/gi, '<a$1 class="link-primary"');
      
      // Add alert classes for common patterns
      content = content.replace(/<div([^>]*class=['"]?)error/gi, '<div$1alert alert-danger error');
      content = content.replace(/<div([^>]*class=['"]?)warning/gi, '<div$1alert alert-warning warning');
      content = content.replace(/<div([^>]*class=['"]?)success/gi, '<div$1alert alert-success success');
      content = content.replace(/<div([^>]*class=['"]?)info/gi, '<div$1alert alert-info info');
    }
    
    return content;
  }, [entity.content, allowUnsafeContent, enableBootstrapClasses]);

  const containerClasses = classNames(
    'bf-html-entity',
    {
      'bf-html-published': entity.isPublished,
      'bf-html-draft': !entity.isPublished
    },
    className
  );

  return (
    <div className={containerClasses} style={style}>
      {/* Header */}
      {(entity.title || entity.description) && (
        <div className="bf-html-header mb-3">
          {entity.title && <h3>{entity.title}</h3>}
          {entity.description && (
            <p className="text-muted">{entity.description}</p>
          )}
        </div>
      )}

      {/* Metadata */}
      <div className="bf-html-metadata mb-2">
        <small className="text-muted">
          {entity.templateName && (
            <span className="me-3">
              Template: <strong>{entity.templateName}</strong>
            </span>
          )}
          {!entity.isPublished && (
            <span className="badge bg-warning text-dark">Draft</span>
          )}
          {entity.tags?.length > 0 && (
            <span className="ms-2">
              {entity.tags.map(tag => (
                <span key={tag} className="badge bg-secondary ms-1">{tag}</span>
              ))}
            </span>
          )}
        </small>
      </div>

      {/* Content */}
      <div 
        className="bf-html-content"
        onClick={onContentClick}
        dangerouslySetInnerHTML={{ __html: processedContent }}
      />

      {/* Styles */}
      <style>{`
        .bf-html-entity {
          position: relative;
        }
        .bf-html-draft {
          border-left: 4px solid #ffc107;
          padding-left: 1rem;
        }
        .bf-html-content {
          line-height: 1.6;
        }
        .bf-html-content img {
          max-width: 100%;
          height: auto;
        }
        .bf-html-content table {
          margin: 1rem 0;
        }
        .bf-html-content pre {
          background-color: #f8f9fa;
          padding: 1rem;
          border-radius: 4px;
          overflow-x: auto;
        }
        .bf-html-content blockquote {
          border-left: 4px solid #dee2e6;
          padding-left: 1rem;
          margin: 1rem 0;
          font-style: italic;
        }
      `}</style>
    </div>
  );
};

HtmlEntityRenderer.displayName = 'HtmlEntityRenderer';