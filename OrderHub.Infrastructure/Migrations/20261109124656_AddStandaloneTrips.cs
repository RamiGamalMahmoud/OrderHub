using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStandaloneTrips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "standalone_trips",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    deliveryman_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    from_party_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    from_party_id = table.Column<int>(type: "integer", nullable: true),
                    from_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    from_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    from_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    from_location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    to_party_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    to_party_id = table.Column<int>(type: "integer", nullable: true),
                    to_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    to_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    to_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    to_location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    scheduled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status_changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    trip_number = table.Column<long>(type: "bigint", nullable: false),
                    sequence_year = table.Column<int>(type: "integer", nullable: false),
                    sequence_month = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_standalone_trips", x => x.id);
                    table.CheckConstraint("ck_standalone_trips_external_party", "(from_party_type <> 'External' OR (from_party_id IS NULL AND from_name IS NOT NULL AND from_phone IS NOT NULL AND from_address IS NOT NULL)) AND (to_party_type <> 'External' OR (to_party_id IS NULL AND to_name IS NOT NULL AND to_phone IS NOT NULL AND to_address IS NOT NULL))");
                    table.CheckConstraint("ck_standalone_trips_known_party", "(from_party_type = 'External' OR from_party_id > 0) AND (to_party_type = 'External' OR to_party_id > 0)");
                    table.CheckConstraint("ck_standalone_trips_numbers", "trip_number > 0 AND sequence_year > 0 AND sequence_month BETWEEN 1 AND 12");
                    table.CheckConstraint("ck_standalone_trips_status", "status IN ('Pending', 'Completed', 'Cancelled', 'Failed')");
                    table.CheckConstraint("ck_standalone_trips_title_length", "length(trim(title)) BETWEEN 1 AND 150");
                    table.ForeignKey(
                        name: "fk_standalone_trips_deliverymen_deliveryman_id",
                        column: x => x.deliveryman_id,
                        principalTable: "deliverymen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "standalone_trip_attempts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    trip_id = table.Column<int>(type: "integer", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ended_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_standalone_trip_attempts", x => x.id);
                    table.CheckConstraint("ck_standalone_trip_attempts_completed_started", "status NOT IN ('Completed', 'Failed') OR started_at_utc IS NOT NULL");
                    table.CheckConstraint("ck_standalone_trip_attempts_number", "attempt_number > 0");
                    table.CheckConstraint("ck_standalone_trip_attempts_pending_no_end", "status <> 'Pending' OR ended_at_utc IS NULL");
                    table.CheckConstraint("ck_standalone_trip_attempts_reason", "status NOT IN ('Failed', 'Cancelled') OR (reason IS NOT NULL AND length(trim(reason)) BETWEEN 1 AND 200)");
                    table.CheckConstraint("ck_standalone_trip_attempts_status", "status IN ('Pending', 'Completed', 'Cancelled', 'Failed')");
                    table.CheckConstraint("ck_standalone_trip_attempts_terminal_has_end", "status = 'Pending' OR ended_at_utc IS NOT NULL");
                    table.CheckConstraint("ck_standalone_trip_attempts_timing", "ended_at_utc IS NULL OR started_at_utc IS NULL OR ended_at_utc >= started_at_utc");
                    table.ForeignKey(
                        name: "fk_standalone_trip_attempts_standalone_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "standalone_trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "standalone_trip_histories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    trip_id = table.Column<int>(type: "integer", nullable: false),
                    event_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: true),
                    previous_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    new_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    previous_deliveryman_id = table.Column<int>(type: "integer", nullable: true),
                    new_deliveryman_id = table.Column<int>(type: "integer", nullable: true),
                    previous_deliveryman_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    new_deliveryman_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    previous_trip_number = table.Column<long>(type: "bigint", nullable: true),
                    new_trip_number = table.Column<long>(type: "bigint", nullable: true),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    changed_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    changed_by_user_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_standalone_trip_histories", x => x.id);
                    table.CheckConstraint("ck_standalone_trip_histories_event_type", "event_type IN ('Created', 'AttemptStarted', 'Completed', 'Failed', 'Cancelled', 'Retried', 'Reassigned', 'Edited')");
                    table.CheckConstraint("ck_standalone_trip_histories_reason", "event_type NOT IN ('Failed', 'Cancelled', 'Reassigned') OR (reason IS NOT NULL AND length(trim(reason)) BETWEEN 1 AND 200)");
                    table.CheckConstraint("ck_standalone_trip_histories_reassignment", "event_type <> 'Reassigned' OR (previous_deliveryman_id > 0 AND new_deliveryman_id > 0 AND previous_deliveryman_name IS NOT NULL AND new_deliveryman_name IS NOT NULL AND previous_trip_number > 0 AND new_trip_number > 0)");
                    table.ForeignKey(
                        name: "fk_standalone_trip_histories_standalone_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "standalone_trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_standalone_trip_histories_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_standalone_trip_attempts_status",
                table: "standalone_trip_attempts",
                columns: new[] { "trip_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_standalone_trip_attempts_number",
                table: "standalone_trip_attempts",
                columns: new[] { "trip_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_standalone_trip_attempts_open",
                table: "standalone_trip_attempts",
                column: "trip_id",
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_standalone_trip_histories_changed_by_user_id",
                table: "standalone_trip_histories",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_standalone_trip_histories_new_deliveryman",
                table: "standalone_trip_histories",
                column: "new_deliveryman_id");

            migrationBuilder.CreateIndex(
                name: "ix_standalone_trip_histories_prev_deliveryman",
                table: "standalone_trip_histories",
                column: "previous_deliveryman_id");

            migrationBuilder.CreateIndex(
                name: "ix_standalone_trip_histories_trip",
                table: "standalone_trip_histories",
                columns: new[] { "trip_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_standalone_trips_created",
                table: "standalone_trips",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_standalone_trips_deliveryman_created",
                table: "standalone_trips",
                columns: new[] { "deliveryman_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "uq_standalone_trips_number",
                table: "standalone_trips",
                columns: new[] { "deliveryman_id", "sequence_year", "sequence_month", "trip_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback safety (NFR-5.1): Trips grants are meaningless without the feature.
            // Seeded/voided number_sequences rows are intentionally left untouched (NFR-5.2).
            migrationBuilder.Sql("DELETE FROM role_permissions WHERE feature = 'Trips';");

            migrationBuilder.DropTable(
                name: "standalone_trip_attempts");

            migrationBuilder.DropTable(
                name: "standalone_trip_histories");

            migrationBuilder.DropTable(
                name: "standalone_trips");
        }
    }
}
