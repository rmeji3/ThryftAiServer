using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ThryftAiServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialOutfitTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OutfitId",
                table: "FashionProducts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Outfits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outfits", x => x.Id);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FashionProducts_Outfits_OutfitId",
                table: "FashionProducts");

            migrationBuilder.DropTable(
                name: "Outfits");

            migrationBuilder.DropIndex(
                name: "IX_FashionProducts_OutfitId",
                table: "FashionProducts");

            migrationBuilder.DropColumn(
                name: "OutfitId",
                table: "FashionProducts");
        }
    }
}
