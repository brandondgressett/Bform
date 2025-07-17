/**
 * Layout system types for Bootstrap grid and ViewRowDef/ViewColumnDef
 */

export type GridColumnSize = 
  | 'col-1' | 'col-2' | 'col-3' | 'col-4' | 'col-5' | 'col-6'
  | 'col-7' | 'col-8' | 'col-9' | 'col-10' | 'col-11' | 'col-12'
  | 'col-sm-1' | 'col-sm-2' | 'col-sm-3' | 'col-sm-4' | 'col-sm-5' | 'col-sm-6'
  | 'col-sm-7' | 'col-sm-8' | 'col-sm-9' | 'col-sm-10' | 'col-sm-11' | 'col-sm-12'
  | 'col-md-1' | 'col-md-2' | 'col-md-3' | 'col-md-4' | 'col-md-5' | 'col-md-6'
  | 'col-md-7' | 'col-md-8' | 'col-md-9' | 'col-md-10' | 'col-md-11' | 'col-md-12'
  | 'col-lg-1' | 'col-lg-2' | 'col-lg-3' | 'col-lg-4' | 'col-lg-5' | 'col-lg-6'
  | 'col-lg-7' | 'col-lg-8' | 'col-lg-9' | 'col-lg-10' | 'col-lg-11' | 'col-lg-12'
  | 'col-xl-1' | 'col-xl-2' | 'col-xl-3' | 'col-xl-4' | 'col-xl-5' | 'col-xl-6'
  | 'col-xl-7' | 'col-xl-8' | 'col-xl-9' | 'col-xl-10' | 'col-xl-11' | 'col-xl-12';

export abstract class ViewRowDef {
  abstract kind: string;
}

export interface ViewSeveralRowDef extends ViewRowDef {
  kind: 'ViewSeveralRowDef';
  prototypeColumnSizes: GridColumnSize[];
  rowQuery: ViewDataQuery;
  renderer: string;
}

export interface ViewColumnsRowDef extends ViewRowDef {
  kind: 'ViewColumnsRowDef';
  columns: ViewColumnDef[];
}

export abstract class ViewColumnDef {
  abstract kind: string;
}

export interface ViewNestedGridDef extends ViewColumnDef {
  kind: 'ViewNestedGridDef';
  nestedGrid: ViewRowDef[];
}

export interface ViewPerColumnDef extends ViewColumnDef {
  kind: 'ViewPerColumnDef';
  sizes: GridColumnSize[];
  columnQuery: ViewDataQuery;
  renderer: string;
}

export interface ViewDataQuery {
  workItemAnyTags?: string[];
  attachedEntityAnyTags?: string[];
  limitMatch?: number;
  descendingOrder?: number;
  grouping?: string;
}

export interface LayoutProps {
  children: React.ReactNode;
  className?: string;
  sizes?: GridColumnSize[];
}

export interface GridRowProps {
  children: React.ReactNode;
  className?: string;
  rowDef?: ViewRowDef;
}

export interface GridColumnProps {
  children: React.ReactNode;
  sizes: GridColumnSize[];
  className?: string;
}