using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Tuition_Systems.Migrations
{
    /// <inheritdoc />
    public partial class CreateEventsAndProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApplicationDeadline = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RegistrationAudience = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MeetingPlatform = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    MeetingUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventProposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: true),
                    ProposedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PreferredStartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PreferredEndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ProposedApplicationDeadline = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ProposedRegistrationAudience = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MeetingPlatform = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ProposedMaxParticipants = table.Column<int>(type: "int", nullable: false),
                    PublishAsAnnouncement = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastRevisedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevisionCount = table.Column<int>(type: "int", nullable: false),
                    CreatedEventId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventProposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventProposals_Events_CreatedEventId",
                        column: x => x.CreatedEventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventProposals_CreatedEventId",
                table: "EventProposals",
                column: "CreatedEventId",
                unique: true,
                filter: "[CreatedEventId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventProposals_ProposedByUserId_Status",
                table: "EventProposals",
                columns: new[] { "ProposedByUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EventProposals_Status",
                table: "EventProposals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CourseId_Status_StartsAt",
                table: "Events",
                columns: new[] { "CourseId", "Status", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_RegistrationAudience",
                table: "Events",
                column: "RegistrationAudience");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Status",
                table: "Events",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Announcements_Events_EventId",
                table: "Announcements",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Announcements_Events_EventId",
                table: "Announcements");

            migrationBuilder.DropTable(
                name: "EventProposals");

            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}
