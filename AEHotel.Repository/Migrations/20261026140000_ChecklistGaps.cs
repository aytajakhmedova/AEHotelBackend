using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AEHotel.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261026140000_ChecklistGaps")]
public partial class ChecklistGaps : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "AssignedHotelId",
            table: "AspNetUsers",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CancellationPolicy",
            table: "Hotels",
            type: "nvarchar(1000)",
            maxLength: 1000,
            nullable: false,
            defaultValue: "Check-in tarixindən əvvəl ləğv etmək olar.");

        migrationBuilder.AddColumn<string>(
            name: "HousekeepingStatus",
            table: "Rooms",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "ready");

        migrationBuilder.CreateTable(
            name: "StayReviews",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                HotelId = table.Column<int>(type: "int", nullable: false),
                BookingId = table.Column<int>(type: "int", nullable: false),
                UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                GuestName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                Rating = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                Comment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StayReviews", x => x.Id);
                table.ForeignKey(
                    name: "FK_StayReviews_Bookings_BookingId",
                    column: x => x.BookingId,
                    principalTable: "Bookings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_StayReviews_Hotels_HotelId",
                    column: x => x.HotelId,
                    principalTable: "Hotels",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_StayReviews_BookingId",
            table: "StayReviews",
            column: "BookingId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_StayReviews_HotelId",
            table: "StayReviews",
            column: "HotelId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "StayReviews");
        migrationBuilder.DropColumn(name: "AssignedHotelId", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "CancellationPolicy", table: "Hotels");
        migrationBuilder.DropColumn(name: "HousekeepingStatus", table: "Rooms");
    }
}
