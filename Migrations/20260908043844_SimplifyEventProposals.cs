using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Tuition_Systems.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyEventProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "EventProposals");

            migrationBuilder.DropColumn(
                name: "MeetingPlatform",
                table: "EventProposals");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "EventProposals");

            migrationBuilder.DropColumn(
                name: "ProposedApplicationDeadline",
                table: "EventProposals");

            migrationBuilder.DropColumn(
                name: "ProposedMaxParticipants",
                table: "EventProposals");

            migrationBuilder.DropColumn(
                name: "PublishAsAnnouncement",
                table: "EventProposals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "Mode",
                table: "EventProposals",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProposedApplicationDeadline",
                table: "EventProposals",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProposedMaxParticipants",
                table: "EventProposals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PublishAsAnnouncement",
                table: "EventProposals",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
