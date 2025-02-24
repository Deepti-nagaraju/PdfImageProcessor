using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure;
using Microsoft.AspNetCore.Http;
using PdfImageProcessor.Models;
using PdfImageProcessor.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static PdfImageProcessor.Controllers.PdfController;

namespace PdfImageProcessor.Services
{
    public class PdfProcessingService
    {
        private const string Endpoint = "https://deepti.cognitiveservices.azure.com/";
        private const string ApiKey = "3lUsGeSbyFujvN5DM45mYggERcTBcob26fhxqwSSXixhWi1PMwkhJQQJ99BBACGhslBXJ3w3AAALACOGVVET";

        private readonly DocumentAnalysisClient _client;

        public PdfProcessingService()
        {
            _client = new DocumentAnalysisClient(new Uri(Endpoint), new AzureKeyCredential(ApiKey));
        }

        public async Task<List<MainTableModel>> ProcessPdfAsync(IFormFileCollection files)
        {
            var extractedDataList = new List<MainTableModel>();

            foreach (var file in files)
            {
                var extractedData = new MainTableModel { FileName = file.FileName };
                using var stream = file.OpenReadStream();
                var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-document", stream);
                var result = operation.Value;

                decimal totalAmount = 0;
                decimal totalTax = 0;
                decimal totalQuantity = 0;
                decimal totalRate = 0;
                extractedData.ExtractedTables = StructureExtractedTables(result,out totalAmount,out totalRate,out totalQuantity);
                ExtractKeyValuePairs(result, extractedData, totalAmount, totalRate, totalQuantity);

                extractedDataList.Add(extractedData);
            }

            return extractedDataList;
        }

        //private List<TableModel> ExtractTables(AnalyzeResult result)
        //{
        //    var tables = new List<TableModel>();

        //    foreach (var table in result.Tables)
        //    {
        //        var headers = table.Cells
        //            .Where(c => c.RowIndex == 0)
        //            .Select(c => c.Content)
        //            .ToList();

        //        if (headers.Any(header => RegexHelper.IsValidTableHeader(header)))
        //        {
        //            var extractedTable = new TableModel
        //            {
        //                Headers = headers,
        //                Rows = table.Cells.Where(c => c.RowIndex > 0)
        //                    .GroupBy(c => c.RowIndex)
        //                    .Select(row => row.OrderBy(c => c.ColumnIndex).Select(c => c.Content).ToList())
        //                    .ToList()
        //            };

        //            tables.Add(extractedTable);
        //        }
        //    }

        //    return tables;
        //}
        public static List<TableModel> StructureExtractedTables(AnalyzeResult result, out decimal totalAmount, out decimal totalRate,  out decimal totalQuantity)
        {
            //List<TableModel> extractedTables = result.Tables;
            var structuredTables = new List<TableModel>();
            totalAmount = 0;
            totalQuantity = 0;
            totalRate = 0;

            foreach (var table in result.Tables)
            {
                var headers = table.Cells
                    .Where(c => c.RowIndex == 0)
                    .Select(c => c.Content)
                    .ToList();
                var rows = table.Cells.Where(c => c.RowIndex > 0)
                            .GroupBy(c => c.RowIndex)
                            .Select(row => row.OrderBy(c => c.ColumnIndex).Select(c => c.Content).ToList());
                if (headers.Any(header => RegexHelper.IsValidTableHeader(header))) { 
                            
                    var structuredTable = new TableModel
                    {
                        Headers = new List<string> { "Sl No", "Description", "HSN Code", "Quantity", "Rate Per Quantity","Taxable Value","SGST%", "CGST%", "IGST%","SGST", "CGST", "IGST", "Amount" },
                        Rows = new List<List<string>>()
                    };

                    int serialNumber = 1;

                    foreach (var row in rows)
                    {
                        var description = GetColumnValue(row, headers, new List<string> { "Description", "Item", "Product", "Model", "Vessel" }, new List<string> { "" });

                        // ✅ Check if the row should be ignored
                        if (!string.IsNullOrEmpty(description) && description.Trim().ToLower().Contains("total"))
                        {
                            continue; // ✅ Skip this row
                        }
                        var quantity = CleanNumericValue(GetColumnValue(row, headers, new List<string> { "Quantity", "Qty" }, new List<string> { "" }));
                        var ratePer = CleanNumericValue(GetColumnValue(row, headers, new List<string> { "Rate", "Rate Per", "Price", "MRP" }, new List<string> { "" }));
                        var amount = CleanNumericValue(GetColumnValue(row, headers, new List<string> { "Amount", "Amt" }, new List<string> { "" }));

                        // ✅ Convert cleaned values to decimal for summing
                        decimal amountValue = decimal.TryParse(amount, out decimal amt) ? amt : 0;
                        totalAmount += amountValue;

                        decimal rateValue = decimal.TryParse(ratePer, out decimal rate) ? rate : 0;
                        totalRate += rateValue;

                        decimal qtyValue = decimal.TryParse(quantity, out decimal qty) ? qty : 0;
                        totalQuantity += qtyValue;


                        var mappedRow = new List<string>
                        {
                            serialNumber.ToString(),  // Sl No
                            GetColumnValue(row, headers, new List<string> { "Description","Item","Product","Model","Vessel" },new List<string> { "" }),
                            GetColumnValue(row, headers, new List<string> { "HSN","Item Code","SAC","code" },new List<string> { "" }),
                            GetColumnValue(row, headers, new List<string> { "Quantity", "Qty" },new List<string> { "" }),
                            GetColumnValue(row, headers, new List<string> { "Rate", "Rate Per","Price","MRP" },new List<string> { "" }),
                            GetColumnValue(row, headers, new List<string> { "tax","value"},new List<string> { "" }),
                            GetColumnValue(row, headers, new List<string> { "SGST","state" },new List<string> {"%","rate" }),
                            GetColumnValue(row, headers, new List<string> { "CGST","central" },new List<string> {"%","rate" }),
                            GetColumnValue(row, headers, new List<string> { "IGST" },new List<string> {"%","rate"}),
                            GetColumnValueForTaxAmount(row, headers, new List<string> { "SGST","state" },new List<string> {"%","rate"}),
                            GetColumnValueForTaxAmount(row, headers, new List<string> { "CGST","central" },new List<string> {"%","rate"}),
                            GetColumnValueForTaxAmount(row, headers, new List<string> { "IGST" },new List<string> {"%","rate"}),
                            GetColumnValue(row, headers, new List<string> { "Amount", "Amt" },new List<string> { "" })
                        };

                        structuredTable.Rows.Add(mappedRow);
                        serialNumber++;
                    }

                    structuredTables.Add(structuredTable);
                    break;
                }
            }

            return structuredTables;
        }

        private static string GetColumnValue(List<string> row, List<string> headers, List<string> possibleHeadersPrimary, List<string> possibleHeadersSecondary)
        {
            int index = headers.FindIndex(h => possibleHeadersPrimary.Any(ph => h.Contains(ph, StringComparison.OrdinalIgnoreCase))&& possibleHeadersSecondary.Any(ph => h.Contains(ph, StringComparison.OrdinalIgnoreCase)));
            return (index >= 0 && index < row.Count) ? row[index] : "";
        }
        private static string GetColumnValueForTaxAmount(List<string> row, List<string> headers, List<string> possibleHeadersPrimary, List<string> possibleHeadersSecondary)
        {
            int index = headers.FindIndex(h => possibleHeadersPrimary.Any(ph => h.Contains(ph, StringComparison.OrdinalIgnoreCase)) && !possibleHeadersSecondary.Any(ph => h.Contains(ph, StringComparison.OrdinalIgnoreCase)));
            return (index >= 0 && index < row.Count) ? row[index] : "";
        }
        private static string CleanNumericValue(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "0"; // Default value if input is empty

            return Regex.Replace(input, @"[^\d.]", ""); // Remove all non-numeric characters except the decimal point
        }



        private void ExtractKeyValuePairs(AnalyzeResult result, MainTableModel extractedData,decimal totalAmount, decimal totalRate,decimal totalQuantity)
        {
            
            foreach (var field in result.KeyValuePairs)
            {
                var key = field.Key?.Content?.Trim().ToLower() ?? "";
                var value = field.Value?.Content?.Trim() ?? "";

                if (!string.IsNullOrEmpty(value))
                {
                    if (key.Contains("irn")) extractedData.Irn.Add(value);
                    if (key.Contains("ack") && ((key.Contains("no") || key.Contains("number")))) extractedData.AcknowledgeNumber.Add(value);
                    if (key.Contains("ack") && ((key.Contains("date") || key.Contains("dt")))) extractedData.AcknowledgeDate.Add(value);

                    if (key.Contains("buyer") || key.Contains("bill to") || key.Contains("customer") || key.Contains("address")|| key.Contains("billed to")|| key.Contains("consignor"))
                    {
                        string remainingText = value.Trim();
                        var companyMatch = RegexHelper.CompanyNameRegex.Match(value);
                        if (companyMatch.Success)
                        {
                            extractedData.Buyer.Add(companyMatch.Value.Trim()); // ✅ Add only the matched company name
                            remainingText = remainingText.Replace(companyMatch.Value, "").Trim(); // Remove extracted company name
                        }
                        var gstin = RegexHelper.GstinRegex.Match(value);
                        if (gstin.Success)
                        {
                            extractedData.BuyerGstin.Add(gstin.Value.Trim()); // ✅ Add only the matched company name
                            remainingText = remainingText.Replace(gstin.Value, "").Trim(); // Remove extracted GSTIN
                        }
                        var email = RegexHelper.EmailRegex.Match(value);
                        if (email.Success)
                        {
                            extractedData.BuyerEmail.Add(email.Value.Trim()); // ✅ Add only the matched company name
                            remainingText = remainingText.Replace(email.Value, "").Trim(); // Remove extracted Contact Number
                        }
                        var contactNumber = RegexHelper.MobileNumberRegex.Match(value);
                        if (contactNumber.Success)
                        {
                            extractedData.BuyerContactNumber.Add(contactNumber.Value.Trim()); // ✅ Add only the matched company name
                            remainingText = remainingText.Replace(contactNumber.Value, "").Trim(); // Remove extracted Email
                        }
                        var pinCode = RegexHelper.PinCodeRegex.Match(value);
                        if (pinCode.Success)
                        {
                            extractedData.BuyerPinCode.Add(pinCode.Value.Trim()); // ✅ Add only the matched company name
                            remainingText = remainingText.Replace(pinCode.Value, "").Trim(); // Remove extracted Email
                        }
                        if (!string.IsNullOrEmpty(remainingText))
                        {
                            extractedData.BuyerAddressLine1.Add(remainingText);
                        }
                    }
                   

                    if (key.Contains("gst") && extractedData.BuyerGstin.Count == 0)
                    {
                        extractedData.BuyerGstin.Add(value.Trim());
                    }

                    //if ((key.Contains("seller")) && RegexHelper.CompanyNameRegex.IsMatch(value) || key.Contains("name") || key.Contains("address")|| key.Contains("deli")|| key.Contains("consignee") || key.Contains("invoice to") || key.Contains("delivery") || key.Contains("shipper"))
                    //{
                    //    string remainingText = value.Trim();
                    //    var companyMatch = RegexHelper.CompanyNameRegex.Match(value);
                    //    if (companyMatch.Success)
                    //    {
                    //        extractedData.Seller.Add(companyMatch.Value.Trim()); // ✅ Add only the matched company name
                    //        remainingText = remainingText.Replace(companyMatch.Value, "").Trim(); // Remove extracted company name
                    //    }
                    //    var gstin = RegexHelper.GstinRegex.Match(value);
                    //    if (gstin.Success)
                    //    {
                    //        extractedData.SellerGstin.Add(gstin.Value.Trim()); // ✅ Add only the matched company name
                    //        remainingText = remainingText.Replace(gstin.Value, "").Trim(); // Remove extracted GSTIN
                    //    }
                    //    var email = RegexHelper.EmailRegex.Match(value);
                    //    if (email.Success)
                    //    {
                    //        extractedData.SellerEmail.Add(email.Value.Trim()); // ✅ Add only the matched company name
                    //        remainingText = remainingText.Replace(email.Value, "").Trim(); // Remove extracted Contact Number
                    //    }
                    //    var contactNumber = RegexHelper.MobileNumberRegex.Match(value);
                    //    if (contactNumber.Success)
                    //    {
                    //        extractedData.SellerContactNumber.Add(contactNumber.Value.Trim()); // ✅ Add only the matched company name
                    //        remainingText = remainingText.Replace(contactNumber.Value, "").Trim(); // Remove extracted Email
                    //    }
                    //    var pinCode = RegexHelper.PinCodeRegex.Match(value);
                    //    if (pinCode.Success)
                    //    {
                    //        extractedData.SellerPinCode.Add(pinCode.Value.Trim()); // ✅ Add only the matched company name
                    //        remainingText = remainingText.Replace(pinCode.Value, "").Trim(); // Remove extracted Email
                    //    }
                    //    if (!string.IsNullOrEmpty(remainingText))
                    //    {
                    //        extractedData.SellerAddressLine1.Add(remainingText);
                    //    }
                    //}
                    //if (key.Contains("gst") && extractedData.BuyerGstin.Count == 0)
                    //{
                    //    extractedData.SellerGstin.Add(value.Trim());
                    //}
                    if ((key.Contains("ship")) || RegexHelper.CompanyNameRegex.IsMatch(value) || key.Contains("address") || key.Contains("ultimate consignee"))
                    {
                        string remainingText = value.Trim();
                        var companyMatch = RegexHelper.CompanyNameRegex.Match(value);
                        if (companyMatch.Success)
                        {
                            extractedData.ShipTo.Add(companyMatch.Value.Trim()); // ✅ Add only the matched company name
                            remainingText = remainingText.Replace(companyMatch.Value, "").Trim(); // Remove extracted company name
                        }

                        var email = RegexHelper.EmailRegex.Match(value);
                        if (email.Success)
                        {
                            extractedData.ShipToEmail.Add(email.Value.Trim()); // ✅ Add only the matched company name
                            remainingText = remainingText.Replace(email.Value, "").Trim(); // Remove extracted Contact Number
                        }
                        var contactNumber = RegexHelper.MobileNumberRegex.Match(value);
                        if (contactNumber.Success)
                        {
                            extractedData.ShipToContactNumber.Add(contactNumber.Value.Trim()); // ✅ Add only the matched company name
                            remainingText = remainingText.Replace(contactNumber.Value, "").Trim(); // Remove extracted Email
                        }
                        //var pinCode = RegexHelper.PinCodeRegex.Match(value);
                        //if (pinCode.Success)
                        //{
                        //    extractedData.ShipToPinCode.Add(pinCode.Value.Trim()); // ✅ Add only the matched company name
                        //    remainingText = remainingText.Replace(pinCode.Value, "").Trim(); // Remove extracted Email
                        //}
                        if (!string.IsNullOrEmpty(remainingText))
                        {
                            extractedData.ShipToAddressLine1.Add(remainingText);
                        }
                    }


                    if (key.Contains("invoice number") || key.Contains("invoice no") || key.Contains("order no") || key.Contains("pl no")) extractedData.InvoiceNumber.Add(value);
                    if (key.Contains("date")) extractedData.InvoiceDate.Add(value);
                    if (key.Contains("note")) extractedData.DeleiveryNote.Add(value);
                    if (key.Contains("payment") && key.Contains("term")) extractedData.TermsOfPayment.Add(value);
                    if (key.Contains("despatch") && ((key.Contains("number") || key.Contains("no")))) extractedData.DespatchDocNo.Add(value);
                    if (key.Contains("through") || (key.Contains("transport"))) extractedData.DespatchThrough.Add(value);
                    if (key.Contains("vehicle no")|| key.Contains("vehicle number")) extractedData.VehicleNo.Add(value);
                    if (key.Contains("destination")) extractedData.Destination.Add(value);
                    if (key.Contains("state")|| key.Contains("place")) extractedData.BuyerState.Add(value);


                    //if (key.Contains("description of goods")) extractedData.DescriptionOfGoods.Add(value);
                    //if (key.Contains("hsn")) extractedData.HsnCode.Add(value);
                    //if (key.Contains("qty") || key.Contains("quantity")) extractedData.Quantity.Add(value);
                    //if (key.Contains("rate")) extractedData.Rate.Add(value);
                    //if (key.Contains("cgst")) extractedData.Cgst.Add(value);
                    //if (key.Contains("sgst")) extractedData.Sgst.Add(value);
                    //if (key.Contains("igst")) extractedData.Igst.Add(value);
                    //if (key.Contains("total amount") || key.Contains("total amt")) extractedData.TotalAmount.Add(value);
                    //if (key.Contains("charg")) extractedData.TotalAmount.Add(value);
                    //if (key.Contains("declaration")) extractedData.Declaration.Add(value);
                    extractedData.Rate.Add(totalRate.ToString());
                    extractedData.TotalAmount.Add(totalAmount.ToString());
                    extractedData.Quantity.Add(totalQuantity.ToString());


                    if (key.Contains("ifs")) extractedData.IfscCode.Add(value);
                    if (key.Contains("bank")) extractedData.BankName.Add(value);
                    if (key.Contains("account number") || key.Contains("acct no")|| key.Contains("account no")|| key.Contains("a/c")|| key.Contains("acct number")) extractedData.AcctNo.Add(value);
                    if (key.Contains("eway")|| key.Contains("ebill")|| key.Contains("e-way") || key.Contains("e-bill") || key.Contains("e way") || key.Contains("e bill")) extractedData.EWayBill.Add(value);
                }
            }
            //After scanning through all key value pairs gain scan through for capturing special cases
            foreach (var field in result.KeyValuePairs)
            {
                var key = field.Key?.Content?.Trim().ToLower() ?? "";
                var value = field.Value?.Content?.Trim() ?? "";

                if (!string.IsNullOrEmpty(value))
                {
                    if(extractedData.BuyerContactNumber.Count==0)
                    {
                        if(key=="t"||key=="m"||key.Contains("mobile")||key.Contains("telephone") || (key.Contains("contact") && RegexHelper.HasNumbersRegex.IsMatch(value)))
                        {
                            extractedData.BuyerContactNumber.Add(value);
                        }
                        
                    }
                    if (extractedData.ShipToContactNumber.Count == 0)
                    {                      
                        if (key == "t" || key == "m" || key.Contains("mobile") || key.Contains("telephone")||(key.Contains("contact")&& RegexHelper.HasNumbersRegex.IsMatch(value)))
                        {
                            extractedData.ShipToContactNumber.Add(value);
                        }
                    }

                }
            }
            var emails = RegexHelper.EmailRegex.Matches(result.Content).Select(match => match.Value).ToList();
            var companyNames = RegexHelper.CompanyNameRegex.Matches(result.Content).Select(match => match.Value).ToList();

            if (extractedData.BuyerEmail.Count==0)
            {
                extractedData.BuyerEmail = emails;
            }
            if(extractedData.ShipToEmail.Count==0)
            {
                extractedData.ShipToEmail = emails;
            }
            if(extractedData.Buyer.Count==0)
            {
                extractedData.Buyer = companyNames;
            }

            // Ensure unique values for each field
            extractedData.Irn = extractedData.Irn.Distinct().ToList();
            extractedData.AcknowledgeNumber = extractedData.AcknowledgeNumber.Distinct().ToList();
            extractedData.AcknowledgeDate = extractedData.AcknowledgeDate.Distinct().ToList();

            extractedData.Buyer = extractedData.Buyer.Distinct().ToList();
            extractedData.BuyerAddressLine1 = extractedData.BuyerAddressLine1.Distinct().ToList();
            //extractedData.BuyerCity = extractedData.BuyerCity.Distinct().ToList();
            extractedData.BuyerState = extractedData.BuyerState.Distinct().ToList();
            extractedData.BuyerPinCode = extractedData.BuyerPinCode.Distinct().ToList();
            extractedData.BuyerEmail = extractedData.BuyerEmail.Distinct().ToList();
            extractedData.BuyerContactPerson = extractedData.BuyerContactPerson.Distinct().ToList();
            extractedData.BuyerContactNumber = extractedData.BuyerContactNumber.Distinct().ToList();
            extractedData.BuyerGstin = extractedData.BuyerGstin.Distinct().ToList();

            //extractedData.Seller = extractedData.Seller.Distinct().ToList();
            //extractedData.SellerAddressLine1 = extractedData.SellerAddressLine1.Distinct().ToList();
            //extractedData.SellerCity = extractedData.SellerCity.Distinct().ToList();
            //extractedData.SellerState = extractedData.SellerState.Distinct().ToList();
            //extractedData.SellerPinCode = extractedData.SellerPinCode.Distinct().ToList();
            //extractedData.SellerEmail = extractedData.SellerEmail.Distinct().ToList();
            //extractedData.SellerContactPerson = extractedData.SellerContactPerson.Distinct().ToList();
            //extractedData.SellerContactNumber = extractedData.SellerContactNumber.Distinct().ToList();
            //extractedData.SellerGstin = extractedData.SellerGstin.Distinct().ToList();

            extractedData.ShipTo = extractedData.ShipTo.Distinct().ToList();
            extractedData.ShipToAddressLine1 = extractedData.ShipToAddressLine1.Distinct().ToList();
            //extractedData.ShipToCity = extractedData.ShipToCity.Distinct().ToList();
            //extractedData.ShipToCity = extractedData.ShipToCity.Distinct().ToList();
            //extractedData.ShipToPinCode = extractedData.ShipToPinCode.Distinct().ToList();
            extractedData.ShipToEmail = extractedData.ShipToEmail.Distinct().ToList();
            extractedData.ShipToContactPerson = extractedData.ShipToContactPerson.Distinct().ToList();
            extractedData.ShipToContactNumber = extractedData.ShipToContactNumber.Distinct().ToList();


            //extractedData.DescriptionOfGoods = extractedData.DescriptionOfGoods.Distinct().ToList();
            //extractedData.HsnCode = extractedData.HsnCode.Distinct().ToList();
            extractedData.Quantity = extractedData.Quantity.Distinct().ToList();
            extractedData.Rate = extractedData.Rate.Distinct().ToList();
            extractedData.Sgst = extractedData.Sgst.Distinct().ToList();
            extractedData.Cgst = extractedData.Cgst.Distinct().ToList();
            extractedData.Igst = extractedData.Igst.Distinct().ToList();
            extractedData.TotalAmount = extractedData.TotalAmount.Distinct().ToList();
            //extractedData.AmountChargable = extractedData.AmountChargable.Distinct().ToList();

            //extractedData.Declaration = extractedData.Declaration.Distinct().ToList();
            extractedData.BankName = extractedData.BankName.Distinct().ToList();
            extractedData.IfscCode = extractedData.IfscCode.Distinct().ToList();
            extractedData.AcctNo = extractedData.AcctNo.Distinct().ToList();

            extractedData.EWayBill = extractedData.EWayBill.Distinct().ToList();
        }
    }
}
