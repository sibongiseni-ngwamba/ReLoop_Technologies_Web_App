using System.ComponentModel.DataAnnotations;
using ReLoop_Technologies_Web_App.Models;
using ReLoop_Technologies_Web_App.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<ReLoopStore>();

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

app.MapPost("/api/auth/login", (AuthRequest request) =>
{
    var errors = Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    return Results.Ok(new { user = "Alex Rivera", role = request.Email.Contains("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Member" });
}).WithTags("Authentication");

app.MapPost("/api/auth/signup", (SignUpRequest request) =>
{
    var errors = Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    return Results.Created("/Dashboard", new { user = request.FullName, role = "Member" });
}).WithTags("Authentication");

app.MapGet("/api/dashboard", (ReLoopStore store) => Results.Ok(store.GetDashboard())).WithTags("Dashboard");
app.MapGet("/api/pickups", (ReLoopStore store, string? status) => Results.Ok(store.GetPickups(status))).WithTags("Pickups");
app.MapPost("/api/pickups", (CreatePickupRequest request, ReLoopStore store) =>
{
    var errors = Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    return Results.Created("/api/pickups", store.CreatePickup(request));
}).WithTags("Pickups");

app.MapPost("/api/scan/classify", (HttpRequest request, ReLoopStore store) =>
{
    var fileName = request.Form.Files.FirstOrDefault()?.FileName ?? "plastic-bottle.jpg";
    return Results.Ok(store.ClassifyScan(fileName));
}).DisableAntiforgery().WithTags("Scan");

app.MapGet("/api/admin/stats", (ReLoopStore store) => Results.Ok(store.GetAdminStats())).WithTags("Admin");
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
