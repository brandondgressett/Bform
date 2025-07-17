import React from 'react';
import { ViewRowDef } from '../types/layout';
import { GridContainer } from './GridContainer';
import { ViewRowRenderer } from './ViewRowRenderer';

interface LayoutRendererProps {
  layout: ViewRowDef[];
  data?: any[];
  fluid?: boolean;
  entityRenderer?: React.ComponentType<any>;
  onEntityClick?: (entity: any) => void;
  className?: string;
}

/**
 * Main layout renderer that converts ViewRowDef arrays into Bootstrap grid layouts
 */
export const LayoutRenderer: React.FC<LayoutRendererProps> = ({
  layout,
  data = [],
  fluid = false,
  entityRenderer,
  onEntityClick,
  className
}) => {
  if (!layout || layout.length === 0) {
    return null;
  }

  return (
    <GridContainer fluid={fluid} className={className}>
      {layout.map((rowDef, index) => (
        <ViewRowRenderer
          key={index}
          rowDef={rowDef}
          data={data}
          entityRenderer={entityRenderer}
          onEntityClick={onEntityClick}
        />
      ))}
    </GridContainer>
  );
};

LayoutRenderer.displayName = 'LayoutRenderer';