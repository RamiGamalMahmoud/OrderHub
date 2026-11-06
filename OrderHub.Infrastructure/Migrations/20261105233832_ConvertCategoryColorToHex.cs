using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertCategoryColorToHex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE categories
                SET color = CASE LOWER(color)
                    WHEN 'blue' THEN '#2563EB'
                    WHEN 'emerald' THEN '#059669'
                    WHEN 'purple' THEN '#7C3AED'
                    WHEN 'amber' THEN '#D97706'
                    WHEN 'rose' THEN '#E11D48'
                    WHEN 'slate' THEN '#64748B'
                    ELSE '#2563EB'
                END
                """);

            migrationBuilder.AlterColumn<string>(
                name: "color",
                table: "categories",
                type: "varchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#2563EB",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Blue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE categories
                SET color = CASE UPPER(color)
                    WHEN '#2563EB' THEN 'Blue'
                    WHEN '#059669' THEN 'Emerald'
                    WHEN '#7C3AED' THEN 'Purple'
                    WHEN '#D97706' THEN 'Amber'
                    WHEN '#E11D48' THEN 'Rose'
                    WHEN '#64748B' THEN 'Slate'
                    ELSE 'Blue'
                END
                """);

            migrationBuilder.AlterColumn<string>(
                name: "color",
                table: "categories",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Blue",
                oldClrType: typeof(string),
                oldType: "varchar(7)",
                oldMaxLength: 7,
                oldDefaultValue: "#2563EB");
        }
    }
}
