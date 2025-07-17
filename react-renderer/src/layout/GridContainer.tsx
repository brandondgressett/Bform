import React from 'react';
import classNames from 'classnames';

interface GridContainerProps {
  children: React.ReactNode;
  fluid?: boolean;
  className?: string;
}

/**
 * Bootstrap grid container component
 */
export const GridContainer: React.FC<GridContainerProps> = ({
  children,
  fluid = false,
  className,
  ...props
}) => {
  const containerClasses = classNames(
    fluid ? 'container-fluid' : 'container',
    className
  );

  return (
    <div className={containerClasses} {...props}>
      {children}
    </div>
  );
};

GridContainer.displayName = 'GridContainer';