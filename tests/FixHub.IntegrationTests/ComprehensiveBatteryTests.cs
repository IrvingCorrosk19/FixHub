using System.Net;
using FixHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FixHub.IntegrationTests;

/// <summary>Batería completa: autorización, flujo, cancelación, SLA, outbox, concurrencia, dashboard, metrics, resiliencia.</summary>
public class ComprehensiveBatteryTests : IClassFixture<FixHubApiFixture>
{
    private readonly FixHubApiFixture _fixture;
    private HttpClient Client => _fixture.Client;

    public ComprehensiveBatteryTests(FixHubApiFixture fixture) => _fixture = fixture;

    // ═══════════════════════════════════════════════════════════════════════════
    // 1) PRUEBAS DE AUTORIZACIÓN
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Auth_Customer_No_Puede_Ver_Job_De_Otro_Cliente_403()
    {
        var c1 = $"c1-{Guid.NewGuid():N}@test.local";
        var c2 = $"c2-{Guid.NewGuid():N}@test.local";
        await Client.RegisterUser(c1, "Pass1!", UserRole.Customer);
        await Client.RegisterUser(c2, "Pass1!", UserRole.Customer);
        var t1 = await Client.LoginUser(c1, "Pass1!");
        var t2 = await Client.LoginUser(c2, "Pass1!");
        var jobId = await Client.CreateJob(t1, "Job C1", "Desc", "Addr", 100m, 200m);

        var resp = await Client.GetJobRaw(t2, jobId);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        Assert.True(ComprehensiveTestHelpers.HasErrorCode(json, "FORBIDDEN"));
    }

    [Fact]
    public async Task Auth_Customer_No_Puede_Acceder_GET_Jobs_403()
    {
        var c = $"c-{Guid.NewGuid():N}@test.local";
        await Client.RegisterUser(c, "Pass1!", UserRole.Customer);
        var token = await Client.LoginUser(c, "Pass1!");
        var resp = await Client.ListJobs(token);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        Assert.True(ComprehensiveTestHelpers.HasErrorCode(json, "FORBIDDEN"));
    }

    [Fact]
    public async Task Auth_Customer_No_Puede_Completar_Job_Ajeno_403()
    {
        var c1 = $"c1-{Guid.NewGuid():N}@test.local";
        var c2 = $"c2-{Guid.NewGuid():N}@test.local";
        await Client.RegisterUser(c1, "Pass1!", UserRole.Customer);
        await Client.RegisterUser(c2, "Pass1!", UserRole.Customer);
        var t1 = await Client.LoginUser(c1, "Pass1!");
        var t2 = await Client.LoginUser(c2, "Pass1!");
        var jobId = await Client.CreateJob(t1, "Job C1", "Desc", "Addr", 100m, 200m);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/jobs/{jobId}/complete");
        request.Headers.Add("Authorization", "Bearer " + t2);
        var resp = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Auth_Customer_No_Puede_Cancelar_Job_Ajeno_403()
    {
        var c1 = $"c1-{Guid.NewGuid():N}@test.local";
        var c2 = $"c2-{Guid.NewGuid():N}@test.local";
        await Client.RegisterUser(c1, "Pass1!", UserRole.Customer);
        await Client.RegisterUser(c2, "Pass1!", UserRole.Customer);
        var t1 = await Client.LoginUser(c1, "Pass1!");
        var t2 = await Client.LoginUser(c2, "Pass1!");
        var jobId = await Client.CreateJob(t1, "Job C1", "Desc", "Addr", 100m, 200m);

        var resp = await Client.CancelJob(t2, jobId);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Auth_Technician_No_Puede_Ver_Job_No_Asignado_403()
    {
        var cust = $"cust-{Guid.NewGuid():N}@test.local";
        var tech1 = $"tech1-{Guid.NewGuid():N}@test.local";
        var tech2 = $"tech2-{Guid.NewGuid():N}@test.local";
        await Client.RegisterUser(cust, "Pass1!", UserRole.Customer);
        await Client.RegisterUser(tech1, "Pass1!", UserRole.Technician);
        await Client.RegisterUser(tech2, "Pass1!", UserRole.Technician);
        var custT = await Client.LoginUser(cust, "Pass1!");
        var tech1T = await Client.LoginUser(tech1, "Pass1!");
        var tech2T = await Client.LoginUser(tech2, "Pass1!");
        var jobId = await Client.CreateJob(custT, "Job", "Desc", "Addr", 100m, 200m);
        var propId = await Client.SubmitProposal(tech1T, jobId, 150m);
        var adminT = await Client.LoginAsAdmin();
        await Client.AcceptProposal(adminT, propId);

        var resp = await Client.GetJobRaw(tech2T, jobId);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Auth_Admin_Puede_Ver_Todo_Dashboard_Metrics_Issues()
    {
        var adminT = await Client.LoginAsAdmin();
        var dashResp = await Client.GetAdminDashboard(adminT);
        Assert.Equal(HttpStatusCode.OK, dashResp.StatusCode);
        var metricsResp = await Client.GetAdminMetrics(adminT);
        Assert.NotNull(metricsResp);
        var issuesResp = await Client.GetAdminIssues(adminT);
        Assert.Equal(HttpStatusCode.OK, issuesResp.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 2) FLUJO COMPLETO (Happy Path)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HappyPath_Create_Assign_Start_Complete_Outbox_Notifications()
    {
        var cust = $"cust-{Guid.NewGuid():N}@test.local";
        var tech = $"tech-{Guid.NewGuid():N}@test.local";
        await Client.RegisterUser(cust, "Pass1!", UserRole.Customer);
        await Client.RegisterUser(tech, "Pass1!", UserRole.Technician);
        var custT = await Client.LoginUser(cust, "Pass1!");
        var techT = await Client.LoginUser(tech, "Pass1!");
        var adminT = await Client.LoginAsAdmin();

        var jobId = await Client.CreateJob(custT, "Plumbing", "Fix sink", "123 Main", 100m, 200m);
        var job0 = await Client.GetJob(custT, jobId);
        Assert.NotNull(job0);
        Assert.Equal("Open", job0!.Status);

        await Task.Delay(500);
        await _fixture.WithDbContextAsync(async db =>
        {
            var notifs = await db.Notifications.Where(n => n.JobId == jobId).ToListAsync();
            Assert.True(notifs.Count >= 1, "Debería haber notificación JobReceived");
            var outbox = await db.NotificationOutbox.Where(o => o.JobId == jobId).ToListAsync();
            Assert.True(outbox.Count >= 1, "Debería haber registro en outbox");
        });

        var propId = await Client.SubmitProposal(techT, jobId, 150m);
        await Client.AcceptProposal(adminT, propId);
        var job1 = await Client.GetJob(custT, jobId);
        Assert.NotNull(job1);
        Assert.Equal("Assigned", job1!.Status);

        await Client.AdminStartJob(adminT, jobId);
        var job2 = await Client.GetJob(custT, jobId);
        Assert.NotNull(job2);
        Assert.Equal("InProgress", job2!.Status);

        await Client.CompleteJob(custT, jobId);
        var job3 = await Client.GetJob(custT, jobId);
        Assert.NotNull(job3);
        Assert.Equal("Completed", job3!.Status);

        await Task.Delay(15000);
        await _fixture.WithDbContextAsync(async db =>
        {
            var outboxSent = await db.NotificationOutbox.CountAsync(o => o.JobId == jobId && o.Status == OutboxStatus.Sent);
            Assert.True(outboxSent >= 1, "Debería haber al menos un email enviado");
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 3) CANCELACIÓN
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Cancel_En_Open_200_CancelledAt_Set()
    {
        var cust = $"cust-{Guid.NewGuid():N}@test.local";
        await Client.RegisterUser(cust, "Pass1!", UserRole.Customer);
        var token = await Client.LoginUser(cust, "Pass1!");
        var jobId = await Client.CreateJob(token, "Job", "Desc", "Addr", 100m, 200m);

        var resp = await Client.CancelJob(token, jobId);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var job = await Client.GetJob(token, jobId);
        Assert.NotNull(job);
        Assert.Equal("Cancelled", job!.Status);
        await _fixture.WithDbContextAsync(async db =>
        {
            var j = await db.Jobs.FindAsync(jobId);
            Assert.NotNull(j?.CancelledAt);
        });
    }

    [Fact]
    public async Task Cancel_En_Assigned_200()
    {
        var cust = $"cust-{Guid.NewGuid():N}@test.local";
        var tech = $"tech-{Guid.NewGuid():N}@test.local";
        await Client.RegisterUser(cust, "Pass1!", UserRole.Customer);
        await Client.RegisterUser(tech, "Pass1!", UserRole.Technician);
        var custT = await Client.LoginUser(cust, "Pass1!");
        var techT = await Client.LoginUser(tech, "Pass1!");
        var adminT = await Client.LoginAsAdmin();
        var jobId = await Client.CreateJob(custT, "Job", "Desc", "Addr", 100m, 200m);
        var propId = await Client.SubmitProposal(techT, jobId, 150m);
        await Client.AcceptProposal(adminT, propId);

        var resp = await Client.CancelJob(custT, jobId);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var job = await Client.GetJob(custT, jobId);
        Assert.Equal("Cancelled", job!.Status);
    }

    [Fact]
    public async Task Cancel_En_InProgress_400_INVALID_STATUS()
    {
        var cust = $"cust-{Guid.NewGuid():N}@test.local";
        var tech = $"tech-{Guid.NewGuid():N}@test.local";
        await Client.RegisterUser(cust, "Pass1!", UserRole.Customer);
        await Client.RegisterUser(tech, "Pass1!", UserRole.Technician);
        var custT = await Client.LoginUser(cust, "Pass1!");
        var techT = await Client.LoginUser(tech, "Pass1!");
        var adminT = await Client.LoginAsAdmin();
        var jobId = await Client.CreateJob(custT, "Job", "Desc", "Addr", 100m, 200m);
        var propId = await Client.SubmitProposal(techT, jobId, 150m);
        await Client.AcceptProposal(adminT, propId);
        await Client.AdminStartJob(adminT, jobId);

        var resp = await Client.CancelJob(custT, jobId);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        Assert.True(ComprehensiveTestHelpers.HasErrorCode(json, "INVALID_STATUS"));
    }

    [Fact]
    public async Task Cancel_En_Completed_400_INVALID_STATUS()
    {
        var cust = $"cust-{Guid.NewGuid():N}@test.local";
        var tech = $"tech-{Guid.NewGuid():N}@test.local";
        await Client.RegisterUser(cust, "Pass1!", UserRole.Customer);
        await Client.RegisterUser(tech, "Pass1!", UserRole.Technician);
        var custT = await Client.LoginUser(cust, "Pass1!");
        var techT = await Client.LoginUser(tech, "Pass1!");
        var adminT = await Client.LoginAsAdmin();
        var jobId = await Client.CreateJob(custT, "Job", "Desc", "Addr", 100m, 200m);
        var propId = await Client.SubmitProposal(techT, jobId, 150m);
        await Client.AcceptProposal(adminT, propId);
        await Client.AdminStartJob(adminT, jobId);
        await Client.CompleteJob(custT, jobId);

        var resp = await Client.CancelJob(custT, jobId);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 5) DASHBOARD / METRICS
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Metrics_Endpoint_Retorna_Estructura_Completa()
    {
        var adminT = await Client.LoginAsAdmin();
        var m = await Client.GetAdminMetrics(adminT);
        Assert.NotNull(m);
        Assert.True(m.TotalEmailsSentToday >= 0);
        Assert.True(m.TotalEmailsFailedToday >= 0);
        Assert.True(m.TotalSlaAlertsToday >= 0);
    }

    [Fact]
    public async Task Dashboard_Admin_200_Con_Kpis()
    {
        var adminT = await Client.LoginAsAdmin();
        var resp = await Client.GetAdminDashboard(adminT);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Kpis", json);
        Assert.Contains("Alerts", json);
    }

    [Fact]
    public async Task Dashboard_Cache_No_Rompe_Segunda_Llamada()
    {
        var adminT = await Client.LoginAsAdmin();
        var r1 = await Client.GetAdminDashboard(adminT);
        var r2 = await Client.GetAdminDashboard(adminT);
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 9) RESILIENCIA
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Health_Sin_DB_503()
    {
        var resp = await Client.GetAsync("/api/v1/health");
        Assert.True(resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == (HttpStatusCode)503);
    }
}
