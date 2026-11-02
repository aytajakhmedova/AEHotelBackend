using AEHotel.Repository.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AEHotel.Repository.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261101120000_PageHeroContent")]
public partial class PageHeroContent : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "HotelsPageTitle",
            table: "SiteSettings",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: "Otelləri Kəşf Et");

        migrationBuilder.AddColumn<string>(
            name: "HotelsPageSubtitle",
            table: "SiteSettings",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: false,
            defaultValue: "Lüks qonaq evlərindən tutmuş büdcəli seçimlərə qədər");

        migrationBuilder.AddColumn<string>(
            name: "HotelsPageBannerUrl",
            table: "SiteSettings",
            type: "nvarchar(400)",
            maxLength: 400,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "HotelsPageVideoUrl",
            table: "SiteSettings",
            type: "nvarchar(400)",
            maxLength: 400,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "FlightsPageTitle",
            table: "SiteSettings",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: "Uçuşları Kəşf Et");

        migrationBuilder.AddColumn<string>(
            name: "FlightsPageSubtitle",
            table: "SiteSettings",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: false,
            defaultValue: "Dünya üzrə ən yaxşı qiymətlərlə uçuşlar");

        migrationBuilder.AddColumn<string>(
            name: "FlightsPageBannerUrl",
            table: "SiteSettings",
            type: "nvarchar(400)",
            maxLength: 400,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "FlightsPageVideoUrl",
            table: "SiteSettings",
            type: "nvarchar(400)",
            maxLength: 400,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "ToursPageTitle",
            table: "SiteSettings",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: "Turları Kəşf Et");

        migrationBuilder.AddColumn<string>(
            name: "ToursPageSubtitle",
            table: "SiteSettings",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: false,
            defaultValue: "Azərbaycanın gözəl yerlərini kəşf edin");

        migrationBuilder.AddColumn<string>(
            name: "ToursPageBannerUrl",
            table: "SiteSettings",
            type: "nvarchar(400)",
            maxLength: 400,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "ToursPageVideoUrl",
            table: "SiteSettings",
            type: "nvarchar(400)",
            maxLength: 400,
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "HotelsPageTitle", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "HotelsPageSubtitle", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "HotelsPageBannerUrl", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "HotelsPageVideoUrl", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "FlightsPageTitle", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "FlightsPageSubtitle", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "FlightsPageBannerUrl", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "FlightsPageVideoUrl", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "ToursPageTitle", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "ToursPageSubtitle", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "ToursPageBannerUrl", table: "SiteSettings");
        migrationBuilder.DropColumn(name: "ToursPageVideoUrl", table: "SiteSettings");
    }
}
