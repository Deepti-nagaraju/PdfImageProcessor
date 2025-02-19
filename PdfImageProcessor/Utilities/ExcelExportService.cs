using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OfficeOpenXml;
using PdfImageProcessor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PdfImageProcessor.Services
{
    public class ExcelExportService
    {
        public IActionResult ExportToExcel(ITempDataDictionary tempData)
        {
            var modelJson = tempData["ExtractedData"] as string;
            if (string.IsNullOrEmpty(modelJson)) return new BadRequestObjectResult("No data available to export.");

            tempData.Keep("ExtractedData");
            var model = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MainTableModel>>(modelJson);
            if (model == null || !model.Any()) return new BadRequestObjectResult("No data available to export.");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Extracted Data");

            worksheet.Cells[1, 1].Value = "File Name";
            worksheet.Cells[1, 2].Value = "IRN";
            worksheet.Cells[1, 3].Value = "Buyer";
            worksheet.Cells[1, 4].Value = "Invoice Number";

            int row = 2;
            foreach (var data in model)
            {
                worksheet.Cells[row, 1].Value = data.FileName;
                worksheet.Cells[row, 2].Value = string.Join(", ", data.Irn);
                worksheet.Cells[row, 3].Value = string.Join(", ", data.Buyer);
                worksheet.Cells[row, 4].Value = string.Join(", ", data.InvoiceNumber);
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            return new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = "ExtractedData.xlsx"
            };
        }
    }
}
