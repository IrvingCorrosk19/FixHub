using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdminModule_UserStatus_AssignmentOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deactivated_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_suspended",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "users",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "suspended_until",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "suspension_reason",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "assignment_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_technician_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_technician_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reason_detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignment_overrides", x => x.id);
                    table.ForeignKey(
                        name: "FK_assignment_overrides_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_is_active = table.Column<bool>(type: "boolean", nullable: false),
                    previous_is_suspended = table.Column<bool>(type: "boolean", nullable: false),
                    new_is_active = table.Column<bool>(type: "boolean", nullable: false),
                    new_is_suspended = table.Column<bool>(type: "boolean", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_status_history_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_status_history_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assignment_overrides_created_at_utc",
                table: "assignment_overrides",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_overrides_job_id",
                table: "assignment_overrides",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_status_history_actor_user_id",
                table: "user_status_history",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_status_history_created_at_utc",
                table: "user_status_history",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_user_status_history_user_id",
                table: "user_status_history",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignment_overrides");

            migrationBuilder.DropTable(
                name: "user_status_history");

            migrationBuilder.DropColumn(
                name: "deactivated_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_suspended",
                table: "users");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "users");

            migrationBuilder.DropColumn(
                name: "suspended_until",
                table: "users");

            migrationBuilder.DropColumn(
                name: "suspension_reason",
                table: "users");
        }
    }
}
