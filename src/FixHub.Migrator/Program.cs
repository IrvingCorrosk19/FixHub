using FixHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings__DefaultConnection must be set (env var).");

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
    .Options;

using var db = new AppDbContext(options);
await db.Database.MigrateAsync();
Console.WriteLine("Migrations applied successfully.");
