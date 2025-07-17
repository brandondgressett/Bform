#!/bin/bash

# BFormDomain React Renderer Build Script

set -e

echo "🏗️  Building @bformdomain/react-renderer..."

# Clean previous build
echo "🧹 Cleaning previous build..."
npm run clean 2>/dev/null || rm -rf dist

# Run linting
echo "🔍 Linting code..."
npm run lint

# Run type checking
echo "📝 Type checking..."
npm run typecheck

# Run tests
echo "🧪 Running tests..."
npm test

# Build the library
echo "📦 Building library..."
npm run build

# Generate documentation
echo "📚 Generating documentation..."
npm run docs:build

echo "✅ Build completed successfully!"

# Display build output info
echo ""
echo "Build outputs:"
echo "  - CommonJS: dist/index.js"
echo "  - ES Module: dist/index.esm.js"
echo "  - TypeScript definitions: dist/index.d.ts"
echo "  - Documentation: docs/api/"
echo ""
echo "To publish: npm publish"