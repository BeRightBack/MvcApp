using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace MvcApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedManifest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeedManifest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PackName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: false),
                    EntityKey = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AppliedBy = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeedManifest", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SeedManifest_PackName_EntityType",
                table: "SeedManifest",
                columns: new[] { "PackName", "EntityType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeedManifest");
        }
    }
}
