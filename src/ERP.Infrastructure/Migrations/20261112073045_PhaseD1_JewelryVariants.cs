using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP_HRM_CMS.Migrations
{
    /// <inheritdoc />
    public partial class PhaseD1_JewelryVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Tạo các bảng mới trước: JewelryMaterials, GoldPrices, ProductVariants
            migrationBuilder.CreateTable(
                name: "GoldPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EffectiveDate = table.Column<DateTime>(type: "date", nullable: false),
                    BuyPricePerGram = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    SellPricePerGram = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoldPrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JewelryMaterials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Metal = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Karat = table.Column<int>(type: "int", nullable: true),
                    PurityFactor = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JewelryMaterials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Size = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MaterialId = table.Column<int>(type: "int", nullable: true),
                    WeightGram = table.Column<decimal>(type: "decimal(10,3)", nullable: true),
                    PricingMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FixedPrice = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    LaborFee = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    StoneFee = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_JewelryMaterials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "JewelryMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 2. Mở rộng bảng Products (thông số đá quý)
            migrationBuilder.AddColumn<decimal>(
                name: "Carat",
                table: "Products",
                type: "decimal(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateNo",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GemClarity",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GemColor",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GemstoneType",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // 3. Thêm cột VariantId (nullable) vào các bảng giao dịch & tồn kho
            migrationBuilder.AddColumn<int>(
                name: "VariantId",
                table: "Inventories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VariantId",
                table: "InventoryTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VariantId",
                table: "OrderDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VariantId",
                table: "CartItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VariantId",
                table: "PurchaseOrderDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VariantId",
                table: "StocktakingDetails",
                type: "int",
                nullable: true);

            // 4. Seed dữ liệu ban đầu cho JewelryMaterials (B2)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM dbo.JewelryMaterials WHERE Code = 'GOLD_10K')
                    INSERT INTO dbo.JewelryMaterials (Code, Name, Metal, Karat, PurityFactor, IsActive, CreatedAt)
                    VALUES ('GOLD_10K', N'Vàng 10K', 'Gold', 10, 0.4167, 1, SYSUTCDATETIME());

                IF NOT EXISTS (SELECT 1 FROM dbo.JewelryMaterials WHERE Code = 'GOLD_14K')
                    INSERT INTO dbo.JewelryMaterials (Code, Name, Metal, Karat, PurityFactor, IsActive, CreatedAt)
                    VALUES ('GOLD_14K', N'Vàng 14K', 'Gold', 14, 0.5833, 1, SYSUTCDATETIME());

                IF NOT EXISTS (SELECT 1 FROM dbo.JewelryMaterials WHERE Code = 'GOLD_18K')
                    INSERT INTO dbo.JewelryMaterials (Code, Name, Metal, Karat, PurityFactor, IsActive, CreatedAt)
                    VALUES ('GOLD_18K', N'Vàng 18K', 'Gold', 18, 0.7500, 1, SYSUTCDATETIME());

                IF NOT EXISTS (SELECT 1 FROM dbo.JewelryMaterials WHERE Code = 'GOLD_24K')
                    INSERT INTO dbo.JewelryMaterials (Code, Name, Metal, Karat, PurityFactor, IsActive, CreatedAt)
                    VALUES ('GOLD_24K', N'Vàng 24K', 'Gold', 24, 0.9999, 1, SYSUTCDATETIME());

                IF NOT EXISTS (SELECT 1 FROM dbo.JewelryMaterials WHERE Code = 'SILVER_925')
                    INSERT INTO dbo.JewelryMaterials (Code, Name, Metal, Karat, PurityFactor, IsActive, CreatedAt)
                    VALUES ('SILVER_925', N'Bạc 925', 'Silver', NULL, 0.9250, 1, SYSUTCDATETIME());

                IF NOT EXISTS (SELECT 1 FROM dbo.JewelryMaterials WHERE Code = 'PLATINUM_950')
                    INSERT INTO dbo.JewelryMaterials (Code, Name, Metal, Karat, PurityFactor, IsActive, CreatedAt)
                    VALUES ('PLATINUM_950', N'Platinum 950', 'Platinum', NULL, 0.9500, 1, SYSUTCDATETIME());

                IF NOT EXISTS (SELECT 1 FROM dbo.JewelryMaterials WHERE Code = 'OTHER')
                    INSERT INTO dbo.JewelryMaterials (Code, Name, Metal, Karat, PurityFactor, IsActive, CreatedAt)
                    VALUES ('OTHER', N'Khác', 'Other', NULL, NULL, 1, SYSUTCDATETIME());
            ");

            // 5. Backfill dữ liệu (B6):
            //    Mỗi Product sinh đúng một ProductVariant mặc định:
            //    - IsDefault = 1
            //    - Sku lấy từ Product.SKU (nếu rỗng lấy 'P' + Id)
            //    - PricingMode = 'Fixed'
            //    - FixedPrice lấy từ Product.SalePrice
            //    - MaterialId trỏ tới 'OTHER'
            migrationBuilder.Sql(@"
                DECLARE @OtherMaterialId INT = (SELECT TOP 1 Id FROM dbo.JewelryMaterials WHERE Code = 'OTHER');

                INSERT INTO dbo.ProductVariants (
                    ProductId, Sku, Size, MaterialId, WeightGram, PricingMode, FixedPrice, LaborFee, StoneFee, IsDefault, IsActive, CreatedAt
                )
                SELECT 
                    p.Id,
                    CASE 
                        WHEN p.SKU IS NOT NULL AND RTRIM(LTRIM(p.SKU)) <> '' THEN RTRIM(LTRIM(p.SKU))
                        ELSE 'P' + CAST(p.Id AS NVARCHAR(20))
                    END,
                    NULL,
                    @OtherMaterialId,
                    NULL,
                    'Fixed',
                    CAST(ROUND(p.SalePrice, 0) AS DECIMAL(18,0)),
                    0,
                    0,
                    1,
                    1,
                    SYSUTCDATETIME()
                FROM dbo.Products p
                WHERE NOT EXISTS (
                    SELECT 1 FROM dbo.ProductVariants pv WHERE pv.ProductId = p.Id AND pv.IsDefault = 1
                );
            ");

            // 6. Gán VariantId của biến thể mặc định cho Inventories và các bảng giao dịch
            migrationBuilder.Sql(@"
                -- Inventories
                UPDATE i
                SET i.VariantId = pv.Id
                FROM dbo.Inventories i
                INNER JOIN dbo.ProductVariants pv ON pv.ProductId = i.ProductId AND pv.IsDefault = 1
                WHERE i.VariantId IS NULL OR i.VariantId = 0;

                -- InventoryTransactions
                UPDATE it
                SET it.VariantId = pv.Id
                FROM dbo.InventoryTransactions it
                INNER JOIN dbo.ProductVariants pv ON pv.ProductId = it.ProductId AND pv.IsDefault = 1
                WHERE it.VariantId IS NULL;

                -- OrderDetails
                UPDATE od
                SET od.VariantId = pv.Id
                FROM dbo.OrderDetails od
                INNER JOIN dbo.ProductVariants pv ON pv.ProductId = od.ProductId AND pv.IsDefault = 1
                WHERE od.VariantId IS NULL;

                -- CartItems
                UPDATE ci
                SET ci.VariantId = pv.Id
                FROM dbo.CartItems ci
                INNER JOIN dbo.ProductVariants pv ON pv.ProductId = ci.ProductId AND pv.IsDefault = 1
                WHERE ci.VariantId IS NULL;

                -- PurchaseOrderDetails
                UPDATE pod
                SET pod.VariantId = pv.Id
                FROM dbo.PurchaseOrderDetails pod
                INNER JOIN dbo.ProductVariants pv ON pv.ProductId = pod.ProductId AND pv.IsDefault = 1
                WHERE pod.VariantId IS NULL;

                -- StocktakingDetails
                UPDATE sd
                SET sd.VariantId = pv.Id
                FROM dbo.StocktakingDetails sd
                INNER JOIN dbo.ProductVariants pv ON pv.ProductId = sd.ProductId AND pv.IsDefault = 1
                WHERE sd.VariantId IS NULL;
            ");

            // 7. Chuyển Inventories.VariantId thành NOT NULL (B4)
            migrationBuilder.AlterColumn<int>(
                name: "VariantId",
                table: "Inventories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // 8. Tạo Indexes và Foreign Keys
            migrationBuilder.CreateIndex(
                name: "IX_StocktakingDetails_VariantId",
                table: "StocktakingDetails",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_VariantId",
                table: "PurchaseOrderDetails",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_VariantId",
                table: "OrderDetails",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_VariantId",
                table: "InventoryTransactions",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_VariantId_BranchId",
                table: "Inventories",
                columns: new[] { "VariantId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_VariantId",
                table: "CartItems",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_GoldPrices_EffectiveDate",
                table: "GoldPrices",
                column: "EffectiveDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JewelryMaterials_Code",
                table: "JewelryMaterials",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_MaterialId",
                table: "ProductVariants",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Sku",
                table: "ProductVariants",
                column: "Sku",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_ProductVariants_VariantId",
                table: "CartItems",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_ProductVariants_VariantId",
                table: "Inventories",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_ProductVariants_VariantId",
                table: "InventoryTransactions",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_ProductVariants_VariantId",
                table: "OrderDetails",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderDetails_ProductVariants_VariantId",
                table: "PurchaseOrderDetails",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StocktakingDetails_ProductVariants_VariantId",
                table: "StocktakingDetails",
                column: "VariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_ProductVariants_VariantId",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_ProductVariants_VariantId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_ProductVariants_VariantId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_ProductVariants_VariantId",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderDetails_ProductVariants_VariantId",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_StocktakingDetails_ProductVariants_VariantId",
                table: "StocktakingDetails");

            migrationBuilder.DropTable(
                name: "GoldPrices");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "JewelryMaterials");

            migrationBuilder.DropIndex(
                name: "IX_StocktakingDetails_VariantId",
                table: "StocktakingDetails");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderDetails_VariantId",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_VariantId",
                table: "OrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_VariantId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Inventories_VariantId_BranchId",
                table: "Inventories");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_VariantId",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "StocktakingDetails");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropColumn(
                name: "Carat",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CertificateNo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "GemClarity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "GemColor",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "GemstoneType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "CartItems");
        }
    }
}
