using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zapper.Api.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScrapedProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Cat = table.Column<string>(type: "text", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastChecked = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastChanged = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPrice = table.Column<double>(type: "double precision", nullable: true),
                    LowestPrice = table.Column<double>(type: "double precision", nullable: true),
                    HighestPrice = table.Column<double>(type: "double precision", nullable: true),
                    ProductLink = table.Column<string>(type: "text", nullable: false),
                    ProductSource = table.Column<int>(type: "integer", nullable: false),
                    ProductType = table.Column<string>(type: "text", nullable: false),
                    ImageUri = table.Column<string>(type: "text", nullable: false),
                    ImagePath = table.Column<string>(type: "text", nullable: true),
                    HasImage = table.Column<bool>(type: "boolean", nullable: false),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    ManufacturerSerial = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapedProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductPriceChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Changed = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousPrice = table.Column<double>(type: "double precision", nullable: true),
                    CurrentPrice = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPriceChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPriceChanges_ScrapedProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "ScrapedProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductPriceChanges_ProductId",
                table: "ProductPriceChanges",
                column: "ProductId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductPriceChanges");

            migrationBuilder.DropTable(
                name: "ScrapedProducts");
        }
    }
}
