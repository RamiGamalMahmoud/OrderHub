using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkRecordNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_supplies_supplier_id",
                table: "supplies");

            migrationBuilder.AddColumn<int>(
                name: "sequence_month",
                table: "supplies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sequence_year",
                table: "supplies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "work_record_number",
                table: "supplies",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "numbering_party_id",
                table: "shipments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sequence_month",
                table: "shipments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sequence_year",
                table: "shipments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "work_record_number",
                table: "shipments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "numbering_party_id",
                table: "delivery_trips",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sequence_month",
                table: "delivery_trips",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sequence_year",
                table: "delivery_trips",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "work_record_number",
                table: "delivery_trips",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM delivery_trips d
                        LEFT JOIN orders o ON o.id = d.order_id
                        LEFT JOIN order_delivery_steps s ON s.id = d.delivery_step_id
                        WHERE COALESCE(s.deliveryman_id, o.deliveryman_id) IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Cannot backfill delivery trip numbers: a record has no assigned deliveryman.';
                    END IF;
                    IF EXISTS (
                        SELECT 1 FROM shipments s
                        LEFT JOIN orders o ON o.id = s.order_id
                        LEFT JOIN order_delivery_steps ds ON ds.id = s.delivery_step_id
                        WHERE COALESCE(ds.shipping_carrier_id, o.shipping_carrier_id) IS NULL
                    ) THEN
                        RAISE EXCEPTION 'Cannot backfill shipment numbers: a record has no assigned shipping carrier.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                WITH ranked AS (
                    SELECT d.id,
                           COALESCE(ds.deliveryman_id, o.deliveryman_id) AS party_id,
                           EXTRACT(YEAR FROM d.created_at AT TIME ZONE 'Asia/Riyadh')::integer AS sequence_year,
                           EXTRACT(MONTH FROM d.created_at AT TIME ZONE 'Asia/Riyadh')::integer AS sequence_month,
                           ROW_NUMBER() OVER (
                               PARTITION BY COALESCE(ds.deliveryman_id, o.deliveryman_id),
                                   EXTRACT(YEAR FROM d.created_at AT TIME ZONE 'Asia/Riyadh'),
                                   EXTRACT(MONTH FROM d.created_at AT TIME ZONE 'Asia/Riyadh')
                               ORDER BY d.created_at, d.id) AS work_record_number
                    FROM delivery_trips d
                    LEFT JOIN orders o ON o.id = d.order_id
                    LEFT JOIN order_delivery_steps ds ON ds.id = d.delivery_step_id
                )
                UPDATE delivery_trips d
                SET numbering_party_id = ranked.party_id,
                    sequence_year = ranked.sequence_year,
                    sequence_month = ranked.sequence_month,
                    work_record_number = ranked.work_record_number
                FROM ranked WHERE ranked.id = d.id;

                WITH ranked AS (
                    SELECT s.id,
                           COALESCE(ds.shipping_carrier_id, o.shipping_carrier_id) AS party_id,
                           EXTRACT(YEAR FROM s.created_at AT TIME ZONE 'Asia/Riyadh')::integer AS sequence_year,
                           EXTRACT(MONTH FROM s.created_at AT TIME ZONE 'Asia/Riyadh')::integer AS sequence_month,
                           ROW_NUMBER() OVER (
                               PARTITION BY COALESCE(ds.shipping_carrier_id, o.shipping_carrier_id),
                                   EXTRACT(YEAR FROM s.created_at AT TIME ZONE 'Asia/Riyadh'),
                                   EXTRACT(MONTH FROM s.created_at AT TIME ZONE 'Asia/Riyadh')
                               ORDER BY s.created_at, s.id) AS work_record_number
                    FROM shipments s
                    LEFT JOIN orders o ON o.id = s.order_id
                    LEFT JOIN order_delivery_steps ds ON ds.id = s.delivery_step_id
                )
                UPDATE shipments s
                SET numbering_party_id = ranked.party_id,
                    sequence_year = ranked.sequence_year,
                    sequence_month = ranked.sequence_month,
                    work_record_number = ranked.work_record_number
                FROM ranked WHERE ranked.id = s.id;

                WITH ranked AS (
                    SELECT s.id,
                           s.supplier_id AS party_id,
                           EXTRACT(YEAR FROM s.created_at AT TIME ZONE 'Asia/Riyadh')::integer AS sequence_year,
                           EXTRACT(MONTH FROM s.created_at AT TIME ZONE 'Asia/Riyadh')::integer AS sequence_month,
                           ROW_NUMBER() OVER (
                               PARTITION BY s.supplier_id,
                                   EXTRACT(YEAR FROM s.created_at AT TIME ZONE 'Asia/Riyadh'),
                                   EXTRACT(MONTH FROM s.created_at AT TIME ZONE 'Asia/Riyadh')
                               ORDER BY s.created_at, s.id) AS work_record_number
                    FROM supplies s
                )
                UPDATE supplies s
                SET sequence_year = ranked.sequence_year,
                    sequence_month = ranked.sequence_month,
                    work_record_number = ranked.work_record_number
                FROM ranked WHERE ranked.id = s.id;

                INSERT INTO number_sequences (kind, party_id, year, month, last_number)
                SELECT 'Deliveryman', numbering_party_id, sequence_year, sequence_month, MAX(work_record_number)
                FROM delivery_trips GROUP BY numbering_party_id, sequence_year, sequence_month
                ON CONFLICT (kind, party_id, year, month) DO UPDATE
                SET last_number = GREATEST(number_sequences.last_number, EXCLUDED.last_number);

                INSERT INTO number_sequences (kind, party_id, year, month, last_number)
                SELECT 'ShippingCarrier', numbering_party_id, sequence_year, sequence_month, MAX(work_record_number)
                FROM shipments GROUP BY numbering_party_id, sequence_year, sequence_month
                ON CONFLICT (kind, party_id, year, month) DO UPDATE
                SET last_number = GREATEST(number_sequences.last_number, EXCLUDED.last_number);

                INSERT INTO number_sequences (kind, party_id, year, month, last_number)
                SELECT 'Supplier', supplier_id, sequence_year, sequence_month, MAX(work_record_number)
                FROM supplies GROUP BY supplier_id, sequence_year, sequence_month
                ON CONFLICT (kind, party_id, year, month) DO UPDATE
                SET last_number = GREATEST(number_sequences.last_number, EXCLUDED.last_number);
                """);

            migrationBuilder.AlterColumn<int>(name: "sequence_month", table: "supplies", type: "integer", nullable: false, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "sequence_year", table: "supplies", type: "integer", nullable: false, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            migrationBuilder.AlterColumn<long>(name: "work_record_number", table: "supplies", type: "bigint", nullable: false, oldClrType: typeof(long), oldType: "bigint", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "numbering_party_id", table: "shipments", type: "integer", nullable: false, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "sequence_month", table: "shipments", type: "integer", nullable: false, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "sequence_year", table: "shipments", type: "integer", nullable: false, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            migrationBuilder.AlterColumn<long>(name: "work_record_number", table: "shipments", type: "bigint", nullable: false, oldClrType: typeof(long), oldType: "bigint", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "numbering_party_id", table: "delivery_trips", type: "integer", nullable: false, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "sequence_month", table: "delivery_trips", type: "integer", nullable: false, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "sequence_year", table: "delivery_trips", type: "integer", nullable: false, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            migrationBuilder.AlterColumn<long>(name: "work_record_number", table: "delivery_trips", type: "bigint", nullable: false, oldClrType: typeof(long), oldType: "bigint", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_supplies_supplier_id_sequence_year_sequence_month_work_reco",
                table: "supplies",
                columns: new[] { "supplier_id", "sequence_year", "sequence_month", "work_record_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_supplies_numbering",
                table: "supplies",
                sql: "work_record_number > 0 AND sequence_year > 0 AND sequence_month BETWEEN 1 AND 12");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_numbering_party_id_sequence_year_sequence_month_w",
                table: "shipments",
                columns: new[] { "numbering_party_id", "sequence_year", "sequence_month", "work_record_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_shipments_numbering",
                table: "shipments",
                sql: "numbering_party_id > 0 AND work_record_number > 0 AND sequence_year > 0 AND sequence_month BETWEEN 1 AND 12");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_trips_numbering_party_id_sequence_year_sequence_mo",
                table: "delivery_trips",
                columns: new[] { "numbering_party_id", "sequence_year", "sequence_month", "work_record_number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_delivery_trips_numbering",
                table: "delivery_trips",
                sql: "numbering_party_id > 0 AND work_record_number > 0 AND sequence_year > 0 AND sequence_month BETWEEN 1 AND 12");

            migrationBuilder.AddForeignKey(
                name: "fk_delivery_trips_deliverymen_numbering_party_id",
                table: "delivery_trips",
                column: "numbering_party_id",
                principalTable: "deliverymen",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shipments_shipping_carriers_numbering_party_id",
                table: "shipments",
                column: "numbering_party_id",
                principalTable: "shipping_carriers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_delivery_trips_deliverymen_numbering_party_id",
                table: "delivery_trips");

            migrationBuilder.DropForeignKey(
                name: "fk_shipments_shipping_carriers_numbering_party_id",
                table: "shipments");

            migrationBuilder.DropIndex(
                name: "ix_supplies_supplier_id_sequence_year_sequence_month_work_reco",
                table: "supplies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_supplies_numbering",
                table: "supplies");

            migrationBuilder.DropIndex(
                name: "ix_shipments_numbering_party_id_sequence_year_sequence_month_w",
                table: "shipments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_shipments_numbering",
                table: "shipments");

            migrationBuilder.DropIndex(
                name: "ix_delivery_trips_numbering_party_id_sequence_year_sequence_mo",
                table: "delivery_trips");

            migrationBuilder.DropCheckConstraint(
                name: "ck_delivery_trips_numbering",
                table: "delivery_trips");

            migrationBuilder.DropColumn(
                name: "sequence_month",
                table: "supplies");

            migrationBuilder.DropColumn(
                name: "sequence_year",
                table: "supplies");

            migrationBuilder.DropColumn(
                name: "work_record_number",
                table: "supplies");

            migrationBuilder.DropColumn(
                name: "numbering_party_id",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "sequence_month",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "sequence_year",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "work_record_number",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "numbering_party_id",
                table: "delivery_trips");

            migrationBuilder.DropColumn(
                name: "sequence_month",
                table: "delivery_trips");

            migrationBuilder.DropColumn(
                name: "sequence_year",
                table: "delivery_trips");

            migrationBuilder.DropColumn(
                name: "work_record_number",
                table: "delivery_trips");

            migrationBuilder.CreateIndex(
                name: "ix_supplies_supplier_id",
                table: "supplies",
                column: "supplier_id");
        }
    }
}
