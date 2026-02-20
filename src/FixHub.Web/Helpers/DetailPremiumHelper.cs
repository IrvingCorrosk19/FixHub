namespace FixHub.Web.Helpers;

/// <summary>
/// FASE 12: Helpers para Jobs/Detail — Timeline, progreso, copy premium.
/// </summary>
public static class DetailPremiumHelper
{
    /// <summary>Formato "14:32 · hace 8 min" para timestamps reales.</summary>
    public static string FormatTimestamp(DateTime? utc, string pendingText = "Pendiente")
    {
        if (!utc.HasValue) return pendingText;
        var local = utc.Value.ToLocalTime();
        var timeStr = local.ToString("HH:mm");
        var ago = RelativeTime(utc.Value);
        return $"{timeStr} · {ago}";
    }

    public static string RelativeTime(DateTime utcTime)
    {
        var diff = DateTime.UtcNow - utcTime;
        if (diff.TotalSeconds < 60) return "hace un momento";
        if (diff.TotalMinutes < 60) return $"hace {(int)diff.TotalMinutes} min";
        if (diff.TotalHours < 24) return $"hace {(int)diff.TotalHours} h";
        if (diff.TotalDays < 7) return $"hace {(int)diff.TotalDays} días";
        return utcTime.ToLocalTime().ToString("dd/MM/yyyy");
    }

    /// <summary>Progreso 0–100 según estado.</summary>
    public static int ProgressPercent(string status) => status switch
    {
        "Open" => 25,
        "Assigned" => 50,
        "InProgress" => 75,
        "Completed" => 100,
        "Cancelled" => 100,
        _ => 0
    };

    /// <summary>Mensaje humano calmante según estado.</summary>
    public static string StatusMessage(string status) => status switch
    {
        "Open" => "Hemos recibido tu solicitud. Estamos coordinando tu técnico.",
        "Assigned" => "Técnico asignado. Te avisaremos cuando esté en camino.",
        "InProgress" => "Tu técnico está en camino o realizando el servicio.",
        "Completed" => "Servicio completado. ¡Gracias por confiar en FixHub!",
        "Cancelled" => "Esta solicitud ha sido cancelada.",
        _ => "En proceso."
    };

    /// <summary>Indica si la barra debe mostrarse en rojo (cancelado).</summary>
    public static bool IsCancelled(string status) => status == "Cancelled";
}
