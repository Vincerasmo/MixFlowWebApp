using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MixFlowWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddLockedPartnerToSessionPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LockedPartnerId",
                table: "SessionPlayers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockedPartnerId",
                table: "SessionPlayers");
        }
    }
}
