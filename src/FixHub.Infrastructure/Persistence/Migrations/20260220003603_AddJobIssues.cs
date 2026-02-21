using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // audit_logs ya existe desde AddAuditLogs; no renombrar AuditLog
            migrationBuilder.CreateTable(
                name: "job_issues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reported_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    detail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_issues", x => x.id);
                    table.ForeignKey(
                        name: "FK_job_issues_jobs_job_id",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_job_issues_users_reported_by_user_id",
                        column: x => x.reported_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_issues_created_at",
                table: "job_issues",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_job_issues_job_id",
                table: "job_issues",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_issues_job_id_created_at",
                table: "job_issues",
                columns: new[] { "job_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_job_issues_reported_by_user_id",
                table: "job_issues",
                column: "reported_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "job_issues");
        }
    }
}
