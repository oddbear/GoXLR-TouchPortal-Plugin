using System;
using System.IO;
using System.Windows;
using GoXLR.Desktop.ViewModels;
using GoXLR.Shared;
using GoXLR.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoXLR.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var configurationRoot = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json")
                .Build();

            var serviceCollection = new ServiceCollection();

            //Add Logging:
            serviceCollection.AddLogging(configure => configure.AddSimpleConsole(options => options.TimestampFormat = "[yyyy.MM.dd HH:mm:ss] "));

            //Add AppSettings:
            serviceCollection.Configure<AppSettings>(configurationRoot);

            //Add Main Program:
            serviceCollection.AddSingleton(typeof(MainWindow));
            serviceCollection.AddSingleton(typeof(MainViewModel));
            
            //Add WebSocket Server:
            WebSocketServerStartup.ConfigureWebSocketServer(serviceCollection, configurationRoot);
            WebSocketServerStartup.AddWebSocketServer(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            
            var settings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

            //Get console window for logging:
            if (settings.DebugConsole)
                AllocConsole();

            //Logs to console, if AllocConsole is called:
            var logger = serviceProvider.GetRequiredService<ILogger<App>>();

            //Init GoXLR Server:
            logger.LogInformation("Initializing GoXLR Server");
            var goXlrServer = serviceProvider.GetRequiredService<GoXLRServer>();
            goXlrServer.Init();
            logger.LogInformation("GoXLR Server initialized");
            
            //Run application:
            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            // Removed from App.xaml: StartupUri="MainWindow.xaml"
        }
        
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        internal static extern bool AllocConsole();
    }

}
