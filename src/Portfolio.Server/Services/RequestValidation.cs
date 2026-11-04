using System.Globalization;
using System.Text.RegularExpressions;
using RayanPortfolio.Server.Contracts;

namespace RayanPortfolio.Server.Services;

public sealed record ValidatedProfile(
    string FullNameAr,
    string FullNameEn,
    string HeadlineAr,
    string HeadlineEn,
    string? BioAr,
    string? BioEn,
    string LocationAr,
    string LocationEn,
    string Email,
    string? LinkedinUrl,
    string? PhotoUrl,
    short? ExpectedGraduationYear);

public sealed record ValidatedProject(
    string TitleAr,
    string TitleEn,
    string CategoryAr,
    string CategoryEn,
    string SummaryAr,
    string SummaryEn,
    IReadOnlyList<string> Technologies,
    string? ExternalUrl,
    string? CoverImageUrl,
    bool IsPublished);

public sealed record ValidatedCertificate(
    string TitleAr,
    string TitleEn,
    string IssuerAr,
    string IssuerEn,
    string DescriptionAr,
    string DescriptionEn,
    DateOnly? IssuedOn,
    string? CredentialUrl,
    bool IsPublished);

public static partial class RequestValidation
{
    public static bool TryValidate(ProfileUpdateRequest request, out ValidatedProfile? value, out string error)
    {
        var fullNameAr = Normalize(request.FullNameAr);
        var fullNameEn = Normalize(request.FullNameEn);
        var headlineAr = Normalize(request.HeadlineAr);
        var headlineEn = Normalize(request.HeadlineEn);
        var bioAr = Normalize(request.BioAr);
        var bioEn = Normalize(request.BioEn);
        var locationAr = Normalize(request.LocationAr);
        var locationEn = Normalize(request.LocationEn);
        var email = Normalize(request.Email).ToLowerInvariant();

        if (!HasLength(fullNameAr, 3, 120) || !HasLength(fullNameEn, 3, 120))
        {
            return Fail("الاسم العربي والإنجليزي مطلوبان وبصياغة صحيحة.", out value, out error);
        }

        if (!HasLength(headlineAr, 5, 180) || !HasLength(headlineEn, 5, 180) ||
            !HasLength(locationAr, 2, 160) || !HasLength(locationEn, 2, 160))
        {
            return Fail("المسمى والموقع بالعربية والإنجليزية مطلوبان وبصياغة صحيحة.", out value, out error);
        }

        if (!HasMatchingOptionalText(bioAr, bioEn, 1200))
        {
            return Fail("أدخل النبذة بالعربية والإنجليزية، أو اتركهما فارغتين.", out value, out error);
        }

        if (!EmailPattern().IsMatch(email))
        {
            return Fail("البريد الإلكتروني غير صحيح.", out value, out error);
        }

        if (!TryNormalizeOptionalUrl(request.LinkedinUrl, out var linkedinUrl) ||
            !TryNormalizeOptionalUrl(request.PhotoUrl, out var photoUrl))
        {
            return Fail("الرابط يجب أن يبدأ بـ https:// أو /.", out value, out error);
        }

        short? graduationYear = null;
        var graduationYearText = Normalize(request.ExpectedGraduationYear);
        if (graduationYearText.Length > 0)
        {
            if (!short.TryParse(graduationYearText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedYear) ||
                parsedYear is < 2000 or > 2100)
            {
                return Fail("سنة التخرج غير صحيحة.", out value, out error);
            }

            graduationYear = parsedYear;
        }

        value = new ValidatedProfile(
            fullNameAr,
            fullNameEn,
            headlineAr,
            headlineEn,
            bioAr.Length == 0 ? null : bioAr,
            bioEn.Length == 0 ? null : bioEn,
            locationAr,
            locationEn,
            email,
            linkedinUrl,
            photoUrl,
            graduationYear);
        error = string.Empty;
        return true;
    }

    public static bool TryValidate(ProjectCreateRequest request, out ValidatedProject? value, out string error)
    {
        var titleAr = Normalize(request.TitleAr);
        var titleEn = Normalize(request.TitleEn);
        var categoryAr = Normalize(request.CategoryAr);
        var categoryEn = Normalize(request.CategoryEn);
        var summaryAr = Normalize(request.SummaryAr);
        var summaryEn = Normalize(request.SummaryEn);
        var technologies = (request.Technologies ?? [])
            .Select(Normalize)
            .Where(item => item.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(16)
            .ToArray();

        if (!HasLength(titleAr, 3, 200) || !HasLength(titleEn, 3, 200) ||
            !HasLength(categoryAr, 2, 120) || !HasLength(categoryEn, 2, 120))
        {
            return Fail("عنوان المشروع وتصنيفه بالعربية والإنجليزية مطلوبان وبصياغة صحيحة.", out value, out error);
        }

        if (!HasLength(summaryAr, 20, 1500) || !HasLength(summaryEn, 20, 1500))
        {
            return Fail("وصف المشروع بالعربية والإنجليزية يجب أن يتراوح بين 20 و1500 حرف.", out value, out error);
        }

        if (technologies.Any(item => item.Length > 60))
        {
            return Fail("إحدى التقنيات المدخلة طويلة جدًا.", out value, out error);
        }

        if (!TryNormalizeOptionalUrl(request.ExternalUrl, out var externalUrl) ||
            !TryNormalizeOptionalUrl(request.CoverImageUrl, out var coverImageUrl))
        {
            return Fail("الرابط يجب أن يبدأ بـ https:// أو /.", out value, out error);
        }

        value = new ValidatedProject(
            titleAr,
            titleEn,
            categoryAr,
            categoryEn,
            summaryAr,
            summaryEn,
            technologies,
            externalUrl,
            coverImageUrl,
            request.IsPublished);
        error = string.Empty;
        return true;
    }

    public static bool TryValidate(CertificateCreateRequest request, out ValidatedCertificate? value, out string error)
    {
        var titleAr = Normalize(request.TitleAr);
        var titleEn = Normalize(request.TitleEn);
        var issuerAr = Normalize(request.IssuerAr);
        var issuerEn = Normalize(request.IssuerEn);
        var descriptionAr = Normalize(request.DescriptionAr);
        var descriptionEn = Normalize(request.DescriptionEn);
        var issuedOnText = Normalize(request.IssuedOn);

        if (!HasLength(titleAr, 2, 200) || !HasLength(titleEn, 2, 200) ||
            !HasLength(issuerAr, 2, 120) || !HasLength(issuerEn, 2, 120))
        {
            return Fail("عنوان الشهادة والجهة المانحة بالعربية والإنجليزية مطلوبان وبصياغة صحيحة.", out value, out error);
        }

        if (!HasLength(descriptionAr, 10, 800) || !HasLength(descriptionEn, 10, 800))
        {
            return Fail("وصف الشهادة بالعربية والإنجليزية يجب أن يتراوح بين 10 و800 حرف.", out value, out error);
        }

        DateOnly? issuedOn = null;
        if (issuedOnText.Length > 0)
        {
            if (!DateOnly.TryParseExact(issuedOnText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return Fail("تاريخ الإصدار غير صحيح.", out value, out error);
            }

            issuedOn = parsedDate;
        }

        if (!TryNormalizeOptionalUrl(request.CredentialUrl, out var credentialUrl))
        {
            return Fail("رابط الملف يجب أن يبدأ بـ https:// أو /.", out value, out error);
        }

        value = new ValidatedCertificate(
            titleAr,
            titleEn,
            issuerAr,
            issuerEn,
            descriptionAr,
            descriptionEn,
            issuedOn,
            credentialUrl,
            request.IsPublished);
        error = string.Empty;
        return true;
    }

    public static string CreateSlug(string source, string prefix)
    {
        var normalized = SlugInvalidCharacters().Replace(Normalize(source).ToLowerInvariant(), "-");
        normalized = SlugSeparator().Replace(normalized, "-").Trim('-');
        return normalized.Length > 0
            ? normalized
            : $"{prefix}-{Guid.NewGuid():N}"[..(prefix.Length + 9)];
    }

    private static bool TryNormalizeOptionalUrl(string? rawValue, out string? normalized)
    {
        var value = Normalize(rawValue);
        if (value.Length == 0)
        {
            normalized = null;
            return true;
        }

        if (value.StartsWith("/", StringComparison.Ordinal) &&
            !value.StartsWith("//", StringComparison.Ordinal) &&
            !value.Contains('\\'))
        {
            normalized = value;
            return true;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var url) && url.Scheme == Uri.UriSchemeHttps)
        {
            normalized = url.AbsoluteUri;
            return true;
        }

        normalized = null;
        return false;
    }

    private static bool HasLength(string value, int minimum, int maximum) =>
        value.Length >= minimum && value.Length <= maximum;

    private static bool HasMatchingOptionalText(string firstValue, string secondValue, int maximum) =>
        firstValue.Length <= maximum &&
        secondValue.Length <= maximum &&
        (firstValue.Length == 0) == (secondValue.Length == 0);

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    private static bool Fail<T>(string message, out T? value, out string error)
    {
        value = default;
        error = message;
        return false;
    }

    [GeneratedRegex("^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex SlugInvalidCharacters();

    [GeneratedRegex("-{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex SlugSeparator();
}
