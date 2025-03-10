namespace PdfImageProcessor.Models
{
    public class User
    {
        public required string UserName { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Gender { get; set; }
        public bool Active { get; set; }
    }
}