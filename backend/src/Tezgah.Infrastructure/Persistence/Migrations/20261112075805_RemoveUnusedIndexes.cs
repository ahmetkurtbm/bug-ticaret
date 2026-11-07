using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tezgah.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_orders_user_id_placed_at",
                table: "orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_orders_user_id_placed_at",
                table: "orders",
                columns: new[] { "user_id", "placed_at" });
        }
    }
}
