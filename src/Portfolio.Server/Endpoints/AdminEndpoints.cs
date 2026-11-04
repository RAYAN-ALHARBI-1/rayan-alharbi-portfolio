using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using RayanPortfolio.Server.Contracts;
using RayanPortfolio.Server.Data;
using RayanPortfolio.Server.Services;

namespace RayanPortfolio.Server.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin")
            .RequireAuthorization(PortfolioAuthentication.AdminPolicy)
            .RequireRateLimiting("admin");

        group.MapGet("/session", GetSession);
        group.MapGet("/profile", GetProfileAsync);
        group.MapPut("/profile", UpdateProfileAsync);
        group.MapGet("/projects", GetProjectsAsync);
        group.MapPost("/projects", CreateProjectAsync);
        group.MapPut("/projects/{slug}", UpdateProjectAsync);
        group.MapDelete("/projects/{slug}", DeleteProjectAsync);
        group.MapGet("/certificates", GetCertificatesAsync);
        group.MapPost("/certificates", CreateCertificateAsync);
        group.MapPut("/certificates/{slug}", UpdateCertificateAsync);
        group.MapDelete("/certificates/{slug}", DeleteCertificateAsync);

        return endpoints;
    }

    private static IResult GetSession(HttpContext context, IAntiforgery antiforgery)
    {
        var csrf = antiforgery.GetAndStoreTokens(context).RequestToken;
        var email = context.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var displayName = context.User.FindFirstValue(ClaimTypes.Name) ?? email;
        var role = context.User.FindFirstValue(PortfolioAuthentication.AdminRoleClaim) ?? string.Empty;

        return Results.Ok(new
        {
            user = new { email, displayName, role },
            csrfToken = csrf
        });
    }

    private static async Task<IResult> GetProfileAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var database = GetDatabase(context);
        if (database is null)
        {
            return ApiErrors.NotConfigured("قاعدة البيانات غير مهيأة بعد.");
        }

        var profile = await database.Profiles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == 1, cancellationToken);
        return Results.Ok(profile?.ToResponse());
    }

    private static async Task<IResult> UpdateProfileAsync(
        HttpContext context,
        ProfileUpdateRequest request,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        var antiforgeryError = await ValidateAntiforgeryAsync(context, antiforgery);
        if (antiforgeryError is not null)
        {
            return antiforgeryError;
        }

        var database = GetDatabase(context);
        if (database is null)
        {
            return ApiErrors.NotConfigured("قاعدة البيانات غير مهيأة بعد.");
        }

        if (!RequestValidation.TryValidate(request, out ValidatedProfile? profileData, out var error))
        {
            return ApiErrors.BadRequest(error);
        }

        var profile = await database.Profiles
            .SingleOrDefaultAsync(item => item.Id == 1, cancellationToken);
        if (profile is null)
        {
            profile = new PortfolioProfile { Id = 1, CreatedAt = DateTimeOffset.UtcNow };
            database.Profiles.Add(profile);
        }

        profile.FullNameAr = profileData!.FullNameAr;
        profile.FullNameEn = profileData.FullNameEn;
        profile.HeadlineAr = profileData.HeadlineAr;
        profile.HeadlineEn = profileData.HeadlineEn;
        profile.BioAr = profileData.BioAr;
        profile.BioEn = profileData.BioEn;
        profile.LocationAr = profileData.LocationAr;
        profile.LocationEn = profileData.LocationEn;
        profile.Email = profileData.Email;
        profile.LinkedinUrl = profileData.LinkedinUrl;
        profile.PhotoUrl = profileData.PhotoUrl;
        profile.ExpectedGraduationYear = profileData.ExpectedGraduationYear;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await database.SaveChangesAsync(cancellationToken);
        return Results.Ok(profile.ToResponse());
    }

    private static async Task<IResult> GetProjectsAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var database = GetDatabase(context);
        if (database is null)
        {
            return ApiErrors.NotConfigured("قاعدة البيانات غير مهيأة بعد.");
        }

        var projects = await database.Projects
            .AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return Results.Ok(projects.Select(item => item.ToResponse()));
    }

    private static async Task<IResult> CreateProjectAsync(
        HttpContext context,
        ProjectCreateRequest request,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        var antiforgeryError = await ValidateAntiforgeryAsync(context, antiforgery);
        if (antiforgeryError is not null)
        {
            return antiforgeryError;
        }

        var database = GetDatabase(context);
        if (database is null)
        {
            return ApiErrors.NotConfigured("قاعدة البيانات غير مهيأة بعد.");
        }

        if (!RequestValidation.TryValidate(request, out ValidatedProject? projectData, out var error))
        {
            return ApiErrors.BadRequest(error);
        }

        var project = new PortfolioProject
        {
            Id = Guid.NewGuid(),
            Slug = await CreateUniqueSlugAsync(database, projectData!.TitleEn, "project", cancellationToken),
            TitleAr = projectData.TitleAr,
            TitleEn = projectData.TitleEn,
            CategoryAr = projectData.CategoryAr,
            CategoryEn = projectData.CategoryEn,
            SummaryAr = projectData.SummaryAr,
            SummaryEn = projectData.SummaryEn,
            TechnologiesJson = PortfolioMapping.SerializeTechnologies(projectData.Technologies),
            ExternalUrl = projectData.ExternalUrl,
            CoverImageUrl = projectData.CoverImageUrl,
            IsPublished = projectData.IsPublished,
            SortOrder = await GetNextSortOrderAsync(database.Projects, cancellationToken),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        database.Projects.Add(project);
        await database.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/admin/projects/{project.Slug}", project.ToResponse());
    }

    private static async Task<IResult> UpdateProjectAsync(
        HttpContext context,
        string slug,
        ProjectCreateRequest request,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        var antiforgeryError = await ValidateAntiforgeryAsync(context, antiforgery);
        if (antiforgeryError is not null)
        {
            return antiforgeryError;
        }

        var database = GetDatabase(context);
        if (database is null)
        {
            return ApiErrors.NotConfigured("قاعدة البيانات غير مهيأة بعد.");
        }

        if (!TryNormalizeSlug(slug, out var normalizedSlug))
        {
            return ApiErrors.BadRequest("معرّف المشروع غير صحيح.");
        }

        if (!RequestValidation.TryValidate(request, out ValidatedProject? projectData, out var error))
        {
            return ApiErrors.BadRequest(error);
        }

        var project = await database.Projects
            .SingleOrDefaultAsync(item => item.Slug == normalizedSlug, cancellationToken);
        if (project is null)
        {
            return ApiErrors.NotFound("المشروع المطلوب غير موجود.");
        }

        ApplyProjectData(project, projectData!);
        project.UpdatedAt = DateTimeOffset.UtcNow;

        await database.SaveChangesAsync(cancellationToken);
        return Results.Ok(project.ToResponse());
    }

    private static async Task<IResult> DeleteProjectAsync(
        HttpContext context,
        string slug,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        var antiforgeryError = await ValidateAntiforgeryAsync(context, antiforgery);
        if (antiforgeryError is not null)
        {
            return antiforgeryError;
        }

        var database = GetDatabase(context);
        if (database is null)
        {
            return ApiErrors.NotConfigured("قاعدة البيانات غير مهيأة بعد.");
        }

        if (!TryNormalizeSlug(slug, out var normalizedSlug))
        {
            return ApiErrors.BadRequest("معرّف المشروع غير صحيح.");
        }

        var project = await database.Projects
            .SingleOrDefaultAsync(item => item.Slug == normalizedSlug, cancellationToken);
        if (project is null)
        {
            return ApiErrors.NotFound("المشروع المطلوب غير موجود.");
        }

        database.Projects.Remove(project);
        await database.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetCertificatesAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var database = GetDatabase(context);
        if (database is null)
        {
            return ApiErrors.NotConfigured("قاعدة البيانات غير مهيأة بعد.");
        }

        var certificates = await database.Certificates
            .AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return Results.Ok(certificates.Select(item => item.ToResponse()));
    }

    private static async Task<IResult> CreateCertificateAsync(
        HttpContext context,
        CertificateCreateRequest request,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        var antiforgeryError = await ValidateAntiforgeryAsync(context, antiforgery);
        if (antiforgeryError is not null)
        {
            return antiforgeryError;
        }

        var database = GetDatabase(context);
        if (database is null)
        {
            return ApiErrors.NotConfigured("قاعدة البيانات غير مهيأة بعد.");
        }

        if (!RequestValidation.TryValidate(request, out ValidatedCertificate? certificateData, out var error))
        {
            return ApiErrors.BadRequest(error);
        }

        var certificate = new PortfolioCertificate
        {
            Id = Guid.NewGuid(),
            Slug = await CreateUniqueSlugAsync(database, certificateData!.TitleEn, "certificate", cancellationToken),
            TitleAr = certificateData.TitleAr,
            TitleEn = certificateData.TitleEn,
            IssuerAr = certificateData.IssuerAr,
            IssuerEn = certificateData.IssuerEn,
            DescriptionAr = certificateData.DescriptionAr,
            DescriptionEn = certificateData.DescriptionEn,
            IssuedOn = certificateData.IssuedOn,
            CredentialUrl = certificateData.CredentialUrl,
            IsPublished = certificateData.IsPublished,
            SortOrder = await GetNextSortOrderAsync(database.Certificates, cancellationToken),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        database.Certificates.Add(certificate);
        await database.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/admin/certificates/{certificate.Slug}", certificate.ToResponse());
    }

    private static async Task<IResult> UpdateCertificateAsync(
        HttpContext context,
        string slug,
        CertificateCreateRequest request,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        var antiforgeryError = await ValidateAntiforgeryAsync(context, antiforgery);
        if (antiforgeryError is not null)
        {
            return antiforgeryError;
        }

        var database = GetDatabase(context);
        if (database is null)
        {
            return ApiErrors.NotConfigured("قاعدة البيانات غير مهيأة بعد.");
        }

        if (!TryNormalizeSlug(slug, out var normalizedSlug))
        {
            return ApiErrors.BadRequest("معرّف الشهادة غير صحيح.");
        }

        if (!RequestValidation.TryValidate(request, out ValidatedCertificate? certificateData, out var error))
        {
            return ApiErrors.BadRequest(error);
        }

        var certificate = await database.Certificates
            .SingleOrDefaultAsync(item => item.Slug == normalizedSlug, cancellationToken);
        if (certificate is null)
        {
            return ApiErrors.NotFound("الشهادة المطلوبة غير موجودة.");
        }

        ApplyCertificateData(certificate, certificateData!);
        certificate.UpdatedAt = DateTimeOffset.UtcNow;

        await database.SaveChangesAsync(cancellationToken);
        return Results.Ok(certificate.ToResponse());
    }

    private static async Task<IResult> DeleteCertificateAsync(
        HttpContext context,
        string slug,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        var antiforgeryError = await ValidateAntiforgeryAsync(context, antiforgery);
        if (antiforgeryError is not null)
        {
            return antiforgeryError;
        }

        var database = GetDatabase(context);
        if (database is null)
        {
            return ApiErrors.NotConfigured("قاعدة البيانات غير مهيأة بعد.");
        }

        if (!TryNormalizeSlug(slug, out var normalizedSlug))
        {
            return ApiErrors.BadRequest("معرّف الشهادة غير صحيح.");
        }

        var certificate = await database.Certificates
            .SingleOrDefaultAsync(item => item.Slug == normalizedSlug, cancellationToken);
        if (certificate is null)
        {
            return ApiErrors.NotFound("الشهادة المطلوبة غير موجودة.");
        }

        database.Certificates.Remove(certificate);
        await database.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static PortfolioDbContext? GetDatabase(HttpContext context) =>
        context.RequestServices.GetService<PortfolioDbContext>();

    private static async Task<short> GetNextSortOrderAsync<TEntity>(DbSet<TEntity> records, CancellationToken cancellationToken)
        where TEntity : class
    {
        var currentMaximum = await records
            .Select(item => EF.Property<short>(item, "SortOrder"))
            .DefaultIfEmpty((short)0)
            .MaxAsync(cancellationToken);
        return currentMaximum > short.MaxValue - 10 ? short.MaxValue : (short)(currentMaximum + 10);
    }

    private static async Task<string> CreateUniqueSlugAsync(
        PortfolioDbContext database,
        string source,
        string prefix,
        CancellationToken cancellationToken)
    {
        var slug = RequestValidation.CreateSlug(source, prefix);
        var candidate = slug;
        var suffix = 2;

        while (await SlugExistsAsync(database, candidate, cancellationToken))
        {
            candidate = $"{slug}-{suffix++}";
        }

        return candidate;
    }

    private static async Task<bool> SlugExistsAsync(
        PortfolioDbContext database,
        string slug,
        CancellationToken cancellationToken)
    {
        if (await database.Projects.AnyAsync(item => item.Slug == slug, cancellationToken))
        {
            return true;
        }

        return await database.Certificates.AnyAsync(item => item.Slug == slug, cancellationToken);
    }

    private static void ApplyProjectData(PortfolioProject project, ValidatedProject projectData)
    {
        project.TitleAr = projectData.TitleAr;
        project.TitleEn = projectData.TitleEn;
        project.CategoryAr = projectData.CategoryAr;
        project.CategoryEn = projectData.CategoryEn;
        project.SummaryAr = projectData.SummaryAr;
        project.SummaryEn = projectData.SummaryEn;
        project.TechnologiesJson = PortfolioMapping.SerializeTechnologies(projectData.Technologies);
        project.ExternalUrl = projectData.ExternalUrl;
        project.CoverImageUrl = projectData.CoverImageUrl;
        project.IsPublished = projectData.IsPublished;
    }

    private static void ApplyCertificateData(PortfolioCertificate certificate, ValidatedCertificate certificateData)
    {
        certificate.TitleAr = certificateData.TitleAr;
        certificate.TitleEn = certificateData.TitleEn;
        certificate.IssuerAr = certificateData.IssuerAr;
        certificate.IssuerEn = certificateData.IssuerEn;
        certificate.DescriptionAr = certificateData.DescriptionAr;
        certificate.DescriptionEn = certificateData.DescriptionEn;
        certificate.IssuedOn = certificateData.IssuedOn;
        certificate.CredentialUrl = certificateData.CredentialUrl;
        certificate.IsPublished = certificateData.IsPublished;
    }

    private static bool TryNormalizeSlug(string? rawSlug, out string normalizedSlug)
    {
        normalizedSlug = rawSlug?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalizedSlug.Length is > 0 and <= 180 &&
               normalizedSlug.All(character => char.IsAsciiLetterOrDigit(character) || character == '-');
    }

    private static async Task<IResult?> ValidateAntiforgeryAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return null;
        }
        catch (AntiforgeryValidationException)
        {
            return ApiErrors.Forbidden("تعذّر التحقق من طلب الحفظ.");
        }
    }
}
