namespace JobMailerApi.Models
{
    public class EmailAttemptRecord
    {
        public string HrName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string FailureCategory { get; set; } = string.Empty;
        public string SmtpErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class EmailFailureDetails
    {
        public string Category { get; set; } = "Unknown Error";
        public string SmtpErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
