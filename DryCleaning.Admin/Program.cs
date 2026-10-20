using System.Security.Claims;
using DryCleaning.Admin.Components;
using DryCleaning.Admin.Services;
using DryCleaning.Shared.Secrets;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddSecretsFile(builder.Environment, "Admin"); // the repository's single secrets.json (git-ignored)

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddHttpClient(AdminApiClient.HttpClientName, client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<AdminApiClient>();
builder.Services.AddScoped<AdminLoginService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "edry.admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/login";
        options.SlidingExpiration = false;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

// Login form post: checks the credentials with the API, then keeps the access token in the sign-in cookie.
app.MapPost("/auth/login", async (HttpContext http, AdminLoginService login, [FromForm] string? identifier, [FromForm] string? password) =>
{
    var (result, error) = await login.LoginAsync(identifier ?? string.Empty, password ?? string.Empty);
    if (result is null)
        return Results.Redirect("/login?error=" + Uri.EscapeDataString(error ?? "Sign-in failed."));

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
        new(ClaimTypes.Name, result.User.Name),
        new(ClaimTypes.Role, "Admin"),
        new(AdminApiClient.TokenClaim, result.AccessToken)
    };
    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
        new AuthenticationProperties { ExpiresUtc = result.ExpiresAt, IsPersistent = false });
    return Results.Redirect("/");
});

app.MapGet("/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
