using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "admin_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    google_sub = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false, defaultValue: "editor"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "certificates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    issuer = table.Column<string>(type: "text", nullable: false),
                    description_ar = table.Column<string>(type: "text", nullable: false),
                    issued_on = table.Column<DateOnly>(type: "date", nullable: true),
                    credential_url = table.Column<string>(type: "text", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contact_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sender_name = table.Column<string>(type: "text", nullable: false),
                    sender_email = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "new"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    full_name_ar = table.Column<string>(type: "text", nullable: false),
                    full_name_en = table.Column<string>(type: "text", nullable: false),
                    headline_ar = table.Column<string>(type: "text", nullable: false),
                    bio_ar = table.Column<string>(type: "text", nullable: true),
                    location_ar = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    linkedin_url = table.Column<string>(type: "text", nullable: true),
                    photo_url = table.Column<string>(type: "text", nullable: true),
                    expected_graduation_year = table.Column<short>(type: "smallint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    slug = table.Column<string>(type: "text", nullable: false),
                    title_ar = table.Column<string>(type: "text", nullable: false),
                    category_ar = table.Column<string>(type: "text", nullable: false),
                    summary_ar = table.Column<string>(type: "text", nullable: false),
                    technologies = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    external_url = table.Column<string>(type: "text", nullable: true),
                    cover_image_url = table.Column<string>(type: "text", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_users_email",
                table: "admin_users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_admin_users_google_sub",
                table: "admin_users",
                column: "google_sub",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_certificates_is_published_sort_order",
                table: "certificates",
                columns: new[] { "is_published", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_certificates_slug",
                table: "certificates",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contact_messages_status_created_at",
                table: "contact_messages",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_profiles_email",
                table: "profiles",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_projects_is_published_sort_order",
                table: "projects",
                columns: new[] { "is_published", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_projects_slug",
                table: "projects",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_users");

            migrationBuilder.DropTable(
                name: "certificates");

            migrationBuilder.DropTable(
                name: "contact_messages");

            migrationBuilder.DropTable(
                name: "profiles");

            migrationBuilder.DropTable(
                name: "projects");
        }
    }
}
