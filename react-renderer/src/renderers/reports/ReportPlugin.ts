import React from 'react';
import { RendererPlugin, RendererProps } from '../../plugins/RendererPlugin';
import { ReportRenderer } from './ReportRenderer';
import { ReportInstance } from '../../types';

/**
 * Plugin for rendering BFormDomain Report entities
 */
export class ReportRendererPlugin implements RendererPlugin {
  id = 'report-renderer';
  name = 'Report Renderer';
  version = '1.0.0';
  supportedEntityTypes = ['ReportInstance' as any];
  supportedRenderers = ['report', 'html-report', 'enhanced-report'];

  canRender(entityType: string, renderer?: string): boolean {
    if (entityType === 'ReportInstance') {
      return true;
    }
    
    if (renderer && this.supportedRenderers.includes(renderer)) {
      return true;
    }
    
    return false;
  }

  render(props: RendererProps): React.ReactElement | null {
    const { entity, renderer, className, style, onClick } = props;
    
    // Determine enhancement mode based on renderer type
    let enhanceMode: 'none' | 'minimal' | 'full' = 'minimal';
    if (renderer === 'enhanced-report') {
      enhanceMode = 'full';
    } else if (renderer === 'html-report') {
      enhanceMode = 'none';
    }

    return React.createElement(ReportRenderer, {
      report: entity as ReportInstance,
      enhanceMode,
      enableTableInteractivity: enhanceMode === 'full',
      enableChartInteractivity: enhanceMode === 'full',
      className,
      style,
      onSectionToggle: (sectionIndex, isExpanded) => {
        console.log(`Section ${sectionIndex} ${isExpanded ? 'expanded' : 'collapsed'}`);
      }
    });
  }

  initialize(context: any): void {
    console.log('ReportRendererPlugin initialized with context:', context);
  }

  dispose(): void {
    console.log('ReportRendererPlugin disposed');
  }
}