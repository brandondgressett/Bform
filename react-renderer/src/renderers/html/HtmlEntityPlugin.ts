import React from 'react';
import { RendererPlugin, RendererProps } from '../../plugins/RendererPlugin';
import { HtmlEntityRenderer } from './HtmlEntityRenderer';
import { HtmlInstance } from '../../types';

/**
 * Plugin for rendering BFormDomain HtmlEntity instances
 */
export class HtmlEntityRendererPlugin implements RendererPlugin {
  id = 'html-entity-renderer';
  name = 'HTML Entity Renderer';
  version = '1.0.0';
  supportedEntityTypes = ['HtmlInstance' as any];
  supportedRenderers = ['html', 'html-content', 'rich-content'];

  canRender(entityType: string, renderer?: string): boolean {
    if (entityType === 'HtmlInstance') {
      return true;
    }
    
    if (renderer && this.supportedRenderers.includes(renderer)) {
      return true;
    }
    
    return false;
  }

  render(props: RendererProps): React.ReactElement | null {
    const { entity, renderer, className, style, onClick } = props;
    
    // Determine safety settings based on renderer type
    const allowUnsafeContent = renderer === 'rich-content';
    const enableBootstrapClasses = renderer !== 'html';

    return React.createElement(HtmlEntityRenderer, {
      entity: entity as HtmlInstance,
      allowUnsafeContent,
      enableBootstrapClasses,
      className,
      style,
      onContentClick: onClick ? (event: React.MouseEvent) => {
        onClick(entity);
      } : undefined
    });
  }

  initialize(context: any): void {
    console.log('HtmlEntityRendererPlugin initialized with context:', context);
  }

  dispose(): void {
    console.log('HtmlEntityRendererPlugin disposed');
  }
}