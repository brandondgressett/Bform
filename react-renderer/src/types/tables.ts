/**
 * Table related types and interfaces
 */

import { SatelliteJson } from './common';

export interface TableTemplate {
  name: string;
  descendingOrder: number;
  domainName: string;
  satelliteData?: Record<string, string>;
  tags: string[];
  comment?: string;
  title?: string;
  description?: string;
  collectionName: string;
  collectionId?: string;
  isVisibleToUsers: boolean;
  isUserEditAllowed: boolean;
  isUserDeleteAllowed: boolean;
  isUserAddAllowed: boolean;
  displayMasterDetail: boolean;
  detailFormTemplate?: string;
  isDataGroomed: boolean;
  monthsRetained: number;
  daysRetained: number;
  hoursRetained: number;
  minutesRetained: number;
  iconClass?: string;
  columns: ColDef[];
  isPerWorkSet: boolean;
  isPerWorkItem: boolean;
  agGridColumnDefs: SatelliteJson;
}

export interface ColDef {
  field: string;
  headerName: string;
  width?: number;
  minWidth?: number;
  maxWidth?: number;
  hide?: boolean;
  sortable?: boolean;
  filter?: boolean | string;
  resizable?: boolean;
  pinned?: 'left' | 'right';
  lockPosition?: boolean;
  cellRenderer?: string;
  cellEditor?: string;
  valueGetter?: string;
  valueSetter?: string;
  valueFormatter?: string;
  cellClass?: string | string[];
  headerClass?: string | string[];
  suppressMenu?: boolean;
  suppressSorting?: boolean;
  suppressFilter?: boolean;
  type?: string;
  keyType?: KeyType;
  mapping?: Mapping;
}

export enum KeyType {
  String = 'String',
  Number = 'Number',
  Date = 'Date',
  Boolean = 'Boolean',
  Object = 'Object'
}

export interface Mapping {
  sourceField: string;
  targetType: KeyType;
  format?: string;
  defaultValue?: any;
}

export interface TableRowData {
  id: string;
  tenantId?: string;
  collectionName: string;
  data: Record<string, any>;
  createdDate: string;
  updatedDate: string;
  tags: string[];
}

export interface TableViewModel {
  templateName: string;
  title?: string;
  description?: string;
  tags: string[];
  collectionName: string;
  isUserEditAllowed: boolean;
  isUserDeleteAllowed: boolean;
  isUserAddAllowed: boolean;
  displayMasterDetail: boolean;
  detailFormTemplate?: string;
  columnDefs: ColDef[];
  agGridOptions: any; // AG-Grid options
  data: TableRowData[];
  totalRows: number;
  currentPage: number;
  pageSize: number;
}

export interface TableSummaryViewModel {
  templateName: string;
  title?: string;
  totalRows: number;
  lastUpdated: string;
  tags: string[];
}

export interface TableQueryCommand {
  collectionName: string;
  filters?: TableFilter[];
  sorting?: TableSort[];
  pagination?: TablePagination;
  grouping?: TableGrouping[];
  projection?: string[];
}

export interface TableFilter {
  field: string;
  operator: FilterOperator;
  value: any;
  logicalOperator?: LogicalOperator;
}

export enum FilterOperator {
  Equals = 'eq',
  NotEquals = 'ne',
  GreaterThan = 'gt',
  GreaterThanOrEquals = 'gte',
  LessThan = 'lt',
  LessThanOrEquals = 'lte',
  Contains = 'contains',
  StartsWith = 'startsWith',
  EndsWith = 'endsWith',
  In = 'in',
  NotIn = 'notIn'
}

export enum LogicalOperator {
  And = 'and',
  Or = 'or'
}

export interface TableSort {
  field: string;
  direction: SortDirection;
}

export enum SortDirection {
  Ascending = 'asc',
  Descending = 'desc'
}

export interface TablePagination {
  page: number;
  pageSize: number;
  totalRows?: number;
}

export interface TableGrouping {
  field: string;
  aggregations?: TableAggregation[];
}

export interface TableAggregation {
  field: string;
  function: AggregationFunction;
  alias?: string;
}

export enum AggregationFunction {
  Count = 'count',
  Sum = 'sum',
  Average = 'avg',
  Min = 'min',
  Max = 'max',
  First = 'first',
  Last = 'last'
}

export interface TableMetadata {
  collectionName: string;
  fields: TableFieldMetadata[];
  indexes: TableIndexMetadata[];
  totalRows: number;
  lastUpdated: string;
}

export interface TableFieldMetadata {
  name: string;
  type: KeyType;
  isRequired: boolean;
  isIndexed: boolean;
  defaultValue?: any;
  validation?: any;
}

export interface TableIndexMetadata {
  name: string;
  fields: string[];
  isUnique: boolean;
  isCompound: boolean;
}