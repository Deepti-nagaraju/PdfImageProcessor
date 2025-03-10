namespace PdfImageProcessor.Models
{
    public class Auth
    {
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
    }
}