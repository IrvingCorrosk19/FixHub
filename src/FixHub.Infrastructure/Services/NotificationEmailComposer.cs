using FixHub.Application.Common.Interfaces;
using FixHub.Infrastructure.EmailTemplates;
using Microsoft.Extensions.Configuration;

namespace FixHub.Infrastructure.Services;

/// <summary>
/// FASE 13: Componedor de emails — recibe evento + datos del job y produce ToEmail, Subject, HtmlBody, JobId.
/// </summary>
public class NotificationEmailComposer : INotificationEmailComposer
{
    private readonly IConfiguration _config;

    public NotificationEmailComposer(IConfiguration config) => _config = config;

    private string BaseWebUrl => _config["BaseWebUrl"] ?? _config["WebOrigin"] ?? "https://localhost:7200";

    /// <summary>Produce el email para el evento dado.</summary>
    public EmailComposition Compose(
        string eventType,
        string toEmail,
        string userName,
        string message,
        Guid jobId,
        string jobTitle,
        string categoryName,
        string addressText,
        string? technicianName = null,
        string? technicianPhone = null)
    {
        var detailUrl = $"{BaseWebUrl.TrimEnd('/')}/Jobs/Detail/{jobId}";
        var subject = GetSubject(eventType);
        var htmlBody = PremiumEmailTemplates.GetHtml(eventType, new EmailTemplateModel
        {
            UserName = userName,
            Message = message,
            JobTitle = jobTitle,
            CategoryName = categoryName,
            AddressText = addressText,
            DetailUrl = detailUrl,
            TechnicianName = technicianName,
            TechnicianPhone = technicianPhone
        });
        return new EmailComposition(toEmail, subject, htmlBody, jobId);
    }

    private static string GetSubject(string eventType) => eventType switch
    {
        "JobReceived" => "FixHub — Hemos recibido tu solicitud",
        "JobCreated" => "FixHub — Nueva solicitud creada",
        "JobAssigned" => "FixHub — Técnico asignado",
        "JobStarted" => "FixHub — Servicio en camino",
        "JobCompleted" => "FixHub — Servicio completado",
        "JobCancelled" => "FixHub — Solicitud cancelada",
        "IssueReported" => "FixHub — Incidencia reportada",
        "SlaAlert" => "FixHub — Alerta SLA",
        _ => "FixHub — Notificación"
    };
}
