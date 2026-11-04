using Microsoft.EntityFrameworkCore;

namespace RayanPortfolio.Server.Data;

public sealed class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : DbContext(options)
{
    public DbSet<PortfolioProfile> Profiles => Set<PortfolioProfile>();

    public DbSet<PortfolioProject> Projects => Set<PortfolioProject>();

    public DbSet<PortfolioCertificate> Certificates => Set<PortfolioCertificate>();

    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");
        ConfigureProfile(modelBuilder);
        ConfigureProjects(modelBuilder);
        ConfigureCertificates(modelBuilder);
        ConfigureContactMessages(modelBuilder);
        ConfigureAdminUsers(modelBuilder);
    }

    private static void ConfigureProfile(ModelBuilder modelBuilder)
    {
        var profile = modelBuilder.Entity<PortfolioProfile>();
        profile.ToTable("profiles");
        profile.HasKey(item => item.Id);
        profile.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        profile.Property(item => item.FullNameAr).HasColumnName("full_name_ar").HasColumnType("text");
        profile.Property(item => item.FullNameEn).HasColumnName("full_name_en").HasColumnType("text");
        profile.Property(item => item.HeadlineAr).HasColumnName("headline_ar").HasColumnType("text");
        profile.Property(item => item.HeadlineEn).HasColumnName("headline_en").HasColumnType("text");
        profile.Property(item => item.BioAr).HasColumnName("bio_ar").HasColumnType("text");
        profile.Property(item => item.BioEn).HasColumnName("bio_en").HasColumnType("text");
        profile.Property(item => item.LocationAr).HasColumnName("location_ar").HasColumnType("text");
        profile.Property(item => item.LocationEn).HasColumnName("location_en").HasColumnType("text");
        profile.Property(item => item.Email).HasColumnName("email").HasColumnType("text");
        profile.Property(item => item.LinkedinUrl).HasColumnName("linkedin_url").HasColumnType("text");
        profile.Property(item => item.PhotoUrl).HasColumnName("photo_url").HasColumnType("text");
        profile.Property(item => item.ExpectedGraduationYear).HasColumnName("expected_graduation_year");
        profile.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        profile.Property(item => item.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        profile.HasIndex(item => item.Email).IsUnique();
    }

    private static void ConfigureProjects(ModelBuilder modelBuilder)
    {
        var project = modelBuilder.Entity<PortfolioProject>();
        project.ToTable("projects");
        project.HasKey(item => item.Id);
        project.Property(item => item.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        project.Property(item => item.Slug).HasColumnName("slug").HasColumnType("text");
        project.Property(item => item.TitleAr).HasColumnName("title_ar").HasColumnType("text");
        project.Property(item => item.TitleEn).HasColumnName("title_en").HasColumnType("text");
        project.Property(item => item.CategoryAr).HasColumnName("category_ar").HasColumnType("text");
        project.Property(item => item.CategoryEn).HasColumnName("category_en").HasColumnType("text");
        project.Property(item => item.SummaryAr).HasColumnName("summary_ar").HasColumnType("text");
        project.Property(item => item.SummaryEn).HasColumnName("summary_en").HasColumnType("text");
        project.Property(item => item.TechnologiesJson).HasColumnName("technologies").HasColumnType("jsonb").HasDefaultValue("[]");
        project.Property(item => item.ExternalUrl).HasColumnName("external_url").HasColumnType("text");
        project.Property(item => item.CoverImageUrl).HasColumnName("cover_image_url").HasColumnType("text");
        project.Property(item => item.IsPublished).HasColumnName("is_published").HasDefaultValue(true);
        project.Property(item => item.SortOrder).HasColumnName("sort_order").HasDefaultValue((short)0);
        project.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        project.Property(item => item.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        project.HasIndex(item => item.Slug).IsUnique();
        project.HasIndex(item => new { item.IsPublished, item.SortOrder });
    }

    private static void ConfigureCertificates(ModelBuilder modelBuilder)
    {
        var certificate = modelBuilder.Entity<PortfolioCertificate>();
        certificate.ToTable("certificates");
        certificate.HasKey(item => item.Id);
        certificate.Property(item => item.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        certificate.Property(item => item.Slug).HasColumnName("slug").HasColumnType("text");
        certificate.Property(item => item.TitleAr).HasColumnName("title_ar").HasColumnType("text");
        certificate.Property(item => item.TitleEn).HasColumnName("title_en").HasColumnType("text");
        certificate.Property(item => item.IssuerAr).HasColumnName("issuer_ar").HasColumnType("text");
        certificate.Property(item => item.IssuerEn).HasColumnName("issuer_en").HasColumnType("text");
        certificate.Property(item => item.DescriptionAr).HasColumnName("description_ar").HasColumnType("text");
        certificate.Property(item => item.DescriptionEn).HasColumnName("description_en").HasColumnType("text");
        certificate.Property(item => item.IssuedOn).HasColumnName("issued_on");
        certificate.Property(item => item.CredentialUrl).HasColumnName("credential_url").HasColumnType("text");
        certificate.Property(item => item.IsPublished).HasColumnName("is_published").HasDefaultValue(true);
        certificate.Property(item => item.SortOrder).HasColumnName("sort_order").HasDefaultValue((short)0);
        certificate.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        certificate.Property(item => item.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        certificate.HasIndex(item => item.Slug).IsUnique();
        certificate.HasIndex(item => new { item.IsPublished, item.SortOrder });
    }

    private static void ConfigureContactMessages(ModelBuilder modelBuilder)
    {
        var message = modelBuilder.Entity<ContactMessage>();
        message.ToTable("contact_messages");
        message.HasKey(item => item.Id);
        message.Property(item => item.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        message.Property(item => item.SenderName).HasColumnName("sender_name").HasColumnType("text");
        message.Property(item => item.SenderEmail).HasColumnName("sender_email").HasColumnType("text");
        message.Property(item => item.Message).HasColumnName("message").HasColumnType("text");
        message.Property(item => item.Status).HasColumnName("status").HasColumnType("text").HasDefaultValue("new");
        message.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        message.HasIndex(item => new { item.Status, item.CreatedAt });
    }

    private static void ConfigureAdminUsers(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<AdminUser>();
        user.ToTable("admin_users");
        user.HasKey(item => item.Id);
        user.Property(item => item.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        user.Property(item => item.GoogleSub).HasColumnName("google_sub").HasColumnType("text");
        user.Property(item => item.Email).HasColumnName("email").HasColumnType("text");
        user.Property(item => item.DisplayName).HasColumnName("display_name").HasColumnType("text");
        user.Property(item => item.Role).HasColumnName("role").HasColumnType("text").HasDefaultValue(AdminRoles.Editor);
        user.Property(item => item.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        user.Property(item => item.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        user.Property(item => item.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        user.HasIndex(item => item.GoogleSub).IsUnique();
        user.HasIndex(item => item.Email).IsUnique();
    }
}
