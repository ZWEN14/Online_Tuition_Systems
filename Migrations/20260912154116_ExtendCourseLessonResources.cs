using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Tuition_Systems.Migrations
{
    /// <inheritdoc />
    public partial class ExtendCourseLessonResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "CourseLessons",
                type: "nvarchar(max)",
                maxLength: 20000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 20000);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableFromUtc",
                table: "CourseLessons",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalResourceUrl",
                table: "CourseLessons",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceContentType",
                table: "CourseLessons",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceFileName",
                table: "CourseLessons",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ResourceSizeBytes",
                table: "CourseLessons",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceStoredName",
                table: "CourseLessons",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableFromUtc",
                table: "CourseLessons");

            migrationBuilder.DropColumn(
                name: "ExternalResourceUrl",
                table: "CourseLessons");

            migrationBuilder.DropColumn(
                name: "ResourceContentType",
                table: "CourseLessons");

            migrationBuilder.DropColumn(
                name: "ResourceFileName",
                table: "CourseLessons");

            migrationBuilder.DropColumn(
                name: "ResourceSizeBytes",
                table: "CourseLessons");

            migrationBuilder.DropColumn(
                name: "ResourceStoredName",
                table: "CourseLessons");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "CourseLessons",
                type: "nvarchar(max)",
                maxLength: 20000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 20000,
                oldNullable: true);
        }
    }
}
