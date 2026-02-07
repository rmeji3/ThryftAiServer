using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThryftAiServer.Migrations
{
    /// <inheritdoc />
    public partial class AddOutfitname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Outfits",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Outfits");
        }
    }
}
