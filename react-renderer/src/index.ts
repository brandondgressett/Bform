/**
 * @bformdomain/react-renderer
 * 
 * A comprehensive React library for rendering BFormDomain entities
 * with Bootstrap layouts, JSON Schema forms, and AG-Grid tables.
 */

// Core components
export * from './components';

// Layout system
export * from './layout';

// Plugin system
export * from './plugins';

// Entity renderers
export * from './renderers';

// Utilities
export * from './utils';

// Type definitions
export * from './types';

// Auto-register default plugins when imported
import { registerDefaultRenderers } from './renderers';
registerDefaultRenderers();

// Version
export const VERSION = '1.0.0';