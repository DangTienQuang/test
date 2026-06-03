using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCRMFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Vouchers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "RequiredTierId",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ValidEndTime",
                table: "Vouchers",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ValidStartTime",
                table: "Vouchers",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoucherType",
                table: "Vouchers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "CustomerProfiles",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_RequiredTierId",
                table: "Vouchers",
                column: "RequiredTierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Tiers_RequiredTierId",
                table: "Vouchers",
                column: "RequiredTierId",
                principalTable: "Tiers",
                principalColumn: "TierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Tiers_RequiredTierId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_RequiredTierId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "RequiredTierId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ValidEndTime",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ValidStartTime",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "VoucherType",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "CustomerProfiles");
        }
    }
}
