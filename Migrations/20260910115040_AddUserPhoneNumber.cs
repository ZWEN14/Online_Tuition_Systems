using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Tuition_Systems.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPhoneNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");
        }
    }
}
