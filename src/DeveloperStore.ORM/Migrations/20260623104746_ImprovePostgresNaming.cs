using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeveloperStore.ORM.Migrations
{
    /// <inheritdoc />
    public partial class ImprovePostgresNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sale_items_sales_sale_id",
                table: "sale_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sales",
                table: "sales");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sale_items",
                table: "sale_items");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "sales",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_sales_sale_number",
                table: "sales",
                newName: "uq_sales_sale_number");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "sale_items",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_sale_items_sale_id",
                table: "sale_items",
                newName: "idx_sale_items_sale_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sales",
                table: "sales",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sale_items",
                table: "sale_items",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "idx_sales_sold_at",
                table: "sales",
                column: "sold_at");

            migrationBuilder.CreateIndex(
                name: "idx_sales_status",
                table: "sales",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_sales_status_sold_at",
                table: "sales",
                columns: new[] { "status", "sold_at" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_items_discount_range",
                table: "sale_items",
                sql: "discount_percentage >= 0 AND discount_percentage <= 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_items_quantity_positive",
                table: "sale_items",
                sql: "quantity > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_sale_items_unit_price_non_neg",
                table: "sale_items",
                sql: "unit_price >= 0");

            migrationBuilder.AddForeignKey(
                name: "fk_sale_items_sales",
                table: "sale_items",
                column: "sale_id",
                principalTable: "sales",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sale_items_sales",
                table: "sale_items");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sales",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "idx_sales_sold_at",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "idx_sales_status",
                table: "sales");

            migrationBuilder.DropIndex(
                name: "idx_sales_status_sold_at",
                table: "sales");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sale_items",
                table: "sale_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_items_discount_range",
                table: "sale_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_items_quantity_positive",
                table: "sale_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_sale_items_unit_price_non_neg",
                table: "sale_items");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "sales",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "uq_sales_sale_number",
                table: "sales",
                newName: "IX_sales_sale_number");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "sale_items",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "idx_sale_items_sale_id",
                table: "sale_items",
                newName: "IX_sale_items_sale_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sales",
                table: "sales",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sale_items",
                table: "sale_items",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_sale_items_sales_sale_id",
                table: "sale_items",
                column: "sale_id",
                principalTable: "sales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
