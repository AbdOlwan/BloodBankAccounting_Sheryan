using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodBankAccounting.Migrations
{
    /// <inheritdoc />
    public partial class EditInvoce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BloodStockId",
                table: "InvoiceLines",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_BloodStockId",
                table: "InvoiceLines",
                column: "BloodStockId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLines_BloodStocks_BloodStockId",
                table: "InvoiceLines",
                column: "BloodStockId",
                principalTable: "BloodStocks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLines_BloodStocks_BloodStockId",
                table: "InvoiceLines");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceLines_BloodStockId",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "BloodStockId",
                table: "InvoiceLines");
        }
    }
}
