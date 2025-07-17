/**
 * WorkItem related types and interfaces
 */

import { IAppEntity, EntityReference } from './common';

export interface WorkItem extends IAppEntity {
  entityType: 'WorkItem';
  isListed: boolean;
  isVisible: boolean;
  userAssignee?: string;
  triageAssignee?: number;
  title: string;
  description?: string;
  status: number;
  priority: number;
  startUnresolved: string;
  eventHistory: WorkItemEventHistory[];
  bookmarks: WorkItemBookmark[];
  links: WorkItemLink[];
  sections: Section[];
}

export interface Section {
  templateId: number;
  entities: EntityReference[];
}

export interface SectionTemplate {
  id: number;
  descendingOrder: number;
  renderer: string;
  isCreateOnNew: boolean;
  isCreateOnDemand: boolean;
  isEntityList: boolean;
  entityTemplateName?: string;
  creationData?: Record<string, any>;
  newInstanceProcess?: ProcessInstanceCommand;
}

export interface ProcessInstanceCommand {
  processName: string;
  parameters: Record<string, any>;
}

export interface WorkItemEventHistory {
  id: string;
  eventType: string;
  timestamp: string;
  userId?: string;
  userName?: string;
  description: string;
  details?: Record<string, any>;
}

export interface WorkItemBookmark {
  id: string;
  title: string;
  url: string;
  description?: string;
  createdDate: string;
  userId: string;
}

export interface WorkItemLink {
  id: string;
  linkType: string;
  targetEntityType: string;
  targetEntityId: string;
  title: string;
  description?: string;
  createdDate: string;
}

export interface WorkItemViewModel {
  id: string;
  title: string;
  description?: string;
  status: number;
  statusText: string;
  priority: number;
  priorityText: string;
  assigneeName?: string;
  tags: string[];
  templateName: string;
  isListed: boolean;
  isVisible: boolean;
  createdDate: string;
  updatedDate: string;
  sections: SectionViewModel[];
  eventHistory: WorkItemEventHistoryViewModel[];
  bookmarks: WorkItemBookmarkViewModel[];
  links: WorkItemLink[];
}

export interface SectionViewModel {
  templateId: number;
  title: string;
  renderer: string;
  entities: EntityReference[];
  isCreateOnDemand: boolean;
  isEntityList: boolean;
  entityTemplateName?: string;
}

export interface WorkItemEventHistoryViewModel {
  id: string;
  eventType: string;
  eventTypeDisplay: string;
  timestamp: string;
  userDisplayName: string;
  description: string;
  details?: Record<string, any>;
}

export interface WorkItemBookmarkViewModel {
  id: string;
  title: string;
  url: string;
  description?: string;
  createdDate: string;
  userDisplayName: string;
}

export interface WorkItemSummaryViewModel {
  id: string;
  title: string;
  description?: string;
  status: number;
  statusText: string;
  priority: number;
  priorityText: string;
  assigneeName?: string;
  tags: string[];
  templateName: string;
  createdDate: string;
  updatedDate: string;
}

export interface WorkItemTemplate {
  domainName: string;
  name: string;
  title?: string;
  description?: string;
  descendingOrder: number;
  isVisibleToUsers: boolean;
  tags: string[];
  sections: SectionTemplate[];
  statusTemplates: StatusTemplate[];
  priorityTemplates: PriorityTemplate[];
  triageTemplates: TriageTemplate[];
}

export interface StatusTemplate {
  id: number;
  name: string;
  displayName: string;
  statusType: StatusType;
  iconClass?: string;
  color?: string;
}

export enum StatusType {
  Open = 'Open',
  InProgress = 'InProgress',
  Resolved = 'Resolved',
  Closed = 'Closed'
}

export interface PriorityTemplate {
  id: number;
  name: string;
  displayName: string;
  numericValue: number;
  iconClass?: string;
  color?: string;
}

export interface TriageTemplate {
  id: number;
  name: string;
  displayName: string;
  iconClass?: string;
  color?: string;
}