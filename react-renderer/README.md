# @bformdomain/react-renderer

A comprehensive React library for rendering BFormDomain entities with Bootstrap layouts, JSON Schema forms, and AG-Grid tables.

## Features

- **Universal Entity Rendering**: Supports all BFormDomain entity types (WorkSets, WorkItems, Forms, Tables, Reports, KPIs, etc.)
- **Bootstrap Grid System**: Native support for ViewRowDef/ViewColumnDef with responsive Bootstrap layouts
- **JSON Schema Forms**: Automatic form generation with UI Schema and Yup validation
- **AG-Grid Integration**: High-performance data tables with sorting, filtering, and pagination
- **Plugin Architecture**: Extensible renderer system for custom entity types
- **TypeScript Support**: Full type safety with comprehensive TypeScript definitions
- **Responsive Design**: Mobile-first responsive layouts
- **Theme Support**: Customizable styling with Bootstrap themes

## Installation

```bash
npm install @bformdomain/react-renderer
```

## Peer Dependencies

Make sure you have these peer dependencies installed:

```bash
npm install react react-dom
```

## Quick Start

```tsx
import { EntityRenderer, BFormDomainProvider } from '@bformdomain/react-renderer';
import '@bformdomain/react-renderer/dist/styles.css';

function App() {
  return (
    <BFormDomainProvider>
      <EntityRenderer 
        entity={workSetViewModel} 
        entityType="WorkSet" 
      />
    </BFormDomainProvider>
  );
}
```

## Entity Types Supported

- **WorkSets**: Container entities with dashboard layouts
- **WorkItems**: Task/ticket entities with sections
- **Forms**: JSON Schema-based forms with validation
- **Tables**: Data tables with AG-Grid
- **Reports**: Generated report displays
- **KPIs**: Key Performance Indicators
- **HtmlEntity**: Rich HTML content
- **Comments**: Threaded comment system
- **ManagedFiles**: File attachments
- **Notifications**: User notifications

## Documentation

For complete documentation, examples, and API reference, visit our [documentation site](https://bformdomain.github.io/react-renderer).

## Development

### Setup

```bash
# Clone the repository
git clone https://github.com/bformdomain/react-renderer.git
cd react-renderer

# Install dependencies
npm install
```

### Build System

The library uses a modern build toolchain:

- **TypeScript** for type safety
- **Rollup** for bundling (CommonJS and ES modules)
- **Jest** for testing with React Testing Library
- **ESLint** and **Prettier** for code quality
- **TypeDoc** for API documentation generation
- **Storybook** for component development

### Development Commands

```bash
# Development build with watch mode
npm run build:watch

# Run tests
npm run test
npm run test:watch     # Watch mode
npm run test:coverage  # With coverage report

# Linting and formatting
npm run lint          # Run ESLint
npm run lint:fix      # Fix linting issues
npm run typecheck     # Type check without building

# Documentation
npm run docs:build    # Generate API documentation
npm run storybook     # Start Storybook dev server

# Production build
npm run build         # Build library
./build.sh           # Full build with all checks
```

### Build Outputs

After running `npm run build`:

- **CommonJS**: `dist/index.js`
- **ES Module**: `dist/index.esm.js`
- **TypeScript Definitions**: `dist/index.d.ts`
- **Styles**: `dist/styles.css`

## License

MIT