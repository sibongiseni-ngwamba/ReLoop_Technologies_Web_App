using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
    });
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

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapPost("/api/auth/login", async (AuthRequest request, ReLoopStore store, HttpContext httpContext) =>
{
    var errors = Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var user = await store.FindOrCreateUserAsync("Alex Rivera", request.Email, request.Password);
    await SignInUserAsync(httpContext, user.FullName, user.Email, user.Role, request.KeepSignedIn);
    return Results.Ok(new { user = user.FullName, role = user.Role });
}).WithTags("Authentication");

app.MapPost("/api/auth/signup", async (SignUpRequest request, ReLoopStore store, HttpContext httpContext) =>
{
    var errors = Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var user = await store.FindOrCreateUserAsync(request.FullName, request.Email, request.Password);
    await SignInUserAsync(httpContext, user.FullName, user.Email, user.Role, true);
    return Results.Created("/Dashboard", new { user = user.FullName, role = user.Role });
}).WithTags("Authentication");

app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok(new { message = "Signed out" });
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
app.MapGet("/api/profile", async (HttpContext httpContext, ReLoopStore store) =>
    Results.Ok(await store.GetProfileAsync(httpContext.User.FindFirstValue(ClaimTypes.Email)))).WithTags("Profile");
app.MapPut("/api/profile", async (ProfileUpdateRequest request, HttpContext httpContext, ReLoopStore store) =>
{
    var errors = Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var profile = await store.UpdateProfileAsync(httpContext.User.FindFirstValue(ClaimTypes.Email), request);
    await SignInUserAsync(httpContext, profile.FullName, profile.Email, profile.Role, true);
    return Results.Ok(profile);
}).WithTags("Profile");
app.MapPost("/api/contact", async (ContactRequest request, ReLoopStore store) =>
{
    var errors = Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var message = await store.CreateContactMessageAsync(request);
    return Results.Created("/Contact", new { message.Id, message.Status });
}).WithTags("Contact");

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

static async Task SignInUserAsync(HttpContext httpContext, string fullName, string email, string role, bool persistent)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, fullName),
        new(ClaimTypes.Email, email),
        new(ClaimTypes.Role, role)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity),
        new AuthenticationProperties
        {
            IsPersistent = persistent,
            ExpiresUtc = persistent ? DateTimeOffset.UtcNow.AddDays(14) : null
        });
}

public partial class Program;
