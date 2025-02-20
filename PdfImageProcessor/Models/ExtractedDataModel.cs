using System.Collections.Generic;

namespace PdfImageProcessor.Models
{
    public class MainTableModel
    {
        public string FileName { get; set; }
        public List<string> Irn { get; set; } = new();
        public List<string> AcknowledgeNumber { get; set; } = new();
        public List<string> AcknowledgeDate { get; set; } = new();



        public List<string> Buyer { get; set; } = new();
        public List<string> BuyerAddressLine1 { get; set; } = new();
        public List<string> BuyerState { get; set; } = new();
        public List<string> BuyerPinCode { get; set; } = new();
        public List<string> BuyerEmail { get; set; } = new();
        public List<string> BuyerContactPerson { get; set; } = new();
        public List<string> BuyerContactNumber { get; set; } = new();
        public List<string> BuyerGstin { get; set; } = new();

        public List<string> ShipTo { get; set; } = new();
        public List<string> ShipToAddressLine1 { get; set; } = new();
        //public List<string> ShipToState { get; set; } = new();
        //public List<string> ShipToPinCode { get; set; } = new();
        public List<string> ShipToEmail { get; set; } = new();
        public List<string> ShipToContactPerson { get; set; } = new();
        public List<string> ShipToContactNumber { get; set; } = new();

        //public List<string> Seller { get; set; } = new();
        //public List<string> SellerAddressLine1 { get; set; } = new();
        //public List<string> SellerCity { get; set; } = new();
        //public List<string> SellerState { get; set; } = new();
        //public List<string> SellerPinCode { get; set; } = new();
        //public List<string> SellerEmail { get; set; } = new();
        //public List<string> SellerContactPerson { get; set; } = new();
        //public List<string> SellerContactNumber { get; set; } = new();
        //public List<string> SellerGstin { get; set; } = new();

        public List<string> InvoiceDate { get; set; } = new();
        public List<string> InvoiceNumber { get; set; } = new();
        public List<string> EWayBill { get; set; } = new();
        public List<string> DeleiveryNote { get; set; } = new();
        public List<string> TermsOfPayment { get; set; } = new();
        public List<string> DespatchDocNo { get; set; } = new();
        public List<string> DespatchThrough { get; set; } = new();
        public List<string> Destination { get; set; } = new();
        public List<string> VehicleNo { get; set; } = new();
        public List<string> Quantity { get; set; } = new();
        public List<string> Rate { get; set; } = new();
        public List<string> Sgst { get; set; } = new();
        public List<string> Cgst { get; set; } = new();
        public List<string> Igst { get; set; } = new();
        public List<string> TotalAmount { get; set; } = new();
        public List<string> BankName { get; set; } = new();
        public List<string> IfscCode { get; set; } = new();
        public List<string> AcctNo { get; set; } = new();

        public List<TableModel> ExtractedTables { get; set; } = new();
        public List<Dictionary<string, string>> SubTable { get; set; } = new();


        public void MapKeyToProperty(string key, string value)
        {
            if (key.Contains("irn")) Irn.Add(value);
            if (key.Contains("buyer")) Buyer.Add(value);
            if (key.Contains("invoice number")) InvoiceNumber.Add(value);
            if (key.Contains("invoice date")) InvoiceDate.Add(value);
        }
    }
    public class ExcelModel
    {
        public string? FileName { get; set; }
        public string? Irn { get; set; } 
        public string? AcknowledgeNumber { get; set; } 
        public string? AcknowledgeDate { get; set; } 



        public string? Buyer { get; set; } 
        public string? BuyerAddressLine1 { get; set; } 
        public string?  BuyerState { get; set; } 
        public string? BuyerPinCode { get; set; } 
        public string? BuyerEmail { get; set; } 
        public string? BuyerContactPerson { get; set; } 
        public string? BuyerContactNumber { get; set; } 
        public string?  BuyerGstin { get; set; } 

        public string? ShipTo { get; set; } 
        public string? ShipToAddressLine1 { get; set; } 
        public string? ShipToEmail { get; set; } 
        public string? ShipToContactPerson { get; set; } 
        public string? ShipToContactNumber { get; set; } 

        public string? InvoiceDate { get; set; } 
        public string? InvoiceNumber { get; set; } 
        public string? EWayBill { get; set; } 
        public string? DeleiveryNote { get; set; } 
        public string? TermsOfPayment { get; set; } 
        public string? DespatchDocNo { get; set; } 
        public string? DespatchThrough { get; set; } 
        public string? Destination { get; set; } 
        public string? VehicleNo { get; set; } 
        public string? Quantity { get; set; } 
        public string? Rate { get; set; } 
        public string? Sgst { get; set; } 
        public string? Cgst { get; set; } 
        public string? Igst { get; set; } 
        public string? TotalAmount { get; set; } 
        public string? BankName { get; set; } 
        public string? IfscCode { get; set; } 
        public string? AcctNo { get; set; } 
        public List<Dictionary<string, string>> SubTable { get; set; } = new();
    }

    public class TableModel
    {
        public List<string> Headers { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
    }
}
