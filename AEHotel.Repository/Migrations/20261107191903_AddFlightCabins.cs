using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AEHotel.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightCabins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CabinClass",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "FlightCabins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlightId = table.Column<int>(type: "int", nullable: false),
                    CabinClass = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SeatsAvailable = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightCabins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightCabins_Flights_FlightId",
                        column: x => x.FlightId,
                        principalTable: "Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlightCabins_FlightId_CabinClass",
                table: "FlightCabins",
                columns: new[] { "FlightId", "CabinClass" },
                unique: true);

            // Every existing flight keeps selling the class it had, with its price and remaining seats.
            // The other classes can be added to it in the admin panel.
            migrationBuilder.Sql(
                "INSERT INTO FlightCabins (FlightId, CabinClass, Price, SeatsAvailable) " +
                "SELECT Id, CASE WHEN LOWER(CabinClass) IN ('economy', 'business', 'first') THEN LOWER(CabinClass) ELSE 'economy' END, Price, SeatsAvailable FROM Flights");
            migrationBuilder.Sql(
                "UPDATE b SET b.CabinClass = ISNULL((SELECT TOP 1 c.CabinClass FROM FlightCabins c WHERE c.FlightId = b.FlightId), 'economy') " +
                "FROM Bookings b WHERE b.BookingType = 'flight'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlightCabins");

            migrationBuilder.DropColumn(
                name: "CabinClass",
                table: "Bookings");
        }
    }
}
