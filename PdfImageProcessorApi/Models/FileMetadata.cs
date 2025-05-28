using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PdfImageProcessorApi.Models
{

    [Table("file_metadata")]
    public class FileMetadata
    {
        [Key]
        public int Id { get; set; }

        public string? FileName { get; set; }

        [MaxLength(100)]
        public string? InvoiceNumber { get; set; }

        public string? InvoiceDate { get; set; }

        [MaxLength(100)]
        public string? IrnNumber { get; set; }

        [MaxLength(100)]
        public string? AcknowledgeNumber { get; set; }

        public string? AcknowledgeDate { get; set; }

        [MaxLength(255)]
        public string? BuyerName { get; set; }

        [MaxLength(2000)]
        public string? BuyerAddressLine1 { get; set; }

        [MaxLength(100)]
        public string? BuyerState { get; set; }

        [MaxLength(100)]
        public string? BuyerPinCode { get; set; }

        [MaxLength(100)]
        public string? BuyerGstin { get; set; }

        [MaxLength(100)]
        public string? BuyerEmail { get; set; }

        [MaxLength(100)]
        public string? BuyerContactPerson { get; set; }

        [MaxLength(100)]
        public string? BuyerContactNumber { get; set; }

        [MaxLength(255)]
        public string? ShiptoName { get; set; }

        [MaxLength(2000)]
        public string? ShiptoAddressLine1 { get; set; }

        [MaxLength(100)]
        public string? ShiptoEmail { get; set; }

        [MaxLength(100)]
        public string? ShiptoContactPerson { get; set; }

        [MaxLength(100)]
        public string? ShiptoContactNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Cgst { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Sgst
        {
            get; set;
        }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Igst { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalTax { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalInvoiceAmount { get; set; }

        [MaxLength(100)]
        public string? EwayBill { get; set; }

        [MaxLength(255)]
        public string? DeliveryNote { get; set; }

        [MaxLength(100)]
        public string? TermsOfPayment { get; set; }

        [MaxLength(100)]
        public string? DespatchDocNo { get; set; }

        [MaxLength(100)]
        public string? DespatchThrough { get; set; }

        [MaxLength(100)]
        public string? Destination { get; set; }

        [MaxLength(100)]
        public string? VehicleNo { get; set; }

        [MaxLength(255)]
        public string? DescriptionOfGoods { get; set; }

        [MaxLength(100)]
        public string? HsnNo { get; set; }

        public string? Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Rate { get; set; }

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(20)]
        public string? IfscCode { get; set; }

        [MaxLength(50)]
        public string? AccountNo { get; set; }
    }
}

