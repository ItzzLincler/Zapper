using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zapper.Migrations
{
    public partial class ProductType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductSource",
                table: "ProductPriceChanges");

            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "ScrapedProducts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "ScrapedProducts");

            migrationBuilder.AddColumn<int>(
                name: "ProductSource",
                table: "ProductPriceChanges",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
