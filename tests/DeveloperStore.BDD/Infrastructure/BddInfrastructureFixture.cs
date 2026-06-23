using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;
using DefaultContext = DeveloperStore.ORM.DefaultContext;

namespace DeveloperStore.BDD.Infrastructure;

public sealed class BddInfrastructureFixture : IAsyncLifetime, IDisposable
{
    private PostgreSqlContainer _container = null!;
    private BddWebApplicationFactory _factory = null!;

    public string ConnectionString => _container.GetConnectionString();

    public BddWebApplicationFactory Factory => _factory;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithAutoRemove(true)
            .Build();

        await _container.StartAsync();

        NpgsqlConnection.ClearAllPools();

        _factory = new BddWebApplicationFactory(ConnectionString);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DefaultContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();

        if (_container is not null)
            await _container.DisposeAsync();
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}
