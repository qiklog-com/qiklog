using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QikLog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLogLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "log_links",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    LineCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_log_links", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_log_links_ExpiresAt",
                table: "log_links",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "log_links");
        }
    }
}
