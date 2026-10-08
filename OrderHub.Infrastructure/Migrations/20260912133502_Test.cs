using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_permissions_users_user_id1",
                table: "user_permissions");

            migrationBuilder.DropIndex(
                name: "ix_user_permissions_user_id1",
                table: "user_permissions");

            migrationBuilder.DropColumn(
                name: "user_id1",
                table: "user_permissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "user_id1",
                table: "user_permissions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_permissions_user_id1",
                table: "user_permissions",
                column: "user_id1");

            migrationBuilder.AddForeignKey(
                name: "fk_user_permissions_users_user_id1",
                table: "user_permissions",
                column: "user_id1",
                principalTable: "users",
                principalColumn: "id");
        }
    }
}
