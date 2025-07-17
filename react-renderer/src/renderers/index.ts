/**
 * All entity renderer exports
 */

// Report renderers
export * from './reports';

// HTML Entity renderers  
export * from './html';

// KPI renderers
export * from './kpis';

// Comment renderers
export * from './comments';

// File renderers
export * from './files';

// Notification renderers
export * from './notifications';

// Register default plugins
import { rendererRegistry } from '../plugins';
import { ReportRendererPlugin } from './reports';
import { HtmlEntityRendererPlugin } from './html';

// Auto-register default plugins
export function registerDefaultPlugins(): void {
  rendererRegistry.register(new ReportRendererPlugin());
  rendererRegistry.register(new HtmlEntityRendererPlugin());
  // Additional plugins can be registered here
}

// Export the registration function
export { registerDefaultPlugins as registerDefaultRenderers };