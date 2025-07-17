#pragma warning disable CS0168
#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8618
#pragma warning disable CS8625

using System.Collections;
using System.Data;
using System.Drawing;
using System.Text;

/****************************************************************************
 *							HTML Report Engine								*
 ****************************************************************************
 * Description	:	The engine helps in creating formatted report as HTML	*
 *					files from given DataSet.								*
 * Author		:	Ambalavanar Thirugnanam									*
 * Email		:	ambalavanar.thiru@gmail.com								*
 * Date			:	01 June 2006											*
 ****************************************************************************

Portions based on "The HTML Report Engine" by Ambalavanar Thirugnanam
https://www.codeproject.com/Articles/14320/The-HTML-Report-Engine

The HTML report engine is provided under the terms of the Code Project Open License (CPOL) 1.2.
https://www.codeproject.com/info/cpol10.aspx

Preamble
This License governs Your use of the Work. This License is intended to allow developers to use the Source Code and Executable Files provided as part of the Work in any application in any form.

The main points subject to the terms of the License are:

Source Code and Executable Files can be used in commercial applications;
Source Code and Executable Files can be redistributed; and
Source Code can be modified to create derivative works.
No claim of suitability, guarantee, or any warranty whatsoever is provided. The software is provided "as-is".
The Article(s) accompanying the Work may not be distributed or republished without the Author's consent
This License is entered between You, the individual or other entity reading or otherwise making use of the Work licensed pursuant to this License and the individual or other entity which offers the Work under the terms of this License ("Author").

License
THE WORK (AS DEFINED BELOW) IS PROVIDED UNDER THE TERMS OF THIS CODE PROJECT OPEN LICENSE ("LICENSE"). THE WORK IS PROTECTED BY COPYRIGHT AND/OR OTHER APPLICABLE LAW. ANY USE OF THE WORK OTHER THAN AS AUTHORIZED UNDER THIS LICENSE OR COPYRIGHT LAW IS PROHIBITED.

BY EXERCISING ANY RIGHTS TO THE WORK PROVIDED HEREIN, YOU ACCEPT AND AGREE TO BE BOUND BY THE TERMS OF THIS LICENSE. THE AUTHOR GRANTS YOU THE RIGHTS CONTAINED HEREIN IN CONSIDERATION OF YOUR ACCEPTANCE OF SUCH TERMS AND CONDITIONS. IF YOU DO NOT AGREE TO ACCEPT AND BE BOUND BY THE TERMS OF THIS LICENSE, YOU CANNOT MAKE ANY USE OF THE WORK.

Definitions.
"Articles" means, collectively, all articles written by Author which describes how the Source Code and Executable Files for the Work may be used by a user.
"Author" means the individual or entity that offers the Work under the terms of this License.
"Derivative Work" means a work based upon the Work or upon the Work and other pre-existing works.
"Executable Files" refer to the executables, binary files, configuration and any required data files included in the Work.
"Publisher" means the provider of the website, magazine, CD-ROM, DVD or other medium from or by which the Work is obtained by You.
"Source Code" refers to the collection of source code and configuration files used to create the Executable Files.
"Standard Version" refers to such a Work if it has not been modified, or has been modified in accordance with the consent of the Author, such consent being in the full discretion of the Author.
"Work" refers to the collection of files distributed by the Publisher, including the Source Code, Executable Files, binaries, data files, documentation, whitepapers and the Articles.
"You" is you, an individual or entity wishing to use the Work and exercise your rights under this License.
Fair Use/Fair Use Rights. Nothing in this License is intended to reduce, limit, or restrict any rights arising from fair use, fair dealing, first sale or other limitations on the exclusive rights of the copyright owner under copyright law or other applicable laws.
License Grant. Subject to the terms and conditions of this License, the Author hereby grants You a worldwide, royalty-free, non-exclusive, perpetual (for the duration of the applicable copyright) license to exercise the rights in the Work as stated below:
You may use the standard version of the Source Code or Executable Files in Your own applications.
You may apply bug fixes, portability fixes and other modifications obtained from the Public Domain or from the Author. A Work modified in such a way shall still be considered the standard version and will be subject to this License.
You may otherwise modify Your copy of this Work (excluding the Articles) in any way to create a Derivative Work, provided that You insert a prominent notice in each changed file stating how, when and where You changed that file.
You may distribute the standard version of the Executable Files and Source Code or Derivative Work in aggregate with other (possibly commercial) programs as part of a larger (possibly commercial) software distribution.
The Articles discussing the Work published in any form by the author may not be distributed or republished without the Author's consent. The author retains copyright to any such Articles. You may use the Executable Files and Source Code pursuant to this License but you may not repost or republish or otherwise distribute or make available the Articles, without the prior written consent of the Author.
Any subroutines or modules supplied by You and linked into the Source Code or Executable Files of this Work shall not be considered part of this Work and will not be subject to the terms of this License.
Patent License. Subject to the terms and conditions of this License, each Author hereby grants to You a perpetual, worldwide, non-exclusive, no-charge, royalty-free, irrevocable (except as stated in this section) patent license to make, have made, use, import, and otherwise transfer the Work.
Restrictions. The license granted in Section 3 above is expressly made subject to and limited by the following restrictions:
You agree not to remove any of the original copyright, patent, trademark, and attribution notices and associated disclaimers that may appear in the Source Code or Executable Files.
You agree not to advertise or in any way imply that this Work is a product of Your own.
The name of the Author may not be used to endorse or promote products derived from the Work without the prior written consent of the Author.
You agree not to sell, lease, or rent any part of the Work. This does not restrict you from including the Work or any part of the Work inside a larger software distribution that itself is being sold. The Work by itself, though, cannot be sold, leased or rented.
You may distribute the Executable Files and Source Code only under the terms of this License, and You must include a copy of, or the Uniform Resource Identifier for, this License with every copy of the Executable Files or Source Code You distribute and ensure that anyone receiving such Executable Files and Source Code agrees that the terms of this License apply to such Executable Files and/or Source Code. You may not offer or impose any terms on the Work that alter or restrict the terms of this License or the recipients' exercise of the rights granted hereunder. You may not sublicense the Work. You must keep intact all notices that refer to this License and to the disclaimer of warranties. You may not distribute the Executable Files or Source Code with any technological measures that control access or use of the Work in a manner inconsistent with the terms of this License.
You agree not to use the Work for illegal, immoral or improper purposes, or on pages containing illegal, immoral or improper material. The Work is subject to applicable export laws. You agree to comply with all such laws and regulations that may apply to the Work after Your receipt of the Work.
Representations, Warranties and Disclaimer. THIS WORK IS PROVIDED "AS IS", "WHERE IS" AND "AS AVAILABLE", WITHOUT ANY EXPRESS OR IMPLIED WARRANTIES OR CONDITIONS OR GUARANTEES. YOU, THE USER, ASSUME ALL RISK IN ITS USE, INCLUDING COPYRIGHT INFRINGEMENT, PATENT INFRINGEMENT, SUITABILITY, ETC. AUTHOR EXPRESSLY DISCLAIMS ALL EXPRESS, IMPLIED OR STATUTORY WARRANTIES OR CONDITIONS, INCLUDING WITHOUT LIMITATION, WARRANTIES OR CONDITIONS OF MERCHANTABILITY, MERCHANTABLE QUALITY OR FITNESS FOR A PARTICULAR PURPOSE, OR ANY WARRANTY OF TITLE OR NON-INFRINGEMENT, OR THAT THE WORK (OR ANY PORTION THEREOF) IS CORRECT, USEFUL, BUG-FREE OR FREE OF VIRUSES. YOU MUST PASS THIS DISCLAIMER ON WHENEVER YOU DISTRIBUTE THE WORK OR DERIVATIVE WORKS.
Indemnity. You agree to defend, indemnify and hold harmless the Author and the Publisher from and against any claims, suits, losses, damages, liabilities, costs, and expenses (including reasonable legal or attorneys’ fees) resulting from or relating to any use of the Work by You.
Limitation on Liability. EXCEPT TO THE EXTENT REQUIRED BY APPLICABLE LAW, IN NO EVENT WILL THE AUTHOR OR THE PUBLISHER BE LIABLE TO YOU ON ANY LEGAL THEORY FOR ANY SPECIAL, INCIDENTAL, CONSEQUENTIAL, PUNITIVE OR EXEMPLARY DAMAGES ARISING OUT OF THIS LICENSE OR THE USE OF THE WORK OR OTHERWISE, EVEN IF THE AUTHOR OR THE PUBLISHER HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGES.
Termination.
This License and the rights granted hereunder will terminate automatically upon any breach by You of any term of this License. Individuals or entities who have received Derivative Works from You under this License, however, will not have their licenses terminated provided such individuals or entities remain in full compliance with those licenses. Sections 1, 2, 6, 7, 8, 9, 10 and 11 will survive any termination of this License.
If You bring a copyright, trademark, patent or any other infringement claim against any contributor over infringements You claim are made by the Work, your License from such contributor to the Work ends automatically.
Subject to the above terms and conditions, this License is perpetual (for the duration of the applicable copyright in the Work). Notwithstanding the above, the Author reserves the right to release the Work under different license terms or to stop distributing the Work at any time; provided, however that any such election will not serve to withdraw this License (or any other license that has been, or is required to be, granted under the terms of this License), and this License will continue in full force and effect unless terminated as stated above.
Publisher.The parties hereby confirm that the Publisher shall not, under any circumstances, be responsible for and shall not have any liability in respect of the subject matter of this License.The Publisher makes no warranty whatsoever in connection with the Work and shall not be liable to You or any party on any legal theory for any damages whatsoever, including without limitation any general, special, incidental or consequential damages arising in connection to this license.The Publisher reserves the right to cease making the Work available to You at any time without notice
Miscellaneous
This License shall be governed by the laws of the location of the head office of the Author or if the Author is an individual, the laws of location of the principal place of residence of the Author.
If any provision of this License is invalid or unenforceable under applicable law, it shall not affect the validity or enforceability of the remainder of the terms of this License, and without further action by the parties to this License, such provision shall be reformed to the minimum extent necessary to make such provision valid and enforceable.
No term or provision of this License shall be deemed waived and no breach consented to unless such waiver or consent shall be in writing and signed by the party to be charged with such waiver or consent.
This License constitutes the entire agreement between the parties with respect to the Work licensed herein. There are no understandings, agreements or representations with respect to the Work not specified herein.The Author shall not be bound by any additional provisions that may appear in any communication from You. This License may not be modified without the mutual written agreement of the Author and You.

*/



namespace HTMLReportEngine
{
    /// <summary>
    /// Holds the report details and Methods to generate report.
    /// </summary>
    public class Report
	{
		#region Class level variables
		private DataSet reportSource;
		private ArrayList sections;
		private string reportTitle;
		private string newline;
		private ArrayList reportFields;
		private int iLevel = 0;
		private string gradientStyle;
		private StringBuilder htmlContent;
		private Hashtable totalList;


		public string ReportFont;
		public ArrayList TotalFields;
		public bool IncludeTotal;

		//Chart fields
		public bool IncludeChart;
		public string ChartTitle;
		public bool ChartShowAtBottom;
		public string ChartChangeOnField;
		public string ChartValueField = "Count";
		public bool ChartShowBorder;
		public string ChartLabelHeader = "Label";
		public string ChartPercentageHeader = "Percentage";
		public string ChartValueHeader = "Value";
		#endregion

		//Constructor 
		public Report()
		{
			htmlContent = new StringBuilder();
			newline = "\n";
			sections = new ArrayList();
			reportFields = new ArrayList();
			ReportFont = "Arial";
			gradientStyle = "FILTER: progid:DXImageTransform.Microsoft.Gradient(gradientType=1,startColorStr=BackColor,endColorStr=#ffffff)";
			totalList = new Hashtable();
			TotalFields = new ArrayList();
		}

		#region Public Properties
		/// <summary>
		/// Gets or Sets report source as DataSet.
		/// </summary>
		public DataSet ReportSource
		{
			get { return reportSource; }
			set { reportSource = value; }
		}

		/// <summary>
		/// Gets or Sets Report sections as ArrayList. Contains of objects of Section class.
		/// </summary>
		public ArrayList Sections
		{
			get { return sections; }
			set { sections = value; }
		}

		/// <summary>
		/// Gets or Sets Report title as string.
		/// </summary>
		public string ReportTitle
		{
			get { return reportTitle; }
			set { reportTitle = value; }
		}

		/// <summary>
		/// Gets or Sets report fields as ArrayList. Contains objects of Field class.
		/// </summary>
		public ArrayList ReportFields
		{
			get { return reportFields; }
			set { reportFields = value; }
		}
		#endregion

		#region Methods
		/// <summary>
		/// Generates the HTML Content for the given ReportSource.
		/// </summary>
		/// <returns>HTML String</returns>
		public string GenerateReport()
		{
			foreach (Field fld in this.ReportFields)
			{
				if (!this.TotalFields.Contains(fld.FieldName) && fld.isTotalField)
				{
					TotalFields.Add(fld.FieldName);
				}
			}
			WriteTitle();
			WriteSections();
			WriteFooter();
			return htmlContent.ToString();
		}

		/// <summary>
		/// Generates and Saves the report into a file.
		/// </summary>
		/// <param name="fileName">HTML Report file name</param>
		/// <returns>On success returns true</returns>
		public bool SaveReport(string fileName)
		{
			try
			{
				GenerateReport();
				StreamWriter sw = new StreamWriter(fileName);
				sw.Write(htmlContent.ToString());
				sw.Flush();
				sw.Close();
				return true;
			}
			catch (Exception exp)
			{
				System.Diagnostics.Debug.WriteLine(exp.Message);
				return false;
			}
		}

		/// <summary>
		/// Writes CSS and HTML title.
		/// </summary>
		private void WriteTitle()
		{
			htmlContent.Append("<HTML><HEAD><TITLE>Report - " + reportTitle + "</TITLE></HEAD>" + newline);
			htmlContent.Append("<STYLE>" + newline);
			htmlContent.Append(" .TableStyle { border-collapse: collapse } " + newline);
			htmlContent.Append(" .TitleStyle { font-family: " + ReportFont + "; font-size:15pt } " + newline);
			htmlContent.Append(" .SectionHeader {font-family: " + ReportFont + "; font-size:10pt } " + newline);
			htmlContent.Append(" .DetailHeader {font-family: " + ReportFont + "; font-size:9pt } " + newline);
			htmlContent.Append(" .DetailData  {font-family: " + ReportFont + "; font-size:9pt } " + newline);
			htmlContent.Append(" .ColumnHeaderStyle  {font-family: " + ReportFont + "; font-size:9pt; border-style:outset; border-width:1} " + newline);
			htmlContent.Append("</STYLE>" + newline);
			htmlContent.Append("<BODY TOPMARGIN=0 LEFTMARGIN=0 RIGHTMARGIN=0 BOTTOMMARGIN=0>" + newline);
			htmlContent.Append("<TABLE Width='100%' style='FILTER: progid:DXImageTransform.Microsoft.Gradient(gradientType=1,startColorStr=#a9d4ff,endColorStr=#ffffff)' Cellpadding=5><TR><TD>");
			htmlContent.Append("<font face='" + ReportFont + "' size=6>" + ReportTitle + "</font>");
			htmlContent.Append("</TD></TR></TABLE>" + newline);
		}

		/// <summary>
		/// Generates all section contents
		/// </summary>
		private void WriteSections()
		{
			if (sections.Count == 0)
			{
				Section dummySection = new Section();
				dummySection.Level = 5;
				dummySection.ChartChangeOnField = this.ChartChangeOnField;
				dummySection.ChartLabelHeader = this.ChartLabelHeader;
				dummySection.ChartPercentageHeader = this.ChartPercentageHeader;
				dummySection.ChartShowAtBottom = this.ChartShowAtBottom;
				dummySection.ChartShowBorder = this.ChartShowAtBottom;
				dummySection.ChartTitle = this.ChartTitle;
				dummySection.ChartValueField = this.ChartValueField;
				dummySection.ChartValueHeader = this.ChartValueHeader;
				dummySection.IncludeChart = this.IncludeChart;
				htmlContent.Append("<TABLE Width='100%' class='TableStyle'  cellspacing=0 cellpadding=5 border=0>" + newline);
				if (this.IncludeChart && !this.ChartShowAtBottom)
					GenerateBarChart("", dummySection);
				Hashtable total = WriteSectionDetail(null, "");
				if (this.IncludeTotal)
				{
					dummySection.IncludeTotal = true;
					WriteSectionFooter(dummySection, total);
				}
				if (this.IncludeChart && this.ChartShowAtBottom)
					GenerateBarChart("", dummySection);
				htmlContent.Append("</TABLE></BODY></HTML>");
			}
			foreach (Section section in sections)
			{
				iLevel = 0;
				htmlContent.Append("<TABLE Width='100%' class='TableStyle'  cellspacing=0 cellpadding=5 border=0>" + newline);
				RecurseSections(section, "");
				htmlContent.Append("</TABLE></BODY></HTML>");
			}
		}

		/// <summary>
		/// Writes the section header information.
		/// </summary>
		/// <param name="section">The section details as Section object</param>
		/// <param name="sectionValue">section group field data</param>
		private void WriteSectionHeader(Section section, string sectionValue)
		{
			string bg = section.backColor;
			string style = " style=\"font-family: " + ReportFont + "; font-weight:bold; font-size:";
			style += getFontSize(section.Level);
			if (section.GradientBackground)
				style += "; " + gradientStyle.Replace("BackColor", bg) + "\"";
			else style += "\" bgcolor='" + bg + "' ";

			htmlContent.Append("<TR><TD colspan='" + this.ReportFields.Count + "' " + style + " >");
			htmlContent.Append(section.TitlePrefix + sectionValue);
			htmlContent.Append("</TD></TR>" + newline);
		}

		/// <summary>
		/// Method to write Chart and Section data information
		/// </summary>
		/// <param name="section">the section details</param>
		/// <param name="criteria">the section selection criteria</param>
		private Hashtable WriteSectionDetail(Section section, string criteria)
		{
			Hashtable totalArray = new Hashtable();
			totalArray = PrepareData(totalArray);
			if (section == null)
			{
				section = new Section();
			}
			try
			{
				//Draw DetailHeader
				htmlContent.Append("<TR>" + newline);
				string cellParams = "";
				foreach (Field field in this.reportFields)
				{
					cellParams = " bgcolor='" + field.headerBackColor + "' ";
					if (field.Width != 0)
						cellParams += " WIDTH=" + field.Width + " ";
					cellParams += " ALIGN='" + field.alignment + "' ";
					htmlContent.Append("  <TD " + cellParams + " class='ColumnHeaderStyle'><b>" + field.HeaderName + "</b></TD>" + newline);
				}
				htmlContent.Append("</TR>" + newline);

				//Draw Data
				if (criteria == null || criteria.Trim() == "")
					criteria = "";
				else
					criteria = criteria.Substring(3);


				foreach (DataRow dr in reportSource.Tables[0].Select(criteria))
				{
					htmlContent.Append("<TR>" + newline);
					foreach (Field field in this.reportFields)
					{
						cellParams = " bgcolor='" + field.backColor + "' ";
						if (field.Width != 0)
							cellParams += " WIDTH=" + field.Width + " ";
						//if total field, by default set to RIGHT align.
						if (this.TotalFields.Contains(field.FieldName))
							cellParams += " align='right' ";
						cellParams += " ALIGN='" + field.alignment + "' ";
						htmlContent.Append("  <TD " + cellParams + " VALIGN='top' class='DetailData'>" + dr[field.FieldName] + "</TD>" + newline);
					}
					htmlContent.Append("</TR>" + newline);
					try
					{
						foreach (object totalField in TotalFields)
						{
							totalArray[totalField.ToString()] = float.Parse(totalArray[totalField.ToString()].ToString()) +
								float.Parse(dr[totalField.ToString()].ToString());
						}
					}
					catch (Exception exp)
					{
						;//to-do: show error message at total field
					}
				}
			}
			catch (Exception err)
			{
				htmlContent.Append("<p align=CENTER><b>Unable to generate report.<br>" + err.Message + "</b></p>");
			}
			return totalArray;
		}

		/// <summary>
		/// Method to write section footer information.
		/// </summary>
		/// <param name="section">The section details</param></param>
		private void WriteSectionFooter(Section section, Hashtable totalArray)
		{
			string cellParams = "";
			//Include Total row if specified.
			if (section.IncludeTotal)
			{
				htmlContent.Append("<TR>" + newline);
				foreach (Field field in this.reportFields)
				{
					cellParams = "";
					if (field.Width != 0)
						cellParams += " WIDTH=" + field.Width + " ";
					cellParams += " style=\"font-family: " + ReportFont + "; font-size:";
					cellParams += getFontSize(section.Level + 1) + "; border-style:outline; border-width:1 \" ";
					if (totalArray.Contains(field.FieldName))
					{
						htmlContent.Append("  <TD " + cellParams + " align='right'><u>Total: " + totalArray[field.FieldName].ToString() + "</u></TD> " + newline);
					}
					else
					{
						htmlContent.Append("  <TD " + cellParams + ">&nbsp;</TD>" + newline);
					}
				}
				htmlContent.Append("</TR>");
			}
		}

		/// <summary>
		/// Writes the HTML closing tags
		/// </summary>
		private void WriteFooter()
		{
			htmlContent.Append("<BR>");
		}

		/// <summary>
		/// A recursive funtion to write all the section headers, details and footer content.
		/// </summary>
		/// <param name="section">the section details</param>
		/// <param name="criteria">section data selection criteria</param>
		private Hashtable RecurseSections(Section section, string criteria)
		{
			iLevel++;
			section.Level = iLevel;
			ArrayList result = GetDistinctValues(this.reportSource, section.GroupBy, criteria);
			Hashtable ht = new Hashtable();
			PrepareData(ht);
			foreach (object obj in result)
			{
				Hashtable sectionTotal = new Hashtable();
				PrepareData(sectionTotal);
				//Construct critiera string to select data for the current section
				string tcriteria = criteria + "and " + section.GroupBy + "='" + obj.ToString().Replace("'", "''") + "' ";
				WriteSectionHeader(section, obj.ToString());
				//If user not specified to display chart at bottom of the section
				if (section.IncludeChart && !section.ChartShowAtBottom && !section.isChartCreated)
					GenerateBarChart(tcriteria, section);
				if (section.SubSection != null)
				{
					sectionTotal = RecurseSections(section.SubSection, tcriteria);
					iLevel--;
				}
				else
				{
					sectionTotal = WriteSectionDetail(section, tcriteria);
					ht = AccumulateTotal(ht, sectionTotal);
				}
				//If user specified to display chart at bottom of the section
				WriteSectionFooter(section, sectionTotal);
				if (section.IncludeChart && section.ChartShowAtBottom && !section.isChartCreated)
					GenerateBarChart(tcriteria, section);
				section.isChartCreated = false;
			}
			if (section.Level < 2)
				htmlContent.Append("<TR><TD colspan='" + this.ReportFields.Count + "'>&nbsp;</TD></TR>");

			return ht;
		}

		/// <summary>
		/// Method to generate BarChart
		/// </summary>
		/// <param name="criteria">Section data selection criteria</param>
		/// <param name="changeOnField">Y-Axis data field</param>
		/// <param name="valueField">X-Axis data field (Send "count" as value for reporting record count)</param>
		/// <param name="showBorder">Enable or disable chart border</param>
		private void GenerateBarChart(string criteria, Section section)
		{
			string changeOnField = section.ChartChangeOnField;
			string valueField = section.ChartValueField;
			bool showBorder = section.ChartShowBorder;
			section.isChartCreated = true;
			string[] colors = { "#ff0000", "#ffff00", "#ff00ff", "#00ff00", "#00ffff", "#0000ff", "#ff0f0f", "#f0f000", "#ff00f0", "#0f00f0" };
			htmlContent.Append("<TR><TD colspan='" + this.ReportFields.Count + "' align=CENTER>" + newline);
			htmlContent.Append("<!--- Chart Table starts here -->" + newline);
			if (showBorder)
			{
				htmlContent.Append("<TABLE cellpadding=4 cellspacing=1 border=0 bgcolor='#f5f5f5' width=550> ");
			}
			else
			{
				htmlContent.Append("<TABLE border=0 cellspacing=5 width=550>");
			}
			if (criteria.ToUpper().StartsWith(" AND "))
			{
				criteria = criteria.Substring(3);
			}
			try
			{
				ArrayList result = GetDistinctValuesForChart(this.reportSource, criteria, changeOnField, valueField);
				ArrayList labels = (ArrayList)result[0];
				ArrayList values = (ArrayList)result[1];
				float total = 0;
				foreach (Object obj in values)
				{
					total += float.Parse(obj.ToString());
				}
				int ChartWidth = 300;

				string barTitle = "<TR><TD class='DetailHeader' colspan=3 align='CENTER' width=550><B>ChartTitle</B></TD></TR>";
				htmlContent.Append(barTitle.Replace("ChartTitle", section.ChartTitle) + newline);

				string barTemplate = "<TR><TD Width=150 class='DetailData' align='right'>Label</TD>" + newline;
				barTemplate += " <TD  class='DetailData' Width=" + (ChartWidth + 25) + ">" + newline;
				barTemplate += "    <TABLE cellpadding=0 cellspacing=0 HEIGHT='20' WIDTH=" + ChartWidth + " class='TableStyle'>" + newline;
				barTemplate += "       <TR>" + newline;
				barTemplate += "          <TD Width=ChartWidth>" + newline;
				barTemplate += "             <TABLE class='TableStyle' HEIGHT='20' Width=ChartTWidth border=NOTZERO>" + newline;
				barTemplate += "                <TR>" + newline;
				barTemplate += "                   <TD Width=ChartWidth bgcolor='BackColor' Width=ChartWidth style=\"FILTER: progid:DXImageTransform.Microsoft.Gradient(gradientType=0,startColorStr=BackColor,endColorStr=#ffffff); \"></TD>" + newline;
				barTemplate += "                </TR>" + newline;
				barTemplate += "             </TABLE>" + newline;
				barTemplate += "          </TD>" + newline;
				barTemplate += "          <TD class='DetailData'>&nbsp;Percentage</TD>" + newline;
				barTemplate += "       </TR>" + newline;
				barTemplate += "    </TABLE>";
				barTemplate += "</TD><TD Width=70 class='DetailData'>Value</TD></TR>";

				string barHTemplate = "<TR>" + newline;
				barHTemplate += " <TD Width=150  class='DetailData' align='right' bgColor='#e5e5e5'>Label</TD>" + newline;
				barHTemplate += " <TD  bgColor='#e5e5e5' class='DetailData' Width=" + (ChartWidth + 25) + ">";
				barHTemplate += "Percentage</TD>" + newline;
				barHTemplate += " <TD Width=25  class='DetailData' bgColor='#e5e5e5'>Value</TD></TR>";
				barHTemplate = barHTemplate.Replace("Label", section.ChartLabelHeader);
				barHTemplate = barHTemplate.Replace("Percentage", section.ChartPercentageHeader);
				barHTemplate = barHTemplate.Replace("Value", section.ChartValueHeader);
				htmlContent.Append(barHTemplate + newline);

				string temp;
				float width = 0;
				float val = 0;
				float percent = 0;
				int cntColor = 0;
				for (int i = 0; i < labels.Count; i++)
				{
					temp = barTemplate;
					val = float.Parse(values[i].ToString());
					width = float.Parse(values[i].ToString()) * ChartWidth / total;
					percent = float.Parse(values[i].ToString()) * 100 / total;

					temp = temp.Replace("Label", labels[i].ToString());
					if (percent == 0.0)
					{
						temp = temp.Replace("BackColor", "#f5f5f5");
						temp = temp.Replace("NOTZERO", "0");
					}
					else
					{
						temp = temp.Replace("BackColor", colors[cntColor]);
						temp = temp.Replace("NOTZERO", "1");
					}
					temp = temp.Replace("ChartTWidth", Decimal.Round((Decimal)(width + 2), 0).ToString());
					temp = temp.Replace("ChartWidth", Decimal.Round((Decimal)width, 0).ToString());
					temp = temp.Replace("Value", val.ToString());
					temp = temp.Replace("Percentage", Decimal.Round((decimal)percent, 2).ToString() + "%");

					htmlContent.Append(temp + newline);
					cntColor++;
					if (cntColor == 10)
						cntColor = 0;
				}
			}
			catch (Exception err)
			{
				htmlContent.Append("<TR><TD valign=MIDDLE align=CENTER><b>Unable to Generate Chart.<br>" + err.Message + "</b></TD></TR>");
			}
			htmlContent.Append("</TABLE>" + newline);
			htmlContent.Append("<!--- Chart Table ends here -->");
			htmlContent.Append("</TD></TR>");
		}


		/// <summary>
		/// Method to get distinct values for given Column name from the dataset for generating Chart.
		/// </summary>
		/// <param name="dataSet">report source dataset</param>
		/// <param name="criteria">data selection criteria</param>
		/// <param name="changeOnField">Column name</param>
		/// <param name="valueField">Column name</param>
		/// <returns>List of distinct labels and values</returns>
		private ArrayList GetDistinctValuesForChart(DataSet dataSet, string criteria, string changeOnField, string valueField)
		{
			ArrayList result = new ArrayList();
			ArrayList distinctValues = new ArrayList();
			if (criteria == null || criteria.Trim() == "")
			{
				criteria = "";
			}
			else
			{
				criteria = criteria.Substring(3);
			}
			foreach (DataRow dr in dataSet.Tables[0].Select(criteria))
			{
				if (!distinctValues.Contains(dr[changeOnField].ToString()))
				{
					distinctValues.Add(dr[changeOnField].ToString());
				}
			}
			ArrayList totalValues = new ArrayList();
			if (criteria.Trim() != "")
				criteria += " and ";
			foreach (object obj in distinctValues)
			{
				DataRow[] rows = reportSource.Tables[0].Select(criteria + changeOnField + "='" + obj.ToString().Replace("'", "''") + "' ");
				if (valueField.Trim().ToUpper() == "COUNT")
				{
					totalValues.Add(rows.Length.ToString());
				}
				else
				{
					float sum = 0;
					foreach (DataRow row in rows)
						sum += float.Parse(row[valueField].ToString());
					totalValues.Add(sum.ToString());
				}
			}
			result.Add(distinctValues);
			result.Add(totalValues);
			return result;
		}


		/// <summary>
		/// Method to get distinct values for the column in the report source dataset
		/// </summary>
		/// <param name="dataSet">report source dataset</param>
		/// <param name="columnName">Column name</param>
		/// <param name="criteria">Data selection criteria</param>
		/// <returns>List of distinct values</returns>
		private ArrayList GetDistinctValues(DataSet dataSet, string columnName, string criteria)
		{
			ArrayList distinctValues = new ArrayList();
			if (criteria == null || criteria.Trim() == "")
			{
				criteria = "";
			}
			else
			{
				criteria = criteria.Substring(3);
			}
			foreach (DataRow dr in dataSet.Tables[0].Select(criteria))
			{
				if (!distinctValues.Contains(dr[columnName].ToString()))
				{
					distinctValues.Add(dr[columnName].ToString());
				}
			}
			return distinctValues;
		}


		private Hashtable PrepareData(Hashtable totalArray)
		{
			foreach (object obj in TotalFields)
			{
				if (!totalArray.Contains(obj.ToString()))
				{
					totalArray.Add(obj.ToString(), 0.0F);
				}
			}
			return totalArray;
		}

		private Hashtable AccumulateTotal(Hashtable totalTable1, Hashtable totalTable2)
		{
			foreach (object totalField in TotalFields)
			{
				totalTable1[totalField.ToString()] = float.Parse(totalTable1[totalField.ToString()].ToString()) +
					float.Parse(totalTable2[totalField.ToString()].ToString());
			}
			return totalTable1;
		}

		private string getFontSize(int level)
		{
			string fontSize = "";
			switch (level)
			{
				case 1:
					fontSize = "14pt";
					break;
				case 2:
					fontSize = "12pt";
					break;
				case 3:
					fontSize = "10pt";
					break;
				default:
					fontSize = "9pt";
					break;
			}
			return fontSize;
		}
		#endregion

	}

	/// <summary>
	/// Class to hold Report section details
	/// </summary>
	public class Section
	{
		public string GroupBy { get; set; }
		public string TitlePrefix { get; set; }
		public bool IncludeFooter { get; set; }
		public bool GradientBackground { get; set; }

		public bool IncludeTotal { get; set; }
		public Section SubSection { get; set; }
		/// <summary>
		/// HTML Color code as string
		/// </summary>
		internal string backColor { get; set; }
		internal Color cBackColor { get; set; }
		internal int Level { get; set; }
		internal bool isChartCreated { get; set; }

		public bool IncludeChart { get; set; }
		public string ChartTitle { get; set; }
		public bool ChartShowAtBottom { get; set; }
		public string ChartChangeOnField { get; set; }
		public string ChartValueField { get; set; }  = "Count";
		public bool ChartShowBorder { get; set; }
		public string ChartLabelHeader { get; set; } = "Label";
		public string ChartPercentageHeader { get; set; } = "Percentage";
		public string ChartValueHeader { get; set; } = "Value";

		public Color BackColor
		{
			set { backColor = Util.GetHTMLColorString(value); cBackColor = value; }
			get { return cBackColor; }
		}

		public Section()
		{
			SubSection = null!;
			BackColor = Color.FromArgb(240, 240, 240);
			ChartValueField = "Count";
			GradientBackground = false;
			ChartTitle = "&nbsp;";
		}

		public Section(string groupBy, string titlePrefix)
		{
			GroupBy = groupBy;
			TitlePrefix = titlePrefix;
			SubSection = null!;
			BackColor = Color.FromArgb(240, 240, 240);
			ChartValueField = "Count";
			GradientBackground = false;
			ChartTitle = "&nbsp;";
		}

		public Section(string groupBy, string titlePrefix, Color bgcolor)
		{
			GroupBy = groupBy;
			TitlePrefix = titlePrefix;
			SubSection = null!;
			BackColor = bgcolor;
			ChartValueField = "Count";
			GradientBackground = false;
			ChartTitle = "&nbsp;";
		}
	}

	/// <summary>
	/// Class to hold Report field details
	/// </summary>
	public class Field
	{
		public string FieldName { get; set; }
		public string HeaderName { get; set; }
		/// <summary>
		/// HTML Color code as string
		/// </summary>
		internal string backColor { get; set; }
		internal Color cBackColor { get; set; }
		/// <summary>
		/// HTML Color code as string
		/// </summary>
		internal string headerBackColor { get; set; }
		internal Color cHeaderBackColor { get; set; }
		public int Width { get; set; }
		public bool isTotalField { get; set; } = false;
		internal string alignment { get; set; } = "LEFT";

		/// <summary>
		/// Gets or sets field alignment of type ALIGN
		/// </summary>
		public ALIGN Alignment
		{
			set
			{
				switch (value)
				{
					case ALIGN.LEFT:
						alignment = "LEFT";
						break;
					case ALIGN.RIGHT:
						alignment = "RIGHT";
						break;
					case ALIGN.CENTER:
						alignment = "CENTER";
						break;
					default:
						alignment = "LEFT";
						break;
				}
			}
			get
			{
				switch (alignment)
				{
					case "LEFT":
						return ALIGN.LEFT;
					case "RIGHT":
						return ALIGN.RIGHT;
					case "CENTER":
						return ALIGN.CENTER;
					default:
						return ALIGN.LEFT;
				}
			}
		}


		public Color BackColor
		{
			set { backColor = Util.GetHTMLColorString(value); cBackColor = value; }
			get { return cBackColor; }
		}

		public Color HeaderBackColor
		{
			set { headerBackColor = Util.GetHTMLColorString(value); cHeaderBackColor = value; }
			get { return cHeaderBackColor; }
		}

		public Field()
		{
			FieldName = "";
			HeaderName = "Column Header";
			BackColor = Color.White;
			Width = 0;
			HeaderBackColor = Color.White;
		}

		public Field(string fieldName, string headerName)
		{
			FieldName = fieldName;
			HeaderName = headerName;
			BackColor = Color.White;
			Width = 0;
			HeaderBackColor = Color.White;
		}

		public Field(string fieldName, string headerName, int width)
		{
			FieldName = fieldName;
			HeaderName = headerName;
			BackColor = Color.White;
			Width = width;
			HeaderBackColor = Color.White;
		}

		public Field(string fieldName, string headerName, int width, Color bgcolor)
		{
			FieldName = fieldName;
			HeaderName = headerName;
			BackColor = Color.White;
			Width = width;
			BackColor = bgcolor;
			HeaderBackColor = Color.White;
		}

		public Field(string fieldName, string headerName, int width, ALIGN TextAlignment)
		{
			FieldName = fieldName;
			HeaderName = headerName;
			BackColor = Color.White;
			Width = width;
			BackColor = Color.White;
			HeaderBackColor = Color.White;
			Alignment = TextAlignment;
		}

		public Field(string fieldName, string headerName, int width, Color bgcolor, Color headerBgColor)
		{
			FieldName = fieldName;
			HeaderName = headerName;
			Width = width;
			BackColor = bgcolor;
			HeaderBackColor = headerBgColor;
		}

		public Field(string fieldName, string headerName, Color bgcolor, Color headerBgColor)
		{
			FieldName = fieldName;
			HeaderName = headerName;
			Width = 0;
			BackColor = bgcolor;
			HeaderBackColor = headerBgColor;
		}

		public Field(string fieldName, string headerName, Color headerBgColor)
		{
			FieldName = fieldName;
			HeaderName = headerName;
			Width = 0;
			BackColor = Color.White;
			HeaderBackColor = headerBgColor;
		}
	}

	public enum ALIGN
	{
		LEFT = 0,
		RIGHT,
		CENTER
	}

	internal class Util
	{
		public static string GetHTMLColorString(Color color)
		{
			if (color.IsNamedColor)
				return color.Name;
			else
				return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
		}
	}
}

