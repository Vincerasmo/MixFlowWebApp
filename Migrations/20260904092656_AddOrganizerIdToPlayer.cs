using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MixFlowWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizerIdToPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add it nullable first so existing rows aren't rejected outright.
            migrationBuilder.AddColumn<int>(
                name: "OrganizerId",
                table: "Players",
                type: "int",
                nullable: true);

            // 2. Backfill every existing player to the earliest-created organizer account —
            //    the one this legacy test data was presumably created under. Replace the
            //    subquery below with a specific OrganizerId if it should be a different account.
            migrationBuilder.Sql(@"
        UPDATE Players
        SET OrganizerId = (SELECT MIN(OrganizerId) FROM Organizers)
        WHERE OrganizerId IS NULL;
    ");

            // 3. Now that every row has a real value, make it required and enforce the relationship.
            migrationBuilder.AlterColumn<int>(
                name: "OrganizerId",
                table: "Players",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_OrganizerId",
                table: "Players",
                column: "OrganizerId");

            // 🐛 FIX: Cascade here collides with the existing Organizer → Session →
            // SessionPlayer cascade chain (Player also cascades to SessionPlayers), giving
            // SQL Server two different cascade paths into the same table from the same root
            // — which it refuses to create ("may cause cycles or multiple cascade paths").
            // NoAction also means deleting an Organizer with existing Players now fails loudly
            // instead of silently wiping out their lifetime stats (TotalWins, GamesPlayed, etc.).
            migrationBuilder.AddForeignKey(
                name: "FK_Players_Organizers_OrganizerId",
                table: "Players",
                column: "OrganizerId",
                principalTable: "Organizers",
                principalColumn: "OrganizerId",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Organizers_OrganizerId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_OrganizerId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "OrganizerId",
                table: "Players");
        }
    }
}