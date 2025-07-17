import React, { useState } from 'react';
import { ReportInstance } from '../../types';
import { ReportRenderer } from './ReportRenderer';
import { AgGridReact } from '@ag-grid-community/react';
import { ColDef } from '@ag-grid-community/core';
import { TableData } from '../../utils/htmlSanitizer';

interface ReportViewerModalProps {
  report: ReportInstance;
  isOpen: boolean;
  onClose: () => void;
  enableEnhancements?: boolean;
}

/**
 * Modal component for viewing reports with optional interactive features
 */
export const ReportViewerModal: React.FC<ReportViewerModalProps> = ({
  report,
  isOpen,
  onClose,
  enableEnhancements = true
}) => {
  const [viewMode, setViewMode] = useState<'html' | 'table' | 'print'>('html');
  const [tableData, setTableData] = useState<TableData | null>(null);

  if (!isOpen) return null;

  const handlePrint = () => {
    const printWindow = window.open('', '_blank');
    if (printWindow) {
      printWindow.document.write(`
        <html>
          <head>
            <title>${report.title || 'Report'}</title>
            <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
            <style>
              @media print {
                .no-print { display: none !important; }
              }
              body { font-family: Arial, sans-serif; }
            </style>
          </head>
          <body>
            ${report.html}
          </body>
        </html>
      `);
      printWindow.document.close();
      printWindow.print();
    }
  };

  const handleExport = () => {
    const blob = new Blob([report.html], { type: 'text/html' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${report.title || 'report'}_${new Date().toISOString()}.html`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  const generateAgGridColumns = (headers: string[]): ColDef[] => {
    return headers.map(header => ({
      field: header,
      headerName: header,
      sortable: true,
      filter: true,
      resizable: true,
      minWidth: 100,
      flex: 1
    }));
  };

  return (
    <>
      {/* Modal Backdrop */}
      <div 
        className="modal-backdrop show" 
        onClick={onClose}
        style={{ zIndex: 1040 }}
      />

      {/* Modal */}
      <div 
        className="modal show d-block" 
        tabIndex={-1}
        style={{ zIndex: 1050 }}
      >
        <div className="modal-dialog modal-xl modal-dialog-scrollable">
          <div className="modal-content">
            {/* Header */}
            <div className="modal-header">
              <h5 className="modal-title">{report.title || 'Report Viewer'}</h5>
              <div className="ms-auto d-flex gap-2">
                {/* View Mode Buttons */}
                <div className="btn-group" role="group">
                  <button
                    type="button"
                    className={`btn btn-sm ${viewMode === 'html' ? 'btn-primary' : 'btn-outline-primary'}`}
                    onClick={() => setViewMode('html')}
                  >
                    HTML View
                  </button>
                  {tableData && (
                    <button
                      type="button"
                      className={`btn btn-sm ${viewMode === 'table' ? 'btn-primary' : 'btn-outline-primary'}`}
                      onClick={() => setViewMode('table')}
                    >
                      Table View
                    </button>
                  )}
                </div>

                {/* Action Buttons */}
                <button
                  type="button"
                  className="btn btn-sm btn-outline-secondary"
                  onClick={handlePrint}
                >
                  <i className="bi bi-printer"></i> Print
                </button>
                <button
                  type="button"
                  className="btn btn-sm btn-outline-secondary"
                  onClick={handleExport}
                >
                  <i className="bi bi-download"></i> Export
                </button>
                <button
                  type="button"
                  className="btn-close"
                  onClick={onClose}
                />
              </div>
            </div>

            {/* Body */}
            <div className="modal-body">
              {viewMode === 'html' && (
                <ReportRenderer
                  report={report}
                  enhanceMode={enableEnhancements ? 'full' : 'minimal'}
                  enableTableInteractivity={true}
                  onTableDataExtracted={setTableData}
                />
              )}

              {viewMode === 'table' && tableData && (
                <div style={{ height: '600px' }}>
                  <AgGridReact
                    rowData={tableData.rows}
                    columnDefs={generateAgGridColumns(tableData.headers)}
                    defaultColDef={{
                      sortable: true,
                      filter: true,
                      resizable: true
                    }}
                    pagination={true}
                    paginationPageSize={20}
                    animateRows={true}
                  />
                  
                  {tableData.hasTotal && tableData.totalRow && (
                    <div className="mt-3 p-3 bg-light border rounded">
                      <h6>Totals</h6>
                      <div className="row">
                        {Object.entries(tableData.totalRow).map(([key, value]) => (
                          <div key={key} className="col-auto">
                            <strong>{key}:</strong> {value}
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Footer */}
            <div className="modal-footer">
              <button
                type="button"
                className="btn btn-secondary"
                onClick={onClose}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

ReportViewerModal.displayName = 'ReportViewerModal';