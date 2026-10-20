using System.Security.Claims;
using System.Text.Json.Serialization;
using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Infrastructure.Auth;
using DryCleaning.Api.Infrastructure.Errors;
using DryCleaning.Api.Infrastructure.Payments;
using DryCleaning.Api.Infrastructure.Push;
using DryCleaning.Api.Infrastructure.Realtime;
using DryCleaning.Api.Infrastructure.Storage;
using DryCleaning.Api.Infrastructure.Swagger;
using DryCleaning.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ── Options ──────────────────────────────────────────────────────────────────
builder.Services.Configure<MarketplaceOptions>(config.GetSection(MarketplaceOptions.Section));
builder.Services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
builder.Services.Configure<PaystackOptions>(config.GetSection(PaystackOptions.Section));
builder.Services.Configure<OtpOptions>(config.GetSection(OtpOptions.Section));
builder.Services.Configure<StorageOptions>(config.GetSection(StorageOptions.Section));

// ── Push notifications (Firebase Cloud Messaging; log-only when not configured) ──
builder.Services.AddPushNotifications(config);

// ── Real-time updates (SignalR hub at /hubs/realtime) ─────────────────────────
builder.Services.AddRealtime();

// ── Database (SQL Server) ────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlServer(
            config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing."),
            sql => sql.EnableRetryOnFailure())
        .AddInterceptors(
            sp.GetRequiredService<NotificationPushInterceptor>(), // every saved notification is also pushed
            sp.GetRequiredService<RealtimeEventInterceptor>())); // order/job/notification changes go to connected apps

// ── Authentication (JWT) & authorization (roles) ─────────────────────────────
var jwt = config.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
    throw new InvalidOperationException("Jwt:SigningKey must be set and at least 32 characters (use user-secrets or environment variables).");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtTokenService.SigningKey(jwt),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = RealtimeSetup.AcceptQueryStringToken();
    });
builder.Services.AddAuthorization();

// ── App services ─────────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddSingleton<IOtpService, MemoryOtpService>();
// Photos and logos: wwwroot for development / single server, Azure Blob Storage when hosted in Azure.
if (config.GetSection(StorageOptions.Section).Get<StorageOptions>()?.UseAzureBlob == true)
    builder.Services.AddSingleton<IFileStorage, AzureBlobFileStorage>();
else
    builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddHttpClient<IPaystackClient, PaystackClient>((sp, client) =>
{
    var baseUrl = config.GetSection(PaystackOptions.Section).Get<PaystackOptions>()?.BaseUrl ?? "https://api.paystack.co/";
    client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<CurrentProfile>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AddressService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<PublicCatalogService>();
builder.Services.AddScoped<ProviderCatalogService>();
builder.Services.AddScoped<ProviderProfileService>();
builder.Services.AddScoped<WorkingHoursService>();
builder.Services.AddScoped<DeliveryPricingService>();
builder.Services.AddScoped<CustomerOrderService>();
builder.Services.AddScoped<ProviderOrderService>();
builder.Services.AddScoped<DriverService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<OrderPhotoService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<UserPhotoService>();
builder.Services.AddScoped<AdminService>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ── Web ──────────────────────────────────────────────────────────────────────
builder.Services
    .AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length == 0)
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    else
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();
FileUrls.Use(app.Services.GetRequiredService<IFileStorage>());

// ── Database initialisation & demo data ──────────────────────────────────────
if (config.GetValue("Database:InitializeOnStartup", true))
    await DbSeeder.InitialiseAsync(app.Services, config.GetValue("Database:SeedDemoData", app.Environment.IsDevelopment()));

// ── Pipeline ─────────────────────────────────────────────────────────────────
app.UseExceptionHandler();
app.UseStatusCodePages();

if (config.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "EDry Cleaners API v1");
        options.DocumentTitle = "EDry Cleaners API";
        options.EnablePersistAuthorization();
        options.DisplayRequestDuration();
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    // Development skips this so the mobile app can call http://10.0.2.2:5180 (emulator) without
    // being redirected to an https address the dev certificate doesn't cover.
    app.UseHttpsRedirection();
}
app.UseStaticFiles(); // serves /uploads/... photos
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRealtime();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

/// <summary>Exposed for integration tests.</summary>
public partial class Program;
