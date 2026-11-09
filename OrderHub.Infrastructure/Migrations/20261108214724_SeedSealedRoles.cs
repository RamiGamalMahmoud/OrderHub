using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSealedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_sealed",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "role_key",
                table: "roles",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_role_key",
                table: "roles",
                column: "role_key",
                unique: true);

            migrationBuilder.Sql(SealedRoleSeedSql.V1);
        }

        // Seeded role rows are intentionally left behind: they may already be assigned to users.
        // After a rollback they remain as ordinary editable roles.
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_roles_role_key",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "is_sealed",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "role_key",
                table: "roles");
        }
    }
}
