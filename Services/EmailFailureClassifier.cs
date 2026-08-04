using System.Net.Mail;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using JobMailerApi.Models;

namespace JobMailerApi.Services
{
    public static class EmailFailureClassifier
    {
        public static EmailFailureDetails ClassifyBounce(string message)
        {
            var details = Classify(new SmtpException(message));
            var smtpCodeMatch = Regex.Match(message, @"\b([45]\d\d)\b");

            if (smtpCodeMatch.Success)
            {
                details.SmtpErrorCode = smtpCodeMatch.Groups[1].Value;
            }

            return details;
        }

        public static EmailFailureDetails Classify(Exception exception)
        {
            var smtpException = FindException<SmtpException>(exception);
            var socketException = FindException<SocketException>(exception);
            var message = exception.Message;
            var normalizedMessage = message.ToLowerInvariant();

            var details = new EmailFailureDetails
            {
                SmtpErrorCode = smtpException is null ? string.Empty : ((int)smtpException.StatusCode).ToString(),
                ErrorMessage = message
            };

            if (exception is FormatException ||
                (normalizedMessage.Contains("invalid") &&
                 (normalizedMessage.Contains("email") || normalizedMessage.Contains("address") || normalizedMessage.Contains("recipient"))))
            {
                details.Category = "Invalid Email Address";
            }
            else if (normalizedMessage.Contains("recipient not found") ||
                     normalizedMessage.Contains("user unknown") ||
                     normalizedMessage.Contains("no such user") ||
                     normalizedMessage.Contains("does not exist") ||
                     normalizedMessage.Contains("address not found") ||
                     normalizedMessage.Contains("unknown to address") ||
                     normalizedMessage.Contains("couldn't be found"))
            {
                details.Category = "Recipient Not Found";
            }
            else if (normalizedMessage.Contains("recipient address rejected") ||
                     normalizedMessage.Contains("address rejected") ||
                     normalizedMessage.Contains("recipient rejected"))
            {
                details.Category = "Recipient Address Rejected";
            }
            else if ((normalizedMessage.Contains("daily") && normalizedMessage.Contains("limit")) ||
                     normalizedMessage.Contains("sending limit") ||
                     normalizedMessage.Contains("too many recipients") ||
                     normalizedMessage.Contains("quota exceeded"))
            {
                details.Category = "Daily Gmail Sending Limit Exceeded";
            }
            else if (normalizedMessage.Contains("blocked") || normalizedMessage.Contains("spam"))
            {
                details.Category = "Message Blocked";
            }
            else if (exception is TimeoutException || normalizedMessage.Contains("timed out"))
            {
                details.Category = "Timeout";
            }
            else if (socketException is not null)
            {
                details.Category = "Network Error";
            }
            else if (smtpException?.StatusCode is SmtpStatusCode.MailboxBusy or SmtpStatusCode.MailboxUnavailable)
            {
                details.Category = "Mailbox Unavailable";
            }

            return details;
        }

        private static TException? FindException<TException>(Exception exception)
            where TException : Exception
        {
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                if (current is TException match)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
