using Microsoft.EntityFrameworkCore;
using PdfImageProcessorApi.Models;

public class InvoiceDbContext : DbContext
{
    public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options)
        : base(options)
    {
    }

    // Add your DbSets
    public DbSet<Filestore> Filestore { get; set; }
    public DbSet<FileMetadata> FileMetadata { get; set; }
    public DbSet<FilestoreAction> FilestoreAction { get; set; }
}
