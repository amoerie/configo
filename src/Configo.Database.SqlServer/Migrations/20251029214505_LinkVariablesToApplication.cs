using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Configo.Database.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class LinkVariablesToApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Variables_Key_TagId",
                table: "Variables");

            migrationBuilder.AddColumn<int>(
                name: "ApplicationId",
                table: "Variables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Variables_ApplicationId",
                table: "Variables",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Variables_Key_TagId_ApplicationId",
                table: "Variables",
                columns: new[] { "Key", "TagId", "ApplicationId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Variables_Applications_ApplicationId",
                table: "Variables",
                column: "ApplicationId",
                principalTable: "Applications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Variables_Applications_ApplicationId",
                table: "Variables");

            migrationBuilder.DropIndex(
                name: "IX_Variables_ApplicationId",
                table: "Variables");

            migrationBuilder.DropIndex(
                name: "IX_Variables_Key_TagId_ApplicationId",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "Variables");

            migrationBuilder.CreateIndex(
                name: "IX_Variables_Key_TagId",
                table: "Variables",
                columns: new[] { "Key", "TagId" },
                unique: true);
        }
    }
}
