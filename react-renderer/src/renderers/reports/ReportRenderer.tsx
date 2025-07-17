import React, { useMemo, useState } from 'react';
import classNames from 'classnames';
import { ReportInstance } from '../../types';
import { 
  sanitizeReportHtml, 
  extractStylesFromHtml, 
  modernizeHtmlAttributes,
  extractTableData 
} from '../../utils/htmlSanitizer';

interface ReportRendererProps {
  report: ReportInstance;
  enhanceMode?: 'none' | 'minimal' | 'full';
  enableTableInteractivity?: boolean;
  enableChartInteractivity?: boolean;
  className?: string;
  style?: React.CSSProperties;
  onSectionToggle?: (sectionIndex: number, isExpanded: boolean) => void;
  onTableDataExtracted?: (tableData: any) => void;
}

/**
 * Renders BFormDomain HTML reports with optional React enhancements
 */
export const ReportRenderer: React.FC<ReportRendererProps> = ({
  report,
  enhanceMode = 'minimal',
  enableTableInteractivity = false,
  enableChartInteractivity = false,
  className,
  style,
  onSectionToggle,
  onTableDataExtracted
}) => {
  const [expandedSections, setExpandedSections] = useState<Set<number>>(new Set());
  const [showRawHtml, setShowRawHtml] = useState(false);

  // Process the HTML content
  const processedContent = useMemo(() => {
    if (!report.html) {
      return { styles: '', htmlContent: '', enhancedHtml: '' };
    }

    // Sanitize the HTML
    const sanitizedHtml = sanitizeReportHtml(report.html);
    
    // Extract styles
    const { styles, htmlWithoutStyles } = extractStylesFromHtml(sanitizedHtml);
    
    // Modernize HTML attributes
    const modernizedHtml = modernizeHtmlAttributes(htmlWithoutStyles);

    // Apply enhancements based on mode
    let enhancedHtml = modernizedHtml;
    
    if (enhanceMode !== 'none') {
      enhancedHtml = applyEnhancements(modernizedHtml, enhanceMode);
    }

    return {
      styles,
      htmlContent: sanitizedHtml,
      enhancedHtml
    };
  }, [report.html, enhanceMode]);

  // Apply React enhancements to the HTML
  const applyEnhancements = (html: string, mode: 'minimal' | 'full'): string => {
    let enhanced = html;

    // Add Bootstrap classes
    enhanced = enhanced.replace(/<table\s/gi, '<table class="table table-striped table-hover" ');
    
    if (mode === 'full') {
      // Make sections collapsible
      enhanced = enhanced.replace(
        /<tr><td colspan=['"]?\d+['"]?\s+style="[^"]*font-weight:bold[^"]*"[^>]*>(.*?)<\/td><\/tr>/gi,
        (match, content, offset) => {
          const sectionIndex = offset;
          const isExpanded = expandedSections.has(sectionIndex);
          return `
            <tr class="bf-report-section-header" data-section-index="${sectionIndex}">
              <td colspan="100%" style="cursor: pointer;">
                <span class="bf-section-toggle">${isExpanded ? '▼' : '▶'}</span>
                ${content}
              </td>
            </tr>
          `;
        }
      );

      // Add responsive wrapper to tables
      enhanced = enhanced.replace(
        /<table([^>]*)>/gi,
        '<div class="table-responsive"><table$1>'
      );
      enhanced = enhanced.replace(
        /<\/table>/gi,
        '</table></div>'
      );
    }

    return enhanced;
  };

  // Handle section toggle
  const handleSectionClick = (event: React.MouseEvent) => {
    const target = event.target as HTMLElement;
    const sectionHeader = target.closest('.bf-report-section-header');
    
    if (sectionHeader) {
      const sectionIndex = parseInt(sectionHeader.getAttribute('data-section-index') || '0');
      const newExpanded = new Set(expandedSections);
      
      if (newExpanded.has(sectionIndex)) {
        newExpanded.delete(sectionIndex);
      } else {
        newExpanded.add(sectionIndex);
      }
      
      setExpandedSections(newExpanded);
      onSectionToggle?.(sectionIndex, newExpanded.has(sectionIndex));
    }
  };

  // Extract table data if needed
  React.useEffect(() => {
    if (enableTableInteractivity && onTableDataExtracted && report.html) {
      const tableData = extractTableData(report.html);
      if (tableData) {
        onTableDataExtracted(tableData);
      }
    }
  }, [report.html, enableTableInteractivity, onTableDataExtracted]);

  const containerClasses = classNames(
    'bf-report-renderer',
    {
      'bf-report-enhanced': enhanceMode !== 'none',
      'bf-report-interactive': enhanceMode === 'full'
    },
    className
  );

  return (
    <div className={containerClasses} style={style}>
      {/* Report Header */}
      <div className="bf-report-header mb-3">
        <h2>{report.title || 'Report'}</h2>
        <div className="bf-report-metadata text-muted">
          <small>
            Generated: {new Date(report.createdDate).toLocaleString()}
            {report.tags?.length > 0 && (
              <span className="ms-3">
                Tags: {report.tags.map(tag => (
                  <span key={tag} className="badge bg-secondary ms-1">{tag}</span>
                ))}
              </span>
            )}
          </small>
        </div>
        
        {/* Enhancement Controls */}
        {enhanceMode !== 'none' && (
          <div className="bf-report-controls mt-2">
            <button 
              className="btn btn-sm btn-outline-secondary"
              onClick={() => setShowRawHtml(!showRawHtml)}
            >
              {showRawHtml ? 'Show Enhanced' : 'Show Original'}
            </button>
          </div>
        )}
      </div>

      {/* Styles */}
      {processedContent.styles && (
        <style dangerouslySetInnerHTML={{ __html: processedContent.styles }} />
      )}

      {/* Enhanced Styles for React features */}
      {enhanceMode !== 'none' && (
        <style>{`
          .bf-report-enhanced table {
            margin-bottom: 1rem;
          }
          .bf-report-enhanced .bf-report-section-header td {
            background-color: #f8f9fa;
            font-weight: bold;
            user-select: none;
          }
          .bf-report-enhanced .bf-report-section-header:hover td {
            background-color: #e9ecef;
          }
          .bf-section-toggle {
            display: inline-block;
            width: 20px;
            margin-right: 8px;
            transition: transform 0.2s;
          }
          .bf-report-interactive .table-responsive {
            margin-bottom: 1rem;
          }
        `}</style>
      )}

      {/* Report Content */}
      <div 
        className="bf-report-content"
        onClick={enhanceMode === 'full' ? handleSectionClick : undefined}
        dangerouslySetInnerHTML={{ 
          __html: showRawHtml ? processedContent.htmlContent : processedContent.enhancedHtml 
        }} 
      />

      {/* Optional Table Data Viewer */}
      {enableTableInteractivity && (
        <div className="bf-report-table-tools mt-4">
          <button 
            className="btn btn-primary"
            onClick={() => {
              const tableData = extractTableData(report.html);
              if (tableData) {
                console.log('Extracted table data:', tableData);
                onTableDataExtracted?.(tableData);
              }
            }}
          >
            Convert to Interactive Table
          </button>
        </div>
      )}
    </div>
  );
};

ReportRenderer.displayName = 'ReportRenderer';