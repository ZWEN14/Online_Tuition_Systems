using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Tuition_Systems.Migrations
{
    /// <inheritdoc />
    public partial class RefineCourseworkGrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "MaxMarks",
                table: "CourseAssignments",
                type: "decimal(7,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(7,2)");

            migrationBuilder.AddColumn<bool>(
                name: "IsGraded",
                table: "CourseAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGraded",
                table: "CourseAssignments");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxMarks",
                table: "CourseAssignments",
                type: "decimal(7,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(7,2)",
                oldNullable: true);
        }
    }
}
