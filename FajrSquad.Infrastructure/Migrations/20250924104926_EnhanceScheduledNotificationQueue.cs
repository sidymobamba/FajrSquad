using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FajrSquad.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceScheduledNotificationQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "ScheduledNotifications",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRetries",
                table: "ScheduledNotifications",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextRetryAt",
                table: "ScheduledNotifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Retries",
                table: "ScheduledNotifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotifications_Status_ExecuteAt",
                table: "ScheduledNotifications",
                columns: new[] { "Status", "ExecuteAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotifications_Status_NextRetryAt",
                table: "ScheduledNotifications",
                columns: new[] { "Status", "NextRetryAt" },
                filter: "\"NextRetryAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledNotifications_UserId_Type_Status",
                table: "ScheduledNotifications",
                columns: new[] { "UserId", "Type", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScheduledNotifications_Status_ExecuteAt",
                table: "ScheduledNotifications");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledNotifications_Status_NextRetryAt",
                table: "ScheduledNotifications");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledNotifications_UserId_Type_Status",
                table: "ScheduledNotifications");

            migrationBuilder.DropColumn(
                name: "MaxRetries",
                table: "ScheduledNotifications");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "ScheduledNotifications");

            migrationBuilder.DropColumn(
                name: "Retries",
                table: "ScheduledNotifications");

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "ScheduledNotifications",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
