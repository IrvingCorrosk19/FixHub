namespace FixHub.Web.Helpers;

/// <summary>
/// FASE 9: Mensajes claros para errores HTTP (403, 404, 409).
/// </summary>
public static class ErrorMessageHelper
{
    /// <summary>
    /// Obtiene un mensaje amigable para el usuario según el código de estado.
    /// Si el mensaje de la API es suficientemente claro, se usa; si no, se aplica un fallback.
    /// </summary>
    public static string GetUserFriendlyMessage(string? apiMessage, int statusCode)
    {
        if (!string.IsNullOrWhiteSpace(apiMessage) && apiMessage.Length < 150)
            return apiMessage;

        return statusCode switch
        {
            0 => apiMessage ?? "No se pudo conectar con el servidor. Comprueba la conexión e inténtalo de nuevo.",
            400 => apiMessage ?? "Los datos enviados no son válidos.",
            401 => "Sesión expirada o credenciales incorrectas. Vuelve a iniciar sesión.",
            403 => "No tienes permiso para realizar esta acción.",
            404 => "El recurso solicitado no existe o fue eliminado.",
            409 => "La operación no puede completarse porque hay un conflicto (por ejemplo, el recurso ya fue modificado).",
            500 => apiMessage ?? "Error temporal del servidor. Inténtalo de nuevo en unos minutos.",
            _ => apiMessage ?? "Ha ocurrido un error. Por favor, inténtalo de nuevo."
        };
    }
}
