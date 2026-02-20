using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MigrateNotificationOutboxToFase13Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notification_outbox_notifications_notification_id",
                table: "notification_outbox");

            migrationBuilder.DropIndex(
                name: "IX_notification_outbox_notification_id",
                table: "notification_outbox");

            migrationBuilder.DropColumn(
                name: "notification_id",
                table: "notification_outbox");

            migrationBuilder.DropColumn(
                name: "payload",
                table: "notification_outbox");

            migrationBuilder.AddColumn<string>(
                name: "html_body",
                table: "notification_outbox",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "job_id",
                table: "notification_outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subject",
                table: "notification_outbox",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "to_email",
                table: "notification_outbox",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_notification_outbox_job_id",
                table: "notification_outbox",
                column: "job_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notification_outbox_job_id",
                table: "notification_outbox");

            migrationBuilder.DropColumn(
                name: "html_body",
                table: "notification_outbox");

            migrationBuilder.DropColumn(
                name: "job_id",
                table: "notification_outbox");

            migrationBuilder.DropColumn(
                name: "subject",
                table: "notification_outbox");

            migrationBuilder.DropColumn(
                name: "to_email",
                table: "notification_outbox");

            migrationBuilder.AddColumn<Guid>(
                name: "notification_id",
                table: "notification_outbox",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "payload",
                table: "notification_outbox",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_notification_outbox_notification_id",
                table: "notification_outbox",
                column: "notification_id");

            migrationBuilder.AddForeignKey(
                name: "FK_notification_outbox_notifications_notification_id",
                table: "notification_outbox",
                column: "notification_id",
                principalTable: "notifications",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
