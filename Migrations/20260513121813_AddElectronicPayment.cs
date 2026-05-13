using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodBankAccounting.Migrations
{
    /// <inheritdoc />
    public partial class AddElectronicPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ElectronicPaymentDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Method = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromAccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ToAccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TransactionDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: true),
                    ReceiptVoucherId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectronicPaymentDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectronicPaymentDetails_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ElectronicPaymentDetails_ReceiptVouchers_ReceiptVoucherId",
                        column: x => x.ReceiptVoucherId,
                        principalTable: "ReceiptVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicPaymentDetails_InvoiceId",
                table: "ElectronicPaymentDetails",
                column: "InvoiceId",
                unique: true,
                filter: "[InvoiceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicPaymentDetails_ReceiptVoucherId",
                table: "ElectronicPaymentDetails",
                column: "ReceiptVoucherId",
                unique: true,
                filter: "[ReceiptVoucherId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ElectronicPaymentDetails");
        }
    }
}
