using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalorieTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FoodLogs_UserId_LoggedAt",
                table: "FoodLogs",
                columns: new[] { "UserId", "LoggedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileHistory_UserId_RecordedAt",
                table: "UserProfileHistory",
                columns: new[] { "UserId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FoodLogs_UserId_LoggedAt",
                table: "FoodLogs");

            migrationBuilder.DropIndex(
                name: "IX_UserProfileHistory_UserId_RecordedAt",
                table: "UserProfileHistory");
        }
    }
}
