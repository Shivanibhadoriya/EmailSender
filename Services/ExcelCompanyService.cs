using JobMailerApi.Models;
using OfficeOpenXml;

namespace JobMailerApi.Services
{
    public class ExcelCompanyService
    {
        public readonly IConfiguration _config;

        public ExcelCompanyService(IConfiguration config)
        {
            _config = config;
        }

        public List<CompanyEmailRow> GetPendingRows(int  takeCount)
        {
            var excelPath = _config["JobMailer:BulkExcelPath"]!;
            var fileInfo = new FileInfo(excelPath);

            using var package = new ExcelPackage(fileInfo);
            var ws = package.Workbook.Worksheets[0];
            var rows = new List<CompanyEmailRow>();

            if(ws.Dimension == null)
            {
                return rows;
            }

            var startRow = 2;
            var endRow = ws.Dimension.End.Row; 

            for(int row = startRow; row <= endRow; row++)
            {
                var status = ws.Cells[row, 6].Text?.Trim();

                var isReadyToSend = string.IsNullOrWhiteSpace(status) ||
                    status.Equals("Pending", StringComparison.OrdinalIgnoreCase);

                if (!isReadyToSend)
                {
                    continue;
                }

                rows.Add(new CompanyEmailRow
                {
                    SNo = int.TryParse(ws.Cells[row, 1].Text, out var sno) ? sno : row - 1,
                    Name = ws.Cells[row, 2].Text,
                    Email = ws.Cells[row, 3].Text,
                    Title = ws.Cells[row, 4].Text,
                    Company = ws.Cells[row, 5].Text,
                    Status = ws.Cells[row, 6].Text
                });

                if (rows.Count >= takeCount)
                    break;
            }
            return rows;
        }

        public void MarkRowStatus(string email, string status, string? errorMessage = null)
        {
            var excelPath = _config["JobMailer:BulkExcelPath"]!;
            var fileInfo = new FileInfo(excelPath);

            using var package = new ExcelPackage(fileInfo);
            var ws = package.Workbook.Worksheets[0];

            if (ws.Dimension == null)
                return;

            for (int row = 2; row <= ws.Dimension.End.Row; row++)
            {
                var currentEmail = ws.Cells[row, 3].Text?.Trim();

                if (string.Equals(currentEmail, email, StringComparison.OrdinalIgnoreCase))
                {
                    ws.Cells[row, 6].Value = status;
                    ws.Cells[row, 7].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    ws.Cells[row, 8].Value = errorMessage ?? string.Empty;
                    package.Save();
                    return;
                }
            }
        }

        public CompanyEmailRow? GetRowByEmail(string email)
        {
            var excelPath = _config["JobMailer:BulkExcelPath"]!;
            using var package = new ExcelPackage(new FileInfo(excelPath));
            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet.Dimension is null)
            {
                return null;
            }

            for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                if (!string.Equals(worksheet.Cells[row, 3].Text?.Trim(), email, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return new CompanyEmailRow
                {
                    SNo = int.TryParse(worksheet.Cells[row, 1].Text, out var sno) ? sno : row - 1,
                    Name = worksheet.Cells[row, 2].Text,
                    Email = worksheet.Cells[row, 3].Text,
                    Title = worksheet.Cells[row, 4].Text,
                    Company = worksheet.Cells[row, 5].Text,
                    Status = worksheet.Cells[row, 6].Text
                };
            }

            return null;
        }

    }
}

