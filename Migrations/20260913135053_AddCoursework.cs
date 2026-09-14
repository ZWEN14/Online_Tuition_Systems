using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Tuition_Systems.Migrations
{
    /// <inheritdoc />
    public partial class AddCoursework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseAssignments",
                columns: table => new
                {
                    CourseAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxMarks = table.Column<decimal>(type: "decimal(7,2)", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    AttachmentFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AttachmentStoredName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AttachmentContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AttachmentSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseAssignments", x => x.CourseAssignmentId);
                    table.ForeignKey(
                        name: "FK_CourseAssignments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseSubmissions",
                columns: table => new
                {
                    CourseSubmissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseAssignmentId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    TextResponse = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    AttachmentFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AttachmentStoredName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AttachmentContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AttachmentSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(7,2)", nullable: true),
                    TutorFeedback = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    GradedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSubmissions", x => x.CourseSubmissionId);
                    table.ForeignKey(
                        name: "FK_CourseSubmissions_CourseAssignments_CourseAssignmentId",
                        column: x => x.CourseAssignmentId,
                        principalTable: "CourseAssignments",
                        principalColumn: "CourseAssignmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseSubmissions_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseAssignments_CourseId_IsPublished_DueAtUtc",
                table: "CourseAssignments",
                columns: new[] { "CourseId", "IsPublished", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseSubmissions_CourseAssignmentId_StudentId",
                table: "CourseSubmissions",
                columns: new[] { "CourseAssignmentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseSubmissions_StudentId",
                table: "CourseSubmissions",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseSubmissions");

            migrationBuilder.DropTable(
                name: "CourseAssignments");
        }
    }
}
