import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { KpiRenderer, KpiDashboard } from '../../src/renderers/kpis/KpiRenderer';
import { KpiInstance } from '../../src/types';

describe('KpiRenderer', () => {
  const mockKpi: KpiInstance = {
    id: '1',
    templateId: 'template-1',
    templateName: 'Sales KPI',
    name: 'Monthly Sales',
    description: 'Total sales for the current month',
    category: 'Sales',
    unitOfMeasure: 'USD',
    formatString: '$#,##0.00',
    targetValue: 100000,
    positiveDirection: 'up',
    rowData: [
      { date: new Date('2024-01-01'), value: 80000 },
      { date: new Date('2024-02-01'), value: 95000 }
    ],
    tags: ['sales', 'monthly'],
    createdDate: new Date('2024-01-01'),
    modifiedDate: new Date('2024-02-01')
  };

  describe('Card variant', () => {
    it('renders KPI in card format', () => {
      render(<KpiRenderer kpi={mockKpi} variant="card" />);
      
      expect(screen.getByText('Monthly Sales')).toBeInTheDocument();
      expect(screen.getByText('$95,000.00')).toBeInTheDocument();
      expect(screen.getByText('USD')).toBeInTheDocument();
    });

    it('shows trend information', () => {
      render(<KpiRenderer kpi={mockKpi} showTrend={true} />);
      
      expect(screen.getByText(/18.8%/)).toBeInTheDocument(); // (95000-80000)/80000 * 100
    });

    it('shows target status', () => {
      render(<KpiRenderer kpi={mockKpi} showTarget={true} />);
      
      expect(screen.getByText('Target:')).toBeInTheDocument();
      expect(screen.getByText('$100,000.00')).toBeInTheDocument();
    });

    it('handles click events', () => {
      const handleClick = jest.fn();
      render(<KpiRenderer kpi={mockKpi} onClick={handleClick} />);
      
      const card = screen.getByRole('button');
      fireEvent.click(card);
      
      expect(handleClick).toHaveBeenCalledTimes(1);
    });
  });

  describe('Inline variant', () => {
    it('renders KPI in inline format', () => {
      render(<KpiRenderer kpi={mockKpi} variant="inline" />);
      
      expect(screen.getByText('Monthly Sales')).toBeInTheDocument();
      expect(screen.getByText('$95,000.00')).toBeInTheDocument();
    });
  });

  describe('Compact variant', () => {
    it('renders KPI in compact format', () => {
      render(<KpiRenderer kpi={mockKpi} variant="compact" />);
      
      expect(screen.getByText('Monthly Sales')).toBeInTheDocument();
      expect(screen.getByText('$95,000.00')).toBeInTheDocument();
    });
  });
});

describe('KpiDashboard', () => {
  const mockKpis: KpiInstance[] = [
    {
      id: '1',
      templateId: 'template-1',
      templateName: 'Sales KPI',
      name: 'Monthly Sales',
      unitOfMeasure: 'USD',
      formatString: '$#,##0.00',
      targetValue: 100000,
      positiveDirection: 'up',
      rowData: [{ date: new Date(), value: 95000 }],
      tags: [],
      createdDate: new Date(),
      modifiedDate: new Date()
    },
    {
      id: '2',
      templateId: 'template-2',
      templateName: 'Customer KPI',
      name: 'Active Customers',
      unitOfMeasure: '',
      formatString: '#,##0',
      targetValue: 1000,
      positiveDirection: 'up',
      rowData: [{ date: new Date(), value: 850 }],
      tags: [],
      createdDate: new Date(),
      modifiedDate: new Date()
    }
  ];

  it('renders multiple KPIs in a grid', () => {
    render(<KpiDashboard kpis={mockKpis} columns={2} />);
    
    expect(screen.getByText('Monthly Sales')).toBeInTheDocument();
    expect(screen.getByText('Active Customers')).toBeInTheDocument();
  });

  it('handles KPI click events', () => {
    const handleKpiClick = jest.fn();
    render(<KpiDashboard kpis={mockKpis} onKpiClick={handleKpiClick} />);
    
    const cards = screen.getAllByRole('button');
    fireEvent.click(cards[0]);
    
    expect(handleKpiClick).toHaveBeenCalledWith(mockKpis[0]);
  });

  it('respects column configuration', () => {
    const { container } = render(<KpiDashboard kpis={mockKpis} columns={3} />);
    
    const columns = container.querySelectorAll('.col-md-4');
    expect(columns.length).toBe(2); // 2 KPIs with col-md-4 (12/3 = 4)
  });
});