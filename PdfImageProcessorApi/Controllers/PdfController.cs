using Microsoft.AspNetCore.Mvc;
using PdfImageProcessor.Services;
using PdfImageProcessor.Models;
using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Authorization;
using PdfImageProcessorApi.Models;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PdfImageProcessor.Controllers
{
     [EnableCors("AllowAllOrigins")]
    [ApiController]
    [Route("api/pdf")]
    public class PdfController : ControllerBase
    {
        private readonly PdfProcessingService _pdfProcessingService;
        private readonly InvoiceDbContext _context;

        public PdfController(PdfProcessingService pdfProcessingService, InvoiceDbContext context)
        {
            _pdfProcessingService = pdfProcessingService;
            _context = context;
        }

        /// <summary>
        /// Uploads a PDF and processes the extracted data.
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadPdf(IFormFileCollection files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { success = false, message = "Please upload at least one valid PDF file." });
            }
            var filesToProcess = new List<IFormFile>();
            foreach (var file in files)
            {
                var fileExist = await _context.Filestore
                .FirstOrDefaultAsync(f => f.SourceFileName == file.FileName);
                if (fileExist != null)
                {
                    // Optionally log or notify that the file is already processed
                    continue; // skip this file
                }
                var filestore = new Filestore
                {
                    OrgId = 1, // Set this based on your logic
                    FinancialYear = DateTime.Now.Year.ToString(),
                    InvoiceFor = "Invoice", // Populate from processing if available
                    SourceFileName = file.FileName,
                    Status = "Uploaded",
                    CreatedBy = "system", // Or actual user
                    CreationDate = DateTime.UtcNow,
                };
                _context.Filestore.Add(filestore);
                filesToProcess.Add(file);
            }
            await _context.SaveChangesAsync();
            var extractedDataList = await _pdfProcessingService.ProcessPdfAsync(filesToProcess);

            if (extractedDataList?.Count > 0)
            {
                return Ok(new { success = true, data = extractedDataList });
            }

            return BadRequest(new { success = false, message = "No data extracted from the uploaded PDF(s)." });
        }

        /// <summary>
        /// Exports extracted data to an Excel file.
        /// </summary>
        [HttpPost("export-to-excel")]
        public IActionResult ExportToExcel([FromBody] List<ExcelModel> tableData)
        {
            if (tableData == null || !tableData.Any())
                return BadRequest("No data available to export.");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Extracted Data");

            // Define headers
            var headers = new List<string>
            {
                "File Name", "IRN", "Acknowledgement Number", "Acknowledge Date",
                "Buyer", "Buyer GSTIN", "Buyer Address", "Buyer State", "Buyer Pin",
                "Buyer Contact", "Buyer Contact Number", "Buyer Email",
                "Ship To", "Ship To Address", "Ship To Contact Person", "Ship To Contact Number", "Ship to Email",
                "Invoice Number", "Invoice Date", "Eway Bill Number", "Delivery Note", "Terms of Payment",
                "Despatch Doc No", "Despatch Through", "Destination", "Vehicle No",
                "Description of Goods", "HSN Code", "Quantity", "Rate", "CGST", "SGST", "IGST",
                "Total Amount", "Bank Name", "Account No", "IFSC Code"
            };

            // Insert headers
            for (int i = 0; i < headers.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Insert data rows
            int row = 2;
            foreach (var mainRow in tableData)
            {
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
                worksheet.Cells[row, 17].Value = string.Join(", ", mainRow.InvoiceNumber);
                worksheet.Cells[row, 18].Value = string.Join(", ", mainRow.InvoiceDate);
                worksheet.Cells[row, 19].Value = string.Join(", ", mainRow.EWayBill);
                worksheet.Cells[row, 20].Value = string.Join(", ", mainRow.DeleiveryNote);
                worksheet.Cells[row, 21].Value = string.Join(", ", mainRow.TermsOfPayment);
                worksheet.Cells[row, 22].Value = string.Join(", ", mainRow.DespatchDocNo);
                worksheet.Cells[row, 23].Value = string.Join(", ", mainRow.DespatchThrough);
                worksheet.Cells[row, 24].Value = string.Join(", ", mainRow.Destination);
                worksheet.Cells[row, 25].Value = string.Join(", ", mainRow.VehicleNo);
                worksheet.Cells[row, 26].Value = string.Join(", ", mainRow.DescriptionOfGoods);
                worksheet.Cells[row, 27].Value = string.Join(", ", mainRow.HsnCode);
                worksheet.Cells[row, 28].Value = string.Join(", ", mainRow.Quantity);
                worksheet.Cells[row, 29].Value = string.Join(", ", mainRow.Rate);
                worksheet.Cells[row, 30].Value = string.Join(", ", mainRow.Cgst);
                worksheet.Cells[row, 31].Value = string.Join(", ", mainRow.Sgst);
                worksheet.Cells[row, 32].Value = string.Join(", ", mainRow.Igst);
                worksheet.Cells[row, 33].Value = string.Join(", ", mainRow.TotalAmount);
                worksheet.Cells[row, 34].Value = string.Join(", ", mainRow.BankName);
                worksheet.Cells[row, 35].Value = string.Join(", ", mainRow.AcctNo);
                worksheet.Cells[row, 36].Value = string.Join(", ", mainRow.IfscCode);
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            byte[] fileBytes = package.GetAsByteArray();
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ExtractedData.xlsx");
        }

        [HttpGet("get-processed-data")]
        public async Task<IActionResult> GetInvoiceDataByFileNameAsync()
        {
          
            var baseRecords = await (
                from file in _context.Filestore
                join meta in _context.FileMetadata
                    on file.SourceFileName equals meta.FileName into metaGroup
                from meta in metaGroup.DefaultIfEmpty()
                select new { file, meta }
            ).ToListAsync();

            // Populate InvoiceItems manually in-memory
            var result = baseRecords.Select(record => new InvoiceDocumentDto
            {
                Filestore = record.file,
                Metadata = record.meta,
                InvoiceItems = _context.InvoiceItem
                    .Where(i => i.SourceFileName == record.file.SourceFileName)
                    .ToList()
            }).ToList();

            return Ok(result);

        }


    }
}
