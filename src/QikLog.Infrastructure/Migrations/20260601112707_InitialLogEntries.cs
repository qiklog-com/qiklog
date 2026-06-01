using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QikLog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialLogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "log_entries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Level = table.Column<short>(type: "smallint", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    PropertiesJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_log_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_log_entries_Source_ReceivedAt",
                table: "log_entries",
                columns: new[] { "Source", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "log_entries");
        }
    }
}
