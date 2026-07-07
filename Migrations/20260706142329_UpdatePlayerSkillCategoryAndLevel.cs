using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MixFlowWebApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlayerSkillCategoryAndLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DUPR",
                table: "Players");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DUPR",
                table: "Players",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
