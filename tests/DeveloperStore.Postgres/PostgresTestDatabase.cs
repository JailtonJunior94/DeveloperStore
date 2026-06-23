using DeveloperStore.ORM;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DeveloperStore.Postgres;

internal static class PostgresTestDatabase
{
    private const string ConnectionStringEnvironmentVariable = "POSTGRES_TEST_CONNECTION_STRING";

    public static string GetRequiredConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"{ConnectionStringEnvironmentVariable} must be configured to run PostgreSQL proof tests.");
        }

        return connectionString;
    }

    public static DbContextOptions<DefaultContext> BuildOptions()
    {
        return new DbContextOptionsBuilder<DefaultContext>()
            .UseNpgsql(GetRequiredConnectionString(), options => options.MigrationsAssembly("DeveloperStore.ORM"))
            .EnableDetailedErrors()
            .Options;
    }

    public static async Task ResetAsync()
    {
        await EnsureDatabaseExistsAsync();
        await using var context = new DefaultContext(BuildOptions());
        try
        {
            await context.Database.EnsureDeletedAsync();
        }
        catch (PostgresException exception) when (exception.SqlState == "3D000")
        {
            // The database may not exist yet on the first run; that is equivalent to "already deleted".
        }

        await context.Database.MigrateAsync();
    }

    public static string BuildWebApiConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder(GetRequiredConnectionString());
        return builder.ConnectionString;
    }

    private static async Task EnsureDatabaseExistsAsync()
    {
        var target = new NpgsqlConnectionStringBuilder(GetRequiredConnectionString());
        var databaseName = string.IsNullOrWhiteSpace(target.Database)
            ? throw new InvalidOperationException("POSTGRES_TEST_CONNECTION_STRING must include a database name.")
            : target.Database;

        var admin = new NpgsqlConnectionStringBuilder(target.ConnectionString)
        {
            Database = "postgres"
        };

        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync();

        await using var existsCommand = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @databaseName", connection);
        existsCommand.Parameters.AddWithValue("databaseName", databaseName);

        var exists = await existsCommand.ExecuteScalarAsync() is not null;
        if (exists)
        {
            return;
        }

        await using var createCommand = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", connection);
        await createCommand.ExecuteNonQueryAsync();
    }
}
