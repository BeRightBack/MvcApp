using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MvcApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApplyInfrastructureEntityConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_EventCategories_CategoryId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_User_ReportedUserId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_User_ReporterId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_EventRSVPs_Events_EventId",
                table: "EventRSVPs");

            migrationBuilder.DropIndex(
                name: "IX_EventRSVPs_EventId",
                table: "EventRSVPs");

            migrationBuilder.CreateIndex(
                name: "IX_VideoUploads_Category",
                table: "VideoUploads",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_VideoUploads_IsApproved",
                table: "VideoUploads",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_VideoUploads_UploadedAt",
                table: "VideoUploads",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_Status",
                table: "VerificationRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_CreatedAt",
                table: "Reports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Status",
                table: "Reports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Events_City",
                table: "Events",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventDate",
                table: "Events",
                column: "EventDate");

            migrationBuilder.CreateIndex(
                name: "IX_EventRSVPs_EventId_UserId",
                table: "EventRSVPs",
                columns: new[] { "EventId", "UserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EventRSVPs_Events_EventId",
                table: "EventRSVPs",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_EventCategories_CategoryId",
                table: "Events",
                column: "CategoryId",
                principalTable: "EventCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_User_ReportedUserId",
                table: "Reports",
                column: "ReportedUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_User_ReporterId",
                table: "Reports",
                column: "ReporterId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_EventCategories_CategoryId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_User_ReportedUserId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_User_ReporterId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_VideoUploads_Category",
                table: "VideoUploads");

            migrationBuilder.DropIndex(
                name: "IX_VideoUploads_IsApproved",
                table: "VideoUploads");

            migrationBuilder.DropIndex(
                name: "IX_VideoUploads_UploadedAt",
                table: "VideoUploads");

            migrationBuilder.DropIndex(
                name: "IX_VerificationRequests_Status",
                table: "VerificationRequests");

            migrationBuilder.DropIndex(
                name: "IX_Reports_CreatedAt",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_Status",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Events_City",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_EventDate",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_EventRSVPs_Events_EventId",
                table: "EventRSVPs");

            migrationBuilder.DropIndex(
                name: "IX_EventRSVPs_EventId_UserId",
                table: "EventRSVPs");

            migrationBuilder.CreateIndex(
                name: "IX_EventRSVPs_EventId",
                table: "EventRSVPs",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventRSVPs_Events_EventId",
                table: "EventRSVPs",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_EventCategories_CategoryId",
                table: "Events",
                column: "CategoryId",
                principalTable: "EventCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_User_ReportedUserId",
                table: "Reports",
                column: "ReportedUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_User_ReporterId",
                table: "Reports",
                column: "ReporterId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
