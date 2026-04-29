using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeadershipHelper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSeenUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OtpChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Contact = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    ConsumedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpChallenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedSituations",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SituationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SavedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedSituations", x => new { x.UserId, x.SituationId });
                });

            migrationBuilder.CreateTable(
                name: "Situations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsCommunity = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Situations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Experiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SituationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExperienceDateUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DetailsMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DidHelp = table.Column<bool>(type: "bit", nullable: true),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Experiences_Situations_SituationId",
                        column: x => x.SituationId,
                        principalTable: "Situations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SituationActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SituationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromptMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiresTextResponse = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SituationActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SituationActions_Situations_SituationId",
                        column: x => x.SituationId,
                        principalTable: "Situations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExperienceActionStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExperienceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SituationActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDone = table.Column<bool>(type: "bit", nullable: false),
                    ResponseText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastChangedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperienceActionStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExperienceActionStates_Experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalTable: "Experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExperienceActionStates_SituationActions_SituationActionId",
                        column: x => x.SituationActionId,
                        principalTable: "SituationActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_UserId_ExpiresUtc",
                table: "AuthSessions",
                columns: new[] { "UserId", "ExpiresUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExperienceActionStates_ExperienceId_SituationActionId",
                table: "ExperienceActionStates",
                columns: new[] { "ExperienceId", "SituationActionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExperienceActionStates_SituationActionId",
                table: "ExperienceActionStates",
                column: "SituationActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_SituationId",
                table: "Experiences",
                column: "SituationId");

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_UserId_ExperienceDateUtc",
                table: "Experiences",
                columns: new[] { "UserId", "ExperienceDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpChallenges_Contact_CreatedUtc",
                table: "OtpChallenges",
                columns: new[] { "Contact", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SituationActions_SituationId",
                table: "SituationActions",
                column: "SituationId");

            migrationBuilder.CreateIndex(
                name: "IX_Situations_AuthorName",
                table: "Situations",
                column: "AuthorName");

            migrationBuilder.CreateIndex(
                name: "IX_Situations_Title",
                table: "Situations",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthSessions");

            migrationBuilder.DropTable(
                name: "ExperienceActionStates");

            migrationBuilder.DropTable(
                name: "OtpChallenges");

            migrationBuilder.DropTable(
                name: "SavedSituations");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Experiences");

            migrationBuilder.DropTable(
                name: "SituationActions");

            migrationBuilder.DropTable(
                name: "Situations");
        }
    }
}
