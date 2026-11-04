using Microsoft.EntityFrameworkCore;
using RayanPortfolio.Server.Services;

namespace RayanPortfolio.Server.Data;

public static class InitialPortfolioDataSeeder
{
    private const string LegacyInitialHeadline = "طالب نظم معلومات حاسوبية ومطور برمجيات";
    private const string InitialHeadlineAr = "مطور تطبيقات ويب وحلول برمجية متكاملة";
    private const string InitialHeadlineEn = "Full-Stack Web Developer";
    private const string LegacyInitialBio = "أبني حلولًا رقمية تجمع بين المنتج والبيانات والتجربة الواضحة.";
    private const string InitialBioAr = "أطوّر تطبيقات ويب متكاملة وأعمل على قواعد البيانات والخدمات الخلفية، مع اهتمام بتحويل المتطلبات إلى تجربة عملية واضحة قابلة للتوسع.";
    private const string InitialBioEn = "I build integrated web applications and work with databases and backend services, focusing on turning requirements into clear, scalable practical experiences.";
    private const string InitialLocationAr = "المدينة المنورة، المملكة العربية السعودية";
    private const string InitialLocationEn = "Madinah, Saudi Arabia";

    public static async Task SeedIfMissingAsync(
        PortfolioDbContext database,
        CancellationToken cancellationToken = default)
    {
        var timestamp = DateTimeOffset.UtcNow;

        var profile = await database.Profiles
            .SingleOrDefaultAsync(item => item.Id == 1, cancellationToken);

        if (profile is null)
        {
            database.Profiles.Add(new PortfolioProfile
            {
                Id = 1,
                FullNameAr = "ريان صالح الحربي",
                FullNameEn = "Rayan Saleh Alharbi",
                HeadlineAr = InitialHeadlineAr,
                HeadlineEn = InitialHeadlineEn,
                BioAr = InitialBioAr,
                BioEn = InitialBioEn,
                LocationAr = InitialLocationAr,
                LocationEn = InitialLocationEn,
                Email = "Rayanalharbi272@gmail.com",
                LinkedinUrl = "https://www.linkedin.com/in/rayan-alharbi-617b1a3a6",
                PhotoUrl = "/rayan-alharbi-portrait.jpeg",
                ExpectedGraduationYear = 2027,
                CreatedAt = timestamp,
                UpdatedAt = timestamp
            });
        }
        else
        {
            var profileWasUpdated = false;

            if (string.Equals(profile.HeadlineAr, LegacyInitialHeadline, StringComparison.Ordinal))
            {
                profile.HeadlineAr = InitialHeadlineAr;
                profileWasUpdated = true;
            }

            if (string.Equals(profile.BioAr, LegacyInitialBio, StringComparison.Ordinal))
            {
                profile.BioAr = InitialBioAr;
                profileWasUpdated = true;
            }

            if (string.IsNullOrWhiteSpace(profile.HeadlineEn))
            {
                profile.HeadlineEn = InitialHeadlineEn;
                profileWasUpdated = true;
            }

            if (string.IsNullOrWhiteSpace(profile.BioEn))
            {
                profile.BioEn = InitialBioEn;
                profileWasUpdated = true;
            }

            if (string.IsNullOrWhiteSpace(profile.LocationEn))
            {
                profile.LocationEn = InitialLocationEn;
                profileWasUpdated = true;
            }

            if (profileWasUpdated)
            {
                profile.UpdatedAt = timestamp;
            }
        }

        var initialProjects = CreateInitialProjects(timestamp);
        var existingProjects = (await database.Projects
                .ToListAsync(cancellationToken))
            .ToDictionary(project => project.Slug, StringComparer.Ordinal);

        foreach (var project in initialProjects)
        {
            if (!existingProjects.TryGetValue(project.Slug, out var existingProject))
            {
                database.Projects.Add(project);
                continue;
            }

            ApplyMissingProjectTranslations(existingProject, project, timestamp);
        }

        var initialCertificates = CreateInitialCertificates(timestamp);
        var existingCertificates = (await database.Certificates
                .ToListAsync(cancellationToken))
            .ToDictionary(certificate => certificate.Slug, StringComparer.Ordinal);

        foreach (var certificate in initialCertificates)
        {
            if (!existingCertificates.TryGetValue(certificate.Slug, out var existingCertificate))
            {
                database.Certificates.Add(certificate);
                continue;
            }

            ApplyMissingCertificateTranslations(existingCertificate, certificate, timestamp);
        }

        if (database.ChangeTracker.HasChanges())
        {
            await database.SaveChangesAsync(cancellationToken);
        }
    }

    private static void ApplyMissingProjectTranslations(
        PortfolioProject project,
        PortfolioProject initialProject,
        DateTimeOffset timestamp)
    {
        var projectWasUpdated = false;

        if (string.IsNullOrWhiteSpace(project.TitleEn))
        {
            project.TitleEn = initialProject.TitleEn;
            projectWasUpdated = true;
        }

        if (string.IsNullOrWhiteSpace(project.CategoryEn))
        {
            project.CategoryEn = initialProject.CategoryEn;
            projectWasUpdated = true;
        }

        if (string.IsNullOrWhiteSpace(project.SummaryEn))
        {
            project.SummaryEn = initialProject.SummaryEn;
            projectWasUpdated = true;
        }

        if (projectWasUpdated)
        {
            project.UpdatedAt = timestamp;
        }
    }

    private static void ApplyMissingCertificateTranslations(
        PortfolioCertificate certificate,
        PortfolioCertificate initialCertificate,
        DateTimeOffset timestamp)
    {
        var certificateWasUpdated = false;

        if (string.IsNullOrWhiteSpace(certificate.TitleAr))
        {
            certificate.TitleAr = initialCertificate.TitleAr;
            certificateWasUpdated = true;
        }

        if (string.IsNullOrWhiteSpace(certificate.TitleEn))
        {
            certificate.TitleEn = initialCertificate.TitleEn;
            certificateWasUpdated = true;
        }

        if (string.IsNullOrWhiteSpace(certificate.IssuerAr))
        {
            certificate.IssuerAr = initialCertificate.IssuerAr;
            certificateWasUpdated = true;
        }

        if (string.IsNullOrWhiteSpace(certificate.IssuerEn))
        {
            certificate.IssuerEn = initialCertificate.IssuerEn;
            certificateWasUpdated = true;
        }

        if (string.IsNullOrWhiteSpace(certificate.DescriptionEn))
        {
            certificate.DescriptionEn = initialCertificate.DescriptionEn;
            certificateWasUpdated = true;
        }

        if (certificateWasUpdated)
        {
            certificate.UpdatedAt = timestamp;
        }
    }

    private static IReadOnlyList<PortfolioProject> CreateInitialProjects(DateTimeOffset timestamp) =>
    [
        CreateProject(
            "student-performance-analytics",
            "تحليلات الأداء الأكاديمي للطلاب",
            "Student Academic Performance Analytics",
            "تحليل بيانات وتنقيب بيانات",
            "Data Analytics & Data Mining",
            "مشروع مستودع بيانات وتنقيب بيانات حلّل محددات الأداء الأكاديمي اعتمادًا على 6,607 سجلات و20 خاصية، مع معالجة البيانات الناقصة وعدم توازن الفئات.",
            "A data warehouse and data mining project that analysed factors affecting academic performance using 6,607 records and 20 features, including missing-data handling and class-imbalance treatment.",
            ["Python", "Scikit-learn", "Pandas", "Data Visualization"],
            "https://drive.google.com/drive/folders/1F4AhHa_Bn77G_ZPqcFpe4QJtS0hIYTox?usp=drive_link",
            10,
            timestamp),
        CreateProject(
            "car-sales-booking-platform",
            "منصة مبيعات وحجز السيارات",
            "Car Sales and Booking Platform",
            "تطبيق ويب وجوال متكامل",
            "Integrated Web & Mobile Application",
            "تطبيق ويب وجوال متكامل لإدارة مبيعات السيارات والحجوزات، مع خلفية .NET Core وقاعدة SQL Server وتطبيق .NET MAUI متصل بالبيانات والخدمات نفسها.",
            "An integrated web and mobile application for managing car sales and bookings, with a .NET Core backend, SQL Server database, and a .NET MAUI client connected to the same data and services.",
            [".NET Core", "SQL Server", ".NET MAUI", "APIs"],
            "https://drive.google.com/drive/folders/1WDSQWvHcaYM8iul6sgvw9m6q7Xuvx9Wm?usp=drive_link",
            20,
            timestamp)
    ];

    private static IReadOnlyList<PortfolioCertificate> CreateInitialCertificates(DateTimeOffset timestamp) =>
    [
        CreateCertificate("ibm-lifelong-professional-skills", "المهارات المهنية المستمرة", "Lifelong Professional Skills", "IBM SkillsBuild", "IBM SkillsBuild", "إتمام برنامج المهارات المهنية المستمرة، الذي يركز على تطوير مهارات العمل والتواصل والتعلم المهني.", "Completed a professional skills program focused on workplace, communication, and lifelong-learning skills.", new DateOnly(2026, 7, 19), null, 10, timestamp),
        CreateCertificate("robotics-bootcamp", "شهادات معسكر الروبوتات", "Robotics Bootcamp Certificates", "معسكر الروبوتات", "Robotics Bootcamp", "مجموعة شهادات متخصصة في أساسيات الروبوتات والأنظمة الذكية وتطبيقاتها العملية.", "A set of specialized certificates covering robotics fundamentals, intelligent systems, and practical applications.", null, "/certificates/Robotics%20Bootcamp%20Certificates%20-%20RAYAN%20SALEH%20ALHARBI.pdf", 20, timestamp),
        CreateCertificate("introduction-to-robotics", "مقدمة في الروبوتات", "Introduction to Robotics", "الروبوتات", "Robotics", "مدخل إلى مفاهيم الروبوتات ومكوّناتها وطريقة عمل الأنظمة الروبوتية.", "An introduction to robotics concepts, components, and the operation of robotic systems.", null, "/certificates/Introduction%20to%20robotics%20-%20Rayan%20Saleh%20Alharbi.pdf", 30, timestamp),
        CreateCertificate("generative-ai-agents-robotics", "من الذكاء الاصطناعي التوليدي إلى الوكلاء والروبوتات", "From Generative AI to Agents & Robotics", "الذكاء الاصطناعي والروبوتات", "AI & Robotics", "استكشاف انتقال الذكاء الاصطناعي التوليدي إلى الوكلاء والأنظمة الروبوتية.", "Explored the progression from generative AI to agents and robotic systems.", null, "/certificates/From%20Generative%20AI%20to%20Agents%20%26%20Robotics%20-%20Rayan%20Saleh%20Alharbi.pdf", 40, timestamp),
        CreateCertificate("robotics-security", "أمن الروبوتات", "Robotics Security", "أمن الروبوتات", "Robotics Security", "مبادئ حماية الأنظمة الروبوتية والاعتبارات الأمنية في بيئاتها المتصلة.", "Learned principles for protecting robotic systems and addressing security considerations in connected environments.", null, "/certificates/Robotics%20security%20-%20Rayan%20Saleh%20Alharbi.pdf", 50, timestamp),
        CreateCertificate("uav-swarm-deployment", "نشر أسراب الطائرات المسيّرة", "UAV Swarm Deployment", "أنظمة الطائرات المسيّرة", "UAV Systems", "تطبيقات نشر أسراب الطائرات المسيّرة والتنسيق بين الأنظمة ذاتية التشغيل.", "Explored UAV swarm deployment and coordination across autonomous systems.", null, "/certificates/UAV%20Swarm%20Deployment%20-%20Rayan%20Saleh%20Alharbi.pdf", 60, timestamp),
        CreateCertificate("bio-inspired-swarms", "من روبوت واحد إلى أسراب مستوحاة من الأنظمة الحيوية", "From One Robot to Bio-Inspired Swarms", "أنظمة مستوحاة من الطبيعة", "Bio-Inspired Systems", "مفاهيم الانتقال من الروبوتات الفردية إلى أسراب مستوحاة من الأنظمة الحيوية.", "Explored the transition from individual robots to swarms inspired by biological systems.", null, "/certificates/from%20one%20robot%20to%20bio%20inspired%20swarms%20-%20Rayan%20Saleh%20Alharbi.pdf", 70, timestamp)
    ];

    private static PortfolioProject CreateProject(
        string slug,
        string titleAr,
        string titleEn,
        string categoryAr,
        string categoryEn,
        string summaryAr,
        string summaryEn,
        IReadOnlyList<string> technologies,
        string externalUrl,
        short sortOrder,
        DateTimeOffset timestamp) =>
        new()
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            TitleAr = titleAr,
            TitleEn = titleEn,
            CategoryAr = categoryAr,
            CategoryEn = categoryEn,
            SummaryAr = summaryAr,
            SummaryEn = summaryEn,
            TechnologiesJson = PortfolioMapping.SerializeTechnologies(technologies),
            ExternalUrl = externalUrl,
            IsPublished = true,
            SortOrder = sortOrder,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

    private static PortfolioCertificate CreateCertificate(
        string slug,
        string titleAr,
        string titleEn,
        string issuerAr,
        string issuerEn,
        string descriptionAr,
        string descriptionEn,
        DateOnly? issuedOn,
        string? credentialUrl,
        short sortOrder,
        DateTimeOffset timestamp) =>
        new()
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            TitleAr = titleAr,
            TitleEn = titleEn,
            IssuerAr = issuerAr,
            IssuerEn = issuerEn,
            DescriptionAr = descriptionAr,
            DescriptionEn = descriptionEn,
            IssuedOn = issuedOn,
            CredentialUrl = credentialUrl,
            IsPublished = true,
            SortOrder = sortOrder,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
}
