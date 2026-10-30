using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrderHub.Infrastructure;

#nullable disable

namespace OrderHub.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261030010000_AddCommercialDocumentFinancialSnapshots")]
public partial class AddCommercialDocumentFinancialSnapshots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "invoices", "quotations", "proforma_invoices" })
        {
            migrationBuilder.AddColumn<decimal>(name: "delivery_cost", table: table, type: "numeric(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<string>(name: "discount_type", table: table, type: "varchar(30)", nullable: false, defaultValue: "FixedAmount");
            migrationBuilder.AddColumn<decimal>(name: "discount_value", table: table, type: "numeric(18,2)", nullable: false, defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(name: "discount_amount", table: table, type: "numeric(18,2)", nullable: false, defaultValue: 0m);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "invoices", "quotations", "proforma_invoices" })
        {
            migrationBuilder.DropColumn(name: "delivery_cost", table: table);
            migrationBuilder.DropColumn(name: "discount_type", table: table);
            migrationBuilder.DropColumn(name: "discount_value", table: table);
            migrationBuilder.DropColumn(name: "discount_amount", table: table);
        }
    }
}
