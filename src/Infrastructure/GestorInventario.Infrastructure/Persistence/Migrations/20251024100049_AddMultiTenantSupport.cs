using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorInventario.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var isSqlite = IsSqliteProvider();

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_Variants_Sku", "Variants");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_Products_Code", "Products");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_ProductPrices_PriceListId_VariantId", "ProductPrices");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_InventoryTransactions_VariantId_WarehouseId", "InventoryTransactions");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_InventoryTransactions_VariantId", "InventoryTransactions");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_InventoryStock_VariantId_WarehouseId", "InventoryStock");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_InventoryStock_VariantId", "InventoryStock");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_DemandHistory_VariantId_Date", "DemandHistory");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_DemandHistory_VariantId", "DemandHistory");

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Variants",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Suppliers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Shipments",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ShipmentLines",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ShipmentEvents",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SalesOrders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SalesOrderLines",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SalesOrderAllocations",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PurchaseOrders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PurchaseOrderLines",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ProductPrices",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ProductImages",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PriceLists",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "InventoryTransactions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "InventoryStock",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "DemandHistory",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AuditLogs",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DefaultCulture = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    DefaultCurrency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            var seedTimestamp = new DateTime(2025, 10, 24, 10, 0, 0, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[]
                {
                    "Id",
                    "Name",
                    "Code",
                    "DefaultCulture",
                    "DefaultCurrency",
                    "IsActive",
                    "CreatedAt",
                    "UpdatedAt"
                },
                values: new object[]
                {
                    1,
                    "Default Tenant",
                    "DEFAULT",
                    "es-ES",
                    "EUR",
                    true,
                    seedTimestamp,
                    null
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Locale = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Branches_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Branches",
                columns: new[]
                {
                    "Id",
                    "Name",
                    "Code",
                    "TimeZone",
                    "Locale",
                    "Currency",
                    "IsDefault",
                    "IsActive",
                    "CreatedAt",
                    "UpdatedAt",
                    "TenantId"
                },
                values: new object[]
                {
                    1,
                    "Headquarters",
                    "HQ",
                    "UTC",
                    "es-ES",
                    "EUR",
                    true,
                    true,
                    seedTimestamp,
                    null,
                    1
                });

            migrationBuilder.CreateTable(
                name: "AbcPolicies",
                columns: table => new
                {
                    Id = ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite"
                        ? table.Column<int>(type: "INTEGER", nullable: false)
                            .Annotation("Sqlite:Autoincrement", true)
                        : table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ThresholdA = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    ThresholdB = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    ServiceLevelA = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    ServiceLevelB = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    ServiceLevelC = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbcPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VariantAbcClassifications",
                columns: table => new
                {
                    Id = ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite"
                        ? table.Column<int>(type: "INTEGER", nullable: false)
                            .Annotation("Sqlite:Autoincrement", true)
                        : table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                    VariantId = table.Column<int>(type: "int", nullable: false),
                    AbcPolicyId = table.Column<int>(type: "int", nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    AnnualConsumptionValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly?>(type: "date", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime?>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantAbcClassifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VariantAbcClassifications_AbcPolicies_AbcPolicyId",
                        column: x => x.AbcPolicyId,
                        principalTable: "AbcPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VariantAbcClassifications_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VariantAbcClassifications_Variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "Variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeasonalFactors",
                columns: table => new
                {
                    Id = ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite"
                        ? table.Column<int>(type: "INTEGER", nullable: false)
                            .Annotation("Sqlite:Autoincrement", true)
                        : table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                    VariantId = table.Column<int>(type: "int", nullable: false),
                    Interval = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Factor = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EffectiveFrom = table.Column<DateOnly?>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly?>(type: "date", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime?>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonalFactors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeasonalFactors_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeasonalFactors_Variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "Variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DemandAggregates",
                columns: table => new
                {
                    Id = ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite"
                        ? table.Column<int>(type: "INTEGER", nullable: false)
                            .Annotation("Sqlite:Autoincrement", true)
                        : table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                    VariantId = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    Interval = table.Column<int>(type: "int", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    AverageLeadTimeDays = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime?>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandAggregates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandAggregates_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DemandAggregates_Variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "Variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_TenantId",
                table: "Warehouses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_VariantAbcClassifications_AbcPolicyId",
                table: "VariantAbcClassifications",
                column: "AbcPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_VariantAbcClassifications_Variant_EffectiveFrom",
                table: "VariantAbcClassifications",
                columns: new[] { "TenantId", "VariantId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_VariantAbcClassifications_VariantId",
                table: "VariantAbcClassifications",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Variants_TenantId_Sku",
                table: "Variants",
                columns: new[] { "TenantId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TenantId",
                table: "Suppliers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TenantId_SalesOrderId",
                table: "Shipments",
                columns: new[] { "TenantId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLines_TenantId_ShipmentId",
                table: "ShipmentLines",
                columns: new[] { "TenantId", "ShipmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentEvents_TenantId_ShipmentId_EventDate",
                table: "ShipmentEvents",
                columns: new[] { "TenantId", "ShipmentId", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SeasonalFactors_TenantId_VariantId_Interval_Sequence_EffectiveFrom",
                table: "SeasonalFactors",
                columns: new[] { "TenantId", "VariantId", "Interval", "Sequence", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_SeasonalFactors_VariantId",
                table: "SeasonalFactors",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_TenantId",
                table: "SalesOrders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLines_TenantId_SalesOrderId",
                table: "SalesOrderLines",
                columns: new[] { "TenantId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderAllocations_TenantId",
                table: "SalesOrderAllocations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId",
                table: "PurchaseOrders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_TenantId_PurchaseOrderId",
                table: "PurchaseOrderLines",
                columns: new[] { "TenantId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_Code",
                table: "Products",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_PriceListId",
                table: "ProductPrices",
                column: "PriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_TenantId_PriceListId_VariantId",
                table: "ProductPrices",
                columns: new[] { "TenantId", "PriceListId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_TenantId_ProductId",
                table: "ProductImages",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_TenantId_Name",
                table: "PriceLists",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_TenantId_VariantId_WarehouseId",
                table: "InventoryTransactions",
                columns: new[] { "TenantId", "VariantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_VariantId",
                table: "InventoryTransactions",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStock_TenantId_VariantId_WarehouseId",
                table: "InventoryStock",
                columns: new[] { "TenantId", "VariantId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStock_VariantId",
                table: "InventoryStock",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandHistory_TenantId_VariantId_Date",
                table: "DemandHistory",
                columns: new[] { "TenantId", "VariantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_DemandHistory_VariantId",
                table: "DemandHistory",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandAggregates_TenantId_VariantId_PeriodStart_Interval",
                table: "DemandAggregates",
                columns: new[] { "TenantId", "VariantId", "PeriodStart", "Interval" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandAggregates_VariantId",
                table: "DemandAggregates",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId",
                table: "Customers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_TenantId",
                table: "Categories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_TenantId_Code",
                table: "Branches",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Code",
                table: "Tenants",
                column: "Code",
                unique: true);

            if (!isSqlite)
            {
                migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Tenants_TenantId",
                table: "AuditLogs",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_Categories_Tenants_TenantId",
                table: "Categories",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_Customers_Tenants_TenantId",
                table: "Customers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_DemandHistory_Tenants_TenantId",
                table: "DemandHistory",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_InventoryStock_Tenants_TenantId",
                table: "InventoryStock",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Tenants_TenantId",
                table: "InventoryTransactions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_PriceLists_Tenants_TenantId",
                table: "PriceLists",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_ProductImages_Tenants_TenantId",
                table: "ProductImages",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_ProductPrices_Tenants_TenantId",
                table: "ProductPrices",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_Products_Tenants_TenantId",
                table: "Products",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLines_Tenants_TenantId",
                table: "PurchaseOrderLines",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Tenants_TenantId",
                table: "PurchaseOrders",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderAllocations_Tenants_TenantId",
                table: "SalesOrderAllocations",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderLines_Tenants_TenantId",
                table: "SalesOrderLines",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Tenants_TenantId",
                table: "SalesOrders",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_ShipmentEvents_Tenants_TenantId",
                table: "ShipmentEvents",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_ShipmentLines_Tenants_TenantId",
                table: "ShipmentLines",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Tenants_TenantId",
                table: "Shipments",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_Tenants_TenantId",
                table: "Suppliers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_Variants_Tenants_TenantId",
                table: "Variants",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Tenants_TenantId",
                table: "Warehouses",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var isSqlite = IsSqliteProvider();

            if (!isSqlite)
            {
                migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Tenants_TenantId",
                table: "AuditLogs");

                migrationBuilder.DropForeignKey(
                name: "FK_Categories_Tenants_TenantId",
                table: "Categories");

                migrationBuilder.DropForeignKey(
                name: "FK_Customers_Tenants_TenantId",
                table: "Customers");

                migrationBuilder.DropForeignKey(
                name: "FK_DemandHistory_Tenants_TenantId",
                table: "DemandHistory");

                migrationBuilder.DropForeignKey(
                name: "FK_InventoryStock_Tenants_TenantId",
                table: "InventoryStock");

                migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Tenants_TenantId",
                table: "InventoryTransactions");

                migrationBuilder.DropForeignKey(
                name: "FK_PriceLists_Tenants_TenantId",
                table: "PriceLists");

                migrationBuilder.DropForeignKey(
                name: "FK_ProductImages_Tenants_TenantId",
                table: "ProductImages");

                migrationBuilder.DropForeignKey(
                name: "FK_ProductPrices_Tenants_TenantId",
                table: "ProductPrices");

                migrationBuilder.DropForeignKey(
                name: "FK_Products_Tenants_TenantId",
                table: "Products");

                migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLines_Tenants_TenantId",
                table: "PurchaseOrderLines");

                migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Tenants_TenantId",
                table: "PurchaseOrders");

                migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderAllocations_Tenants_TenantId",
                table: "SalesOrderAllocations");

                migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderLines_Tenants_TenantId",
                table: "SalesOrderLines");

                migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Tenants_TenantId",
                table: "SalesOrders");

                migrationBuilder.DropForeignKey(
                name: "FK_ShipmentEvents_Tenants_TenantId",
                table: "ShipmentEvents");

                migrationBuilder.DropForeignKey(
                name: "FK_ShipmentLines_Tenants_TenantId",
                table: "ShipmentLines");

                migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Tenants_TenantId",
                table: "Shipments");

                migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_Tenants_TenantId",
                table: "Suppliers");

                migrationBuilder.DropForeignKey(
                name: "FK_Variants_Tenants_TenantId",
                table: "Variants");

                migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Tenants_TenantId",
                table: "Warehouses");
            }

            migrationBuilder.DropTable(
                name: "DemandAggregates");

            migrationBuilder.DropTable(
                name: "SeasonalFactors");

            migrationBuilder.DropTable(
                name: "VariantAbcClassifications");

            migrationBuilder.DropTable(
                name: "AbcPolicies");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Tenants");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_Warehouses_TenantId", "Warehouses");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_Variants_TenantId_Sku", "Variants");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_Suppliers_TenantId", "Suppliers");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_Shipments_TenantId_SalesOrderId", "Shipments");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_ShipmentLines_TenantId_ShipmentId", "ShipmentLines");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_ShipmentEvents_TenantId_ShipmentId_EventDate", "ShipmentEvents");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_SalesOrders_TenantId", "SalesOrders");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_SalesOrderLines_TenantId_SalesOrderId", "SalesOrderLines");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_SalesOrderAllocations_TenantId", "SalesOrderAllocations");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_PurchaseOrders_TenantId", "PurchaseOrders");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_PurchaseOrderLines_TenantId_PurchaseOrderId", "PurchaseOrderLines");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_Products_TenantId_Code", "Products");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_ProductPrices_PriceListId", "ProductPrices");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_ProductPrices_TenantId_PriceListId_VariantId", "ProductPrices");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_ProductImages_TenantId_ProductId", "ProductImages");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_PriceLists_TenantId_Name", "PriceLists");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_InventoryTransactions_TenantId_VariantId_WarehouseId", "InventoryTransactions");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_InventoryTransactions_VariantId", "InventoryTransactions");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_InventoryStock_TenantId_VariantId_WarehouseId", "InventoryStock");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_InventoryStock_VariantId", "InventoryStock");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_DemandHistory_TenantId_VariantId_Date", "DemandHistory");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_DemandHistory_VariantId", "DemandHistory");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_Customers_TenantId", "Customers");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_Categories_TenantId", "Categories");

            DropIndexIfExistsWithProviderCheck(migrationBuilder, "IX_AuditLogs_TenantId", "AuditLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Variants");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ShipmentLines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ShipmentEvents");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SalesOrderLines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SalesOrderAllocations");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProductPrices");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InventoryStock");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DemandHistory");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DemandAggregates");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_Variants_Sku",
                table: "Variants",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Code",
                table: "Products",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_PriceListId_VariantId",
                table: "ProductPrices",
                columns: new[] { "PriceListId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_VariantId_WarehouseId",
                table: "InventoryTransactions",
                columns: new[] { "VariantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryStock_VariantId_WarehouseId",
                table: "InventoryStock",
                columns: new[] { "VariantId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandHistory_VariantId_Date",
                table: "DemandHistory",
                columns: new[] { "VariantId", "Date" });
        }

        private void DropIndexIfExistsWithProviderCheck(MigrationBuilder migrationBuilder, string indexName, string tableName)
        {
            if (IsSqliteProvider())
            {
                migrationBuilder.Sql($"DROP INDEX IF EXISTS \"{indexName}\";");
                return;
            }

            if (IsSqlServerProvider())
            {
                migrationBuilder.Sql($@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'{indexName}' AND object_id = OBJECT_ID(N'[dbo].[{tableName}]'))
    DROP INDEX [{indexName}] ON [dbo].[{tableName}];
");
                return;
            }

            migrationBuilder.DropIndex(
                name: indexName,
                table: tableName);
        }

        private bool IsSqliteProvider()
        {
            var provider = ActiveProvider ?? string.Empty;
            return provider.Equals("Microsoft.EntityFrameworkCore.Sqlite", StringComparison.OrdinalIgnoreCase) ||
                provider.EndsWith(".Sqlite", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSqlServerProvider()
        {
            var provider = ActiveProvider ?? string.Empty;
            return provider.Equals("Microsoft.EntityFrameworkCore.SqlServer", StringComparison.OrdinalIgnoreCase) ||
                provider.EndsWith(".SqlServer", StringComparison.OrdinalIgnoreCase);
        }
    }
}
