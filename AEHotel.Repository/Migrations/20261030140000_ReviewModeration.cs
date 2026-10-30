using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AEHotel.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261030140000_ReviewModeration")]
public partial class ReviewModeration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsApproved",
            table: "StayReviews",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql("UPDATE StayReviews SET IsApproved = 1;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsApproved", table: "StayReviews");
    }
}
