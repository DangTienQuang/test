using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class CreateVehicleTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailySlotCapacities_SlotId_Date",
                table: "DailySlotCapacities");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "TimeSlots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "ServicePrices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "DailySlotCapacities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "BookingDetails",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_BranchId",
                table: "TimeSlots",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrices_BranchId",
                table: "ServicePrices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_DailySlotCapacities_BranchId",
                table: "DailySlotCapacities",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_DailySlotCapacities_SlotId_Date_BranchId",
                table: "DailySlotCapacities",
                columns: new[] { "SlotId", "Date", "BranchId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DailySlotCapacities_Branches_BranchId",
                table: "DailySlotCapacities",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServicePrices_Branches_BranchId",
                table: "ServicePrices",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeSlots_Branches_BranchId",
                table: "TimeSlots",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailySlotCapacities_Branches_BranchId",
                table: "DailySlotCapacities");

            migrationBuilder.DropForeignKey(
                name: "FK_ServicePrices_Branches_BranchId",
                table: "ServicePrices");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeSlots_Branches_BranchId",
                table: "TimeSlots");

            migrationBuilder.DropIndex(
                name: "IX_TimeSlots_BranchId",
                table: "TimeSlots");

            migrationBuilder.DropIndex(
                name: "IX_ServicePrices_BranchId",
                table: "ServicePrices");

            migrationBuilder.DropIndex(
                name: "IX_DailySlotCapacities_BranchId",
                table: "DailySlotCapacities");

            migrationBuilder.DropIndex(
                name: "IX_DailySlotCapacities_SlotId_Date_BranchId",
                table: "DailySlotCapacities");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ServicePrices");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "DailySlotCapacities");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BookingDetails");

            migrationBuilder.CreateIndex(
                name: "IX_DailySlotCapacities_SlotId_Date",
                table: "DailySlotCapacities",
                columns: new[] { "SlotId", "Date" },
                unique: true);
        }
    }
}
