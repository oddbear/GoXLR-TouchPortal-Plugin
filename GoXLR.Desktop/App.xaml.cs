using System.Windows;
using GoXLR.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoXLR.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //Could create configuration
            //var builder = new ConfigurationBuilder()
            //    .SetBasePath(Directory.GetCurrentDirectory())
            //    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            //
            //var vonfiguration = builder.Build();

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

            var serviceProvider = serviceCollection.BuildServiceProvider();

#if DEBUG
            //Get console window for logging:
            AllocConsole();
#endif
            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            // Removed from App.xaml: StartupUri="MainWindow.xaml"
        }

#if DEBUG
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        internal static extern bool AllocConsole();
#endif
    }

}
