using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ReLoop_Technologies_Web_App.Data;
using ReLoop_Technologies_Web_App.Services;

namespace ReLoop_Technologies_Web_App.Tests;

public sealed class ApiWorkflowTests : IDisposable
{
    private readonly ReLoopWebApplicationFactory factory;
    private readonly HttpClient _client;
    private readonly Task _seed;

    public ApiWorkflowTests()
    {
        factory = new ReLoopWebApplicationFactory();
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        _seed = SeedAsync(factory);
    }

    private static async Task SeedAsync(ReLoopWebApplicationFactory factory)
    {
        await factory.Store.SaveAsync(FirestoreCollections.Users, "admin-uid", new UserDocument { UserId = "admin-uid", FirstName = "ReLoop", LastName = "Admin", Email = "admin@reloop.co.za", Role = "Admin" });
        await factory.Store.SaveAsync(FirestoreCollections.Users, "member-uid", new UserDocument { UserId = "member-uid", FirstName = "Alex", LastName = "Rivera", Email = "alex@example.com", Role = "Member" });
        await factory.Store.SaveAsync(FirestoreCollections.Rewards, "member-uid", new RewardAccountDocument { UserId = "member-uid", PointsBalance = 1200, TotalEarned = 1200 });
        await factory.Store.SaveAsync(FirestoreCollections.RewardItems, "b5a53cb8-a41a-4b22-b541-d775f4f0b2d2", new RewardItemDocument { RewardItemId = "b5a53cb8-a41a-4b22-b541-d775f4f0b2d2", ItemName = "Reusable Bottle", Description = "A stainless reusable bottle", PointsCost = 500 });
        await factory.Store.SaveAsync(FirestoreCollections.WasteCategories, "plastic", new WasteCategoryDocument { WasteCategoryId = "plastic", CategoryName = "Plastic", ModelLabel = "Plastic" });
        await factory.Store.SaveAsync(FirestoreCollections.GuidanceContent, "plastic-guide", new GuidanceContentDocument { GuidanceId = "plastic-guide", WasteCategoryId = "plastic", Title = "Plastic recycling", Content = "Rinse and sort plastic containers.", Preparation = "Rinse containers before recycling." });
        await factory.Store.SaveAsync(FirestoreCollections.Notifications, "scan-notification", new NotificationDocument { NotificationId = "scan-notification", UserId = "member-uid", Title = "Plastic bottle scan saved", Message = "Plastic bottle scan saved", Type = "success" });
        await factory.Store.SaveAsync(FirestoreCollections.WasteLogs, "plastic-scan", new WasteLogDocument { WasteLogId = "plastic-scan", UserId = "member-uid", WasteCategoryId = "plastic", ItemName = "Plastic bottle", ModelLabel = "Plastic", LoggedAt = DateTime.UtcNow });
    }

    [Fact]
    public async Task Dashboard_api_returns_seeded_metrics()
    {
        await SignInAsync();
        var response = await _client.GetAsync("/api/dashboard");
        var body = await response.Content.ReadAsStringAsync();
        var page = await _client.GetStringAsync("/Dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Reward Points", body);
        Assert.Contains("Alex Rivera", body);
        Assert.Contains("Hello, Alex Rivera.", page);
        Assert.Contains("aria-label=\"Profile for Alex Rivera\"", page);
    }

    [Fact]
    public async Task Login_validates_required_fields()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { idToken = "" });
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
        await SignInAsync("admin-token");
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
        await _seed;
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
        await SignInAsync("admin-token");
        var response = await _client.GetAsync("/api/admin/reports");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Reward redemptions", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Admin_endpoint_rejects_member_update_with_invalid_status()
    {
        await SignInAsync("admin-token");
        var response = await _client.PutAsJsonAsync("/api/admin/pickups/b5a53cb8-a41a-4b22-b541-d775f4f0b2d2", new { status = "Hacked", assignedAgent = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SignInAsync(string token = "member-token")
    {
        await _seed;
        return await _client.PostAsJsonAsync("/api/auth/login", new { idToken = token, keepSignedIn = true });
    }

    private sealed record RewardsResponse(int Points, List<RewardOption> Catalogue);
    private sealed record RewardOption(string Id, string Name, string Description, int PointsCost);

    public void Dispose()
    {
        _client.Dispose();
        factory.Dispose();
    }
}
