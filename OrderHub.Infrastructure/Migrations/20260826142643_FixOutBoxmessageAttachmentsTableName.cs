using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixOutBoxmessageAttachmentsTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_outbox_message_attachments_outbox_messages_outbox_message_id",
                table: "OutboxMessageAttachments");

            migrationBuilder.DropPrimaryKey(
                name: "pk_outbox_message_attachments",
                table: "OutboxMessageAttachments");

            migrationBuilder.RenameTable(
                name: "OutboxMessageAttachments",
                newName: "message_attachments");

            migrationBuilder.RenameIndex(
                name: "ix_outbox_message_attachments_outbox_message_id",
                table: "message_attachments",
                newName: "ix_message_attachments_outbox_message_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_message_attachments",
                table: "message_attachments",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_message_attachments_outbox_messages_outbox_message_id",
                table: "message_attachments",
                column: "outbox_message_id",
                principalTable: "outbox_messages",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_message_attachments_outbox_messages_outbox_message_id",
                table: "message_attachments");

            migrationBuilder.DropPrimaryKey(
                name: "pk_message_attachments",
                table: "message_attachments");

            migrationBuilder.RenameTable(
                name: "message_attachments",
                newName: "OutboxMessageAttachments");

            migrationBuilder.RenameIndex(
                name: "ix_message_attachments_outbox_message_id",
                table: "OutboxMessageAttachments",
                newName: "ix_outbox_message_attachments_outbox_message_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_outbox_message_attachments",
                table: "OutboxMessageAttachments",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_outbox_message_attachments_outbox_messages_outbox_message_id",
                table: "OutboxMessageAttachments",
                column: "outbox_message_id",
                principalTable: "outbox_messages",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
