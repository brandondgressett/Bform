/**
 * HTML sanitization utilities for safely rendering report HTML
 */

export interface SanitizeOptions {
  allowedTags?: string[];
  allowedAttributes?: Record<string, string[]>;
  allowedClasses?: string[];
  allowedStyles?: string[];
  transformTags?: Record<string, string>;
}

const DEFAULT_ALLOWED_TAGS = [
  'html', 'head', 'title', 'style', 'body',
  'table', 'thead', 'tbody', 'tfoot', 'tr', 'td', 'th',
  'div', 'span', 'p', 'br', 'hr',
  'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
  'strong', 'b', 'em', 'i', 'u',
  'font', 'center',
  'ul', 'ol', 'li',
  'details', 'summary', 'pre'
];

const DEFAULT_ALLOWED_ATTRIBUTES: Record<string, string[]> = {
  '*': ['class', 'style', 'id'],
  'table': ['width', 'cellpadding', 'cellspacing', 'border', 'bgcolor'],
  'tr': ['bgcolor'],
  'td': ['width', 'height', 'colspan', 'rowspan', 'align', 'valign', 'bgcolor'],
  'th': ['width', 'height', 'colspan', 'rowspan', 'align', 'valign', 'bgcolor'],
  'font': ['face', 'size', 'color'],
  'div': ['align'],
  'p': ['align'],
  'span': ['align']
};

const DEFAULT_ALLOWED_STYLES = [
  'color', 'background-color', 'bgcolor',
  'font-family', 'font-size', 'font-weight', 'font-style',
  'text-align', 'vertical-align',
  'width', 'height', 'min-width', 'max-width', 'min-height', 'max-height',
  'border', 'border-style', 'border-width', 'border-color', 'border-collapse',
  'margin', 'padding',
  'filter', 'display'
];

const DEFAULT_ALLOWED_CLASSES = [
  'TableStyle', 'TitleStyle', 'SectionHeader', 'DetailHeader', 
  'DetailData', 'ColumnHeaderStyle', 'bf-report-enhanced'
];

/**
 * Sanitize HTML content for safe rendering
 * Note: In production, you should use a proper HTML sanitization library like DOMPurify
 */
export function sanitizeReportHtml(html: string, options: SanitizeOptions = {}): string {
  const {
    allowedTags = DEFAULT_ALLOWED_TAGS,
    allowedAttributes = DEFAULT_ALLOWED_ATTRIBUTES,
    allowedClasses = DEFAULT_ALLOWED_CLASSES,
    allowedStyles = DEFAULT_ALLOWED_STYLES,
    transformTags = {}
  } = options;

  // This is a simplified sanitizer for demonstration
  // In production, use DOMPurify or similar library
  let sanitized = html;

  // Remove script tags and event handlers
  sanitized = sanitized.replace(/<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>/gi, '');
  sanitized = sanitized.replace(/on\w+\s*=\s*["'][^"']*["']/gi, '');
  sanitized = sanitized.replace(/javascript:/gi, '');

  return sanitized;
}

/**
 * Extract CSS from HTML report
 */
export function extractStylesFromHtml(html: string): {
  styles: string;
  htmlWithoutStyles: string;
} {
  const styleRegex = /<style[^>]*>([\s\S]*?)<\/style>/gi;
  const styles: string[] = [];
  let match;

  while ((match = styleRegex.exec(html)) !== null) {
    styles.push(match[1]);
  }

  const htmlWithoutStyles = html.replace(styleRegex, '');

  return {
    styles: styles.join('\n'),
    htmlWithoutStyles
  };
}

/**
 * Convert old HTML attributes to CSS classes
 */
export function modernizeHtmlAttributes(html: string): string {
  // Convert bgcolor to background-color style
  html = html.replace(/bgcolor=['"]?([^'">\s]+)['"]?/gi, 'style="background-color: $1"');
  
  // Convert align attribute to text-align style
  html = html.replace(/align=['"]?([^'">\s]+)['"]?/gi, 'style="text-align: $1"');
  
  // Convert valign attribute to vertical-align style
  html = html.replace(/valign=['"]?([^'">\s]+)['"]?/gi, 'style="vertical-align: $1"');

  return html;
}

/**
 * Parse HTML table data for potential AG-Grid conversion
 */
export interface TableData {
  headers: string[];
  rows: Record<string, any>[];
  hasTotal: boolean;
  totalRow?: Record<string, any>;
}

export function extractTableData(tableHtml: string): TableData | null {
  // This is a simplified parser - in production, use a proper HTML parser
  const parser = new DOMParser();
  const doc = parser.parseFromString(tableHtml, 'text/html');
  const table = doc.querySelector('table');
  
  if (!table) return null;

  const headers: string[] = [];
  const rows: Record<string, any>[] = [];
  let hasTotal = false;
  let totalRow: Record<string, any> | undefined;

  // Extract headers
  const headerCells = table.querySelectorAll('tr:first-child td, tr:first-child th');
  headerCells.forEach(cell => {
    headers.push(cell.textContent?.trim() || '');
  });

  // Extract rows
  const dataRows = table.querySelectorAll('tr:not(:first-child)');
  dataRows.forEach((row, rowIndex) => {
    const rowData: Record<string, any> = {};
    const cells = row.querySelectorAll('td');
    
    cells.forEach((cell, cellIndex) => {
      const text = cell.textContent?.trim() || '';
      const header = headers[cellIndex] || `col_${cellIndex}`;
      
      // Check if this is a total row
      if (text.toLowerCase().includes('total:')) {
        hasTotal = true;
        if (rowIndex === dataRows.length - 1) {
          totalRow = rowData;
        }
      }
      
      rowData[header] = text;
    });
    
    if (!totalRow || rowData !== totalRow) {
      rows.push(rowData);
    }
  });

  return {
    headers,
    rows,
    hasTotal,
    totalRow
  };
}