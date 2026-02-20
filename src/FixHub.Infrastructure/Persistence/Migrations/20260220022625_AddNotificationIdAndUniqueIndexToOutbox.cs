using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationIdAndUniqueIndexToOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "notification_id",
                table: "notification_outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notification_outbox_notification_id_channel",
                table: "notification_outbox",
                columns: new[] { "notification_id", "channel" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notification_outbox_notification_id_channel",
                table: "notification_outbox");

            migrationBuilder.DropColumn(
                name: "notification_id",
                table: "notification_outbox");
        }
    }
}
