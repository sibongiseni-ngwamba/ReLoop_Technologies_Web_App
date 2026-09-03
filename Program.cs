using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ReLoop_Technologies_Web_App.Data;
using ReLoop_Technologies_Web_App.Models;
using ReLoop_Technologies_Web_App.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<ReLoopDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ReLoopDatabase")));
builder.Services.AddScoped<ReLoopStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.MapPost("/api/auth/login", async (AuthRequest request, ReLoopStore store) =>
{
    var errors = Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var user = await store.FindOrCreateUserAsync("Alex Rivera", request.Email, request.Password);
    return Results.Ok(new { user = user.FullName, role = user.Role });
}).WithTags("Authentication");

app.MapPost("/api/auth/signup", async (SignUpRequest request, ReLoopStore store) =>
{
    var errors = Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var user = await store.FindOrCreateUserAsync(request.FullName, request.Email, request.Password);
    return Results.Created("/Dashboard", new { user = user.FullName, role = user.Role });
}).WithTags("Authentication");

app.MapGet("/api/dashboard", async (ReLoopStore store) => Results.Ok(await store.GetDashboardAsync())).WithTags("Dashboard");
app.MapGet("/api/pickups", async (ReLoopStore store, string? status) => Results.Ok(await store.GetPickupsAsync(status))).WithTags("Pickups");
app.MapPost("/api/pickups", async (CreatePickupRequest request, ReLoopStore store) =>
{
    var errors = Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    return Results.Created("/api/pickups", await store.CreatePickupAsync(request));
}).WithTags("Pickups");

app.MapPost("/api/scan/classify", async (HttpRequest request, ReLoopStore store) =>
{
    var fileName = request.Form.Files.FirstOrDefault()?.FileName ?? "plastic-bottle.jpg";
    return Results.Ok(await store.ClassifyScanAsync(fileName));
}).DisableAntiforgery().WithTags("Scan");

app.MapGet("/api/admin/stats", async (ReLoopStore store) => Results.Ok(await store.GetAdminStatsAsync())).WithTags("Admin");
app.MapGet("/api/rewards", () => Results.Ok(new { points = 1200, tier = "Circular Citizen", nextRewardAt = 1500 })).WithTags("Rewards");

app.Run();

static Dictionary<string, string[]> Validate<T>(T model)
{
    var context = new ValidationContext(model!);
    var results = new List<ValidationResult>();
    Validator.TryValidateObject(model!, context, results, true);

    return results
        .GroupBy(result => result.MemberNames.FirstOrDefault() ?? string.Empty)
        .ToDictionary(group => group.Key, group => group.Select(result => result.ErrorMessage ?? "Invalid value").ToArray());
}
