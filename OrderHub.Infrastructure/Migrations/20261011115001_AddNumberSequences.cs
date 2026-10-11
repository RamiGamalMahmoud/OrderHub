using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNumberSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "number_sequences",
                columns: table => new
                {
                    kind = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    party_id = table.Column<int>(type: "integer", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    last_number = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_number_sequences", x => new { x.kind, x.party_id, x.year, x.month });
                    table.CheckConstraint("ck_number_sequences_kind", "kind IN ('Order', 'Supplier', 'Deliveryman', 'ShippingCarrier')");
                    table.CheckConstraint("ck_number_sequences_last_number", "last_number >= 0");
                    table.CheckConstraint("ck_number_sequences_scope", "(\r\n    kind = 'Order'\r\n    AND party_id = 0\r\n    AND year = 0\r\n    AND month = 0\r\n)\r\nOR\r\n(\r\n    kind IN ('Supplier', 'Deliveryman', 'ShippingCarrier')\r\n    AND party_id > 0\r\n    AND year > 0\r\n    AND month BETWEEN 1 AND 12\r\n)");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "number_sequences");
        }
    }
}
