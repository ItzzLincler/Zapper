using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zapper.Api.Migrations
{
    public partial class hasimage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasImage",
                table: "ScrapedProducts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasImage",
                table: "ScrapedProducts");
        }
    }
}
