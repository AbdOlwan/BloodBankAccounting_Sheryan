using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodBankAccounting.Migrations
{
    /// <inheritdoc />
    public partial class AddEdtiInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OldDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OldDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OldTotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OldPaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OldNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NewDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewTotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewPaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FinancialImpactSummary = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceAuditLogs_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAuditLogs_InvoiceId",
                table: "InvoiceAuditLogs",
                column: "InvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceAuditLogs");
        }
    }
}
