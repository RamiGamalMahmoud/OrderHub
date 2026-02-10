using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeliverymanPhonesRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deliveryman_phone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "deliveryman_phone",
                columns: table => new
                {
                    deliveryman_id = table.Column<int>(type: "INTEGER", nullable: false),
                    phones_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deliveryman_phone", x => new { x.deliveryman_id, x.phones_id });
                    table.ForeignKey(
                        name: "fk_deliveryman_phone_deliveryman_deliveryman_id",
                        column: x => x.deliveryman_id,
                        principalTable: "deliveryman",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_deliveryman_phone_phones_phones_id",
                        column: x => x.phones_id,
                        principalTable: "phones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_deliveryman_phone_phones_id",
                table: "deliveryman_phone",
                column: "phones_id");
        }
    }
}
