using DeveloperStore.Domain.Repositories;
using DeveloperStore.ORM.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DeveloperStore.IoC.ModuleInitializers;

public class InfrastructureModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<ISaleRepository, SaleRepository>();
    }
}
