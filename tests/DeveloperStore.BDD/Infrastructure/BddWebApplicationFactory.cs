using DeveloperStore.ORM;
using DeveloperStore.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DeveloperStore.BDD.Infrastructure;

public sealed class BddWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public BddWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<DefaultContext>));
            services.RemoveAll<DefaultContext>();
            services.AddDbContext<DefaultContext>(options =>
                options.UseNpgsql(_connectionString, o => o.MigrationsAssembly("DeveloperStore.ORM")));
        });
    }

    public DefaultContext CreateTestDbContext()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseNpgsql(_connectionString, o => o.MigrationsAssembly("DeveloperStore.ORM"))
            .Options;
        return new DefaultContext(options);
    }
}
