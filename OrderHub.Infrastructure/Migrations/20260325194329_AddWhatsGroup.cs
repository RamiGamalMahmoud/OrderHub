using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "whatsapp_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    group_name = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    group_type = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_whatsapp_groups", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "whatsapp_groups");
        }
    }
}
