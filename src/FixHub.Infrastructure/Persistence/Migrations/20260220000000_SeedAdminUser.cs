using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixHub.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Inserta un usuario Admin por defecto para poder asignar técnicos a los trabajos.
    /// Email: admin@fixhub.com / Password: Admin123!
    /// </summary>
    public partial class SeedAdminUser : Migration
    {
        private const string AdminEmail = "admin@fixhub.com";
        private const string AdminId = "a0000001-0001-0001-0001-000000000001";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("Admin123!", 12);
            var hashEscaped = hash.Replace("'", "''");

            migrationBuilder.Sql($@"
INSERT INTO users (id, email, password_hash, full_name, role, is_active, created_at)
VALUES ('{AdminId}', '{AdminEmail}', '{hashEscaped}', 'Admin FixHub', 3, true, NOW())
ON CONFLICT (email) DO NOTHING;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
DELETE FROM users WHERE email = '{AdminEmail}';");
        }
    }
}
