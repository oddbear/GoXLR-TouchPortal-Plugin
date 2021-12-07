using System;
using System.IO;
using GoXLR.Server;
using GoXLR.Server.Configuration;
using GoXLR.TouchPortal.Plugin.Client;
using Lamar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TouchPortalSDK.Configuration;
using TouchPortalSDK.Interfaces;

namespace GoXLR.TouchPortal.Plugin
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var container = new Container(cfg =>
            {
                var configuration = BuildConfiguration();

                AddLogging(cfg, configuration);
                AddTouchPortalPlugin(cfg, configuration);
                AddGoXLRServer(cfg, configuration);
            });

            var logger = container.GetInstance<ILogger<Program>>();

            //Start the GoXLR Server:
            logger.LogInformation("Starting: GoXLR Server");
            container.GetInstance<GoXLRServer>().Start();
            logger.LogInformation("Started: GoXLR Server");

            //Connect the Touch Portal Client to Touch Portal:
            logger.LogInformation("Connecting: Touch Portal client");
            container.GetInstance<ITouchPortalClient>().Connect();
            logger.LogInformation("Connected: TouchPortal client");

            container.GetInstance<GoXLRPlugin>();
            logger.LogInformation("Plugin is now running.");
        }

        private static void AddGoXLRServer(ServiceRegistry serviceRegistry, IConfiguration configuration)
        {
            serviceRegistry.Configure<WebSocketServerSettings>(configuration.GetSection("WebSocketServerSettings"));

            serviceRegistry.IncludeRegistry<GoXLRServerRegistry>();
        }

        private static void AddTouchPortalPlugin(ServiceRegistry serviceRegistry, IConfiguration configuration)
        {
            //Add General AppSettings:
            serviceRegistry.Configure<AppSettings>(configuration);

            serviceRegistry.AddTouchPortalSdk(configuration);

            serviceRegistry.IncludeRegistry<GoXLRPluginRegistry>();
        }

        private static void AddLogging(ServiceRegistry serviceRegistry, IConfiguration configuration)
        {
            serviceRegistry.AddLogging(configure =>
            {
                configure.AddSimpleConsole(options => options.TimestampFormat = "[yyyy.MM.dd HH:mm:ss] ");
                configure.AddConfiguration(configuration.GetSection("Logging"));
            });
        }

        private static IConfiguration BuildConfiguration()
        {
            var configurationRoot = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json")
                .Build();

            return configurationRoot;
        }
    }
}
