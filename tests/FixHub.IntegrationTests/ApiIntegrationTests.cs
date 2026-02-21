using System.Net;
using System.Net.Http.Json;
using FixHub.Domain.Enums;
using Xunit;

namespace FixHub.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<FixHubApiFixture>
{
    private readonly FixHubApiFixture _fixture;
    private HttpClient Client => _fixture.Client;

    public ApiIntegrationTests(FixHubApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Health_Returns_200()
    {
        var response = await Client.GetAsync("/api/v1/health");
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", json);
    }

    [Fact]
    public async Task Register_And_Login_Return_Token()
    {
        var email = $"user-{Guid.NewGuid():N}@test.local";
        var registerBody = new
        {
            fullName = "Test User",
            email,
            password = "Password1!",
            role = (int)UserRole.Customer,
            phone = (string?)null
        };
        var registerResp = await Client.PostAsJsonAsync("/api/v1/auth/register", registerBody);
        registerResp.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, (HttpStatusCode)registerResp.StatusCode);
        var registerData = await registerResp.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(registerData?.Token);
        Assert.NotEmpty(registerData.Token);

        var loginBody = new { email, password = "Password1!" };
        var loginResp = await Client.PostAsJsonAsync("/api/v1/auth/login", loginBody);
        loginResp.EnsureSuccessStatusCode();
        var loginData = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(loginData?.Token);
        Assert.NotEmpty(loginData.Token);
    }

    [Fact]
    public async Task Technician_Cannot_Create_Job_Returns_403()
    {
        var email = $"tech-{Guid.NewGuid():N}@test.local";
        var registerBody = new
        {
            fullName = "Tech User",
            email,
            password = "Password1!",
            role = (int)UserRole.Technician,
            phone = (string?)null
        };
        await Client.PostAsJsonAsync("/api/v1/auth/register", registerBody);

        var loginBody = new { email, password = "Password1!" };
        var loginResp = await Client.PostAsJsonAsync("/api/v1/auth/login", loginBody);
        loginResp.EnsureSuccessStatusCode();
        var loginData = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(loginData?.Token);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/jobs");
        request.Headers.Add("Authorization", "Bearer " + loginData.Token);
        request.Content = JsonContent.Create(new
        {
            categoryId = 1,
            title = "Plumbing job",
            description = "Fix sink",
            addressText = "123 Main St",
            lat = (decimal?)null,
            lng = (decimal?)null,
            budgetMin = (decimal?)100m,
            budgetMax = (decimal?)200m
        });
        var createResp = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, createResp.StatusCode);
    }

    // ─── FASE 9: Tests de autorización ────────────────────────────────────────

    [Fact]
    public async Task Customer_Cannot_View_Other_Customers_Job_Returns_403()
    {
        var cust1 = $"cust1-{Guid.NewGuid():N}@test.local";
        var cust2 = $"cust2-{Guid.NewGuid():N}@test.local";
        await Register(cust1, "Password1!", UserRole.Customer);
        await Register(cust2, "Password1!", UserRole.Customer);

        var token1 = await Login(cust1, "Password1!");
        var token2 = await Login(cust2, "Password1!");

        var jobId = await CreateJob(token1, "Job from cust1", "Description", "123 Main St", 100m, 200m);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/jobs/{jobId}");
        request.Headers.Add("Authorization", "Bearer " + token2);
        var resp = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Technician_Cannot_View_Unassigned_Job_Returns_403()
    {
        var cust = $"cust-{Guid.NewGuid():N}@test.local";
        var tech1 = $"tech1-{Guid.NewGuid():N}@test.local";
        var tech2 = $"tech2-{Guid.NewGuid():N}@test.local";
        await Register(cust, "Password1!", UserRole.Customer);
        await Register(tech1, "Password1!", UserRole.Technician);
        await Register(tech2, "Password1!", UserRole.Technician);

        var custToken = await Login(cust, "Password1!");
        var tech1Token = await Login(tech1, "Password1!");
        var tech2Token = await Login(tech2, "Password1!");

        var jobId = await CreateJob(custToken, "Job for tech1", "Description", "123 Main St", 100m, 200m);
        await SubmitProposal(tech1Token, jobId, 150m);

        var adminToken = await LoginAdmin();
        var proposalId = await GetFirstProposalId(adminToken, jobId);
        await AcceptProposal(adminToken, proposalId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/jobs/{jobId}");
        request.Headers.Add("Authorization", "Bearer " + tech2Token);
        var resp = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Customer_Cannot_Use_Get_Jobs_Returns_403()
    {
        var cust = $"cust-{Guid.NewGuid():N}@test.local";
        await Register(cust, "Password1!", UserRole.Customer);
        var token = await Login(cust, "Password1!");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/jobs?page=1&pageSize=10");
        request.Headers.Add("Authorization", "Bearer " + token);
        var resp = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task CancelJob_InvalidStatus_Returns_400_With_ErrorCode()
    {
        var cust = $"cust-{Guid.NewGuid():N}@test.local";
        await Register(cust, "Password1!", UserRole.Customer);
        var token = await Login(cust, "Password1!");

        var jobId = await CreateJob(token, "Job to complete", "Desc", "123 St", 100m, 200m);
        var adminToken = await LoginAdmin();
        await AdminUpdateJobStatus(adminToken, jobId, "InProgress");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/jobs/{jobId}/cancel");
        request.Headers.Add("Authorization", "Bearer " + token);
        request.Content = null;
        var resp = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        Assert.Contains("errorCode", json);
        Assert.Contains("INVALID_STATUS", json);
    }

    [Fact]
    public async Task ReportJobIssue_InvalidReason_Returns_400()
    {
        var cust = $"cust-{Guid.NewGuid():N}@test.local";
        await Register(cust, "Password1!", UserRole.Customer);
        var token = await Login(cust, "Password1!");
        var jobId = await CreateJob(token, "Job", "Desc", "123 St", 100m, 200m);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/jobs/{jobId}/issues");
        request.Headers.Add("Authorization", "Bearer " + token);
        request.Content = JsonContent.Create(new { reason = "invalid_reason", detail = (string?)null });
        var resp = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ─── CICLO 1: IDOR — Cancel y Complete ────────────────────────────────────

    [Fact]
    public async Task Customer_Cannot_Cancel_Other_Customers_Job_Returns_403()
    {
        var cust1 = $"cust1-{Guid.NewGuid():N}@test.local";
        var cust2 = $"cust2-{Guid.NewGuid():N}@test.local";
        await Register(cust1, "Password1!", UserRole.Customer);
        await Register(cust2, "Password1!", UserRole.Customer);

        var token1 = await Login(cust1, "Password1!");
        var token2 = await Login(cust2, "Password1!");
        var jobId = await CreateJob(token1, "Job to cancel", "Desc", "123 St", 100m, 200m);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/jobs/{jobId}/cancel");
        request.Headers.Add("Authorization", "Bearer " + token2);
        var resp = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Customer_Cannot_Complete_Other_Customers_Job_Returns_403()
    {
        var cust1 = $"cust1-{Guid.NewGuid():N}@test.local";
        var cust2 = $"cust2-{Guid.NewGuid():N}@test.local";
        await Register(cust1, "Password1!", UserRole.Customer);
        await Register(cust2, "Password1!", UserRole.Customer);

        var token1 = await Login(cust1, "Password1!");
        var token2 = await Login(cust2, "Password1!");
        var adminToken = await LoginAdmin();

        var jobId = await CreateJob(token1, "Job to complete", "Desc", "123 St", 100m, 200m);
        await AdminUpdateJobStatus(adminToken, jobId, "InProgress");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/jobs/{jobId}/complete");
        request.Headers.Add("Authorization", "Bearer " + token2);
        var resp = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ─── CICLO 3: Technician start job ────────────────────────────────────────

    [Fact]
    public async Task Technician_Can_Start_Assigned_Job_Returns_200()
    {
        var custEmail = $"cust-{Guid.NewGuid():N}@test.local";
        var techEmail = $"tech-{Guid.NewGuid():N}@test.local";
        await Register(custEmail, "Password1!", UserRole.Customer);
        await Register(techEmail, "Password1!", UserRole.Technician);

        var custToken  = await Login(custEmail, "Password1!");
        var techToken  = await Login(techEmail, "Password1!");
        var adminToken = await LoginAdmin();

        var jobId      = await CreateJob(custToken, "Electrician job", "Fix wiring", "456 Ave", 100m, 200m);
        var proposalId = await SubmitProposal(techToken, jobId, 150m);
        await AcceptProposal(adminToken, proposalId);

        // Technician starts the job
        var startReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/jobs/{jobId}/start");
        startReq.Headers.Add("Authorization", "Bearer " + techToken);
        var startResp = await Client.SendAsync(startReq);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);

        // Verify status via admin
        var getReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/jobs/{jobId}");
        getReq.Headers.Add("Authorization", "Bearer " + adminToken);
        var getResp = await Client.SendAsync(getReq);
        getResp.EnsureSuccessStatusCode();
        var json = await getResp.Content.ReadAsStringAsync();
        Assert.Contains("InProgress", json);
        Assert.Contains("startedAt", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Technician_Cannot_Start_Job_Not_Assigned_To_Them_Returns_403()
    {
        var custEmail  = $"cust-{Guid.NewGuid():N}@test.local";
        var tech1Email = $"tech1-{Guid.NewGuid():N}@test.local";
        var tech2Email = $"tech2-{Guid.NewGuid():N}@test.local";
        await Register(custEmail,  "Password1!", UserRole.Customer);
        await Register(tech1Email, "Password1!", UserRole.Technician);
        await Register(tech2Email, "Password1!", UserRole.Technician);

        var custToken  = await Login(custEmail,  "Password1!");
        var tech1Token = await Login(tech1Email, "Password1!");
        var tech2Token = await Login(tech2Email, "Password1!");
        var adminToken = await LoginAdmin();

        var jobId      = await CreateJob(custToken, "Job for tech1", "Desc", "789 Blvd", 100m, 200m);
        var proposalId = await SubmitProposal(tech1Token, jobId, 150m);
        await AcceptProposal(adminToken, proposalId);

        // tech2 tries to start job assigned to tech1
        var startReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/jobs/{jobId}/start");
        startReq.Headers.Add("Authorization", "Bearer " + tech2Token);
        var startResp = await Client.SendAsync(startReq);
        Assert.Equal(HttpStatusCode.Forbidden, startResp.StatusCode);
    }

    // ─── CICLO 3: No doble completación ───────────────────────────────────────

    [Fact]
    public async Task CompleteJob_Twice_Returns_400_InvalidStatus()
    {
        var custEmail  = $"cust-{Guid.NewGuid():N}@test.local";
        var techEmail  = $"tech-{Guid.NewGuid():N}@test.local";
        await Register(custEmail, "Password1!", UserRole.Customer);
        await Register(techEmail, "Password1!", UserRole.Technician);

        var custToken  = await Login(custEmail, "Password1!");
        var techToken  = await Login(techEmail, "Password1!");
        var adminToken = await LoginAdmin();

        var jobId      = await CreateJob(custToken, "Job for double complete", "Desc", "123 St", 100m, 200m);
        var proposalId = await SubmitProposal(techToken, jobId, 150m);
        await AcceptProposal(adminToken, proposalId);
        await AdminUpdateJobStatus(adminToken, jobId, "InProgress");
        await CompleteJob(custToken, jobId);

        // Second complete attempt should fail
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/jobs/{jobId}/complete");
        req.Headers.Add("Authorization", "Bearer " + custToken);
        var resp = await Client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ─── CICLO 4: No doble resolución de incidencia ───────────────────────────

    [Fact]
    public async Task ResolveIssue_Twice_Returns_409_Conflict()
    {
        var custEmail  = $"cust-{Guid.NewGuid():N}@test.local";
        await Register(custEmail, "Password1!", UserRole.Customer);

        var custToken  = await Login(custEmail, "Password1!");
        var adminToken = await LoginAdmin();

        var jobId   = await CreateJob(custToken, "Job with issue", "Desc", "123 St", 100m, 200m);
        var issueId = await ReportIssue(custToken, jobId, "late");

        // First resolve
        await ResolveIssue(adminToken, issueId, "Issue resolved by admin.");

        // Second resolve should return 409
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/issues/{issueId}/resolve");
        req.Headers.Add("Authorization", "Bearer " + adminToken);
        req.Content = JsonContent.Create(new { resolutionNote = "Attempted double resolve." });
        var resp = await Client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        Assert.Contains("ALREADY_RESOLVED", json);
    }

    [Fact]
    public async Task Happy_Path_Job_Proposal_Accept_Complete_Review()
    {
        var customerEmail = $"cust-{Guid.NewGuid():N}@test.local";
        var techEmail = $"tech-{Guid.NewGuid():N}@test.local";

        await Register(customerEmail, "Password1!", UserRole.Customer);
        await Register(techEmail, "Password1!", UserRole.Technician);

        var customerToken = await Login(customerEmail, "Password1!");
        var techToken = await Login(techEmail, "Password1!");
        var adminToken = await LoginAdmin();

        var jobId = await CreateJob(customerToken, "Plumbing job", "Fix sink", "123 Main St", 100m, 200m);
        var proposalId = await SubmitProposal(techToken, jobId, 150m);
        await AcceptProposal(adminToken, proposalId);
        await CompleteJob(customerToken, jobId);
        await CreateReview(customerToken, jobId, 5, "Great work!");
    }

    private async Task Register(string email, string password, UserRole role)
    {
        var body = new
        {
            fullName = "User",
            email,
            password,
            role = (int)role,
            phone = (string?)null
        };
        var resp = await Client.PostAsJsonAsync("/api/v1/auth/register", body);
        resp.EnsureSuccessStatusCode();
    }

    private async Task<string> Login(string email, string password)
    {
        var resp = await Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        return data!.Token;
    }

    private async Task<Guid> CreateJob(string bearerToken, string title, string description, string addressText, decimal min, decimal max)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/jobs");
        request.Headers.Add("Authorization", "Bearer " + bearerToken);
        request.Content = JsonContent.Create(new
        {
            categoryId = 1,
            title,
            description,
            addressText,
            lat = (decimal?)null,
            lng = (decimal?)null,
            budgetMin = min,
            budgetMax = max
        });
        var resp = await Client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
        var job = await resp.Content.ReadFromJsonAsync<JobDto>();
        return job!.Id;
    }

    private async Task<Guid> SubmitProposal(string bearerToken, Guid jobId, decimal price)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/jobs/{jobId}/proposals");
        request.Headers.Add("Authorization", "Bearer " + bearerToken);
        request.Content = JsonContent.Create(new { price, message = (string?)null });
        var resp = await Client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
        var proposal = await resp.Content.ReadFromJsonAsync<ProposalDto>();
        return proposal!.Id;
    }

    private async Task AcceptProposal(string bearerToken, Guid proposalId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/proposals/{proposalId}/accept");
        request.Headers.Add("Authorization", "Bearer " + bearerToken);
        var resp = await Client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
    }

    private async Task CompleteJob(string bearerToken, Guid jobId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/jobs/{jobId}/complete");
        request.Headers.Add("Authorization", "Bearer " + bearerToken);
        var resp = await Client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
    }

    private async Task CreateReview(string bearerToken, Guid jobId, int stars, string? comment)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reviews");
        request.Headers.Add("Authorization", "Bearer " + bearerToken);
        request.Content = JsonContent.Create(new { jobId, stars, comment });
        var resp = await Client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
    }

    private async Task<string> LoginAdmin()
    {
        var resp = await Client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "admin@fixhub.com", password = "Admin123!" });
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        return data!.Token;
    }

    private async Task<Guid> GetFirstProposalId(string adminToken, Guid jobId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/jobs/{jobId}/proposals");
        request.Headers.Add("Authorization", "Bearer " + adminToken);
        var resp = await Client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
        var list = await resp.Content.ReadFromJsonAsync<List<ProposalDto>>();
        return list!.First().Id;
    }

    private async Task AdminUpdateJobStatus(string adminToken, Guid jobId, string newStatus)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/admin/jobs/{jobId}/status");
        request.Headers.Add("Authorization", "Bearer " + adminToken);
        request.Content = JsonContent.Create(new { newStatus });
        var resp = await Client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
    }

    private async Task<Guid> ReportIssue(string bearerToken, Guid jobId, string reason)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/jobs/{jobId}/issues");
        request.Headers.Add("Authorization", "Bearer " + bearerToken);
        request.Content = JsonContent.Create(new { reason, detail = (string?)null });
        var resp = await Client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
        var issue = await resp.Content.ReadFromJsonAsync<IssueDto>();
        return issue!.Id;
    }

    private async Task ResolveIssue(string adminToken, Guid issueId, string resolutionNote)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/issues/{issueId}/resolve");
        request.Headers.Add("Authorization", "Bearer " + adminToken);
        request.Content = JsonContent.Create(new { resolutionNote });
        var resp = await Client.SendAsync(request);
        resp.EnsureSuccessStatusCode();
    }

    private sealed class AuthResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "";
        public string Token { get; set; } = "";
    }

    private sealed class JobDto
    {
        public Guid Id { get; set; }
    }

    private sealed class ProposalDto
    {
        public Guid Id { get; set; }
    }

    private sealed class IssueDto
    {
        public Guid Id { get; set; }
    }
}
