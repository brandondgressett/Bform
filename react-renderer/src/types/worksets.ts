/**
 * WorkSet related types and interfaces
 */

import { IAppEntity, EntityReference } from './common';
import { ViewRowDef } from './layout';

export interface WorkSet extends IAppEntity {
  entityType: 'WorkSet';
  title: string;
  description: string;
  projectOwner?: string;
  interactivityState: WorkSetInteractivityState;
  lockedDate: string;
}

export enum WorkSetInteractivityState {
  Open = 'Open',
  ReadOnly = 'ReadOnly',
  FixedContent = 'FixedContent',
  Hidden = 'Hidden'
}

export enum WorkSetHome {
  List = 'List',
  Menu = 'Menu'
}

export enum WorkSetManagement {
  UserManaged = 'UserManaged',
  RulesManaged = 'RulesManaged'
}

export interface WorkSetMenuItem {
  descendingOrder: number;
  title: string;
  iconClass?: string;
  isDefaultMenuItem: boolean;
  isVisible: boolean;
}

export interface CreatableWorkItem {
  templateName?: string;
  tags?: string[];
  userCreatable: boolean;
  createOnInitialization: boolean;
  title?: string;
}

export interface DashboardItemViewModel {
  descendingOrder: number;
  grouping?: string;
  entityRef: EntityReference;
  entityType: string;
  entity: Record<string, any>;
  tags: string[];
  metaTags: string[];
}

export interface WorkSetViewModel {
  title: string;
  description: string;
  ownerUserName: string;
  tags: string[];
  templateName: string;
  isVisibleToUsers: boolean;
  templateTags: string[];
  home: WorkSetHome;
  menuItem?: WorkSetMenuItem;
  notificationGroupTags: string[];
  interactivityState: WorkSetInteractivityState;
  management: WorkSetManagement;
  view: ViewRowDef[];
  isEveryoneAMember: boolean;
  workItemCreationTemplates: CreatableWorkItem[];
  dashboardData: DashboardItemViewModel[];
}

export interface WorkSetTemplate {
  domainName: string;
  name: string;
  comments?: string;
  title?: string;
  descendingOrder: number;
  isVisibleToUsers: boolean;
  satelliteData?: Record<string, string>;
  tags: string[];
  home: WorkSetHome;
  menuItem?: WorkSetMenuItem;
  notificationGroupTags?: string[];
  startingInteractivityState: WorkSetInteractivityState;
  management: WorkSetManagement;
  isUserCreatable: boolean;
  dashboardBuildDeferralSeconds?: number;
  view?: ViewRowDef[];
  isEveryoneAMember: boolean;
  workItemCreationTemplates?: CreatableWorkItem[];
}

export interface WorkSetSummaryViewModel {
  id: string;
  title: string;
  description: string;
  templateName: string;
  tags: string[];
  interactivityState: WorkSetInteractivityState;
  createdDate: string;
  updatedDate: string;
}

export interface WorkSetMemberViewModel {
  userId: string;
  userName: string;
  joinedDate: string;
  isOwner: boolean;
}