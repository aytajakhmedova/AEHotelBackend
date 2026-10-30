using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AEHotel.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261030160000_CouponCodes")]
public partial class CouponCodes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Code",
            table: "DiscountPackages",
            type: "nvarchar(40)",
            maxLength: 40,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CouponCode",
            table: "Bookings",
            type: "nvarchar(40)",
            maxLength: 40,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<decimal>(
            name: "DiscountAmount",
            table: "Bookings",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.CreateIndex(
            name: "IX_DiscountPackages_Code",
            table: "DiscountPackages",
            column: "Code",
            unique: true,
            filter: "[Code] IS NOT NULL AND [Code] <> ''");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_DiscountPackages_Code",
            table: "DiscountPackages");

        migrationBuilder.DropColumn(name: "Code", table: "DiscountPackages");
        migrationBuilder.DropColumn(name: "CouponCode", table: "Bookings");
        migrationBuilder.DropColumn(name: "DiscountAmount", table: "Bookings");
    }
}
