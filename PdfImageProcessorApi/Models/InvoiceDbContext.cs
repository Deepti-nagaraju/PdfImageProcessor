using Microsoft.EntityFrameworkCore;
using PdfImageProcessorApi.Models;
using PdfImageProcessor.Models;



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
    public DbSet<InvoiceItem> InvoiceItem { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Auth> Auth { get; set; }

}
