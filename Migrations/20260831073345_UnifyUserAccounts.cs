using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online_Tuition_Systems.Migrations
{
    /// <inheritdoc />
    public partial class UnifyUserAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TemporaryUsers",
                table: "TemporaryUsers");

            migrationBuilder.RenameTable(
                name: "TemporaryUsers",
                newName: "Users");

            migrationBuilder.RenameIndex(
                name: "IX_TemporaryUsers_Email",
                table: "Users",
                newName: "IX_Users_Email");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.Sql(
                """
                UPDATE proposal
                SET proposal.ProposedByUserId = account.Email
                FROM EventProposals AS proposal
                INNER JOIN Users AS account
                    ON proposal.ProposedByUserId =
                        'temporary-user:' + CONVERT(nvarchar(20), account.Id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE proposal
                SET proposal.ProposedByUserId =
                    'temporary-user:' + CONVERT(nvarchar(20), account.Id)
                FROM EventProposals AS proposal
                INNER JOIN Users AS account
                    ON proposal.ProposedByUserId = account.Email;
                """);

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "TemporaryUsers");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Email",
                table: "TemporaryUsers",
                newName: "IX_TemporaryUsers_Email");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TemporaryUsers",
                table: "TemporaryUsers",
                column: "Id");
        }
    }
}
