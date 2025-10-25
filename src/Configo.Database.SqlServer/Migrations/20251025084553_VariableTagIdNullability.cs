using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Configo.Database.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class VariableTagIdNullability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Variables_Key_TagId",
                table: "Variables");

            migrationBuilder.AlterColumn<int>(
                name: "TagId",
                table: "Variables",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Variables_Key_TagId",
                table: "Variables",
                columns: new[] { "Key", "TagId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Variables_Key_TagId",
                table: "Variables");

            migrationBuilder.AlterColumn<int>(
                name: "TagId",
                table: "Variables",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Variables_Key_TagId",
                table: "Variables",
                columns: new[] { "Key", "TagId" },
                unique: true,
                filter: "[TagId] IS NOT NULL");
        }
    }
}
