namespace FixHub.Application.Common.Interfaces;

/// <summary>
/// FASE 10.2: Abstracción para envío de emails (SendGrid, SMTP, etc.).
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Envía un email.
    /// </summary>
    /// <param name="toEmail">Destinatario</param>
    /// <param name="subject">Asunto</param>
    /// <param name="htmlBody">Cuerpo HTML</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True si se envió correctamente</returns>
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
