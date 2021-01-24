using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TouchPortalApi;
using TouchPortalApi.Interfaces;

namespace GoXLR.Plugin.Client
{
    public static class TouchPortalClientConfiguration
    {
        public static void ConfigureTouchPortalApi(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<TouchPortalClient>();

            serviceCollection.ConfigureTouchPointApi(options =>
            {
                options.ServerIp = "127.0.0.1";
                options.ServerPort = 12136;
                options.PluginId = "oddbear.touchportal.goxlr";
            });

            serviceCollection.AddScoped<Task<IMessageProcessor>>(async provider =>
            {
                var logger = provider.GetService<ILogger<Program>>();
                var settings = provider.GetRequiredService<IOptions<AppSettings>>();
                if (!TimeSpan.TryParse(settings.Value.ReconnectWaitTime, out var reconnectWaitTime))
                    reconnectWaitTime = TimeSpan.FromSeconds(5);

                while (true)
                {
                    try
                    {
                        //retry until we have connection:
                        //The API can throw exceptions in the constructor.
                        //Strange that it even works, being a Singleton and all (is this undefined behaviour?). :(
                        return provider.GetRequiredService<IMessageProcessor>();
                    }
                    //When TouchPortal is not running:
                    // System.Net.Internals.SocketExceptionFactory.ExtendedSocketException:
                    //  'No connection could be made because the target machine actively refused it. 127.0.0.1:12136'
                    catch (SocketException e)
                        when (e.Message.StartsWith("No connection could be made because the target machine actively refused it. "))
                    {
                        logger.LogInformation("Retry connection to TouchPortal");
                        await Task.Delay(reconnectWaitTime);
                    }
                }
            });
        }
    }
}
