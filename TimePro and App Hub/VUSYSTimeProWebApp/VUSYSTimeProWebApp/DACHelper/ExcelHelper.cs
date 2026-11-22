using System.Data;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using CommonLib;
using CommonLib.Constants;

namespace VUSYSTimeProWebApp.DACHelper
{
    public static class ExcelHelper
    {
        /// <summary>
        /// This Method is Used to Export Data To Excel.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="SheetName"></param>
        /// <param name="Title"></param>
        /// <param name="TitleRange"></param>
        /// <param name="BorderStyleRangeRow"></param>
        /// <param name="BorderStyleRangeColumn"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public static FileContentResult ExportToExcel(DataTable dt, string SheetName, string Title, string TitleRange, int BorderStyleRangeRow, int BorderStyleRangeColumn, string FileName)
        {
            FileContentResult result = null;
            try
            {
                using (XLWorkbook wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add(SheetName);
                    ws.Cell(1, 1).Value = Title;
                    var titlerange = ws.Range(TitleRange);
                    titlerange.Merge().Style.Font.SetBold().Font.FontSize = 13;
                    ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(BorderStyleRangeRow, BorderStyleRangeColumn).Style.Border.TopBorder = XLBorderStyleValues.Thick;
                    ws.Cell(BorderStyleRangeRow, BorderStyleRangeColumn).Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                    ws.Cell(BorderStyleRangeRow, BorderStyleRangeColumn).Style.Border.LeftBorder = XLBorderStyleValues.Thick;
                    ws.Cell(BorderStyleRangeRow, BorderStyleRangeColumn).Style.Border.RightBorder = XLBorderStyleValues.Thick;
                    ws.Cell(2, 1).InsertTable(dt);
                    ws.Table(0).ShowAutoFilter = false;
                    // Add borders to all cells in the used range (entire table)
                    var range = ws.Range(2, 1, ws.LastRowUsed().RowNumber(), ws.LastColumnUsed().ColumnNumber());
                    range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    range.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    range.Style.Border.RightBorder = XLBorderStyleValues.Thin;

                    ws.Columns().AdjustToContents();
                    using (MemoryStream stream = new MemoryStream())
                    {
                        wb.SaveAs(stream);

                        FileContentResult res = new FileContentResult(stream.ToArray(), ContentTypeConstants.EXCEL_XLSX_CONTENTTYPE)
                        {
                            FileDownloadName = FileName
                        };
                        result = res;
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.DACHelper", "ExcelHelper.ExportToExcel()", ex.Message, false);
                throw;
            }
            return result;
        }

        /// <summary>
        /// This Method is Used to Export Data To Excel.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="SheetName"></param>
        /// <param name="Title"></param>
        /// <param name="TitleRange"></param>
        /// <param name="BorderStyleRangeRow"></param>
        /// <param name="BorderStyleRangeColumn"></param>
        /// <param name="IsWordWrap"></param>
        /// <param name="WordWrapColumn"></param>
        /// <param name="WordWrapColumnIndex"></param>
        /// <param name="IsFreezeRows"></param>
        /// <param name="FreezeRowNumber"></param>
        /// <param name="IsFreezeColumns"></param>
        /// <param name="FreezeColumnNumber"></param>
        /// <param name="FileName"></param>
        /// <returns></returns>
        public static FileContentResult ExportToExcel(DataTable dt, string SheetName, string Title, string TitleRange, int BorderStyleRangeRow, int BorderStyleRangeColumn, bool IsWordWrap, string WordWrapColumn, int WordWrapColumnIndex, bool IsFreezeRows, int FreezeRowNumber, bool IsFreezeColumns, int FreezeColumnNumber, string FileName)
        {
            FileContentResult result = null;
            try
            {
                using (XLWorkbook wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add(SheetName);

                    // Title
                    ws.Cell(1, 1).Value = Title;
                    var titlerange = ws.Range(TitleRange);
                    titlerange.Merge().Style.Font.SetBold().Font.FontSize = 13;
                    ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Top thick border
                    ws.Cell(BorderStyleRangeRow, BorderStyleRangeColumn).Style.Border.TopBorder = XLBorderStyleValues.Thick;
                    ws.Cell(BorderStyleRangeRow, BorderStyleRangeColumn).Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                    ws.Cell(BorderStyleRangeRow, BorderStyleRangeColumn).Style.Border.LeftBorder = XLBorderStyleValues.Thick;
                    ws.Cell(BorderStyleRangeRow, BorderStyleRangeColumn).Style.Border.RightBorder = XLBorderStyleValues.Thick;

                    // Insert DataTable
                    ws.Cell(2, 1).InsertTable(dt);
                    ws.Table(0).ShowAutoFilter = false;

                    if (IsWordWrap)
                    {
                        for (int row = 3; row <= ws.LastRowUsed().RowNumber(); row++)
                        {
                            for (int col = 1; col <= ws.LastColumnUsed().ColumnNumber(); col++)
                            {
                                var cell = ws.Cell(row, col);

                                // Apply center-center for SL No, Employee Code, Name Task Date, Client, Project, No. of Hours Worked
                                if (col == 1 || col == 2 || col == 3 || col == 4 || col == 5 || col == 6 || col == 7 || col == WordWrapColumnIndex + 1)
                                {
                                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                                }
                                else
                                {
                                    // Default alignment for any other columns (including Task Description)
                                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                                }
                            }

                            // ✅ Task Description formatting
                            var wordWrapCell = ws.Cell(row, WordWrapColumnIndex);
                            var content = wordWrapCell.GetString()?.Trim();

                            if (!string.IsNullOrWhiteSpace(content))
                            {
                                var lines = content
                                    .Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(line =>
                                    {
                                        line = line.Trim();
                                        line = System.Text.RegularExpressions.Regex.Replace(line, @"^[,\s]*\d+\.\s*", "");
                                        return line;
                                    })
                                    .Where(line => !string.IsNullOrWhiteSpace(line))
                                .ToList();

                                string formatted = string.Join("\n", lines.Select((desc, index) => $"{index + 1}. {desc}"));
                                formatted = System.Text.RegularExpressions.Regex.Replace(formatted, @"(\n){2,}", "\n");

                                // Apply content
                                wordWrapCell.SetValue(formatted);
                                wordWrapCell.Style.Alignment.WrapText = true;
                                wordWrapCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                                // ✅ Apply vertical alignment based on number of lines
                                if (lines.Count > 1)
                                {
                                    wordWrapCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                                }
                                else
                                {
                                    wordWrapCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                                }

                                // Add borders
                                wordWrapCell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                                wordWrapCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                                wordWrapCell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                                wordWrapCell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
                            }
                        }

                        // Adjust column width
                        ws.Column(WordWrapColumnIndex).AdjustToContents();
                    }

                    // Borders for entire used range
                    var range = ws.Range(2, 1, ws.LastRowUsed().RowNumber(), ws.LastColumnUsed().ColumnNumber());
                    range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                    range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    range.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                    range.Style.Border.RightBorder = XLBorderStyleValues.Thin;

                    // Auto adjust all columns
                    ws.Columns().AdjustToContents();
                    if(IsFreezeRows)
                    {
                        ws.SheetView.FreezeRows(FreezeRowNumber); //2
                    }
                    if(IsFreezeColumns)
                    {
                        ws.SheetView.FreezeColumns(FreezeColumnNumber); //5
                    }

                    using (MemoryStream stream = new MemoryStream())
                    {
                        wb.SaveAs(stream);
                        result = new FileContentResult(stream.ToArray(), ContentTypeConstants.EXCEL_XLSX_CONTENTTYPE)
                        {
                            FileDownloadName = FileName
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSTimeProWebApp.DACHelper", "ExcelHelper.ExportToExcel()", ex.Message, false);
                throw;
            }

            return result;
        }




    }
}
