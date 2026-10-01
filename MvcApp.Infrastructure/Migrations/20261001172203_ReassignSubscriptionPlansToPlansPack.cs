using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MvcApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// SubscriptionPlan and SubscriptionDetail used to be seeded by DatingPack. They now belong
    /// to PlansPack, because a plan is not dating content — /Vip and /iptv-store read the same
    /// three tiers, and the IPTV template needs them just as much as Dating does.
    ///
    /// Without this the plan rows stay claimed by Dating, and PlansPack's adopt pass records
    /// them a second time under Plans. Two packs then own the same rows, and removing Dating
    /// would delete tiers the IPTV store is still selling.
    ///
    /// InterestTag rows are deliberately left on Dating — that part never moved.
    ///
    /// The trailing semicolons are load-bearing: Oracle's script generator emits no batch
    /// terminator inside START TRANSACTION, so raw SQL without one swallows the following
    /// INSERT into __EFMigrationsHistory.
    /// </remarks>
    public partial class ReassignSubscriptionPlansToPlansPack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE `SeedManifest` SET `PackName` = 'Plans' "
                + "WHERE `PackName` = 'Dating' AND `EntityType` = 'MvcApp.Core.SubscriptionPlan';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE `SeedManifest` SET `PackName` = 'Dating' "
                + "WHERE `PackName` = 'Plans' AND `EntityType` = 'MvcApp.Core.SubscriptionPlan';");
        }
    }
}
