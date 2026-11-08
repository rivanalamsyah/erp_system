using ERP.Application;
using ERP.Infrastructure;
using ERP.Infrastructure.Identity.Models;
using ERP.Infrastructure.Persistence.Seeders;
using ERP.Web.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Application & Infrastructure Clean Architecture layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add Blazor and Authentication state
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Authentication Endpoints
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
}).DisableAntiforgery();

app.MapPost("/api/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
