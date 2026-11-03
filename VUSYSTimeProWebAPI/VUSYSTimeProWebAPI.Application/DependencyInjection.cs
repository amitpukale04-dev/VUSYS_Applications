using Microsoft.Extensions.DependencyInjection;

namespace VUSYSTimeProWebAPI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => { }, typeof(DependencyInjection).Assembly);
        return services;
    }
}