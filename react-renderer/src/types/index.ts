/**
 * Main types export file for @bformdomain/react-renderer
 */

// Common types
export * from './common';
export * from './layout';

// Entity types
export * from './worksets';
export * from './workitems';
export * from './forms';
export * from './tables';

// Additional entity types
export interface ReportTemplate {
  domainName: string;
  name: string;
  title?: string;
  description?: string;
  descendingOrder: number;
  isVisibleToUsers: boolean;
  tags: string[];
  reportType: ReportType;
  templateContent: Record<string, any>;
  chartSpecs: ChartSpec[];
  iconClass?: string;
}

export interface ReportInstance extends IAppEntity {
  entityType: 'ReportInstance';
  templateName: string;
  reportData: Record<string, any>;
  generatedDate: string;
  parameters: Record<string, any>;
}

export enum ReportType {
  Table = 'Table',
  Chart = 'Chart',
  Dashboard = 'Dashboard',
  Document = 'Document'
}

export interface ChartSpec {
  type: ChartType;
  title?: string;
  dataSource: string;
  xAxis?: string;
  yAxis?: string | string[];
  options: Record<string, any>;
}

export enum ChartType {
  Line = 'line',
  Bar = 'bar',
  Pie = 'pie',
  Area = 'area',
  Scatter = 'scatter'
}

export interface KPITemplate {
  domainName: string;
  name: string;
  title?: string;
  description?: string;
  descendingOrder: number;
  isVisibleToUsers: boolean;
  tags: string[];
  computeType: KPIComputeType;
  computeStage: KPIComputeStage;
  source: KPISource;
  iconClass?: string;
}

export interface KPIInstance extends IAppEntity {
  entityType: 'KPIInstance';
  templateName: string;
  currentValue: number;
  previousValue?: number;
  trend: KPITrend;
  lastComputed: string;
  samples: KPISample[];
  signals: KPISignal[];
}

export enum KPIComputeType {
  Count = 'Count',
  Sum = 'Sum',
  Average = 'Average',
  Percentage = 'Percentage',
  Ratio = 'Ratio'
}

export enum KPIComputeStage {
  RealTime = 'RealTime',
  Hourly = 'Hourly',
  Daily = 'Daily',
  Weekly = 'Weekly',
  Monthly = 'Monthly'
}

export interface KPISource {
  sourceType: string;
  connectionString?: string;
  query: string;
  parameters: Record<string, any>;
}

export enum KPITrend {
  Up = 'Up',
  Down = 'Down',
  Stable = 'Stable',
  Unknown = 'Unknown'
}

export interface KPISample {
  timestamp: string;
  value: number;
  tags: string[];
}

export interface KPISignal {
  id: string;
  type: KPISignalType;
  stage: KPISignalStage;
  threshold: number;
  message: string;
  isActive: boolean;
}

export enum KPISignalType {
  Warning = 'Warning',
  Critical = 'Critical',
  Information = 'Information'
}

export enum KPISignalStage {
  PreCompute = 'PreCompute',
  PostCompute = 'PostCompute',
  Threshold = 'Threshold'
}

export interface Comment extends IAppEntity {
  entityType: 'Comment';
  text: string;
  content?: string; // Alias for text
  userId: string;
  userName?: string;
  authorId?: string; // Alias for userId
  authorName?: string; // Alias for userName
  parentId?: string;
  parentCommentId?: string; // Alias for parentId
  threadId?: string;
  isEdited?: boolean;
  isDeleted?: boolean;
  editedDate?: string;
  replies?: Comment[];
}

export interface ManagedFileInstance extends IAppEntity {
  entityType: 'ManagedFile';
  fileName: string;
  originalFileName: string;
  mimeType: string;
  fileSize: number;
  storagePath: string;
  downloadUrl?: string;
  thumbnailUrl?: string;
  uploadedBy: string;
  uploadDate: Date | string;
  description?: string;
  fileExtension?: string;
}

// Alias for component compatibility
export type FileEntity = ManagedFileInstance;

export interface HtmlInstance extends IAppEntity {
  entityType: 'HtmlInstance';
  content: string;
  html?: string; // Alias for content
  title?: string;
  description?: string;
  templateName: string;
  isPublished: boolean;
}

// Alias for component compatibility
export type HtmlEntity = HtmlInstance;

export interface KpiTemplate {
  domainName: string;
  name: string;
  displayName: string;
  description?: string;
  category?: string;
  unitOfMeasure?: string;
  formatString?: string;
  targetValue?: number;
  positiveDirection?: 'up' | 'down';
  calculateMethod: 'sum' | 'average' | 'count' | 'min' | 'max';
  dataSource: string;
  groupBy?: string;
  filterExpression?: string;
}

export interface KpiInstance extends IAppEntity {
  entityType: 'KpiInstance';
  templateId: string;
  templateName: string;
  name: string;
  description?: string;
  category?: string;
  unitOfMeasure?: string;
  formatString?: string;
  targetValue?: number;
  positiveDirection?: 'up' | 'down';
  rowData?: KpiRowData[];
}

export interface KpiRowData {
  date: Date | string;
  value: number;
  label?: string;
}

export interface Notification extends IAppEntity {
  entityType: 'Notification';
  recipientId: string;
  title: string;
  body: string;
  message?: string; // Alias for body
  priority?: 'low' | 'medium' | 'high';
  channelType: NotificationChannelType;
  notificationType?: NotificationType;
  isRead: boolean;
  readDate?: string;
  actionUrl?: string;
  actionText?: string;
  expirationDate?: string;
  actions?: NotificationAction[];
  metadata?: Record<string, any>;
}

export interface NotificationAction {
  id: string;
  label: string;
  url?: string;
  action?: string;
}

export enum NotificationType {
  Information = 'Information',
  Warning = 'Warning',
  Error = 'Error',
  Success = 'Success'
}

export enum NotificationChannelType {
  Email = 'email',
  SMS = 'sms',
  Web = 'web',
  InApp = 'inapp'
}

// Import common interfaces for re-export
import { IAppEntity } from './common';