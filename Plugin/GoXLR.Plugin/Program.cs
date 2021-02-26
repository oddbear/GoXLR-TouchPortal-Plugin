using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GoXLR.Plugin.Client;
using GoXLR.Server;
using GoXLR.Server.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            serviceCollection.AddLogging(configure => configure.AddSimpleConsole(options => options.TimestampFormat = "[yyyy.MM.dd HH:mm:ss] "));

            var logger = serviceCollection
                .BuildServiceProvider()
                .GetRequiredService<ILogger<Program>>();
            
            //Add AppSettings:
            serviceCollection.Configure<AppSettings>(configurationRoot);

            //Add TouchPortal Client:
            serviceCollection.AddScoped<TouchPortalClient>();

            //Add WebSocket Server:
            serviceCollection.Configure<WebSocketServerSettings>(configurationRoot.GetSection("WebSocketServerSettings"));
            serviceCollection.AddScoped<GoXLRServer>();

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

            logger.LogInformation("Plugin is now running.");
        }
    }
}
