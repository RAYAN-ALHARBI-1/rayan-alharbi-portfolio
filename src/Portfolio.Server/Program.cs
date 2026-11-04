using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using RayanPortfolio.Server.Data;
using RayanPortfolio.Server.Endpoints;
using RayanPortfolio.Server.Options;
using RayanPortfolio.Server.Services;
using System.Text.Json;
using System.Threading.RateLimiting;

var webRootDirectory = FindWebRootDirectory();
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = webRootDirectory
});
builder.Services.AddProblemDetails();
builder.Services.Configure<AdminAccessOptions>(builder.Configuration.GetSection(AdminAccessOptions.SectionName));
builder.Services.AddSingleton<AdminAccessService>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-Token";
    options.Cookie.Name = builder.Environment.IsDevelopment()
        ? "rayan_portfolio_antiforgery"
        : "__Host-rayan-portfolio-antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

var databaseConnectionString = builder.Configuration.GetConnectionString("PortfolioDb");
if (!string.IsNullOrWhiteSpace(databaseConnectionString))
{
    builder.Services.AddDbContext<PortfolioDbContext>(options =>
        options.UseNpgsql(databaseConnectionString, postgres =>
            postgres.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null)));
}

PortfolioAuthentication.Configure(builder.Services, builder.Configuration, builder.Environment);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("public-api", limiter =>
    {
        limiter.PermitLimit = 120;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(5);
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("admin", limiter =>
    {
        limiter.PermitLimit = 90;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

var app = builder.Build();

if (args.Contains("--seed-initial-portfolio", StringComparer.Ordinal))
{
    await SeedInitialPortfolioAsync(app);
    return;
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), geolocation=(), microphone=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; img-src 'self' data: https:; object-src 'none'; style-src 'self'; script-src 'self'; connect-src 'self'";

    var requestPath = context.Request.Path.Value;
    if (requestPath is "/admin.html" or "/admin-login.html" ||
        context.Request.Path.StartsWithSegments("/api/v1/admin") ||
        context.Request.Path.StartsWithSegments("/api/v1/auth"))
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
        context.Response.Headers["Pragma"] = "no-cache";
    }

    await next();
});

app.UseRateLimiter();
app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(webRootDirectory) });
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/", () => Results.Redirect("/portfolio.html"));
app.MapGet("/admin", () => Results.Redirect("/admin-login.html"));

app.MapGet("/health", async (HttpContext context, CancellationToken cancellationToken) =>
{
    var database = context.RequestServices.GetService<PortfolioDbContext>();
    if (database is null)
    {
        return Results.Ok(new { status = "ok", service = "rayan-portfolio-server", database = "not_configured" });
    }

    try
    {
        var isConnected = await database.Database.CanConnectAsync(cancellationToken);
        return isConnected
            ? Results.Ok(new { status = "ok", service = "rayan-portfolio-server", database = "ready" })
            : Results.Json(new { status = "degraded", service = "rayan-portfolio-server", database = "unavailable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.Json(new { status = "degraded", service = "rayan-portfolio-server", database = "unavailable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).RequireRateLimiting("public-api");

app.MapGet("/error", (HttpContext context) =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    app.Logger.LogError(exception, "Unhandled request error.");
    return Results.Problem(statusCode: StatusCodes.Status500InternalServerError);
});

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapAuthenticationEndpoints();
app.MapPublicPortfolioEndpoints();
app.MapAdminEndpoints();

app.Run();

static async Task SeedInitialPortfolioAsync(WebApplication app)
{
    await using var serviceScope = app.Services.CreateAsyncScope();
    var database = serviceScope.ServiceProvider.GetService<PortfolioDbContext>()
        ?? throw new InvalidOperationException(
            "لم يتم إعداد قاعدة البيانات. اضبط ConnectionStrings:PortfolioDb قبل تنفيذ التعبئة الأولية.");

    await database.Database.MigrateAsync();
    await InitialPortfolioDataSeeder.SeedIfMissingAsync(database);
    app.Logger.LogInformation("Initial portfolio content is ready.");
}

static string FindWebRootDirectory()
{
    var candidates = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), "public"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "public"),
        Path.Combine(AppContext.BaseDirectory, "wwwroot")
    };

    return candidates
        .Select(Path.GetFullPath)
        .FirstOrDefault(Directory.Exists)
        ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "wwwroot"));
}
