using System.Net;

namespace ReLoop_Technologies_Web_App.Tests;

public sealed class PageSmokeTests(ReLoopWebApplicationFactory factory) : IClassFixture<ReLoopWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/")]
    [InlineData("/Login")]
    [InlineData("/SignUp")]
    [InlineData("/Dashboard")]
    [InlineData("/ScanWaste")]
    [InlineData("/SchedulePickup")]
    [InlineData("/MyPickups")]
    [InlineData("/Admin")]
    [InlineData("/Profile")]
    [InlineData("/Logout")]
    [InlineData("/About")]
    [InlineData("/Contact")]
    [InlineData("/Services")]
    [InlineData("/Recycling")]
    [InlineData("/Privacy")]
    public async Task Main_pages_render_successfully(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ReLoop", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Admin_console_renders_role_managed_tools_for_mobile_collections()
    {
        var response = await _client.GetAsync("/Admin");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("User Management", html);
        Assert.Contains("Pickup Management", html);
        Assert.Contains("Rewards Catalogue", html);
        Assert.Contains("Reward Claims", html);
        Assert.Contains("Create claim", html);
        Assert.Contains("Waste Scan Log", html);
        Assert.Contains("data-admin-create=\"reward\"", html);
        Assert.Contains("data-admin-users", html);
    }
}
