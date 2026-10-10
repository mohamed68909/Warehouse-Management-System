using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMSPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPickingWaves : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PickingWaves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WaveNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AssignedPicker = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickingWaves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickingWaves_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StorageLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Zone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Aisle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Rack = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ShelfLevel = table.Column<int>(type: "int", nullable: false),
                    BinNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CapacityUnits = table.Column<int>(type: "int", nullable: false),
                    CurrentStock = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorageLocations_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StorageLocations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PickItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PickingWaveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductSku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StorageLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Zone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Aisle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Rack = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ShelfLevel = table.Column<int>(type: "int", nullable: false),
                    BinNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SequenceOrder = table.Column<int>(type: "int", nullable: false),
                    QuantityRequested = table.Column<int>(type: "int", nullable: false),
                    QuantityPicked = table.Column<int>(type: "int", nullable: false),
                    IsPicked = table.Column<bool>(type: "bit", nullable: false),
                    PickedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickItems_PickingWaves_PickingWaveId",
                        column: x => x.PickingWaveId,
                        principalTable: "PickingWaves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PickingWaves_WarehouseId",
                table: "PickingWaves",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PickingWaves_WaveNumber",
                table: "PickingWaves",
                column: "WaveNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PickItems_PickingWaveId",
                table: "PickItems",
                column: "PickingWaveId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_ProductId",
                table: "StorageLocations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_WarehouseId_LocationCode",
                table: "StorageLocations",
                columns: new[] { "WarehouseId", "LocationCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PickItems");

            migrationBuilder.DropTable(
                name: "StorageLocations");

            migrationBuilder.DropTable(
                name: "PickingWaves");
        }
    }
}
