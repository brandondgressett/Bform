import React from 'react';
import classNames from 'classnames';
import { GridColumnSize, GridColumnProps } from '../types/layout';

/**
 * Bootstrap grid column component that converts BFormDomain GridColumnSizes
 * to Bootstrap CSS classes
 */
export const GridColumn: React.FC<GridColumnProps> = ({
  children,
  sizes,
  className,
  ...props
}) => {
  // Convert enum-style sizes to Bootstrap class names
  const getBootstrapClasses = (columnSizes: GridColumnSize[]): string[] => {
    return columnSizes.map(size => {
      // Convert enum format (col_md_6) to Bootstrap format (col-md-6)
      return size.replace(/_/g, '-');
    });
  };

  const bootstrapClasses = getBootstrapClasses(sizes);
  
  const columnClasses = classNames(
    ...bootstrapClasses,
    className
  );

  return (
    <div className={columnClasses} {...props}>
      {children}
    </div>
  );
};

GridColumn.displayName = 'GridColumn';