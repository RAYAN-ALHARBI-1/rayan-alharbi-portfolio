using System.Text.Json;
using RayanPortfolio.Server.Contracts;
using RayanPortfolio.Server.Data;

namespace RayanPortfolio.Server.Services;

public static class PortfolioMapping
{
    public static ProfileResponse ToResponse(this PortfolioProfile profile) => new(
        profile.FullNameAr,
        profile.FullNameEn,
        profile.HeadlineAr,
        profile.HeadlineEn,
        profile.BioAr,
        profile.BioEn,
        profile.LocationAr,
        profile.LocationEn,
        profile.Email,
        profile.LinkedinUrl,
        profile.PhotoUrl,
        profile.ExpectedGraduationYear);

    public static ProjectResponse ToResponse(this PortfolioProject project) => new(
        project.Slug,
        project.TitleAr,
        project.TitleEn,
        project.CategoryAr,
        project.CategoryEn,
        project.SummaryAr,
        project.SummaryEn,
        DeserializeTechnologies(project.TechnologiesJson),
        project.ExternalUrl,
        project.CoverImageUrl,
        project.IsPublished,
        project.SortOrder);

    public static CertificateResponse ToResponse(this PortfolioCertificate certificate) => new(
        certificate.Slug,
        certificate.TitleAr,
        certificate.TitleEn,
        certificate.IssuerAr,
        certificate.IssuerEn,
        certificate.DescriptionAr,
        certificate.DescriptionEn,
        certificate.IssuedOn,
        certificate.CredentialUrl,
        certificate.IsPublished,
        certificate.SortOrder);

    public static string SerializeTechnologies(IReadOnlyList<string> technologies) =>
        JsonSerializer.Serialize(technologies);

    private static IReadOnlyList<string> DeserializeTechnologies(string? value)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(value ?? "[]") ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
