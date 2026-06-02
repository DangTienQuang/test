using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCarModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CarModelId",
                table: "Vehicles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CarModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Brand = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarModels", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CarModelId",
                table: "Vehicles",
                column: "CarModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_CarModels_CarModelId",
                table: "Vehicles",
                column: "CarModelId",
                principalTable: "CarModels",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_CarModels_CarModelId",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "CarModels");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_CarModelId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CarModelId",
                table: "Vehicles");
        }
    }
}
