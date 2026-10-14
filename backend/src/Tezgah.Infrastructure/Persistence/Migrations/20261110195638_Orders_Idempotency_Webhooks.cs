using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tezgah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Orders_Idempotency_Webhooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "stock_item_id",
                table: "order_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "processed_webhook_events",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_webhook_events", x => x.event_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orders_status_created_at",
                table: "orders",
                columns: new[] { "status", "created_at" },
                filter: "status = 1");

            migrationBuilder.CreateIndex(
                name: "ix_orders_user_id_idempotency_key",
                table: "orders",
                columns: new[] { "user_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_stock_item_id",
                table: "order_items",
                column: "stock_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_processed_webhook_events_processed_at",
                table: "processed_webhook_events",
                column: "processed_at");

            migrationBuilder.AddForeignKey(
                name: "fk_order_items_stock_items_stock_item_id",
                table: "order_items",
                column: "stock_item_id",
                principalTable: "stock_items",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_order_items_stock_items_stock_item_id",
                table: "order_items");

            migrationBuilder.DropTable(
                name: "processed_webhook_events");

            migrationBuilder.DropIndex(
                name: "ix_orders_status_created_at",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_user_id_idempotency_key",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_order_items_stock_item_id",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "stock_item_id",
                table: "order_items");
        }
    }
}
