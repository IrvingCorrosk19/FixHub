using FixHub.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace FixHub.Infrastructure.Services;

/// <summary>
/// FASE 10.2: Implementación de IEmailSender usando SendGrid.
/// </summary>
public class SendGridEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SendGridEmailSender> _log;
    private readonly string? _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailSender(IConfiguration config, ILogger<SendGridEmailSender> log)
    {
        _config = config;
        _log = log;
        var section = config.GetSection("SendGrid");
        _apiKey = section["ApiKey"];
        _fromEmail = section["FromEmail"] ?? "noreply@fixhub.com";
        _fromName = section["FromName"] ?? "FixHub";
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _log.LogWarning("SendGrid ApiKey not configured; skipping email to {To}", toEmail);
            return false;
        }

        try
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlBody);
            var response = await client.SendEmailAsync(msg, ct);

            if (response.IsSuccessStatusCode)
            {
                _log.LogDebug("Email sent to {To}", toEmail);
                return true;
            }

            var body = await response.Body.ReadAsStringAsync(ct);
            _log.LogWarning("SendGrid failed: {StatusCode} - {Body}", response.StatusCode, body);
            return false;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error sending email to {To}", toEmail);
            throw;
        }
    }
}
