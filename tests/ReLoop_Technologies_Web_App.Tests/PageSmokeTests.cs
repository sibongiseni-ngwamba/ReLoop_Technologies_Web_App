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
    public async Task Main_pages_render_successfully(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ReLoop", await response.Content.ReadAsStringAsync());
    }
}
