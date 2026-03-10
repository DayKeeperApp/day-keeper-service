using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DayKeeper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceNotificationPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceNotificationPreference",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DndEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DndStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DndEndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DefaultReminderLeadTime = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    NotificationSound = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    NotifyEvents = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyTasks = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyLists = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyPeople = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceNotificationPreference", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceNotificationPreference_Device_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Device",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeviceNotificationPreference_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceNotificationPreference_DeviceId",
                table: "DeviceNotificationPreference",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceNotificationPreference_TenantId",
                table: "DeviceNotificationPreference",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceNotificationPreference");
        }
    }
}
