using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AEHotel.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddFavoriteHotels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FavoriteHotels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    HotelId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteHotels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FavoriteHotels_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "Hotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteHotels_HotelId",
                table: "FavoriteHotels",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteHotels_UserId_HotelId",
                table: "FavoriteHotels",
                columns: new[] { "UserId", "HotelId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoriteHotels");
        }
    }
}
