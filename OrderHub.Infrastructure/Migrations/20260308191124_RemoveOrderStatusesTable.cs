using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrderStatusesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_order_statuses_order_status_id",
                table: "orders");

            migrationBuilder.DropTable(
                name: "order_statuses");

            migrationBuilder.DropIndex(
                name: "ix_orders_order_status_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "order_status_id",
                table: "orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "order_status_id",
                table: "orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "order_statuses",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    display_name = table.Column<string>(type: "VARCHAR(50)", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "VARCHAR(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_statuses", x => x.id);
                });

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

            migrationBuilder.CreateIndex(
                name: "ix_orders_order_status_id",
                table: "orders",
                column: "order_status_id");

            migrationBuilder.AddForeignKey(
                name: "fk_orders_order_statuses_order_status_id",
                table: "orders",
                column: "order_status_id",
                principalTable: "order_statuses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
