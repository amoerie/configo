using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Configo.Database.NpgSql.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Variables_Key_TagId",
                table: "Variables",
                columns: new[] { "Key", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_Name",
                table: "Applications",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_Key",
                table: "ApiKeys",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Variables_Key_TagId",
                table: "Variables");

            migrationBuilder.DropIndex(
                name: "IX_Tags_Name",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Applications_Name",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_Key",
                table: "ApiKeys");
        }
    }
}
