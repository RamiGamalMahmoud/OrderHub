using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrderItemAttributer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attribute_names");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attribute_names",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attribute_names", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "attribute_names",
                columns: new[] { "id", "created_at", "modified_at", "name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 4, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "القماش" },
                    { 2, new DateTime(2026, 4, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "الخشب" },
                    { 3, new DateTime(2026, 4, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "الموديل" },
                    { 4, new DateTime(2026, 4, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "اللون" },
                    { 5, new DateTime(2026, 4, 11, 0, 0, 0, 0, DateTimeKind.Utc), null, "المقاس" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_attribute_names_name",
                table: "attribute_names",
                column: "name",
                unique: true);
        }
    }
}
