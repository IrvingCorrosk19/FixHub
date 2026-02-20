using System.Text.Json;

namespace FixHub.Infrastructure.EmailTemplates;

/// <summary>
/// FASE 13: Plantillas HTML premium para emails.
/// Header + logo placeholder, estado grande, resumen, CTA "Ver mi servicio", footer soporte.
/// </summary>
public static class PremiumEmailTemplates
{
    private const string AppName = "FixHub";
    private const string SupportEmail = "soporte@fixhub.com";
    private const string SupportPhone = "+56 9 1234 5678";

    private static readonly string Wrapper = $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""/><meta name=""viewport"" content=""width=device-width,initial-scale=1""/></head>
<body style=""margin:0;font-family:'Segoe UI',system-ui,sans-serif;background:#f9fafb;color:#1f2937;line-height:1.6;"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f9fafb;padding:24px 0;"">
<tr><td align=""center"">
<table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;background:#fff;border-radius:12px;box-shadow:0 4px 6px rgba(0,0,0,.07);overflow:hidden;"">
<tr><td style=""padding:28px 32px;background:linear-gradient(135deg,#1E3A8A,#2563EB);text-align:center;"">
<div style=""font-size:24px;font-weight:700;color:#fff;"">🔧 {AppName}</div>
<div style=""font-size:12px;color:rgba(255,255,255,.85);margin-top:4px;"">Servicios técnicos del hogar</div>
</td></tr>
<tr><td style=""padding:32px;"">
{{BODY}}
</td></tr>
<tr><td style=""padding:20px 32px;background:#f9fafb;border-top:1px solid #e5e7eb;font-size:12px;color:#6b7280;text-align:center;"">
<div>¿Necesitas ayuda? <a href=""mailto:{SupportEmail}"" style=""color:#2563eb;"">{SupportEmail}</a> · {SupportPhone}</div>
<div style=""margin-top:8px;"">&copy; {DateTime.UtcNow.Year} {AppName}</div>
</td></tr>
</table>
</td></tr>
</table>
</body>
</html>";

    /// <summary>Overload para modelo tipado (FASE 13 Composer).</summary>
    public static string GetHtml(string type, EmailTemplateModel model)
    {
        var userName = model.UserName ?? "Usuario";
        var message = model.Message ?? "";
        var jobTitle = model.JobTitle ?? "Trabajo";
        var categoryName = model.CategoryName ?? "";
        var addressText = model.AddressText ?? "";
        var detailUrl = model.DetailUrl ?? "#";
        var technicianName = model.TechnicianName;
        var technicianPhone = model.TechnicianPhone;
        return BuildHtml(type, userName, message, jobTitle, categoryName, addressText, detailUrl, technicianName, technicianPhone);
    }

    public static string GetHtml(string type, JsonDocument payload)
    {
        var userName = GetString(payload, "userName") ?? "Usuario";
        var message = GetString(payload, "message") ?? "";
        var jobTitle = GetString(payload, "jobTitle") ?? "Trabajo";
        var categoryName = GetString(payload, "categoryName") ?? "";
        var addressText = GetString(payload, "addressText") ?? "";
        var detailUrl = GetString(payload, "detailUrl") ?? "#";
        return BuildHtml(type, userName, message, jobTitle, categoryName, addressText, detailUrl, null, null);
    }

    private static string BuildHtml(string type, string userName, string message, string jobTitle,
        string categoryName, string addressText, string detailUrl, string? technicianName, string? technicianPhone)
    {
        var showCta = !string.IsNullOrEmpty(detailUrl) && detailUrl != "#";

        var (title, icon, statusBg) = type switch
        {
            "JobReceived" or "JobCreated" => ("Solicitud recibida", "📋", "#EFF6FF"),
            "JobAssigned" => ("Técnico asignado", "✅", "#ECFDF5"),
            "JobStarted" => ("Servicio en camino", "🚀", "#FEF3C7"),
            "JobCompleted" => ("Servicio completado", "🎉", "#ECFDF5"),
            "JobCancelled" => ("Solicitud cancelada", "⚠️", "#FEF2F2"),
            "IssueReported" => ("Incidencia reportada", "⚠️", "#FEF2F2"),
            "SlaAlert" => ("Alerta SLA", "⚠️", "#FEF2F2"),
            _ => ("Notificación", "🔔", "#F3F4F6")
        };

        var summaryHtml = "";
        if (!string.IsNullOrEmpty(categoryName) || !string.IsNullOrEmpty(jobTitle) || !string.IsNullOrEmpty(addressText))
        {
            var lines = new List<string>();
            if (!string.IsNullOrEmpty(categoryName)) lines.Add($"<strong>Categoría:</strong> {Escape(categoryName)}");
            if (!string.IsNullOrEmpty(jobTitle)) lines.Add($"<strong>Trabajo:</strong> {Escape(jobTitle)}");
            if (!string.IsNullOrEmpty(addressText)) lines.Add($"<strong>Dirección:</strong> {Escape(addressText)}");
            if (!string.IsNullOrEmpty(technicianName)) lines.Add($"<strong>Técnico:</strong> {Escape(technicianName)}" + (string.IsNullOrEmpty(technicianPhone) ? "" : $" · {Escape(technicianPhone)}"));
            summaryHtml = $@"<div style=""background:{statusBg};padding:16px;border-radius:8px;margin:20px 0;font-size:14px;"">
{string.Join("<br/>", lines)}
</div>";
        }

        var bodyContent = type switch
        {
            "JobReceived" or "JobCreated" => $@"<p style=""font-size:18px;font-weight:600;color:#1e3a8a;margin:0 0 12px;"">{icon} {title}</p>
<p>Hola <strong>{Escape(userName)}</strong>,</p>
<p>Hemos recibido tu solicitud. Nuestro equipo la está revisando y te asignará un técnico pronto.</p>
{summaryHtml}
<p style=""color:#6b7280;font-size:14px;"">Tiempo estimado de asignación: 15 minutos.</p>",
            "JobAssigned" => $@"<p style=""font-size:18px;font-weight:600;color:#1e3a8a;margin:0 0 12px;"">{icon} {title}</p>
<p>Hola <strong>{Escape(userName)}</strong>,</p>
<p>Te hemos asignado un técnico para tu solicitud. Te avisaremos cuando esté en camino.</p>
{summaryHtml}
<p>{Escape(message)}</p>",
            "JobStarted" => $@"<p style=""font-size:18px;font-weight:600;color:#1e3a8a;margin:0 0 12px;"">{icon} {title}</p>
<p>Hola <strong>{Escape(userName)}</strong>,</p>
<p>Tu técnico ya está en camino o realizando el servicio.</p>
{summaryHtml}
<p>{Escape(message)}</p>",
            "JobCompleted" => $@"<p style=""font-size:18px;font-weight:600;color:#16a34a;margin:0 0 12px;"">{icon} {title}</p>
<p>Hola <strong>{Escape(userName)}</strong>,</p>
<p>Tu servicio ha sido completado. ¡Gracias por confiar en {AppName}!</p>
{summaryHtml}
<p>{Escape(message)}</p>
<p style=""margin-top:20px;""><strong>¿Cómo estuvo el servicio?</strong> Tu opinión nos ayuda a mejorar.</p>",
            "JobCancelled" => $@"<p style=""font-size:18px;font-weight:600;color:#991b1b;margin:0 0 12px;"">{icon} {title}</p>
<p>Hola <strong>{Escape(userName)}</strong>,</p>
<p>La siguiente solicitud ha sido cancelada:</p>
{summaryHtml}
<p>{Escape(message)}</p>",
            "IssueReported" => $@"<p style=""font-size:18px;font-weight:600;color:#991b1b;margin:0 0 12px;"">{icon} {title}</p>
<p>Hola <strong>{Escape(userName)}</strong>,</p>
<p>Se ha reportado una incidencia en un trabajo:</p>
{summaryHtml}
<p>{Escape(message)}</p>",
            "SlaAlert" => $@"<p style=""font-size:18px;font-weight:600;color:#991b1b;margin:0 0 12px;"">{icon} {title}</p>
<p>Hola <strong>{Escape(userName)}</strong>,</p>
<p><strong>Alerta SLA:</strong></p>
<p style=""background:#fef2f2;padding:16px;border-radius:8px;border-left:4px solid #dc2626;"">{Escape(message)}</p>
{summaryHtml}",
            _ => $@"<p style=""font-size:18px;font-weight:600;color:#1e3a8a;margin:0 0 12px;"">{icon} {title}</p>
<p>Hola <strong>{Escape(userName)}</strong>,</p>
<p>{Escape(message)}</p>
{summaryHtml}"
        };

        var ctaHtml = showCta ? $@"
<div style=""margin:24px 0;text-align:center;"">
<a href=""{Escape(detailUrl)}"" style=""display:inline-block;background:#2563eb;color:#fff!important;padding:14px 28px;border-radius:8px;font-weight:600;text-decoration:none;font-size:15px;"">Ver mi servicio</a>
</div>" : "";

        var ratingCta = (type == "JobCompleted" && showCta) ? $@"
<div style=""margin:16px 0;text-align:center;"">
<a href=""{Escape(detailUrl)}"" style=""display:inline-block;background:#eab308;color:#1f2937!important;padding:14px 28px;border-radius:8px;font-weight:600;text-decoration:none;font-size:15px;"">⭐ Calificar servicio</a>
</div>" : "";

        var body = bodyContent + ctaHtml + ratingCta;
        return Wrapper.Replace("{{BODY}}", body);
    }

    private static string? GetString(JsonDocument doc, string prop)
    {
        if (doc.RootElement.TryGetProperty(prop, out var el))
            return el.GetString();
        return null;
    }

    private static string Escape(string s) => System.Net.WebUtility.HtmlEncode(s ?? "");
}

/// <summary>Modelo para plantillas (FASE 13).</summary>
public class EmailTemplateModel
{
    public string? UserName { get; set; }
    public string? Message { get; set; }
    public string? JobTitle { get; set; }
    public string? CategoryName { get; set; }
    public string? AddressText { get; set; }
    public string? DetailUrl { get; set; }
    public string? TechnicianName { get; set; }
    public string? TechnicianPhone { get; set; }
}
