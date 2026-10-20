using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AEHotel.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261022120000_SiteContent")]
public partial class SiteContent : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SiteSettings",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SiteName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                LogoUrl = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                HeroKicker = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                HeroTitleBefore = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                HeroTitleAccent = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                HeroTitleAfter = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                HeroDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                HeroPrimaryButtonText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                HeroPrimaryButtonUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                HeroSecondaryButtonText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                HeroVideoUrl = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                HeroFloatImage1Url = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                HeroFloatImage2Url = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                HeroSupportBadge = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                HeroSupportLabel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                HeroStoryTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                HeroStoryText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                AdvantagesTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                AdvantagesDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                AdvantagesImageUrl = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                AdvantagesRating = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                GalleryTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                FeaturedEyebrow = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                FeaturedTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                FooterDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                FooterLinksTitle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                FooterHoursTitle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                FooterContactTitle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                HoursWeekdays = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                HoursSaturday = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                HoursSunday = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                HoursValue = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                ContactAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                ContactPhone = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                CopyrightText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                PrivacyUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                TermsUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                LinkedInUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                TwitterUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                InstagramUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                FacebookUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                NoticeSummer = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                NoticeSupport = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SiteSettings", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SiteMenuItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Label = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Url = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                Placement = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                IconKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                IsRoute = table.Column<bool>(type: "bit", nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SiteMenuItems", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SiteMenuItems_Placement_SortOrder",
            table: "SiteMenuItems",
            columns: new[] { "Placement", "SortOrder" });

        migrationBuilder.CreateTable(
            name: "SiteSlides",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ImageUrl = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SiteSlides", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SiteAdvantages",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                IconKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SiteAdvantages", x => x.Id);
            });

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

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SiteSettings");
        migrationBuilder.DropTable(name: "SiteMenuItems");
        migrationBuilder.DropTable(name: "SiteSlides");
        migrationBuilder.DropTable(name: "SiteAdvantages");
        migrationBuilder.DropTable(name: "TaxiTransfers");
    }
}
