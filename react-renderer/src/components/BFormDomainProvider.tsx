import React, { createContext, useContext, useEffect, useState } from 'react';
import { RenderContext } from '../types/common';
import { rendererRegistry, RendererRegistry } from '../plugins/RendererPlugin';

interface BFormDomainContextValue {
  context: RenderContext;
  registry: RendererRegistry;
  updateContext: (updates: Partial<RenderContext>) => void;
}

const BFormDomainContext = createContext<BFormDomainContextValue | null>(null);

interface BFormDomainProviderProps {
  children: React.ReactNode;
  initialContext?: Partial<RenderContext>;
  registry?: RendererRegistry;
}

/**
 * Provider component for BFormDomain rendering context
 */
export const BFormDomainProvider: React.FC<BFormDomainProviderProps> = ({
  children,
  initialContext = {},
  registry = rendererRegistry
}) => {
  const [context, setContext] = useState<RenderContext>({
    theme: 'default',
    locale: 'en-US',
    ...initialContext
  });

  const updateContext = (updates: Partial<RenderContext>) => {
    setContext(prev => ({ ...prev, ...updates }));
  };

  // Initialize registry when context changes
  useEffect(() => {
    registry.initialize(context);
    
    return () => {
      registry.dispose();
    };
  }, [context, registry]);

  const value: BFormDomainContextValue = {
    context,
    registry,
    updateContext
  };

  return (
    <BFormDomainContext.Provider value={value}>
      {children}
    </BFormDomainContext.Provider>
  );
};

/**
 * Hook to access BFormDomain context
 */
export const useBFormDomain = (): BFormDomainContextValue => {
  const context = useContext(BFormDomainContext);
  if (!context) {
    throw new Error('useBFormDomain must be used within a BFormDomainProvider');
  }
  return context;
};