namespace RayanPortfolio.Server.Options;

public sealed class AdminAccessOptions
{
    public const string SectionName = "AdminAccess";

    public List<string> AllowedEmails { get; init; } = [];

    public List<string> OwnerEmails { get; init; } = [];
}

public sealed class GoogleAuthenticationOptions
{
    public const string SectionName = "Authentication:Google";

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);
}
