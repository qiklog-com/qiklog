using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QikLog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BillingEventAt",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PlanChangedAt",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "tenants",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSubscriptionId",
                table: "tenants",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "processed_stripe_events",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_stripe_events", x => x.EventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenants_StripeCustomerId",
                table: "tenants",
                column: "StripeCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_StripeSubscriptionId",
                table: "tenants",
                column: "StripeSubscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processed_stripe_events");

            migrationBuilder.DropIndex(
                name: "IX_tenants_StripeCustomerId",
                table: "tenants");

            migrationBuilder.DropIndex(
                name: "IX_tenants_StripeSubscriptionId",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "BillingEventAt",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "PlanChangedAt",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionId",
                table: "tenants");
        }
    }
}
