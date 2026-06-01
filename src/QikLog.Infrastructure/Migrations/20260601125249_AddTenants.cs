using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QikLog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "api_keys",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ZitadelOrgId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Plan = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_TenantId",
                table: "api_keys",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_ZitadelOrgId",
                table: "tenants",
                column: "ZitadelOrgId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_api_keys_tenants_TenantId",
                table: "api_keys",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_api_keys_tenants_TenantId",
                table: "api_keys");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropIndex(
                name: "IX_api_keys_TenantId",
                table: "api_keys");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "api_keys");
        }
    }
}
