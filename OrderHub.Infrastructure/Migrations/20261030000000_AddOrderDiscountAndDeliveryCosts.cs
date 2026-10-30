using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OrderHub.Infrastructure;

#nullable disable

namespace OrderHub.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261030000000_AddOrderDiscountAndDeliveryCosts")]
public partial class AddOrderDiscountAndDeliveryCosts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(name: "delivery_cost", table: "orders", type: "numeric(18,2)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<string>(name: "discount_type", table: "orders", type: "varchar(30)", nullable: false, defaultValue: "FixedAmount");
        migrationBuilder.AddColumn<decimal>(name: "discount_value", table: "orders", type: "numeric(18,2)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>(name: "discount_amount", table: "orders", type: "numeric(18,2)", nullable: false, defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "delivery_cost", table: "orders");
        migrationBuilder.DropColumn(name: "discount_type", table: "orders");
        migrationBuilder.DropColumn(name: "discount_value", table: "orders");
        migrationBuilder.DropColumn(name: "discount_amount", table: "orders");
    }
}
