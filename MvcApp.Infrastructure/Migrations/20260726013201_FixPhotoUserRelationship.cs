using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MvcApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPhotoUserRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Photo_User_UserDetailsId1",
                table: "Photo");

            migrationBuilder.DropIndex(
                name: "IX_Photo_UserDetailsId1",
                table: "Photo");

            migrationBuilder.DropColumn(
                name: "UserDetailsId1",
                table: "Photo");

            migrationBuilder.AlterColumn<string>(
                name: "UserDetailsId",
                table: "Photo",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Photo_UserDetailsId",
                table: "Photo",
                column: "UserDetailsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Photo_User_UserDetailsId",
                table: "Photo",
                column: "UserDetailsId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Photo_User_UserDetailsId",
                table: "Photo");

            migrationBuilder.DropIndex(
                name: "IX_Photo_UserDetailsId",
                table: "Photo");

            migrationBuilder.AlterColumn<int>(
                name: "UserDetailsId",
                table: "Photo",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)");

            migrationBuilder.AddColumn<string>(
                name: "UserDetailsId1",
                table: "Photo",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Photo_UserDetailsId1",
                table: "Photo",
                column: "UserDetailsId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Photo_User_UserDetailsId1",
                table: "Photo",
                column: "UserDetailsId1",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
