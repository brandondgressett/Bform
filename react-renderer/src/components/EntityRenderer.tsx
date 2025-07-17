import React from 'react';
import classNames from 'classnames';
import { EntityType } from '../types/common';
import { useBFormDomain } from './BFormDomainProvider';
import { RendererProps } from '../plugins/RendererPlugin';

interface EntityRendererProps extends Omit<RendererProps, 'context'> {
  entity: any;
  entityType?: EntityType;
  renderer?: string;
  fallbackRenderer?: React.ComponentType<RendererProps>;
  className?: string;
  style?: React.CSSProperties;
  onClick?: (entity: any) => void;
  onEdit?: (entity: any) => void;
  onDelete?: (entity: any) => void;
  onAction?: (action: string, entity: any) => void;
  children?: React.ReactNode;
}

/**
 * Default fallback renderer for unknown entity types
 */
const DefaultFallbackRenderer: React.FC<RendererProps> = ({ 
  entity, 
  entityType, 
  className,
  children 
}) => (
  <div className={classNames('bf-entity-fallback', className)}>
    <div className="bf-entity-header">
      <h5>Unknown Entity Type: {entityType}</h5>
    </div>
    <div className="bf-entity-content">
      {entity?.title && <p><strong>Title:</strong> {entity.title}</p>}
      {entity?.description && <p><strong>Description:</strong> {entity.description}</p>}
      {entity?.id && <p><strong>ID:</strong> {entity.id}</p>}
      <details>
        <summary>Raw Data</summary>
        <pre className="bg-light p-2 mt-2">
          {JSON.stringify(entity, null, 2)}
        </pre>
      </details>
      {children}
    </div>
  </div>
);

/**
 * Main entity renderer component that uses the plugin system
 */
export const EntityRenderer: React.FC<EntityRendererProps> = ({
  entity,
  entityType,
  renderer,
  fallbackRenderer: FallbackRenderer = DefaultFallbackRenderer,
  className,
  style,
  onClick,
  onEdit,
  onDelete,
  onAction,
  children,
  ...otherProps
}) => {
  const { context, registry } = useBFormDomain();

  // Auto-detect entity type if not provided
  const resolvedEntityType = entityType || (entity?.entityType as EntityType);
  
  if (!resolvedEntityType) {
    console.warn('EntityRenderer: No entity type specified and could not auto-detect from entity');
    return (
      <FallbackRenderer
        entity={entity}
        entityType={'Unknown' as EntityType}
        context={context}
        className={className}
        style={style}
        onClick={onClick}
        onEdit={onEdit}
        onDelete={onDelete}
        onAction={onAction}
        {...otherProps}
      >
        {children}
      </FallbackRenderer>
    );
  }

  // Find appropriate renderer plugin
  const plugin = registry.findRenderer(resolvedEntityType, renderer);
  
  if (!plugin) {
    console.warn(`EntityRenderer: No plugin found for entity type '${resolvedEntityType}' with renderer '${renderer || 'default'}'`);
    return (
      <FallbackRenderer
        entity={entity}
        entityType={resolvedEntityType}
        context={context}
        className={className}
        style={style}
        onClick={onClick}
        onEdit={onEdit}
        onDelete={onDelete}
        onAction={onAction}
        {...otherProps}
      >
        {children}
      </FallbackRenderer>
    );
  }

  // Render using the plugin
  try {
    return plugin.render({
      entity,
      entityType: resolvedEntityType,
      renderer,
      context,
      className,
      style,
      onClick,
      onEdit,
      onDelete,
      onAction,
      children,
      ...otherProps
    });
  } catch (error) {
    console.error(`EntityRenderer: Error rendering entity with plugin '${plugin.id}':`, error);
    return (
      <FallbackRenderer
        entity={entity}
        entityType={resolvedEntityType}
        context={context}
        className={className}
        style={style}
        onClick={onClick}
        onEdit={onEdit}
        onDelete={onDelete}
        onAction={onAction}
        {...otherProps}
      >
        {children}
        <div className="alert alert-danger mt-2">
          <strong>Rendering Error:</strong> {error instanceof Error ? error.message : String(error)}
        </div>
      </FallbackRenderer>
    );
  }
};

EntityRenderer.displayName = 'EntityRenderer';