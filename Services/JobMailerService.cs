using OfficeOpenXml;
using System.Net;
using System.Net.Mail;
using JobMailerApi.Models;

namespace JobMailerApi.Services
{
    public class JobMailerService
    {
        private readonly IConfiguration _config;
        private readonly EmailAttemptLogService _attemptLogService;

        public JobMailerService(IConfiguration config, EmailAttemptLogService attemptLogService)
        {
            _config = config;
            _attemptLogService = attemptLogService;
        }

        public void ProcessApplication(JobApplicationRequest req)
        {
            var body = BuildEmailBody(req);
            try
            {
                SendEmail(req.HrEmail, "Application for Software Developer Role", body);
                LogAttemptSafely(new EmailAttemptRecord
                {
                    HrName = req.HrName,
                    CompanyName = req.CompanyName,
                    EmailAddress = req.HrEmail,
                    Status = "Sent",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                var failure = EmailFailureClassifier.Classify(ex);
                LogAttemptSafely(new EmailAttemptRecord
                {
                    HrName = req.HrName,
                    CompanyName = req.CompanyName,
                    EmailAddress = req.HrEmail,
                    Status = "Failed",
                    FailureCategory = failure.Category,
                    SmtpErrorCode = failure.SmtpErrorCode,
                    ErrorMessage = failure.ErrorMessage,
                    Timestamp = DateTime.Now
                });
                throw;
            }

            LogToExcel(req);
        }

        private void LogAttemptSafely(EmailAttemptRecord record)
        {
            try
            {
                _attemptLogService.LogAttempt(record);
            }
            catch
            {
                // The SMTP result must be preserved even if an audit workbook is locked or unavailable.
            }
        }

        private string BuildEmailBody(JobApplicationRequest req)
        {
            var template = @"Hello {HR_NAME},

            I hope you are doing well.

            My name is Shivani Bhadoriya, and I am a Software Development Engineer with 1.8 years of experience in designing and developing enterprise applications, scalable backend services, and RESTful APIs using C#, .NET Core, ASP.NET MVC, SQL Server, and Clean Architecture.

            I am writing to express my interest in the {JOB_TITLE} position at {COMPANY_NAME}.

            At Tech Extensor Pvt. Ltd., I worked on enterprise-grade, multi-tenant applications where I developed REST APIs, backend services, reporting modules, and database solutions. I also built Notification Services, Template Services, and background job processing using RabbitMQ, while following Clean Architecture and microservices principles to deliver scalable and maintainable solutions.

            My technical expertise includes:

            C#, .NET Core, ASP.NET MVC, Web API, LINQ, Blazor, Razor Pages
            SQL Server, PostgreSQL, Oracle, T-SQL
            REST APIs, RabbitMQ
            Clean Architecture, Repository Pattern, Microservices
            Git, Bitbucket, GitLab

            I am passionate about software development and enjoy solving complex engineering problems while building high-quality, scalable applications.

            You can find my resume here:
            {RESUME_URL}

            I would appreciate the opportunity to discuss how my skills and experience align with your requirements. Thank you for your time and consideration. I look forward to hearing from you.

            Best regards,

            Shivani Bhadoriya
            📞 +91 7697029714
            📧 Shivani.bhadouriya110@gmail.com
            LinkedIn: https://www.linkedin.com/in/shivani-bhadoriya10/
            LeetCode: https://leetcode.com/u/__shivani123/";

            return template
                .Replace("{HR_NAME}", req.HrName)
                .Replace("{JOB_TITLE}", req.JobTitle)
                .Replace("{COMPANY_NAME}", req.CompanyName)
                .Replace("{LOCATION}", req.Location)
                .Replace("{RESUME_URL}", _config["Profile:ResumeUrl"] ?? string.Empty);
        }

        private void SendEmail(string toEmail, string subject, string body)
        {
            var smtp = _config.GetSection("Smtp");
            using var client = new SmtpClient(smtp["Host"], int.Parse(smtp["Port"]!))
            {
                Credentials = new NetworkCredential(smtp["User"], smtp["Password"]),
                EnableSsl = bool.Parse(smtp["EnableSsl"]!)
            };

            using var mail = new MailMessage(smtp["FromEmail"]!, toEmail, subject, body);
            client.Send(mail);
        }

        public void SendCustomEmail(string toEmail, string subject, string body)
        {
            var smtp = _config.GetSection("Smtp");
            using var client = new SmtpClient(smtp["Host"], int.Parse(smtp["Port"]!))
            {
                Credentials = new NetworkCredential(smtp["User"], smtp["Password"]),
                EnableSsl = bool.Parse(smtp["EnableSsl"]!)
            };

            using var mail = new MailMessage(smtp["FromEmail"]!, toEmail, subject, body);
            client.Send(mail);
        }

        private void LogToExcel(JobApplicationRequest req)
        {
            var excelPath = _config["JobMailer:ApplicationLogPath"]!;
            var fileInfo = new FileInfo(excelPath);

            using var package = new ExcelPackage(fileInfo);
            var ws = package.Workbook.Worksheets.Count > 0
                ? package.Workbook.Worksheets[0]
                : package.Workbook.Worksheets.Add("Applications");

            if (ws.Dimension == null)
            {
                ws.Cells[1, 1].Value = "Date";
                ws.Cells[1, 2].Value = "Company";
                ws.Cells[1, 3].Value = "HR Name";
                ws.Cells[1, 4].Value = "HR Email";
                ws.Cells[1, 5].Value = "Job Title";
                ws.Cells[1, 6].Value = "Job Link";
                ws.Cells[1, 7].Value = "Location";
                ws.Cells[1, 8].Value = "Resume URL";
            }

            var nextRow = (ws.Dimension?.End.Row ?? 1) + 1;

            ws.Cells[nextRow, 1].Value = DateTime.Now;
            ws.Cells[nextRow, 2].Value = req.CompanyName;
            ws.Cells[nextRow, 3].Value = req.HrName;
            ws.Cells[nextRow, 4].Value = req.HrEmail;
            ws.Cells[nextRow, 5].Value = req.JobTitle;
            ws.Cells[nextRow, 6].Value = req.JobLink;
            ws.Cells[nextRow, 7].Value = req.Location;
            ws.Cells[nextRow, 8].Value = req.ResumeUrl;

            package.Save();
        }
    }
}
