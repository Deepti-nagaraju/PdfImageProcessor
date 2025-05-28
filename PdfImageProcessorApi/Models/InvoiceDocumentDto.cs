namespace PdfImageProcessorApi.Models
{
    public class InvoiceDocumentDto
    {
        public Filestore Filestore { get; set; }
        public FileMetadata Metadata { get; set; }
        public List<InvoiceItem> InvoiceItems { get; set; } = new();
    }
}
