using System.Threading.RateLimiting;
using ERP.Application;
using ERP.Infrastructure;
using ERP.Infrastructure.Identity.Models;
using ERP.Infrastructure.Persistence.Context;
using ERP.Infrastructure.Persistence.Seeders;
using ERP.Web.Components;
using ERP.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MudBlazor.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog structured logging (Console + Rolling File)
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/erp-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));

// Add Application & Infrastructure Clean Architecture layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register Dynamic PBAC Policy Provider, Handler & Blazor Circuit User Synchronizer
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<CircuitHandler, UserCircuitHandler>();

// Add MudBlazor services
builder.Services.AddMudServices();

// Configure Forwarded Headers for reverse proxy client IP resolution
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Rate Limiting Policy for Authentication Endpoints (Partitioned per Client IP)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("LoginPolicy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

// Configure Secure Antiforgery Cookie
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.HttpOnly = true;
});

// Add Health Checks (PostgreSQL connectivity verification)
builder.Services.AddHealthChecks()
    .AddTypeActivatedCheck<PostgresDatabaseHealthCheck>("postgresql", HealthStatus.Unhealthy);

// Add Blazor Server with optimized circuit retention and SignalR resilience
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
        options.DisconnectedCircuitMaxRetained = 500;
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });

builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.MaximumReceiveMessageSize = 1024 * 1024; // 1 MB
    hubOptions.KeepAliveInterval = TimeSpan.FromSeconds(15);
    hubOptions.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    hubOptions.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Seed initial baseline data (Company, Branch, Roles, Permissions, Super Admin)
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue("Database:RunMigrationsOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Health Check Endpoint for Container / Load Balancer Probes
app.MapHealthChecks("/health").AllowAnonymous();

// Authentication Endpoints with CSRF Protection & Per-IP Rate Limiting
app.MapPost("/api/auth/login", async (
    [FromForm] string email,
    [FromForm] string password,
    [FromForm] bool? rememberMe,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) =>
{
    var user = await userManager.FindByEmailAsync(email);
    if (user == null || !user.IsActive || user.IsDeleted)
    {
        return Results.Redirect("/login?error=invalid_credentials");
    }

    var result = await signInManager.PasswordSignInAsync(user.UserName!, password, rememberMe ?? false, lockoutOnFailure: true);
    if (result.Succeeded)
    {
        return Results.Redirect("/");
    }
    if (result.IsLockedOut)
    {
        return Results.Redirect("/login?error=locked_out");
    }

    return Results.Redirect("/login?error=invalid_credentials");
}).RequireRateLimiting("LoginPolicy");

app.MapPost("/api/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public sealed class PostgresDatabaseHealthCheck(ApplicationDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL database connection is healthy.")
                : HealthCheckResult.Unhealthy("PostgreSQL database cannot be reached.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL health check failed.", ex);
        }
    }
}
