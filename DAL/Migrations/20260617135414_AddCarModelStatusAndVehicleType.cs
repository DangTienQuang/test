using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCarModelStatusAndVehicleType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestedByUserId",
                table: "CarModels",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CarModels",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Approved")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "VehicleTypeId",
                table: "CarModels",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarModels_RequestedByUserId",
                table: "CarModels",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CarModels_VehicleTypeId",
                table: "CarModels",
                column: "VehicleTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_CarModels_Users_RequestedByUserId",
                table: "CarModels",
                column: "RequestedByUserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CarModels_VehicleTypes_VehicleTypeId",
                table: "CarModels",
                column: "VehicleTypeId",
                principalTable: "VehicleTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarModels_Users_RequestedByUserId",
                table: "CarModels");

            migrationBuilder.DropForeignKey(
                name: "FK_CarModels_VehicleTypes_VehicleTypeId",
                table: "CarModels");

            migrationBuilder.DropIndex(
                name: "IX_CarModels_RequestedByUserId",
                table: "CarModels");

            migrationBuilder.DropIndex(
                name: "IX_CarModels_VehicleTypeId",
                table: "CarModels");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "CarModels");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CarModels");

            migrationBuilder.DropColumn(
                name: "VehicleTypeId",
                table: "CarModels");
        }
    }
}
