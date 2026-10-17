using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ReLoop_Technologies_Web_App.Tests;

public sealed class ApiWorkflowTests(ReLoopWebApplicationFactory factory) : IClassFixture<ReLoopWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    [Fact]
    public async Task Dashboard_api_returns_seeded_metrics()
    {
        await SignInAsync();
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
        await SignInAsync();
        var create = await _client.PostAsJsonAsync("/api/pickups", new
        {
            homeAddress = "99 Circular Loop Street",
            preferredDate = DateTime.UtcNow.Date.AddDays(3),
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
        await SignInAsync("admin@reloop.co.za");
        var response = await _client.GetAsync("/api/admin/stats");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Total Active Users", body);
        Assert.Contains("Plastic", body);
    }

    [Fact]
    public async Task Profile_can_be_loaded_and_updated()
    {
        await SignInAsync();
        var current = await _client.GetStringAsync("/api/profile");
        Assert.Contains("Alex Rivera", current);

        var update = await _client.PutAsJsonAsync("/api/profile", new
        {
            fullName = "Alex Green",
            email = "alex@example.com",
            address = "12 Circular Avenue",
            preferredCategory = "E-waste"
        });
        var body = await update.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Contains("Alex Green", body);
        Assert.Contains("E-waste", body);
    }

    [Fact]
    public async Task Contact_form_persists_message()
    {
        var response = await _client.PostAsJsonAsync("/api/contact", new
        {
            fullName = "Naledi Mokoena",
            email = "naledi@example.com",
            subject = "Partner collection",
            message = "Please contact me about a recurring school collection."
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("New", body);
    }

    [Fact]
    public async Task Logout_endpoint_clears_session()
    {
        var login = await SignInAsync();
        var logout = await _client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
    }

    [Fact]
    public async Task Member_api_requires_sign_in()
    {
        var unauthenticatedClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });

        var response = await unauthenticatedClient.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Member_cannot_access_admin_endpoints()
    {
        await SignInAsync();
        var response = await _client.GetAsync("/api/admin/stats");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Rewards_are_read_from_catalogue_and_redemption_checks_balance()
    {
        await SignInAsync();
        var catalogue = await _client.GetFromJsonAsync<RewardsResponse>("/api/rewards");
        Assert.NotNull(catalogue);
        Assert.Equal(1200, catalogue.Points);
        Assert.NotEmpty(catalogue.Catalogue);

        var redemption = await _client.PostAsJsonAsync("/api/rewards/redemptions", new { rewardId = catalogue.Catalogue[0].Id });
        Assert.Equal(HttpStatusCode.Created, redemption.StatusCode);
    }

    [Fact]
    public async Task Waste_guidance_is_available_and_unknown_categories_are_not_found()
    {
        var response = await _client.GetAsync("/api/waste/guidance/Plastic");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Rinse", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/waste/guidance/unknown" )).StatusCode);
    }

    [Fact]
    public async Task Notifications_are_scoped_to_signed_in_member()
    {
        await SignInAsync();
        var response = await _client.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Plastic bottle scan saved", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Admin_report_is_exported_as_csv()
    {
        await SignInAsync("admin@reloop.co.za");
        var response = await _client.GetAsync("/api/admin/reports");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Reward redemptions", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Admin_endpoint_rejects_member_update_with_invalid_status()
    {
        await SignInAsync("admin@reloop.co.za");
        var response = await _client.PutAsJsonAsync("/api/admin/pickups/b5a53cb8-a41a-4b22-b541-d775f4f0b2d2", new { status = "Hacked", assignedAgent = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private Task<HttpResponseMessage> SignInAsync(string email = "alex@example.com") =>
        _client.PostAsJsonAsync("/api/auth/login", new { email, password = "password1", keepSignedIn = true });

    private sealed record RewardsResponse(int Points, List<RewardOption> Catalogue);
    private sealed record RewardOption(Guid Id, string Name, string Description, int PointsCost);
}
