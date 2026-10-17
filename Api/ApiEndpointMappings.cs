using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
        app.MapPost("/api/auth/login", async (AuthRequest request, ReLoopStore store, HttpContext context) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            var user = await store.AuthenticateAsync(request.Email.Trim(), request.Password);
            if (user is null) return Results.Problem(statusCode: 401, title: "Invalid email or password");
            await SignInAsync(context, user.FullName, user.Email, user.Role, request.KeepSignedIn);
            return Results.Ok(new { user = user.FullName, role = user.Role });
        }).WithTags("Authentication");

        app.MapPost("/api/auth/signup", async (SignUpRequest request, ReLoopStore store, HttpContext context) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            var user = await store.CreateUserAsync(request.FullName.Trim(), request.Email.Trim(), request.Password);
            if (user is null) return Results.Conflict(new { title = "An account already exists for this email.", errors = new Dictionary<string, string[]> { ["email"] = ["Use another email or sign in instead."] } });
            await SignInAsync(context, user.FullName, user.Email, user.Role, true);
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
        app.MapGet("/api/dashboard", async (ReLoopStore store, HttpContext context) => Results.Ok(await store.GetDashboardAsync(Email(context)))).RequireAuthorization().WithTags("Dashboard");
        app.MapGet("/api/pickups", async (ReLoopStore store, HttpContext context, string? status) => Results.Ok(await store.GetPickupsAsync(Email(context), status))).RequireAuthorization().WithTags("Pickups");
        app.MapPost("/api/pickups", async (CreatePickupRequest request, ReLoopStore store, HttpContext context) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            return Results.Created("/api/pickups", await store.CreatePickupAsync(Email(context), request));
        }).RequireAuthorization().WithTags("Pickups");
        app.MapPost("/api/scan/classify", async (HttpRequest request, ReLoopStore store, HttpContext context) =>
        {
            var fileName = request.HasFormContentType ? request.Form.Files.FirstOrDefault()?.FileName : null;
            return Results.Ok(await store.ClassifyScanAsync(Email(context), fileName ?? "plastic-bottle.jpg"));
        }).RequireAuthorization().DisableAntiforgery().WithTags("Scan");
        app.MapGet("/api/rewards", async (MemberServices services, HttpContext context) => Results.Ok(await services.GetRewardsAsync(Email(context)!))).RequireAuthorization().WithTags("Rewards");
        app.MapPost("/api/rewards/redemptions", async (RedeemRewardRequest request, MemberServices services, HttpContext context) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            var result = await services.RedeemAsync(Email(context)!, request);
            return result.Redemption is null ? Results.Conflict(new { title = result.Error }) : Results.Created("/api/rewards", result.Redemption);
        }).RequireAuthorization().WithTags("Rewards");
        app.MapGet("/api/notifications", async (MemberServices services, HttpContext context) => Results.Ok(await services.GetNotificationsAsync(Email(context)!))).RequireAuthorization().WithTags("Notifications");
        app.MapGet("/api/waste/guidance", () => Results.Ok(WasteGuidanceService.GetAll())).WithTags("Waste guidance");
        app.MapGet("/api/waste/guidance/{category}", (string category) =>
        {
            var item = WasteGuidanceService.Get(category);
            return item is null ? Results.NotFound(new { title = "Guidance not found" }) : Results.Ok(item);
        }).WithTags("Waste guidance");
        app.MapGet("/api/profile", async (HttpContext context, ReLoopStore store) => Results.Ok(await store.GetProfileAsync(Email(context)))).RequireAuthorization().WithTags("Profile");
        app.MapPut("/api/profile", async (ProfileUpdateRequest request, HttpContext context, ReLoopStore store) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            ProfileDto profile;
            try { profile = await store.UpdateProfileAsync(Email(context), request); }
            catch (InvalidOperationException exception) { return Results.Conflict(new { title = exception.Message }); }
            await SignInAsync(context, profile.FullName, profile.Email, profile.Role, true);
            return Results.Ok(profile);
        }).RequireAuthorization().WithTags("Profile");
        app.MapPost("/api/contact", async (ContactRequest request, ReLoopStore store) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            var message = await store.CreateContactMessageAsync(request);
            return Results.Created("/Contact", new { message.Id, message.Status });
        }).WithTags("Contact");
    }

    private static void MapAdminEndpoints(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin").RequireAuthorization("AdminOnly").WithTags("Admin");
        admin.MapGet("/stats", async (ReLoopStore store) => Results.Ok(await store.GetAdminStatsAsync()));
        admin.MapGet("/overview", async (AdminManagementService service) => Results.Ok(await service.GetOverviewAsync()));
        admin.MapGet("/users", async (AdminManagementService service, string? search, int page = 1, int pageSize = 10) => Results.Ok(await service.GetUsersAsync(search, page, pageSize)));
        admin.MapPut("/users/{id:guid}", async (Guid id, AdminUserUpdateRequest request, AdminManagementService service) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            var result = await service.UpdateUserAsync(id, request);
            return result is null ? Results.NotFound(new { title = "User not found" }) : Results.Ok(result);
        });
        admin.MapGet("/pickups", async (AdminManagementService service, string? status, string? search, int page = 1, int pageSize = 10) => Results.Ok(await service.GetPickupsAsync(status, search, page, pageSize)));
        admin.MapPut("/pickups/{id:guid}", async (Guid id, AdminPickupUpdateRequest request, AdminManagementService service) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            var result = await service.UpdatePickupAsync(id, request);
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
        admin.MapPut("/rewards/{id:guid}", async (Guid id, RewardCatalogueRequest request, AdminManagementService service) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            var result = await service.UpdateRewardAsync(id, request);
            return result is null ? Results.NotFound(new { title = "Reward not found" }) : Results.Ok(result);
        });
        admin.MapGet("/redemptions", async (AdminManagementService service, int page = 1, int pageSize = 10) => Results.Ok(await service.GetRedemptionsAsync(page, pageSize)));
        admin.MapPut("/redemptions/{id:guid}", async (Guid id, RedemptionUpdateRequest request, AdminManagementService service) =>
        {
            if (ValidationErrors(request) is { Count: > 0 } errors) return Results.ValidationProblem(errors);
            var result = await service.UpdateRedemptionAsync(id, request);
            return result is null ? Results.NotFound(new { title = "Redemption not found" }) : Results.Ok(result);
        });
        admin.MapGet("/reports", async (AdminManagementService service) => Results.Text(await service.BuildReportAsync(), "text/csv", System.Text.Encoding.UTF8));
    }

    private static string? Email(HttpContext context) => context.User.FindFirstValue(ClaimTypes.Email);

    private static Dictionary<string, string[]>? ValidationErrors<T>(T model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model!, new ValidationContext(model!), results, true);
        var errors = results.GroupBy(result => result.MemberNames.FirstOrDefault() ?? string.Empty)
            .ToDictionary(group => group.Key, group => group.Select(result => result.ErrorMessage ?? "Invalid value").ToArray());
        if (model is CreatePickupRequest pickup && pickup.PreferredDate.Date < DateTime.UtcNow.Date)
            errors["preferredDate"] = ["Choose today or a future date."];
        if (model is RedeemRewardRequest redemption && redemption.RewardId == Guid.Empty)
            errors["rewardId"] = ["Select a valid reward."];
        return errors.Count == 0 ? null : errors;
    }

    private static async Task SignInAsync(HttpContext context, string fullName, string email, string role, bool persistent)
    {
        var claims = new[] { new Claim(ClaimTypes.Name, fullName), new Claim(ClaimTypes.Email, email), new Claim(ClaimTypes.Role, role) };
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = persistent, ExpiresUtc = persistent ? DateTimeOffset.UtcNow.AddDays(14) : null });
    }
}
