namespace JobMailerApi.Models
{
    public class JobApplicationRequest
    {
        public string CompanyName { get; set; } = string.Empty;
        public string HrName { get; set; } = string.Empty;
        public string HrEmail { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string JobLink { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ResumeUrl { get; set; } = string.Empty;
    }
}