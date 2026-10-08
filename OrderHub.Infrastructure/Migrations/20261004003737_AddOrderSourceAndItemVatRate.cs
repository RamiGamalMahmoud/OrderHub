using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderSourceAndItemVatRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "order_source",
                table: "orders",
                type: "varchar(30)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "vat_rate",
                table: "order_items",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "order_source",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "vat_rate",
                table: "order_items");
        }
    }
}
