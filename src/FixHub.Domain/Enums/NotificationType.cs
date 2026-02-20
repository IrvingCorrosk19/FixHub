namespace FixHub.Domain.Enums;

/// <summary>
/// FASE 10: Tipos de notificación interna.
/// </summary>
public enum NotificationType
{
    JobCreated = 1,
    JobAssigned = 2,
    JobStarted = 3,
    JobCompleted = 4,
    JobCancelled = 5,
    IssueReported = 6,
    SlaAlert = 7  // FASE 11: Alerta SLA para Admin
}
