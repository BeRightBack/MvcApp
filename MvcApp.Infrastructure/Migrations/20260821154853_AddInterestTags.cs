using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace MvcApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInterestTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterestTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    IsCurated = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestTags", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserInterestTags",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    UserDetailsId = table.Column<string>(type: "varchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInterestTags", x => new { x.UserId, x.TagId });
                    table.ForeignKey(
                        name: "FK_UserInterestTags_InterestTags_TagId",
                        column: x => x.TagId,
                        principalTable: "InterestTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserInterestTags_User_UserDetailsId",
                        column: x => x.UserDetailsId,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserInterestTags_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_InterestTags_Name_Category",
                table: "InterestTags",
                columns: new[] { "Name", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserInterestTags_TagId",
                table: "UserInterestTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInterestTags_UserDetailsId",
                table: "UserInterestTags",
                column: "UserDetailsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserInterestTags");

            migrationBuilder.DropTable(
                name: "InterestTags");
        }
    }
}
