using System.ComponentModel;
using System.Text.RegularExpressions;

namespace PdfImageProcessor.Utilities
{
    public static class RegexHelper
    {
        // ✅ Mobile Number (India)
        public static readonly Regex MobileNumberRegex = new(@"\b(\+91[\s-]?)?[6-9]\d{9}\b", RegexOptions.IgnoreCase);

        // ✅ Email Address
        public static readonly Regex EmailRegex = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.IgnoreCase);

        // ✅ Company Name (Supports Pvt Ltd, LLC, Inc, etc.)
        public static readonly Regex CompanyNameRegex = new(@"\b[A-Za-z&.\s]+(?:Pvt Ltd|Private Limited|Ltd|LLC|Inc|Corporation|Company|GmbH|S.A.|AG|LLP|PLC|SNC|SAS|S.R.L|S.L)\b", RegexOptions.IgnoreCase);

        // ✅ GSTIN (India - 15-character format)
        public static readonly Regex GstinRegex = new(@"\b[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]\b", RegexOptions.IgnoreCase);

        // ✅ Indian Pincode (6 digits)
        public static readonly Regex PinCodeRegex = new(@"\b\d{6}\b", RegexOptions.IgnoreCase);

        // ✅ Invoice Number
        public static readonly Regex InvoiceNumberRegex = new(@"\b(?:[A-Z]{2,}-)?\d{2,4}(?:\/\d{2,4})?(?:-\d{2,4})?(?:-\d{4})?\b", RegexOptions.IgnoreCase);

        // ✅ E-Way Bill Number (12-digit numeric)
        public static readonly Regex EWayBillRegex = new(@"\b\d{12}\b", RegexOptions.IgnoreCase);

        // ✅ HSN Code (4 to 8-digit numbers)
        public static readonly Regex HsnCodeRegex = new(@"\b\d{4,8}\b", RegexOptions.IgnoreCase);

        // ✅ IFSC Code (Indian bank identification code)
        public static readonly Regex IfscRegex = new(@"\b[A-Z]{4}0[A-Z0-9]{6}\b", RegexOptions.IgnoreCase);

        // ✅ Bank Account Number (9-18 digits)
        public static readonly Regex AccountNumberRegex = new(@"\b\d{9,18}\b", RegexOptions.IgnoreCase);

        // ✅ Total Amount (Currency)
        public static readonly Regex TotalAmountRegex = new(@"\b(?:Rs\.?|₹|\$)\s?\d{1,3}(?:,\d{3})*(?:\.\d{1,2})?\b", RegexOptions.IgnoreCase);

        // ✅ Vehicle Number (India format: e.g., MH12AB1234)
        public static readonly Regex VehicleNoRegex = new(@"\b[A-Z]{2}\d{2}[A-Z]{0,3}\d{1,4}\b", RegexOptions.IgnoreCase);

        // ✅ Address Line (Starts with numbers followed by street keywords)
        public static readonly Regex AddressLineRegex = new(@"\b\d+\s+[\w\s]+(Street|St|Road|Rd|Avenue|Ave|Lane|Ln|Boulevard|Blvd|Drive|Dr|Way|Circle|Cir)\b", RegexOptions.IgnoreCase);

        // ✅ City Name (Assumes it starts with a capital letter)
        public static readonly Regex CityRegex = new(@"\b[A-Z][a-z]+(?:\s[A-Z][a-z]+)*\b", RegexOptions.IgnoreCase);

        // ✅ Valid Table Headers Check
        public static bool IsValidTableHeader(string header)
        {
            return header.Contains("Description", System.StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("Rate", System.StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("HSN", System.StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("Commodity Name", System.StringComparison.OrdinalIgnoreCase) ||
                   header.Contains("Item", System.StringComparison.OrdinalIgnoreCase)||
                   header.Contains("Product", System.StringComparison.OrdinalIgnoreCase)||
                   header.Contains("Vessel", System.StringComparison.OrdinalIgnoreCase)||
                   header.Contains("SAC", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
