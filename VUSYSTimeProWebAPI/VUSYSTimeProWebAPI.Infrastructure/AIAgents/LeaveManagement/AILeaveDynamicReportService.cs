using System.Data;
using System.Globalization;
using System.Net;
using System.Text;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.Hosting;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.LeaveManagement;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.Constants;
using static CommonLibCore10.CONST;

namespace VUSYSTimeProWebAPI.Infrastructure.AIAgents.LeaveManagement;

/// <summary>
/// This Service Is Used To Execute Validated Read Only AI Leave Reports.
/// </summary>
public sealed class AILeaveDynamicReportService(SQLDBHelper _objSQLDBHelper)
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
        static DateTime? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (!DateTime.TryParseExact(value, DateTimeFormatConstants.YYYYMMDD_DATEFORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                throw new ArgumentException("Report dates must use yyyy-MM-dd.");
            return date;
        }
        DateTime? from = ParseDate(definition.FromDate);
        DateTime? to = ParseDate(definition.ToDate);
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
        if (groupFields.Length > 0)
            detailRows = detailRows.GroupBy(r => string.Join("\u001f", groupFields.Select(field => FormatGroupValue(r[field])))).Select(g => g.First());
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
        string FileNameWithoutExtension = $"VUSYS_AILeaveReport_{definition.ReportType}_{DateTime.Now:yyyyMMdd_HHmmss}";
        string FileName = FileNameWithoutExtension + (exportType == "Excel" ? FileTypes.Xlsx : FileTypes.Pdf);
        string ContentType = exportType == "Excel" ? ContentTypeConstants.EXCEL_XLSX_CONTENTTYPE : ContentTypeConstants.PDF_CONTENTTYPE;
        string FileBase64 = string.Empty;

        if (exportType == "Excel")
        {
            ct.ThrowIfCancellationRequested();
            DataTable excelTable = source.Copy();
            int NoOfColumns = excelTable.Columns.Count;
            string SheetName = "AI Leave Report";
            string lastColumn = GetExcelColumnName(NoOfColumns);
            string[] numericColumns = excelTable.Columns.Cast<DataColumn>()
                .Where(x => IsNumericType(x.DataType)).Select(x => x.ColumnName).ToArray();
            FileBase64 = ExcelHelper.ExportToExcelBase64(excelTable, SheetName, ReportTitle,
                $"A1:{lastColumn}1", 1, NoOfColumns, true, NoOfColumns, false, 0,
                "#FFFFFF", FileName, true, numericColumns, "#4F81BD", "#DCE6F1", true);
            //string Folder = CommonHelper.SaveFileToFolder(FileBase64, exportType == "Excel" ? FileTypes.Xlsx : FileTypes.Pdf, webHostEnvironment);
        }
        else
        {
            string TempFilePath = Path.Combine(Path.GetTempPath(), $"VUSYS_AILeaveReport_{Guid.NewGuid():N}.html");
            try
            {
                string html = BuildReportHTML(source, ReportTitle);
                await File.WriteAllTextAsync(TempFilePath, html, Encoding.UTF8, ct);
                ct.ThrowIfCancellationRequested();
                byte[] pdfBytes = PDFHelper.ConvertHTMLToPDF(TempFilePath, true, false, null, false, null, false, null, null);
                ct.ThrowIfCancellationRequested();
                if (pdfBytes.Length > 0)
                    FileBase64 = Convert.ToBase64String(pdfBytes);
                //string Folder = CommonHelper.SaveFileToFolder(FileBase64, exportType == "Excel" ? FileTypes.Xlsx : FileTypes.Pdf, webHostEnvironment);
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
            title += $" ({definition.FromDate ?? "All"} to {definition.ToDate ?? "All"})";
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

    private static string BuildReportHTML(DataTable table, string title)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/>");
        sb.AppendLine("<style>@page { size: A4 landscape; margin: 12mm; } body { font-family: Arial, sans-serif; font-size: 9px; color: #222; } h2 { text-align: center; margin: 0 0 12px 0; } table { width: 100%; border-collapse: collapse; table-layout: auto; } thead { display: table-header-group; } tr { page-break-inside: avoid; } th, td { border: 1px solid #aaa; padding: 5px; overflow-wrap: anywhere; } th { background: #4F81BD; color: #fff; text-align: left; } tbody tr:nth-child(even) { background: #f0f4f9; }</style></head><body>");
        sb.AppendLine($"<h2>{WebUtility.HtmlEncode(title)}</h2>");
        sb.AppendLine("<table><thead><tr>");
        foreach (DataColumn column in table.Columns)
            sb.AppendLine($"<th>{WebUtility.HtmlEncode(column.ColumnName)}</th>");
        sb.AppendLine("</tr></thead><tbody>");
        foreach (DataRow row in table.Rows)
        {
            sb.AppendLine("<tr>");
            foreach (DataColumn column in table.Columns)
            {
                object value = row[column];
                string text = value == DBNull.Value ? string.Empty : value is DateTime date
                    ? date.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture)
                    : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                sb.AppendLine($"<td>{WebUtility.HtmlEncode(text)}</td>");
            }
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody></table></body></html>");
        return sb.ToString();
    }

    private static string FormatGroupValue(object value)
    {
        if (value == DBNull.Value) return string.Empty;
        return value is DateTime date ? date.ToString("yyyy-MM", CultureInfo.InvariantCulture) : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
