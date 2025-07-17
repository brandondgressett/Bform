/**
 * Formatting utilities for displaying data in renderers
 */

/**
 * Format a numeric value with an optional format string
 * @param value The value to format
 * @param formatString Optional format string (e.g., "$#,##0.00")
 * @returns Formatted string
 */
export function formatValue(value: number, formatString?: string): string {
  if (!formatString) {
    return value.toString();
  }

  // Handle common format patterns
  if (formatString.includes('$')) {
    // Currency formatting
    const formatter = new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: formatString.split('.')[1]?.length || 0,
      maximumFractionDigits: formatString.split('.')[1]?.length || 0
    });
    return formatter.format(value);
  }

  if (formatString.includes('%')) {
    // Percentage formatting
    const formatter = new Intl.NumberFormat('en-US', {
      style: 'percent',
      minimumFractionDigits: formatString.split('.')[1]?.length || 0,
      maximumFractionDigits: formatString.split('.')[1]?.length || 0
    });
    return formatter.format(value / 100);
  }

  if (formatString.includes(',')) {
    // Number with thousands separator
    const decimalPlaces = formatString.split('.')[1]?.length || 0;
    const formatter = new Intl.NumberFormat('en-US', {
      minimumFractionDigits: decimalPlaces,
      maximumFractionDigits: decimalPlaces
    });
    return formatter.format(value);
  }

  // Default number formatting
  return value.toLocaleString();
}

/**
 * Format a date/time value as relative time (e.g., "2 hours ago")
 * @param date The date to format
 * @returns Relative time string
 */
export function formatRelativeTime(date: Date | string): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  const now = new Date();
  const seconds = Math.floor((now.getTime() - dateObj.getTime()) / 1000);

  const intervals = [
    { label: 'year', seconds: 31536000 },
    { label: 'month', seconds: 2592000 },
    { label: 'day', seconds: 86400 },
    { label: 'hour', seconds: 3600 },
    { label: 'minute', seconds: 60 },
    { label: 'second', seconds: 1 }
  ];

  for (const interval of intervals) {
    const count = Math.floor(seconds / interval.seconds);
    if (count >= 1) {
      return count === 1 
        ? `${count} ${interval.label} ago`
        : `${count} ${interval.label}s ago`;
    }
  }

  return 'just now';
}

/**
 * Format file size in human-readable format
 * @param bytes File size in bytes
 * @returns Formatted file size string
 */
export function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 Bytes';

  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));

  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

/**
 * Format a date according to locale
 * @param date The date to format
 * @param options Intl.DateTimeFormatOptions
 * @returns Formatted date string
 */
export function formatDate(
  date: Date | string, 
  options?: Intl.DateTimeFormatOptions
): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  return dateObj.toLocaleDateString('en-US', options);
}

/**
 * Format a date and time according to locale
 * @param date The date to format
 * @param options Intl.DateTimeFormatOptions
 * @returns Formatted date/time string
 */
export function formatDateTime(
  date: Date | string,
  options?: Intl.DateTimeFormatOptions
): string {
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  return dateObj.toLocaleString('en-US', options);
}

/**
 * Truncate text with ellipsis
 * @param text Text to truncate
 * @param maxLength Maximum length
 * @returns Truncated text
 */
export function truncateText(text: string, maxLength: number): string {
  if (text.length <= maxLength) return text;
  return text.substring(0, maxLength - 3) + '...';
}

/**
 * Format duration in human-readable format
 * @param seconds Duration in seconds
 * @returns Formatted duration string
 */
export function formatDuration(seconds: number): string {
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const secs = Math.floor(seconds % 60);

  const parts = [];
  if (hours > 0) parts.push(`${hours}h`);
  if (minutes > 0) parts.push(`${minutes}m`);
  if (secs > 0 || parts.length === 0) parts.push(`${secs}s`);

  return parts.join(' ');
}

/**
 * Format a percentage value
 * @param value The value to format (0-100)
 * @param decimals Number of decimal places
 * @returns Formatted percentage string
 */
export function formatPercentage(value: number, decimals: number = 1): string {
  return `${value.toFixed(decimals)}%`;
}