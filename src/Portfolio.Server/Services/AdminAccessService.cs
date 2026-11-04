using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RayanPortfolio.Server.Data;
using RayanPortfolio.Server.Options;

namespace RayanPortfolio.Server.Services;

public sealed class AdminAccessService
{
    private readonly HashSet<string> _allowedEmails;
    private readonly HashSet<string> _ownerEmails;

    public AdminAccessService(IOptions<AdminAccessOptions> options)
    {
        _allowedEmails = CreateEmailSet(options.Value.AllowedEmails);
        _ownerEmails = CreateEmailSet(options.Value.OwnerEmails);
    }

    public bool IsConfigured => _allowedEmails.Count > 0;

    public bool IsAllowed(string email) => _allowedEmails.Contains(NormalizeEmail(email));

    public async Task<AdminUser?> FindOrCreateAuthorizedUserAsync(
        PortfolioDbContext database,
        string googleSubject,
        string email,
        string displayName,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (!IsAllowed(normalizedEmail))
        {
            return null;
        }

        var user = await database.AdminUsers
            .SingleOrDefaultAsync(item => item.GoogleSub == googleSubject, cancellationToken);

        if (user is null)
        {
            var matchingEmail = await database.AdminUsers
                .SingleOrDefaultAsync(item => item.Email == normalizedEmail, cancellationToken);

            if (matchingEmail is not null)
            {
                return null;
            }

            user = new AdminUser
            {
                Id = Guid.NewGuid(),
                GoogleSub = googleSubject,
                Email = normalizedEmail,
                DisplayName = displayName,
                Role = _ownerEmails.Contains(normalizedEmail) ? AdminRoles.Owner : AdminRoles.Editor,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            database.AdminUsers.Add(user);
        }
        else
        {
            if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            user.DisplayName = displayName;
            user.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await database.SaveChangesAsync(cancellationToken);
        return user.IsActive ? user : null;
    }

    private static HashSet<string> CreateEmailSet(IEnumerable<string> emails) =>
        emails
            .Select(NormalizeEmail)
            .Where(email => email.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string NormalizeEmail(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;
}
