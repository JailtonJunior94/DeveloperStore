using Microsoft.AspNetCore.Builder;

namespace DeveloperStore.IoC;

public interface IModuleInitializer
{
    void Initialize(WebApplicationBuilder builder);
}
