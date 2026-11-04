using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RayanPortfolio.Server.Data;
using RayanPortfolio.Server.Options;

namespace RayanPortfolio.Server.Services;

public static class PortfolioAuthentication
{
    public const string AdminPolicy = "portfolio-admin";

    public const string AdminUserIdClaim = "portfolio_admin_user_id";

    public const string AdminRoleClaim = "portfolio_admin_role";

    public static void Configure(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.Configure<GoogleAuthenticationOptions>(configuration.GetSection(GoogleAuthenticationOptions.SectionName));

        var authentication = services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = environment.IsDevelopment()
                    ? "rayan_portfolio_admin"
                    : "__Host-rayan-portfolio-admin";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;
                options.LoginPath = "/admin-login.html";
                options.Events.OnRedirectToLogin = WriteUnauthorizedApiResponse;
                options.Events.OnRedirectToAccessDenied = WriteForbiddenApiResponse;
                options.Events.OnValidatePrincipal = ValidateAdminPrincipalAsync;
            });

        var googleOptions = configuration
            .GetSection(GoogleAuthenticationOptions.SectionName)
            .Get<GoogleAuthenticationOptions>() ?? new GoogleAuthenticationOptions();

        if (googleOptions.IsConfigured)
        {
            authentication.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = googleOptions.ClientId;
                options.ClientSecret = googleOptions.ClientSecret;
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = CreateAdminIdentityAsync,
                    OnRemoteFailure = RedirectAfterGoogleFailureAsync
                };
            });
        }

        services.AddAuthorizationBuilder()
            .AddPolicy(AdminPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(AdminRoleClaim, AdminRoles.Owner, AdminRoles.Editor);
            });
    }

    private static async Task CreateAdminIdentityAsync(OAuthCreatingTicketContext context)
    {
        var email = GetString(context.User, "email").ToLowerInvariant();
        var googleSubject = GetString(context.User, "sub");
        var displayName = GetString(context.User, "name");

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = email;
        }

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(googleSubject) ||
            !IsEmailVerified(context.User))
        {
            context.Fail("The Google account could not be verified.");
            return;
        }

        var access = context.HttpContext.RequestServices.GetRequiredService<AdminAccessService>();
        var database = context.HttpContext.RequestServices.GetService<PortfolioDbContext>();
        if (!access.IsConfigured || database is null)
        {
            context.Fail("The administration service is not configured.");
            return;
        }

        var user = await access.FindOrCreateAuthorizedUserAsync(
            database,
            googleSubject,
            email,
            displayName,
            context.HttpContext.RequestAborted);
        if (user is null)
        {
            context.Fail("The Google account is not authorized.");
            return;
        }

        var identity = context.Principal?.Identity as ClaimsIdentity;
        if (identity is null)
        {
            context.Fail("The Google identity is invalid.");
            return;
        }

        AddClaimIfMissing(identity, ClaimTypes.NameIdentifier, googleSubject);
        AddClaimIfMissing(identity, ClaimTypes.Email, email);
        AddClaimIfMissing(identity, ClaimTypes.Name, displayName);
        AddClaimIfMissing(identity, AdminUserIdClaim, user.Id.ToString());
        AddClaimIfMissing(identity, AdminRoleClaim, user.Role);
    }

    private static async Task ValidateAdminPrincipalAsync(CookieValidatePrincipalContext context)
    {
        var userIdValue = context.Principal?.FindFirstValue(AdminUserIdClaim);
        var role = context.Principal?.FindFirstValue(AdminRoleClaim);
        if (!Guid.TryParse(userIdValue, out var userId) || string.IsNullOrWhiteSpace(role))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        var database = context.HttpContext.RequestServices.GetService<PortfolioDbContext>();
        if (database is null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        var user = await database.AdminUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, context.HttpContext.RequestAborted);
        if (user is null || !user.IsActive || !string.Equals(user.Role, role, StringComparison.Ordinal))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    private static Task RedirectAfterGoogleFailureAsync(RemoteFailureContext context)
    {
        context.HandleResponse();
        context.Response.Redirect("/admin-login.html?error=google_sign_in_failed");
        return Task.CompletedTask;
    }

    private static Task WriteUnauthorizedApiResponse(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(new { error = "يلزم تسجيل الدخول للوصول إلى الإدارة." });
    }

    private static Task WriteForbiddenApiResponse(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return context.Response.WriteAsJsonAsync(new { error = "هذا الحساب غير مصرح له بالدخول." });
    }

    private static bool IsEmailVerified(JsonElement user) =>
        user.TryGetProperty("email_verified", out var value) &&
        (value.ValueKind == JsonValueKind.True ||
         value.ValueKind == JsonValueKind.String &&
         bool.TryParse(value.GetString(), out var verified) &&
         verified);

    private static string GetString(JsonElement user, string propertyName) =>
        user.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim() ?? string.Empty
            : string.Empty;

    private static void AddClaimIfMissing(ClaimsIdentity identity, string type, string value)
    {
        if (!identity.HasClaim(claim => claim.Type == type))
        {
            identity.AddClaim(new Claim(type, value));
        }
    }
}
