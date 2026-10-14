using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "delivery_trips",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: true),
                    delivery_step_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_trips", x => x.id);
                    table.CheckConstraint("ck_delivery_trips_single_parent", "(order_id IS NOT NULL) <> (delivery_step_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_delivery_trips_order_delivery_steps_delivery_step_id",
                        column: x => x.delivery_step_id,
                        principalTable: "order_delivery_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_delivery_trips_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: true),
                    delivery_step_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipments", x => x.id);
                    table.CheckConstraint("ck_shipments_single_parent", "(order_id IS NOT NULL) <> (delivery_step_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_shipments_order_delivery_steps_delivery_step_id",
                        column: x => x.delivery_step_id,
                        principalTable: "order_delivery_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_shipments_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplies",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplies", x => x.id);
                    table.ForeignKey(
                        name: "fk_supplies_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_supplies_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_record_status_histories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    work_record_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    work_record_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_record_status_histories", x => x.id);
                    table.CheckConstraint("ck_work_record_history_reason", "status NOT IN ('Failed','Cancelled') OR (reason IS NOT NULL AND length(trim(reason)) BETWEEN 1 AND 200)");
                    table.CheckConstraint("ck_work_record_history_record_id", "work_record_id > 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_delivery_trips_delivery_step_id",
                table: "delivery_trips",
                column: "delivery_step_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_trips_order_id",
                table: "delivery_trips",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_delivery_step_id",
                table: "shipments",
                column: "delivery_step_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_order_id",
                table: "shipments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplies_order_id_supplier_id",
                table: "supplies",
                columns: new[] { "order_id", "supplier_id" });

            migrationBuilder.CreateIndex(
                name: "ix_supplies_supplier_id",
                table: "supplies",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_record_status_histories_work_record_type_work_record_i",
                table: "work_record_status_histories",
                columns: new[] { "work_record_type", "work_record_id", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "delivery_trips");

            migrationBuilder.DropTable(
                name: "shipments");

            migrationBuilder.DropTable(
                name: "supplies");

            migrationBuilder.DropTable(
                name: "work_record_status_histories");
        }
    }
}
