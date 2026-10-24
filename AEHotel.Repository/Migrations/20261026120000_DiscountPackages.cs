using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AEHotel.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261026120000_DiscountPackages")]
public partial class DiscountPackages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DiscountPackages",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Combo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                StartsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                EndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DiscountPackages", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DiscountPackages_Combo",
            table: "DiscountPackages",
            column: "Combo");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DiscountPackages");
    }
}
