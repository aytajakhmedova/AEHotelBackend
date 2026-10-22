using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AEHotel.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261022160000_RemoveTaxiTransfers")]
public partial class RemoveTaxiTransfers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TaxiTransfers");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TaxiTransfers",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Origin = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Destination = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                VehicleType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Duration = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Seats = table.Column<int>(type: "int", nullable: false),
                ImageFile = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TaxiTransfers", x => x.Id);
            });
    }
}
