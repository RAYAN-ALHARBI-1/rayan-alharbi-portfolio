using System.Text.Json.Serialization;

namespace RayanPortfolio.Server.Contracts;

public sealed record ProfileUpdateRequest(
    string? FullNameAr,
    string? FullNameEn,
    string? HeadlineAr,
    string? HeadlineEn,
    string? BioAr,
    string? BioEn,
    string? LocationAr,
    string? LocationEn,
    string? Email,
    string? LinkedinUrl,
    string? PhotoUrl,
    string? ExpectedGraduationYear);

public sealed record ProjectCreateRequest(
    string? TitleAr,
    string? TitleEn,
    string? CategoryAr,
    string? CategoryEn,
    string? SummaryAr,
    string? SummaryEn,
    IReadOnlyList<string>? Technologies,
    string? ExternalUrl,
    string? CoverImageUrl,
    bool IsPublished);

public sealed record CertificateCreateRequest(
    string? TitleAr,
    string? TitleEn,
    string? IssuerAr,
    string? IssuerEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string? IssuedOn,
    string? CredentialUrl,
    bool IsPublished);

public sealed record ProfileResponse(
    [property: JsonPropertyName("full_name_ar")] string FullNameAr,
    [property: JsonPropertyName("full_name_en")] string FullNameEn,
    [property: JsonPropertyName("headline_ar")] string HeadlineAr,
    [property: JsonPropertyName("headline_en")] string HeadlineEn,
    [property: JsonPropertyName("bio_ar")] string? BioAr,
    [property: JsonPropertyName("bio_en")] string? BioEn,
    [property: JsonPropertyName("location_ar")] string LocationAr,
    [property: JsonPropertyName("location_en")] string LocationEn,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("linkedin_url")] string? LinkedinUrl,
    [property: JsonPropertyName("photo_url")] string? PhotoUrl,
    [property: JsonPropertyName("expected_graduation_year")] short? ExpectedGraduationYear);

public sealed record ProjectResponse(
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("title_ar")] string TitleAr,
    [property: JsonPropertyName("title_en")] string TitleEn,
    [property: JsonPropertyName("category_ar")] string CategoryAr,
    [property: JsonPropertyName("category_en")] string CategoryEn,
    [property: JsonPropertyName("summary_ar")] string SummaryAr,
    [property: JsonPropertyName("summary_en")] string SummaryEn,
    [property: JsonPropertyName("technologies")] IReadOnlyList<string> Technologies,
    [property: JsonPropertyName("external_url")] string? ExternalUrl,
    [property: JsonPropertyName("cover_image_url")] string? CoverImageUrl,
    [property: JsonPropertyName("is_published")] bool IsPublished,
    [property: JsonPropertyName("sort_order")] short SortOrder);

public sealed record CertificateResponse(
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("title_ar")] string TitleAr,
    [property: JsonPropertyName("title_en")] string TitleEn,
    [property: JsonPropertyName("issuer_ar")] string IssuerAr,
    [property: JsonPropertyName("issuer_en")] string IssuerEn,
    [property: JsonPropertyName("description_ar")] string DescriptionAr,
    [property: JsonPropertyName("description_en")] string DescriptionEn,
    [property: JsonPropertyName("issued_on")] DateOnly? IssuedOn,
    [property: JsonPropertyName("credential_url")] string? CredentialUrl,
    [property: JsonPropertyName("is_published")] bool IsPublished,
    [property: JsonPropertyName("sort_order")] short SortOrder);

public sealed record PublicPortfolioResponse(
    [property: JsonPropertyName("profile")] ProfileResponse? Profile,
    [property: JsonPropertyName("projects")] IReadOnlyList<ProjectResponse> Projects,
    [property: JsonPropertyName("certificates")] IReadOnlyList<CertificateResponse> Certificates);
