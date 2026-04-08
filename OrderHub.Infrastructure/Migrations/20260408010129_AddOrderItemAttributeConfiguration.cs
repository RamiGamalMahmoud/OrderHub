using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemAttributeConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_item_attributes",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    order_item_id = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "VARCHAR(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_item_attributes", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_item_attributes_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_item_attributes_order_item_id",
                table: "order_item_attributes",
                column: "order_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_item_attributes");
        }
    }
}
