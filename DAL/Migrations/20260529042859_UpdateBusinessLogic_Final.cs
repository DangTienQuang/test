using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBusinessLogic_Final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrustScorePenalty",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "TrustScore",
                table: "CustomerProfiles",
                newName: "CurrentYearTierPoints");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CurrentYearTierPoints",
                table: "CustomerProfiles",
                newName: "TrustScore");

            migrationBuilder.AddColumn<int>(
                name: "TrustScorePenalty",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
