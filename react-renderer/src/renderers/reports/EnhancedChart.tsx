import React, { useMemo } from 'react';
import classNames from 'classnames';

interface ChartData {
  label: string;
  value: number;
  percentage: number;
  color: string;
}

interface EnhancedChartProps {
  htmlChartContent: string;
  interactive?: boolean;
  chartType?: 'bar' | 'pie' | 'donut';
  showLegend?: boolean;
  className?: string;
  style?: React.CSSProperties;
  onSegmentClick?: (data: ChartData) => void;
}

/**
 * Converts HTML bar charts from reports into interactive React components
 */
export const EnhancedChart: React.FC<EnhancedChartProps> = ({
  htmlChartContent,
  interactive = true,
  chartType = 'bar',
  showLegend = true,
  className,
  style,
  onSegmentClick
}) => {
  // Extract chart data from HTML
  const chartData = useMemo(() => {
    const data: ChartData[] = [];
    const parser = new DOMParser();
    const doc = parser.parseFromString(htmlChartContent, 'text/html');
    
    // Extract title
    const titleElement = doc.querySelector('b');
    const title = titleElement?.textContent || 'Chart';

    // Extract data rows
    const rows = doc.querySelectorAll('tr');
    const colors = ['#ff0000', '#ffff00', '#ff00ff', '#00ff00', '#00ffff', '#0000ff', '#ff0f0f', '#f0f000', '#ff00f0', '#0f00f0'];
    let colorIndex = 0;

    rows.forEach(row => {
      const cells = row.querySelectorAll('td');
      if (cells.length >= 3) {
        const label = cells[0].textContent?.trim() || '';
        const percentageText = cells[1].textContent?.trim() || '';
        const valueText = cells[2].textContent?.trim() || '';
        
        // Skip header rows
        if (label && !label.includes('Label') && valueText && !valueText.includes('Value')) {
          const value = parseFloat(valueText) || 0;
          const percentage = parseFloat(percentageText.replace('%', '')) || 0;
          
          data.push({
            label,
            value,
            percentage,
            color: colors[colorIndex % colors.length]
          });
          colorIndex++;
        }
      }
    });

    return { title, data };
  }, [htmlChartContent]);

  const renderBarChart = () => {
    const maxValue = Math.max(...chartData.data.map(d => d.value));
    
    return (
      <div className="bf-enhanced-bar-chart">
        <h5 className="chart-title mb-3">{chartData.title}</h5>
        <div className="chart-container">
          {chartData.data.map((item, index) => (
            <div 
              key={index} 
              className={classNames('chart-row mb-2', { 'interactive': interactive })}
              onClick={() => interactive && onSegmentClick?.(item)}
              style={{ cursor: interactive ? 'pointer' : 'default' }}
            >
              <div className="d-flex align-items-center">
                <div className="label-column" style={{ width: '150px', textAlign: 'right', paddingRight: '10px' }}>
                  {item.label}
                </div>
                <div className="bar-column flex-grow-1" style={{ maxWidth: '400px' }}>
                  <div className="progress" style={{ height: '25px' }}>
                    <div 
                      className="progress-bar"
                      role="progressbar"
                      style={{
                        width: `${(item.value / maxValue) * 100}%`,
                        backgroundColor: item.color,
                        transition: 'width 0.5s ease'
                      }}
                      aria-valuenow={item.value}
                      aria-valuemin={0}
                      aria-valuemax={maxValue}
                    >
                      <span className="text-dark px-2">{item.percentage.toFixed(1)}%</span>
                    </div>
                  </div>
                </div>
                <div className="value-column ms-3" style={{ minWidth: '80px', textAlign: 'right' }}>
                  {item.value.toLocaleString()}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  };

  const renderPieChart = () => {
    const radius = 120;
    const centerX = 150;
    const centerY = 150;
    let currentAngle = -90; // Start at top

    const createPath = (startAngle: number, endAngle: number, isDonut: boolean) => {
      const innerRadius = isDonut ? radius * 0.6 : 0;
      const startAngleRad = (startAngle * Math.PI) / 180;
      const endAngleRad = (endAngle * Math.PI) / 180;
      
      const x1 = centerX + radius * Math.cos(startAngleRad);
      const y1 = centerY + radius * Math.sin(startAngleRad);
      const x2 = centerX + radius * Math.cos(endAngleRad);
      const y2 = centerY + radius * Math.sin(endAngleRad);
      
      const largeArcFlag = endAngle - startAngle > 180 ? 1 : 0;
      
      if (isDonut) {
        const ix1 = centerX + innerRadius * Math.cos(startAngleRad);
        const iy1 = centerY + innerRadius * Math.sin(startAngleRad);
        const ix2 = centerX + innerRadius * Math.cos(endAngleRad);
        const iy2 = centerY + innerRadius * Math.sin(endAngleRad);
        
        return `
          M ${x1} ${y1}
          A ${radius} ${radius} 0 ${largeArcFlag} 1 ${x2} ${y2}
          L ${ix2} ${iy2}
          A ${innerRadius} ${innerRadius} 0 ${largeArcFlag} 0 ${ix1} ${iy1}
          Z
        `;
      } else {
        return `
          M ${centerX} ${centerY}
          L ${x1} ${y1}
          A ${radius} ${radius} 0 ${largeArcFlag} 1 ${x2} ${y2}
          Z
        `;
      }
    };

    return (
      <div className="bf-enhanced-pie-chart">
        <h5 className="chart-title mb-3">{chartData.title}</h5>
        <div className="d-flex align-items-center">
          <svg width="300" height="300" className="me-4">
            {chartData.data.map((item, index) => {
              const angle = (item.percentage / 100) * 360;
              const endAngle = currentAngle + angle;
              const path = createPath(currentAngle, endAngle, chartType === 'donut');
              const midAngle = currentAngle + angle / 2;
              currentAngle = endAngle;
              
              return (
                <g key={index}>
                  <path
                    d={path}
                    fill={item.color}
                    stroke="#fff"
                    strokeWidth="2"
                    className={classNames({ 'chart-segment': interactive })}
                    onClick={() => interactive && onSegmentClick?.(item)}
                    style={{ cursor: interactive ? 'pointer' : 'default', transition: 'opacity 0.3s' }}
                    onMouseEnter={(e) => interactive && (e.currentTarget.style.opacity = '0.8')}
                    onMouseLeave={(e) => interactive && (e.currentTarget.style.opacity = '1')}
                  />
                  {angle > 15 && ( // Only show label if segment is large enough
                    <text
                      x={centerX + (radius * 0.7) * Math.cos((midAngle * Math.PI) / 180)}
                      y={centerY + (radius * 0.7) * Math.sin((midAngle * Math.PI) / 180)}
                      textAnchor="middle"
                      fill="white"
                      fontSize="12"
                      fontWeight="bold"
                      style={{ pointerEvents: 'none' }}
                    >
                      {item.percentage.toFixed(0)}%
                    </text>
                  )}
                </g>
              );
            })}
          </svg>
          
          {showLegend && (
            <div className="chart-legend">
              {chartData.data.map((item, index) => (
                <div key={index} className="legend-item mb-2 d-flex align-items-center">
                  <div 
                    className="legend-color me-2"
                    style={{ 
                      width: '20px', 
                      height: '20px', 
                      backgroundColor: item.color,
                      border: '1px solid #ddd'
                    }}
                  />
                  <div className="legend-label flex-grow-1">{item.label}</div>
                  <div className="legend-value ms-3">{item.value.toLocaleString()}</div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    );
  };

  const containerClasses = classNames(
    'bf-enhanced-chart',
    className
  );

  return (
    <div className={containerClasses} style={style}>
      {chartType === 'bar' ? renderBarChart() : renderPieChart()}
      
      <style>{`
        .bf-enhanced-chart .chart-row.interactive:hover {
          background-color: #f8f9fa;
          border-radius: 4px;
        }
        .bf-enhanced-chart .chart-segment {
          transition: opacity 0.3s;
        }
        .bf-enhanced-chart .progress {
          background-color: #e9ecef;
        }
        .bf-enhanced-chart .progress-bar {
          font-size: 12px;
          display: flex;
          align-items: center;
          justify-content: center;
        }
      `}</style>
    </div>
  );
};

EnhancedChart.displayName = 'EnhancedChart';