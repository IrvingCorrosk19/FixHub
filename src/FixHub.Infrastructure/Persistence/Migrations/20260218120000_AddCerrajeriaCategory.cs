using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCerrajeriaCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "service_categories",
                columns: new[] { "id", "icon", "is_active", "name" },
                values: new object[] { 6, "key", true, "Cerrajería" });

            migrationBuilder.Sql(
                "SELECT setval(pg_get_serial_sequence('service_categories', 'id'), 6);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "service_categories",
                keyColumn: "id",
                keyValue: 6);
        }
    }
}
