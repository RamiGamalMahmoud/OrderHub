using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderLifecycleSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AlterColumn<string>(
                name: "order_status",
                table: "orders",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Processing",
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30,
                oldDefaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "archive_reason",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "archived_at_utc",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "archived_by_user_id",
                table: "orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "archived_by_user_name",
                table: "orders",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_archived",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "order_delivery_steps",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.CreateTable(
                name: "delivery_step_status_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    delivery_step_id = table.Column<int>(type: "integer", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    new_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    changed_by_user_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    order_version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_step_status_history", x => x.id);
                    table.CheckConstraint("ck_step_history_attribution", "source = 'LegacyMigration' OR (changed_by_user_id IS NOT NULL AND changed_by_user_name IS NOT NULL AND (new_status NOT IN ('Failed','Cancelled') OR (reason IS NOT NULL AND length(trim(reason)) BETWEEN 1 AND 200)))");
                    table.ForeignKey(
                        name: "fk_delivery_step_status_history_order_delivery_steps_delivery_",
                        column: x => x.delivery_step_id,
                        principalTable: "order_delivery_steps",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_delivery_step_status_history_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_delivery_step_status_history_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_status_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_id = table.Column<int>(type: "integer", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    new_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    changed_by_user_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    order_version = table.Column<int>(type: "integer", nullable: false),
                    original_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_status_history", x => x.id);
                    table.CheckConstraint("ck_order_history_attribution", "source = 'LegacyMigration' OR (changed_by_user_id IS NOT NULL AND changed_by_user_name IS NOT NULL AND ((new_status NOT IN ('Cancelled','DeliveryFailed') AND source <> 'DirectDelivery') OR (reason IS NOT NULL AND length(trim(reason)) BETWEEN 1 AND 200)))");
                    table.ForeignKey(
                        name: "fk_order_status_history_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_status_history_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orders_archived_by_user_id",
                table: "orders",
                column: "archived_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_is_archived_created_at",
                table: "orders",
                columns: new[] { "is_archived", "created_at" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_archive_metadata",
                table: "orders",
                sql: "NOT is_archived OR (archived_at_utc IS NOT NULL AND archived_by_user_id IS NOT NULL AND archive_reason IS NOT NULL AND length(trim(archive_reason)) BETWEEN 1 AND 200)");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_step_status_history_changed_by_user_id",
                table: "delivery_step_status_history",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_step_status_history_delivery_step_id_order_version",
                table: "delivery_step_status_history",
                columns: new[] { "delivery_step_id", "order_version", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_delivery_step_status_history_order_id_order_version_id",
                table: "delivery_step_status_history",
                columns: new[] { "order_id", "order_version", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_order_status_history_changed_by_user_id",
                table: "order_status_history",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_status_history_order_id_order_version_id",
                table: "order_status_history",
                columns: new[] { "order_id", "order_version", "id" });

            migrationBuilder.AddForeignKey(
                name: "fk_orders_users_archived_by_user_id",
                table: "orders",
                column: "archived_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM order_status_history) OR EXISTS (SELECT 1 FROM delivery_step_status_history) OR EXISTS (SELECT 1 FROM orders WHERE is_archived) THEN RAISE EXCEPTION 'Lifecycle history cannot be removed; restore a verified backup instead.'; END IF; END $$;");
            migrationBuilder.DropForeignKey(
                name: "fk_orders_users_archived_by_user_id",
                table: "orders");

            migrationBuilder.DropTable(
                name: "delivery_step_status_history");

            migrationBuilder.DropTable(
                name: "order_status_history");

            migrationBuilder.DropIndex(
                name: "ix_orders_archived_by_user_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_is_archived_created_at",
                table: "orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_archive_metadata",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "archive_reason",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "archived_at_utc",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "archived_by_user_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "archived_by_user_name",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "is_archived",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "version",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "status",
                table: "order_delivery_steps");

            migrationBuilder.AlterColumn<string>(
                name: "order_status",
                table: "orders",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30,
                oldDefaultValue: "Processing");
        }
    }
}
