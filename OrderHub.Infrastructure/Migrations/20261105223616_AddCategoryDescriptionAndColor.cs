using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryDescriptionAndColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color",
                table: "categories",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Blue");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "categories",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "color",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "description",
                table: "categories");
        }
    }
}
