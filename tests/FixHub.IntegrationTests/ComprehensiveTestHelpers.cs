using System.Net;
using System.Net.Http.Json;
using FixHub.Domain.Enums;

namespace FixHub.IntegrationTests;

/// <summary>Helpers compartidos para la batería de pruebas funcionales/seguridad/resiliencia.</summary>
public static class ComprehensiveTestHelpers
{
    public static async Task RegisterUser(this HttpClient client, string email, string password, UserRole role)
    {
        var body = new { fullName = "User", email, password, role = (int)role, phone = (string?)null };
        var resp = await client.PostAsJsonAsync("/api/v1/auth/register", body);
        resp.EnsureSuccessStatusCode();
    }

    public static async Task<string> LoginUser(this HttpClient client, string email, string password)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        return data!.Token;
    }

    public static async Task<string> LoginAsAdmin(this HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "admin@fixhub.com", password = "Admin123!" });
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        return data!.Token;
    }

    public static async Task<Guid> CreateJob(this HttpClient client, string bearerToken, string title, string description, string addressText, decimal min, decimal max)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/jobs");
        request.Headers.Add("Authorization", "Bearer " + bearerToken);
        request.Content = JsonContent.Create(new { categoryId = 1, title, description, addressText, lat = (decimal?)null, lng = (decimal?)null, budgetMin = min, budgetMax = max });
        var resp = await client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
        var job = await resp.Content.ReadFromJsonAsync<JobDto>();
        return job!.Id;
    }

    public static async Task<Guid> SubmitProposal(this HttpClient client, string bearerToken, Guid jobId, decimal price)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/jobs/{jobId}/proposals");
        request.Headers.Add("Authorization", "Bearer " + bearerToken);
        request.Content = JsonContent.Create(new { price, message = (string?)null });
        var resp = await client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
        var proposal = await resp.Content.ReadFromJsonAsync<ProposalDto>();
        return proposal!.Id;
    }

    public static async Task<Guid> GetFirstProposalId(this HttpClient client, string adminToken, Guid jobId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/jobs/{jobId}/proposals");
        request.Headers.Add("Authorization", "Bearer " + adminToken);
        var resp = await client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
        var list = await resp.Content.ReadFromJsonAsync<List<ProposalDto>>();
        return list!.First().Id;
    }

    public static async Task AcceptProposal(this HttpClient client, string bearerToken, Guid proposalId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/proposals/{proposalId}/accept");
        request.Headers.Add("Authorization", "Bearer " + bearerToken);
        var resp = await client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
    }

    public static async Task AdminStartJob(this HttpClient client, string adminToken, Guid jobId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/jobs/{jobId}/start");
        request.Headers.Add("Authorization", "Bearer " + adminToken);
        var resp = await client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
    }

    public static async Task AdminUpdateJobStatus(this HttpClient client, string adminToken, Guid jobId, string newStatus)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/admin/jobs/{jobId}/status");
        request.Headers.Add("Authorization", "Bearer " + adminToken);
        request.Content = JsonContent.Create(new { newStatus });
        var resp = await client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
    }

    public static async Task CompleteJob(this HttpClient client, string bearerToken, Guid jobId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/jobs/{jobId}/complete");
        request.Headers.Add("Authorization", "Bearer " + bearerToken);
        var resp = await client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
    }

    public static async Task<HttpResponseMessage> CancelJob(this HttpClient client, string bearerToken, Guid jobId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/jobs/{jobId}/cancel");
        request.Headers.Add("Authorization", "Bearer " + bearerToken);
        return await client.SendAsync(request);
    }

    public static async Task<JobDetailDto?> GetJob(this HttpClient client, string bearerToken, Guid jobId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/jobs/{jobId}");
        request.Headers.Add("Authorization", "Bearer " + bearerToken);
        var resp = await client.SendAsync(request);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<JobDetailDto>();
    }

    public static async Task<HttpResponseMessage> GetJobRaw(this HttpClient client, string bearerToken, Guid jobId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/jobs/{jobId}");
        request.Headers.Add("Authorization", "Bearer " + bearerToken);
        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> ListJobs(this HttpClient client, string bearerToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/jobs?page=1&pageSize=10");
        request.Headers.Add("Authorization", "Bearer " + bearerToken);
        return await client.SendAsync(request);
    }

    public static async Task<AdminMetricsDto?> GetAdminMetrics(this HttpClient client, string adminToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/metrics");
        request.Headers.Add("Authorization", "Bearer " + adminToken);
        var resp = await client.SendAsync(request);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<AdminMetricsDto>();
    }

    public static async Task<HttpResponseMessage> GetAdminDashboard(this HttpClient client, string adminToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/dashboard");
        request.Headers.Add("Authorization", "Bearer " + adminToken);
        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> GetAdminIssues(this HttpClient client, string adminToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/issues?page=1&pageSize=10");
        request.Headers.Add("Authorization", "Bearer " + adminToken);
        return await client.SendAsync(request);
    }

    public static bool HasErrorCode(string json, string code) => json.Contains($"\"errorCode\":\"{code}\"") || json.Contains($"\"errorCode\": \"{code}\"");

    public sealed class AuthResponse { public string Token { get; set; } = ""; }
    public sealed class JobDto { public Guid Id { get; set; } }
    public sealed class ProposalDto { public Guid Id { get; set; } }
    public sealed class JobDetailDto { public Guid Id { get; set; } public string Status { get; set; } = ""; }
    public sealed class AdminMetricsDto
    {
        public int TotalEmailsSentToday { get; set; }
        public int TotalEmailsFailedToday { get; set; }
        public int TotalSlaAlertsToday { get; set; }
        public double? AvgMinutesOpenToAssigned { get; set; }
        public double? AvgMinutesAssignedToCompleted { get; set; }
    }
}
