namespace FixHub.Domain.Enums;

/// <summary>
/// FASE 11: Tipos de alerta SLA.
/// </summary>
public enum JobAlertType
{
    OpenTooLong = 1,        // Open > 15 min
    AssignedNotStarted = 2, // Assigned > 30 min sin StartedAt
    InProgressTooLong = 3,  // InProgress > 3 horas
    IssueUnresolved = 4     // Issue > 1 hora sin resolución
}
