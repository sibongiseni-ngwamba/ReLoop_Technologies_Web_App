using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReLoop_Technologies_Web_App.Data;
using ReLoop_Technologies_Web_App.Services;

namespace ReLoop_Technologies_Web_App.Tests;

public sealed class ReLoopWebApplicationFactory : WebApplicationFactory<Program>
{
    public InMemoryReLoopDocumentStore Store { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IReLoopDocumentStore>();
            services.AddSingleton<IReLoopDocumentStore>(Store);
            services.RemoveAll<IFirebaseIdentityService>();
            services.AddSingleton<IFirebaseIdentityService, TestFirebaseIdentityService>();
        });
    }
}

public sealed class TestFirebaseIdentityService : IFirebaseIdentityService
{
    public Task<FirebaseIdentity?> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default) =>
        Task.FromResult<FirebaseIdentity?>(idToken switch
        {
            "member-token" => new("member-uid", "alex@example.com", "Alex Rivera"),
            "admin-token" => new("admin-uid", "admin@reloop.co.za", "ReLoop Admin"),
            "expired-token" => null,
            "new-user-token" => new("new-user-uid", "new@example.com", "New User"),
            _ => null
        });
}
