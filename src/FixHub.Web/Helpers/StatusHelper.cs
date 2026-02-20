namespace FixHub.Web.Helpers;

/// <summary>
/// Labels y clases CSS de estados de Job en lenguaje cliente.
/// Centraliza la traducción de enums internos para toda la Web layer.
/// </summary>
public static class StatusHelper
{
    /// <summary>Label humano en español para mostrar al usuario.</summary>
    public static string Label(string status) => status switch
    {
        "Open"       => "Recibida",
        "Assigned"   => "Técnico asignado",
        "InProgress" => "En camino",
        "Completed"  => "Completada",
        "Cancelled"  => "Cancelada",
        _            => status
    };

    /// <summary>Clase CSS de badge para el estado.</summary>
    public static string Badge(string status) => status switch
    {
        "Open"       => "fixhub-badge-pending",     // ámbar — en espera
        "Assigned"   => "fixhub-badge-assigned",    // azul
        "InProgress" => "fixhub-badge-inprogress",  // ámbar — en curso
        "Completed"  => "fixhub-badge-completed",   // verde — éxito
        "Cancelled"  => "fixhub-badge-cancelled",   // rojo
        _            => "bg-light text-dark"
    };
}
