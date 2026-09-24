using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MvcApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdZoneFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BannerHeight",
                table: "AdZones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BannerWidth",
                table: "AdZones",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AdZones",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BannerHeight", "BannerWidth" },
                values: new object[] { 250, 970 });

            migrationBuilder.UpdateData(
                table: "AdZones",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "BannerHeight", "BannerWidth" },
                values: new object[] { 250, 300 });

            migrationBuilder.UpdateData(
                table: "AdZones",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "BannerHeight", "BannerWidth" },
                values: new object[] { 250, 300 });

            migrationBuilder.UpdateData(
                table: "AdZones",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "BannerHeight", "BannerWidth" },
                values: new object[] { 90, 728 });

            migrationBuilder.UpdateData(
                table: "AdZones",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "BannerHeight", "BannerWidth" },
                values: new object[] { 250, 970 });

            migrationBuilder.UpdateData(
                table: "AdZones",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "BannerHeight", "BannerWidth" },
                values: new object[] { 90, 728 });

            migrationBuilder.UpdateData(
                table: "AdZones",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "BannerHeight", "BannerWidth" },
                values: new object[] { 90, 728 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerHeight",
                table: "AdZones");

            migrationBuilder.DropColumn(
                name: "BannerWidth",
                table: "AdZones");
        }
    }
}
