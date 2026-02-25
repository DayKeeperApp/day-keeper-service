using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DayKeeper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTasksDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Category_Tenant_TenantId",
                table: "Category");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItem_Project_ProjectId",
                table: "TaskItem");

            migrationBuilder.DropIndex(
                name: "IX_TaskCategory_TaskItemId",
                table: "TaskCategory");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "TaskItem",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TaskItem",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "RecurrenceRule",
                table: "TaskItem",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "TaskItem",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "Project",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Project",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "Category",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Category",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Category",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "Category",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItem_SpaceId_Status_DueAt",
                table: "TaskItem",
                columns: new[] { "SpaceId", "Status", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskCategory_TaskItemId_CategoryId",
                table: "TaskCategory",
                columns: new[] { "TaskItemId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Project_SpaceId_NormalizedName",
                table: "Project",
                columns: new[] { "SpaceId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Category_TenantId_NormalizedName",
                table: "Category",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.AddForeignKey(
                name: "FK_Category_Tenant_TenantId",
                table: "Category",
                column: "TenantId",
                principalTable: "Tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItem_Project_ProjectId",
                table: "TaskItem",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Category_Tenant_TenantId",
                table: "Category");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItem_Project_ProjectId",
                table: "TaskItem");

            migrationBuilder.DropIndex(
                name: "IX_TaskItem_SpaceId_Status_DueAt",
                table: "TaskItem");

            migrationBuilder.DropIndex(
                name: "IX_TaskCategory_TaskItemId_CategoryId",
                table: "TaskCategory");

            migrationBuilder.DropIndex(
                name: "IX_Project_SpaceId_NormalizedName",
                table: "Project");

            migrationBuilder.DropIndex(
                name: "IX_Category_TenantId_NormalizedName",
                table: "Category");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "TaskItem",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "TaskItem",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "RecurrenceRule",
                table: "TaskItem",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Priority",
                table: "TaskItem",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "Project",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Project",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "Category",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Category",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Category",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Color",
                table: "Category",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.CreateIndex(
                name: "IX_TaskCategory_TaskItemId",
                table: "TaskCategory",
                column: "TaskItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Category_Tenant_TenantId",
                table: "Category",
                column: "TenantId",
                principalTable: "Tenant",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItem_Project_ProjectId",
                table: "TaskItem",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id");
        }
    }
}
