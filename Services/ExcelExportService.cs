using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using FishFarmManager.Models;

namespace FishFarmManager.Services
{
    /// <summary>
    /// خدمة التصدير إلى Excel
    /// Excel Export Service
    /// </summary>
    public class ExcelExportService
    {
        private readonly string _exportPath;

        public ExcelExportService()
        {
            _exportPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                "AquaFarm Pro", 
                "Excel"
            );
            
            if (!Directory.Exists(_exportPath))
            {
                Directory.CreateDirectory(_exportPath);
            }
        }

        /// <summary>
        /// تصدير DataTable إلى Excel
        /// </summary>
        public string ExportDataTableToExcel(DataTable dataTable, string fileName, string sheetName = "البيانات")
        {
            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(sheetName);

                // إضافة البيانات
                worksheet.Cell(1, 1).InsertTable(dataTable);

                // تنسيق الرأس
                var headerRange = worksheet.Range(1, 1, 1, dataTable.Columns.Count);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(0, 121, 140); // Aqua Green
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // تنسيق الخلايا
                worksheet.Columns().AdjustToContents();

                // حفظ الملف
                var fullPath = Path.Combine(_exportPath, fileName);
                workbook.SaveAs(fullPath);

                LoggingService.LogInfo($"Excel file created: {fileName}");
                return fullPath;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error exporting to Excel");
                throw;
            }
        }

        /// <summary>
        /// تصدير قائمة من الكائنات إلى Excel
        /// </summary>
        public string ExportListToExcel<T>(List<T> data, string fileName, string sheetName = "البيانات")
        {
            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(sheetName);

                // إضافة البيانات
                if (data != null && data.Count > 0)
                {
                    worksheet.Cell(1, 1).InsertData(data.AsEnumerable());

                    // تنسيق الرأس
                    var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 1;
                    var headerRange = worksheet.Range(1, 1, 1, lastColumn);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(0, 121, 140);
                    headerRange.Style.Font.FontColor = XLColor.White;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Columns().AdjustToContents();
                }

                var fullPath = Path.Combine(_exportPath, fileName);
                workbook.SaveAs(fullPath);

                LoggingService.LogInfo($"Excel file created: {fileName}");
                return fullPath;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error exporting list to Excel");
                throw;
            }
        }

        /// <summary>
        /// تصدير كشف الرواتب إلى Excel (يقبل DataTable)
        /// </summary>
        public string ExportSalaryToExcel(DataTable salaryData, int month, int year)
        {
            try
            {
                var fileName = $"Salary_{year}_{month:D2}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return ExportDataTableToExcel(salaryData, fileName, $"رواتب {month}/{year}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error exporting salary to Excel");
                throw;
            }
        }

        /// <summary>
        /// فتح ملف Excel
        /// </summary>
        public bool OpenExcelFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    LoggingService.LogError($"Excel file not found: {filePath}");
                    return false;
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });

                LoggingService.LogInfo($"Excel file opened: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Error opening Excel file");
                return false;
            }
        }

        /// <summary>
        /// الحصول على مسار مجلد Excel
        /// </summary>
        public string GetExportPath() => _exportPath;
    }
}

