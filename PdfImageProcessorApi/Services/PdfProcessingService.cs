using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure;
using Microsoft.AspNetCore.Http;
using PdfImageProcessor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection.PortableExecutable;
using PdfImageProcessor.Utilities;

namespace PdfImageProcessor.Services
{
    public class PdfProcessingService
    {
        private const string Endpoint = "https://vrtekh-doc-int.cognitiveservices.azure.com/";
        private const string ApiKey = "GIsd8NQSsh9VKgyDHRR457U1dFCrP1v4WPxWVplu6SwP4hURjbwdJQQJ99BCACGhslBXJ3w3AAALACOGJ8Ad";

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
                extractedData.ExtractedTables = StructureExtractedTables(result,out totalAmount,out totalRate,out totalQuantity,out decimal cgst,out decimal sgst,out decimal igst);
                ExtractKeyValuePairs(result, extractedData, totalAmount, totalRate, totalQuantity, cgst, sgst, igst);

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
        public static List<TableModel> StructureExtractedTables(AnalyzeResult result, out decimal totalAmount, out decimal totalRate,  out decimal totalQuantity,out decimal sgst, out decimal cgst, out decimal igst)
        {
            //List<TableModel> extractedTables = result.Tables;
            var structuredTables = new List<TableModel>();
            totalAmount = 0;
            totalQuantity = 0;
            totalRate = 0;
            sgst = 0;
            cgst = 0;
            igst = 0;

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
                        var cgstFetch = CleanNumericValue(GetColumnValueForGST(row, headers, new List<string> { "CGST", "central" }, new List<string> { "%", "rate" }));
                        var sgstFetch = CleanNumericValue(GetColumnValueForGST(row, headers, new List<string> { "SGST", "state" }, new List<string> { "%", "rate" }));
                        var igstFetch = CleanNumericValue(GetColumnValueForGST(row, headers, new List<string> { "IGST" }, new List<string> { "%", "rate" }));

                    // ✅ Convert cleaned values to decimal for summing
                    decimal amountValue = decimal.TryParse(amount, out decimal amt) ? amt : 0;
                        totalAmount += amountValue;

                        decimal rateValue = decimal.TryParse(ratePer, out decimal rate) ? rate : 0;
                        totalRate += rateValue;

                        decimal qtyValue = decimal.TryParse(quantity, out decimal qty) ? qty : 0;
                        totalQuantity += qtyValue;

                        decimal cgstValue = decimal.TryParse(cgstFetch, out decimal cgstOut) ? cgstOut : 0;
                        cgst += cgstValue;
                        decimal sgstValue = decimal.TryParse(sgstFetch, out decimal sgstOut) ? sgstOut : 0;
                        sgst += sgstValue;
                        decimal igstValue = decimal.TryParse(igstFetch, out decimal igstOut) ? igstOut : 0;
                        igst += igstValue;

                        var mappedRow = new List<string>
                        {
                            serialNumber.ToString(),  // Sl No
                            GetColumnValue(row, headers, new List<string> { "Description","Item","Product","Model","Vessel" ,"particulars"},new List<string> { "" }),
                            GetColumnValue(row, headers, new List<string> { "HSN","Item Code","SAC","code"},new List<string> { "" }),
                            CleanNumericValue(GetColumnValue(row, headers, new List<string> { "Quantity", "Qty" },new List<string> { "" })),
                            CleanNumericValue(GetColumnValue(row, headers, new List<string> { "Rate", "Rate Per","Price","MRP" },new List<string> { "" })),
                            CleanNumericValue(GetColumnValue(row, headers, new List<string> { "tax","value"},new List<string> { "" })),
                            CleanNumericValue(GetColumnValue(row, headers, new List<string> { "SGST","state" },new List<string> {"%","rate" })),
                            CleanNumericValue(GetColumnValue(row, headers, new List<string> { "CGST","central" },new List<string> {"%","rate" })),
                            CleanNumericValue(GetColumnValue(row, headers, new List<string> { "IGST" },new List<string> {"%","rate"})),
                            CleanNumericValue(GetColumnValueForTaxAmount(row, headers, new List<string> { "SGST","state" },new List<string> {"%","rate"})),
                            CleanNumericValue(GetColumnValueForTaxAmount(row, headers, new List<string> { "CGST","central" },new List<string> {"%","rate"})),
                            CleanNumericValue(GetColumnValueForTaxAmount(row, headers, new List<string> { "IGST" },new List<string> {"%","rate"})),
                            CleanNumericValue(GetColumnValue(row, headers, new List<string> { "Amount", "Amt" },new List<string> { "" }))
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
        private static string GetColumnValueForGST(List<string> row, List<string> headers, List<string> possibleHeadersPrimary, List<string> possibleHeadersSecondary)
        {
            int index = headers.FindIndex(h => possibleHeadersPrimary.Any(ph => h.Contains(ph, StringComparison.OrdinalIgnoreCase)) && !possibleHeadersSecondary.Any(ph => h.Contains(ph, StringComparison.OrdinalIgnoreCase)));
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



        private void ExtractKeyValuePairs(AnalyzeResult result, MainTableModel extractedData,decimal totalAmount, decimal totalRate,decimal totalQuantity, decimal cgst, decimal sgst, decimal igst)
        {
            
            foreach (var field in result.KeyValuePairs)
            {
                var key = field.Key?.Content?.Trim().ToLower() ?? "";
                var value = field.Value?.Content?.Trim() ?? "";
                var valueToCheck = field.Value?.Content?.Trim().ToLower() ?? "";

                if (!string.IsNullOrEmpty(value))
                {
                    if (key.Contains("irn")) extractedData.Irn.Add(value);
                    if (key.Contains("ack") && ((key.Contains("no") || key.Contains("number")))) extractedData.AcknowledgeNumber.Add(value);
                    if (key.Contains("ack") && ((key.Contains("date") || key.Contains("dt")))) extractedData.AcknowledgeDate.Add(value);

                    if (key.Contains("buyer") || key.Contains("consigners name") || key.Contains("dell at") || key.Contains("bill to") || key.Contains("customer") || key.Contains("address") || key.Contains("billed to") || key.Contains("consignor"))
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


                    if (key.Contains("gst"))
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
                    if ((key.Contains("ship")) || RegexHelper.CompanyNameRegex.IsMatch(value) || key.Contains("consignee") || key.Contains("shipped to") || key.Contains("address") || key.Contains("ultimate consignee"))
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


                    if (key.Contains("invoice number") ||key.Contains("invoice")||key.Contains("pi no") || key.Contains("bill no") || key.Contains("inv.no") || key.Contains("inovice no\n:") || key.Contains("document no") || key.Contains("invoice serial number") || key.Contains("proforma no ") || key.Contains("invoice #") || key.Contains("invoice no") || key.Contains("order no") || key.Contains("pl no")) extractedData.InvoiceNumber.Add(value);
                    if (key.Contains("date") || key.Contains("invoice no & date")) extractedData.InvoiceDate.Add(value);
                    if (key.Contains("note")) extractedData.DeleiveryNote.Add(value);
                    if (key.Contains("payment") && key.Contains("term")) extractedData.TermsOfPayment.Add(value);
                    if ((key.Contains("despatch") || key.Contains("dispatch")) && ((key.Contains("number") || key.Contains("no")))) extractedData.DespatchDocNo.Add(value);
                    if (key.Contains("through") | (key.Contains(":\n"))|(key.Contains("transport name")) | (key.Contains("transport"))) extractedData.DespatchThrough.Add(value);
                    if (key.Contains("vehicle no") || key.Contains("vehical no")||key.Contains("vehicle number")) extractedData.VehicleNo.Add(value);
                    if (key.Contains("destination")|| key.Contains("final destination")) extractedData.Destination.Add(value);
                    if (key.Contains("state") || key.Contains("slate name") ||key.Contains("place")) extractedData.BuyerState.Add(value);
                    if (key.Contains("contact person")||(key.Contains("contact")))  extractedData.BuyerContactPerson.Add(value);
                    if (key.Contains("contact person") || (key.Contains("attn"))||(key.Contains("contact"))) extractedData.ShipToContactPerson.Add(value);
                    if (key.Contains("survey no")) extractedData.BuyerAddressLine1.Add(value);
                    if (key.Contains("survey no")) extractedData.ShipToAddressLine1.Add(value);


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
                    extractedData.Cgst.Add(cgst.ToString());
                    extractedData.Sgst.Add(sgst.ToString());
                    extractedData.Igst.Add(igst.ToString());


                    if (key.Contains("ifs")|| key.Contains("ifsc code")|| key.Contains("account no")||key.Contains("rtgs/ifcs code"))extractedData.IfscCode.Add(value);
                    if (key.Contains("bank")|| key.Contains("bank details") ||key.Contains("bank name")|| valueToCheck.Contains("bank"))  
                        extractedData.BankName.Add(value);
                    if (key.Contains("account number") || key.Contains("acct no")|| key.Contains("account no")|| key.Contains("a/c no")||  key.Contains("bank")||key.Contains("a/c")|| key.Contains("acct number") || key.Contains("account no") || key.Contains("account")) extractedData.AcctNo.Add(value);
                    if (key.Contains("eway")|| key.Contains("e way bill no") || key.Contains("bill no")||key.Contains("ewb no")||key.Contains("ebill")|| key.Contains("e-way") || key.Contains("e-bill") || key.Contains("e way") || key.Contains("e bill")) extractedData.EWayBill.Add(value);
                }
            }
            //After scanning through all key value pairs gain scan through for capturing special cases
            foreach (var field in result.KeyValuePairs)
            {
                var key = field.Key?.Content?.Trim().ToLower() ?? "";
                var value = field.Value?.Content?.Trim() ?? "";

                if (!string.IsNullOrEmpty(value))
                {
                    
                        if(key == "t" || key == "m" || key.Contains("cell no")||key.Contains("mobile") || key.Contains("telephone") || key.Contains("tel/mob") || key.Contains("phone") || key.Contains("telephone") ||(key.Contains("contact") && RegexHelper.HasNumbersRegex.IsMatch(value)))
                        {
                            extractedData.BuyerContactNumber.Add(value);
                        }
                       
                    
                    
                                         
                        if (key == "t" || key == "m" || key.Contains("cell no") ||key.Contains("mobile") || key.Contains("telephone")|| key.Contains("phone")||(key.Contains("contact")&& RegexHelper.HasNumbersRegex.IsMatch(value)))
                        {
                            extractedData.ShipToContactNumber.Add(value);
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
            if(extractedData.Irn.Count ==0)
            {
                extractedData.Irn.Add("NA");
            }

            extractedData.AcknowledgeNumber = extractedData.AcknowledgeNumber.Distinct().ToList();
            if (extractedData.AcknowledgeNumber.Count == 0)
            {
                extractedData.AcknowledgeNumber.Add("NA");
            }
            extractedData.AcknowledgeDate = extractedData.AcknowledgeDate.Distinct().ToList();
            if (extractedData.AcknowledgeDate.Count == 0)
            {
                extractedData.AcknowledgeDate.Add("NA");
            }

            extractedData.Buyer = extractedData.Buyer.Distinct().ToList();
            if (extractedData.Buyer.Count == 0)
            {
                extractedData.Buyer.Add("NA");
            }
            extractedData.BuyerAddressLine1 = extractedData.BuyerAddressLine1.Distinct().ToList();
            if (extractedData.BuyerAddressLine1.Count == 0)
            {
                extractedData.BuyerAddressLine1.Add("NA");
            }
            //extractedData.BuyerCity = extractedData.BuyerCity.Distinct().ToList();
            extractedData.BuyerState = extractedData.BuyerState.Distinct().ToList();
            if (extractedData.BuyerState.Count == 0)
            {
                extractedData.BuyerState.Add("NA");
            }
            extractedData.BuyerPinCode = extractedData.BuyerPinCode.Distinct().ToList();
            if (extractedData.BuyerPinCode.Count == 0)
            {
                extractedData.BuyerPinCode.Add("NA");
            }
            extractedData.BuyerEmail = extractedData.BuyerEmail.Distinct().ToList();
            if (extractedData.BuyerEmail.Count == 0)
            {
                extractedData.BuyerEmail.Add("NA");
            }
            extractedData.BuyerContactPerson = extractedData.BuyerContactPerson.Distinct().ToList();
            if (extractedData.BuyerContactPerson.Count == 0)
            {
                extractedData.BuyerContactPerson.Add("NA");
            }
            extractedData.BuyerContactNumber = extractedData.BuyerContactNumber.Distinct().ToList();
            if (extractedData.BuyerContactNumber.Count == 0)
            {
                extractedData.BuyerContactNumber.Add("NA");
            }
            extractedData.BuyerGstin = extractedData.BuyerGstin.Distinct().ToList();
            if (extractedData.BuyerGstin.Count == 0)
            {
                extractedData.BuyerGstin.Add("NA");
            }

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
            if (extractedData.ShipTo.Count == 0)
            {
                extractedData.ShipTo.Add("NA");
            }
            extractedData.ShipToAddressLine1 = extractedData.ShipToAddressLine1.Distinct().ToList();
            if (extractedData.ShipToAddressLine1.Count == 0)
            {
                extractedData.ShipToAddressLine1.Add("NA");
            }
            //extractedData.ShipToCity = extractedData.ShipToCity.Distinct().ToList();
            //extractedData.ShipToCity = extractedData.ShipToCity.Distinct().ToList();
            //extractedData.ShipToPinCode = extractedData.ShipToPinCode.Distinct().ToList();
            extractedData.ShipToEmail = extractedData.ShipToEmail.Distinct().ToList();
            if (extractedData.ShipToEmail.Count == 0)
            {
                extractedData.ShipToEmail.Add("NA");
            }
            extractedData.ShipToContactPerson = extractedData.ShipToContactPerson.Distinct().ToList();
            if (extractedData.ShipToContactPerson.Count == 0)
            {
                extractedData.ShipToContactPerson.Add("NA");
            }
            extractedData.ShipToContactNumber = extractedData.ShipToContactNumber.Distinct().ToList();
            if (extractedData.ShipToContactNumber.Count == 0)
            {
                extractedData.ShipToContactNumber.Add("NA");
            }


            //extractedData.DescriptionOfGoods = extractedData.DescriptionOfGoods.Distinct().ToList();
            //extractedData.HsnCode = extractedData.HsnCode.Distinct().ToList();
            extractedData.Quantity = extractedData.Quantity.Distinct().ToList();
            if (extractedData.Quantity.Count == 0)
            {
                extractedData.Quantity.Add("NA");
            }
            extractedData.Rate = extractedData.Rate.Distinct().ToList();
            if (extractedData.Rate.Count == 0)
            {
                extractedData.Rate.Add("NA");
            }
            extractedData.Sgst = extractedData.Sgst.Distinct().ToList();
            if (extractedData.Sgst.Count == 0)
            {
                extractedData.Sgst.Add("NA");
            }
            extractedData.Cgst = extractedData.Cgst.Distinct().ToList();
            if (extractedData.Cgst.Count == 0)
            {
                extractedData.Cgst.Add("NA");
            }
            extractedData.Igst = extractedData.Igst.Distinct().ToList();
            if (extractedData.Igst.Count == 0)
            {
                extractedData.Igst.Add("NA");
            }
            extractedData.TotalAmount = extractedData.TotalAmount.Distinct().ToList();
            if (extractedData.TotalAmount.Count == 0)
            {
                extractedData.TotalAmount.Add("NA");
            }
            //extractedData.AmountChargable = extractedData.AmountChargable.Distinct().ToList();

            //extractedData.Declaration = extractedData.Declaration.Distinct().ToList();
            extractedData.BankName = extractedData.BankName.Distinct().ToList();
            if (extractedData.BankName.Count == 0)
            {
                extractedData.BankName.Add("NA");
            }
            extractedData.IfscCode = extractedData.IfscCode.Distinct().ToList();
            if (extractedData.IfscCode.Count == 0)
            {
                extractedData.IfscCode.Add("NA");
            }
            extractedData.AcctNo = extractedData.AcctNo.Distinct().ToList();
            if (extractedData.AcctNo.Count == 0)
            {
                extractedData.AcctNo.Add("NA");
            }

            extractedData.EWayBill = extractedData.EWayBill.Distinct().ToList();
            if (extractedData.EWayBill.Count == 0)
            {
                extractedData.EWayBill.Add("NA");
            }
            extractedData.DeleiveryNote = extractedData.DeleiveryNote.Distinct().ToList();
            if (extractedData.DeleiveryNote.Count == 0)
            {
                extractedData.DeleiveryNote.Add("NA");
            }
            extractedData.TermsOfPayment = extractedData.TermsOfPayment.Distinct().ToList();
            if (extractedData.TermsOfPayment.Count == 0)
            {
                extractedData.TermsOfPayment.Add("NA");
            }
            extractedData.DespatchDocNo = extractedData.DespatchDocNo.Distinct().ToList();
            if (extractedData.DespatchDocNo.Count == 0)
            {
                extractedData.DespatchDocNo.Add("NA");
            }
            extractedData.DespatchThrough = extractedData.DespatchThrough.Distinct().ToList();
            if (extractedData.DespatchThrough.Count == 0)
            {
                extractedData.DespatchThrough.Add("NA");
            }
            extractedData.VehicleNo = extractedData.VehicleNo.Distinct().ToList();
            if (extractedData.VehicleNo.Count == 0)
            {
                extractedData.VehicleNo.Add("NA");
            }
            if(extractedData.ExtractedTables!= null && extractedData.ExtractedTables.Count!=0 && extractedData.ExtractedTables.FirstOrDefault().Rows.Count==1)
            {
                extractedData.DescriptionOfGoods.Add(extractedData.ExtractedTables.FirstOrDefault().Rows.FirstOrDefault()[1]);
                extractedData.HsnNo.Add(extractedData.ExtractedTables.FirstOrDefault().Rows.FirstOrDefault()[2]);
            }
            else
            {
                extractedData.DescriptionOfGoods.Add("Refer table");
                extractedData.HsnNo.Add("Refer table");
            }
        }
    }
}
