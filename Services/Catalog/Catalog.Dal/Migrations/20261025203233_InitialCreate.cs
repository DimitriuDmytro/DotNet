using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Catalog.Dal.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Packages",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                    table.CheckConstraint("CK_Packages_Price", "[Price] > 0");
                });

            migrationBuilder.CreateTable(
                name: "ServiceCategories",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DurationMin = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.CheckConstraint("CK_Services_DurationMin", "[DurationMin] > 0");
                    table.CheckConstraint("CK_Services_Price", "[Price] > 0");
                    table.ForeignKey(
                        name: "FK_Services_ServiceCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "dbo",
                        principalTable: "ServiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackageServices",
                schema: "dbo",
                columns: table => new
                {
                    PackageId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceId = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    AddedAt = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageServices", x => new { x.PackageId, x.ServiceId });
                    table.CheckConstraint("CK_PackageServices_SortOrder", "[SortOrder] > 0");
                    table.ForeignKey(
                        name: "FK_PackageServices_Packages_PackageId",
                        column: x => x.PackageId,
                        principalSchema: "dbo",
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageServices_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "dbo",
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceImages",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceId = table.Column<long>(type: "bigint", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsMain = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceImages_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "dbo",
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceSpecs",
                schema: "dbo",
                columns: table => new
                {
                    ServiceId = table.Column<long>(type: "bigint", nullable: false),
                    Materials = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    WarrantyMonths = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSpecs", x => x.ServiceId);
                    table.CheckConstraint("CK_ServiceSpecs_WarrantyMonths", "[WarrantyMonths] >= 0");
                    table.ForeignKey(
                        name: "FK_ServiceSpecs_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "dbo",
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "ServiceCategories",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1L, "Дбайлива багатофазна детейлінг-мийка кузова, підвіски та дисків.", "Мийка" },
                    { 2L, "Відновлювальне, коригувальне та глибоке абразивне полірування кузова і оптики.", "Полірування" },
                    { 3L, "Нанесення кераміки 9H, рідкого скла, антидощу та захисних плівок PPF.", "Захист" },
                    { 4L, "Комплексна хімчистка інтер'єру, озонування та консервація шкіри.", "Салон" }
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Packages_Name",
                schema: "dbo",
                table: "Packages",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackageServices_ServiceId",
                schema: "dbo",
                table: "PackageServices",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "UQ_ServiceCategories_Name",
                schema: "dbo",
                table: "ServiceCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceImages_ServiceId_IsMain",
                schema: "dbo",
                table: "ServiceImages",
                columns: new[] { "ServiceId", "IsMain" });

            migrationBuilder.CreateIndex(
                name: "UX_ServiceImages_ServiceId_MainOnly",
                schema: "dbo",
                table: "ServiceImages",
                column: "ServiceId",
                unique: true,
                filter: "[IsMain] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Services_CategoryId_Price",
                schema: "dbo",
                table: "Services",
                columns: new[] { "CategoryId", "Price" });

            migrationBuilder.CreateIndex(
                name: "IX_Services_Name",
                schema: "dbo",
                table: "Services",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageServices",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ServiceImages",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ServiceSpecs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Packages",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Services",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ServiceCategories",
                schema: "dbo");
        }
    }
}
