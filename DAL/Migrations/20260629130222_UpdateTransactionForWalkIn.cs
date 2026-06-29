using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTransactionForWalkIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Wallets_WalletId",
                table: "Transactions");

            migrationBuilder.AlterColumn<int>(
                name: "WalletId",
                table: "Transactions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Transactions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Wallets_WalletId",
                table: "Transactions",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "WalletId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Wallets_WalletId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Transactions");

            migrationBuilder.AlterColumn<int>(
                name: "WalletId",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Wallets_WalletId",
                table: "Transactions",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "WalletId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
