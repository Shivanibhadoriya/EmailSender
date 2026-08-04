using System.Text.RegularExpressions;
using JobMailerApi.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;

namespace JobMailerApi.Services
{
    public class BounceProcessingService
    {
        private readonly IConfiguration _config;
        private readonly ExcelCompanyService _excelService;
        private readonly EmailAttemptLogService _attemptLogService;
        private readonly ILogger<BounceProcessingService> _logger;

        public BounceProcessingService(
            IConfiguration config,
            ExcelCompanyService excelService,
            EmailAttemptLogService attemptLogService,
            ILogger<BounceProcessingService> logger)
        {
            _config = config;
            _excelService = excelService;
            _attemptLogService = attemptLogService;
            _logger = logger;
        }

        public async Task<BounceProcessingResult> ProcessUnreadBouncesAsync(CancellationToken cancellationToken)
        {
            var smtp = _config.GetSection("Smtp");
            var host = _config["BounceProcessing:ImapHost"] ?? "imap.gmail.com";
            var port = _config.GetValue<int?>("BounceProcessing:ImapPort") ?? 993;
            var userName = smtp["User"] ?? throw new InvalidOperationException("SMTP user is not configured.");
            var password = smtp["Password"] ?? throw new InvalidOperationException("SMTP password is not configured.");
            var result = new BounceProcessingResult();

            using var client = new ImapClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect, cancellationToken);
            await client.AuthenticateAsync(userName, password, cancellationToken);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var query = SearchQuery.NotSeen.And(
                SearchQuery.Or(
                    SearchQuery.FromContains("mailer-daemon"),
                    SearchQuery.FromContains("postmaster")));
            var messageIds = await inbox.SearchAsync(query, cancellationToken);
            result.BounceMessagesFound = messageIds.Count;

            foreach (var messageId in messageIds)
            {
                var message = await inbox.GetMessageAsync(messageId, cancellationToken);
                var bounceText = GetBounceText(message.Subject, message.TextBody, message.HtmlBody);
                var recipient = ExtractFailedRecipient(bounceText, smtp["FromEmail"]);

                if (string.IsNullOrWhiteSpace(recipient))
                {
                    result.UnmatchedBounceMessages++;
                    _logger.LogWarning("Could not identify a failed recipient from bounce message {MessageId}", messageId);
                    continue;
                }

                try
                {
                    var failure = EmailFailureClassifier.ClassifyBounce(bounceText);
                    var sourceRow = _excelService.GetRowByEmail(recipient);

                    _excelService.MarkRowStatus(recipient, "Bounced", failure.ErrorMessage);
                    _attemptLogService.LogAttempt(new EmailAttemptRecord
                    {
                        HrName = sourceRow?.Name ?? string.Empty,
                        CompanyName = sourceRow?.Company ?? string.Empty,
                        EmailAddress = recipient,
                        Status = "Bounced",
                        FailureCategory = failure.Category,
                        SmtpErrorCode = failure.SmtpErrorCode,
                        ErrorMessage = failure.ErrorMessage,
                        Timestamp = DateTime.Now
                    });

                    await inbox.AddFlagsAsync(messageId, MessageFlags.Seen, true, cancellationToken);
                    result.BouncesProcessed++;
                    _logger.LogInformation("Recorded bounced recipient {Email} as {Category}", recipient, failure.Category);
                }
                catch (Exception ex)
                {
                    result.ProcessingFailures++;
                    _logger.LogError(ex, "Could not record bounce message {MessageId} for {Email}", messageId, recipient);
                }
            }

            await client.DisconnectAsync(true, cancellationToken);
            return result;
        }

        private static string GetBounceText(string? subject, string? textBody, string? htmlBody)
        {
            var htmlAsText = Regex.Replace(htmlBody ?? string.Empty, "<[^>]+>", " ");
            return $"{subject}\n{textBody}\n{htmlAsText}";
        }

        private static string? ExtractFailedRecipient(string bounceText, string? senderEmail)
        {
            var patterns = new[]
            {
                @"Final-Recipient:\s*rfc822;\s*([\w.+-]+@[\w.-]+\.[A-Za-z]{2,})",
                @"(?:Your message to|wasn't delivered to)\s*([\w.+-]+@[\w.-]+\.[A-Za-z]{2,})",
                @"([\w.+-]+@[\w.-]+\.[A-Za-z]{2,})\s+(?:couldn't be delivered|wasn't found|was not found)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(bounceText, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            var candidates = Regex.Matches(bounceText, @"[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}")
                .Select(match => match.Value)
                .Where(email => !email.Equals(senderEmail, StringComparison.OrdinalIgnoreCase))
                .Where(email => !email.Contains("mailer-daemon", StringComparison.OrdinalIgnoreCase))
                .Where(email => !email.Contains("postmaster", StringComparison.OrdinalIgnoreCase));

            return candidates.FirstOrDefault();
        }
    }

    public class BounceProcessingResult
    {
        public int BounceMessagesFound { get; set; }
        public int BouncesProcessed { get; set; }
        public int UnmatchedBounceMessages { get; set; }
        public int ProcessingFailures { get; set; }
    }
}
