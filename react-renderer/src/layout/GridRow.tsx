import React from 'react';
import classNames from 'classnames';
import { GridRowProps } from '../types/layout';

/**
 * Bootstrap grid row component for containing columns
 */
export const GridRow: React.FC<GridRowProps> = ({
  children,
  className,
  rowDef,
  ...props
}) => {
  const rowClasses = classNames(
    'row',
    className
  );

  return (
    <div className={rowClasses} {...props}>
      {children}
    </div>
  );
};

GridRow.displayName = 'GridRow';