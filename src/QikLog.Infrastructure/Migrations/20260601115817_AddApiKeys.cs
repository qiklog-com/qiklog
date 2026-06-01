using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QikLog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApiKeyId",
                table: "log_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "api_keys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LookupPrefix = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    SecretHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RateLimitPerMinute = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_keys", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_log_entries_ApiKeyId",
                table: "log_entries",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_IsActive_RevokedAt",
                table: "api_keys",
                columns: new[] { "IsActive", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_LookupPrefix",
                table: "api_keys",
                column: "LookupPrefix");

            migrationBuilder.AddForeignKey(
                name: "FK_log_entries_api_keys_ApiKeyId",
                table: "log_entries",
                column: "ApiKeyId",
                principalTable: "api_keys",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_log_entries_api_keys_ApiKeyId",
                table: "log_entries");

            migrationBuilder.DropTable(
                name: "api_keys");

            migrationBuilder.DropIndex(
                name: "IX_log_entries_ApiKeyId",
                table: "log_entries");

            migrationBuilder.DropColumn(
                name: "ApiKeyId",
                table: "log_entries");
        }
    }
}
