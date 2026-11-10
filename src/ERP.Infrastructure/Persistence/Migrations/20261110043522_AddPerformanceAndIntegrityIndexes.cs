using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceAndIntegrityIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId",
                schema: "inventory",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_WarehouseId",
                schema: "inventory",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_CompanyId",
                schema: "sales",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_CustomerId",
                schema: "sales",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_CompanyId",
                schema: "procurement",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_SupplierId",
                schema: "procurement",
                table: "purchase_orders");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId_MovementDateUtc",
                schema: "inventory",
                table: "stock_movements",
                columns: new[] { "CompanyId", "MovementDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_WarehouseId_ProductId_MovementDateUtc",
                schema: "inventory",
                table: "stock_movements",
                columns: new[] { "WarehouseId", "ProductId", "MovementDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_CompanyId_Status",
                schema: "sales",
                table: "sales_orders",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_CustomerId_OrderDate",
                schema: "sales",
                table: "sales_orders",
                columns: new[] { "CustomerId", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_CompanyId_Status",
                schema: "procurement",
                table: "purchase_orders",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_SupplierId_OrderDate",
                schema: "procurement",
                table: "purchase_orders",
                columns: new[] { "SupplierId", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_InvoiceId",
                schema: "finance",
                table: "payment_allocations",
                column: "InvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_movements_CompanyId_MovementDateUtc",
                schema: "inventory",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_WarehouseId_ProductId_MovementDateUtc",
                schema: "inventory",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_CompanyId_Status",
                schema: "sales",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_sales_orders_CustomerId_OrderDate",
                schema: "sales",
                table: "sales_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_CompanyId_Status",
                schema: "procurement",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_SupplierId_OrderDate",
                schema: "procurement",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_payment_allocations_InvoiceId",
                schema: "finance",
                table: "payment_allocations");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CompanyId",
                schema: "inventory",
                table: "stock_movements",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_WarehouseId",
                schema: "inventory",
                table: "stock_movements",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_CompanyId",
                schema: "sales",
                table: "sales_orders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_CustomerId",
                schema: "sales",
                table: "sales_orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_CompanyId",
                schema: "procurement",
                table: "purchase_orders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_SupplierId",
                schema: "procurement",
                table: "purchase_orders",
                column: "SupplierId");
        }
    }
}
