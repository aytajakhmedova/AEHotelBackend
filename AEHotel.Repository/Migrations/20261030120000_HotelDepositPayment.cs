using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AEHotel.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261030120000_HotelDepositPayment")]
public partial class HotelDepositPayment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "DepositAmount",
            table: "Payments",
            type: "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<int>(
            name: "DepositPercent",
            table: "Payments",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DepositAmount", table: "Payments");
        migrationBuilder.DropColumn(name: "DepositPercent", table: "Payments");
    }
}
