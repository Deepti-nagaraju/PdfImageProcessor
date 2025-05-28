using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace PdfImageProcessorApi.Models
{
    [Table("filestore")]
    public class Filestore
    {
        [Key]
        public int Id { get; set; }

        public int? OrgId { get; set; }

        [MaxLength(100)]
        public string? FinancialYear { get; set; }

        [MaxLength(1000)]
        public string? InvoiceFor { get; set; }

        public string? Comments { get; set; } // varchar(MAX)

        [MaxLength(255)]
        public string? SourceFileName { get; set; }

        [MaxLength(2000)]
        public string? SourceFileLoc { get; set; }

        [MaxLength(255)]
        public string? TargetFileName { get; set; }

        [MaxLength(1000)]
        public string? TargetFileLoc { get; set; }

        [MaxLength(100)]
        public string? Status { get; set; }

        [MaxLength(100)]
        public string? TargetErpSystem { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? CreationDate { get; set; }

        public string? LastUpdatedBy { get; set; }

        public DateTime? LastUpdatedDate { get; set; }
    }
}
