using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP_HRM_CMS.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeedHistories",
                columns: table => new
                {
                    SeedKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeedHistories", x => x.SeedKey);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeedHistories");
        }
    }
}
