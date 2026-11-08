using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sales");

            migrationBuilder.CreateTable(
                name: "quotations",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationNumber = table.Column<string>(type: "text", nullable: false),
                    QuotationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpirationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quotations_branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "master",
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotations_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "master",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotations_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "master",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quotation_items",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quotation_items_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "master",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotation_items_quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalSchema: "sales",
                        principalTable: "quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_orders",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderNumber = table.Column<string>(type: "text", nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_orders_branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "master",
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_orders_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "master",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_orders_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "master",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_orders_quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalSchema: "sales",
                        principalTable: "quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "deliveries",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryNumber = table.Column<string>(type: "text", nullable: false),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TrackingNumber = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_deliveries_branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "master",
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_deliveries_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "master",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_deliveries_sales_orders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "sales_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_deliveries_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_invoices",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "text", nullable: false),
                    TaxInvoiceNumber = table.Column<string>(type: "text", nullable: true),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_invoices_branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "master",
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_invoices_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "master",
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_invoices_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "master",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_invoices_sales_orders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "sales_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_items",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    ShippedQuantity = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_order_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_order_items_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "master",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_order_items_sales_orders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "sales_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "delivery_items",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    SoItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityShipped = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delivery_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_delivery_items_deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalSchema: "sales",
                        principalTable: "deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_delivery_items_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "master",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_delivery_items_sales_order_items_SoItemId",
                        column: x => x.SoItemId,
                        principalSchema: "sales",
                        principalTable: "sales_order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_deliveries_BranchId",
                schema: "sales",
                table: "deliveries",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_deliveries_CompanyId",
                schema: "sales",
                table: "deliveries",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_deliveries_DeliveryNumber",
                schema: "sales",
                table: "deliveries",
                column: "DeliveryNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deliveries_SalesOrderId",
                schema: "sales",
                table: "deliveries",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_deliveries_WarehouseId",
                schema: "sales",
                table: "deliveries",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_items_DeliveryId",
                schema: "sales",
                table: "delivery_items",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_items_ProductId",
                schema: "sales",
                table: "delivery_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_delivery_items_SoItemId",
                schema: "sales",
                table: "delivery_items",
                column: "SoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_items_ProductId",
                schema: "sales",
                table: "quotation_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_items_QuotationId",
                schema: "sales",
                table: "quotation_items",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_BranchId",
                schema: "sales",
                table: "quotations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_CompanyId",
                schema: "sales",
                table: "quotations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_CustomerId",
                schema: "sales",
                table: "quotations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_QuotationNumber",
                schema: "sales",
                table: "quotations",
                column: "QuotationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_BranchId",
                schema: "sales",
                table: "sales_invoices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_CompanyId",
                schema: "sales",
                table: "sales_invoices",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_CustomerId",
                schema: "sales",
                table: "sales_invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_InvoiceNumber",
                schema: "sales",
                table: "sales_invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_SalesOrderId",
                schema: "sales",
                table: "sales_invoices",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_items_ProductId",
                schema: "sales",
                table: "sales_order_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_items_SalesOrderId",
                schema: "sales",
                table: "sales_order_items",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_BranchId",
                schema: "sales",
                table: "sales_orders",
                column: "BranchId");

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
                name: "IX_sales_orders_OrderNumber",
                schema: "sales",
                table: "sales_orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_QuotationId",
                schema: "sales",
                table: "sales_orders",
                column: "QuotationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "delivery_items",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "quotation_items",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_invoices",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "deliveries",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_order_items",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sales_orders",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "quotations",
                schema: "sales");
        }
    }
}
