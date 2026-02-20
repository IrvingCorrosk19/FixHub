namespace FixHub.Application.Common.Interfaces;

/// <summary>
/// FASE 13: Componedor de emails — recibe evento + datos del job y produce ToEmail, Subject, HtmlBody, JobId.
/// </summary>
public interface INotificationEmailComposer
{
    /// <summary>Produce el email para el evento dado.</summary>
    EmailComposition Compose(
        string eventType,
        string toEmail,
        string userName,
        string message,
        Guid jobId,
        string jobTitle,
        string categoryName,
        string addressText,
        string? technicianName = null,
        string? technicianPhone = null);
}

/// <summary>Resultado de la composición de un email.</summary>
public record EmailComposition(string ToEmail, string Subject, string HtmlBody, Guid? JobId);
