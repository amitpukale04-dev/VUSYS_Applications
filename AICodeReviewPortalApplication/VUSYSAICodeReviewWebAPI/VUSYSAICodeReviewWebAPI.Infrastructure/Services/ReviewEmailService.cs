using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VUSYSAICodeReviewWebAPI.Application.Abstractions.Services;
using VUSYSAICodeReviewWebAPI.Application.Common.Models;
using VUSYSAICodeReviewWebAPI.Application.Common.Options;

namespace VUSYSAICodeReviewWebAPI.Infrastructure.Services;

public sealed class ReviewEmailService(IOptions<ReviewEmailOptions> configuration) : IReviewEmail
{
    public async Task<(string Status, string Message)> Notify(Pull pull, string repository, long reviewId,
        string body, CancellationToken ct)
    {
        var options = configuration.Value;
        // GitHub's public profile email does not expose a verified flag for another user.
        // Only trust an administratively verified address keyed by the immutable GitHub user ID.
        if (pull.Author.ValueKind != JsonValueKind.Object || !pull.Author.TryGetProperty("id", out var id)
            || !id.TryGetInt64(out long authorId)
            || !options.VerifiedAuthorEmails.TryGetValue(authorId.ToString(CultureInfo.InvariantCulture), out var address)
            || !MailAddress.TryCreate(address, out var recipient)
            || recipient.Host.Equals("noreply.github.com", StringComparison.OrdinalIgnoreCase)
            || recipient.Host.EndsWith(".noreply.github.com", StringComparison.OrdinalIgnoreCase))
            return ("skipped", "Author email skipped because no verified address is configured.");

        if (string.IsNullOrWhiteSpace(options.Host) || string.IsNullOrWhiteSpace(options.From))
            return ("skipped", "Author email skipped because SMTP is not configured.");

        using var message = new MailMessage(new MailAddress(options.From), recipient)
        {
            Subject = $"Code review published for {repository} PR #{pull.Number}",
            Body = $"Repository: {repository}\nPR #{pull.Number}: {pull.Title}\n\n{body}\n\n"
                + $"https://github.com/{repository}/pull/{pull.Number}#pullrequestreview-{reviewId}"
        };
        using var smtp = new SmtpClient(options.Host, options.Port) { EnableSsl = true };
        if (!string.IsNullOrWhiteSpace(options.Username))
            smtp.Credentials = new NetworkCredential(options.Username, options.Password);
        await smtp.SendMailAsync(message, ct);
        return ("sent", "Author email accepted by the mail server.");
    }
}
