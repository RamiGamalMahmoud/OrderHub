using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeeDataToOrderStatusesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "order_statuses",
                columns: new[] { "id", "created_at", "display_name", "modified_at", "status" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 2, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "تحت المراجعة", null, "Pending" },
                    { 2, new DateTime(2026, 2, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "قيد التنفيذ", null, "Processing" },
                    { 3, new DateTime(2026, 2, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "تم الشحن", null, "Shipped" },
                    { 4, new DateTime(2026, 2, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "تم التوصيل", null, "Delivered" },
                    { 5, new DateTime(2026, 2, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "ملغي", null, "Cancelled" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "order_statuses",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "order_statuses",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "order_statuses",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "order_statuses",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "order_statuses",
                keyColumn: "id",
                keyValue: 5);
        }
    }
}
