using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FixHub.Web.Services;

/// <summary>
/// Cliente HTTP hacia FixHub.API.
/// El token Bearer se agrega automáticamente via BearerTokenHandler.
/// </summary>
public class FixHubApiClient(HttpClient http) : IFixHubApiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    // ─── Health ───────────────────────────────────────────────────────────────
    public async Task<HealthResponse?> GetHealthAsync()
    {
        try { return await http.GetFromJsonAsync<HealthResponse>("api/v1/health", JsonOpts); }
        catch { return null; }
    }

    // ─── Auth (sin token) ─────────────────────────────────────────────────────
    public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request) =>
        PostAsync<AuthResponse>("api/v1/auth/register", request);

    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request) =>
        PostAsync<AuthResponse>("api/v1/auth/login", request);

    // ─── Jobs ─────────────────────────────────────────────────────────────────
    public Task<ApiResult<JobDto>> CreateJobAsync(CreateJobRequest request) =>
        PostAsync<JobDto>("api/v1/jobs", request);

    public Task<ApiResult<JobDto>> GetJobAsync(Guid jobId) =>
        GetAsync<JobDto>($"api/v1/jobs/{jobId}");

    public Task<ApiResult<PagedResult<JobDto>>> ListJobsAsync(
        int page, int pageSize, int? status = null, int? categoryId = null)
    {
        var qs = $"api/v1/jobs?page={page}&pageSize={pageSize}";
        if (status.HasValue)     qs += $"&status={status}";
        if (categoryId.HasValue) qs += $"&categoryId={categoryId}";
        return GetAsync<PagedResult<JobDto>>(qs);
    }

    public Task<ApiResult<PagedResult<JobDto>>> ListMyJobsAsync(int page, int pageSize) =>
        GetAsync<PagedResult<JobDto>>($"api/v1/jobs/mine?page={page}&pageSize={pageSize}");

    public Task<ApiResult<JobDto>> CompleteJobAsync(Guid jobId) =>
        PostAsync<JobDto>($"api/v1/jobs/{jobId}/complete", null);

    // ─── Proposals ────────────────────────────────────────────────────────────
    public Task<ApiResult<ProposalDto>> SubmitProposalAsync(Guid jobId, SubmitProposalRequest request) =>
        PostAsync<ProposalDto>($"api/v1/jobs/{jobId}/proposals", request);

    public Task<ApiResult<List<ProposalDto>>> GetJobProposalsAsync(Guid jobId) =>
        GetAsync<List<ProposalDto>>($"api/v1/jobs/{jobId}/proposals");

    public Task<ApiResult<AcceptProposalResponse>> AcceptProposalAsync(Guid proposalId) =>
        PostAsync<AcceptProposalResponse>($"api/v1/proposals/{proposalId}/accept", null);

    // ─── Reviews ──────────────────────────────────────────────────────────────
    public Task<ApiResult<ReviewDto>> CreateReviewAsync(CreateReviewRequest request) =>
        PostAsync<ReviewDto>("api/v1/reviews", request);

    // ─── Technicians ──────────────────────────────────────────────────────────
    public Task<ApiResult<TechnicianProfileDto>> GetTechnicianProfileAsync(Guid userId) =>
        GetAsync<TechnicianProfileDto>($"api/v1/technicians/{userId}/profile");

    public Task<ApiResult<PagedResult<AssignmentDto>>> GetMyAssignmentsAsync(int page, int pageSize) =>
        GetAsync<PagedResult<AssignmentDto>>($"api/v1/technicians/me/assignments?page={page}&pageSize={pageSize}");

    // ─── Admin ─────────────────────────────────────────────────────────────────
    public Task<ApiResult<PagedResult<ApplicantDto>>> ListApplicantsAsync(int page, int pageSize, string? status = null)
    {
        var qs = $"api/v1/admin/applicants?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status)) qs += $"&status={status}";
        return GetAsync<PagedResult<ApplicantDto>>(qs);
    }

    public Task<ApiResult<object>> UpdateTechnicianStatusAsync(Guid technicianId, int status) =>
        PatchAsync<object>($"api/v1/admin/technicians/{technicianId}/status", new { Status = status });

    // ─── Helpers ──────────────────────────────────────────────────────────────
    private async Task<ApiResult<T>> GetAsync<T>(string url)
    {
        try
        {
            var response = await http.GetAsync(url);
            return await ParseResponseAsync<T>(response);
        }
        catch (Exception ex) { return ApiResult<T>.Failure($"Error de red: {ex.Message}", 0); }
    }

    private async Task<ApiResult<T>> PostAsync<T>(string url, object? body = null)
    {
        try
        {
            HttpResponseMessage response;
            if (body is null)
                response = await http.PostAsync(url, null);
            else
                response = await http.PostAsJsonAsync(url, body);
            return await ParseResponseAsync<T>(response);
        }
        catch (Exception ex) { return ApiResult<T>.Failure($"Error de red: {ex.Message}", 0); }
    }

    private async Task<ApiResult<T>> PatchAsync<T>(string url, object body)
    {
        try
        {
            var response = await http.PatchAsJsonAsync(url, body);
            return await ParseResponseAsync<T>(response);
        }
        catch (Exception ex) { return ApiResult<T>.Failure($"Error de red: {ex.Message}", 0); }
    }

    private static async Task<ApiResult<T>> ParseResponseAsync<T>(HttpResponseMessage response)
    {
        var code = (int)response.StatusCode;
        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
                return ApiResult<T>.Success(default!, code);
            var value = await response.Content.ReadFromJsonAsync<T>(JsonOpts);
            return ApiResult<T>.Success(value!, code);
        }
        try
        {
            var err = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOpts);
            return ApiResult<T>.Failure(err?.Title ?? response.ReasonPhrase ?? "Error", code);
        }
        catch { return ApiResult<T>.Failure(response.ReasonPhrase ?? "Error desconocido", code); }
    }
}
