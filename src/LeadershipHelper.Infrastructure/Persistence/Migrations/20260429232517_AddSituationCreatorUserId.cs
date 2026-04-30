using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadershipHelper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSituationCreatorUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatorUserId",
                table: "Situations",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatorUserId",
                table: "Situations");
        }
    }
}
