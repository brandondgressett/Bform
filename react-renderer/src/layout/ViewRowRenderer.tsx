import React from 'react';
import { ViewRowDef, ViewSeveralRowDef, ViewColumnsRowDef } from '../types/layout';
import { GridRow } from './GridRow';
import { ViewColumnRenderer } from './ViewColumnRenderer';
import { EntityRenderer } from '../components/EntityRenderer';

interface ViewRowRendererProps {
  rowDef: ViewRowDef;
  data?: any[];
  entityRenderer?: React.ComponentType<any>;
  onEntityClick?: (entity: any) => void;
  className?: string;
}

/**
 * Renders a ViewRowDef into Bootstrap grid layout
 */
export const ViewRowRenderer: React.FC<ViewRowRendererProps> = ({
  rowDef,
  data = [],
  entityRenderer: CustomEntityRenderer,
  onEntityClick,
  className
}) => {
  const renderSeveralRow = (severalRowDef: ViewSeveralRowDef) => {
    const { prototypeColumnSizes, rowQuery, renderer } = severalRowDef;
    
    return (
      <GridRow className={className}>
        {data.map((item, index) => {
          const columnSizes = prototypeColumnSizes.length > 0 
            ? prototypeColumnSizes 
            : ['col-12']; // Default to full width
            
          const EntityComponent = CustomEntityRenderer || EntityRenderer;
          
          return (
            <div key={index} className={columnSizes.map(size => size.replace(/_/g, '-')).join(' ')}>
              <EntityComponent
                entity={item}
                renderer={renderer}
                onClick={() => onEntityClick?.(item)}
              />
            </div>
          );
        })}
      </GridRow>
    );
  };

  const renderColumnsRow = (columnsRowDef: ViewColumnsRowDef) => {
    const { columns } = columnsRowDef;
    
    return (
      <GridRow className={className}>
        {columns.map((columnDef, index) => (
          <ViewColumnRenderer
            key={index}
            columnDef={columnDef}
            data={data}
            entityRenderer={CustomEntityRenderer}
            onEntityClick={onEntityClick}
          />
        ))}
      </GridRow>
    );
  };

  switch (rowDef.kind) {
    case 'ViewSeveralRowDef':
      return renderSeveralRow(rowDef as ViewSeveralRowDef);
    case 'ViewColumnsRowDef':
      return renderColumnsRow(rowDef as ViewColumnsRowDef);
    default:
      console.warn(`Unknown ViewRowDef kind: ${rowDef.kind}`);
      return null;
  }
};

ViewRowRenderer.displayName = 'ViewRowRenderer';