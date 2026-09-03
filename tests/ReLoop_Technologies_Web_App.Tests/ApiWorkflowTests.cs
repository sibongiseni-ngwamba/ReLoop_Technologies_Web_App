using System.Net;
using System.Net.Http.Json;

namespace ReLoop_Technologies_Web_App.Tests;

public sealed class ApiWorkflowTests(ReLoopWebApplicationFactory factory) : IClassFixture<ReLoopWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Dashboard_api_returns_seeded_metrics()
    {
        var response = await _client.GetAsync("/api/dashboard");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Reward Points", body);
        Assert.Contains("Alex Rivera", body);
    }

    [Fact]
    public async Task Login_validates_required_fields()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email = "bad-email", password = "" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("errors", body);
    }

    [Fact]
    public async Task Pickup_creation_persists_to_ledger()
    {
        var create = await _client.PostAsJsonAsync("/api/pickups", new
        {
            homeAddress = "99 Circular Loop Street",
            preferredDate = "2026-09-10",
            preferredTimeWindow = "09:00 AM - 12:00 PM",
            wasteCategory = "Recyclables",
            notes = "Ring the bell"
        });

        var ledger = await _client.GetStringAsync("/api/pickups?status=Scheduled");

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Contains("99 Circular Loop Street", ledger);
    }

    [Fact]
    public async Task Admin_stats_api_returns_operational_summary()
    {
        var response = await _client.GetAsync("/api/admin/stats");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Total Active Users", body);
        Assert.Contains("Plastic", body);
    }
}
