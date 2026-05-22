using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Vehicles_LicensePlate",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_PointLedgers_Wallets_WalletId",
                table: "PointLedgers");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Bookings_BookingId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_BookingId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_LicensePlate",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TotalLoyaltyPoints",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "EarnedDate",
                table: "PointLedgers");

            migrationBuilder.RenameColumn(
                name: "MainBalance",
                table: "Wallets",
                newName: "Balance");

            migrationBuilder.RenameColumn(
                name: "TransactionDate",
                table: "Transactions",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "BookingId",
                table: "Transactions",
                newName: "ReferenceBookingId");

            migrationBuilder.RenameColumn(
                name: "WalletId",
                table: "PointLedgers",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "PointsRemaining",
                table: "PointLedgers",
                newName: "PointsDeducted");

            migrationBuilder.RenameColumn(
                name: "ExpirationDate",
                table: "PointLedgers",
                newName: "TransactionDate");

            migrationBuilder.RenameIndex(
                name: "IX_PointLedgers_WalletId",
                table: "PointLedgers",
                newName: "IX_PointLedgers_UserId");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Wallets",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Transactions",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "PointLedgers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "PointLedgers",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ReferenceBookingId",
                table: "PointLedgers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AppliedVoucherId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Bookings",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "FinalAmount",
                table: "Bookings",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                table: "Bookings",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PointDiscountAmount",
                table: "Bookings",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PointsUsed",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Bookings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VoucherDiscountAmount",
                table: "Bookings",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "TimeSlots",
                columns: table => new
                {
                    SlotId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StartTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false),
                    IsVipOnly = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlots", x => x.SlotId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_PointLedgers_Users_UserId",
                table: "PointLedgers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PointLedgers_Users_UserId",
                table: "PointLedgers");

            migrationBuilder.DropTable(
                name: "TimeSlots");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "PointLedgers");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "PointLedgers");

            migrationBuilder.DropColumn(
                name: "ReferenceBookingId",
                table: "PointLedgers");

            migrationBuilder.DropColumn(
                name: "AppliedVoucherId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "FinalAmount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "OriginalPrice",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PointDiscountAmount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PointsUsed",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "VoucherDiscountAmount",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "Balance",
                table: "Wallets",
                newName: "MainBalance");

            migrationBuilder.RenameColumn(
                name: "ReferenceBookingId",
                table: "Transactions",
                newName: "BookingId");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Transactions",
                newName: "TransactionDate");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "PointLedgers",
                newName: "WalletId");

            migrationBuilder.RenameColumn(
                name: "TransactionDate",
                table: "PointLedgers",
                newName: "ExpirationDate");

            migrationBuilder.RenameColumn(
                name: "PointsDeducted",
                table: "PointLedgers",
                newName: "PointsRemaining");

            migrationBuilder.RenameIndex(
                name: "IX_PointLedgers_UserId",
                table: "PointLedgers",
                newName: "IX_PointLedgers_WalletId");

            migrationBuilder.AddColumn<int>(
                name: "TotalLoyaltyPoints",
                table: "Wallets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EarnedDate",
                table: "PointLedgers",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BookingId",
                table: "Transactions",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_LicensePlate",
                table: "Bookings",
                column: "LicensePlate");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Vehicles_LicensePlate",
                table: "Bookings",
                column: "LicensePlate",
                principalTable: "Vehicles",
                principalColumn: "LicensePlate",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PointLedgers_Wallets_WalletId",
                table: "PointLedgers",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "WalletId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Bookings_BookingId",
                table: "Transactions",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "BookingId");
        }
    }
}
