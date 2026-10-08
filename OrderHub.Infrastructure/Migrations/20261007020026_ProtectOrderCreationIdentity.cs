using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProtectOrderCreationIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "creation_request_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_creation_request_id",
                table: "orders",
                column: "creation_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_order_number",
                table: "orders",
                column: "order_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM orders WHERE creation_request_id IS NOT NULL) THEN RAISE EXCEPTION 'Creation identities cannot be removed after use; restore a verified backup instead.'; END IF; END $$;");
            migrationBuilder.DropIndex(
                name: "ix_orders_creation_request_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_order_number",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "creation_request_id",
                table: "orders");
        }
    }
}
