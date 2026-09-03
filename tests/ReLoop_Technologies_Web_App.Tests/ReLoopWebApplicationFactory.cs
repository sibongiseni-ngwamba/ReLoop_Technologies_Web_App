using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using ReLoop_Technologies_Web_App.Data;

namespace ReLoop_Technologies_Web_App.Tests;

public sealed class ReLoopWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptors = services
                .Where(service => service.ServiceType == typeof(DbContextOptions<ReLoopDbContext>))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ReLoopDbContext>(options =>
                options.UseInMemoryDatabase("reloop-tests", _databaseRoot));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<ReLoopDbContext>().Database.EnsureCreated();
        });
    }
}
