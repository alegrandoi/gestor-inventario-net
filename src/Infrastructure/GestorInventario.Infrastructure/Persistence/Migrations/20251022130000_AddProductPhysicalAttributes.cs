using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorInventario.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddProductPhysicalAttributes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "HeightCm",
            table: "Products",
            type: "decimal(18,4)",
            precision: 18,
            scale: 4,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "LeadTimeDays",
            table: "Products",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "LengthCm",
            table: "Products",
            type: "decimal(18,4)",
            precision: 18,
            scale: 4,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "ReorderPoint",
            table: "Products",
            type: "decimal(18,4)",
            precision: 18,
            scale: 4,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "ReorderQuantity",
            table: "Products",
            type: "decimal(18,4)",
            precision: 18,
            scale: 4,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "RequiresSerialTracking",
            table: "Products",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<decimal>(
            name: "SafetyStock",
            table: "Products",
            type: "decimal(18,4)",
            precision: 18,
            scale: 4,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "WeightKg",
            table: "Products",
            type: "decimal(18,4)",
            precision: 18,
            scale: 4,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "WidthCm",
            table: "Products",
            type: "decimal(18,4)",
            precision: 18,
            scale: 4,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "HeightCm",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "LeadTimeDays",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "LengthCm",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "ReorderPoint",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "ReorderQuantity",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "RequiresSerialTracking",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "SafetyStock",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "WeightKg",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "WidthCm",
            table: "Products");
    }
}
