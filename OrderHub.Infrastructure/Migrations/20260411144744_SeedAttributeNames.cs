using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedAttributeNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "attribute_names",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "attribute_names",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "attribute_names",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "attribute_names",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "attribute_names",
                keyColumn: "id",
                keyValue: 5);
        }
    }
}
