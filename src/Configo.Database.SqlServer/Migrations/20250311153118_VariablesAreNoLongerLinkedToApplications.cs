using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Configo.Database.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class VariablesAreNoLongerLinkedToApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationVariables");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationVariables",
                columns: table => new
                {
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    VariableId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationVariables", x => new { x.ApplicationId, x.VariableId });
                    table.ForeignKey(
                        name: "FK_ApplicationVariables_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationVariables_Variables_VariableId",
                        column: x => x.VariableId,
                        principalTable: "Variables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationVariables_VariableId",
                table: "ApplicationVariables",
                column: "VariableId");
        }
    }
}
