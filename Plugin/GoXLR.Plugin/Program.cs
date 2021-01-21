using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using GoXLR.Plugin.Client;
using GoXLR.Shared;
using GoXLR.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TouchPortalApi;
using TouchPortalApi.Interfaces;

namespace GoXLR.Plugin
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var configurationRoot = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json")
                .Build();

            var serviceCollection = new ServiceCollection();

            //Add Logging:
            LoggingStartup.AddLogging(serviceCollection);

            var logger = serviceCollection
                .BuildServiceProvider()
                .GetRequiredService<ILogger<Program>>();

            //Add AppSettings:
            serviceCollection.Configure<AppSettings>(configurationRoot);

            //Add TouchPortal Client:
            ConfigureTouchPortalApi(serviceCollection);
            
            //Add WebSocket Server:
            WebSocketServerStartup.ConfigureWebSocketServer(serviceCollection, configurationRoot);
            WebSocketServerStartup.AddWebSocketServer(serviceCollection);

            var rootServiceProvider = serviceCollection.BuildServiceProvider();

            using var scope = rootServiceProvider.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            
            //Init GoXLR Server:
            logger.LogInformation("Initializing GoXLR Server");
            var goXlrServer = serviceProvider.GetRequiredService<GoXLRServer>();
            goXlrServer.Init();
            logger.LogInformation("GoXLR Server initialized");

            //Init TouchPortal:
            logger.LogInformation("Initializing TouchPortal client");
            var touchPortalClient = serviceProvider.GetRequiredService<TouchPortalClient>();
            await touchPortalClient.InitAsync();
            logger.LogInformation("TouchPortal client initialized");
            
            Console.WriteLine("Plugin is now running.");

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }
        
        private static void ConfigureTouchPortalApi(IServiceCollection serviceCollection)
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
