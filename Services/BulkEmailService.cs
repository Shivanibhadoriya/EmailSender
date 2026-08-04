using JobMailerApi.Models;

namespace JobMailerApi.Services
{
    public class BulkEmailService
    {
        private readonly ExcelCompanyService _excelService;
        private readonly JobMailerService _mailer;
        private readonly ILogger<BulkEmailService> _logger;
        private readonly IConfiguration _config;
        private readonly EmailAttemptLogService _attemptLogService;

        public BulkEmailService(
            ExcelCompanyService excelService,
            JobMailerService mailer,
            ILogger<BulkEmailService> logger,
            IConfiguration config,
            EmailAttemptLogService attemptLogService)
        {
            _excelService = excelService;
            _mailer = mailer;
            _logger = logger;
            _config = config;
            _attemptLogService = attemptLogService;
        }

        public async Task<BulkEmailResult> ProcessPendingEmailsAsync(
            int takeCount,
            CancellationToken cancellationToken)
        {
            var rows = _excelService.GetPendingRows(takeCount);
            var result = new BulkEmailResult { Found = rows.Count };

            foreach (var row in rows)
            {
                try
                {
                    _excelService.MarkRowStatus(row.Email, "Processing");

                    var subject = "Application for Software Developer Role";
                    _mailer.SendCustomEmail(row.Email, subject, BuildBody(row));

                    _excelService.MarkRowStatus(row.Email, "Sent");
                    result.Sent++;
                    LogAttemptSafely(new EmailAttemptRecord
                    {
                        HrName = row.Name,
                        CompanyName = row.Company,
                        EmailAddress = row.Email,
                        Status = "Sent",
                        Timestamp = DateTime.Now
                    });
                }
                catch (Exception ex)
                {
                    var failure = EmailFailureClassifier.Classify(ex);
                    TryMarkRowFailed(row.Email, failure.ErrorMessage);
                    result.Failed++;
                    LogAttemptSafely(new EmailAttemptRecord
                    {
                        HrName = row.Name,
                        CompanyName = row.Company,
                        EmailAddress = row.Email,
                        Status = "Failed",
                        FailureCategory = failure.Category,
                        SmtpErrorCode = failure.SmtpErrorCode,
                        ErrorMessage = failure.ErrorMessage,
                        Timestamp = DateTime.Now
                    });
                    _logger.LogError(ex, "Failed to process email for {Email}. Category: {Category}", row.Email, failure.Category);
                }

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }

            _logger.LogInformation(
                $"Bulk email summary:{Environment.NewLine}" +
                $"Total Emails: {result.Found}{Environment.NewLine}" +
                $"Successfully Sent: {result.Sent}{Environment.NewLine}" +
                $"Failed: {result.Failed}");

            return result;
        }

        private void TryMarkRowFailed(string email, string errorMessage)
        {
            try
            {
                _excelService.MarkRowStatus(email, "Failed", errorMessage);
            }
            catch (Exception statusException)
            {
                _logger.LogError(statusException, "Could not update the bulk workbook status for {Email}", email);
            }
        }

        private void LogAttemptSafely(EmailAttemptRecord record)
        {
            try
            {
                _attemptLogService.LogAttempt(record);
            }
            catch (Exception logException)
            {
                _logger.LogError(logException, "Could not append the email attempt log for {Email}", record.EmailAddress);
            }
        }

        private string BuildBody(CompanyEmailRow row)
        {
            var resumeUrl = _config["Profile:ResumeUrl"] ?? string.Empty;

            return $@"Hello {row.Name},

I hope you are doing well.

I am Shivani Bhadoriya, a Software Development Engineer with 1.8 years of experience in designing and developing enterprise applications, scalable backend services, and RESTful APIs using C#, .NET Core, ASP.NET MVC, SQL Server, and Clean Architecture.

I am writing to express my interest in the {row.Title} position at {row.Company}.

At Tech Extensor Pvt. Ltd., I worked on enterprise-grade, multi-tenant applications where I developed REST APIs, backend services, reporting modules, and database solutions. I also built Notification Services, Template Services, and background job processing using RabbitMQ, while following Clean Architecture and microservices principles to deliver scalable and maintainable solutions.

My technical expertise includes:

C#, .NET Core, ASP.NET MVC, Web API, LINQ, Blazor, Razor Pages
SQL Server, PostgreSQL, Oracle, T-SQL
REST APIs, RabbitMQ
Clean Architecture, Repository Pattern, Microservices
Git, Bitbucket, GitLab

I am passionate about software development and enjoy solving complex engineering problems while building high-quality, scalable applications.

You can find my resume here:
{resumeUrl}

I would appreciate the opportunity to discuss how my skills and experience align with your requirements.

Best regards,
Shivani Bhadoriya
📞 +91 7697029714
📧 Shivani.bhadouriya110@gmail.com
LinkedIn: https://www.linkedin.com/in/shivani-bhadoriya10/
LeetCode: https://leetcode.com/u/__shivani123/";
        }
    }

    public class BulkEmailResult
    {
        public int Found { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
    }
}
