using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tezgah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueSellerOwnerAndShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_shipments_order_id_seller_id",
                table: "shipments");

            migrationBuilder.DropIndex(
                name: "ix_sellers_owner_user_id",
                table: "sellers");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_order_id_seller_id",
                table: "shipments",
                columns: new[] { "order_id", "seller_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sellers_owner_user_id",
                table: "sellers",
                column: "owner_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_shipments_order_id_seller_id",
                table: "shipments");

            migrationBuilder.DropIndex(
                name: "ix_sellers_owner_user_id",
                table: "sellers");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_order_id_seller_id",
                table: "shipments",
                columns: new[] { "order_id", "seller_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sellers_owner_user_id",
                table: "sellers",
                column: "owner_user_id");
        }
    }
}
