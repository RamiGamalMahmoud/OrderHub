using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BindOutboxMessageWithRecipientTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "recipient_id",
                table: "outbox_messages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_recipient_id",
                table: "outbox_messages",
                column: "recipient_id");

            migrationBuilder.AddForeignKey(
                name: "fk_outbox_messages_outbox_message_recipient_recipient_id",
                table: "outbox_messages",
                column: "recipient_id",
                principalTable: "outbox_message_recipients",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_outbox_messages_outbox_message_recipient_recipient_id",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_recipient_id",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "recipient_id",
                table: "outbox_messages");
        }
    }
}
