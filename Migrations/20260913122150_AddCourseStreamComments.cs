using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Tuition_Systems.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseStreamComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseStreamComments",
                columns: table => new
                {
                    CourseStreamCommentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnnouncementId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseStreamComments", x => x.CourseStreamCommentId);
                    table.ForeignKey(
                        name: "FK_CourseStreamComments_Announcements_AnnouncementId",
                        column: x => x.AnnouncementId,
                        principalTable: "Announcements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseStreamComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseStreamComments_AnnouncementId_CreatedAtUtc",
                table: "CourseStreamComments",
                columns: new[] { "AnnouncementId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseStreamComments_UserId",
                table: "CourseStreamComments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseStreamComments");
        }
    }
}
