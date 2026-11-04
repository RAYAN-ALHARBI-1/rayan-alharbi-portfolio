using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RayanPortfolio.Server.Data;

public sealed class PortfolioDbContextFactory : IDesignTimeDbContextFactory<PortfolioDbContext>
{
    public PortfolioDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<PortfolioDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("PortfolioDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "لم يتم إعداد ConnectionStrings:PortfolioDb. شغّل scripts\\connect-cloud-database.ps1 أو اضبط متغير البيئة المطلوب.");
        }

        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseNpgsql(connectionString, postgres =>
                postgres.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null))
            .Options;
        return new PortfolioDbContext(options);
    }
}
