using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ReLoop_Technologies_Web_App.Data;
using ReLoop_Technologies_Web_App.Models;
using ReLoop_Technologies_Web_App.Services;

namespace ReLoop_Technologies_Web_App.Api;

public static class ApiEndpointMappings
{
    public static IEndpointRouteBuilder MapReLoopApi(this IEndpointRouteBuilder endpoints)
    {
        MapAuthentication(endpoints);
        MapMemberEndpoints(endpoints);
        MapAdminEndpoints(endpoints);
        return endpoints;
    }

    private static void MapAuthentication(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/firebase/config", (IConfiguration configuration) =>
        {
            var apiKey = configuration["Firebase:WebApiKey"];
            var appId = configuration["Firebase:WebAppId"];
            var authDomain = configuration["Firebase:AuthDomain"];
            var projectId = configuration["Firebase:ProjectId"];
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(authDomain) || string.IsNullOrWhiteSpace(projectId)) return Results.Problem(statusCode: 503, title: "Firebase web app configuration is not available.");
            return Results.Ok(new { apiKey, appId, projectId, authDomain });
        }).WithTags("Authentication");

        app.MapPost("/api/auth/login", async (AuthRequest request, IFirebaseIdentityService identityService, ReLoopStore store, HttpContext context) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            var identity = await identityService.VerifyIdTokenAsync(request.IdToken, context.RequestAborted);
            if (identity is null) return Results.Problem(statusCode: 401, title: "Firebase authentication token is invalid, expired, revoked, or disabled.");
            var user = await store.EnsureUserAsync(identity);
            if (!user.IsActive) return Results.Problem(statusCode: 403, title: "This account is inactive.");
            await SignInAsync(context, user.UserId, user.FullName, user.Email, user.Role, request.KeepSignedIn);
            return Results.Ok(new { user = user.FullName, role = user.Role });
        }).WithTags("Authentication");

        app.MapPost("/api/auth/signup", async (SignUpRequest request, IFirebaseIdentityService identityService, ReLoopStore store, HttpContext context) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            var identity = await identityService.VerifyIdTokenAsync(request.IdToken, context.RequestAborted);
            if (identity is null) return Results.Problem(statusCode: 401, title: "Firebase authentication token is invalid, expired, revoked, or disabled.");
            var user = await store.EnsureUserAsync(identity with { DisplayName = request.FullName.Trim() });
            if (!user.IsActive) return Results.Problem(statusCode: 403, title: "This account is inactive.");
            await SignInAsync(context, user.UserId, user.FullName, user.Email, user.Role, true);
            return Results.Created("/Dashboard", new { user = user.FullName, role = user.Role });
        }).WithTags("Authentication");

        app.MapPost("/api/auth/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok(new { message = "Signed out" });
        }).WithTags("Authentication");
        app.MapGet("/api/auth/session", (HttpContext context) => Results.Ok(new { authenticated = context.User.Identity?.IsAuthenticated == true, name = context.User.Identity?.Name, role = context.User.FindFirstValue(ClaimTypes.Role) })).WithTags("Authentication");
    }

    private static void MapMemberEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", async (ReLoopStore store, HttpContext context) => Results.Ok(await store.GetDashboardAsync(Uid(context)))).RequireAuthorization().WithTags("Dashboard");
        app.MapGet("/api/pickups", async (ReLoopStore store, HttpContext context, string? status) => Results.Ok(await store.GetPickupsAsync(Uid(context), status))).RequireAuthorization().WithTags("Pickups");
        app.MapPost("/api/pickups", async (CreatePickupRequest request, ReLoopStore store, HttpContext context) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            return Results.Created("/api/pickups", await store.CreatePickupAsync(Uid(context), request));
        }).RequireAuthorization().WithTags("Pickups");
        app.MapPost("/api/scan/classify", async (HttpRequest request, ReLoopStore store, HttpContext context) =>
        {
            var fileName = request.HasFormContentType ? request.Form.Files.FirstOrDefault()?.FileName : null;
            try { return Results.Ok(await store.ClassifyScanAsync(Uid(context), fileName ?? "plastic-bottle.jpg")); }
            catch (InvalidOperationException exception) { return Results.Conflict(new { title = exception.Message }); }
        }).RequireAuthorization().DisableAntiforgery().WithTags("Scan");
        app.MapGet("/api/rewards", async (MemberServices services, HttpContext context) => Results.Ok(await services.GetRewardsAsync(Uid(context)!))).RequireAuthorization().WithTags("Rewards");
        app.MapPost("/api/rewards/redemptions", async (RedeemRewardRequest request, MemberServices services, HttpContext context) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            var result = await services.RedeemAsync(Uid(context)!, request);
            return result.Redemption is null ? Results.Conflict(new { title = result.Error }) : Results.Created("/api/rewards", result.Redemption);
        }).RequireAuthorization().WithTags("Rewards");
        app.MapGet("/api/notifications", async (MemberServices services, HttpContext context) => Results.Ok(await services.GetNotificationsAsync(Uid(context)!))).RequireAuthorization().WithTags("Notifications");
        app.MapGet("/api/waste/guidance", async (ReLoopStore store) => Results.Ok(await store.GetWasteGuidanceAsync())).WithTags("Waste guidance");
        app.MapGet("/api/waste/guidance/{category}", async (string category, ReLoopStore store) =>
        {
            var item = (await store.GetWasteGuidanceAsync(category)).FirstOrDefault();
            return item is null ? Results.NotFound(new { title = "Guidance not found" }) : Results.Ok(item);
        }).WithTags("Waste guidance");
        app.MapGet("/api/profile", async (HttpContext context, ReLoopStore store) => Results.Ok(await store.GetProfileAsync(Uid(context)))).RequireAuthorization().WithTags("Profile");
        app.MapPut("/api/profile", async (ProfileUpdateRequest request, HttpContext context, ReLoopStore store) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            ProfileDto profile;
            if (!string.Equals(request.Email, Email(context), StringComparison.OrdinalIgnoreCase)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["email"] = ["Change your Firebase Auth email first, then reload your profile."] });
            try { profile = await store.UpdateProfileAsync(Uid(context), request); }
            catch (InvalidOperationException exception) { return Results.Conflict(new { title = exception.Message }); }
            await SignInAsync(context, Uid(context)!, profile.FullName, profile.Email, profile.Role, true);
            return Results.Ok(profile);
        }).RequireAuthorization().WithTags("Profile");
        app.MapPost("/api/contact", async (ContactRequest request, ReLoopStore store) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            var message = await store.CreateContactMessageAsync(request);
            return Results.Created("/Contact", new { id = message.ContactId, message.Status });
        }).WithTags("Contact");
    }

    private static void MapAdminEndpoints(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin").RequireAuthorization("AdminOnly").WithTags("Admin");
        admin.MapGet("/stats", async (ReLoopStore store) => Results.Ok(await store.GetAdminStatsAsync()));
        admin.MapGet("/overview", async (AdminManagementService service) => Results.Ok(await service.GetOverviewAsync()));
        admin.MapGet("/users", async (AdminManagementService service, string? search, int page = 1, int pageSize = 10) => Results.Ok(await service.GetUsersAsync(search, page, pageSize)));
        admin.MapPut("/users/{id}", async (string id, AdminUserUpdateRequest request, AdminManagementService service) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            var result = await service.UpdateUserAsync(id, request);
            return result is null ? Results.NotFound(new { title = "User not found" }) : Results.Ok(result);
        });
        admin.MapGet("/pickups", async (AdminManagementService service, string? status, string? search, int page = 1, int pageSize = 10) => Results.Ok(await service.GetPickupsAsync(status, search, page, pageSize)));
        admin.MapPut("/pickups/{id}", async (string id, AdminPickupUpdateRequest request, AdminManagementService service) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            AdminPickupDto? result;
            try { result = await service.UpdatePickupAsync(id, request); }
            catch (InvalidOperationException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["assignedAgent"] = [exception.Message] }); }
            return result is null ? Results.NotFound(new { title = "Pickup not found" }) : Results.Ok(result);
        });
        admin.MapGet("/activity", async (AdminManagementService service, int page = 1, int pageSize = 10) => Results.Ok(await service.GetActivityAsync(page, pageSize)));
        admin.MapGet("/rewards", async (AdminManagementService service) => Results.Ok(await service.GetRewardsAsync()));
        admin.MapPost("/rewards", async (RewardCatalogueRequest request, AdminManagementService service) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            var result = await service.CreateRewardAsync(request);
            return Results.Created($"/api/admin/rewards/{result.Id}", result);
        });
        admin.MapPut("/rewards/{id}", async (string id, RewardCatalogueRequest request, AdminManagementService service) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            var result = await service.UpdateRewardAsync(id, request);
            return result is null ? Results.NotFound(new { title = "Reward not found" }) : Results.Ok(result);
        });
        admin.MapGet("/redemptions", async (AdminManagementService service, int page = 1, int pageSize = 10) => Results.Ok(await service.GetRedemptionsAsync(page, pageSize)));
        admin.MapPut("/redemptions/{id}", async (string id, RedemptionUpdateRequest request, AdminManagementService service) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            var result = await service.UpdateRedemptionAsync(id, request);
            return result is null ? Results.NotFound(new { title = "Redemption not found" }) : Results.Ok(result);
        });
        admin.MapGet("/reports", async (AdminManagementService service) => Results.Text(await service.BuildReportAsync(), "text/csv", System.Text.Encoding.UTF8));
    }

    private static string? Email(HttpContext context) => context.User.FindFirstValue(ClaimTypes.Email);
    private static string? Uid(HttpContext context) => context.User.FindFirstValue(ClaimTypes.NameIdentifier);

    private static Dictionary<string, string[]>? ValidationErrors<T>(T model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model!, new ValidationContext(model!), results, true);
        var errors = results.GroupBy(result => result.MemberNames.FirstOrDefault() ?? string.Empty)
            .ToDictionary(group => group.Key, group => group.Select(result => result.ErrorMessage ?? "Invalid value").ToArray());
        if (model is CreatePickupRequest pickup && pickup.PreferredDate.Date < DateTime.UtcNow.Date)
            errors["preferredDate"] = ["Choose today or a future date."];
        if (model is RedeemRewardRequest redemption && string.IsNullOrWhiteSpace(redemption.RewardId))
            errors["rewardId"] = ["Select a valid reward."];
        return errors.Count == 0 ? null : errors;
    }

    private static async Task SignInAsync(HttpContext context, string uid, string fullName, string email, string role, bool persistent)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, uid), new Claim(ClaimTypes.Name, fullName), new Claim(ClaimTypes.Email, email), new Claim(ClaimTypes.Role, role) };
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = persistent, ExpiresUtc = persistent ? DateTimeOffset.UtcNow.AddDays(14) : null });
    }
}
