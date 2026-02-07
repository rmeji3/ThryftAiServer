using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThryftAiServer.Migrations
{
    /// <inheritdoc />
    public partial class ManyToManyOutfits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FashionProducts_Outfits_OutfitId",
                table: "FashionProducts");

            migrationBuilder.DropIndex(
                name: "IX_FashionProducts_OutfitId",
                table: "FashionProducts");

            migrationBuilder.DropColumn(
                name: "OutfitId",
                table: "FashionProducts");

            migrationBuilder.CreateTable(
                name: "FashionProductOutfit",
                columns: table => new
                {
                    ItemsId = table.Column<int>(type: "integer", nullable: false),
                    OutfitsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FashionProductOutfit", x => new { x.ItemsId, x.OutfitsId });
                    table.ForeignKey(
                        name: "FK_FashionProductOutfit_FashionProducts_ItemsId",
                        column: x => x.ItemsId,
                        principalTable: "FashionProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FashionProductOutfit_Outfits_OutfitsId",
                        column: x => x.OutfitsId,
                        principalTable: "Outfits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FashionProductOutfit_OutfitsId",
                table: "FashionProductOutfit",
                column: "OutfitsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FashionProductOutfit");

            migrationBuilder.AddColumn<int>(
                name: "OutfitId",
                table: "FashionProducts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FashionProducts_OutfitId",
                table: "FashionProducts",
                column: "OutfitId");

            migrationBuilder.AddForeignKey(
                name: "FK_FashionProducts_Outfits_OutfitId",
                table: "FashionProducts",
                column: "OutfitId",
                principalTable: "Outfits",
                principalColumn: "Id");
        }
    }
}
