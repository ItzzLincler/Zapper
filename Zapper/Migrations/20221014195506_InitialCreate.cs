using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zapper.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductPriceChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Changed = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousPrice = table.Column<double>(type: "double precision", nullable: false),
                    CurrentPrice = table.Column<double>(type: "double precision", nullable: false),
                    ProductSource = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPriceChanges", x => x.Id);
                });

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
                    CurrentPrice = table.Column<double>(type: "double precision", nullable: false),
                    LowestPrice = table.Column<double>(type: "double precision", nullable: false),
                    HighestPrice = table.Column<double>(type: "double precision", nullable: false),
                    ProductLink = table.Column<string>(type: "text", nullable: false),
                    ProductSource = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapedProducts", x => x.Id);
                });
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
