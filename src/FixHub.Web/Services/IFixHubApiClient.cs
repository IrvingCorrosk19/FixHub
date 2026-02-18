namespace FixHub.Web.Services;

/// <summary>
/// Abstracción del cliente HTTP hacia FixHub.API.
/// La Web NO llama directo a Application/Infrastructure.
/// El token Bearer se inyecta automáticamente via BearerTokenHandler (DelegatingHandler).
/// </summary>
public interface IFixHubApiClient
{
    Task<HealthResponse?> GetHealthAsync();

    // Auth (no token needed)
    Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request);

    // Jobs
    Task<ApiResult<JobDto>> CreateJobAsync(CreateJobRequest request);
    Task<ApiResult<JobDto>> GetJobAsync(Guid jobId);
    Task<ApiResult<PagedResult<JobDto>>> ListJobsAsync(int page, int pageSize, int? status = null, int? categoryId = null);
    Task<ApiResult<PagedResult<JobDto>>> ListMyJobsAsync(int page, int pageSize);
    Task<ApiResult<JobDto>> CompleteJobAsync(Guid jobId);

    // Proposals
    Task<ApiResult<ProposalDto>> SubmitProposalAsync(Guid jobId, SubmitProposalRequest request);
    Task<ApiResult<List<ProposalDto>>> GetJobProposalsAsync(Guid jobId);
    Task<ApiResult<AcceptProposalResponse>> AcceptProposalAsync(Guid proposalId);

    // Reviews
    Task<ApiResult<ReviewDto>> CreateReviewAsync(CreateReviewRequest request);

    // Technicians
    Task<ApiResult<TechnicianProfileDto>> GetTechnicianProfileAsync(Guid userId);
    Task<ApiResult<PagedResult<AssignmentDto>>> GetMyAssignmentsAsync(int page, int pageSize);

    // Admin (postulantes)
    Task<ApiResult<PagedResult<ApplicantDto>>> ListApplicantsAsync(int page, int pageSize, string? status = null);
    Task<ApiResult<object>> UpdateTechnicianStatusAsync(Guid technicianId, int status);
}

public record HealthResponse(string Status, string Version, DateTime Timestamp);

/// <summary>Result wrapper que distingue éxito de error sin lanzar excepciones.</summary>
public record ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? ErrorMessage { get; init; }
    public int StatusCode { get; init; }

    public static ApiResult<T> Success(T value, int statusCode = 200) =>
        new() { IsSuccess = true, Value = value, StatusCode = statusCode };

    public static ApiResult<T> Failure(string error, int statusCode) =>
        new() { IsSuccess = false, ErrorMessage = error, StatusCode = statusCode };
}
