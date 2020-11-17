using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TouchPortalApi;

namespace GoXLR_TouchPortal_Plugin
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.ConfigureTouchPointApi(options =>
                    {
                        options.ServerIp = "127.0.0.1";
                        options.ServerPort = 12136;
                        options.PluginId = "TP-GoXLR";
                    });
                });
    }
}
