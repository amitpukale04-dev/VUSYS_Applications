using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using VUSYSTimeProScheduler;
using VUSYSTimeProScheduler.Repository;
class Program
{
    static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(config =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Bind config to CommonHelper.KeyOption
                context.Configuration.GetSection("MyApplicationKeys").Bind(CommonHelper.KeyOption);
                services.AddScoped<ITimeProSchedulerRepository, TimeProSchedulerRepository>();

                services.AddScoped<MainApp>();
            })
            .Build();

        using var scope = host.Services.CreateScope();
        var app = scope.ServiceProvider.GetRequiredService<MainApp>();
        await app.Run(args);
    }
}
