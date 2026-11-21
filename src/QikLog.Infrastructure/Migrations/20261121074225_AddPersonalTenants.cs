using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QikLog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalTenants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tenants_ZitadelOrgId",
                table: "tenants");

            migrationBuilder.AddColumn<string>(
                name: "ZitadelUserId",
                table: "tenants",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenants_ZitadelOrgId",
                table: "tenants",
                column: "ZitadelOrgId",
                unique: true,
                filter: "\"ZitadelUserId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_ZitadelUserId",
                table: "tenants",
                column: "ZitadelUserId",
                unique: true,
                filter: "\"ZitadelUserId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tenants_ZitadelOrgId",
                table: "tenants");

            migrationBuilder.DropIndex(
                name: "IX_tenants_ZitadelUserId",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "ZitadelUserId",
                table: "tenants");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_ZitadelOrgId",
                table: "tenants",
                column: "ZitadelOrgId",
                unique: true);
        }
    }
}
