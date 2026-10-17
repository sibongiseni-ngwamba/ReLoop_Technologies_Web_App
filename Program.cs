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
builder.Services.AddRazorPages(options =>
{
    foreach (var page in new[] { "/Dashboard", "/ScanWaste", "/SchedulePickup", "/MyPickups", "/Profile", "/Logout" }) options.Conventions.AuthorizePage(page);
    options.Conventions.AuthorizePage("/Admin/Index", "AdminOnly");
});
builder.Services.AddAuthorization(options => options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api")) { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; }
            context.Response.Redirect(context.RedirectUri); return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api")) { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; }
            context.Response.Redirect(context.RedirectUri); return Task.CompletedTask;
        };
        options.Events.OnValidatePrincipal = async context =>
        {
            var store = context.HttpContext.RequestServices.GetRequiredService<ReLoopStore>();
            var email = context.Principal?.FindFirstValue(ClaimTypes.Email);
            var role = context.Principal?.FindFirstValue(ClaimTypes.Role);
            if (!await store.IsSessionValidAsync(email, role))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }
        };
    });
builder.Services.AddDbContext<ReLoopDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ReLoopDatabase")));
builder.Services.AddScoped<ReLoopStore>();
builder.Services.AddScoped<AdminManagementService>();

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

    var user = await store.AuthenticateAsync(request.Email, request.Password);
    if (user is null) return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password");
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

    var user = await store.CreateUserAsync(request.FullName, request.Email, request.Password);
    if (user is null) return Results.Conflict(new { title = "An account already exists for this email.", errors = new Dictionary<string, string[]> { ["email"] = ["Use another email or sign in instead."] } });
    await SignInUserAsync(httpContext, user.FullName, user.Email, user.Role, true);
    return Results.Created("/Dashboard", new { user = user.FullName, role = user.Role });
}).WithTags("Authentication");

app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok(new { message = "Signed out" });
}).WithTags("Authentication");
app.MapGet("/api/auth/session", (HttpContext context) => Results.Ok(new { authenticated = context.User.Identity?.IsAuthenticated == true, name = context.User.Identity?.Name, role = context.User.FindFirstValue(ClaimTypes.Role) })).WithTags("Authentication");

app.MapGet("/api/dashboard", async (ReLoopStore store, HttpContext context) => Results.Ok(await store.GetDashboardAsync(context.User.FindFirstValue(ClaimTypes.Email)))).RequireAuthorization().WithTags("Dashboard");
app.MapGet("/api/pickups", async (ReLoopStore store, HttpContext context, string? status) => Results.Ok(await store.GetPickupsAsync(context.User.FindFirstValue(ClaimTypes.Email), status))).RequireAuthorization().WithTags("Pickups");
app.MapPost("/api/pickups", async (CreatePickupRequest request, ReLoopStore store, HttpContext context) =>
{
    var errors = Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    return Results.Created("/api/pickups", await store.CreatePickupAsync(context.User.FindFirstValue(ClaimTypes.Email), request));
}).RequireAuthorization().WithTags("Pickups");

app.MapPost("/api/scan/classify", async (HttpRequest request, ReLoopStore store, HttpContext context) =>
{
    var fileName = request.Form.Files.FirstOrDefault()?.FileName ?? "plastic-bottle.jpg";
    return Results.Ok(await store.ClassifyScanAsync(context.User.FindFirstValue(ClaimTypes.Email), fileName));
}).RequireAuthorization().DisableAntiforgery().WithTags("Scan");

app.MapGet("/api/admin/stats", async (ReLoopStore store) => Results.Ok(await store.GetAdminStatsAsync())).RequireAuthorization("AdminOnly").WithTags("Admin");
app.MapGet("/api/admin/overview", async (AdminManagementService service) => Results.Ok(await service.GetOverviewAsync())).RequireAuthorization("AdminOnly");
app.MapGet("/api/admin/users", async (AdminManagementService service, string? search, int page = 1, int pageSize = 10) => Results.Ok(await service.GetUsersAsync(search, page, pageSize))).RequireAuthorization("AdminOnly");
app.MapPut("/api/admin/users/{id:guid}", async (Guid id, AdminUserUpdateRequest request, AdminManagementService service) =>
{
    var errors = Validate(request); if (errors.Count > 0) return Results.ValidationProblem(errors);
    var user = await service.UpdateUserAsync(id, request); return user is null ? Results.NotFound() : Results.Ok(user);
}).RequireAuthorization("AdminOnly");
app.MapGet("/api/admin/pickups", async (AdminManagementService service, string? status, string? search, int page = 1, int pageSize = 10) => Results.Ok(await service.GetPickupsAsync(status, search, page, pageSize))).RequireAuthorization("AdminOnly");
app.MapPut("/api/admin/pickups/{id:guid}", async (Guid id, AdminPickupUpdateRequest request, AdminManagementService service) =>
{
    var errors = Validate(request); if (errors.Count > 0) return Results.ValidationProblem(errors);
    var pickup = await service.UpdatePickupAsync(id, request); return pickup is null ? Results.NotFound() : Results.Ok(pickup);
}).RequireAuthorization("AdminOnly");
app.MapGet("/api/admin/activity", async (AdminManagementService service, int page = 1, int pageSize = 10) => Results.Ok(await service.GetActivityAsync(page, pageSize))).RequireAuthorization("AdminOnly");
app.MapGet("/api/admin/rewards", async (AdminManagementService service) => Results.Ok(await service.GetRewardsAsync())).RequireAuthorization("AdminOnly");
app.MapPost("/api/admin/rewards", async (RewardCatalogueRequest request, AdminManagementService service) => { var errors = Validate(request); return errors.Count > 0 ? Results.ValidationProblem(errors) : Results.Created("/api/admin/rewards", await service.CreateRewardAsync(request)); }).RequireAuthorization("AdminOnly");
app.MapPut("/api/admin/rewards/{id:guid}", async (Guid id, RewardCatalogueRequest request, AdminManagementService service) => { var errors = Validate(request); if (errors.Count > 0) return Results.ValidationProblem(errors); var reward = await service.UpdateRewardAsync(id, request); return reward is null ? Results.NotFound() : Results.Ok(reward); }).RequireAuthorization("AdminOnly");
app.MapGet("/api/admin/redemptions", async (AdminManagementService service, int page = 1, int pageSize = 10) => Results.Ok(await service.GetRedemptionsAsync(page, pageSize))).RequireAuthorization("AdminOnly");
app.MapPut("/api/admin/redemptions/{id:guid}", async (Guid id, RedemptionUpdateRequest request, AdminManagementService service) => { var errors = Validate(request); if (errors.Count > 0) return Results.ValidationProblem(errors); var redemption = await service.UpdateRedemptionAsync(id, request); return redemption is null ? Results.NotFound() : Results.Ok(redemption); }).RequireAuthorization("AdminOnly");
app.MapGet("/api/admin/reports/system.csv", async (AdminManagementService service) => Results.File(System.Text.Encoding.UTF8.GetBytes(await service.BuildReportAsync()), "text/csv", "reloop-system-report.csv")).RequireAuthorization("AdminOnly");
app.MapGet("/api/rewards", () => Results.Ok(new { points = 1200, tier = "Circular Citizen", nextRewardAt = 1500 })).RequireAuthorization().WithTags("Rewards");
app.MapGet("/api/profile", async (HttpContext httpContext, ReLoopStore store) =>
    Results.Ok(await store.GetProfileAsync(httpContext.User.FindFirstValue(ClaimTypes.Email)))).RequireAuthorization().WithTags("Profile");
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
}).RequireAuthorization().WithTags("Profile");
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
