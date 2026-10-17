using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ReLoop_Technologies_Web_App.Data;
using ReLoop_Technologies_Web_App.Models;
using ReLoop_Technologies_Web_App.Services;
using ReLoop_Technologies_Web_App.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Dashboard");
    options.Conventions.AuthorizePage("/ScanWaste");
    options.Conventions.AuthorizePage("/SchedulePickup");
    options.Conventions.AuthorizePage("/MyPickups");
    options.Conventions.AuthorizePage("/Profile");
    options.Conventions.AuthorizePage("/Logout");
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
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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
var connectionString = builder.Configuration.GetConnectionString("ReLoopDatabase");
if (string.IsNullOrWhiteSpace(connectionString) && !builder.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException("Configure ConnectionStrings:ReLoopDatabase using user-secrets or the ConnectionStrings__ReLoopDatabase environment variable.");
}
builder.Services.AddDbContext<ReLoopDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<ReLoopStore>();
builder.Services.AddScoped<AdminManagementService>();
builder.Services.AddScoped<MemberServices>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapReLoopApi();

app.Run();

public partial class Program;
