using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthToDocumentSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_document_sequences",
                table: "document_sequences");

            migrationBuilder.AddColumn<int>(
                name: "month",
                table: "document_sequences",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "pk_document_sequences",
                table: "document_sequences",
                columns: new[] { "document_type", "year", "month" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_document_sequences",
                table: "document_sequences");

            migrationBuilder.DropColumn(
                name: "month",
                table: "document_sequences");

            migrationBuilder.AddPrimaryKey(
                name: "pk_document_sequences",
                table: "document_sequences",
                columns: new[] { "document_type", "year" });
        }
    }
}
