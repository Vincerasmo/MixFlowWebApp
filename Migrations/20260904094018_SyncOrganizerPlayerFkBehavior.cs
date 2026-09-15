using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MixFlowWebApp.Migrations
{
    /// <inheritdoc />
    public partial class SyncOrganizerPlayerFkBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Organizers_OrganizerId",
                table: "Players");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Organizers_OrganizerId",
                table: "Players",
                column: "OrganizerId",
                principalTable: "Organizers",
                principalColumn: "OrganizerId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Organizers_OrganizerId",
                table: "Players");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Organizers_OrganizerId",
                table: "Players",
                column: "OrganizerId",
                principalTable: "Organizers",
                principalColumn: "OrganizerId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
