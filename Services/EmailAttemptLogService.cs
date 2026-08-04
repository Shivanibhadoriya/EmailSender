using JobMailerApi.Models;
using OfficeOpenXml;

namespace JobMailerApi.Services
{
    public class EmailAttemptLogService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailAttemptLogService> _logger;

        public EmailAttemptLogService(IConfiguration config, ILogger<EmailAttemptLogService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public void LogAttempt(EmailAttemptRecord record)
        {
            AppendRecord(_config["JobMailer:EmailAttemptLogPath"]!, "Email Attempts", record);

            if (record.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase) ||
                record.Status.Equals("Bounced", StringComparison.OrdinalIgnoreCase))
            {
                AppendRecord(_config["JobMailer:FailedEmailsPath"]!, "Failed Emails", record);
            }

            _logger.LogInformation(
                "Email attempt: {Status} for {Email} at {Timestamp}",
                record.Status,
                record.EmailAddress,
                record.Timestamp);
        }

        private static void AppendRecord(string excelPath, string worksheetName, EmailAttemptRecord record)
        {
            var fileInfo = new FileInfo(excelPath);
            using var package = new ExcelPackage(fileInfo);
            var worksheet = package.Workbook.Worksheets.Count > 0
                ? package.Workbook.Worksheets[0]
                : package.Workbook.Worksheets.Add(worksheetName);

            if (worksheet.Dimension is null)
            {
                worksheet.Cells[1, 1].Value = "HR Name";
                worksheet.Cells[1, 2].Value = "Company Name";
                worksheet.Cells[1, 3].Value = "Email Address";
                worksheet.Cells[1, 4].Value = "Status";
                worksheet.Cells[1, 5].Value = "Failure Category";
                worksheet.Cells[1, 6].Value = "SMTP Error Code";
                worksheet.Cells[1, 7].Value = "Error Message";
                worksheet.Cells[1, 8].Value = "Timestamp";
            }

            var nextRow = (worksheet.Dimension?.End.Row ?? 1) + 1;
            worksheet.Cells[nextRow, 1].Value = record.HrName;
            worksheet.Cells[nextRow, 2].Value = record.CompanyName;
            worksheet.Cells[nextRow, 3].Value = record.EmailAddress;
            worksheet.Cells[nextRow, 4].Value = record.Status;
            worksheet.Cells[nextRow, 5].Value = record.FailureCategory;
            worksheet.Cells[nextRow, 6].Value = record.SmtpErrorCode;
            worksheet.Cells[nextRow, 7].Value = record.ErrorMessage;
            worksheet.Cells[nextRow, 8].Value = record.Timestamp;
            worksheet.Cells[nextRow, 8].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";

            package.Save();
        }
    }
}
