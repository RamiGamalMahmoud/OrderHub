using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateNewOutboxMessageAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxMessageAttachments",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    original_file_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    stored_file_name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    relative_path = table.Column<string>(type: "TEXT", nullable: true),
                    content_type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    file_size = table.Column<long>(type: "INTEGER", nullable: false),
                    outbox_message_id = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modified_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_message_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_outbox_message_attachments_outbox_messages_outbox_message_id",
                        column: x => x.outbox_message_id,
                        principalTable: "outbox_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_attachments_outbox_message_id",
                table: "OutboxMessageAttachments",
                column: "outbox_message_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessageAttachments");
        }
    }
}
