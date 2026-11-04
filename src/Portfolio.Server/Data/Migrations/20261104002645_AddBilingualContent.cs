using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBilingualContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "title",
                table: "certificates",
                newName: "title_en");

            migrationBuilder.RenameColumn(
                name: "issuer",
                table: "certificates",
                newName: "issuer_en");

            migrationBuilder.AddColumn<string>(
                name: "category_en",
                table: "projects",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "summary_en",
                table: "projects",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "title_en",
                table: "projects",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "bio_en",
                table: "profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "headline_en",
                table: "profiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "location_en",
                table: "profiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description_en",
                table: "certificates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "title_ar",
                table: "certificates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "issuer_ar",
                table: "certificates",
                type: "text",
                nullable: false,
                defaultValue: "");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category_en",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "summary_en",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "title_en",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "bio_en",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "headline_en",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "location_en",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "description_en",
                table: "certificates");

            migrationBuilder.DropColumn(
                name: "title_ar",
                table: "certificates");

            migrationBuilder.DropColumn(
                name: "issuer_ar",
                table: "certificates");

            migrationBuilder.RenameColumn(
                name: "title_en",
                table: "certificates",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "issuer_en",
                table: "certificates",
                newName: "issuer");
        }
    }
}
