using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorInventario.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseProductVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_TenantId",
                table: "Categories");

            migrationBuilder.CreateTable(
                name: "WarehouseProductVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WarehouseId = table.Column<int>(type: "INTEGER", nullable: false),
                    VariantId = table.Column<int>(type: "INTEGER", nullable: false),
                    MinimumQuantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    TargetQuantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseProductVariants_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehouseProductVariants_Variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "Variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehouseProductVariants_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_Name_Region",
                table: "TaxRates",
                columns: new[] { "Name", "Region" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_TenantId_ParentId_Name",
                table: "Categories",
                columns: new[] { "TenantId", "ParentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseProductVariants_TenantId_WarehouseId_VariantId",
                table: "WarehouseProductVariants",
                columns: new[] { "TenantId", "WarehouseId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseProductVariants_VariantId",
                table: "WarehouseProductVariants",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseProductVariants_WarehouseId",
                table: "WarehouseProductVariants",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarehouseProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_TaxRates_Name_Region",
                table: "TaxRates");

            migrationBuilder.DropIndex(
                name: "IX_Categories_TenantId_ParentId_Name",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_TenantId",
                table: "Categories",
                column: "TenantId");
        }
    }
}
