using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefExpress.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "professional_profiles",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "professional_profiles",
                type: "double",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_professional_profiles_Latitude_Longitude",
                table: "professional_profiles",
                columns: new[] { "Latitude", "Longitude" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_professional_profiles_Latitude_Longitude",
                table: "professional_profiles");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "professional_profiles");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "professional_profiles");
        }
    }
}
