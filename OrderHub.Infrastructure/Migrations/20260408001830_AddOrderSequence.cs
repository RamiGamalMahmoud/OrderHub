using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_entity_sequences",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    order_id = table.Column<int>(type: "INTEGER", nullable: false),
                    recipient_type = table.Column<string>(type: "TEXT", nullable: false),
                    entity_id = table.Column<int>(type: "INTEGER", nullable: false),
                    sequence_year = table.Column<int>(type: "INTEGER", nullable: false),
                    sequence_month = table.Column<int>(type: "INTEGER", nullable: false),
                    sequence_number = table.Column<int>(type: "INTEGER", nullable: false),
                    display_title = table.Column<string>(type: "VARCHAR(50)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_entity_sequences", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_entity_sequences_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_entity_sequences_order_id_recipient_type_entity_id",
                table: "order_entity_sequences",
                columns: new[] { "order_id", "recipient_type", "entity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_entity_sequences_recipient_type_entity_id_sequence_year_sequence_month_sequence_number",
                table: "order_entity_sequences",
                columns: new[] { "recipient_type", "entity_id", "sequence_year", "sequence_month", "sequence_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_entity_sequences");
        }
    }
}
