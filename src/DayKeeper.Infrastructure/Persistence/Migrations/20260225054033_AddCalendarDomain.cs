using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DayKeeper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarEvent_EventType_EventTypeId",
                table: "CalendarEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_EventType_Tenant_TenantId",
                table: "EventType");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "EventType",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "EventType",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "EventType",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "EventType",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Method",
                table: "EventReminder",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "CalendarEvent",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Timezone",
                table: "CalendarEvent",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "RecurrenceRule",
                table: "CalendarEvent",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "CalendarEvent",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "Calendar",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Calendar",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "Calendar",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_EventType_TenantId_NormalizedName",
                table: "EventType",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvent_CalendarId_StartAt",
                table: "CalendarEvent",
                columns: new[] { "CalendarId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Calendar_SpaceId_NormalizedName",
                table: "Calendar",
                columns: new[] { "SpaceId", "NormalizedName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarEvent_EventType_EventTypeId",
                table: "CalendarEvent",
                column: "EventTypeId",
                principalTable: "EventType",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EventType_Tenant_TenantId",
                table: "EventType",
                column: "TenantId",
                principalTable: "Tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarEvent_EventType_EventTypeId",
                table: "CalendarEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_EventType_Tenant_TenantId",
                table: "EventType");

            migrationBuilder.DropIndex(
                name: "IX_EventType_TenantId_NormalizedName",
                table: "EventType");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEvent_CalendarId_StartAt",
                table: "CalendarEvent");

            migrationBuilder.DropIndex(
                name: "IX_Calendar_SpaceId_NormalizedName",
                table: "Calendar");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "EventType",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "EventType",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "EventType",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "EventType",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<int>(
                name: "Method",
                table: "EventReminder",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "CalendarEvent",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "Timezone",
                table: "CalendarEvent",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "RecurrenceRule",
                table: "CalendarEvent",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "CalendarEvent",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "Calendar",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Calendar",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "Calendar",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarEvent_EventType_EventTypeId",
                table: "CalendarEvent",
                column: "EventTypeId",
                principalTable: "EventType",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EventType_Tenant_TenantId",
                table: "EventType",
                column: "TenantId",
                principalTable: "Tenant",
                principalColumn: "Id");
        }
    }
}
