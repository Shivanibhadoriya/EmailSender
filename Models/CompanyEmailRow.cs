namespace JobMailerApi.Models
{
    public class CompanyEmailRow
    {
        public int SNo { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? SentAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
