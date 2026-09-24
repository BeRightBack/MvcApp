using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MvcApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideoRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    MaxSpots = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<string>(type: "varchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoRooms_User_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "User",
                        principalColumn: "Id");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VideoRoomMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    VideoRoomId = table.Column<int>(type: "int", nullable: false),
                    SenderId = table.Column<string>(type: "varchar(255)", nullable: true),
                    SenderUsername = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    Content = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    MessageSent = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoRoomMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoRoomMessages_User_SenderId",
                        column: x => x.SenderId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VideoRoomMessages_VideoRooms_VideoRoomId",
                        column: x => x.VideoRoomId,
                        principalTable: "VideoRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "VideoRooms",
                columns: new[] { "Id", "CreatedAt", "CreatedById", "Description", "IsActive", "MaxSpots", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, "The main video lounge — come say hello on camera", true, 10, "Main Lounge" },
                    { 2, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, "Meet new people face-to-face", true, 10, "Dating Lounge" },
                    { 3, new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, "Smaller, more intimate room", true, 4, "VIP Lounge" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoRoomMessages_SenderId",
                table: "VideoRoomMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoRoomMessages_VideoRoomId",
                table: "VideoRoomMessages",
                column: "VideoRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoRooms_CreatedById",
                table: "VideoRooms",
                column: "CreatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoRoomMessages");

            migrationBuilder.DropTable(
                name: "VideoRooms");
        }
    }
}
