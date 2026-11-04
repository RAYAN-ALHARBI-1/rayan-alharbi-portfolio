using Microsoft.EntityFrameworkCore;
using RayanPortfolio.Server.Contracts;
using RayanPortfolio.Server.Data;
using RayanPortfolio.Server.Services;

namespace RayanPortfolio.Server.Endpoints;

public static class PublicPortfolioEndpoints
{
    public static IEndpointRouteBuilder MapPublicPortfolioEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/portfolio", GetPortfolioAsync)
            .RequireRateLimiting("public-api");
        return endpoints;
    }

    private static async Task<IResult> GetPortfolioAsync(
        HttpContext context,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var database = context.RequestServices.GetService<PortfolioDbContext>();
        if (database is null)
        {
            return ApiErrors.NotConfigured("قاعدة البيانات غير مهيأة بعد.");
        }

        try
        {
            var profile = await database.Profiles
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == 1, cancellationToken);
            var projects = await database.Projects
                .AsNoTracking()
                .Where(item => item.IsPublished)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.CreatedAt)
                .ToListAsync(cancellationToken);
            var certificates = await database.Certificates
                .AsNoTracking()
                .Where(item => item.IsPublished)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.CreatedAt)
                .ToListAsync(cancellationToken);

            var response = new PublicPortfolioResponse(
                profile?.ToResponse(),
                projects.Select(item => item.ToResponse()).ToArray(),
                certificates.Select(item => item.ToResponse()).ToArray());
            return Results.Ok(response);
        }
        catch (Exception exception)
        {
            loggerFactory.CreateLogger("PortfolioApi").LogError(exception, "Unable to retrieve public portfolio data.");
            return ApiErrors.ServerError();
        }
    }
}
