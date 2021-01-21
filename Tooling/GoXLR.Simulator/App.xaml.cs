using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using GoXLR.Simulator.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoXLR.Simulator
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

            //Add Main Program:
            serviceCollection.AddSingleton(typeof(MainWindow));
            serviceCollection.AddSingleton(typeof(MainViewModel));

            serviceCollection.AddLogging(configure => configure.AddSimpleConsole(options => options.TimestampFormat = "[yyyy.MM.dd HH:mm:ss] "));

            serviceCollection.Configure<AppSettings>(configurationRoot);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var settings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

            //Get console window for logging:
            if (settings.DebugConsole)
                AllocConsole();

            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            // Removed from App.xaml: StartupUri="MainWindow.xaml"
        }
        
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        internal static extern bool AllocConsole();
    }
}
