/**
 * Common types and interfaces for BFormDomain entities
 */

export interface IAppEntity {
  id: string;
  version: number;
  tenantId?: string;
  entityType: string;
  template: string;
  createdDate: string;
  updatedDate: string;
  creator?: string;
  lastModifier?: string;
  hostWorkSet?: string;
  hostWorkItem?: string;
  tags: string[];
  attachedSchedules: string[];
}

export interface ITaggable {
  tags: string[];
  tagged(...anyTags: string[]): boolean;
}

export interface IDataModel {
  id: string;
  version: number;
}

export interface EntityReference {
  entityType: string;
  entityId: string;
  template?: string;
  vm?: boolean;
  queryParameters?: Record<string, string>;
}

export interface SatelliteJson {
  content?: Record<string, any>;
  isLoaded: boolean;
}

export type EntityType = 
  | 'WorkSet'
  | 'WorkItem' 
  | 'FormInstance'
  | 'FormTemplate'
  | 'TableTemplate'
  | 'ReportInstance'
  | 'ReportTemplate'
  | 'KPIInstance'
  | 'KPITemplate'
  | 'HtmlInstance'
  | 'HtmlTemplate'
  | 'Comment'
  | 'ManagedFile'
  | 'Notification';

export interface RenderContext {
  tenantId?: string;
  userId?: string;
  userRoles?: string[];
  permissions?: string[];
  theme?: string;
  locale?: string;
}

export interface ComponentProps {
  className?: string;
  style?: React.CSSProperties;
  'data-testid'?: string;
}