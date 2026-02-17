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

    [Fact]
    public async Task Happy_Path_Job_Proposal_Accept_Complete_Review()
    {
        var customerEmail = $"cust-{Guid.NewGuid():N}@test.local";
        var techEmail = $"tech-{Guid.NewGuid():N}@test.local";

        await Register(customerEmail, "Password1!", UserRole.Customer);
        await Register(techEmail, "Password1!", UserRole.Technician);

        var customerToken = await Login(customerEmail, "Password1!");
        var techToken = await Login(techEmail, "Password1!");

        var jobId = await CreateJob(customerToken, "Plumbing job", "Fix sink", "123 Main St", 100m, 200m);
        var proposalId = await SubmitProposal(techToken, jobId, 150m);
        await AcceptProposal(customerToken, proposalId);
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
}
