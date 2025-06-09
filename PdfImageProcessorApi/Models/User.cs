using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PdfImageProcessor.Models
{
    [Table("user")] // Ensure this matches your actual table name
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [MaxLength(100)]
        [Column("UserName")]
        public string UserName { get; set; } = string.Empty;

        [Column("PasswordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("PasswordSalt")]
        public string PasswordSalt { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("FirstName")]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        [Column("LastName")]
        public string? LastName { get; set; }

        [MaxLength(10)]
        [Column("Gender")]
        public string? Gender { get; set; }

        [Column("OrgId")]
        public int? OrgId { get; set; }

        [Column("Active")]
        public bool Active { get; set; }
    }
}
