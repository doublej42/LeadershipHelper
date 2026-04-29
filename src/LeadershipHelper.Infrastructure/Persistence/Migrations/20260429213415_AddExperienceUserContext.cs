using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadershipHelper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExperienceUserContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserContext",
                table: "Experiences",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserContext",
                table: "Experiences");
        }
    }
}
