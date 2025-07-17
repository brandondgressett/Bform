import React, { useMemo } from 'react';
import classNames from 'classnames';
import { KpiInstance, KpiRowData } from '../../types';
import { formatValue } from '../../utils/formatting';

interface KpiRendererProps {
  kpi: KpiInstance;
  variant?: 'card' | 'inline' | 'compact';
  showTrend?: boolean;
  showTarget?: boolean;
  className?: string;
  style?: React.CSSProperties;
  onClick?: () => void;
}

/**
 * Renders a Key Performance Indicator with various display options
 */
export const KpiRenderer: React.FC<KpiRendererProps> = ({
  kpi,
  variant = 'card',
  showTrend = true,
  showTarget = true,
  className,
  style,
  onClick
}) => {
  // Calculate current value and trend
  const { currentValue, trendValue, trendPercentage, targetStatus } = useMemo(() => {
    const rows = kpi.rowData || [];
    if (rows.length === 0) {
      return { 
        currentValue: 0, 
        trendValue: 0, 
        trendPercentage: 0,
        targetStatus: 'none' as const
      };
    }

    const current = rows[rows.length - 1]?.value || 0;
    const previous = rows.length > 1 ? rows[rows.length - 2]?.value || 0 : current;
    const trend = current - previous;
    const trendPct = previous !== 0 ? (trend / previous) * 100 : 0;

    let status: 'above' | 'below' | 'met' | 'none' = 'none';
    if (kpi.targetValue !== undefined && kpi.targetValue !== null) {
      if (current > kpi.targetValue) status = 'above';
      else if (current < kpi.targetValue) status = 'below';
      else status = 'met';
    }

    return {
      currentValue: current,
      trendValue: trend,
      trendPercentage: trendPct,
      targetStatus: status
    };
  }, [kpi.rowData, kpi.targetValue]);

  const getTrendIcon = () => {
    if (trendValue > 0) return '↑';
    if (trendValue < 0) return '↓';
    return '→';
  };

  const getTrendColor = () => {
    if (!kpi.positiveDirection) return 'text-muted';
    
    const isPositive = kpi.positiveDirection === 'up' ? trendValue > 0 : trendValue < 0;
    return isPositive ? 'text-success' : 'text-danger';
  };

  const getTargetStatusColor = () => {
    if (targetStatus === 'none' || !kpi.targetValue) return '';
    
    if (kpi.positiveDirection === 'up') {
      return targetStatus === 'above' || targetStatus === 'met' ? 'text-success' : 'text-danger';
    } else {
      return targetStatus === 'below' || targetStatus === 'met' ? 'text-success' : 'text-danger';
    }
  };

  const containerClasses = classNames(
    'bf-kpi-renderer',
    `bf-kpi-${variant}`,
    {
      'bf-kpi-clickable': !!onClick
    },
    className
  );

  // Card variant
  if (variant === 'card') {
    return (
      <div 
        className={containerClasses} 
        style={style}
        onClick={onClick}
        role={onClick ? 'button' : undefined}
        tabIndex={onClick ? 0 : undefined}
      >
        <div className="card h-100">
          <div className="card-body">
            <h6 className="card-subtitle mb-2 text-muted">
              {kpi.name}
            </h6>
            
            <div className="d-flex align-items-baseline mb-2">
              <h2 className="card-title mb-0">
                {formatValue(currentValue, kpi.formatString)}
              </h2>
              {kpi.unitOfMeasure && (
                <span className="ms-2 text-muted">{kpi.unitOfMeasure}</span>
              )}
            </div>

            {showTrend && (
              <div className={`d-flex align-items-center mb-2 ${getTrendColor()}`}>
                <span className="me-1">{getTrendIcon()}</span>
                <span>{Math.abs(trendPercentage).toFixed(1)}%</span>
                <small className="ms-2 text-muted">
                  ({trendValue > 0 ? '+' : ''}{formatValue(trendValue, kpi.formatString)})
                </small>
              </div>
            )}

            {showTarget && kpi.targetValue !== undefined && (
              <div className="d-flex justify-content-between align-items-center">
                <small className="text-muted">Target:</small>
                <small className={getTargetStatusColor()}>
                  {formatValue(kpi.targetValue, kpi.formatString)}
                  {targetStatus === 'met' && ' ✓'}
                </small>
              </div>
            )}

            {kpi.description && (
              <p className="card-text small text-muted mt-2 mb-0">
                {kpi.description}
              </p>
            )}
          </div>
        </div>
      </div>
    );
  }

  // Inline variant
  if (variant === 'inline') {
    return (
      <div 
        className={containerClasses} 
        style={style}
        onClick={onClick}
        role={onClick ? 'button' : undefined}
        tabIndex={onClick ? 0 : undefined}
      >
        <div className="d-flex align-items-center">
          <div className="me-3">
            <small className="text-muted d-block">{kpi.name}</small>
            <div className="d-flex align-items-baseline">
              <strong className="me-1">
                {formatValue(currentValue, kpi.formatString)}
              </strong>
              {kpi.unitOfMeasure && (
                <small className="text-muted">{kpi.unitOfMeasure}</small>
              )}
            </div>
          </div>

          {showTrend && (
            <div className={`me-3 ${getTrendColor()}`}>
              <span>{getTrendIcon()} {Math.abs(trendPercentage).toFixed(1)}%</span>
            </div>
          )}

          {showTarget && kpi.targetValue !== undefined && (
            <div className="text-muted">
              <small>
                Target: <span className={getTargetStatusColor()}>
                  {formatValue(kpi.targetValue, kpi.formatString)}
                </span>
              </small>
            </div>
          )}
        </div>
      </div>
    );
  }

  // Compact variant
  return (
    <div 
      className={containerClasses} 
      style={style}
      onClick={onClick}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
    >
      <div className="d-flex justify-content-between align-items-center">
        <small className="text-muted">{kpi.name}</small>
        <div className="d-flex align-items-baseline">
          <strong className={targetStatus !== 'none' ? getTargetStatusColor() : ''}>
            {formatValue(currentValue, kpi.formatString)}
          </strong>
          {showTrend && (
            <small className={`ms-2 ${getTrendColor()}`}>
              {getTrendIcon()}
            </small>
          )}
        </div>
      </div>
    </div>
  );
};

KpiRenderer.displayName = 'KpiRenderer';

// KPI Dashboard Component
interface KpiDashboardProps {
  kpis: KpiInstance[];
  columns?: 1 | 2 | 3 | 4 | 6;
  variant?: 'card' | 'inline' | 'compact';
  className?: string;
  onKpiClick?: (kpi: KpiInstance) => void;
}

/**
 * Renders multiple KPIs in a responsive grid layout
 */
export const KpiDashboard: React.FC<KpiDashboardProps> = ({
  kpis,
  columns = 3,
  variant = 'card',
  className,
  onKpiClick
}) => {
  const colClass = `col-12 col-md-${12 / columns}`;
  
  return (
    <div className={classNames('bf-kpi-dashboard', className)}>
      <div className="row g-3">
        {kpis.map(kpi => (
          <div key={kpi.id} className={colClass}>
            <KpiRenderer
              kpi={kpi}
              variant={variant}
              onClick={onKpiClick ? () => onKpiClick(kpi) : undefined}
            />
          </div>
        ))}
      </div>
    </div>
  );
};

KpiDashboard.displayName = 'KpiDashboard';