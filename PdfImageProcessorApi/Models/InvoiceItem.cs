using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("invoice_items")]
public class InvoiceItem
{
    [Key]
    public int Id { get; set; }

    [MaxLength(255)]
    public string? SourceFileName { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? HSNCode { get; set; }

    [MaxLength(50)]
    public string? Quantity { get; set; }

    [MaxLength(50)]
    public string? UnitPrice { get; set; }

    [MaxLength(50)]
    public string? Amount { get; set; }

    [MaxLength(50)]
    public string? TaxAmt { get; set; }

    [MaxLength(50)]
    public string? SgstPercent { get; set; }

    [MaxLength(50)]
    public string? CgstPercent { get; set; }

    [MaxLength(50)]
    public string? IgstPercent { get; set; }

    [MaxLength(50)]
    public string? SgstAmt { get; set; }

    [MaxLength(50)]
    public string? CgstAmt { get; set; }

    [MaxLength(50)]
    public string? IgstAmt { get; set; }

    [MaxLength(50)]
    public string? InsuranceAmt { get; set; }
}
