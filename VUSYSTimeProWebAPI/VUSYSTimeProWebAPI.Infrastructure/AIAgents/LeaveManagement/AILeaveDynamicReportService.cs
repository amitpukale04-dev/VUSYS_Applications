using System.Data;
using System.Globalization;
using System.Text;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Hosting;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.LeaveManagement;
using VUSYSTimeProWebAPI.Infrastructure.Common;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.Constants;
using static CommonLibCore10.CONST;

namespace VUSYSTimeProWebAPI.Infrastructure.AIAgents.LeaveManagement;

/// <summary>
/// This Service Is Used To Execute Validated Read Only AI Leave Reports.
/// </summary>
/// <param name="_objSQLDBHelper"></param>
/// <param name="ContentTypeSvc"></param>
public sealed class AILeaveDynamicReportService(SQLDBHelper _objSQLDBHelper, IContentTypeService ContentTypeSvc)
{
    private static readonly Dictionary<string, string[]> AllowedColumns = new(StringComparer.Ordinal)
    {
        ["LeaveApplications"] = ["LeaveApplicationID", "EmployeeCode", "EmployeeName", "LeaveTypeCode", "LeaveType", "FromDate", "ToDate", "TotalLeaveDays", "IsHalfDay", "HalfDayType", "LeaveStatus", "LeaveStatusText", "ApproverEmployeeCode", "ApprovedOrRejectedDate", "ApprovedOrRejectedRemarks"],
        ["LeaveBalances"] = ["EmployeeCode", "EmployeeName", "LeaveYear", "LeaveTypeCode", "LeaveType", "OpeningBalance", "AllocatedLeave", "UsedLeave", "PendingLeave", "AvailableBalance"],
        ["LeaveActions"] = ["HistoryID", "LeaveApplicationID", "EmployeeCode", "EmployeeName", "LeaveType", "FromDate", "ToDate", "TotalLeaveDays", "LeaveStatus", "ActionType", "ActionBy", "ActionDate", "ApprovedOrRejectedRemarks"],
        ["Holidays"] = ["Id", "HolidayName", "HolidayDate", "HolidayYear", "HolidayDescription", "IsOptionalHoliday"]
    };

    /// <summary>
    /// This Method Is Used To Generate Report.
    /// </summary>
    /// <param name="definition"></param>
    /// <param name="employeeCode"></param>
    /// <param name="allowAllEmployees"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="UnauthorizedAccessException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<DataSet> GenerateReportAsync(AILeaveReportDefinitionM definition, string employeeCode, bool allowAllEmployees, CancellationToken ct = default)
    {
        string TableName = "AI_LEAVE_DYNAMIC_REPORT_RESPONSE";
        ArgumentNullException.ThrowIfNull(definition);
        if (!AllowedColumns.TryGetValue(definition.ReportType, out string[]? allowed))
            throw new ArgumentException("Unsupported report type.");
        if (definition.ReportType != "Holidays" && !allowAllEmployees && string.IsNullOrWhiteSpace(employeeCode))
            throw new UnauthorizedAccessException("Employee scope is required.");
        if (definition.MaxRows < 1 || definition.MaxRows > 1000)
            throw new ArgumentException("Report row limit must be between 1 and 1000.");
        if (definition.LeaveStatus.HasValue && (definition.LeaveStatus < 1 || definition.LeaveStatus > 4))
            throw new ArgumentException("Invalid leave status.");
        if (definition.LeaveYear.HasValue && (definition.LeaveYear < 1900 || definition.LeaveYear > 9999))
            throw new ArgumentException("Invalid leave year.");
        if (definition.LeaveTypeID.HasValue && definition.LeaveTypeID <= 0)
            throw new ArgumentException("Invalid leave type ID.");
        if (definition.Columns is null || definition.GroupBy is null || definition.Columns.Concat(definition.GroupBy).Any(x => !allowed.Contains(x, StringComparer.Ordinal)))
            throw new ArgumentException("Report contains an unsupported column.");
        if (definition.Columns.Count > allowed.Length || definition.GroupBy.Count > allowed.Length)
            throw new ArgumentException("Too many report columns.");
        if (definition.Aggregate is not ("None" or "Count" or "Sum" or "Average" or "Min" or "Max"))
            throw new ArgumentException("Unsupported aggregation.");
        if (definition.Aggregate != "None" && definition.Aggregate != "Count" && (definition.AggregateColumn is null || !allowed.Contains(definition.AggregateColumn, StringComparer.Ordinal)))
            throw new ArgumentException("Invalid aggregation column.");
        // Grouping without aggregation is treated as a distinct detailed report.
        if (definition.Aggregate is "Sum" or "Average" or "Min" or "Max" && !new[] { "TotalLeaveDays", "OpeningBalance", "AllocatedLeave", "UsedLeave", "PendingLeave", "AvailableBalance" }.Contains(definition.AggregateColumn))
            throw new ArgumentException("Sum and Average require a numeric leave amount column.");
        string dateField = definition.ReportType switch
        {
            "LeaveApplications" when definition.DateField is "LeavePeriod" or "ApprovalDate" => definition.DateField,
            "LeaveActions" => "ActionDate",
            "Holidays" => "HolidayDate",
            "LeaveBalances" => "LeavePeriod",
            _ => throw new ArgumentException("Invalid report date field.")
        };
        
        DateTime? from = string.IsNullOrWhiteSpace(definition.FromDate) ? null : DataTypeUtils.ToDateTimeExact(definition.FromDate.Trim(), DateTimeFormatConstants.YYYYMMDD_DATEFORMAT);
        DateTime? to = string.IsNullOrWhiteSpace(definition.ToDate) ? null : DataTypeUtils.ToDateTimeExact(definition.ToDate.Trim(), DateTimeFormatConstants.YYYYMMDD_DATEFORMAT);

        if (from > to) throw new ArgumentException("FromDate cannot be after ToDate.");
        if (definition.ReportType == "LeaveBalances" && (from.HasValue || to.HasValue))
            throw new ArgumentException("Leave balances support LeaveYear rather than date range.");
        if (definition.ReportType == "Holidays" && (definition.LeaveStatus.HasValue || definition.LeaveTypeID.HasValue))
            throw new ArgumentException("Holiday reports do not support leave filters.");
        Dictionary<string, object> parameters = new()
        {
            ["@ReportType"] = definition.ReportType,
            ["@EmployeeCode"] = employeeCode,
            ["@AllowAllEmployees"] = allowAllEmployees ? 1 : 0,
            ["@FromDate"] = from.HasValue ? from.Value : DBNull.Value,
            ["@ToDate"] = to.HasValue ? to.Value : DBNull.Value,
            ["@LeaveStatus"] = definition.LeaveStatus.HasValue ? definition.LeaveStatus.Value : DBNull.Value,
            ["@LeaveTypeID"] = definition.LeaveTypeID.HasValue ? definition.LeaveTypeID.Value : DBNull.Value,
            ["@LeaveYear"] = definition.LeaveYear.HasValue ? definition.LeaveYear.Value : DBNull.Value,
            ["@DateField"] = dateField,
            ["@MaxRows"] = definition.MaxRows
        };
        DataSet ds = await _objSQLDBHelper.ExecuteStoredProcedureWithMultipleParametersAsync(StoredProcedure.AI_LEAVE_DYNAMIC_REPORT, parameters, CONST.DBRetType.DATASET, ct) as DataSet ?? new DataSet();
        DataTable? source = ds.Tables.Cast<DataTable>().FirstOrDefault();
        if (source is null) return new DataSet();
        if (allowed.Any(x => definition.Columns.Contains(x) && !source.Columns.Contains(x)))
            throw new InvalidOperationException("Report result does not contain the requested columns.");
        string[] groupFields = definition.GroupBy.Distinct(StringComparer.Ordinal).ToArray();
        if (definition.Aggregate != "None")
        {
            DataTable summary = new(TableName);
            foreach (string field in groupFields) summary.Columns.Add(field, typeof(string));
            string metric = definition.Aggregate == "Count" ? "Count" : definition.Aggregate + "_" + definition.AggregateColumn;
            summary.Columns.Add(metric, typeof(decimal));
            var groups = source.Rows.Cast<DataRow>().GroupBy(r => string.Join("\u001f", groupFields.Select(field => FormatGroupValue(r[field]))));
            foreach (var group in groups)
            {
                DataRow row = summary.NewRow();
                DataRow first = group.First();
                foreach (string field in groupFields) row[field] = FormatGroupValue(first[field]);
                decimal[] values = definition.Aggregate == "Count" ? [] : group.Where(r => r[definition.AggregateColumn!] != DBNull.Value).Select(r => Convert.ToDecimal(r[definition.AggregateColumn!], CultureInfo.InvariantCulture)).ToArray();
                row[metric] = definition.Aggregate switch
                {
                    "Count" => group.Count(),
                    "Sum" => values.Sum(),
                    "Average" => values.Length == 0 ? 0 : values.Average(),
                    "Min" => values.Length == 0 ? 0 : values.Min(),
                    "Max" => values.Length == 0 ? 0 : values.Max(),
                    _ => 0
                };
                summary.Rows.Add(row);
            }
            DataSet grouped = new(); grouped.Tables.Add(summary); return grouped;
        }
        DataTable result = new(TableName);
        string[] columns = definition.Columns.Count == 0 ? allowed : definition.Columns.Distinct(StringComparer.Ordinal).ToArray();
        foreach (string column in columns) result.Columns.Add(column, source.Columns[column].DataType);
        IEnumerable<DataRow> detailRows = source.Rows.Cast<DataRow>();
        if (definition.Distinct)
        {
            detailRows = detailRows
                .GroupBy(r => string.Join(
                    "\u001f",
                    columns.Select(column => FormatGroupValue(r[column]))))
                .Select(g => g.First());
        }
        foreach (DataRow original in detailRows)
        {
            DataRow row = result.NewRow();
            foreach (string column in columns) row[column] = original[column];
            result.Rows.Add(row);
        }
        DataSet output = new(); output.Tables.Add(result); return output;
    }

    /// <summary>
    /// This Method Is Used To Export AI Leave Dynamic Report To Excel Or PDF.
    /// </summary>
    /// <param name="definition"></param>
    /// <param name="employeeCode"></param>
    /// <param name="allowAllEmployees"></param>
    /// <param name="exportType"></param>
    /// <param name="webHostEnvironment"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<DataSet> ExportReportAsync(AILeaveReportDefinitionM definition, string employeeCode, bool allowAllEmployees, string exportType, IWebHostEnvironment webHostEnvironment, CancellationToken ct = default)
    {
        if (exportType is not ("Excel" or "PDF"))
            throw new ArgumentException("Unsupported export type.");

        // Admin authorization must be checked by the authenticated API/service entry point before calling this method.
        ct.ThrowIfCancellationRequested();
        DataSet report = await GenerateReportAsync(definition, employeeCode, allowAllEmployees, ct);
        DataTable? source = report.Tables.Cast<DataTable>().FirstOrDefault();
        string TableName = exportType == "Excel" ? "AI_LEAVE_EXCEL_EXPORTED_RESPONSE" : "AI_LEAVE_PDF_EXPORTED_RESPONSE";
        List<string> columns = new List<string> { "Status", "Message", "FileName", "ContentType", "FileBase64" };
        if (source is null || source.Rows.Count == 0)
            return Helper.GenerateCommonMessage(columns, new List<List<string>>
            {
                new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), "No matching report records found.", string.Empty, string.Empty, string.Empty }
            }, TableName);

        string ReportTitle = GetReportTitle(definition);
        string FileNameWithoutExtension = $"VUSYS_LeaveReport_{definition.ReportType}_{DateTime.Now.ToString(DateTimeFormatConstants.YYYYMMDD_HHMMSS_COMPACT_DATETIMEFORMAT)}";
        string FileName = FileNameWithoutExtension + (exportType == "Excel" ? FileTypes.Xlsx : FileTypes.Pdf);
        string ContentType = exportType == "Excel" ? ContentTypeConstants.EXCEL_XLSX_CONTENTTYPE : ContentTypeConstants.PDF_CONTENTTYPE;
        string FileBase64 = string.Empty;

        DataTable excelTable = source.Copy();
        // Add SLNo As First Column
        if (!excelTable.Columns.Contains("SLNo"))
        {
            Helper.AddNewColumnToDataTable(excelTable, "SLNo", typeof(int));
            excelTable.Columns["SLNo"].SetOrdinal(0);
        }

        // Generate Sequential SLNo
        excelTable.AsEnumerable().Select((row, index) => new { Row = row, SLNo = index + 1 }).ToList().ForEach(x => x.Row["SLNo"] = x.SLNo);


        // Convert IsHalfDay Values From True/False To Yes/No
        if (excelTable.Columns.Contains("IsHalfDay"))
        {
            int columnIndex = excelTable.Columns["IsHalfDay"]!.Ordinal;

            // Convert Boolean Column To String Column
            if (excelTable.Columns["IsHalfDay"]!.DataType == typeof(bool))
            {
                Helper.AddNewColumnToDataTable(excelTable, "IsHalfDay_Text", typeof(string));
                excelTable.Columns["IsHalfDay_Text"].SetOrdinal(columnIndex);
                excelTable.AsEnumerable().ToList().ForEach(row => row["IsHalfDay_Text"] = row["IsHalfDay"] != DBNull.Value ? Convert.ToBoolean(row["IsHalfDay"]) ? "Yes" : "No" : string.Empty);
                Helper.RemoveColumnFromDataTable(excelTable, "IsHalfDay");
                Helper.RenameDataTableColumnName(excelTable, "IsHalfDay_Text", "IsHalfDay");
            }
            else
            {
                excelTable.AsEnumerable().Where(row => row["IsHalfDay"] != DBNull.Value).ToList()
                    .ForEach(row =>
                    {
                        string value = Convert.ToString(row["IsHalfDay"])?.Trim() ?? string.Empty;
                        row["IsHalfDay"] = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1" ? "Yes" : "No";
                    });
            }
        }

        if (exportType == "Excel")
        {
            ct.ThrowIfCancellationRequested();
            int NoOfColumns = excelTable.Columns.Count;
            string SheetName = "AI Leave Report";
            string lastColumn = GetExcelColumnName(NoOfColumns);
            string[] numericColumns = excelTable.Columns.Cast<DataColumn>()
                .Where(x => IsNumericType(x.DataType)).Select(x => x.ColumnName).ToArray();
            FileBase64 = ExcelHelper.ExportToExcelBase64(excelTable, SheetName, ReportTitle, $"A1:{lastColumn}1", 1, NoOfColumns, true, NoOfColumns, false, 0, "#FFFFFF", FileName, true, numericColumns, "#4F81BD", "#DCE6F1", true);
            //string Folder = CommonHelper.SaveFileToFolder(FileBase64, exportType == "Excel" ? FileTypes.Xlsx : FileTypes.Pdf, webHostEnvironment);
        }
        else
        {
            string TempFilePath = Path.Combine(Path.GetTempPath(), $"VUSYS_AILeaveReport_{Guid.NewGuid():N}.html");
            try
            {
                string html = await BuildReportHTML(excelTable, ReportTitle, ContentTypeSvc, webHostEnvironment);
                await File.WriteAllTextAsync(TempFilePath, html, Encoding.UTF8, ct);
                ct.ThrowIfCancellationRequested();
                byte[] pdfBytes = PDFHelper.ConvertHTMLToPDF(TempFilePath, true, false, null, false, null, false, null, null);
                ct.ThrowIfCancellationRequested();
                if (pdfBytes.Length > 0)
                {
                    FileBase64 = Convert.ToBase64String(pdfBytes);
                    //string Folder = CommonHelper.SaveFileToFolder(FileBase64, exportType == "Excel" ? FileTypes.Xlsx : FileTypes.Pdf, webHostEnvironment);
                }
            }
            finally
            {
                if (File.Exists(TempFilePath)) File.Delete(TempFilePath);
            }
        }

        if (!string.IsNullOrWhiteSpace(FileBase64))
        {
            FileBase64 = Helper.CleanBase64(FileBase64);
            return Helper.GenerateCommonMessage(columns, new List<List<string>>
            {
                new List<string> { Helper.ToProperCase(MessageTypeConstants.Success), "AI Leave Report Downloaded Successfully !!!", FileName, ContentType, FileBase64 }
            }, TableName);
        }
        return Helper.GenerateCommonMessage(columns, new List<List<string>>
        {
            new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), "Failed To Download AI Leave Report !!!", string.Empty, string.Empty, string.Empty }
        }, TableName);
    }

    /// <summary>
    /// This Method Is Used To Get Report Title.
    /// </summary>
    /// <param name="definition"></param>
    /// <returns></returns>
    private static string GetReportTitle(AILeaveReportDefinitionM definition)
    {
        string title = definition.ReportType switch
        {
            "LeaveApplications" => "Leave Applications Report",
            "LeaveBalances" => "Employee Leave Balance Report",
            "LeaveActions" => "Leave Actions Report",
            "Holidays" => "Holiday Report",
            _ => "AI Leave Management Report"
        };
        if (definition.LeaveYear.HasValue) title += $" - {definition.LeaveYear.Value}";
        if (!string.IsNullOrWhiteSpace(definition.FromDate) || !string.IsNullOrWhiteSpace(definition.ToDate))
            title += $" ({DateTimeHelper.ConvertDateTimeStringToSpecificFormat(definition.FromDate, DateTimeFormatConstants.DDMMMYYYY_SLASH_DATEFORMAT) ?? "All"} to {DateTimeHelper.ConvertDateTimeStringToSpecificFormat(definition.ToDate, DateTimeFormatConstants.DDMMMYYYY_SLASH_DATEFORMAT) ?? "All"})";
        return title;
    }

    /// <summary>
    /// This Method Is Used To Get Is Numeric Type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static bool IsNumericType(Type type) => type == typeof(int) || type == typeof(long) ||
        type == typeof(short) || type == typeof(decimal) || type == typeof(double) ||
        type == typeof(float) || type == typeof(byte);

    /// <summary>
    /// This Method Is Used To Get Excel Column Name.
    /// </summary>
    /// <param name="columnNumber"></param>
    /// <returns></returns>
    private static string GetExcelColumnName(int columnNumber)
    {
        StringBuilder result = new StringBuilder();
        while (columnNumber > 0)
        {
            columnNumber--;
            result.Insert(0, (char)('A' + columnNumber % 26));
            columnNumber /= 26;
        }
        return result.ToString();
    }

    /// <summary>
    /// This Method Is Used To Build Dynamic AI Leave Report HTML Using The PDF Template.
    /// </summary>
    /// <param name="table"></param>
    /// <param name="title"></param>
    /// <param name="ContentTypeSvc"></param>
    /// <param name="webHostEnvironment"></param>
    /// <returns></returns>
    private static async Task<string> BuildReportHTML(DataTable table, string title, IContentTypeService ContentTypeSvc, IWebHostEnvironment webHostEnvironment)
    {
        string html = await CommonHelper.ReadHTMLTemplateUsingStreamReaderAsync(webHostEnvironment, "AILeaveReportPDFTemplate.html");
        StringBuilder reportHeaders = new StringBuilder();
        StringBuilder reportRows = new StringBuilder();

        // Generate Dynamic Table Headers Based On The Report Columns.
        foreach (DataColumn column in table.Columns)
        {
            reportHeaders.AppendLine($"<th>{Helper.EncodeHTMLText(column.ColumnName)}</th>");
        }

        // Generate Dynamic Table Rows Based On The Report Records.
        foreach (DataRow row in table.Rows)
        {
            reportRows.AppendLine("<tr>");
            foreach (DataColumn column in table.Columns)
            {
                object value = row[column];
                string text = value == DBNull.Value ? string.Empty : value is DateTime date
                    ? date.ToString(DateTimeFormatConstants.DDMMMYYYY_DATEFORMAT, CultureInfo.InvariantCulture)
                    : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

                string cellClass = column.ColumnName == "SLNo" ? " class=\"col-slno\""
                    : IsNumericType(column.DataType) ? " class=\"col-number\"" : string.Empty;
                reportRows.AppendLine($"<td{cellClass}>{Helper.EncodeHTMLText(text)}</td>");
            }
            reportRows.AppendLine("</tr>");
        }

        // Replace Dynamic Report Placeholders.
        html = html.Replace("#CompanyName#", "VUSYS TECHNOLOGY")
            .Replace("#ReportTitle#", Helper.EncodeHTMLText(title))
            .Replace("#FooterText#", Helper.EncodeHTMLText(title))
            .Replace("#ReportTableHeaders#", reportHeaders.ToString())
            .Replace("#ReportTableRows#", reportRows.ToString());

        string LogoFileName = "VUSYS_HeaderLogo.png";
        string logoPath = Path.Combine(CommonHelper.GetActualPath(webHostEnvironment, "Content"), "Images", LogoFileName);
        if (File.Exists(logoPath))
        {
            string LogoBase64 = FileHelper.ConvertFileToBase64(logoPath);
            if (!string.IsNullOrEmpty(LogoBase64))
            {
                string LogoImageFileContentType = ContentTypeSvc.GetFileContentType(LogoFileName);
                string imgSrc = $"data:{LogoImageFileContentType};base64,{LogoBase64}";
                string LogoImage = $@"<img src='{imgSrc}' alt='VUSYS Technology Logo'/>";
                html = html.Replace("#CompanyLogo#", LogoImage);
            }
        }
        return html;
    }

    /// <summary>
    /// This Method Is Used To Format Group Value.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private static string FormatGroupValue(object value)
    {
        if (value == DBNull.Value) return string.Empty;
        return value is DateTime date ? date.ToString(DateTimeFormatConstants.YYYYMM_DATEFORMAT, CultureInfo.InvariantCulture) : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
