namespace FixHub.Infrastructure.Services;

/// <summary>
/// FASE 13: Componedor de emails — recibe tipo de evento y produce Subject.
/// Channel="Email" siempre; HtmlBody se genera en PremiumEmailTemplates.
/// </summary>
public static class NotificationComposer
{
    public static string GetSubject(string type) => type switch
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
