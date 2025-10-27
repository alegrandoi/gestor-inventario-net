using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorInventario.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCarriersAndDeliveryMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Carrier",
                table: "Shipments");

            migrationBuilder.AddColumn<int>(
                name: "CarrierId",
                table: "Shipments",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CarrierId",
                table: "SalesOrders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedDeliveryDate",
                table: "SalesOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Carriers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    ContactName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TrackingUrl = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carriers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Carriers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_CarrierId",
                table: "Shipments",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CarrierId",
                table: "SalesOrders",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_Carriers_TenantId",
                table: "Carriers",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Carriers_CarrierId",
                table: "SalesOrders",
                column: "CarrierId",
                principalTable: "Carriers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Carriers_CarrierId",
                table: "Shipments",
                column: "CarrierId",
                principalTable: "Carriers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Carriers_CarrierId",
                table: "SalesOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Carriers_CarrierId",
                table: "Shipments");

            migrationBuilder.DropTable(
                name: "Carriers");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_CarrierId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_CarrierId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CarrierId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CarrierId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "EstimatedDeliveryDate",
                table: "SalesOrders");

            migrationBuilder.AddColumn<string>(
                name: "Carrier",
                table: "Shipments",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }
    }
}
