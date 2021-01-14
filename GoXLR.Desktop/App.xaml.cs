using System;
using System.IO;
using System.Windows;
using GoXLR.Desktop.ViewModels;
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
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(typeof(MainWindow));
            serviceCollection.AddSingleton(typeof(MainViewModel));
            serviceCollection.AddLogging(configure =>
            {
                configure.AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "[yyyy.MM.dd HH:mm:ss] ";
                });
            });
            
            AddConfiguration(serviceCollection);
            
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var settings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

            //Get console window for logging:
            if (settings.DebugConsole)
                AllocConsole();

            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            // Removed from App.xaml: StartupUri="MainWindow.xaml"
        }

        private static void AddConfiguration(IServiceCollection serviceCollection)
        {
            //Create configuration:
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) //implement reload later?
                .Build();

            //Add configuration as IConfiguration (manual lookup):
            serviceCollection.AddSingleton(configuration);

            //Root settings:
            serviceCollection.Configure<AppSettings>(configuration);

            //Sectional settings:
            serviceCollection.Configure<WebSocketServerSettings>(configuration.GetSection("WebSocketServerSettings"));
        }

#if DEBUG
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        internal static extern bool AllocConsole();
#endif
    }

}
