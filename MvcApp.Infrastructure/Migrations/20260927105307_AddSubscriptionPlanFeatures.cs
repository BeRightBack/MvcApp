using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MvcApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlanFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Features",
                table: "SubscriptionPlans",
                type: "longtext",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Features",
                table: "SubscriptionPlans");
        }
    }
}
