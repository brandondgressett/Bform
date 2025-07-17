# BFormDomain Report Rendering System

## Overview

The BFormDomain report system generates complete HTML reports using the HTMLReportEngine. This React library provides components and utilities to render these HTML reports with optional enhancements and interactivity.

## Architecture

### Report Generation Flow
1. **C# Backend**: Uses `HTMLReportEngine` to generate complete HTML documents
2. **ReportInstance**: Stores generated HTML in the `Html` property
3. **React Frontend**: Renders HTML with sanitization and enhancements

### Key Components

#### 1. ReportRenderer
Main component for rendering HTML reports with three enhancement modes:
- **none**: Raw HTML rendering (no modifications)
- **minimal**: Basic Bootstrap styling and modernization
- **full**: Interactive features (collapsible sections, table extraction)

```tsx
<ReportRenderer
  report={reportInstance}
  enhanceMode="full"
  enableTableInteractivity={true}
  onTableDataExtracted={(data) => console.log(data)}
/>
```

#### 2. ReportViewerModal
Full-featured modal viewer with:
- HTML and Table view modes
- Print functionality
- Export to HTML file
- AG-Grid integration for table data

```tsx
<ReportViewerModal
  report={reportInstance}
  isOpen={true}
  onClose={() => {}}
  enableEnhancements={true}
/>
```

#### 3. EnhancedChart
Converts HTML bar charts to interactive React components:
- Bar chart visualization
- Pie/Donut chart options
- Click interactions
- Responsive design

```tsx
<EnhancedChart
  htmlChartContent={reportHtml}
  chartType="pie"
  interactive={true}
  onSegmentClick={(data) => console.log(data)}
/>
```

#### 4. HtmlEntityRenderer
Renders generic HTML content with:
- Content sanitization
- Bootstrap class injection
- Attribute modernization
- Draft/Published states

```tsx
<HtmlEntityRenderer
  entity={htmlInstance}
  enableBootstrapClasses={true}
  allowUnsafeContent={false}
/>
```

## HTML Processing Pipeline

### 1. Sanitization
- Removes dangerous scripts and event handlers
- Whitelists allowed tags and attributes
- Preserves report-specific CSS classes

### 2. Modernization
- Converts deprecated attributes to CSS
- `bgcolor` → `background-color`
- `align` → `text-align`
- `valign` → `vertical-align`

### 3. Enhancement
- Adds Bootstrap classes to tables
- Makes sections collapsible
- Wraps tables in responsive containers
- Converts alerts and notifications

## Plugin System Integration

The report renderers integrate with the plugin architecture:

```typescript
// Auto-registered plugins
- ReportRendererPlugin: Handles 'ReportInstance' entities
- HtmlEntityRendererPlugin: Handles 'HtmlInstance' entities

// Usage with EntityRenderer
<EntityRenderer
  entity={reportInstance}
  entityType="ReportInstance"
  renderer="enhanced-report"
/>
```

## Table Data Extraction

Reports can have their table data extracted for use with AG-Grid:

```typescript
interface TableData {
  headers: string[];
  rows: Record<string, any>[];
  hasTotal: boolean;
  totalRow?: Record<string, any>;
}
```

## Security Considerations

1. **HTML Sanitization**: All HTML is sanitized by default
2. **CSP Compliance**: No inline scripts or dangerous content
3. **Safe Rendering**: Uses React's dangerouslySetInnerHTML safely
4. **Configurable Safety**: `allowUnsafeContent` flag for trusted content

## Styling and Theming

### CSS Classes Preserved
- `.TableStyle`: Report tables
- `.TitleStyle`: Report titles
- `.SectionHeader`: Section headers
- `.DetailHeader`: Detail headers
- `.DetailData`: Data cells
- `.ColumnHeaderStyle`: Column headers

### Bootstrap Integration
- Tables get `table`, `table-striped`, `table-hover`
- Responsive wrappers added automatically
- Alert patterns converted to Bootstrap alerts
- Buttons styled with Bootstrap classes

## Advanced Features

### 1. Section Collapsing
Sections with bold headers become collapsible in full enhancement mode.

### 2. Print Optimization
Reports open in new window with print-specific styles.

### 3. Export Functionality
Download reports as standalone HTML files.

### 4. Chart Interactivity
HTML bar charts can be converted to interactive visualizations.

## Performance Considerations

1. **Memoization**: HTML processing is memoized to avoid re-parsing
2. **Lazy Loading**: Table data extraction only happens when needed
3. **Virtual Scrolling**: AG-Grid handles large datasets efficiently

## Browser Compatibility

- Modern browsers (Chrome, Firefox, Safari, Edge)
- IE11 not supported due to modern JavaScript features
- Mobile responsive with Bootstrap grid system

## Future Enhancements

1. **Chart Libraries**: Integration with Chart.js or D3.js
2. **PDF Export**: Direct PDF generation from reports
3. **Real-time Updates**: WebSocket integration for live data
4. **Custom Themes**: User-defined styling options
5. **Accessibility**: Enhanced ARIA labels and keyboard navigation