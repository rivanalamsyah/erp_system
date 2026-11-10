using System.Threading.RateLimiting;
using ERP.Application;
using ERP.Infrastructure;
using ERP.Infrastructure.Identity.Models;
using ERP.Infrastructure.Persistence.Seeders;
using ERP.Web.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Application & Infrastructure Clean Architecture layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add MudBlazor services
builder.Services.AddMudServices();

// Rate Limiting Policy for Authentication Endpoints
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("LoginPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

// Configure Secure Antiforgery Cookie
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.HttpOnly = true;
});

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
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

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

// Authentication Endpoints with CSRF Protection & Rate Limiting
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
