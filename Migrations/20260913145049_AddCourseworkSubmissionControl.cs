using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Tuition_Systems.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseworkSubmissionControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubmissionsClosedAtUtc",
                table: "CourseAssignments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmissionsClosedAtUtc",
                table: "CourseAssignments");
        }
    }
}
