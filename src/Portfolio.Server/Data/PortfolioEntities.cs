namespace RayanPortfolio.Server.Data;

public sealed class PortfolioProfile
{
    public short Id { get; set; } = 1;

    public string FullNameAr { get; set; } = string.Empty;

    public string FullNameEn { get; set; } = string.Empty;

    public string HeadlineAr { get; set; } = string.Empty;

    public string HeadlineEn { get; set; } = string.Empty;

    public string? BioAr { get; set; }

    public string? BioEn { get; set; }

    public string LocationAr { get; set; } = string.Empty;

    public string LocationEn { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? LinkedinUrl { get; set; }

    public string? PhotoUrl { get; set; }

    public short? ExpectedGraduationYear { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class PortfolioProject
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string TitleAr { get; set; } = string.Empty;

    public string TitleEn { get; set; } = string.Empty;

    public string CategoryAr { get; set; } = string.Empty;

    public string CategoryEn { get; set; } = string.Empty;

    public string SummaryAr { get; set; } = string.Empty;

    public string SummaryEn { get; set; } = string.Empty;

    public string TechnologiesJson { get; set; } = "[]";

    public string? ExternalUrl { get; set; }

    public string? CoverImageUrl { get; set; }

    public bool IsPublished { get; set; } = true;

    public short SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class PortfolioCertificate
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string TitleAr { get; set; } = string.Empty;

    public string TitleEn { get; set; } = string.Empty;

    public string IssuerAr { get; set; } = string.Empty;

    public string IssuerEn { get; set; } = string.Empty;

    public string DescriptionAr { get; set; } = string.Empty;

    public string DescriptionEn { get; set; } = string.Empty;

    public DateOnly? IssuedOn { get; set; }

    public string? CredentialUrl { get; set; }

    public bool IsPublished { get; set; } = true;

    public short SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class AdminUser
{
    public Guid Id { get; set; }

    public string GoogleSub { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Role { get; set; } = AdminRoles.Editor;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ContactMessage
{
    public Guid Id { get; set; }

    public string SenderName { get; set; } = string.Empty;

    public string SenderEmail { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = "new";

    public DateTimeOffset CreatedAt { get; set; }
}

public static class AdminRoles
{
    public const string Owner = "owner";
    public const string Editor = "editor";
}
