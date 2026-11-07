using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryRouteMovementHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_delivery_movements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    delivery_step_id = table.Column<int>(type: "integer", nullable: true),
                    replacement_step_id = table.Column<int>(type: "integer", nullable: true),
                    old_shipment_id = table.Column<int>(type: "integer", nullable: true),
                    old_delivery_trip_id = table.Column<int>(type: "integer", nullable: true),
                    new_shipment_id = table.Column<int>(type: "integer", nullable: true),
                    new_delivery_trip_id = table.Column<int>(type: "integer", nullable: true),
                    previous_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    previous_party_id = table.Column<int>(type: "integer", nullable: true),
                    previous_party_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    next_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    next_party_id = table.Column<int>(type: "integer", nullable: true),
                    next_party_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    original_step_rank = table.Column<int>(type: "integer", nullable: true),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    changed_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    changed_by_user_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    order_version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_delivery_movements", x => x.id);
                    table.CheckConstraint("ck_movement_attribution", "changed_by_user_id IS NOT NULL AND changed_by_user_name IS NOT NULL AND length(trim(reason)) BETWEEN 1 AND 200");
                    table.ForeignKey(
                        name: "fk_order_delivery_movements_delivery_trips_new_delivery_trip_id",
                        column: x => x.new_delivery_trip_id,
                        principalTable: "delivery_trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_delivery_movements_delivery_trips_old_delivery_trip_id",
                        column: x => x.old_delivery_trip_id,
                        principalTable: "delivery_trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_delivery_movements_order_delivery_steps_delivery_step",
                        column: x => x.delivery_step_id,
                        principalTable: "order_delivery_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_delivery_movements_order_delivery_steps_replacement_s",
                        column: x => x.replacement_step_id,
                        principalTable: "order_delivery_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_delivery_movements_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_delivery_movements_shipments_new_shipment_id",
                        column: x => x.new_shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_delivery_movements_shipments_old_shipment_id",
                        column: x => x.old_shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_delivery_movements_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_delivery_movements_changed_by_user_id",
                table: "order_delivery_movements",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_delivery_movements_delivery_step_id",
                table: "order_delivery_movements",
                column: "delivery_step_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_delivery_movements_new_delivery_trip_id",
                table: "order_delivery_movements",
                column: "new_delivery_trip_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_delivery_movements_new_shipment_id",
                table: "order_delivery_movements",
                column: "new_shipment_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_delivery_movements_old_delivery_trip_id",
                table: "order_delivery_movements",
                column: "old_delivery_trip_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_delivery_movements_old_shipment_id",
                table: "order_delivery_movements",
                column: "old_shipment_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_delivery_movements_order_id_order_version_changed_at_",
                table: "order_delivery_movements",
                columns: new[] { "order_id", "order_version", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_order_delivery_movements_order_id_order_version_id",
                table: "order_delivery_movements",
                columns: new[] { "order_id", "order_version", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_order_delivery_movements_replacement_step_id",
                table: "order_delivery_movements",
                column: "replacement_step_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_delivery_movements");
        }
    }
}
