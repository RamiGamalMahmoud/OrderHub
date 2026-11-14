using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropOrderArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_users_archived_by_user_id",
                table: "orders");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddForeignKey(
                name: "fk_orders_users_archived_by_user_id",
                table: "orders",
                column: "archived_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
