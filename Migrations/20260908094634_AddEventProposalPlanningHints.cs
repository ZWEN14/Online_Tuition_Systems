using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Tuition_Systems.Migrations
{
    /// <inheritdoc />
    public partial class AddEventProposalPlanningHints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstimatedParticipants",
                table: "EventProposals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredMode",
                table: "EventProposals",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedParticipants",
                table: "EventProposals");

            migrationBuilder.DropColumn(
                name: "PreferredMode",
                table: "EventProposals");
        }
    }
}
