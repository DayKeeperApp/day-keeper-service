using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DayKeeper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TaskItem_SpaceId_Status_DueDate",
                table: "TaskItem",
                columns: new[] { "SpaceId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItem_SpaceId_UpdatedAt",
                table: "TaskItem",
                columns: new[] { "SpaceId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingList_SpaceId_UpdatedAt",
                table: "ShoppingList",
                columns: new[] { "SpaceId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Project_SpaceId_UpdatedAt",
                table: "Project",
                columns: new[] { "SpaceId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Person_SpaceId_UpdatedAt",
                table: "Person",
                columns: new[] { "SpaceId", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvent_CalendarId_StartDate",
                table: "CalendarEvent",
                columns: new[] { "CalendarId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Calendar_SpaceId_UpdatedAt",
                table: "Calendar",
                columns: new[] { "SpaceId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskItem_SpaceId_Status_DueDate",
                table: "TaskItem");

            migrationBuilder.DropIndex(
                name: "IX_TaskItem_SpaceId_UpdatedAt",
                table: "TaskItem");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingList_SpaceId_UpdatedAt",
                table: "ShoppingList");

            migrationBuilder.DropIndex(
                name: "IX_Project_SpaceId_UpdatedAt",
                table: "Project");

            migrationBuilder.DropIndex(
                name: "IX_Person_SpaceId_UpdatedAt",
                table: "Person");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEvent_CalendarId_StartDate",
                table: "CalendarEvent");

            migrationBuilder.DropIndex(
                name: "IX_Calendar_SpaceId_UpdatedAt",
                table: "Calendar");
        }
    }
}
