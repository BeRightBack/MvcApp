using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MvcApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdsEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdZones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    MaxBanners = table.Column<int>(type: "int", nullable: false),
                    ExcludeFromLandingPage = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DefaultCssClass = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    BannerWrapperTemplate = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdZones", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdBanners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ZoneId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true),
                    TargetUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    AltText = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    CssClass = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TargetRoles = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    TargetCultures = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    MaxImpressionsPerDay = table.Column<int>(type: "int", nullable: false),
                    MaxClicksPerDay = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdBanners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdBanners_AdZones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "AdZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdPlacements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ZoneId = table.Column<int>(type: "int", nullable: false),
                    PageSlug = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false),
                    IsExclusion = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MaxBannersOverride = table.Column<int>(type: "int", nullable: true),
                    CssClassOverride = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    WrapperTemplateOverride = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdPlacements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdPlacements_AdZones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "AdZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdClicks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    BannerId = table.Column<int>(type: "int", nullable: false),
                    PageSlug = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true),
                    ZoneKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    IpHash = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true),
                    Referrer = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdClicks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdClicks_AdBanners_BannerId",
                        column: x => x.BannerId,
                        principalTable: "AdBanners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdImpressions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    BannerId = table.Column<int>(type: "int", nullable: false),
                    PageSlug = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true),
                    ZoneKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    IpHash = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Culture = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    UserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdImpressions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdImpressions_AdBanners_BannerId",
                        column: x => x.BannerId,
                        principalTable: "AdBanners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "AdZones",
                columns: new[] { "Id", "BannerWrapperTemplate", "CreatedAt", "DefaultCssClass", "Description", "DisplayOrder", "ExcludeFromLandingPage", "IsActive", "Key", "MaxBanners", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ad-zone ad-top-header mb-3", "Full-width banner at the very top of the page", 10, true, true, "top-header", 1, "Top Header", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ad-zone ad-sidebar-left mb-3", "Vertical banner in the left sidebar", 20, false, true, "sidebar-left", 2, "Left Sidebar", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ad-zone ad-sidebar-right mb-3", "Vertical banner in the right sidebar", 30, false, true, "sidebar-right", 2, "Right Sidebar", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ad-zone ad-content-top mb-4", "Banner at the top of the main content area", 40, true, true, "content-top", 1, "Content Top", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ad-zone ad-content-bottom mt-4", "Banner at the bottom of the main content area", 50, false, true, "content-bottom", 1, "Content Bottom", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ad-zone ad-footer mt-4", "Banner in the footer area", 60, false, true, "footer", 3, "Footer", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ad-zone ad-in-article my-4", "Banner inserted between paragraphs in article content", 70, true, true, "in-article", 1, "In-Article", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdBanners_ZoneId_IsActive_StartDate_EndDate",
                table: "AdBanners",
                columns: new[] { "ZoneId", "IsActive", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AdBanners_ZoneId_Weight",
                table: "AdBanners",
                columns: new[] { "ZoneId", "Weight" });

            migrationBuilder.CreateIndex(
                name: "IX_AdClicks_BannerId_CreatedAt",
                table: "AdClicks",
                columns: new[] { "BannerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdClicks_PageSlug_CreatedAt",
                table: "AdClicks",
                columns: new[] { "PageSlug", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdImpressions_BannerId_CreatedAt",
                table: "AdImpressions",
                columns: new[] { "BannerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdImpressions_PageSlug_CreatedAt",
                table: "AdImpressions",
                columns: new[] { "PageSlug", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdPlacements_ZoneId_PageSlug",
                table: "AdPlacements",
                columns: new[] { "ZoneId", "PageSlug" });

            migrationBuilder.CreateIndex(
                name: "IX_AdZones_Key",
                table: "AdZones",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdClicks");

            migrationBuilder.DropTable(
                name: "AdImpressions");

            migrationBuilder.DropTable(
                name: "AdPlacements");

            migrationBuilder.DropTable(
                name: "AdBanners");

            migrationBuilder.DropTable(
                name: "AdZones");
        }
    }
}
