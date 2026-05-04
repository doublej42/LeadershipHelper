using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadershipHelper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSituationActionOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatorUserId",
                table: "SituationActions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "SituationActions",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "SituationActions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCommunity",
                table: "SituationActions",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatorUserId",
                table: "SituationActions");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "SituationActions");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "SituationActions");

            migrationBuilder.DropColumn(
                name: "IsCommunity",
                table: "SituationActions");
        }
    }
}
