import React, { useState } from 'react';
import { 
  BFormDomainProvider, 
  EntityRenderer,
  ReportRenderer,
  ReportViewerModal,
  EnhancedChart,
  HtmlEntityRenderer
} from '@bformdomain/react-renderer';
import { ReportInstance, HtmlInstance } from '@bformdomain/react-renderer/types';

/**
 * Example application demonstrating BFormDomain report rendering
 */
export const ReportViewerExample: React.FC = () => {
  const [selectedReport, setSelectedReport] = useState<ReportInstance | null>(null);
  const [showModal, setShowModal] = useState(false);

  // Sample report data (would come from your API)
  const sampleReport: ReportInstance = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    entityType: 'ReportInstance',
    template: 'SalesReport',
    title: 'Monthly Sales Report - March 2024',
    tenantId: 'tenant-001',
    createdDate: '2024-03-15T10:30:00Z',
    updatedDate: '2024-03-15T10:30:00Z',
    tags: ['sales', 'monthly', 'finance'],
    html: `
      <HTML><HEAD><TITLE>Report - Monthly Sales Report</TITLE></HEAD>
      <STYLE>
       .TableStyle { border-collapse: collapse } 
       .TitleStyle { font-family: Arial; font-size:15pt } 
       .SectionHeader {font-family: Arial; font-size:10pt } 
       .DetailHeader {font-family: Arial; font-size:9pt } 
       .DetailData  {font-family: Arial; font-size:9pt } 
       .ColumnHeaderStyle  {font-family: Arial; font-size:9pt; border-style:outset; border-width:1}
      </STYLE>
      <BODY>
      <TABLE Width='100%' style='FILTER: progid:DXImageTransform.Microsoft.Gradient(gradientType=1,startColorStr=#a9d4ff,endColorStr=#ffffff)' Cellpadding=5><TR><TD>
      <font face='Arial' size=6>Monthly Sales Report</font>
      </TD></TR></TABLE>
      
      <TABLE Width='100%' class='TableStyle' cellspacing=0 cellpadding=5 border=0>
      <TR><TD colspan='4' style="font-weight:bold; font-size:12pt; FILTER: progid:DXImageTransform.Microsoft.Gradient(gradientType=1,startColorStr=#f0f0f0,endColorStr=#ffffff)">
      Region: North America
      </TD></TR>
      
      <TR>
        <TD bgcolor='#e5e5e5' class='ColumnHeaderStyle'><b>Product</b></TD>
        <TD bgcolor='#e5e5e5' class='ColumnHeaderStyle'><b>Quantity</b></TD>
        <TD bgcolor='#e5e5e5' class='ColumnHeaderStyle'><b>Revenue</b></TD>
        <TD bgcolor='#e5e5e5' class='ColumnHeaderStyle'><b>Profit</b></TD>
      </TR>
      
      <TR>
        <TD class='DetailData'>Widget A</TD>
        <TD class='DetailData' align='right'>150</TD>
        <TD class='DetailData' align='right'>$15,000</TD>
        <TD class='DetailData' align='right'>$3,000</TD>
      </TR>
      
      <TR>
        <TD class='DetailData'>Widget B</TD>
        <TD class='DetailData' align='right'>200</TD>
        <TD class='DetailData' align='right'>$40,000</TD>
        <TD class='DetailData' align='right'>$8,000</TD>
      </TR>
      
      <TR>
        <TD class='DetailData'>Widget C</TD>
        <TD class='DetailData' align='right'>100</TD>
        <TD class='DetailData' align='right'>$25,000</TD>
        <TD class='DetailData' align='right'>$5,000</TD>
      </TR>
      
      <TR>
        <TD style="border-style:outline; border-width:1">&nbsp;</TD>
        <TD style="border-style:outline; border-width:1" align='right'><u>Total: 450</u></TD>
        <TD style="border-style:outline; border-width:1" align='right'><u>Total: $80,000</u></TD>
        <TD style="border-style:outline; border-width:1" align='right'><u>Total: $16,000</u></TD>
      </TR>
      
      <!--- Chart Table starts here -->
      <TR><TD colspan='4' align=CENTER>
      <TABLE border=0 cellspacing=5 width=550>
      <TR><TD class='DetailHeader' colspan=3 align='CENTER' width=550><B>Sales by Product</B></TD></TR>
      <TR>
       <TD Width=150 class='DetailData' align='right' bgColor='#e5e5e5'>Label</TD>
       <TD bgColor='#e5e5e5' class='DetailData' Width=325>Percentage</TD>
       <TD Width=25 class='DetailData' bgColor='#e5e5e5'>Value</TD>
      </TR>
      <TR><TD Width=150 class='DetailData' align='right'>Widget A</TD>
       <TD class='DetailData' Width=325>
          <TABLE cellpadding=0 cellspacing=0 HEIGHT='20' WIDTH=300 class='TableStyle'>
             <TR>
                <TD Width=56>
                   <TABLE class='TableStyle' HEIGHT='20' Width=58 border=1>
                      <TR>
                         <TD Width=56 bgcolor='#ff0000' style="FILTER: progid:DXImageTransform.Microsoft.Gradient(gradientType=0,startColorStr=#ff0000,endColorStr=#ffffff);"></TD>
                      </TR>
                   </TABLE>
                </TD>
                <TD class='DetailData'>&nbsp;18.8%</TD>
             </TR>
          </TABLE>
      </TD><TD Width=70 class='DetailData'>15000</TD></TR>
      </TABLE>
      </TD></TR>
      <!--- Chart Table ends here -->
      
      </TABLE></BODY></HTML>
    `
  };

  const sampleHtmlEntity: HtmlInstance = {
    id: '456e7890-e89b-12d3-a456-426614174001',
    entityType: 'HtmlInstance',
    templateName: 'RichContent',
    title: 'Product Overview',
    description: 'Detailed information about our product lineup',
    tenantId: 'tenant-001',
    createdDate: '2024-03-10T08:00:00Z',
    updatedDate: '2024-03-14T14:30:00Z',
    tags: ['product', 'documentation'],
    isPublished: true,
    content: `
      <h2>Our Product Portfolio</h2>
      <p>We offer a comprehensive range of widgets designed to meet various industry needs.</p>
      
      <div class="alert info">
        <strong>New!</strong> Widget C has been upgraded with enhanced features.
      </div>
      
      <h3>Product Comparison</h3>
      <table>
        <thead>
          <tr>
            <th>Product</th>
            <th>Features</th>
            <th>Price</th>
            <th>Availability</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td><strong>Widget A</strong></td>
            <td>Basic features, suitable for small businesses</td>
            <td>$100</td>
            <td>In Stock</td>
          </tr>
          <tr>
            <td><strong>Widget B</strong></td>
            <td>Advanced features, enterprise-ready</td>
            <td>$200</td>
            <td>In Stock</td>
          </tr>
          <tr>
            <td><strong>Widget C</strong></td>
            <td>Premium features, includes support</td>
            <td>$250</td>
            <td>Pre-order</td>
          </tr>
        </tbody>
      </table>
      
      <blockquote>
        "These widgets have transformed our business operations!" - Happy Customer
      </blockquote>
    `
  };

  return (
    <BFormDomainProvider>
      <div className="container py-4">
        <h1>BFormDomain Report Viewer Example</h1>
        
        {/* Example 1: Basic Report Rendering */}
        <section className="mb-5">
          <h2>1. Basic HTML Report Rendering</h2>
          <p>Direct rendering of HTML report with minimal enhancements:</p>
          
          <div className="border rounded p-3">
            <ReportRenderer
              report={sampleReport}
              enhanceMode="minimal"
              className="report-example"
            />
          </div>
        </section>

        {/* Example 2: Enhanced Report Rendering */}
        <section className="mb-5">
          <h2>2. Enhanced Report with Interactive Features</h2>
          <p>Full enhancement mode with collapsible sections and table extraction:</p>
          
          <div className="border rounded p-3">
            <ReportRenderer
              report={sampleReport}
              enhanceMode="full"
              enableTableInteractivity={true}
              onTableDataExtracted={(data) => {
                console.log('Table data extracted:', data);
              }}
              onSectionToggle={(index, expanded) => {
                console.log(`Section ${index} is now ${expanded ? 'expanded' : 'collapsed'}`);
              }}
            />
          </div>
        </section>

        {/* Example 3: Report Modal Viewer */}
        <section className="mb-5">
          <h2>3. Report Modal Viewer</h2>
          <p>Full-featured modal with print, export, and table view options:</p>
          
          <button 
            className="btn btn-primary"
            onClick={() => {
              setSelectedReport(sampleReport);
              setShowModal(true);
            }}
          >
            Open Report in Modal
          </button>
          
          {selectedReport && (
            <ReportViewerModal
              report={selectedReport}
              isOpen={showModal}
              onClose={() => setShowModal(false)}
              enableEnhancements={true}
            />
          )}
        </section>

        {/* Example 4: Entity Renderer with Plugin */}
        <section className="mb-5">
          <h2>4. Universal Entity Renderer</h2>
          <p>Using the plugin system to automatically render different entity types:</p>
          
          <div className="row">
            <div className="col-md-6">
              <h4>Report Entity</h4>
              <div className="border rounded p-3">
                <EntityRenderer
                  entity={sampleReport}
                  entityType="ReportInstance"
                  renderer="enhanced-report"
                />
              </div>
            </div>
            
            <div className="col-md-6">
              <h4>HTML Entity</h4>
              <div className="border rounded p-3">
                <EntityRenderer
                  entity={sampleHtmlEntity}
                  entityType="HtmlInstance"
                />
              </div>
            </div>
          </div>
        </section>

        {/* Example 5: HTML Entity Direct Rendering */}
        <section className="mb-5">
          <h2>5. HTML Entity Renderer</h2>
          <p>Direct rendering of HTML content with Bootstrap enhancement:</p>
          
          <div className="border rounded p-3">
            <HtmlEntityRenderer
              entity={sampleHtmlEntity}
              enableBootstrapClasses={true}
              onContentClick={(e) => {
                console.log('Content clicked:', e);
              }}
            />
          </div>
        </section>

        {/* Example 6: Chart Enhancement */}
        <section className="mb-5">
          <h2>6. Enhanced Chart from HTML</h2>
          <p>Converting HTML bar charts to interactive React components:</p>
          
          <div className="row">
            <div className="col-md-6">
              <h4>Bar Chart</h4>
              <EnhancedChart
                htmlChartContent={sampleReport.html}
                chartType="bar"
                interactive={true}
                onSegmentClick={(data) => {
                  alert(`Clicked: ${data.label} - ${data.value}`);
                }}
              />
            </div>
            
            <div className="col-md-6">
              <h4>Pie Chart</h4>
              <EnhancedChart
                htmlChartContent={sampleReport.html}
                chartType="pie"
                interactive={true}
                showLegend={true}
                onSegmentClick={(data) => {
                  alert(`Clicked: ${data.label} - ${data.percentage}%`);
                }}
              />
            </div>
          </div>
        </section>
      </div>
    </BFormDomainProvider>
  );
};