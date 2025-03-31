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
using System.Reflection.PortableExecutable;
using Microsoft.IdentityModel.Tokens;
using System.Reflection.Metadata;

namespace PdfImageProcessor.Services
{
    public class PdfProcessingService
    {
        private const string Endpoint = "https://deepti.cognitiveservices.azure.com/";
        private const string ApiKey = "3lUsGeSbyFujvN5DM45mYggERcTBcob26fhxqwSSXixhWi1PMwkhJQQJ99BBACGhslBXJ3w3AAALACOGVVET";
        private static readonly string modelId = "Prebuilt_Invoice_4";

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
                var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, modelId, stream);
                var result = operation.Value;

                decimal totalAmount = 0;
                decimal totalTax = 0;
                decimal totalQuantity = 0;
                decimal totalRate = 0;

                extractedData.ExtractedTables = StructureExtractedTables(result, out totalAmount, out totalRate, out totalQuantity, out decimal cgst, out decimal sgst, out decimal igst);
                //if (extractedData.ExtractedTables.Count == 0)
                //{
                // extractedData.ExtractedTables = StructureExtractedTables(result);
                //}
                //ExtractKeyValuePairs(result, extractedData, totalAmount, totalRate, totalQuantity, cgst, sgst, igst);
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
        //  public static List<TableModel> StructureExtractedTables(
        //AnalyzeResult result,
        //out decimal totalAmount,
        //out decimal totalRate,
        //out decimal totalQuantity,
        //out decimal sgst,
        //out decimal cgst,
        //out decimal igst)
        //  {
        //      var structuredTables = new List<TableModel>();
        //      totalAmount = 0;
        //      totalQuantity = 0;
        //      totalRate = 0;
        //      sgst = 0;
        //      cgst = 0;
        //      igst = 0;

        //      // ✅ Extract key-value pairs for additional tax values
        //      var extractedKeyValues = result.KeyValuePairs
        //          .Where(kvp => kvp.Key != null && kvp.Value != null)
        //          .ToLookup(
        //              kvp => NormalizeKey(kvp.Key.Content),
        //              kvp => kvp.Value.Content.Trim(),
        //              StringComparer.OrdinalIgnoreCase
        //          );

        //      // ✅ Extract Taxable Values & Tax % from the secondary table
        //      var taxableValues = ExtractTaxableAmounts(result);

        //      foreach (var table in result.Tables)
        //      {
        //          var headers = table.Cells
        //              .Where(c => c.RowIndex == 0)
        //              .Select(c => c.Content)
        //              .ToList();

        //          var rows = table.Cells.Where(c => c.RowIndex > 0)
        //                     .GroupBy(c => c.RowIndex)
        //                     .Select(row => row.OrderBy(c => c.ColumnIndex).Select(c => c.Content).ToList());

        //          if (headers.Any(header => RegexHelper.IsValidTableHeader(header)))
        //          {
        //              var structuredTable = new TableModel
        //              {
        //                  Headers = new List<string> { "Sl No", "Description", "HSN Code", "Quantity", "Rate Per Quantity", "Taxable Value", "SGST%", "CGST%", "IGST%", "SGST", "CGST", "IGST", "Amount" },
        //                  Rows = new List<List<string>>()
        //              };

        //              int serialNumber = 1;

        //              foreach (var row in rows)
        //              {
        //                  var description = GetColumnValue(row, headers, new List<string> { "Description", "Item", "Product", "Particulars", "Model", "Vessel" }, new List<string> { "" });

        //                  if (!string.IsNullOrEmpty(description) && description.Trim().ToLower().Contains("total"))
        //                  {
        //                      continue; // ✅ Skip this row
        //                  }

        //                  var hsnCode = GetColumnValue(row, headers, new List<string> { "HSN", "SAC", }, new List<string> { "" });

        //                  var quantity = CleanNumericValue(GetColumnValue(row, headers, new List<string> { "Quantity", "Qty" }, new List<string> { "" }));
        //                  var ratePer = CleanNumericValue(GetColumnValue(row, headers, new List<string> { "Rate", "Rate Per", "Price", "MRP" }, new List<string> { "" }));
        //                  var amount = CleanNumericValue(GetColumnValue(row, headers, new List<string> { "Amount", "Amt" }, new List<string> { "" }));

        //                  // ✅ Fetch from primary table first, else use secondary table
        //                  var taxableAmount = CleanNumericValue(GetColumnValue(row, headers, new List<string> { "Taxable Value", "Taxable Amount" }, new List<string> { "" }));
        //                  var cgstPercent = CleanNumericValue(GetColumnValue(row, headers, new List<string> { "CGST", "Central GST" }, new List<string> { "%", "rate" }));
        //                  var sgstPercent = CleanNumericValue(GetColumnValue(row, headers, new List<string> { "SGST", "State GST" }, new List<string> { "%", "rate" }));
        //                  var igstPercent = CleanNumericValue(GetColumnValue(row, headers, new List<string> { "IGST" }, new List<string> { "%", "rate" }));



        //                  var cgstFetch = CleanNumericValue(GetColumnValueForTaxAmount(row, headers, new List<string> { "CGST", "central" }, new List<string> { "%", "rate" }));
        //                  var sgstFetch = CleanNumericValue(GetColumnValueForTaxAmount(row, headers, new List<string> { "SGST", "state" }, new List<string> { "%", "rate" }));
        //                  var igstFetch = CleanNumericValue(GetColumnValueForTaxAmount(row, headers, new List<string> { "IGST" }, new List<string> { "%", "rate" }));
        //                  // ✅ If values are missing, fetch from secondary tax table
        //                  if (taxableAmount == "0" && cgstPercent == "0" && sgstPercent == "0" && igstPercent == "0" && taxableValues.ContainsKey(hsnCode))
        //                  {
        //                      (taxableAmount, cgstPercent, cgstFetch, sgstPercent, sgstFetch, igstPercent, igstFetch, amount) = taxableValues[hsnCode];
        //                  }
        //                  // ✅ Convert cleaned values to decimal for summing
        //                  decimal amountValue = decimal.TryParse(amount, out decimal amt) ? amt : 0;
        //                  totalAmount += amountValue;

        //                  decimal rateValue = decimal.TryParse(ratePer, out decimal rate) ? rate : 0;
        //                  totalRate += rateValue;

        //                  decimal qtyValue = decimal.TryParse(quantity, out decimal qty) ? qty : 0;
        //                  totalQuantity += qtyValue;

        //                  decimal cgstValue = decimal.TryParse(cgstFetch, out decimal cgstOut) ? cgstOut : 0;
        //                  cgst += cgstValue;
        //                  decimal sgstValue = decimal.TryParse(sgstFetch, out decimal sgstOut) ? sgstOut : 0;
        //                  sgst += sgstValue;
        //                  decimal igstValue = decimal.TryParse(igstFetch, out decimal igstOut) ? igstOut : 0;
        //                  igst += igstValue;

        //                  var mappedRow = new List<string>
        //          {
        //              serialNumber.ToString(),
        //              description,
        //              hsnCode,
        //              quantity,
        //              ratePer,
        //              taxableAmount, // ✅ Fetched first from primary table, then secondary
        //              cgstPercent,   // ✅ Fetched first from primary table, then secondary
        //              sgstPercent,   // ✅ Fetched first from primary table, then secondary
        //              igstPercent,   // ✅ Fetched first from primary table, then secondary
        //              cgstFetch,
        //              sgstFetch,
        //              igstFetch,
        //              amount
        //          };

        //                  structuredTable.Rows.Add(mappedRow);
        //                  serialNumber++;
        //              }

        //              structuredTables.Add(structuredTable);
        //              break;
        //          }
        //      }

        //      return structuredTables;
        //  }


        public static List<TableModel> StructureExtractedTables(AnalyzeResult result, out decimal totalAmount,
      out decimal totalRate,
      out decimal totalQuantity,
      out decimal sgst,
      out decimal cgst,
      out decimal igst)
        {
            var structuredTables = new List<TableModel>();
            totalAmount = 0;
            totalQuantity = 0;
            totalRate = 0;
            sgst = 0;
            cgst = 0;
            igst = 0;


            // Check if 'Items' field exists and is a list
            if (result.Documents.FirstOrDefault()?.Fields.TryGetValue("Items", out var itemsField) == true &&
                itemsField.FieldType == DocumentFieldType.List)
            {
                var items = itemsField.Value.AsList();
                var table = new TableModel
                {
                    Headers = new List<string>
            {
                "Sl No", "Description", "HSN Code", "Quantity", "Rate Per Quantity",
                "Taxable Value", "SGST%", "CGST%", "IGST%", "SGST", "CGST", "IGST", "Amount"
            },
                    Rows = new List<List<string>>()
                };

                int serialNumber = 1;

                foreach (var item in items)
                {
                    if (item.FieldType != DocumentFieldType.Dictionary) continue;

                    var fields = item.Value.AsDictionary();

                    string description = fields.TryGetValue("Description", out var desc) ? desc?.Content?.Trim() ?? "" : "";
                    string hsnCode = fields.TryGetValue("Product Code", out var productCode) ? productCode?.Content?.Trim() ?? "" : "";
                    string quant = fields.TryGetValue("Quantity", out var qty) ? qty?.Content?.Trim() ?? "" : "";
                    string unitPrice = fields.TryGetValue("Unit Price", out var price) ? price?.Content?.Trim() ?? "" : "";
                    string amnt = fields.TryGetValue("Total Amount", out var amt) ? amt?.Content?.Trim() ?? "" : "";
                    string taxValue = fields.TryGetValue("Amount", out var tax) ? tax?.Content?.Trim() ?? "" : "";
                    string sgstPercent = fields.TryGetValue("SGST%", out var sgstPer) ? sgstPer?.Content?.Trim() ?? "" : "";
                    string cgstPercent = fields.TryGetValue("CGST%", out var cgstPer) ? cgstPer?.Content?.Trim() ?? "" : "";
                    string igstPercent = fields.TryGetValue("IGST%", out var igstPer) ? igstPer?.Content?.Trim() ?? "" : "";
                    string sgstValue = fields.TryGetValue("SGST", out var sgstVal) ? sgstVal?.Content?.Trim() ?? "" : "";
                    string cgstValue = fields.TryGetValue("CGST", out var cgstVal) ? cgstVal?.Content?.Trim() ?? "" : "";
                    string igstValue = fields.TryGetValue("IGST", out var igstVal) ? igstVal?.Content?.Trim() ?? "" : "";

                    var row = new List<string>
                    {
                        serialNumber.ToString(),
                        description,
                        hsnCode,
                        quant,
                        unitPrice,
                        taxValue,
                        sgstPercent,
                        cgstPercent,
                        igstPercent,
                        sgstValue,
                        cgstValue,
                        igstValue,
                        amnt
                    };

                    table.Rows.Add(row);
                    serialNumber++;
                    var quantity = CleanNumericValue(quant);
                    //var amount = CleanNumericValue(amnt);
                    var sgstamt = CleanNumericValue(sgstValue);
                    var cgstamt = CleanNumericValue(cgstValue);
                    var igstamt = CleanNumericValue(igstValue);
                    //totalAmount += Convert.ToDecimal(amount);
                    try
                    {
                        cgst += Convert.ToDecimal(cgstamt);
                        sgst += Convert.ToDecimal(sgstamt);
                        igst += Convert.ToDecimal(igstamt);
                    }
                    catch
                    {
                        continue;
                    }




                }


                structuredTables.Add(table);
            }

            return structuredTables;
        }




        private static string GetTaxFromKeyValuePairs(ILookup<string, string> keyValuePairs, List<string> possibleKeys)
        {
            // Normalize input search keys
            var normalizedKeys = possibleKeys.Select(k => NormalizeKey(k)).ToList();

            // Find the best-matching key in KeyValuePairs
            var matchedKey = keyValuePairs
                .Select(pair => pair.Key) // Extract all stored keys
                .FirstOrDefault(storedKey =>
                    normalizedKeys.Any(searchKey => storedKey.Contains(searchKey, StringComparison.OrdinalIgnoreCase))
                );

            if (matchedKey != null)
            {
                return string.Join(", ", keyValuePairs[matchedKey]); // Return the corresponding value(s)
            }

            return ""; // Return empty if no match found
        }

        private static string NormalizeKey(string key)
        {
            return key.Trim().Replace("\n", " ").ToLowerInvariant(); // Remove newlines and trim spaces
        }


        private static string GetColumnValue(List<string> row, List<string> headers, List<string> possibleHeadersPrimary, List<string> possibleHeadersSecondary)
        {
            int index = headers.FindIndex(h => possibleHeadersPrimary.Any(ph => h.Contains(ph, StringComparison.OrdinalIgnoreCase)) && possibleHeadersSecondary.Any(ph => h.Contains(ph, StringComparison.OrdinalIgnoreCase)));
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

        private static Dictionary<string, (string TaxableAmount, string CGSTRate, string CGSTAmount, string SGSTRate, string SGSTAmount, string IGSTRate, string IGSTAmount, string TotalAmount)> ExtractTaxableAmounts(AnalyzeResult result)
        {
            var taxableAmounts = new Dictionary<string, (string, string, string, string, string, string, string, string)>();

            foreach (var table in result.Tables)
            {
                var headers = table.Cells
                    .Where(c => c.RowIndex == 0)
                    .Select(c => c.Content.ToLower().Trim()) // Normalize headers
                    .ToList();

                // ✅ Find column indexes for relevant fields
                int hsnIndex = headers.FindIndex(h => h.Contains("hsn") || h.Contains("sac"));
                int taxableIndex = headers.FindIndex(h => h.Contains("tax"));
                int cgstRateIndex = headers.FindIndex(h => h.Contains("cgst"));
                int cgstAmountIndex = cgstRateIndex != -1 ? cgstRateIndex + 1 : -1;
                int sgstRateIndex = cgstRateIndex != -1 ? cgstAmountIndex + 1 : -1;
                int sgstAmountIndex = cgstRateIndex != -1 ? sgstRateIndex + 1 : -1;
                int igstRateIndex = headers.FindIndex(h => h.Contains("igst") || h.Contains("integrated tax"));
                int igstAmountIndex = igstRateIndex != -1 ? igstRateIndex + 1 : -1;
                int totalAmountIndex = -1;
                if (igstAmountIndex >= 1)
                {
                    totalAmountIndex = igstAmountIndex + 1;
                }
                else if (sgstAmountIndex >= 1)
                {
                    totalAmountIndex = igstAmountIndex + 1;
                }

                if (hsnIndex == -1 || taxableIndex == -1)
                {
                    continue; // ✅ Skip if required columns are missing
                }

                var rows = table.Cells.Where(c => c.RowIndex > 0)
                            .GroupBy(c => c.RowIndex)
                            .Select(row => row.OrderBy(c => c.ColumnIndex).Select(c => c.Content).ToList());

                foreach (var row in rows)
                {
                    if (row.Count <= hsnIndex || row.Count <= taxableIndex || row.Count <= cgstRateIndex || row.Count <= sgstRateIndex)
                    {
                        continue; // ✅ Skip incomplete rows
                    }

                    string hsnCode = row[hsnIndex].Trim();
                    string taxableAmount = row[taxableIndex].Trim();
                    string cgstRate = cgstRateIndex != -1 && row.Count > cgstRateIndex ? row[cgstRateIndex].Trim() : "N/A";
                    string cgstAmount = cgstAmountIndex != -1 && row.Count > cgstAmountIndex ? row[cgstAmountIndex].Trim() : "N/A";
                    string sgstRate = sgstRateIndex != -1 && row.Count > sgstRateIndex ? row[sgstRateIndex].Trim() : "N/A";
                    string sgstAmount = sgstAmountIndex != -1 && row.Count > sgstAmountIndex ? row[sgstAmountIndex].Trim() : "N/A";
                    string igstRate = igstRateIndex != -1 && row.Count > igstRateIndex ? row[igstRateIndex].Trim() : "N/A";
                    string igstAmount = igstAmountIndex != -1 && row.Count > igstAmountIndex ? row[igstAmountIndex].Trim() : "N/A";
                    string totalAmount = totalAmountIndex != -1 && row.Count > totalAmountIndex ? row[totalAmountIndex].Trim() : "N/A";

                    // ✅ Store the extracted values only if HSN code is present
                    if (!string.IsNullOrEmpty(hsnCode) && hsnCode.ToLower() != "total")
                    {
                        taxableAmounts[hsnCode] = (taxableAmount, cgstRate, cgstAmount, sgstRate, sgstAmount, igstRate, igstAmount, totalAmount);
                    }
                }
            }
            return taxableAmounts;
        }







        private void ExtractKeyValuePairs(AnalyzeResult result, MainTableModel extractedData, decimal totalAmount = 0, decimal totalRate = 0, decimal totalQuantity = 0, decimal cgst = 0, decimal sgst = 0, decimal igst = 0)
        {
            extractedData.Sgst.Add(sgst.ToString());
            extractedData.Cgst.Add(cgst.ToString());
            extractedData.Igst.Add(igst.ToString());
            foreach (var field in result.Documents[0].Fields)
            {
                var key = field.Key?.Trim().ToLower() ?? "";
                var value = field.Value?.Content?.Trim() ?? "";
                var valueToCheck = field.Value?.Content?.Trim().ToLower() ?? "";

                if (!string.IsNullOrEmpty(value))
                {
                    if (key.Contains("irn")) extractedData.Irn.Add(value);
                    if (key == "acknowledgementnumber") extractedData.AcknowledgeNumber.Add(value);
                    if (key == "acknowledgementdate") extractedData.AcknowledgeDate.Add(value);
                    if (key == "billingaddressrecipient") extractedData.Buyer.Add(value);
                    if (extractedData.Buyer.Count == 0) { if (key == "customername") extractedData.Buyer.Add(value); }
                    if (key == "billingaddress") extractedData.BuyerAddressLine1.Add(value);
                    if (key == "customertaxid") extractedData.BuyerGstin.Add(value.Trim());
                    if (extractedData.BuyerGstin.Count == 0) { if (key == "vendortaxid") extractedData.BuyerGstin.Add(value); }
                    if (key == "invoiceid") extractedData.InvoiceNumber.Add(value);
                    if (key == "invoicedate") extractedData.InvoiceDate.Add(value);
                    if (key == "shippingaddressrecipient") extractedData.ShipTo.Add(value);
                    if (key == "shippingaddress") extractedData.ShipToAddressLine1.Add(value);
                    if (key == "deliverynote") extractedData.DeleiveryNote.Add(value);
                    if (key == "paymentterm") extractedData.TermsOfPayment.Add(value);
                    if ((key.Contains("despatch") || key.Contains("dispatch")) && ((key.Contains("number") || key.Contains("no")))) extractedData.DespatchDocNo.Add(value);
                    if (key == "dispatchmode") extractedData.DespatchThrough.Add(value);
                    if (key == "vehiclenumber") extractedData.VehicleNo.Add(value);
                    if (key.Contains("destination") || key.Contains("final destination")) extractedData.Destination.Add(value);
                    if (key == "buyerstate") extractedData.BuyerState.Add(value);
                    if (key == "billtocontactperson") extractedData.BuyerContactPerson.Add(value);
                    if (key == "buyercontactnumber") extractedData.BuyerContactNumber.Add(value);
                    if (key == "shiptocontactperson") extractedData.ShipToContactPerson.Add(value);
                    if (key == "shippeingcontactnumber") extractedData.ShipToContactNumber.Add(value);
                    if (key == "cgstamount") extractedData.Cgst.Add(value);
                    if (key == "sgstamount") extractedData.Sgst.Add(value);
                    if (key == "igstamount") extractedData.Igst.Add(value);
                    if (key == "invoicetotal") extractedData.TotalAmount.Add(value);
                    if (key == "ewaybillnumber") extractedData.EWayBill.Add(value);
                    if (key == "ifsccode") extractedData.IfscCode.Add(value);
                    if (key == "bankname") extractedData.BankName.Add(value);
                    if (key == "accountnumber") extractedData.AcctNo.Add(value);
                    if (key == "buyerpin") extractedData.BuyerPinCode.Add(value);

                }
            }
            //After scanning through all key value pairs gain scan through for capturing special cases
            foreach (var field in result.KeyValuePairs)
            {
                var key = field.Key?.Content?.Trim().ToLower() ?? "";
                var value = field.Value?.Content?.Trim() ?? "";

                if (!string.IsNullOrEmpty(value))
                {

                    if (key == "t" || key == "m" || key.Contains("cell no") || key.Contains("mobile") || key.Contains("telephone") || key.Contains("tel/mob") || key.Contains("phone") || key.Contains("telephone") || (key.Contains("contact") && RegexHelper.HasNumbersRegex.IsMatch(value)))
                    {
                        extractedData.BuyerContactNumber.Add(value);
                    }




                    if (key == "t" || key == "m" || key.Contains("cell no") || key.Contains("mobile") || key.Contains("telephone") || key.Contains("phone") || (key.Contains("contact") && RegexHelper.HasNumbersRegex.IsMatch(value)))
                    {
                        extractedData.ShipToContactNumber.Add(value);
                    }

                    if (extractedData.Igst.Count == 0 && (key.Contains("integrated tax") || key.Contains("igst")))
                    {
                        extractedData.Igst.Add(value);
                    }


                }
            }
            var emails = RegexHelper.EmailRegex.Matches(result.Content).Select(match => match.Value).ToList();
            var companyNames = RegexHelper.CompanyNameRegex.Matches(result.Content).Select(match => match.Value).ToList();

            if (extractedData.BuyerEmail.Count == 0)
            {
                extractedData.BuyerEmail = emails;
            }
            if (extractedData.ShipToEmail.Count == 0)
            {
                extractedData.ShipToEmail = emails;
            }
            if (extractedData.Buyer.Count == 0)
            {
                extractedData.Buyer = companyNames;
            }

            // Ensure unique values for each field
            extractedData.Irn = extractedData.Irn.Distinct().ToList();
            if (extractedData.Irn.Count == 0)
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
            //extractedData.Rate = extractedData.Rate.Distinct().ToList();
            //if (extractedData.Rate.Count == 0)
            //{
            //    extractedData.Rate.Add("NA");
            //}
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
            //extractedData.TotalAmount = extractedData.TotalAmount.Distinct().ToList();
            //if (extractedData.TotalAmount.Count == 0)
            //{
            //    extractedData.TotalAmount.Add("NA");
            //}
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
            if (extractedData.ExtractedTables != null && extractedData.ExtractedTables.Count != 0 && extractedData.ExtractedTables.FirstOrDefault().Rows.Count == 1)
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
