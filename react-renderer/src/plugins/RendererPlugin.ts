import React from 'react';
import { EntityType, RenderContext } from '../types/common';

/**
 * Base interface for entity renderer plugins
 */
export interface RendererPlugin {
  /**
   * Unique identifier for the plugin
   */
  id: string;
  
  /**
   * Display name for the plugin
   */
  name: string;
  
  /**
   * Entity types this plugin can handle
   */
  supportedEntityTypes: EntityType[];
  
  /**
   * Renderer names this plugin can handle (optional - for custom renderers)
   */
  supportedRenderers?: string[];
  
  /**
   * Plugin version
   */
  version: string;
  
  /**
   * Check if this plugin can render the given entity/renderer combination
   */
  canRender(entityType: EntityType, renderer?: string): boolean;
  
  /**
   * Render the entity
   */
  render(props: RendererProps): React.ReactElement | null;
  
  /**
   * Optional initialization function
   */
  initialize?(context: RenderContext): void | Promise<void>;
  
  /**
   * Optional cleanup function
   */
  dispose?(): void | Promise<void>;
}

export interface RendererProps {
  entity: any;
  entityType: EntityType;
  renderer?: string;
  context: RenderContext;
  className?: string;
  style?: React.CSSProperties;
  onClick?: (entity: any) => void;
  onEdit?: (entity: any) => void;
  onDelete?: (entity: any) => void;
  onAction?: (action: string, entity: any) => void;
  children?: React.ReactNode;
  [key: string]: any;
}

/**
 * Registry for managing renderer plugins
 */
export class RendererRegistry {
  private plugins: Map<string, RendererPlugin> = new Map();
  private entityTypeMap: Map<EntityType, RendererPlugin[]> = new Map();
  private rendererMap: Map<string, RendererPlugin[]> = new Map();

  /**
   * Register a renderer plugin
   */
  register(plugin: RendererPlugin): void {
    this.plugins.set(plugin.id, plugin);
    
    // Update entity type mapping
    plugin.supportedEntityTypes.forEach(entityType => {
      if (!this.entityTypeMap.has(entityType)) {
        this.entityTypeMap.set(entityType, []);
      }
      this.entityTypeMap.get(entityType)!.push(plugin);
    });
    
    // Update renderer mapping
    if (plugin.supportedRenderers) {
      plugin.supportedRenderers.forEach(renderer => {
        if (!this.rendererMap.has(renderer)) {
          this.rendererMap.set(renderer, []);
        }
        this.rendererMap.get(renderer)!.push(plugin);
      });
    }
  }

  /**
   * Unregister a renderer plugin
   */
  unregister(pluginId: string): void {
    const plugin = this.plugins.get(pluginId);
    if (!plugin) return;

    this.plugins.delete(pluginId);
    
    // Clean up entity type mapping
    plugin.supportedEntityTypes.forEach(entityType => {
      const plugins = this.entityTypeMap.get(entityType) || [];
      const filtered = plugins.filter(p => p.id !== pluginId);
      if (filtered.length === 0) {
        this.entityTypeMap.delete(entityType);
      } else {
        this.entityTypeMap.set(entityType, filtered);
      }
    });
    
    // Clean up renderer mapping
    if (plugin.supportedRenderers) {
      plugin.supportedRenderers.forEach(renderer => {
        const plugins = this.rendererMap.get(renderer) || [];
        const filtered = plugins.filter(p => p.id !== pluginId);
        if (filtered.length === 0) {
          this.rendererMap.delete(renderer);
        } else {
          this.rendererMap.set(renderer, filtered);
        }
      });
    }
  }

  /**
   * Find the best plugin for rendering an entity
   */
  findRenderer(entityType: EntityType, renderer?: string): RendererPlugin | null {
    // First try to find by specific renderer name
    if (renderer) {
      const rendererPlugins = this.rendererMap.get(renderer) || [];
      const matchingPlugin = rendererPlugins.find(plugin => 
        plugin.canRender(entityType, renderer)
      );
      if (matchingPlugin) {
        return matchingPlugin;
      }
    }
    
    // Fall back to entity type matching
    const entityPlugins = this.entityTypeMap.get(entityType) || [];
    return entityPlugins.find(plugin => plugin.canRender(entityType, renderer)) || null;
  }

  /**
   * Get all registered plugins
   */
  getAllPlugins(): RendererPlugin[] {
    return Array.from(this.plugins.values());
  }

  /**
   * Get plugin by ID
   */
  getPlugin(id: string): RendererPlugin | null {
    return this.plugins.get(id) || null;
  }

  /**
   * Initialize all plugins
   */
  async initialize(context: RenderContext): Promise<void> {
    const initPromises = Array.from(this.plugins.values())
      .filter(plugin => plugin.initialize)
      .map(plugin => plugin.initialize!(context));
    
    await Promise.all(initPromises);
  }

  /**
   * Dispose all plugins
   */
  async dispose(): Promise<void> {
    const disposePromises = Array.from(this.plugins.values())
      .filter(plugin => plugin.dispose)
      .map(plugin => plugin.dispose!());
    
    await Promise.all(disposePromises);
  }
}

// Global registry instance
export const rendererRegistry = new RendererRegistry();