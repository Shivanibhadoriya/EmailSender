using OfficeOpenXml;
using System.Net;
using System.Net.Mail;
using JobMailerApi.Models;

namespace JobMailerApi.Services
{
    public class JobMailerService
    {
        private readonly IConfiguration _config;

        public JobMailerService(IConfiguration config)
        {
            _config = config;
        }

        public void ProcessApplication(JobApplicationRequest req)
        {
            var body = BuildEmailBody(req);
            SendEmail(req.HrEmail, $"Application for {req.JobTitle}", body);
            LogToExcel(req);
        }

        private string BuildEmailBody(JobApplicationRequest req)
        {
            var template = @"
Dear {HR_NAME},

I hope you are doing well. I am writing to express my interest in the position of {JOB_TITLE} at {COMPANY_NAME}, located in {LOCATION}.

You can find my resume here: {RESUME_URL}
Job posting: {JOB_LINK}

Thank you for considering my application.

Best regards,
[Your Name]
[Your Phone]";

            return template
                .Replace("{HR_NAME}", req.HrName)
                .Replace("{JOB_TITLE}", req.JobTitle)
                .Replace("{COMPANY_NAME}", req.CompanyName)
                .Replace("{LOCATION}", req.Location)
                .Replace("{RESUME_URL}", req.ResumeUrl)
                .Replace("{JOB_LINK}", req.JobLink);
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

        private void LogToExcel(JobApplicationRequest req)
        {
            var excelPath = _config["JobMailer:ExcelPath"]!;
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