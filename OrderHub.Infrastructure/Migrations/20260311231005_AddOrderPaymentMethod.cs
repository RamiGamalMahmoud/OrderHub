using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "payment_method_id",
                table: "orders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_payment_method_id",
                table: "orders",
                column: "payment_method_id");

            migrationBuilder.AddForeignKey(
                name: "fk_orders_payment_methods_payment_method_id",
                table: "orders",
                column: "payment_method_id",
                principalTable: "payment_methods",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_payment_methods_payment_method_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_payment_method_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "payment_method_id",
                table: "orders");
        }
    }
}
