using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Options;
using RayanPortfolio.Server.Data;
using RayanPortfolio.Server.Options;
using RayanPortfolio.Server.Services;

namespace RayanPortfolio.Server.Endpoints;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/auth/config", GetConfiguration)
            .RequireRateLimiting("public-api");
        endpoints.MapGet("/api/v1/auth/google", StartGoogleSignIn)
            .RequireRateLimiting("auth");
        endpoints.MapPost("/api/v1/auth/logout", async (HttpContext context, IAntiforgery antiforgery) =>
                await SignOutAsync(context, antiforgery))
            .RequireAuthorization(PortfolioAuthentication.AdminPolicy)
            .RequireRateLimiting("admin");
        return endpoints;
    }

    private static IResult GetConfiguration(
        HttpContext context,
        IOptions<GoogleAuthenticationOptions> google,
        AdminAccessService access)
    {
        var databaseConfigured = context.RequestServices.GetService<PortfolioDbContext>() is not null;
        return Results.Ok(new
        {
            googleEnabled = google.Value.IsConfigured && access.IsConfigured && databaseConfigured
        });
    }

    private static IResult StartGoogleSignIn(
        HttpContext context,
        IOptions<GoogleAuthenticationOptions> google,
        AdminAccessService access)
    {
        if (!google.Value.IsConfigured || !access.IsConfigured ||
            context.RequestServices.GetService<PortfolioDbContext>() is null)
        {
            return ApiErrors.NotConfigured("خدمة الدخول غير مهيأة بعد.");
        }

        var returnUrl = GetSafeReturnUrl(context.Request.Query["returnUrl"]);
        var properties = new AuthenticationProperties { RedirectUri = returnUrl };
        return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
    }

    private static async Task<IResult> SignOutAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            return ApiErrors.Forbidden("تعذّر التحقق من طلب تسجيل الخروج.");
        }

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok(new { status = "signed_out" });
    }

    private static string GetSafeReturnUrl(string? requestedUrl)
    {
        if (!string.IsNullOrWhiteSpace(requestedUrl) &&
            requestedUrl.StartsWith("/", StringComparison.Ordinal) &&
            !requestedUrl.StartsWith("//", StringComparison.Ordinal) &&
            !requestedUrl.Contains('\\'))
        {
            return requestedUrl;
        }

        return "/admin.html";
    }
}
