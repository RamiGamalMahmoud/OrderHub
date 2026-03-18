using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnWantedProperetiesFromOutboxMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "error_message",
                table: "outbox_messages");

            migrationBuilder.AlterColumn<int>(
                name: "retry_count",
                table: "outbox_messages",
                type: "INTEGER",
                nullable: true,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "max_retries",
                table: "outbox_messages",
                type: "INTEGER",
                nullable: true,
                defaultValue: 3,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "retry_count",
                table: "outbox_messages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true,
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "max_retries",
                table: "outbox_messages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 3,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true,
                oldDefaultValue: 3);

            migrationBuilder.AddColumn<string>(
                name: "error_message",
                table: "outbox_messages",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
