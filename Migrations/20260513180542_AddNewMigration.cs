using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodBankAccounting.Migrations
{
    /// <inheritdoc />
    public partial class AddNewMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Safes",
                columns: new[] { "Id", "Balance", "IsMain", "Name", "Type" },
                values: new object[] { 2, 0m, false, "الخزينة الإلكترونية", "Electronic" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Safes",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
