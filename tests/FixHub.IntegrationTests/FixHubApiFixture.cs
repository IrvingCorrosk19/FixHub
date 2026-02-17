using Testcontainers.PostgreSql;
using Xunit;

namespace FixHub.IntegrationTests;

/// <summary>
/// Fixture que levanta PostgreSQL con Testcontainers y expone un HttpClient contra la API.
/// Tests reproducibles: cada run usa un contenedor fresco con migraciones aplicadas.
/// </summary>
public class FixHubApiFixture : IAsyncLifetime, IDisposable
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("FixHub")
        .WithUsername("postgres")
        .WithPassword("test")
        .Build();

    private FixHubApiFactory? _factory;

    public HttpClient Client => _factory!.CreateClient();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var connectionString = _postgres.GetConnectionString();
        _factory = new FixHubApiFactory(connectionString);
    }

    public void Dispose() => _postgres.DisposeAsync().AsTask().GetAwaiter().GetResult();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();
}
