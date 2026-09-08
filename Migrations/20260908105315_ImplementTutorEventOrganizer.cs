using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Tuition_Systems.Migrations
{
    /// <inheritdoc />
    public partial class ImplementTutorEventOrganizer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProposedRegistrationAudience",
                table: "EventProposals",
                newName: "RegistrationAudience");

            migrationBuilder.RenameColumn(
                name: "PreferredStartsAt",
                table: "EventProposals",
                newName: "StartsAt");

            migrationBuilder.RenameColumn(
                name: "PreferredMode",
                table: "EventProposals",
                newName: "Mode");

            migrationBuilder.RenameColumn(
                name: "PreferredEndsAt",
                table: "EventProposals",
                newName: "EndsAt");

            migrationBuilder.RenameColumn(
                name: "EstimatedParticipants",
                table: "EventProposals",
                newName: "MaxParticipants");

            migrationBuilder.AddColumn<string>(
                name: "OrganizerUserId",
                table: "Events",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApplicationDeadline",
                table: "EventProposals",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "EventProposals",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingPlatform",
                table: "EventProposals",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingUrl",
                table: "EventProposals",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [events]
                SET [events].[OrganizerUserId] = [proposals].[ProposedByUserId]
                FROM [Events] AS [events]
                INNER JOIN [EventProposals] AS [proposals]
                    ON [proposals].[CreatedEventId] = [events].[Id]
                INNER JOIN [Users] AS [users]
                    ON [users].[Email] = [proposals].[ProposedByUserId]
                WHERE [users].[Role] = N'Tutor'
                    AND [events].[OrganizerUserId] IS NULL;

                UPDATE [events]
                SET [events].[OrganizerUserId] = [events].[CreatedByUserId]
                FROM [Events] AS [events]
                INNER JOIN [Users] AS [users]
                    ON [users].[Email] = [events].[CreatedByUserId]
                WHERE [users].[Role] = N'Tutor'
                    AND [events].[OrganizerUserId] IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganizerUserId",
                table: "Events",
                column: "OrganizerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_OrganizerUserId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "OrganizerUserId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ApplicationDeadline",
                table: "EventProposals");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "EventProposals");

            migrationBuilder.DropColumn(
                name: "MeetingPlatform",
                table: "EventProposals");

            migrationBuilder.DropColumn(
                name: "MeetingUrl",
                table: "EventProposals");

            migrationBuilder.RenameColumn(
                name: "StartsAt",
                table: "EventProposals",
                newName: "PreferredStartsAt");

            migrationBuilder.RenameColumn(
                name: "RegistrationAudience",
                table: "EventProposals",
                newName: "ProposedRegistrationAudience");

            migrationBuilder.RenameColumn(
                name: "Mode",
                table: "EventProposals",
                newName: "PreferredMode");

            migrationBuilder.RenameColumn(
                name: "MaxParticipants",
                table: "EventProposals",
                newName: "EstimatedParticipants");

            migrationBuilder.RenameColumn(
                name: "EndsAt",
                table: "EventProposals",
                newName: "PreferredEndsAt");
        }
    }
}
