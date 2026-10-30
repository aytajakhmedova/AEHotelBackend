using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AEHotel.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261030150000_VisibleReviewCatalog")]
public partial class VisibleReviewCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_StayReviews_BookingId",
            table: "StayReviews");

        migrationBuilder.AlterColumn<int>(
            name: "BookingId",
            table: "StayReviews",
            type: "int",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "int");

        migrationBuilder.CreateIndex(
            name: "IX_StayReviews_BookingId",
            table: "StayReviews",
            column: "BookingId",
            unique: true,
            filter: "[BookingId] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_StayReviews_BookingId",
            table: "StayReviews");

        migrationBuilder.AlterColumn<int>(
            name: "BookingId",
            table: "StayReviews",
            type: "int",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_StayReviews_BookingId",
            table: "StayReviews",
            column: "BookingId",
            unique: true);
    }
}
