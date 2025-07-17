import React from 'react';
import { ViewColumnDef, ViewNestedGridDef, ViewPerColumnDef } from '../types/layout';
import { GridColumn } from './GridColumn';
import { ViewRowRenderer } from './ViewRowRenderer';
import { EntityRenderer } from '../components/EntityRenderer';

interface ViewColumnRendererProps {
  columnDef: ViewColumnDef;
  data?: any[];
  entityRenderer?: React.ComponentType<any>;
  onEntityClick?: (entity: any) => void;
  className?: string;
}

/**
 * Renders a ViewColumnDef into Bootstrap grid column
 */
export const ViewColumnRenderer: React.FC<ViewColumnRendererProps> = ({
  columnDef,
  data = [],
  entityRenderer: CustomEntityRenderer,
  onEntityClick,
  className
}) => {
  const renderNestedGrid = (nestedGridDef: ViewNestedGridDef) => {
    const { nestedGrid } = nestedGridDef;
    
    // Default to full width if no sizes specified
    const defaultSizes = ['col-12'];
    
    return (
      <GridColumn sizes={defaultSizes} className={className}>
        {nestedGrid.map((rowDef, index) => (
          <ViewRowRenderer
            key={index}
            rowDef={rowDef}
            data={data}
            entityRenderer={CustomEntityRenderer}
            onEntityClick={onEntityClick}
          />
        ))}
      </GridColumn>
    );
  };

  const renderPerColumn = (perColumnDef: ViewPerColumnDef) => {
    const { sizes, columnQuery, renderer } = perColumnDef;
    
    // Filter data based on column query if specified
    let columnData = data;
    if (columnQuery) {
      columnData = filterDataByQuery(data, columnQuery);
    }
    
    const EntityComponent = CustomEntityRenderer || EntityRenderer;
    
    return (
      <GridColumn sizes={sizes} className={className}>
        {columnData.map((item, index) => (
          <EntityComponent
            key={index}
            entity={item}
            renderer={renderer}
            onClick={() => onEntityClick?.(item)}
          />
        ))}
      </GridColumn>
    );
  };

  // Helper function to filter data based on ViewDataQuery
  const filterDataByQuery = (data: any[], query: any) => {
    let filtered = [...data];
    
    // Apply tag filtering
    if (query.workItemAnyTags?.length > 0) {
      filtered = filtered.filter(item => 
        query.workItemAnyTags.some((tag: string) => item.tags?.includes(tag))
      );
    }
    
    if (query.attachedEntityAnyTags?.length > 0) {
      filtered = filtered.filter(item => 
        query.attachedEntityAnyTags.some((tag: string) => item.tags?.includes(tag))
      );
    }
    
    // Apply sorting
    if (query.descendingOrder !== undefined) {
      filtered.sort((a, b) => {
        const aValue = a.descendingOrder || 0;
        const bValue = b.descendingOrder || 0;
        return query.descendingOrder > 0 ? bValue - aValue : aValue - bValue;
      });
    }
    
    // Apply limit
    if (query.limitMatch && query.limitMatch > 0) {
      filtered = filtered.slice(0, query.limitMatch);
    }
    
    return filtered;
  };

  switch (columnDef.kind) {
    case 'ViewNestedGridDef':
      return renderNestedGrid(columnDef as ViewNestedGridDef);
    case 'ViewPerColumnDef':
      return renderPerColumn(columnDef as ViewPerColumnDef);
    default:
      console.warn(`Unknown ViewColumnDef kind: ${columnDef.kind}`);
      return null;
  }
};

ViewColumnRenderer.displayName = 'ViewColumnRenderer';