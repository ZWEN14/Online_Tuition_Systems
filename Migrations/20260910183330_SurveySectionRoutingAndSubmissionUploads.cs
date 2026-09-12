using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Tuition_Systems.Migrations
{
    /// <inheritdoc />
    public partial class SurveySectionRoutingAndSubmissionUploads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AfterSectionAction",
                table: "SurveySections",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "NextSectionId",
                table: "SurveySections",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubmissionAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    SurveyAnswerId = table.Column<int>(type: "int", nullable: true),
                    ComplaintId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionAttachments", x => x.Id);
                    table.CheckConstraint("CK_SubmissionAttachment_Owner", "([SurveyAnswerId] IS NOT NULL AND [ComplaintId] IS NULL) OR ([SurveyAnswerId] IS NULL AND [ComplaintId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_SubmissionAttachments_Complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "Complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubmissionAttachments_SurveyAnswers_SurveyAnswerId",
                        column: x => x.SurveyAnswerId,
                        principalTable: "SurveyAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SurveySections_NextSectionId",
                table: "SurveySections",
                column: "NextSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionAttachments_ComplaintId",
                table: "SubmissionAttachments",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionAttachments_SurveyAnswerId",
                table: "SubmissionAttachments",
                column: "SurveyAnswerId");

            migrationBuilder.AddForeignKey(
                name: "FK_SurveySections_SurveySections_NextSectionId",
                table: "SurveySections",
                column: "NextSectionId",
                principalTable: "SurveySections",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SurveySections_SurveySections_NextSectionId",
                table: "SurveySections");

            migrationBuilder.DropTable(
                name: "SubmissionAttachments");

            migrationBuilder.DropIndex(
                name: "IX_SurveySections_NextSectionId",
                table: "SurveySections");

            migrationBuilder.DropColumn(
                name: "AfterSectionAction",
                table: "SurveySections");

            migrationBuilder.DropColumn(
                name: "NextSectionId",
                table: "SurveySections");
        }
    }
}
