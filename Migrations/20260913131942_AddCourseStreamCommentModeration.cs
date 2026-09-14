using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Tuition_Systems.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseStreamCommentModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRemovedByTutor",
                table: "CourseStreamComments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RemovedAtUtc",
                table: "CourseStreamComments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemovedByTutorName",
                table: "CourseStreamComments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRemovedByTutor",
                table: "CourseStreamComments");

            migrationBuilder.DropColumn(
                name: "RemovedAtUtc",
                table: "CourseStreamComments");

            migrationBuilder.DropColumn(
                name: "RemovedByTutorName",
                table: "CourseStreamComments");
        }
    }
}
