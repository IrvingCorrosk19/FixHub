using System.Text.Json;
using FixHub.Domain.Enums;

namespace FixHub.Infrastructure.EmailTemplates;

/// <summary>
/// FASE 10.2: Proveedor de plantillas HTML para emails.
/// Placeholders: {{UserName}}, {{Message}}, {{JobTitle}}, {{AppName}}
/// </summary>
public static class EmailTemplateProvider
{
    private const string AppName = "FixHub";
    private const string BaseStyles = @"
        font-family: 'Segoe UI', system-ui, sans-serif;
        line-height: 1.6;
        color: #1f2937;
        max-width: 600px;
        margin: 0 auto;
        padding: 24px;
    ";
    private const string CardStyle = @"
        background: #ffffff;
        border-radius: 12px;
        padding: 28px;
        box-shadow: 0 4px 6px rgba(0,0,0,0.07);
        border: 1px solid #e5e7eb;
    ";
    private const string HeaderStyle = @"
        color: #1e3a8a;
        font-size: 20px;
        font-weight: 600;
        margin: 0 0 16px 0;
    ";
    private const string FooterStyle = @"
        margin-top: 24px;
        padding-top: 16px;
        border-top: 1px solid #e5e7eb;
        font-size: 12px;
        color: #6b7280;
    ";

    public static string GetHtml(string type, JsonDocument payload)
    {
        var userName = payload.RootElement.TryGetProperty("userName", out var u) ? u.GetString() ?? "Usuario" : "Usuario";
        var message = payload.RootElement.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
        var jobTitle = payload.RootElement.TryGetProperty("jobTitle", out var j) ? j.GetString() ?? "Trabajo" : "Trabajo";

        var (title, icon) = type switch
        {
            "JobCreated" => ("Nueva solicitud creada", "📋"),
            "JobAssigned" => ("Trabajo asignado", "✅"),
            "JobStarted" => ("Trabajo iniciado", "🚀"),
            "JobCompleted" => ("Trabajo completado", "🎉"),
            "JobCancelled" => ("Trabajo cancelado", "⚠️"),
            "SlaAlert" => ("Alerta SLA", "⚠️"),
            _ => ("Notificación", "🔔")
        };

        var bodyContent = type switch
        {
            "JobCreated" => $@"<p>Hola <strong>{Escape(userName)}</strong>,</p>
                <p>Se ha registrado una nueva solicitud de servicio:</p>
                <p style=""background:#f3f4f6; padding:16px; border-radius:8px;""><strong>{Escape(jobTitle)}</strong></p>
                <p>{Escape(message)}</p>",
            "JobAssigned" => $@"<p>Hola <strong>{Escape(userName)}</strong>,</p>
                <p>Te hemos asignado el siguiente trabajo:</p>
                <p style=""background:#f3f4f6; padding:16px; border-radius:8px;""><strong>{Escape(jobTitle)}</strong></p>
                <p>{Escape(message)}</p>",
            "JobStarted" => $@"<p>Hola <strong>{Escape(userName)}</strong>,</p>
                <p>El técnico ha iniciado el trabajo:</p>
                <p style=""background:#f3f4f6; padding:16px; border-radius:8px;""><strong>{Escape(jobTitle)}</strong></p>
                <p>{Escape(message)}</p>",
            "JobCompleted" => $@"<p>Hola <strong>{Escape(userName)}</strong>,</p>
                <p>El trabajo ha sido completado:</p>
                <p style=""background:#f3f4f6; padding:16px; border-radius:8px;""><strong>{Escape(jobTitle)}</strong></p>
                <p>{Escape(message)}</p>
                <p>¡Gracias por confiar en {AppName}!</p>",
            "JobCancelled" => $@"<p>Hola <strong>{Escape(userName)}</strong>,</p>
                <p>El siguiente trabajo ha sido cancelado:</p>
                <p style=""background:#fef3c7; padding:16px; border-radius:8px;""><strong>{Escape(jobTitle)}</strong></p>
                <p>{Escape(message)}</p>",
            "SlaAlert" => $@"<p>Hola <strong>{Escape(userName)}</strong>,</p>
                <p><strong>Alerta SLA:</strong></p>
                <p style=""background:#fef2f2; padding:16px; border-radius:8px; border-left:4px solid #dc2626;"">{Escape(message)}</p>",
            _ => $@"<p>Hola <strong>{Escape(userName)}</strong>,</p><p>{Escape(message)}</p>"
        };

        return $@"<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""/><meta name=""viewport"" content=""width=device-width,initial-scale=1""/></head>
<body style=""background:#f9fafb; margin:0; {BaseStyles}"">
<div style=""{CardStyle}"">
    <p style=""{HeaderStyle}"">{icon} {title}</p>
    {bodyContent}
    <div style=""{FooterStyle}"">&copy; {DateTime.UtcNow.Year} {AppName} — Servicios técnicos del hogar</div>
</div>
</body>
</html>";
    }

    private static string Escape(string s) => System.Net.WebUtility.HtmlEncode(s ?? "");
}
