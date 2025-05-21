using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace PdfImageProcessorApi.Models
{
    [Table("filestore_action")]
    public class FilestoreAction
    {
        [Key]
        public int Id { get; set; }

        public int? FilestoreId { get; set; }

        [MaxLength(100)]
        public string Status { get; set; }

        [MaxLength(100)]
        public string ActionType { get; set; }

        public DateTime? ActionDate { get; set; }

        public int? ActionPerformedBy { get; set; }
    }
}

