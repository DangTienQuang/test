using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToDailySlotCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailySlotCapacities_SlotId",
                table: "DailySlotCapacities");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "DailySlotCapacities",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true)
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.CreateIndex(
                name: "IX_DailySlotCapacities_SlotId_Date",
                table: "DailySlotCapacities",
                columns: new[] { "SlotId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailySlotCapacities_SlotId_Date",
                table: "DailySlotCapacities");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RowVersion",
                table: "DailySlotCapacities",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true,
                oldNullable: true)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);

            migrationBuilder.CreateIndex(
                name: "IX_DailySlotCapacities_SlotId",
                table: "DailySlotCapacities",
                column: "SlotId");
        }
    }
}
