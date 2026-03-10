using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    code = table.Column<string>(type: "TEXT", unicode: false, maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_methods", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "payment_methods",
                columns: new[] { "id", "code", "created_at", "description", "display_name", "is_active", "modified_at" },
                values: new object[,]
                {
                    { 1, "ON_DELIVERY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "الدفع نقداً عند الاستلام", "نقداً", true, null },
                    { 2, "BANK_TRANSFER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "الدفع عبر التحويل البنكي", "تحويل بنكي", true, null },
                    { 3, "SUPPLIER_ACCOUNT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "الدفع على حساب المورد", "على حساب المورد", true, null },
                    { 4, "SALLA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "الدفع عبر منصة سلة", "سلة", true, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_DisplayName",
                table: "payment_methods",
                column: "display_name");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_IsActive",
                table: "payment_methods",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "UX_PaymentMethods_Code",
                table: "payment_methods",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_methods");
        }
    }
}
