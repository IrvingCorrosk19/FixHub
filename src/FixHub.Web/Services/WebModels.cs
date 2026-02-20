namespace FixHub.Web.Services;

// ─── Auth ─────────────────────────────────────────────────────────────────────

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    int Role,           // 1=Customer, 2=Technician
    string? Phone = null);

public record LoginRequest(string Email, string Password);

public record AuthResponse(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    string Token);

// ─── Jobs ─────────────────────────────────────────────────────────────────────

public record JobDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    int CategoryId,
    string CategoryName,
    string Title,
    string Description,
    string AddressText,
    decimal? Lat,
    decimal? Lng,
    string Status,
    decimal? BudgetMin,
    decimal? BudgetMax,
    DateTime CreatedAt,
    Guid? AssignedTechnicianId = null,
    string? AssignedTechnicianName = null,
    DateTime? AssignedAt = null,
    DateTime? StartedAt = null,
    DateTime? CompletedAt = null,
    DateTime? CancelledAt = null);

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);

public record CreateJobRequest(
    int CategoryId,
    string Title,
    string Description,
    string AddressText,
    decimal? Lat = null,
    decimal? Lng = null,
    decimal? BudgetMin = null,
    decimal? BudgetMax = null);

// ─── Proposals ────────────────────────────────────────────────────────────────

public record ProposalDto(
    Guid Id,
    Guid JobId,
    Guid TechnicianId,
    string TechnicianName,
    decimal Price,
    string? Message,
    string Status,
    DateTime CreatedAt);

// EstimatedDays shown in UI only; API accepts Price + Message (cover letter)
public record SubmitProposalRequest(decimal Price, string? Message);

public record AcceptProposalResponse(
    Guid AssignmentId,
    Guid JobId,
    Guid ProposalId,
    Guid TechnicianId,
    string TechnicianName,
    decimal AcceptedPrice,
    DateTime AcceptedAt);

// ─── Reviews ──────────────────────────────────────────────────────────────────

public record ReviewDto(
    Guid Id,
    Guid JobId,
    Guid TechnicianId,
    string TechnicianName,
    int Stars,
    string? Comment,
    DateTime CreatedAt);

public record CreateReviewRequest(
    Guid JobId,
    Guid RevieweeId,
    int Stars,
    string? Comment);

// ─── Technician Profile ───────────────────────────────────────────────────────

public record TechnicianProfileDto(
    Guid UserId,
    string FullName,
    string Email,
    string? Phone,
    string? Bio,
    int ServiceRadiusKm,
    bool IsVerified,
    decimal AvgRating,
    int CompletedJobs,
    decimal CancelRate,
    string? Status = null);

// ─── AI Scoring ───────────────────────────────────────────────────────────────

public record TechnicianRankDto(
    Guid TechnicianId,
    string FullName,
    double Score,
    decimal AvgRating,
    int CompletedJobs,
    decimal CancelRate,
    bool IsVerified);

// ─── Assignments (Technician view) ────────────────────────────────────────────

public record AssignmentDto(
    Guid AssignmentId,
    Guid JobId,
    string JobTitle,
    string CategoryName,
    string CustomerName,
    string AddressText,
    decimal AcceptedPrice,
    string JobStatus,
    DateTime AcceptedAt,
    DateTime? CompletedAt);

// ─── Admin (postulantes técnicos) ─────────────────────────────────────────────

public record ApplicantDto(
    Guid UserId,
    string FullName,
    string Email,
    string? Phone,
    string Status,
    DateTime CreatedAt);

// ─── Job Issues (incidencias) ─────────────────────────────────────────────────

public record IssueDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string ReportedByName,
    string Reason,
    string? Detail,
    DateTime CreatedAt);

public record ReportIssueRequest(string Reason, string? Detail);

// ─── Admin Dashboard ──────────────────────────────────────────────────────────

public record DashboardKpisDto(
    int TotalToday,
    int OpenToday,
    int AssignedToday,
    int InProgressToday,
    int CompletedToday,
    int CancelledToday,
    int IssuesLast24h,
    int? AvgAssignmentTimeMinutes,
    int? AvgCompletionTimeMinutes,
    decimal? CancellationRateToday);

public record DashboardAlertJobDto(
    Guid JobId,
    string Title,
    string CustomerName,
    string Status,
    DateTime CreatedAt,
    int ElapsedMinutes,
    string AlertType,
    string Severity);

public record DashboardRecentJobDto(
    Guid JobId,
    string Title,
    string CustomerName,
    string CategoryName,
    string Status,
    DateTime CreatedAt);

public record OpsDashboardDto(
    DashboardKpisDto Kpis,
    List<DashboardAlertJobDto> Alerts,
    List<DashboardRecentJobDto> RecentJobs,
    List<IssueDto> RecentIssues);

// ─── Notifications ────────────────────────────────────────────────────────────

public record NotificationDto(
    Guid Id,
    Guid? JobId,
    string Type,
    string Message,
    bool IsRead,
    DateTime CreatedAt);

// ─── Errors ───────────────────────────────────────────────────────────────────

public record ApiErrorResponse(string Title, int Status, string? ErrorCode);
