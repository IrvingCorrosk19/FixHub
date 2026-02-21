using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FASE14_Hardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "proposals",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_retry_at",
                table: "notification_outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "notification_outbox",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "jobs",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<string>(
                name: "resolution_note",
                table: "job_issues",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "resolved_at",
                table: "job_issues",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "resolved_by_user_id",
                table: "job_issues",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "resolved_at",
                table: "job_alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "resolved_by_user_id",
                table: "job_alerts",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "proposals");

            migrationBuilder.DropColumn(
                name: "next_retry_at",
                table: "notification_outbox");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "notification_outbox");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "resolution_note",
                table: "job_issues");

            migrationBuilder.DropColumn(
                name: "resolved_at",
                table: "job_issues");

            migrationBuilder.DropColumn(
                name: "resolved_by_user_id",
                table: "job_issues");

            migrationBuilder.DropColumn(
                name: "resolved_at",
                table: "job_alerts");

            migrationBuilder.DropColumn(
                name: "resolved_by_user_id",
                table: "job_alerts");
        }
    }
}
