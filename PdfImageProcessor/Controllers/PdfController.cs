using Microsoft.AspNetCore.Mvc;
using PdfImageProcessor.Services;
using PdfImageProcessor.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.IO;

namespace PdfImageProcessor.Controllers
{
    [ApiController]
    [Route("Pdf")]
    public class PdfController : Controller
    {
        private readonly PdfProcessingService _pdfProcessingService;

        public PdfController()
        {
            _pdfProcessingService = new PdfProcessingService();
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("UploadPdf")]
        public async Task<IActionResult> UploadPdf()
        {
            try
            {
                if (Request.Form.Files.Count == 0)
                {
                    TempData["Error"] = "Please upload at least one valid PDF file.";
                    return RedirectToAction("Index");
                }

                var extractedDataList = await _pdfProcessingService.ProcessPdfAsync(Request.Form.Files);

                if (extractedDataList.Count > 0)
                {
                    TempData["ExtractedData"] = JsonConvert.SerializeObject(extractedDataList);
                    return PartialView("DisplayExtractedData", extractedDataList);
                }

                return Json(new { success = false, message = "No data extracted from the uploaded PDF(s)." });
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
        [HttpPost("ExportToExcel")]
        public IActionResult ExportToExcel([FromBody] List<ExcelModel> tableData)
        {
            if (tableData == null || !tableData.Any())
                return BadRequest("No data available to export.");
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Extracted Data");

            int row = 1; // Start from row 1

            foreach (var mainRow in tableData)
            {
                var headers = new List<string>
                     {
                      "File Name", "IRN", "Acknowledgement Number", "Acknowledge Date", 
                       "Buyer","Buyer GSTIN", "Buyer Address","Buyer State", "Buyer Pin", "Buyer Contact", "Buyer Contact Number", "Buyer Email",
                      "Ship To", "Ship To Address", "Ship To Contact Person", "Ship To Contact Number","Ship to Email",
                      "Invoice Number", "Invoice Date","Eway Bill Number", "Delivery Note", "Terms of Payment",
                      "Despatch Doc No", "Despatch Through", "Destination", "Vehicle No",
                      "Quantity", "Rate", "CGST", "SGST","IGST",
                      "Total Amount", "Bank Name","Account No", "IFSC Code"
                     };

                // ✅ Insert Main Table Headers (only once)
                if (row == 1)
                {
                    for (int i = 0; i < headers.Count; i++)
                    {
                        worksheet.Cells[row, i + 1].Value = headers[i];
                        worksheet.Cells[row, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[row, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[row, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }
                    row++;
                }

                // ✅ Insert Main Table Data
                worksheet.Cells[row, 1].Value = mainRow.FileName;
                worksheet.Cells[row, 2].Value = string.Join(", ", mainRow.Irn);
                worksheet.Cells[row, 3].Value = string.Join(", ", mainRow.AcknowledgeNumber);
                worksheet.Cells[row, 4].Value = string.Join(", ", mainRow.AcknowledgeDate);

                worksheet.Cells[row, 5].Value = string.Join(", ", mainRow.Buyer);
                worksheet.Cells[row, 6].Value = string.Join(", ", mainRow.BuyerGstin);
                worksheet.Cells[row, 7].Value = string.Join(", ", mainRow.BuyerAddressLine1);
                worksheet.Cells[row, 8].Value = string.Join(", ", mainRow.BuyerState);
                worksheet.Cells[row, 9].Value = string.Join(", ", mainRow.BuyerPinCode);
                worksheet.Cells[row, 10].Value = string.Join(", ", mainRow.BuyerContactPerson);
                worksheet.Cells[row, 11].Value = string.Join(", ", mainRow.BuyerContactNumber);
                worksheet.Cells[row, 12].Value = string.Join(", ", mainRow.BuyerEmail);


                worksheet.Cells[row, 13].Value = string.Join(", ", mainRow.ShipTo);
                worksheet.Cells[row, 14].Value = string.Join(", ", mainRow.ShipToAddressLine1);
                worksheet.Cells[row, 15].Value = string.Join(", ", mainRow.ShipToContactPerson);
                worksheet.Cells[row, 16].Value = string.Join(", ", mainRow.ShipToContactNumber);
                worksheet.Cells[row, 17].Value = string.Join(", ", mainRow.ShipToEmail);

                worksheet.Cells[row, 18].Value = string.Join(", ", mainRow.InvoiceNumber);
                worksheet.Cells[row, 19].Value = string.Join(", ", mainRow.InvoiceDate);
                worksheet.Cells[row, 20].Value = string.Join(", ", mainRow.EWayBill);
                worksheet.Cells[row, 21].Value = string.Join(", ", mainRow.DeleiveryNote);
                worksheet.Cells[row, 22].Value = string.Join(", ", mainRow.TermsOfPayment);

                worksheet.Cells[row, 23].Value = string.Join(", ", mainRow.DespatchDocNo);
                worksheet.Cells[row, 24].Value = string.Join(", ", mainRow.DespatchThrough);
                worksheet.Cells[row, 25].Value = string.Join(", ", mainRow.Destination);
                worksheet.Cells[row, 26].Value = string.Join(", ", mainRow.VehicleNo);

                worksheet.Cells[row, 27].Value = string.Join(", ", mainRow.Quantity);
                worksheet.Cells[row, 28].Value = string.Join(", ", mainRow.Rate);
                worksheet.Cells[row, 29].Value = string.Join(", ", mainRow.Cgst);
                worksheet.Cells[row, 30].Value = string.Join(", ", mainRow.Sgst);
                worksheet.Cells[row, 31].Value = string.Join(", ", mainRow.Igst);
                worksheet.Cells[row, 32].Value = string.Join(", ", mainRow.TotalAmount);

                worksheet.Cells[row, 33].Value = string.Join(", ", mainRow.BankName);
                worksheet.Cells[row, 34].Value = string.Join(", ", mainRow.AcctNo);
                worksheet.Cells[row, 35].Value = string.Join(", ", mainRow.IfscCode);
                row++;

                // ✅ Check if SubTable is present and properly deserialized
                if (mainRow.SubTable != null && mainRow.SubTable.Any())
                {
                    worksheet.Cells[row, 1].Value = "Sub-Table Data:";
                    worksheet.Cells[row, 1].Style.Font.Bold = true;
                    row++;

                    var subHeaders = mainRow.SubTable.First().Keys.ToList();

                    // ✅ Insert Sub-Table Headers
                    for (int i = 0; i < subHeaders.Count; i++)
                    {
                        worksheet.Cells[row, i + 1].Value = subHeaders[i];
                        worksheet.Cells[row, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[row, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[row, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                    }
                    row++;

                    // ✅ Insert Sub-Table Data
                    foreach (var subRow in mainRow.SubTable)
                    {
                        for (int col = 0; col < subHeaders.Count; col++)
                        {
                            worksheet.Cells[row, col + 1].Value = subRow[subHeaders[col]];
                        }
                        row++;
                    }
                }

                row += 1; // Leave space before the next main entry
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            byte[] fileBytes = package.GetAsByteArray();
            return File(
           fileContents: fileBytes,
           contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
           fileDownloadName: "ExtractedData.xlsx"
);
        }

        //        [HttpPost("ExportToExcel")]
        //        public IActionResult ExportToExcel([FromBody] List<MainTableModel> tableData)
        //        {
        //            if (tableData == null || !tableData.Any())
        //                return BadRequest("No data available to export.");

        //            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        //            using var package = new ExcelPackage();

        //            var worksheet = package.Workbook.Worksheets.Add("Extracted Data");

        //            int row = 1; // Start from row 1
        //            foreach (var mainRow in tableData)
        //            {
        //                var headers = mainRow.Keys.Where(k => k != "SubTable").ToList(); // Exclude sub-table key

        //                // ✅ Insert Main Table Headers (only once)
        //                if (row == 1)
        //                {
        //                    for (int i = 0; i < headers.Count; i++)
        //                    {
        //                        worksheet.Cells[row, i + 1].Value = headers[i];
        //                        worksheet.Cells[row, i + 1].Style.Font.Bold = true;
        //                        worksheet.Cells[row, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //                        worksheet.Cells[row, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        //                    }
        //                    row++;
        //                }

        //                // ✅ Insert Main Table Data
        //                for (int col = 0; col < headers.Count; col++)
        //                {
        //                    worksheet.Cells[row, col + 1].Value = mainRow[headers[col]].ToString();
        //                }
        //                row++;

        //                // ✅ Insert Sub-Table Below
        //                if (mainRow.ContainsKey("SubTable") && mainRow["SubTable"] is List<Dictionary<string, object>> subTableData)
        //                {
        //                    if (subTableData.Any())
        //                    {
        //                        worksheet.Cells[row, 1].Value = "Sub-Table Data:";
        //                        worksheet.Cells[row, 1].Style.Font.Bold = true;
        //                        row++;

        //                        var subHeaders = subTableData.First().Keys.ToList();

        //                        // ✅ Insert Sub-Table Headers
        //                        for (int i = 0; i < subHeaders.Count; i++)
        //                        {
        //                            worksheet.Cells[row, i + 1].Value = subHeaders[i];
        //                            worksheet.Cells[row, i + 1].Style.Font.Bold = true;
        //                            worksheet.Cells[row, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //                            worksheet.Cells[row, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
        //                        }
        //                        row++;

        //                        // ✅ Insert Sub-Table Data
        //                        foreach (var subRow in subTableData)
        //                        {
        //                            for (int col = 0; col < subHeaders.Count; col++)
        //                            {
        //                                worksheet.Cells[row, col + 1].Value = subRow[subHeaders[col]].ToString();
        //                            }
        //                            row++;
        //                        }
        //                    }
        //                }

        //                row += 1; // Leave space before the next main entry
        //            }

        //            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        //            //return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ExtractedData.xlsx");
        //            byte[] fileBytes = package.GetAsByteArray();
        //            return File(
        //           fileContents: fileBytes,
        //           contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        //           fileDownloadName: "ExtractedData.xlsx"
        //);
        //        }
        //byte[] fileBytes = package.GetAsByteArray();
        //    return File(
        //   fileContents: fileBytes,
        //   contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        //   fileDownloadName: "ExtractedData.xlsx"

        //     [HttpPost("ExportToExcel")]
        //     public IActionResult ExportToExcel()
        //     {
        //         // ✅ Retrieve extracted data from TempData
        //         var modelJson = TempData["ExtractedData"] as string;
        //         if (string.IsNullOrEmpty(modelJson))
        //         {
        //             return BadRequest("No data available to export.");
        //         }

        //         // ✅ Keep TempData so it persists after this request
        //         TempData.Keep("ExtractedData");

        //         var model = JsonConvert.DeserializeObject<List<MainTableModel>>(modelJson);
        //         if (model == null || !model.Any())
        //         {
        //             return BadRequest("No data available to export.");
        //         }

        //         ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        //         using (var package = new ExcelPackage())
        //         {
        //             var worksheet = package.Workbook.Worksheets.Add("Extracted Data");

        //             // ✅ Define headers
        //             var headers = new string[]
        //             {
        //              "File Name", "IRN", "Acknowledge Number", "Acknowledge Date","Buyer GSTIN", "Buyer", "Buyer Address",
        //              "Buyer State", "Buyer Pin", "Buyer Contact", "Buyer Contact Number", "Buyer Email",
        //              "Ship To", "Ship To Address", "Ship Contact Person", "Ship Contact Number",
        //              "Invoice Number", "Invoice Date", "Delivery Note", "Terms of Payment",
        //              "Despatch Doc No", "Despatch Through", "Destination", "Vehicle No",
        //              "Description of Goods", "HSN Code", "Quantity", "Rate", "CGST", "SGST",
        //              "Total Amount", "Amount Chargeable", "Declaration", "Bank Name",
        //              "Account No", "IFSC Code"
        //             };

        //             // ✅ Add headers
        //             for (int i = 0; i < headers.Length; i++)
        //             {
        //                 worksheet.Cells[1, i + 1].Value = headers[i];
        //                 worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        //                 worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //                 worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        //             }

        //             // ✅ Populate data rows
        //             int row = 2;
        //             foreach (var data in model)
        //             {
        //                 worksheet.Cells[row, 1].Value = data.FileName;
        //                 worksheet.Cells[row, 2].Value = string.Join(", ", data.Irn);
        //                 worksheet.Cells[row, 3].Value = string.Join(", ", data.AcknowledgeNumber);
        //                 worksheet.Cells[row, 4].Value = string.Join(", ", data.AcknowledgeDate);
        //                 worksheet.Cells[row, 5].Value = string.Join(", ", data.BuyerGstin);
        //                 worksheet.Cells[row, 6].Value = string.Join(", ", data.Buyer);
        //                 worksheet.Cells[row, 7].Value = string.Join(", ", data.BuyerAddressLine1);
        //                 worksheet.Cells[row, 8].Value = string.Join(", ", data.BuyerState);
        //                 worksheet.Cells[row, 9].Value = string.Join(", ", data.BuyerPinCode);
        //                 worksheet.Cells[row, 10].Value = string.Join(", ", data.BuyerContactPerson);
        //                 worksheet.Cells[row, 11].Value = string.Join(", ", data.BuyerContactNumber);
        //                 worksheet.Cells[row, 12].Value = string.Join(", ", data.BuyerEmail);
        //                 worksheet.Cells[row, 13].Value = string.Join(", ", data.ShipTo);
        //                 worksheet.Cells[row, 14].Value = string.Join(", ", data.ShipToAddressLine1);
        //                 worksheet.Cells[row, 15].Value = string.Join(", ", data.ShipToContactPerson);
        //                 worksheet.Cells[row, 16].Value = string.Join(", ", data.ShipToContactNumber);
        //                 worksheet.Cells[row, 17].Value = string.Join(", ", data.InvoiceNumber);
        //                 worksheet.Cells[row, 18].Value = string.Join(", ", data.InvoiceDate);
        //                 worksheet.Cells[row, 19].Value = string.Join(", ", data.DeleiveryNote);
        //                 worksheet.Cells[row, 20].Value = string.Join(", ", data.TermsOfPayment);
        //                 worksheet.Cells[row, 21].Value = string.Join(", ", data.DespatchDocNo);
        //                 worksheet.Cells[row, 22].Value = string.Join(", ", data.DespatchThrough);
        //                 worksheet.Cells[row, 23].Value = string.Join(", ", data.Destination);
        //                 worksheet.Cells[row, 24].Value = string.Join(", ", data.VehicleNo);
        //                 worksheet.Cells[row, 25].Value = string.Join(", ", data.DescriptionOfGoods);
        //                 worksheet.Cells[row, 26].Value = string.Join(", ", data.HsnCode);
        //                 worksheet.Cells[row, 27].Value = string.Join(", ", data.Quantity);
        //                 worksheet.Cells[row, 28].Value = string.Join(", ", data.Rate);
        //                 worksheet.Cells[row, 29].Value = string.Join(", ", data.Cgst);
        //                 worksheet.Cells[row, 30].Value = string.Join(", ", data.Sgst);
        //                 worksheet.Cells[row, 31].Value = string.Join(", ", data.TotalAmount);
        //                 worksheet.Cells[row, 32].Value = string.Join(", ", data.AmountChargable);
        //                 worksheet.Cells[row, 33].Value = string.Join(", ", data.Declaration);
        //                 worksheet.Cells[row, 34].Value = string.Join(", ", data.BankName);
        //                 worksheet.Cells[row, 35].Value = string.Join(", ", data.AcctNo);
        //                 worksheet.Cells[row, 36].Value = string.Join(", ", data.IfscCode);
        //                 row++;
        //                 //foreach (var table in data.ExtractedTables)
        //                 //{
        //                 //    // ✅ Insert Table Headers Below the Main Row
        //                 //    for (int col = 0; col < table.Headers.Count; col++)
        //                 //    {
        //                 //        worksheet.Cells[row, col + 1].Value = table.Headers[col];
        //                 //        worksheet.Cells[row, col + 1].Style.Font.Bold = true;
        //                 //        worksheet.Cells[row, col + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //                 //        worksheet.Cells[row, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
        //                 //    }
        //                 //    row++;

        //                 //    // ✅ Insert Table Data Below Headers
        //                 //    foreach (var rowData in table.Rows)
        //                 //    {
        //                 //        for (int col = 0; col < rowData.Count; col++)
        //                 //        {
        //                 //            worksheet.Cells[row, col + 1].Value = rowData[col];
        //                 //        }
        //                 //        row++;
        //                 //    }

        //                 //    row++; // Leave a gap after each table for readability
        //                 //}
        //                 InsertStructuredTables(worksheet, data.ExtractedTables, ref row);
        //             }

        //             // ✅ Auto-fit columns
        //             worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        //             //// ✅ Sheet 2: Extracted Dynamic Tables
        //             //var tableSheet = package.Workbook.Worksheets.Add("Extracted Tables");
        //             //int tableRow = 1;

        //             //foreach (var data in model)
        //             //{
        //             //    foreach (var table in data.ExtractedTables)
        //             //    {
        //             //        // ✅ Write Table Title
        //             //        tableSheet.Cells[tableRow, 1].Value = $"Table from {data.FileName}";
        //             //        tableSheet.Cells[tableRow, 1].Style.Font.Bold = true;
        //             //        tableRow++;

        //             //        // ✅ Write Table Headers
        //             //        for (int col = 0; col < table.Headers.Count; col++)
        //             //        {
        //             //            tableSheet.Cells[tableRow, col + 1].Value = table.Headers[col];
        //             //            tableSheet.Cells[tableRow, col + 1].Style.Font.Bold = true;
        //             //            tableSheet.Cells[tableRow, col + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //             //            tableSheet.Cells[tableRow, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        //             //        }
        //             //        tableRow++;

        //             //        // ✅ Write Table Rows
        //             //        foreach (var rowData in table.Rows)
        //             //        {
        //             //            for (int col = 0; col < rowData.Count; col++)
        //             //            {
        //             //                tableSheet.Cells[tableRow, col + 1].Value = rowData[col];
        //             //            }
        //             //            tableRow++;
        //             //        }

        //             //        tableRow += 2; // Space between tables
        //             //    }
        //             //}

        //             //tableSheet.Cells[tableSheet.Dimension.Address].AutoFitColumns();

        //             // ✅ Properly handle the MemoryStream
        //             using (var stream = new MemoryStream())
        //             {
        //                 package.SaveAs(stream);
        //                 stream.Position = 0; // ✅ Reset position before returning

        //                 // ✅ Convert to byte array
        //                 //byte[] fileBytes = stream.ToArray();
        //                 //string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        //                 //string filePath = Path.Combine(downloadsPath, "TestExcel.xlsx");
        //                 //System.IO.File.WriteAllBytes(filePath, fileBytes);
        //                 //return Json("");
        //                 //if (fileBytes == null || fileBytes.Length == 0)
        //                 //{
        //                 //    return BadRequest("Error: Empty Excel file.");
        //                 //}

        //                 //return File(
        //                 //    fileContents: fileBytes,
        //                 //    contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        //                 //    fileDownloadName: "ExtractedData.xlsx"
        //                 //);
        //                 byte[] fileBytes = stream.ToArray();
        //                 return File(
        //                fileContents: fileBytes,
        //                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        //                fileDownloadName: "ExtractedData.xlsx"
        //);
        //             }
        //         }
        //     }

        private static void InsertStructuredTables(ExcelWorksheet worksheet, List<TableModel> extractedTables, ref int row)
        {
            foreach (var table in extractedTables)
            {
                if (table.Headers.Count == 0 || table.Rows.Count == 0)
                    continue; // Skip empty tables

                // ✅ Merge cells to indicate extracted table section
                worksheet.Cells[row, 1, row, table.Headers.Count].Merge = true;
                worksheet.Cells[row, 1].Value = "Extracted Table Below:";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                row++;

                // ✅ Insert Extracted Table Headers (Use predefined headers)
                for (int col = 0; col < table.Headers.Count; col++)
                {
                    worksheet.Cells[row, col + 1].Value = table.Headers[col];
                    worksheet.Cells[row, col + 1].Style.Font.Bold = true;
                    worksheet.Cells[row, col + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[row, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                }
                row++;

                // ✅ Insert Extracted Table Data
                foreach (var rowData in table.Rows)
                {
                    for (int col = 0; col < rowData.Count; col++)
                    {
                        worksheet.Cells[row, col + 1].Value = rowData[col];
                    }
                    row++;
                }

                row += 1; // Leave space after each table
            }
        }

    }
}
