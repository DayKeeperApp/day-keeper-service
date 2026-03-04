using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DayKeeper.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeSpaceTenantIdOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Space_TenantId_NormalizedName",
                table: "Space");

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "Space",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_Space_TenantId_NormalizedName",
                table: "Space",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Space_TenantId_NormalizedName",
                table: "Space");

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "Space",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Space_TenantId_NormalizedName",
                table: "Space",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);
        }
    }
}
