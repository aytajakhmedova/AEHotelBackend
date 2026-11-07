using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AEHotel.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddTourHotel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HotelId",
                table: "Tours",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tours_HotelId",
                table: "Tours",
                column: "HotelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tours_Hotels_HotelId",
                table: "Tours",
                column: "HotelId",
                principalTable: "Hotels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Existing tours are attached to the first hotel of the city they run in; admins can change it.
            migrationBuilder.Sql(
                "UPDATE t SET t.HotelId = (SELECT TOP 1 h.Id FROM Hotels h WHERE h.IsDeleted = 0 AND (t.Destination LIKE h.City + '%' OR t.Name LIKE h.City + '%') ORDER BY h.Id) FROM Tours t WHERE t.HotelId IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tours_Hotels_HotelId",
                table: "Tours");

            migrationBuilder.DropIndex(
                name: "IX_Tours_HotelId",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "HotelId",
                table: "Tours");
        }
    }
}
