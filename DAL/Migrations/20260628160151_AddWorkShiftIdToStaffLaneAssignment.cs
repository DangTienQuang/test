using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkShiftIdToStaffLaneAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkShiftId",
                table: "StaffLaneAssignments",
                type: "int",
                nullable: false,
                defaultValue: 1); // Defaulting to 1 to avoid foreign key constraints errors for existing data.

            migrationBuilder.CreateIndex(
                name: "IX_StaffLaneAssignments_WorkShiftId",
                table: "StaffLaneAssignments",
                column: "WorkShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffLaneAssignments_WorkShifts_WorkShiftId",
                table: "StaffLaneAssignments",
                column: "WorkShiftId",
                principalTable: "WorkShifts",
                principalColumn: "WorkShiftId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaffLaneAssignments_WorkShifts_WorkShiftId",
                table: "StaffLaneAssignments");

            migrationBuilder.DropIndex(
                name: "IX_StaffLaneAssignments_WorkShiftId",
                table: "StaffLaneAssignments");

            migrationBuilder.DropColumn(
                name: "WorkShiftId",
                table: "StaffLaneAssignments");
        }
    }
}
