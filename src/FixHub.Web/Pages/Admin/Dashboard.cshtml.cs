using FixHub.Web.Helpers;
using FixHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DashboardModel(IFixHubApiClient apiClient) : PageModel
{
    public OpsDashboardDto? Data { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        var result = await apiClient.GetAdminDashboardAsync();
        if (result.IsSuccess)
            Data = result.Value;
        else
            ErrorMessage = ErrorMessageHelper.GetUserFriendlyMessage(result.ErrorMessage, result.StatusCode);
    }

    // ── Acciones inline desde la tabla de solicitudes recientes ───────────────

    public async Task<IActionResult> OnPostUpdateStatusAsync(Guid jobId, string newStatus)
    {
        var result = await apiClient.AdminUpdateJobStatusAsync(jobId, newStatus);
        if (result.IsSuccess)
            TempData["Success"] = $"Solicitud actualizada a '{StatusLabel(newStatus)}'.";
        else
            TempData["Error"] = ErrorMessageHelper.GetUserFriendlyMessage(result.ErrorMessage, result.StatusCode);

        return RedirectToPage();
    }

    // ── Helpers de presentación ───────────────────────────────────────────────

    public static string RelativeTime(DateTime utcTime)
    {
        var diff = DateTime.UtcNow - utcTime;
        return diff.TotalSeconds < 60  ? "hace un momento"
             : diff.TotalMinutes < 60  ? $"hace {(int)diff.TotalMinutes} min"
             : diff.TotalHours < 24    ? $"hace {(int)diff.TotalHours} h"
             : diff.TotalDays < 7      ? $"hace {(int)diff.TotalDays} días"
             : utcTime.ToLocalTime().ToString("dd/MM/yyyy");
    }

    /// <summary>Formatea minutos como "X min" o "Yh Zmin" de forma legible.</summary>
    public static string FormatMinutes(int? minutes)
    {
        if (minutes is null) return "—";
        if (minutes < 60)    return $"{minutes} min";
        var h = minutes.Value / 60;
        var m = minutes.Value % 60;
        return m == 0 ? $"{h}h" : $"{h}h {m}min";
    }

    public static string AlertTypeLabel(string alertType) => alertType switch
    {
        "open_overdue"       => "Sin asignar",
        "inprogress_overdue" => "En camino — demora",
        "issue"              => "Incidencia reportada",
        _                    => alertType
    };

    /// <summary>Clases CSS según tipo + severidad de la alerta.</summary>
    public static string AlertRowClass(string severity) => severity switch
    {
        "CRITICAL" => "fixhub-ops-alert-critical",
        "WARNING"  => "fixhub-ops-alert-warning",
        _          => ""
    };

    public static string AlertBadgeClass(string alertType, string severity)
    {
        if (severity == "CRITICAL") return "fixhub-badge-cancelled";
        return alertType switch
        {
            "open_overdue"       => "fixhub-badge-pending",
            "inprogress_overdue" => "fixhub-badge-inprogress",
            "issue"              => "fixhub-badge-cancelled",
            _                    => "bg-secondary"
        };
    }

    public static string SeverityIcon(string severity) => severity switch
    {
        "CRITICAL" => "🔴",
        "WARNING"  => "🟡",
        _          => "🔵"
    };

    // Compat: se mantiene para alertas (llamadas existentes en cshtml)
    public static string AlertTypeBadgeClass(string alertType) => alertType switch
    {
        "open_overdue"       => "fixhub-badge-pending",
        "inprogress_overdue" => "fixhub-badge-inprogress",
        "issue"              => "fixhub-badge-cancelled",
        _                    => "bg-secondary"
    };

    public static string StatusLabel(string status) => StatusHelper.Label(status);
    public static string StatusBadge(string status)  => StatusHelper.Badge(status);

    public static string ReasonLabel(string reason) => reason switch
    {
        "no_contact"  => "Sin contacto",
        "late"        => "Retraso",
        "bad_service" => "Mal servicio",
        "other"       => "Otro",
        _             => reason
    };

    /// <summary>
    /// Determina qué acciones inline están disponibles para un job según su estado.
    /// Devuelve lista de (newStatus, label, btnClass).
    /// </summary>
    public static List<(string Status, string Label, string BtnClass)> AvailableActions(string currentStatus)
        => currentStatus switch
        {
            "Open" or "Assigned" => [
                ("InProgress", "Marcar En camino",   "btn-warning"),
                ("Cancelled",  "Cancelar",            "btn-outline-danger")
            ],
            "InProgress" => [
                ("Completed",  "Marcar Completada",  "btn-success"),
                ("Cancelled",  "Cancelar",           "btn-outline-danger")
            ],
            _ => []
        };
}
