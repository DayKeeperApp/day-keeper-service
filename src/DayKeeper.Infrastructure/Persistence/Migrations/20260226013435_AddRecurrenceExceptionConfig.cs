using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DayKeeper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurrenceExceptionConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RecurrenceEndAt",
                table: "CalendarEvent",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecurrenceException",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CalendarEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalStartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Location = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurrenceException", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurrenceException_CalendarEvent_CalendarEventId",
                        column: x => x.CalendarEventId,
                        principalTable: "CalendarEvent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvent_CalendarId_Recurring",
                table: "CalendarEvent",
                column: "CalendarId",
                filter: "\"RecurrenceRule\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RecurrenceException_CalendarEventId",
                table: "RecurrenceException",
                column: "CalendarEventId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurrenceException_CalendarEventId_OriginalStartAt",
                table: "RecurrenceException",
                columns: new[] { "CalendarEventId", "OriginalStartAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecurrenceException");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEvent_CalendarId_Recurring",
                table: "CalendarEvent");

            migrationBuilder.DropColumn(
                name: "RecurrenceEndAt",
                table: "CalendarEvent");
        }
    }
}
