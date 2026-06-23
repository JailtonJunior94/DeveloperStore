using DeveloperStore.BDD.Infrastructure;
using DeveloperStore.ORM;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Reqnroll;
using Reqnroll.BoDi;

namespace DeveloperStore.BDD.Hooks;

[Binding]
public static class GlobalHooks
{
    private static readonly Lazy<Task<BddInfrastructureFixture>> FixtureLazy = new(async () =>
    {
        var fixture = new BddInfrastructureFixture();
        await fixture.InitializeAsync();
        return fixture;
    });

    public static Task<BddInfrastructureFixture> Fixture => FixtureLazy.Value;

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        _ = await Fixture;
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        if (FixtureLazy.IsValueCreated)
        {
            var fixture = await Fixture;
            fixture.Dispose();
        }
    }
}

[Binding]
public sealed class ScenarioHooks
{
    private readonly IObjectContainer _objectContainer;

    public ScenarioHooks(IObjectContainer objectContainer)
    {
        _objectContainer = objectContainer;
    }

    [BeforeScenario(Order = 0)]
    public async Task BeforeScenario()
    {
        var fixture = await GlobalHooks.Fixture;
        var factory = fixture.Factory;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DefaultContext>();
            await db.Database.ExecuteSqlRawAsync(
                "TRUNCATE TABLE sale_items, sales RESTART IDENTITY CASCADE");
        }

        NpgsqlConnection.ClearAllPools();

        var client = factory.CreateClient();
        var driver = new SalesApiDriver(client);
        _objectContainer.RegisterInstanceAs(factory);
        _objectContainer.RegisterInstanceAs(client);
        _objectContainer.RegisterInstanceAs(driver);
    }

    [AfterScenario(Order = 0)]
    public async Task AfterScenario()
    {
        if (_objectContainer.IsRegistered<HttpClient>())
        {
            var client = _objectContainer.Resolve<HttpClient>();
            client.Dispose();
        }

        NpgsqlConnection.ClearAllPools();
    }
}
